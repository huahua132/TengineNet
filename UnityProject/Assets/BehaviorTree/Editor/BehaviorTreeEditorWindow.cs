#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTreeRuntime = BehaviorTree.BehaviorTree;

namespace BehaviorTree.Editor
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        #region Fields
        private BehaviorTreeAsset _currentAsset;
        private Vector2 _scrollPosition;
        private Vector2 _offset;
        private float _zoom = 1.0f;
        
        private BehaviorNodeData _selectedNode;
        private BehaviorNodeData _connectingNode;
        private int _nextNodeId = 1;
        
        // 未保存状态
        private bool _isDirty = false;
        
        // 左侧面板
        private Vector2 _leftPanelScroll;
        private float _leftPanelWidth = 250f;
        private bool _leftPanelFoldout = true;
        
        // 右侧面板
        private Vector2 _rightPanelScroll;
        private float _rightPanelWidth = 350f;
        private bool _rightPanelFoldout = true;
        
        // 节点视图
        private const float NODE_WIDTH = 200f;
        private const float NODE_HEIGHT = 60f;
        private const float GRID_SIZE = 20f;
        private const float TOOLBAR_HEIGHT = 20f;
        
        // 拖拽
        private BehaviorNodeTypeInfo _draggingNodeType;
        private bool _isDragging = false;
        
        // 运行时调试
        private BehaviorTreeRuntime _runtimeTree;
        private bool _isDebugMode = false;
        private Dictionary<int, BehaviorRet> _nodeRuntimeStatus = new Dictionary<int, BehaviorRet>();
        
        // 折叠状态
        private Dictionary<BehaviorProcessType, bool> _categoryFoldouts = new Dictionary<BehaviorProcessType, bool>();
        #endregion

        [MenuItem("Tools/BehaviorTree/Editor Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<BehaviorTreeEditorWindow>();
            window.minSize = new Vector2(1200, 600);
            window.UpdateTitle();
        }
        
        private void UpdateTitle()
        {
            string title = "Behavior Tree Editor";
            if (_currentAsset != null)
            {
                title += $" - {_currentAsset.name}";
            }
            if (_isDirty)
            {
                title += " *";
            }
            titleContent = new GUIContent(title);
        }

        private void OnEnable()
        {
            _offset = Vector2.zero;
            
            // 启用键盘事件
            wantsMouseMove = true;
            
            // 初始化折叠状态
            foreach (BehaviorProcessType type in System.Enum.GetValues(typeof(BehaviorProcessType)))
            {
                _categoryFoldouts[type] = true;
            }
        }

        private void OnGUI()
        {
            // 每帧更新标题（确保未保存状态正确显示）
            if (_isDirty && !titleContent.text.EndsWith(" *"))
            {
                UpdateTitle();
            }
            
            // 首先处理全局事件（在绘制之前）
            ProcessGlobalEvents();
            
            DrawToolbar();
            
            Rect mainRect = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);
            
            // 左侧节点面板
            if (_leftPanelFoldout)
            {
                DrawLeftPanel(new Rect(0, TOOLBAR_HEIGHT, _leftPanelWidth, mainRect.height));
            }
            
            // 中间画布
            float canvasX = _leftPanelFoldout ? _leftPanelWidth : 0;
            float canvasWidth = position.width - canvasX - (_rightPanelFoldout ? _rightPanelWidth : 0);
            Rect canvasRect = new Rect(canvasX, TOOLBAR_HEIGHT, canvasWidth, mainRect.height);
            DrawCanvas(canvasRect);
            
            // 右侧属性面板
            if (_rightPanelFoldout && _selectedNode != null)
            {
                DrawRightPanel(new Rect(position.width - _rightPanelWidth, TOOLBAR_HEIGHT, _rightPanelWidth, mainRect.height));
            }
            
            ProcessCanvasEvents();
            
            if (_isDragging)
            {
                Repaint();
            }
        }

        #region Toolbar
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 左侧面板开关
            _leftPanelFoldout = GUILayout.Toggle(_leftPanelFoldout, "Nodes", EditorStyles.toolbarButton, GUILayout.Width(60));

            GUILayout.Space(10);

            // 资源选择
            EditorGUI.BeginChangeCheck();
            _currentAsset = (BehaviorTreeAsset)EditorGUILayout.ObjectField(
                _currentAsset, typeof(BehaviorTreeAsset), false, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                OnAssetChanged();
            }

            GUILayout.FlexibleSpace();

            // 调试模式开关
            Color oldColor = GUI.backgroundColor;
            if (_isDebugMode)
            {
                GUI.backgroundColor = Color.green;
            }
            if (GUILayout.Button(_isDebugMode ? "Debug: ON" : "Debug: OFF", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                _isDebugMode = !_isDebugMode;
            }
            GUI.backgroundColor = oldColor;

            GUILayout.Space(10);

            // 文件操作
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                CreateNewAsset();
            }

            // 保存按钮 - 未保存时高亮显示
            Color saveButtonColor = GUI.backgroundColor;
            if (_isDirty)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // 淡红色表示有未保存更改
            }
            string saveText = _isDirty ? "Save *" : "Save";
            if (GUILayout.Button(saveText, EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveAsset();
            }
            GUI.backgroundColor = saveButtonColor;

            GUILayout.Space(10);

            // 右侧面板开关
            _rightPanelFoldout = GUILayout.Toggle(_rightPanelFoldout, "Inspector", EditorStyles.toolbarButton, GUILayout.Width(70));

            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Left Panel - Node Library
        private void DrawLeftPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, GUI.skin.box);
            
            EditorGUILayout.LabelField("Node Library", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            _leftPanelScroll = EditorGUILayout.BeginScrollView(_leftPanelScroll);
            
            // 按类型分组显示节点
            foreach (BehaviorProcessType type in System.Enum.GetValues(typeof(BehaviorProcessType)))
            {
                DrawNodeCategory(type);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }

        private void DrawNodeCategory(BehaviorProcessType type)
        {
            var nodes = BehaviorNodeRegistry.GetNodesByType(type);
            if (nodes.Count == 0) return;

            // 分类标题
            EditorGUILayout.BeginHorizontal();
            
            Color typeColor = BehaviorNodeRegistry.GetTypeColor(type);
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = typeColor;
            
            _categoryFoldouts[type] = EditorGUILayout.Foldout(
                _categoryFoldouts[type], 
                $"{type} ({nodes.Count})", 
                true,
                EditorStyles.foldoutHeader);
            
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();

            if (!_categoryFoldouts[type]) return;

            EditorGUI.indentLevel++;
            
            // 显示该分类下的所有节点
            foreach (var nodeInfo in nodes)
            {
                DrawNodeButton(nodeInfo);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        private void DrawNodeButton(BehaviorNodeTypeInfo nodeInfo)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 图标
            GUIContent iconContent = EditorGUIUtility.IconContent(nodeInfo.Icon);
            if (iconContent != null && iconContent.image != null)
            {
                GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(20));
            }
            
            // 按钮
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = nodeInfo.Color * 0.8f;
            
            if (GUILayout.Button(nodeInfo.Name, GUILayout.Height(25)))
            {
                AddNodeToCanvas(nodeInfo, Event.current.mousePosition);
            }
            
            GUI.backgroundColor = oldColor;
            
            // 处理拖拽开始
            if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                _draggingNodeType = nodeInfo;
                _isDragging = true;
                Event.current.Use();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示描述（工具提示）
            if (!string.IsNullOrEmpty(nodeInfo.Description))
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                EditorGUI.LabelField(lastRect, new GUIContent("", nodeInfo.Description));
            }
        }
        #endregion

        #region Canvas
        private void DrawCanvas(Rect rect)
        {
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            
            GUILayout.BeginArea(rect);
            
            // 绘制网格
            DrawGrid(rect);
            
            // 绘制节点和连接
            BeginWindows();
            DrawNodes();
            DrawConnections();
            EndWindows();
            
            // 处理拖拽放置
            HandleDrop(rect);
            
            GUILayout.EndArea();
        }

        private void DrawGrid(Rect rect)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            float gridSpacing = GRID_SIZE * _zoom;
            int widthDivs = Mathf.CeilToInt(rect.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(rect.height / gridSpacing);

            Vector2 gridOffset = new Vector2(_offset.x % gridSpacing, _offset.y % gridSpacing);

            for (int i = 0; i < widthDivs + 1; i++)
            {
                Handles.DrawLine(
                    new Vector3(gridSpacing * i + gridOffset.x, 0, 0),
                    new Vector3(gridSpacing * i + gridOffset.x, rect.height, 0));
            }

            for (int i = 0; i < heightDivs + 1; i++)
            {
                Handles.DrawLine(
                    new Vector3(0, gridSpacing * i + gridOffset.y, 0),
                    new Vector3(rect.width, gridSpacing * i + gridOffset.y, 0));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            if (_currentAsset == null || _currentAsset.nodes == null)
                return;

            for (int i = 0; i < _currentAsset.nodes.Count; i++)
            {
                var node = _currentAsset.nodes[i];
                DrawNode(node, i);
            }
        }

        private void DrawNode(BehaviorNodeData node, int index)
        {
            Vector2 pos = node.editorPosition * _zoom + _offset;
            Rect rect = new Rect(pos.x, pos.y, NODE_WIDTH * _zoom, NODE_HEIGHT * _zoom);

            // 获取节点信息
            var nodeInfo = BehaviorNodeRegistry.GetNodeInfo(node.processTypeName);
            Color nodeColor = nodeInfo != null ? nodeInfo.Color : Color.gray;

            // 设置颜色
            Color originalColor = GUI.backgroundColor;
            
            // 根节点用绿色边框
            if (_currentAsset != null && node.id == _currentAsset.rootId)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), Color.green);
            }
            
            // 选中节点高亮
            if (node == _selectedNode)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), Color.yellow);
            }
            
            // 运行时状态颜色
            if (_isDebugMode && _nodeRuntimeStatus.ContainsKey(node.id))
            {
                Color statusColor = GetStatusColor(_nodeRuntimeStatus[node.id]);
                EditorGUI.DrawRect(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), statusColor);
            }
            
            GUI.backgroundColor = nodeColor;

            rect = GUI.Window(index, rect, (id) =>
            {
                // 在Window内部，坐标是相对于窗口的本地坐标，已经是缩放后的尺寸
                float windowWidth = NODE_WIDTH * _zoom;
                float windowHeight = NODE_HEIGHT * _zoom;
                
                // 字体大小随缩放变化
                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel);
                nameStyle.fontSize = Mathf.RoundToInt(12 * _zoom);
                nameStyle.alignment = TextAnchor.MiddleCenter;
                nameStyle.wordWrap = false;
                
                GUIStyle typeStyle = new GUIStyle(EditorStyles.miniLabel);
                typeStyle.fontSize = Mathf.RoundToInt(9 * _zoom);
                typeStyle.alignment = TextAnchor.MiddleCenter;
                nameStyle.wordWrap = false;
                
                // 计算固定位置（相对于窗口，使用缩放后的值）
                float yPos = 8 * _zoom;
                float lineHeight = 20 * _zoom;
                
                // 绘制图标和名称
                if (nodeInfo != null)
                {
                    GUIContent iconContent = EditorGUIUtility.IconContent(nodeInfo.Icon);
                    if (iconContent != null && iconContent.image != null)
                    {
                        float iconSize = 16 * _zoom;
                        float textWidth = windowWidth * 0.7f;
                        float iconX = (windowWidth - iconSize - textWidth) / 2;
                        
                        Rect iconRect = new Rect(iconX, yPos, iconSize, iconSize);
                        GUI.Label(iconRect, iconContent);
                        
                        Rect nameRect = new Rect(iconRect.xMax + 5 * _zoom, yPos, textWidth, lineHeight);
                        GUI.Label(nameRect, node.name, nameStyle);
                        yPos += lineHeight;
                    }
                    else
                    {
                        Rect nameRect = new Rect(0, yPos, windowWidth, lineHeight);
                        GUI.Label(nameRect, node.name, nameStyle);
                        yPos += lineHeight;
                    }
                }
                else
                {
                    Rect nameRect = new Rect(0, yPos, windowWidth, lineHeight);
                    GUI.Label(nameRect, node.name, nameStyle);
                    yPos += lineHeight;
                }
                
                // 绘制类型
                Rect typeRect = new Rect(0, yPos, windowWidth, lineHeight * 0.9f);
                GUI.Label(typeRect, node.processTypeName, typeStyle);
                yPos += lineHeight * 0.9f;

                // 运行时状态显示
                if (_isDebugMode && _nodeRuntimeStatus.ContainsKey(node.id))
                {
                    GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
                    statusStyle.fontSize = Mathf.RoundToInt(8 * _zoom);
                    statusStyle.alignment = TextAnchor.MiddleCenter;
                    statusStyle.wordWrap = false;
                    
                    Rect statusRect = new Rect(0, yPos, windowWidth, lineHeight * 0.8f);
                    GUI.Label(statusRect, $"{_nodeRuntimeStatus[node.id]}", statusStyle);
                }

                // 处理右键菜单
                Event e = Event.current;
                if (e != null)
                {
                    if (e.type == EventType.MouseDown && e.button == 1) // 右键
                    {
                        ShowNodeContextMenu(node);
                        e.Use();
                    }
                    else if (e.type == EventType.ContextClick)
                    {
                        ShowNodeContextMenu(node);
                        e.Use();
                    }
                }

                GUI.DragWindow();
            }, GUIContent.none);

            GUI.backgroundColor = originalColor;

            node.editorPosition = (rect.position - _offset) / _zoom;
        }

        private Color GetStatusColor(BehaviorRet status)
        {
            switch (status)
            {
                case BehaviorRet.SUCCESS:
                    return Color.green;
                case BehaviorRet.FAIL:
                    return Color.red;
                case BehaviorRet.RUNNING:
                    return Color.yellow;
                case BehaviorRet.ABORT:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        private void DrawConnections()
        {
            if (_currentAsset == null || _currentAsset.nodes == null)
                return;

            Handles.BeginGUI();
            
            // 使用ToList()创建副本，避免在遍历时修改集合
            var nodesCopy = _currentAsset.nodes.ToList();
            foreach (var node in nodesCopy)
            {
                if (node.childrenIds == null) continue;
                
                // 也要复制子节点ID列表
                var childIdsCopy = node.childrenIds.ToList();
                foreach (var childId in childIdsCopy)
                {
                    var childNode = _currentAsset.GetNode(childId);
                    if (childNode != null)
                    {
                        DrawConnection(node, childNode);
                    }
                }
            }

            if (_connectingNode != null)
            {
                Vector2 startPos = GetNodeCenter(_connectingNode);
                DrawSmoothCurve(startPos, Event.current.mousePosition, Color.yellow, 5f);
            }

            Handles.EndGUI();
        }

        private void DrawConnection(BehaviorNodeData from, BehaviorNodeData to)
        {
            Vector2 startPos = GetNodeCenter(from);
            Vector2 endPos = GetNodeTop(to);
            
            // 绘制贝塞尔曲线连接（参考图风格）
            DrawSmoothCurve(startPos, endPos, Color.white, 5f);
            
            // 在连接线中点绘制一个小按钮用于删除连接
            Vector2 midPoint = (startPos + endPos) / 2;
            Rect deleteButtonRect = new Rect(midPoint.x - 8, midPoint.y - 8, 16, 16);
            
            if (GUI.Button(deleteButtonRect, "×", EditorStyles.miniButton))
            {
                DisconnectNodes(from, to);
            }
        }

        private void DrawSmoothCurve(Vector2 start, Vector2 end, Color color, float thickness)
        {
            // 计算控制点，创建流畅的S形曲线
            float distance = Vector2.Distance(start, end);
            float tangentLength = Mathf.Min(distance * 0.5f, 80f);
            
            Vector2 startTangent = start + Vector2.down * tangentLength;
            Vector2 endTangent = end + Vector2.up * tangentLength;
            
            // 绘制贝塞尔曲线
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, thickness);
        }
        
        private void DrawBezier(Vector2 start, Vector2 end, Color color)
        {
            Vector2 startTangent = start + Vector2.down * 50;
            Vector2 endTangent = end + Vector2.up * 50;
            
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 3f);
        }
        
        private Vector2 GetNodeTop(BehaviorNodeData node)
        {
            Vector2 pos = node.editorPosition * _zoom + _offset;
            return new Vector2(
                pos.x + NODE_WIDTH * _zoom * 0.5f,
                pos.y);
        }

        private Vector2 GetNodeCenter(BehaviorNodeData node)
        {
            Vector2 pos = node.editorPosition * _zoom + _offset;
            return new Vector2(
                pos.x + NODE_WIDTH * _zoom * 0.5f,
                pos.y + NODE_HEIGHT * _zoom);
        }

        private void HandleDrop(Rect canvasRect)
        {
            Event e = Event.current;
            
            if (_isDragging && e.type == EventType.MouseUp && e.button == 0)
            {
                if (canvasRect.Contains(e.mousePosition) && _draggingNodeType != null)
                {
                    // 在画布上放置节点
                    Vector2 localPos = e.mousePosition - canvasRect.position;
                    AddNodeToCanvas(_draggingNodeType, localPos);
                }
                
                _isDragging = false;
                _draggingNodeType = null;
                e.Use();
            }
        }
        #endregion

        #region Right Panel - Inspector
        private void DrawRightPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, GUI.skin.box);
            
            EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            _rightPanelScroll = EditorGUILayout.BeginScrollView(_rightPanelScroll);
            
            if (_selectedNode != null)
            {
                DrawNodeInspector();
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }

        private void DrawNodeInspector()
        {
            EditorGUILayout.BeginVertical();
            
            // 基本信息
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _selectedNode.name = EditorGUILayout.TextField("名称", _selectedNode.name);
            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
            }
            EditorGUILayout.LabelField("类型", _selectedNode.processTypeName);
            EditorGUILayout.LabelField("ID", _selectedNode.id.ToString());
            
            // 显示节点描述
            var nodeInfo = BehaviorNodeRegistry.GetNodeInfo(_selectedNode.processTypeName);
            if (nodeInfo != null && !string.IsNullOrEmpty(nodeInfo.Description))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(nodeInfo.Description, MessageType.Info);
            }

            EditorGUILayout.Space(10);
            
            // 节点备注
            EditorGUILayout.LabelField("备注", EditorStyles.boldLabel);
            // 确保comment不为null
            if (_selectedNode.comment == null)
            {
                _selectedNode.comment = "";
            }
            EditorGUI.BeginChangeCheck();
            _selectedNode.comment = EditorGUILayout.TextArea(_selectedNode.comment, GUILayout.Height(60));
            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
            }
            
            EditorGUILayout.Space(10);
            
            // 节点参数
            EditorGUILayout.LabelField("参数配置", EditorStyles.boldLabel);
            DrawNodeParameters();

            EditorGUILayout.Space(10);

            // 操作按钮
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            if (GUILayout.Button("设为根节点", GUILayout.Height(25)))
            {
                if (_currentAsset != null)
                {
                    _currentAsset.rootId = _selectedNode.id;
                    MarkDirty();
                }
            }

            if (GUILayout.Button("删除节点", GUILayout.Height(25)))
            {
                DeleteNode(_selectedNode);
                _selectedNode = null;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawNodeParameters()
        {
            if (_selectedNode == null) return;
            
            // 使用反射获取节点类型的公共字段
            var nodeInfo = BehaviorNodeRegistry.GetNodeInfo(_selectedNode.processTypeName);
            if (nodeInfo == null) return;
            
            var fields = nodeInfo.Type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.DeclaringType == nodeInfo.Type).ToList();
            
            if (fields.Count == 0)
            {
                EditorGUILayout.HelpBox("此节点没有可配置的参数", MessageType.Info);
                return;
            }
            
            // 确保parametersList已初始化
            if (_selectedNode.parametersList == null)
            {
                _selectedNode.parametersList = new List<SerializableParameter>();
            }
            
            foreach (var field in fields)
            {
                string fieldName = field.Name;
                string currentValue = _selectedNode.HasParameter(fieldName)
                    ? _selectedNode.GetParameter(fieldName)
                    : GetDefaultValue(field);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(fieldName, GUILayout.Width(100));
                
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUILayout.TextField(currentValue);
                if (EditorGUI.EndChangeCheck() && newValue != currentValue)
                {
                    _selectedNode.SetParameter(fieldName, newValue);
                    MarkDirty();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private string GetDefaultValue(FieldInfo field)
        {
            var defaultAttr = field.GetCustomAttribute<System.ComponentModel.DefaultValueAttribute>();
            if (defaultAttr != null)
            {
                return defaultAttr.Value?.ToString() ?? "";
            }
            
            // 根据类型返回默认值
            if (field.FieldType == typeof(string))
                return "";
            if (field.FieldType == typeof(int))
                return "0";
            if (field.FieldType == typeof(float))
                return "0.0";
            if (field.FieldType == typeof(bool))
                return "false";
            
            return "";
        }
        #endregion

        #region Event Processing
        // 全局事件处理（键盘事件）
        private void ProcessGlobalEvents()
        {
            Event e = Event.current;
            if (e == null) return;

            // 键盘事件处理
            if (e.type == EventType.KeyDown)
            {
                bool isCtrl = e.control || e.command;
                
                // Ctrl+S 保存
                if (isCtrl && e.keyCode == KeyCode.S)
                {
                    SaveAsset();
                    e.Use();
                    return;
                }
                
                // Ctrl+Z 撤销（暂未实现撤销系统）
                if (isCtrl && e.keyCode == KeyCode.Z)
                {
                    Debug.Log("撤销功能暂未实现");
                    e.Use();
                    return;
                }
                
                // ESC 关闭编辑器或取消连接
                if (e.keyCode == KeyCode.Escape)
                {
                    if (_connectingNode != null)
                    {
                        _connectingNode = null;
                        e.Use();
                        Repaint();
                    }
                    else
                    {
                        // 关闭编辑器窗口
                        Close();
                        e.Use();
                    }
                    return;
                }
                
                // Delete键删除选中节点
                if (e.keyCode == KeyCode.Delete && _selectedNode != null)
                {
                    DeleteNode(_selectedNode);
                    _selectedNode = null;
                    e.Use();
                    Repaint();
                    return;
                }
            }
            
            // KeyUp作为备选
            if (e.type == EventType.KeyUp)
            {
                if (e.keyCode == KeyCode.Delete && _selectedNode != null)
                {
                    DeleteNode(_selectedNode);
                    _selectedNode = null;
                    e.Use();
                    Repaint();
                }
            }
        }
        
        // 画布事件处理（鼠标事件）
        private void ProcessCanvasEvents()
        {
            Event e = Event.current;
            if (e == null) return;

            // 节点选择
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _selectedNode = GetNodeAtPosition(e.mousePosition);
                Repaint();
            }

            // 画布拖动
            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                _offset += e.delta;
                Repaint();
            }

            // 缩放
            if (e.type == EventType.ScrollWheel)
            {
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.01f, 0.5f, 2f);
                Repaint();
            }
        }

        private BehaviorNodeData GetNodeAtPosition(Vector2 mousePosition)
        {
            if (_currentAsset == null || _currentAsset.nodes == null)
                return null;

            // 调整鼠标位置（考虑左侧面板偏移）
            float canvasX = _leftPanelFoldout ? _leftPanelWidth : 0;
            Vector2 adjustedPos = new Vector2(mousePosition.x - canvasX, mousePosition.y - TOOLBAR_HEIGHT);

            for (int i = _currentAsset.nodes.Count - 1; i >= 0; i--)
            {
                var node = _currentAsset.nodes[i];
                Vector2 pos = node.editorPosition * _zoom + _offset;
                Rect rect = new Rect(pos.x, pos.y, NODE_WIDTH * _zoom, NODE_HEIGHT * _zoom);
                
                if (rect.Contains(adjustedPos))
                {
                    return node;
                }
            }

            return null;
        }
        #endregion

        #region Node Operations
        private void ShowNodeContextMenu(BehaviorNodeData node)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("设为根节点"), false, () =>
            {
                if (_currentAsset != null)
                {
                    _currentAsset.rootId = node.id;
                    MarkDirty();
                }
            });

            menu.AddItem(new GUIContent("连接到..."), false, () =>
            {
                _connectingNode = node;
            });

            if (_connectingNode != null && _connectingNode != node)
            {
                menu.AddItem(new GUIContent("连接到这里"), false, () =>
                {
                    ConnectNodes(_connectingNode, node);
                    _connectingNode = null;
                });
            }

            menu.AddItem(new GUIContent("取消连接"), _connectingNode != null, () =>
            {
                _connectingNode = null;
            });

            menu.AddSeparator("");
            
            // 断开所有连接
            if (node.childrenIds != null && node.childrenIds.Count > 0)
            {
                menu.AddItem(new GUIContent("断开所有子节点"), false, () =>
                {
                    node.childrenIds.Clear();
                    MarkDirty();
                    Repaint();
                });
            }
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("重新分配节点ID"), false, () => ReassignNodeIds());
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除"), false, () => DeleteNode(node));

            menu.ShowAsContext();
        }
        
        /// <summary>
        /// 按照树的层级顺序重新分配节点ID
        /// </summary>
        private void ReassignNodeIds()
        {
            if (_currentAsset == null || _currentAsset.nodes == null)
                return;

            // 找到根节点
            var rootNode = _currentAsset.GetNode(_currentAsset.rootId);
            if (rootNode == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到根节点！", "确定");
                return;
            }

            // 创建旧ID到新ID的映射
            Dictionary<int, int> idMapping = new Dictionary<int, int>();
            int newId = 1;

            // 广度优先遍历，按连接顺序分配ID
            Queue<BehaviorNodeData> queue = new Queue<BehaviorNodeData>();
            HashSet<int> visited = new HashSet<int>();
            
            queue.Enqueue(rootNode);
            visited.Add(rootNode.id);
            idMapping[rootNode.id] = newId++;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                if (current.childrenIds != null)
                {
                    foreach (var childId in current.childrenIds)
                    {
                        if (visited.Contains(childId)) continue;
                        
                        var childNode = _currentAsset.GetNode(childId);
                        if (childNode != null)
                        {
                            visited.Add(childId);
                            idMapping[childId] = newId++;
                            queue.Enqueue(childNode);
                        }
                    }
                }
            }

            // 应用新ID
            foreach (var node in _currentAsset.nodes)
            {
                if (idMapping.ContainsKey(node.id))
                {
                    int oldId = node.id;
                    node.id = idMapping[oldId];
                    
                    // 更新父节点ID
                    if (node.parentId >= 0 && idMapping.ContainsKey(node.parentId))
                    {
                        node.parentId = idMapping[node.parentId];
                    }
                    
                    // 更新子节点ID列表
                    if (node.childrenIds != null)
                    {
                        for (int i = 0; i < node.childrenIds.Count; i++)
                        {
                            if (idMapping.ContainsKey(node.childrenIds[i]))
                            {
                                node.childrenIds[i] = idMapping[node.childrenIds[i]];
                            }
                        }
                    }
                }
            }

            // 更新根节点ID
            if (idMapping.ContainsKey(_currentAsset.rootId))
            {
                _currentAsset.rootId = idMapping[_currentAsset.rootId];
            }

            // 更新下一个节点ID
            _nextNodeId = newId;

            MarkDirty();
            Repaint();
            
            Debug.Log($"已重新分配节点ID（共{idMapping.Count}个节点）");
        }

        private void AddNodeToCanvas(BehaviorNodeTypeInfo nodeInfo, Vector2 position)
        {
            if (_currentAsset == null)
            {
                EditorUtility.DisplayDialog("错误", "请先创建或选择一个行为树资源！", "确定");
                return;
            }

            if (_currentAsset.nodes == null)
            {
                _currentAsset.nodes = new List<BehaviorNodeData>();
            }

            var node = new BehaviorNodeData
            {
                id = _nextNodeId++,
                name = $"{nodeInfo.Name}_{_nextNodeId}",
                processTypeName = nodeInfo.Type.Name,
                editorPosition = (position - _offset) / _zoom,
                childrenIds = new List<int>(),
                comment = "", // 确保初始化为空字符串而不是null
                parametersList = new List<SerializableParameter>()
            };

            _currentAsset.AddNode(node);
            
            if (_currentAsset.nodes.Count == 1)
            {
                _currentAsset.rootId = node.id;
            }

            MarkDirty();
            Repaint();
        }

        private void DeleteNode(BehaviorNodeData node)
        {
            if (_currentAsset == null || node == null)
                return;

            if (_currentAsset.nodes == null)
                return;

            foreach (var n in _currentAsset.nodes)
            {
                if (n.childrenIds != null)
                {
                    n.childrenIds.RemoveAll(id => id == node.id);
                }
            }

            _currentAsset.RemoveNode(node.id);
            MarkDirty();
            Repaint();
        }

        private void ConnectNodes(BehaviorNodeData from, BehaviorNodeData to)
        {
            if (from == null || to == null || from == to)
                return;

            if (from.childrenIds == null)
            {
                from.childrenIds = new List<int>();
            }

            if (!from.childrenIds.Contains(to.id))
            {
                from.childrenIds.Add(to.id);
                to.parentId = from.id;
                MarkDirty();
            }

            Repaint();
        }
        
        private void DisconnectNodes(BehaviorNodeData from, BehaviorNodeData to)
        {
            if (from == null || to == null)
                return;
            
            if (from.childrenIds != null && from.childrenIds.Contains(to.id))
            {
                from.childrenIds.Remove(to.id);
                to.parentId = -1;
                MarkDirty();
                Repaint();
            }
        }
        #endregion

        #region Asset Operations
        private void OnAssetChanged()
        {
            _selectedNode = null;
            _connectingNode = null;
            _nodeRuntimeStatus.Clear();
            _isDirty = false;
            
            if (_currentAsset != null && _currentAsset.nodes != null)
            {
                _nextNodeId = _currentAsset.nodes.Count > 0
                    ? _currentAsset.nodes.Max(n => n.id) + 1
                    : 1;
            }
            else
            {
                _nextNodeId = 1;
            }
            
            UpdateTitle();
        }

        private void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建行为树资源",
                "NewBehaviorTree",
                "asset",
                "创建一个新的行为树资源");

            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<BehaviorTreeAsset>();
                asset.treeName = System.IO.Path.GetFileNameWithoutExtension(path);
                asset.rootId = 0;
                asset.nodes = new List<BehaviorNodeData>();
                
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                _currentAsset = asset;
                _nextNodeId = 1;
                
                Debug.Log($"已创建新资源: {path}");
            }
        }

        private void SaveAsset()
        {
            if (_currentAsset != null)
            {
                EditorUtility.SetDirty(_currentAsset);
                AssetDatabase.SaveAssets();
                _isDirty = false;
                UpdateTitle();
                Debug.Log($"已保存: {_currentAsset.name}");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "没有选中的资源！", "确定");
            }
        }
        
        private void MarkDirty()
        {
            if (_currentAsset != null)
            {
                EditorUtility.SetDirty(_currentAsset);
                _isDirty = true;
                UpdateTitle();
            }
        }
        #endregion

        #region Runtime Debug
        public void SetRuntimeTree(BehaviorTreeRuntime tree)
        {
            _runtimeTree = tree;
        }

        public void UpdateNodeStatus(int nodeId, BehaviorRet status)
        {
            _nodeRuntimeStatus[nodeId] = status;
            Repaint();
        }

        public void ClearRuntimeStatus()
        {
            _nodeRuntimeStatus.Clear();
            Repaint();
        }
        #endregion
    }
}
#endif
