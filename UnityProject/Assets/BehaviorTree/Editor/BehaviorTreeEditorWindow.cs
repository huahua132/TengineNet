#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTreeRuntime = BehaviorTree.Tree;

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
        private const float NODE_MIN_HEIGHT = 80f;  // 最小高度
        private const float NODE_HEADER_HEIGHT = 30f; // 标题栏高度
        private const float NODE_PARAM_LINE_HEIGHT = 20f; // 每个参数行高度
        private const float NODE_PADDING = 5f; // 内边距
        private const float GRID_SIZE = 20f;
        private const float TOOLBAR_HEIGHT = 20f;
        
        // 节点高度缓存
        private Dictionary<int, float> _nodeHeights = new Dictionary<int, float>();
        
        // 拖拽
        private BehaviorNodeTypeInfo _draggingNodeType;
        private bool _isDragging = false;
        
        // 鼠标悬停
        private BehaviorNodeData _hoveredNode;
        
        // 折叠状态
        private Dictionary<BehaviorProcessType, bool> _categoryFoldouts = new Dictionary<BehaviorProcessType, bool>();
        private Dictionary<string, bool> _assemblyFoldouts = new Dictionary<string, bool>(); // 程序集折叠状态
        #endregion

        [MenuItem("Tools/BehaviorTree/Editor Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<BehaviorTreeEditorWindow>();
            window.minSize = new Vector2(1200, 600);
            window.UpdateTitle();
        }
        
        /// <summary>
        /// 加载指定的行为树资产（用于双击打开）
        /// </summary>
        public void LoadAsset(BehaviorTreeAsset asset)
        {
            _currentAsset = asset;
            OnAssetChanged();
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
            
            // 初始化程序集折叠状态
            var assemblies = BehaviorNodeRegistry.GetAllNodeAssemblies(excludeRuntime: false);
            foreach (var assembly in assemblies)
            {
                _assemblyFoldouts[assembly] = true;
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
            if (_rightPanelFoldout)
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
            
            // 自动布局按钮
            if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                AutoLayoutNodes();
            }

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
            
            // 按特定顺序显示程序集：Runtime -> 共享程序集 -> 归属程序集
            var orderedAssemblies = new List<string>();
            
            // 1. 优先显示Runtime
            orderedAssemblies.Add("BehaviorTree.Runtime");
            
            if (_currentAsset != null)
            {
                // 2. 按添加顺序显示共享程序集
                if (_currentAsset.sharedAssemblies != null)
                {
                    foreach (var assembly in _currentAsset.sharedAssemblies)
                    {
                        if (!string.IsNullOrEmpty(assembly) && !orderedAssemblies.Contains(assembly))
                        {
                            orderedAssemblies.Add(assembly);
                        }
                    }
                }
                
                // 3. 最后显示归属程序集
                if (!string.IsNullOrEmpty(_currentAsset.ownerAssembly) && !orderedAssemblies.Contains(_currentAsset.ownerAssembly))
                {
                    orderedAssemblies.Add(_currentAsset.ownerAssembly);
                }
            }
            
            // 按顺序绘制程序集分组
            foreach (var assembly in orderedAssemblies)
            {
                DrawAssemblyCategory(assembly);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }

        private void DrawAssemblyCategory(string assemblyName)
        {
            // 获取该程序集下的所有节点
            var assemblyNodes = BehaviorNodeRegistry.GetNodesByAssemblies(new List<string> { assemblyName });
            if (assemblyNodes.Count == 0) return;
            
            // 确保程序集在折叠字典中
            if (!_assemblyFoldouts.ContainsKey(assemblyName))
            {
                _assemblyFoldouts[assemblyName] = true;
            }
            
            // 程序集标题
            EditorGUILayout.BeginHorizontal();
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.9f); // 淡蓝色
            
            _assemblyFoldouts[assemblyName] = EditorGUILayout.Foldout(
                _assemblyFoldouts[assemblyName],
                $"📦 {assemblyName} ({assemblyNodes.Count})",
                true,
                EditorStyles.foldoutHeader);
            
            GUI.backgroundColor = oldBgColor;
            EditorGUILayout.EndHorizontal();
            
            if (!_assemblyFoldouts[assemblyName]) return;
            
            EditorGUI.indentLevel++;
            
            // 在该程序集下按节点类型分组
            foreach (BehaviorProcessType type in System.Enum.GetValues(typeof(BehaviorProcessType)))
            {
                DrawNodeCategoryInAssembly(assemblyName, type);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        
        private void DrawNodeCategoryInAssembly(string assemblyName, BehaviorProcessType type)
        {
            // 获取该程序集下该类型的节点
            var allNodesInAssembly = BehaviorNodeRegistry.GetNodesByAssemblies(new List<string> { assemblyName });
            var nodes = allNodesInAssembly.FindAll(n => n.ProcessType == type);
            
            if (nodes.Count == 0) return;
            
            // 类型标题
            EditorGUILayout.BeginHorizontal();
            
            Color typeColor = BehaviorNodeRegistry.GetTypeColor(type);
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = typeColor * 0.9f;
            
            // 使用组合键作为折叠状态的key
            string foldoutKey = $"{assemblyName}_{type}";
            if (!_categoryFoldouts.ContainsKey(type))
            {
                _categoryFoldouts[type] = true;
            }
            
            bool foldoutState = _categoryFoldouts.ContainsKey(type) ? _categoryFoldouts[type] : true;
            foldoutState = EditorGUILayout.Foldout(
                foldoutState,
                $"  {type} ({nodes.Count})",
                true,
                EditorStyles.foldout);
            _categoryFoldouts[type] = foldoutState;
            
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
            
            if (!foldoutState) return;
            
            EditorGUI.indentLevel++;
            
            // 显示该类型下的所有节点
            foreach (var nodeInfo in nodes)
            {
                DrawNodeButton(nodeInfo);
            }
            
            EditorGUI.indentLevel--;
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

        /// <summary>
        /// 计算节点高度
        /// </summary>
        private float CalculateNodeHeight(BehaviorNodeData node)
        {
            if (_nodeHeights.TryGetValue(node.id, out float cachedHeight))
            {
                return cachedHeight;
            }
            
            float height = NODE_HEADER_HEIGHT; // 标题栏
            
            // 计算备注高度
            int commentLines = 0;
            if (!string.IsNullOrEmpty(node.comment))
            {
                // 估算备注行数（假设每行约25个字符）
                commentLines = Mathf.CeilToInt(node.comment.Length / 25f);
                commentLines = Mathf.Max(commentLines, 1);
                height += commentLines * NODE_PARAM_LINE_HEIGHT;
            }
            
            // 计算参数数量
            int paramCount = 0;
            if (node.parametersList != null)
            {
                paramCount = node.parametersList.Count(p => !string.IsNullOrEmpty(p.value));
            }
            
            if (paramCount > 0 || commentLines > 0)
            {
                height += paramCount * NODE_PARAM_LINE_HEIGHT + NODE_PADDING * 2;
            }
            else
            {
                // 即使没有参数和备注，也保留一定空间
                height += NODE_PADDING * 2;
            }
            
            // 确保不小于最小高度
            height = Mathf.Max(height, NODE_MIN_HEIGHT);
            
            _nodeHeights[node.id] = height;
            return height;
        }
        
        private void DrawNode(BehaviorNodeData node, int index)
        {
            Vector2 pos = node.editorPosition * _zoom + _offset;
            float nodeHeight = CalculateNodeHeight(node);
            
            // 鼠标悬停检测（在窗口坐标系中）
            bool isHovered = false;
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                Vector2 mousePos = Event.current.mousePosition;
                Rect nodeRect = new Rect(pos.x, pos.y, NODE_WIDTH * _zoom, nodeHeight * _zoom);
                isHovered = nodeRect.Contains(mousePos);
            }
            
            // 如果是悬停节点，放大20%
            float scaleMultiplier = (isHovered && !_isDragging) ? 1.2f : 1.0f;
            float scaledWidth = NODE_WIDTH * _zoom * scaleMultiplier;
            float scaledHeight = nodeHeight * _zoom * scaleMultiplier;
            
            // 居中放大（从中心点缩放）
            if (scaleMultiplier > 1.0f)
            {
                float widthDiff = scaledWidth - NODE_WIDTH * _zoom;
                float heightDiff = scaledHeight - nodeHeight * _zoom;
                pos.x -= widthDiff / 2;
                pos.y -= heightDiff / 2;
            }
            
            // 获取节点信息
            var nodeInfo = BehaviorNodeRegistry.GetNodeInfo(node.processTypeName);
            Color nodeColor = nodeInfo != null ? nodeInfo.Color : Color.gray;

            // 绘制节点ID（在节点正上方）
            GUIStyle idStyle = new GUIStyle(EditorStyles.miniLabel);
            idStyle.fontSize = Mathf.RoundToInt(10 * _zoom);
            idStyle.alignment = TextAnchor.MiddleCenter;
            idStyle.normal.textColor = Color.white;
            
            Rect idRect = new Rect(pos.x, pos.y - 15 * _zoom, NODE_WIDTH * _zoom, 15 * _zoom);
            GUI.Label(idRect, $"ID: {node.id}", idStyle);
            
            Rect rect = new Rect(pos.x, pos.y, scaledWidth, scaledHeight);

            // 设置颜色
            Color originalColor = GUI.backgroundColor;
            
            // 绘制边框（使用节点类型颜色）
            float borderWidth = 2f;
            EditorGUI.DrawRect(new Rect(rect.x - borderWidth, rect.y - borderWidth, rect.width + borderWidth * 2, rect.height + borderWidth * 2), nodeColor);
            
            // 根节点用绿色边框
            if (_currentAsset != null && node.id == _currentAsset.rootId)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), Color.green);
            }
            
            // 选中节点高亮（黄色边框）
            if (node == _selectedNode)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), Color.yellow);
            }

            // 保存原始zoom值用于窗口内的绘制
            float originalZoom = _zoom;
            float effectiveZoom = _zoom * scaleMultiplier;
            
            rect = GUI.Window(index, rect, (id) =>
            {
                // 处理右键菜单
                Event e = Event.current;
                if (e != null && e.type == EventType.MouseDown && e.button == 1)
                {
                    ShowNodeContextMenu(node);
                    e.Use();
                }
                
                // 处理节点点击
                if (e != null && e.type == EventType.MouseUp && e.button == 0)
                {
                    // 如果正在连接状态，点击完成连接
                    if (_connectingNode != null && _connectingNode != node)
                    {
                        ConnectNodes(_connectingNode, node);
                        _connectingNode = null;
                        e.Use();
                        Repaint();
                    }
                    else
                    {
                        // 否则选中节点
                        // 如果切换到不同的节点，清除GUI焦点
                        if (_selectedNode != node)
                        {
                            GUI.FocusControl(null);
                            GUIUtility.keyboardControl = 0;
                        }
                        _selectedNode = node;
                        Repaint();
                    }
                }
                
                float windowWidth = scaledWidth;
                float windowHeight = scaledHeight;
                
                // === 标题部分（彩色背景）===
                float headerHeight = NODE_HEADER_HEIGHT * effectiveZoom;
                Rect headerRect = new Rect(0, 0, windowWidth, headerHeight);
                
                // 绘制标题背景（使用节点类型颜色，完全不透明）
                EditorGUI.DrawRect(headerRect, nodeColor);
                
                // 绘制标题内容（左上角图标 + 节点类型）
                float iconSize = 16 * effectiveZoom;
                float iconPadding = 5 * effectiveZoom;
                
                if (nodeInfo != null)
                {
                    GUIContent iconContent = EditorGUIUtility.IconContent(nodeInfo.Icon);
                    if (iconContent != null && iconContent.image != null)
                    {
                        Rect iconRect = new Rect(iconPadding, (headerHeight - iconSize) / 2, iconSize, iconSize);
                        GUI.Label(iconRect, iconContent);
                    }
                }
                
                // 节点显示名称（使用节点的Name属性，黑色加粗）
                GUIStyle typeNameStyle = new GUIStyle(EditorStyles.boldLabel);
                typeNameStyle.fontSize = Mathf.RoundToInt(11 * effectiveZoom);
                typeNameStyle.alignment = TextAnchor.MiddleLeft;
                typeNameStyle.normal.textColor = Color.black;
                typeNameStyle.hover.textColor = Color.black;  // 鼠标悬停不改变颜色
                typeNameStyle.fontStyle = FontStyle.Bold;
                
                string displayName = nodeInfo != null ? nodeInfo.Name : node.processTypeName;
                Rect typeNameRect = new Rect(iconSize + iconPadding * 2, 0, windowWidth - iconSize - iconPadding * 3, headerHeight);
                GUI.Label(typeNameRect, displayName, typeNameStyle);
                
                // === 内容部分（纯白色背景）===
                Rect contentRect = new Rect(0, headerHeight, windowWidth, windowHeight - headerHeight);
                
                // 绘制内容背景（纯白色） - 使用DrawRect确保是纯白色
                EditorGUI.DrawRect(contentRect, Color.white);
                
                // 绘制内容
                GUIStyle contentStyle = new GUIStyle(EditorStyles.label);
                contentStyle.fontSize = Mathf.RoundToInt(9 * effectiveZoom);
                contentStyle.alignment = TextAnchor.UpperLeft;
                contentStyle.normal.textColor = Color.black;
                contentStyle.wordWrap = false;  // 不换行
                contentStyle.clipping = TextClipping.Clip;  // 超出部分裁剪
                float contentY = headerHeight + NODE_PADDING * effectiveZoom;
                float contentPadding = NODE_PADDING * effectiveZoom;
                
                contentStyle.hover.textColor = Color.black; // 鼠标悬停不改变颜色
                
                // 优先显示备注（如果有）
                if (!string.IsNullOrEmpty(node.comment))
                {
                    GUIStyle commentLabelStyle = new GUIStyle(EditorStyles.label);
                    commentLabelStyle.fontSize = Mathf.RoundToInt(8 * effectiveZoom);
                    commentLabelStyle.alignment = TextAnchor.UpperLeft;
                    commentLabelStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f); // 灰色标签
                    commentLabelStyle.hover.textColor = new Color(0.5f, 0.5f, 0.5f);
                    commentLabelStyle.fontStyle = FontStyle.Bold;
                    
                    GUIStyle commentStyle = new GUIStyle(EditorStyles.label);
                    commentStyle.fontSize = Mathf.RoundToInt(8 * effectiveZoom);
                    commentStyle.alignment = TextAnchor.UpperLeft;
                    commentStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f); // 深灰色
                    commentStyle.hover.textColor = new Color(0.3f, 0.3f, 0.3f);
                    commentStyle.wordWrap = true;
                    commentStyle.fontStyle = FontStyle.Italic;
                    
                    float labelWidth = 50 * effectiveZoom;
                    
                    // 绘制"备注:"标签
                    Rect commentLabelRect = new Rect(contentPadding, contentY, labelWidth, NODE_PARAM_LINE_HEIGHT * effectiveZoom);
                    GUI.Label(commentLabelRect, "备注:", commentLabelStyle);
                    
                    // 计算备注内容区域
                    float commentContentWidth = windowWidth - contentPadding * 2 - labelWidth;
                    float commentHeight = commentStyle.CalcHeight(new GUIContent(node.comment), commentContentWidth);
                    Rect commentRect = new Rect(contentPadding + labelWidth, contentY, commentContentWidth, commentHeight);
                    GUI.Label(commentRect, node.comment, commentStyle);
                    contentY += Mathf.Max(commentHeight, NODE_PARAM_LINE_HEIGHT * effectiveZoom) + NODE_PADDING * effectiveZoom * 0.5f;
                }
                
                // 显示参数
                if (node.parametersList != null && node.parametersList.Count > 0)
                {
                    foreach (var param in node.parametersList)
                    {
                        if (!string.IsNullOrEmpty(param.value))
                        {
                            float labelWidth = 70 * effectiveZoom;
                            
                            // 绘制参数名（左侧）- 确保不换行
                            GUIStyle paramLabelStyle = new GUIStyle(contentStyle);
                            paramLabelStyle.fontStyle = FontStyle.Bold;
                            paramLabelStyle.wordWrap = false;
                            paramLabelStyle.clipping = TextClipping.Clip;
                            
                            Rect paramLabelRect = new Rect(contentPadding, contentY, labelWidth, NODE_PARAM_LINE_HEIGHT * effectiveZoom);
                            GUI.Label(paramLabelRect, $"{param.key}:", paramLabelStyle);
                            
                            // 绘制参数值（右侧）- 确保不换行
                            GUIStyle paramValueStyle = new GUIStyle(contentStyle);
                            paramValueStyle.wordWrap = false;
                            paramValueStyle.clipping = TextClipping.Clip;
                            
                            Rect paramValueRect = new Rect(contentPadding + labelWidth, contentY, windowWidth - contentPadding * 2 - labelWidth, NODE_PARAM_LINE_HEIGHT * effectiveZoom);
                            string displayValue = param.value.Length > 12 ? param.value.Substring(0, 9) + "..." : param.value;
                            GUI.Label(paramValueRect, displayValue, paramValueStyle);
                            contentY += NODE_PARAM_LINE_HEIGHT * effectiveZoom;
                        }
                    }
                }

                GUI.DragWindow();
            }, GUIContent.none);

            GUI.backgroundColor = originalColor;

            // 只在非悬停状态更新位置（避免悬停时位置抖动）
            if (scaleMultiplier == 1.0f)
            {
                node.editorPosition = (rect.position - _offset) / _zoom;
            }
            
            // 如果有悬停，触发重绘
            if (isHovered)
            {
                Repaint();
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
            float nodeHeight = CalculateNodeHeight(node);
            return new Vector2(
                pos.x + NODE_WIDTH * _zoom * 0.5f,
                pos.y + nodeHeight * _zoom);
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
            
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            _rightPanelScroll = EditorGUILayout.BeginScrollView(_rightPanelScroll);
            
            // 显示行为树资产配置
            if (_currentAsset != null && _selectedNode == null)
            {
                DrawAssetInspector();
            }
            
            // 显示选中节点的属性
            if (_selectedNode != null)
            {
                DrawNodeInspector();
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        private void DrawAssetInspector()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("行为树配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 行为树名称（只读，显示文件名）
            EditorGUILayout.LabelField("树名称", _currentAsset.name);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("程序集配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("配置后，该行为树只能使用指定程序集中的节点", MessageType.Info);
            
            // 归属程序集
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("归属程序集", GUILayout.Width(100));
            
            // 获取所有可用的程序集（排除Runtime程序集）
            var availableAssemblies = BehaviorNodeRegistry.GetAllNodeAssemblies(excludeRuntime: true);
            
            // 查找当前选中的索引
            int currentIndex = string.IsNullOrEmpty(_currentAsset.ownerAssembly)
                ? 0
                : availableAssemblies.IndexOf(_currentAsset.ownerAssembly) + 1;
            if (currentIndex < 0) currentIndex = 0;
            
            // 创建选项列表 (添加"无"选项)
            var options = new List<string> { "(无)" };
            options.AddRange(availableAssemblies);
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(currentIndex, options.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                _currentAsset.ownerAssembly = newIndex == 0 ? "" : availableAssemblies[newIndex - 1];
                MarkDirty();
                Repaint(); // 重新绘制以更新节点列表
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("共享程序集列表", EditorStyles.miniLabel);
            
            // 共享程序集列表
            if (_currentAsset.sharedAssemblies == null)
            {
                _currentAsset.sharedAssemblies = new List<string>();
            }
            
            for (int i = 0; i < _currentAsset.sharedAssemblies.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 获取所有可用的程序集（排除Runtime程序集，因为它自动包含）
                var sharedAvailableAssemblies = BehaviorNodeRegistry.GetAllNodeAssemblies(excludeRuntime: true);
                
                // 查找当前选中的索引
                int sharedCurrentIndex = string.IsNullOrEmpty(_currentAsset.sharedAssemblies[i])
                    ? 0
                    : sharedAvailableAssemblies.IndexOf(_currentAsset.sharedAssemblies[i]) + 1;
                if (sharedCurrentIndex < 0) sharedCurrentIndex = 0;
                
                // 创建选项列表 (添加"选择程序集"选项)
                var sharedOptions = new List<string> { "(选择程序集)" };
                sharedOptions.AddRange(sharedAvailableAssemblies);
                
                EditorGUI.BeginChangeCheck();
                int sharedNewIndex = EditorGUILayout.Popup(sharedCurrentIndex, sharedOptions.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    _currentAsset.sharedAssemblies[i] = sharedNewIndex == 0 ? "" : sharedAvailableAssemblies[sharedNewIndex - 1];
                    MarkDirty();
                    Repaint();
                }
                
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    _currentAsset.sharedAssemblies.RemoveAt(i);
                    MarkDirty();
                    Repaint();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("+ 添加共享程序集", GUILayout.Height(25)))
            {
                _currentAsset.sharedAssemblies.Add("");
                MarkDirty();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"节点总数: {_currentAsset.nodes?.Count ?? 0}");
            EditorGUILayout.LabelField($"根节点ID: {_currentAsset.rootId}");
            
            EditorGUILayout.EndVertical();
        }

        private void DrawNodeInspector()
        {
            EditorGUILayout.BeginVertical();
            
            // 基本信息
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
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
            
            // 使用唯一的控件名称来避免GUI状态残留
            string commentControlName = $"Comment_{_selectedNode.id}";
            GUI.SetNextControlName(commentControlName);
            
            EditorGUI.BeginChangeCheck();
            _selectedNode.comment = EditorGUILayout.TextArea(_selectedNode.comment, GUILayout.Height(60));
            if (EditorGUI.EndChangeCheck())
            {
                // 清除节点高度缓存，以便重新计算
                if (_nodeHeights.ContainsKey(_selectedNode.id))
                {
                    _nodeHeights.Remove(_selectedNode.id);
                }
                MarkDirty();
                Repaint();
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
            if (nodeInfo == null)
            {
                EditorGUILayout.HelpBox($"无法获取节点类型信息: {_selectedNode.processTypeName}", MessageType.Warning);
                return;
            }
            
            // 获取所有公共实例字段
            var allFields = nodeInfo.Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            // 过滤掉基类的字段（_Node, _Context 等）
            var fields = allFields.Where(f =>
                f.DeclaringType != typeof(BehaviorProcessNodeBase) &&
                !f.Name.StartsWith("_")
            ).ToList();
            
            if (fields.Count == 0)
            {
                EditorGUILayout.HelpBox("此节点没有可配置的参数", MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space(5);
            
            // 确保parametersList已初始化
            if (_selectedNode.parametersList == null)
            {
                _selectedNode.parametersList = new List<SerializableParameter>();
            }
            
            foreach (var field in fields)
            {
                string fieldName = field.Name;
                bool hasParameter = _selectedNode.HasParameter(fieldName);
                string currentValue = hasParameter
                    ? _selectedNode.GetParameter(fieldName)
                    : GetDefaultValue(field);
                
                // 如果参数不存在且有默认值，自动设置默认值（只在第一次显示时）
                if (!hasParameter && !string.IsNullOrEmpty(currentValue))
                {
                    _selectedNode.SetParameter(fieldName, currentValue);
                    // 清除节点高度缓存
                    if (_nodeHeights.ContainsKey(_selectedNode.id))
                    {
                        _nodeHeights.Remove(_selectedNode.id);
                    }
                }
                
                // 获取字段的描述信息用于Tooltip
                string tooltip = GetFieldTooltip(field);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(fieldName, tooltip), GUILayout.Width(100));
                
                // 使用唯一的控件名称来避免GUI状态残留
                string controlName = $"Param_{_selectedNode.id}_{fieldName}";
                GUI.SetNextControlName(controlName);
                
                EditorGUI.BeginChangeCheck();
                string newValue;
                
                // 检测是否为枚举类型
                if (field.FieldType.IsEnum)
                {
                    // 枚举类型使用下拉菜单
                    var enumNames = System.Enum.GetNames(field.FieldType);
                    int currentIndex = System.Array.IndexOf(enumNames, currentValue);
                    if (currentIndex < 0) currentIndex = 0;
                    
                    int newIndex = EditorGUILayout.Popup(currentIndex, enumNames);
                    newValue = enumNames[newIndex];
                }
                else
                {
                    // 其他类型使用文本输入框
                    newValue = EditorGUILayout.TextField(new GUIContent("", tooltip), currentValue);
                }
                
                if (EditorGUI.EndChangeCheck() && newValue != currentValue)
                {
                    _selectedNode.SetParameter(fieldName, newValue);
                    // 清除节点高度缓存，以便重新计算
                    if (_nodeHeights.ContainsKey(_selectedNode.id))
                    {
                        _nodeHeights.Remove(_selectedNode.id);
                    }
                    MarkDirty();
                    Repaint();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private string GetDefaultValue(FieldInfo field)
        {
            // 优先使用Attribute标记的默认值
            var defaultAttr = field.GetCustomAttribute<System.ComponentModel.DefaultValueAttribute>();
            if (defaultAttr != null)
            {
                return defaultAttr.Value?.ToString() ?? "";
            }
            
            // 通过创建类型实例来获取字段的初始值
            try
            {
                var instance = System.Activator.CreateInstance(field.DeclaringType);
                var fieldValue = field.GetValue(instance);
                if (fieldValue != null)
                {
                    return fieldValue.ToString();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BehaviorTree] 无法获取字段 {field.Name} 的初始值: {ex.Message}");
            }
            
            // 如果无法获取实例值，使用类型默认值
            if (field.FieldType == typeof(string))
                return "";
            if (field.FieldType == typeof(int))
                return "0";
            if (field.FieldType == typeof(float))
                return "0";
            if (field.FieldType == typeof(bool))
                return "False";
            if (field.FieldType == typeof(long))
                return "0";
            if (field.FieldType == typeof(double))
                return "0";
            
            // 枚举类型返回第一个枚举值
            if (field.FieldType.IsEnum)
            {
                var enumValues = System.Enum.GetNames(field.FieldType);
                return enumValues.Length > 0 ? enumValues[0] : "";
            }
            
            return "";
        }
        
        /// <summary>
        /// 获取字段的Tooltip描述
        /// </summary>
        private string GetFieldTooltip(FieldInfo field)
        {
            // 优先使用TooltipAttribute
            var tooltipAttr = field.GetCustomAttribute<TooltipAttribute>();
            if (tooltipAttr != null && !string.IsNullOrEmpty(tooltipAttr.tooltip))
            {
                return tooltipAttr.tooltip;
            }
            
            // 其次使用System.ComponentModel.DescriptionAttribute
            var descAttr = field.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descAttr != null && !string.IsNullOrEmpty(descAttr.Description))
            {
                return descAttr.Description;
            }
            
            // 返回字段类型信息作为默认tooltip
            return $"类型: {field.FieldType.Name}";
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

            // 计算画布区域
            float canvasX = _leftPanelFoldout ? _leftPanelWidth : 0;
            float canvasWidth = position.width - canvasX - (_rightPanelFoldout ? _rightPanelWidth : 0);
            Rect canvasRect = new Rect(canvasX, TOOLBAR_HEIGHT, canvasWidth, position.height - TOOLBAR_HEIGHT);

            // 节点选择 - 只在画布区域内生效
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // 检查鼠标是否在画布区域内
                if (canvasRect.Contains(e.mousePosition))
                {
                    BehaviorNodeData newSelectedNode = GetNodeAtPosition(e.mousePosition);
                    // 如果切换到不同的节点，清除GUI焦点
                    if (_selectedNode != newSelectedNode)
                    {
                        GUI.FocusControl(null);
                        GUIUtility.keyboardControl = 0;
                    }
                    _selectedNode = newSelectedNode;
                    Repaint();
                }
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
                float nodeHeight = CalculateNodeHeight(node);
                Rect rect = new Rect(pos.x, pos.y, NODE_WIDTH * _zoom, nodeHeight * _zoom);
                
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
        /// 按照节点层级（从上到下），同层级内从左到右重新分配节点ID
        /// </summary>
        private void ReassignNodeIds()
        {
            if (_currentAsset == null || _currentAsset.nodes == null || _currentAsset.nodes.Count == 0)
                return;

            // 创建旧ID到新ID的映射
            Dictionary<int, int> idMapping = new Dictionary<int, int>();
            int newId = 1;

            // 按层级分组（Y坐标相近的视为同一层）
            const float LEVEL_THRESHOLD = 50f; // Y坐标差值小于50视为同一层
            
            // 先按Y坐标排序
            var nodesByY = _currentAsset.nodes.OrderBy(n => n.editorPosition.y).ToList();
            
            // 分层
            List<List<BehaviorNodeData>> levels = new List<List<BehaviorNodeData>>();
            List<BehaviorNodeData> currentLevel = new List<BehaviorNodeData>();
            float currentLevelY = nodesByY[0].editorPosition.y;
            
            foreach (var node in nodesByY)
            {
                if (Mathf.Abs(node.editorPosition.y - currentLevelY) <= LEVEL_THRESHOLD)
                {
                    // 同一层级
                    currentLevel.Add(node);
                }
                else
                {
                    // 新层级
                    if (currentLevel.Count > 0)
                    {
                        levels.Add(currentLevel);
                    }
                    currentLevel = new List<BehaviorNodeData> { node };
                    currentLevelY = node.editorPosition.y;
                }
            }
            
            // 添加最后一层
            if (currentLevel.Count > 0)
            {
                levels.Add(currentLevel);
            }

            // 对每一层内的节点按X坐标（从左到右）排序，然后分配ID
            foreach (var level in levels)
            {
                var sortedLevel = level.OrderBy(n => n.editorPosition.x).ToList();
                foreach (var node in sortedLevel)
                {
                    idMapping[node.id] = newId++;
                }
            }

            // 应用新ID到所有节点
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

            // 按新ID排序节点列表
            _currentAsset.nodes = _currentAsset.nodes.OrderBy(n => n.id).ToList();

            // 更新下一个节点ID
            _nextNodeId = newId;

            MarkDirty();
            Repaint();
            
            Debug.Log($"已按层级位置重新分配节点ID（共{levels.Count}层，{idMapping.Count}个节点）");
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
                processTypeName = nodeInfo.Type.Name,
                editorPosition = (position - _offset) / _zoom,
                childrenIds = new List<int>(),
                comment = "", // 确保初始化为空字符串而不是null
                parametersList = new List<SerializableParameter>()
            };

            // 自动填充默认参数值
            InitializeNodeParameters(node, nodeInfo.Type);

            _currentAsset.AddNode(node);
            
            if (_currentAsset.nodes.Count == 1)
            {
                _currentAsset.rootId = node.id;
            }

            MarkDirty();
            Repaint();
        }
        
        /// <summary>
        /// 初始化节点的默认参数值
        /// </summary>
        private void InitializeNodeParameters(BehaviorNodeData node, System.Type nodeType)
        {
            if (node == null || nodeType == null) return;
            
            // 获取所有公共实例字段
            var allFields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            // 过滤掉基类的字段
            var fields = allFields.Where(f =>
                f.DeclaringType != typeof(BehaviorProcessNodeBase) &&
                !f.Name.StartsWith("_")
            ).ToList();
            
            // 为每个字段设置默认值
            foreach (var field in fields)
            {
                string fieldName = field.Name;
                string defaultValue = GetDefaultValue(field);
                
                // 设置默认值（除了空字符串外都设置，因为空字符串是有效的string默认值）
                // 对于string类型，即使是空字符串也不设置（避免显示空参数）
                if (!string.IsNullOrEmpty(defaultValue) || field.FieldType != typeof(string))
                {
                    if (field.FieldType == typeof(string) && string.IsNullOrEmpty(defaultValue))
                    {
                        // string类型的空默认值不设置
                        continue;
                    }
                    
                    node.SetParameter(fieldName, defaultValue);
                    
                    // 调试日志
                    Debug.Log($"[BehaviorTree] 节点 {nodeType.Name} 设置参数 {fieldName} = {defaultValue}");
                }
            }
            
            // 清除节点高度缓存，确保重新计算
            if (_nodeHeights.ContainsKey(node.id))
            {
                _nodeHeights.Remove(node.id);
            }
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
        
        /// <summary>
        /// 自动整理节点布局 - 横向从左到右的树状结构
        /// </summary>
        private void AutoLayoutNodes()
        {
            if (_currentAsset == null || _currentAsset.nodes == null || _currentAsset.nodes.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有节点需要整理", "确定");
                return;
            }
            
            // 找到根节点
            var rootNode = _currentAsset.GetNode(_currentAsset.rootId);
            if (rootNode == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到根节点", "确定");
                return;
            }
            
            // 清除高度缓存
            _nodeHeights.Clear();
            
            // 布局参数（横向布局）
            const float HORIZONTAL_SPACING = 300f;  // 横向间距（父节点到子节点）
            const float VERTICAL_SPACING = 120f;    // 纵向间距（子节点之间）
            const float START_X = 100f;             // 起始X坐标（根节点最左边）
            const float START_Y = 300f;             // 起始Y坐标（垂直居中）
            
            // 第一步：计算布局信息（递归计算每个子树的高度）
            Dictionary<int, HorizontalLayoutInfo> layoutInfos = new Dictionary<int, HorizontalLayoutInfo>();
            CalculateHorizontalLayout(rootNode, layoutInfos, VERTICAL_SPACING);
            
            // 第二步：应用绝对位置（从根节点开始，横向布局）
            ApplyHorizontalPositions(rootNode, START_X, START_Y, HORIZONTAL_SPACING, layoutInfos);
            
            MarkDirty();
            Repaint();
            
            Debug.Log($"已自动整理为横向布局，共 {layoutInfos.Count} 个节点");
        }
        
        /// <summary>
        /// 横向布局信息
        /// </summary>
        private class HorizontalLayoutInfo
        {
            public float subtreeHeight;          // 子树总高度
            public float relativeY;              // 相对于父节点的Y偏移
            public List<float> childrenOffsets;  // 子节点的相对Y偏移列表
        }
        
        /// <summary>
        /// 递归计算横向布局信息
        /// 子节点按ID从小到大纵向排列（从上到下）
        /// </summary>
        private float CalculateHorizontalLayout(BehaviorNodeData node, Dictionary<int, HorizontalLayoutInfo> layoutInfos, float verticalSpacing)
        {
            var info = new HorizontalLayoutInfo();
            info.childrenOffsets = new List<float>();
            
            // 没有子节点
            if (node.childrenIds == null || node.childrenIds.Count == 0)
            {
                info.subtreeHeight = verticalSpacing;
                info.relativeY = 0;
                layoutInfos[node.id] = info;
                return verticalSpacing;
            }
            
            // 获取所有子节点并按ID排序（从小到大，从上到下）
            var sortedChildNodes = node.childrenIds
                .Select(id => _currentAsset.GetNode(id))
                .Where(n => n != null)
                .OrderBy(n => n.id)
                .ToList();
            
            // 递归计算所有子节点的高度
            List<float> childHeights = new List<float>();
            foreach (var childNode in sortedChildNodes)
            {
                float childHeight = CalculateHorizontalLayout(childNode, layoutInfos, verticalSpacing);
                childHeights.Add(childHeight);
            }
            
            // 计算总高度
            float totalHeight = 0;
            foreach (var height in childHeights)
            {
                totalHeight += height;
            }
            
            // 计算每个子节点的Y偏移量（从上到下排列，ID小的在上面）
            float currentOffset = -totalHeight / 2;
            for (int i = 0; i < childHeights.Count; i++)
            {
                float childCenterOffset = currentOffset + childHeights[i] / 2;
                info.childrenOffsets.Add(childCenterOffset);
                currentOffset += childHeights[i];
            }
            
            info.subtreeHeight = Mathf.Max(totalHeight, verticalSpacing);
            info.relativeY = 0; // 父节点垂直居中
            layoutInfos[node.id] = info;
            
            return info.subtreeHeight;
        }
        
        /// <summary>
        /// 应用横向布局的绝对位置
        /// </summary>
        private void ApplyHorizontalPositions(BehaviorNodeData node, float absoluteX, float absoluteY, float horizontalSpacing, Dictionary<int, HorizontalLayoutInfo> layoutInfos)
        {
            // 设置当前节点的绝对位置
            node.editorPosition = new Vector2(absoluteX, absoluteY);
            
            // 没有子节点，直接返回
            if (node.childrenIds == null || node.childrenIds.Count == 0)
                return;
            
            // 获取布局信息
            if (!layoutInfos.TryGetValue(node.id, out var info))
                return;
            
            // 获取所有子节点并按ID排序
            var sortedChildNodes = node.childrenIds
                .Select(id => _currentAsset.GetNode(id))
                .Where(n => n != null)
                .OrderBy(n => n.id)
                .ToList();
            
            // 递归设置子节点位置（子节点在父节点右边，纵向排列，ID小的在上面）
            for (int i = 0; i < sortedChildNodes.Count && i < info.childrenOffsets.Count; i++)
            {
                var childNode = sortedChildNodes[i];
                float childX = absoluteX + horizontalSpacing;  // 子节点在父节点右边
                float childY = absoluteY + info.childrenOffsets[i];  // Y坐标根据偏移量调整
                ApplyHorizontalPositions(childNode, childX, childY, horizontalSpacing, layoutInfos);
            }
        }
        #endregion

        #region Asset Operations
        private void OnAssetChanged()
        {
            _selectedNode = null;
            _connectingNode = null;
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
    }
}
#endif
