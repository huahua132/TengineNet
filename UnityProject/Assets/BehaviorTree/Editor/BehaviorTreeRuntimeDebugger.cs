#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree.Editor
{
    /// <summary>
    /// 行为树运行时调试窗口
    /// 三栏布局：左侧树列表 | 中间树可视化 | 右侧数据面板
    /// </summary>
    public class BehaviorTreeRuntimeDebugger : EditorWindow
    {
        // 窗口布局
        private float _leftPanelWidth = 250f;
        private float _rightPanelWidth = 300f;
        private bool _isResizingLeft = false;
        private bool _isResizingRight = false;
        private const float MinPanelWidth = 150f;
        private const float ResizeHandleWidth = 5f;

        // 数据
        private List<BehaviorTreeRuntimeManager.TreeInstance> _runningTrees;
        private BehaviorTreeRuntimeManager.TreeInstance _selectedTree;
        private int _selectedNodeId = -1;
        
        // 可视化
        private Vector2 _treeScrollPos;
        private Vector2 _leftScrollPos;
        private Vector2 _rightScrollPos;
        private Vector2 _canvasOffset = Vector2.zero;
        private float _canvasZoom = 1.0f;
        
        // 节点布局
        private Dictionary<int, Rect> _nodeRects = new Dictionary<int, Rect>();
        private Dictionary<int, Vector2> _nodePositions = new Dictionary<int, Vector2>();
        private const float NodeWidth = 200f;
        private const float NodeMinHeight = 80f;
        private const float NodeHeaderHeight = 30f;
        private const float NodeParamLineHeight = 20f;
        private const float NodePadding = 5f;
        private const float HorizontalSpacing = 300f;
        private const float VerticalPadding = 30f;
        private const float GridSize = 20f;
        
        // 自动刷新
        private bool _autoRefresh = true;
        private double _lastRefreshTime = 0;
        private const double RefreshInterval = 0.5;

        [MenuItem("Tools/BehaviorTree/Runtime Debugger")]
        public static void OpenWindow()
        {
            var window = GetWindow<BehaviorTreeRuntimeDebugger>("BT Runtime Debugger");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
            RefreshTreeList();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && EditorApplication.isPlaying)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                if (currentTime - _lastRefreshTime >= RefreshInterval)
                {
                    _lastRefreshTime = currentTime;
                    RefreshTreeList();
                    Repaint();
                }
            }
        }

        private void RefreshTreeList()
        {
            _runningTrees = BehaviorTreeRuntimeManager.GetRunningTrees();
            
            if (_selectedTree != null && !_runningTrees.Contains(_selectedTree))
            {
                _selectedTree = null;
                _selectedNodeId = -1;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            DrawLeftPanel();
            DrawResizeHandle(ref _isResizingLeft, ref _leftPanelWidth, true);
            DrawMiddlePanel();
            DrawResizeHandle(ref _isResizingRight, ref _rightPanelWidth, false);
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();

            if (_isResizingLeft && Event.current.type == EventType.MouseDrag)
            {
                _leftPanelWidth += Event.current.delta.x;
                _leftPanelWidth = Mathf.Clamp(_leftPanelWidth, MinPanelWidth, position.width - MinPanelWidth * 2);
                Repaint();
            }

            if (_isResizingRight && Event.current.type == EventType.MouseDrag)
            {
                _rightPanelWidth -= Event.current.delta.x;
                _rightPanelWidth = Mathf.Clamp(_rightPanelWidth, MinPanelWidth, position.width - MinPanelWidth * 2);
                Repaint();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                _isResizingLeft = false;
                _isResizingRight = false;
            }
        }

        private void DrawResizeHandle(ref bool isResizing, ref float panelWidth, bool isLeft)
        {
            Rect handleRect = EditorGUILayout.BeginVertical(GUILayout.Width(ResizeHandleWidth));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            {
                isResizing = true;
                Event.current.Use();
            }
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("运行中的行为树", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                RefreshTreeList();
            }
            EditorGUILayout.EndHorizontal();

            _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos);

            if (_runningTrees == null || _runningTrees.Count == 0)
            {
                EditorGUILayout.HelpBox("没有运行中的行为树", MessageType.Info);
            }
            else
            {
                foreach (var treeInstance in _runningTrees)
                {
                    bool isSelected = _selectedTree == treeInstance;
                    
                    EditorGUILayout.BeginVertical(isSelected ? "SelectionRect" : "box");
                    
                    if (GUILayout.Button(treeInstance.GetDisplayName(), GUILayout.Height(30)))
                    {
                        _selectedTree = treeInstance;
                        _selectedNodeId = -1;
                        _canvasOffset = Vector2.zero;
                        _canvasZoom = 1.0f;
                        CalculateNodeLayout();
                    }

                    if (isSelected)
                    {
                        EditorGUILayout.LabelField($"ID: {treeInstance.InstanceId}");
                        EditorGUILayout.LabelField($"运行次数: {treeInstance.TickCount}");
                        EditorGUILayout.LabelField($"最后结果: {treeInstance.LastResult}");
                        
                        if (treeInstance.Asset != null)
                        {
                            EditorGUILayout.LabelField($"资产: {treeInstance.Asset.name}");
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawMiddlePanel()
        {
            float middlePanelWidth = position.width - _leftPanelWidth - _rightPanelWidth - ResizeHandleWidth * 2;
            EditorGUILayout.BeginVertical(GUILayout.Width(middlePanelWidth));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("行为树可视化", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("居中", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _canvasOffset = Vector2.zero;
                _canvasZoom = 1.0f;
            }
            
            GUILayout.Label($"缩放: {(_canvasZoom * 100):F0}%", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (_selectedTree != null && _selectedTree.Tree != null)
            {
                DrawTreeCanvas(middlePanelWidth);
            }
            else
            {
                Rect canvasRect = GUILayoutUtility.GetRect(middlePanelWidth, position.height - 20);
                GUI.Box(canvasRect, "");
                GUILayout.BeginArea(canvasRect);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("请从左侧选择一个行为树", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndArea();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTreeCanvas(float width)
        {
            Rect canvasRect = GUILayoutUtility.GetRect(width, position.height - 20);
            
            // 深色背景
            EditorGUI.DrawRect(canvasRect, new Color(0.2f, 0.2f, 0.2f));

            HandleCanvasInput(canvasRect);

            // 使用GUI裁剪确保内容只显示在画布区域内
            GUI.BeginClip(canvasRect);
            
            // 创建相对于裁剪区域的本地Rect
            Rect localRect = new Rect(0, 0, canvasRect.width, canvasRect.height);
            
            // 绘制网格
            Handles.BeginGUI();
            DrawGrid(localRect);
            Handles.EndGUI();
            
            // 绘制树
            var tree = _selectedTree.Tree;
            var rootNode = tree.GetRootNode();
            if (rootNode != null)
            {
                CalculateNodeLayout();
                
                // 绘制连接线
                Handles.BeginGUI();
                DrawConnectionsRecursive(rootNode, localRect);
                Handles.EndGUI();
                
                // 绘制节点
                DrawNodesRecursive(rootNode, localRect);
            }
            
            GUI.EndClip();
        }

        private void DrawGrid(Rect rect)
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            float gridSpacing = GridSize * _canvasZoom;
            int widthDivs = Mathf.CeilToInt(rect.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(rect.height / gridSpacing);

            Vector2 gridOffset = new Vector2(_canvasOffset.x % gridSpacing, _canvasOffset.y % gridSpacing);

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
        }

        private void HandleCanvasInput(Rect canvasRect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDrag && e.button == 2 && canvasRect.Contains(e.mousePosition))
            {
                _canvasOffset += e.delta / _canvasZoom;
                e.Use();
                Repaint();
            }

            if (e.type == EventType.ScrollWheel && canvasRect.Contains(e.mousePosition))
            {
                float zoomDelta = -e.delta.y * 0.05f;
                _canvasZoom = Mathf.Clamp(_canvasZoom + zoomDelta, 0.3f, 2.0f);
                e.Use();
                Repaint();
            }
        }

        private void CalculateNodeLayout()
        {
            if (_selectedTree == null || _selectedTree.Tree == null) return;
            
            _nodePositions.Clear();
            var rootNode = _selectedTree.Tree.GetRootNode();
            if (rootNode == null) return;
            
            // 横向布局：从左到右
            float startX = 100f;
            float startY = 300f;
            
            CalculateNodePositionsRecursive(rootNode, 0, 0);
            ApplyNodePositions(rootNode, startX, startY);
        }

        private void CalculateNodePositionsRecursive(BehaviorNode node, int depth, int siblingIndex)
        {
            // 递归计算所有节点
            var children = node.Childrens;
            if (children != null && children.Count > 0)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    CalculateNodePositionsRecursive((BehaviorNode)children[i], depth + 1, i);
                }
            }
        }

        private void ApplyNodePositions(BehaviorNode node, float x, float y)
        {
            float nodeHeight = CalculateNodeHeight(node);
            _nodePositions[node.ID] = new Vector2(x, y - nodeHeight / 2);
            
            var children = node.Childrens;
            if (children != null && children.Count > 0)
            {
                float totalHeight = 0;
                foreach (BehaviorNode child in children)
                {
                    totalHeight += CalculateSubtreeHeight(child) + VerticalPadding;
                }
                
                float currentY = y - totalHeight / 2;
                foreach (BehaviorNode child in children)
                {
                    float subtreeHeight = CalculateSubtreeHeight(child);
                    float childY = currentY + subtreeHeight / 2;
                    ApplyNodePositions(child, x + HorizontalSpacing, childY);
                    currentY += subtreeHeight + VerticalPadding;
                }
            }
        }

        private float CalculateSubtreeHeight(BehaviorNode node)
        {
            float nodeHeight = CalculateNodeHeight(node);
            var children = node.Childrens;
            
            if (children == null || children.Count == 0)
            {
                return nodeHeight + VerticalPadding;
            }
            
            float totalChildHeight = 0;
            foreach (BehaviorNode child in children)
            {
                totalChildHeight += CalculateSubtreeHeight(child);
            }
            
            return Mathf.Max(nodeHeight + VerticalPadding, totalChildHeight);
        }

        private float CalculateNodeHeight(BehaviorNode node)
        {
            float height = NodeHeaderHeight;
            
            // 获取节点数据
            var nodeData = _selectedTree.Tree.GetAsset()?.GetNode(node.ID);
            if (nodeData != null)
            {
                if (!string.IsNullOrEmpty(nodeData.comment))
                {
                    int commentLines = Mathf.CeilToInt(nodeData.comment.Length / 25f);
                    height += commentLines * NodeParamLineHeight;
                }
                
                if (nodeData.parametersList != null)
                {
                    int paramCount = nodeData.parametersList.Count(p => !string.IsNullOrEmpty(p.value));
                    height += paramCount * NodeParamLineHeight;
                }
            }
            
            height += NodePadding * 2;
            return Mathf.Max(height, NodeMinHeight);
        }

        private void DrawConnectionsRecursive(BehaviorNode node, Rect canvasRect)
        {
            var children = node.Childrens;
            if (children == null || children.Count == 0) return;
            
            foreach (BehaviorNode child in children)
            {
                DrawConnection(node, child, canvasRect);
                DrawConnectionsRecursive(child, canvasRect);
            }
        }

        private void DrawConnection(BehaviorNode from, BehaviorNode to, Rect canvasRect)
        {
            Vector2 startPos = GetNodeBottomCenter(from, canvasRect);
            Vector2 endPos = GetNodeTopCenter(to, canvasRect);
            
            // 绘制贝塞尔曲线 (参考编辑器风格)
            float distance = Vector2.Distance(startPos, endPos);
            float tangentLength = Mathf.Min(distance * 0.5f, 80f);
            
            Vector2 startTangent = startPos + Vector2.down * tangentLength;
            Vector2 endTangent = endPos + Vector2.up * tangentLength;
            
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.white, null, 3f);
        }

        private Vector2 GetNodeTopCenter(BehaviorNode node, Rect canvasRect)
        {
            if (!_nodePositions.TryGetValue(node.ID, out Vector2 pos)) return Vector2.zero;
            Vector2 screenPos = pos * _canvasZoom + _canvasOffset;
            return new Vector2(screenPos.x + NodeWidth * _canvasZoom * 0.5f, screenPos.y);
        }

        private Vector2 GetNodeBottomCenter(BehaviorNode node, Rect canvasRect)
        {
            if (!_nodePositions.TryGetValue(node.ID, out Vector2 pos)) return Vector2.zero;
            Vector2 screenPos = pos * _canvasZoom + _canvasOffset;
            float nodeHeight = CalculateNodeHeight(node);
            return new Vector2(screenPos.x + NodeWidth * _canvasZoom * 0.5f,
                               screenPos.y + nodeHeight * _canvasZoom);
        }

        private void DrawNodesRecursive(BehaviorNode node, Rect canvasRect)
        {
            if (!_nodePositions.TryGetValue(node.ID, out Vector2 pos)) return;
            
            DrawNode(node, pos, canvasRect);
            
            var children = node.Childrens;
            if (children != null)
            {
                foreach (BehaviorNode child in children)
                {
                    DrawNodesRecursive(child, canvasRect);
                }
            }
        }

        private void DrawNode(BehaviorNode node, Vector2 position, Rect canvasRect)
        {
            Vector2 screenPos = position * _canvasZoom + _canvasOffset;
            float nodeHeight = CalculateNodeHeight(node);
            Rect nodeRect = new Rect(screenPos.x, screenPos.y, NodeWidth * _canvasZoom, nodeHeight * _canvasZoom);
            
            // 获取节点信息
            var nodeInfo = BehaviorNodeRegistry.GetNodeInfo(node.ProcessNode?.GetType().Name);
            Color nodeColor = nodeInfo != null ? nodeInfo.Color : Color.gray;
            
            // 绘制边框
            EditorGUI.DrawRect(new Rect(nodeRect.x - 2, nodeRect.y - 2, nodeRect.width + 4, nodeRect.height + 4), nodeColor);
            
            // 选中高亮
            if (node.ID == _selectedNodeId)
            {
                EditorGUI.DrawRect(new Rect(nodeRect.x - 3, nodeRect.y - 3, nodeRect.width + 6, nodeRect.height + 6), Color.yellow);
            }
            
            // 运行状态高亮 (蓝色)
            if (node.IsResume())
            {
                EditorGUI.DrawRect(new Rect(nodeRect.x - 3, nodeRect.y - 3, nodeRect.width + 6, nodeRect.height + 6), new Color(0.3f, 0.6f, 1f, 0.8f));
            }
            
            // 标题栏
            float headerHeight = NodeHeaderHeight * _canvasZoom;
            Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, headerHeight);
            EditorGUI.DrawRect(headerRect, nodeColor);
            
            // 标题文字
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = Mathf.RoundToInt(11 * _canvasZoom);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.black;
            string displayName = nodeInfo != null ? nodeInfo.Name : node.ProcessNode?.GetType().Name;
            GUI.Label(headerRect, displayName, titleStyle);
            
            // 内容区域
            Rect contentRect = new Rect(nodeRect.x, nodeRect.y + headerHeight, nodeRect.width, nodeRect.height - headerHeight);
            EditorGUI.DrawRect(contentRect, Color.white);
            
            // 绘制内容
            GUIStyle contentStyle = new GUIStyle(EditorStyles.label);
            contentStyle.fontSize = Mathf.RoundToInt(9 * _canvasZoom);
            contentStyle.normal.textColor = Color.black;
            contentStyle.alignment = TextAnchor.UpperLeft;
            contentStyle.padding = new RectOffset(5, 5, 5, 5);
            
            float contentY = nodeRect.y + headerHeight + 5 * _canvasZoom;
            
            // 显示节点ID
            GUI.Label(new Rect(nodeRect.x + 5, contentY, nodeRect.width - 10, 15 * _canvasZoom), 
                $"ID: {node.ID}", contentStyle);
            contentY += 15 * _canvasZoom;
            
            // 显示备注和参数
            var nodeData = _selectedTree.Tree.GetAsset()?.GetNode(node.ID);
            if (nodeData != null)
            {
                if (!string.IsNullOrEmpty(nodeData.comment))
                {
                    GUIStyle commentStyle = new GUIStyle(contentStyle);
                    commentStyle.fontStyle = FontStyle.Italic;
                    commentStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
                    GUI.Label(new Rect(nodeRect.x + 5, contentY, nodeRect.width - 10, 30 * _canvasZoom),
                        nodeData.comment, commentStyle);
                    contentY += 20 * _canvasZoom;
                }
                
                if (nodeData.parametersList != null)
                {
                    foreach (var param in nodeData.parametersList)
                    {
                        if (!string.IsNullOrEmpty(param.value))
                        {
                            string displayValue = param.value.Length > 10 ? param.value.Substring(0, 7) + "..." : param.value;
                            GUI.Label(new Rect(nodeRect.x + 5, contentY, nodeRect.width - 10, 15 * _canvasZoom),
                                $"{param.key}: {displayValue}", contentStyle);
                            contentY += 15 * _canvasZoom;
                        }
                    }
                }
            }
            
            // 处理点击（考虑画布偏移）
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 mousePos = e.mousePosition;
                if (nodeRect.Contains(mousePos))
                {
                    _selectedNodeId = node.ID;
                    e.Use();
                    Repaint();
                }
            }
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_rightPanelWidth));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("属性面板", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            _rightScrollPos = EditorGUILayout.BeginScrollView(_rightScrollPos);

            if (_selectedTree == null)
            {
                EditorGUILayout.HelpBox("请先选择一个行为树", MessageType.Info);
            }
            else if (_selectedNodeId == -1)
            {
                DrawContextInfo();
            }
            else
            {
                DrawNodeProperties();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawContextInfo()
        {
            EditorGUILayout.LabelField("上下文信息", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var context = _selectedTree.Tree?.GetContext();
            if (context == null)
            {
                EditorGUILayout.HelpBox("上下文不可用", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"执行栈深度: {context.GetStackCount()}");
            EditorGUILayout.LabelField($"最后结果: {context.GetLastRet()}");
            EditorGUILayout.LabelField($"是否中断: {context.IsAbort()}");

            EditorGUILayout.Space();

            var transform = context.GetBindTransform();
            if (transform != null)
            {
                EditorGUILayout.LabelField("绑定对象", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Transform", transform, typeof(Transform), true);
                EditorGUILayout.LabelField($"GameObject: {transform.gameObject.name}");
                EditorGUILayout.LabelField($"位置: {transform.position}");
            }
        }

        private void DrawNodeProperties()
        {
            EditorGUILayout.LabelField("节点属性", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var nodeDict = _selectedTree.Tree?.GetNodeDict();
            if (nodeDict == null || !nodeDict.TryGetValue(_selectedNodeId, out var node))
            {
                EditorGUILayout.HelpBox("节点不存在", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"节点ID: {node.ID}");
            EditorGUILayout.LabelField($"类型: {node.ProcessNode?.GetType().Name}");
            EditorGUILayout.LabelField($"是否挂起: {node.IsResume()}");

            EditorGUILayout.Space();

            if (node.ProcessNode != null)
            {
                EditorGUILayout.LabelField("节点参数", EditorStyles.boldLabel);
                
                var processNode = node.ProcessNode;
                var fields = processNode.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (fields.Length == 0)
                {
                    EditorGUILayout.LabelField("(无公共字段)", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var field in fields)
                    {
                        try
                        {
                            object value = field.GetValue(processNode);
                            EditorGUILayout.LabelField($"{field.Name}: {value ?? "null"}");
                        }
                        catch (System.Exception ex)
                        {
                            EditorGUILayout.LabelField($"{field.Name}: (读取失败: {ex.Message})");
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("子节点", EditorStyles.boldLabel);
            if (node.Childrens == null || node.Childrens.Count == 0)
            {
                EditorGUILayout.LabelField("(无子节点)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var child in node.Childrens)
                {
                    var childNode = (BehaviorNode)child;
                    if (GUILayout.Button($"节点 {childNode.ID} - {childNode.ProcessNode?.GetType().Name}", EditorStyles.miniButton))
                    {
                        _selectedNodeId = childNode.ID;
                    }
                }
            }
        }
    }
}
#endif