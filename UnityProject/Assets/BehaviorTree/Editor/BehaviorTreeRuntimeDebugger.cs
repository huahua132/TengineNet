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
            Selection.selectionChanged += OnSelectionChanged;
            RefreshTreeList();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
        }
        
        private void OnSelectionChanged()
        {
            // 只在运行时且选中了GameObject时处理
            if (!EditorApplication.isPlaying) return;
            if (Selection.activeGameObject == null) return;
            
            // 通过GameObject查找对应的行为树实例
            if (_runningTrees != null)
            {
                var treeInstance = _runningTrees.Find(t =>
                    t.BoundGameObject != null &&
                    t.BoundGameObject == Selection.activeGameObject);
                
                if (treeInstance != null && treeInstance != _selectedTree)
                {
                    _selectedTree = treeInstance;
                    _selectedNodeId = -1;
                    _canvasOffset = Vector2.zero;
                    _canvasZoom = 1.0f;
                    CalculateNodeLayout();
                    Repaint();
                }
            }
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
            bool nodeClicked = false;
            if (rootNode != null)
            {
                CalculateNodeLayout();
                
                // 绘制连接线
                Handles.BeginGUI();
                DrawConnectionsRecursive(rootNode, localRect);
                Handles.EndGUI();
                
                // 绘制节点，并记录是否有节点被点击
                nodeClicked = DrawNodesRecursive(rootNode, localRect);
            }
            
            GUI.EndClip();
            
            // 处理画布空白区域点击（取消选中）
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !nodeClicked)
            {
                if (canvasRect.Contains(e.mousePosition))
                {
                    _selectedNodeId = -1;
                    e.Use();
                    Repaint();
                }
            }
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

        /// <summary>
        /// 横向布局信息（参考编辑器实现）
        /// </summary>
        private class HorizontalLayoutInfo
        {
            public float subtreeHeight;          // 子树总高度（包括节点实际高度和间距）
            public float relativeY;              // 相对于父节点的Y偏移
            public List<float> childrenOffsets;  // 子节点的相对Y偏移列表
            public List<float> childNodeHeights; // 子节点的实际高度列表
        }
        
        private void CalculateNodeLayout()
        {
            if (_selectedTree == null || _selectedTree.Tree == null) return;
            
            _nodePositions.Clear();
            var rootNode = _selectedTree.Tree.GetRootNode();
            if (rootNode == null) return;
            
            // 横向布局：从左到右（完全参考编辑器）
            const float START_X = 100f;
            const float START_Y = 300f;
            
            // 第一步：计算布局信息
            Dictionary<int, HorizontalLayoutInfo> layoutInfos = new Dictionary<int, HorizontalLayoutInfo>();
            CalculateHorizontalLayout(rootNode, layoutInfos);
            
            // 第二步：应用绝对位置
            ApplyHorizontalPositions(rootNode, START_X, START_Y, layoutInfos);
        }
        
        /// <summary>
        /// 递归计算横向布局信息（完全参考编辑器算法）
        /// </summary>
        private float CalculateHorizontalLayout(BehaviorNode node, Dictionary<int, HorizontalLayoutInfo> layoutInfos)
        {
            var info = new HorizontalLayoutInfo();
            info.childrenOffsets = new List<float>();
            info.childNodeHeights = new List<float>();
            
            // 获取当前节点的实际高度
            float nodeHeight = CalculateNodeHeight(node);
            
            // 没有子节点，返回节点本身的高度加上padding
            if (node.Childrens == null || node.Childrens.Count == 0)
            {
                info.subtreeHeight = nodeHeight + VerticalPadding;
                info.relativeY = 0;
                layoutInfos[node.ID] = info;
                return info.subtreeHeight;
            }
            
            // 获取所有子节点并按ID排序（从小到大，从上到下）
            var sortedChildren = node.Childrens
                .Cast<BehaviorNode>()
                .OrderBy(n => n.ID)
                .ToList();
            
            // 递归计算所有子节点子树的高度
            List<float> childSubtreeHeights = new List<float>();
            foreach (var childNode in sortedChildren)
            {
                float childHeight = CalculateHorizontalLayout(childNode, layoutInfos);
                childSubtreeHeights.Add(childHeight);
                info.childNodeHeights.Add(CalculateNodeHeight(childNode));
            }
            
            // 计算所有子节点的总高度（子树高度之和）
            float totalHeight = 0;
            foreach (var height in childSubtreeHeights)
            {
                totalHeight += height;
            }
            
            // 计算每个子节点的Y偏移量（从上到下排列，ID小的在上面）
            float currentOffset = -totalHeight / 2;
            for (int i = 0; i < childSubtreeHeights.Count; i++)
            {
                // 子节点居中于其子树高度范围内
                float childCenterOffset = currentOffset + childSubtreeHeights[i] / 2;
                info.childrenOffsets.Add(childCenterOffset);
                currentOffset += childSubtreeHeights[i];
            }
            
            // 子树总高度取：所有子节点子树高度之和 与 当前节点高度+padding 中的较大值
            info.subtreeHeight = Mathf.Max(totalHeight, nodeHeight + VerticalPadding);
            info.relativeY = 0; // 父节点垂直居中
            layoutInfos[node.ID] = info;
            
            return info.subtreeHeight;
        }
        
        /// <summary>
        /// 应用横向布局的绝对位置（完全参考编辑器算法）
        /// </summary>
        private void ApplyHorizontalPositions(BehaviorNode node, float absoluteX, float absoluteY, Dictionary<int, HorizontalLayoutInfo> layoutInfos)
        {
            // 设置当前节点的绝对位置（Y坐标考虑节点高度，使其居中）
            float nodeHeight = CalculateNodeHeight(node);
            _nodePositions[node.ID] = new Vector2(absoluteX, absoluteY - nodeHeight / 2);
            
            // 没有子节点，直接返回
            if (node.Childrens == null || node.Childrens.Count == 0)
                return;
            
            // 获取布局信息
            if (!layoutInfos.TryGetValue(node.ID, out var info))
                return;
            
            // 获取所有子节点并按ID排序
            var sortedChildren = node.Childrens
                .Cast<BehaviorNode>()
                .OrderBy(n => n.ID)
                .ToList();
            
            // 递归设置子节点位置（子节点在父节点右边，纵向排列，ID小的在上面）
            for (int i = 0; i < sortedChildren.Count && i < info.childrenOffsets.Count; i++)
            {
                var childNode = sortedChildren[i];
                float childX = absoluteX + HorizontalSpacing;  // 子节点在父节点右边
                float childY = absoluteY + info.childrenOffsets[i];  // Y坐标根据偏移量调整（已经是中心点）
                ApplyHorizontalPositions(childNode, childX, childY, layoutInfos);
            }
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
            
            // 判断是否是执行路径（父子节点都在执行栈中）
            bool isInExecutionPath = IsNodeInStack(from) && IsNodeInStack(to);
            Color lineColor = isInExecutionPath ? new Color(0.3f, 0.85f, 0.3f) : Color.white; // 绿色或白色
            
            // 绘制贝塞尔曲线
            float distance = Vector2.Distance(startPos, endPos);
            float tangentLength = Mathf.Min(distance * 0.5f, 80f);
            
            Vector2 startTangent = startPos + Vector2.down * tangentLength;
            Vector2 endTangent = endPos + Vector2.up * tangentLength;
            
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, lineColor, null, 3f);
        }
        
        /// <summary>
        /// 检查节点是否在执行栈中
        /// </summary>
        private bool IsNodeInStack(BehaviorNode node)
        {
            if (_selectedTree == null || _selectedTree.Tree == null) return false;
            
            var context = _selectedTree.Tree.GetContext();
            if (context == null) return false;
            
            // 通过检查节点的IsResume状态来判断是否在栈中
            // 或者通过遍历栈来检查（如果有公开的栈访问方法）
            return node.IsResume();
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

        private bool DrawNodesRecursive(BehaviorNode node, Rect canvasRect)
        {
            if (!_nodePositions.TryGetValue(node.ID, out Vector2 pos)) return false;
            
            bool clicked = DrawNode(node, pos, canvasRect);
            
            var children = node.Childrens;
            if (children != null)
            {
                foreach (BehaviorNode child in children)
                {
                    bool childClicked = DrawNodesRecursive(child, canvasRect);
                    if (childClicked) clicked = true;
                }
            }
            
            return clicked;
        }

        private bool DrawNode(BehaviorNode node, Vector2 position, Rect canvasRect)
        {
            Vector2 screenPos = position * _canvasZoom + _canvasOffset;
            float nodeHeight = CalculateNodeHeight(node);
            Rect nodeRect = new Rect(screenPos.x, screenPos.y, NodeWidth * _canvasZoom, nodeHeight * _canvasZoom);
            
            // 获取节点信息
            var nodeInfo = BehaviorNodeRegistry.GetNodeInfo(node.ProcessNode?.GetType().Name);
            Color nodeColor = nodeInfo != null ? nodeInfo.Color : Color.gray;
            
            // 绘制节点ID（在节点正上方）
            GUIStyle idStyle = new GUIStyle(EditorStyles.miniLabel);
            idStyle.fontSize = Mathf.RoundToInt(10 * _canvasZoom);
            idStyle.alignment = TextAnchor.MiddleCenter;
            idStyle.normal.textColor = Color.white;
            
            Rect idRect = new Rect(nodeRect.x, nodeRect.y - 15 * _canvasZoom, NodeWidth * _canvasZoom, 15 * _canvasZoom);
            GUI.Label(idRect, $"ID: {node.ID}", idStyle);
            
            // 绘制边框（使用节点类型颜色）
            float borderWidth = 2f;
            EditorGUI.DrawRect(new Rect(nodeRect.x - borderWidth, nodeRect.y - borderWidth,
                nodeRect.width + borderWidth * 2, nodeRect.height + borderWidth * 2), nodeColor);
            
            // 选中节点高亮（黄色边框）
            if (node.ID == _selectedNodeId)
            {
                EditorGUI.DrawRect(new Rect(nodeRect.x - 3, nodeRect.y - 3, nodeRect.width + 6, nodeRect.height + 6), Color.yellow);
            }
            
            // 运行状态高亮（蓝色边框）
            if (node.IsResume())
            {
                EditorGUI.DrawRect(new Rect(nodeRect.x - 3, nodeRect.y - 3, nodeRect.width + 6, nodeRect.height + 6),
                    new Color(0.3f, 0.6f, 1f, 0.8f));
            }
            
            // === 标题部分（彩色背景）===
            float headerHeight = NodeHeaderHeight * _canvasZoom;
            Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, headerHeight);
            
            // 绘制标题背景（使用节点类型颜色，完全不透明）
            EditorGUI.DrawRect(headerRect, nodeColor);
            
            // 绘制标题内容（左上角图标 + 节点类型）
            float iconSize = 16 * _canvasZoom;
            float iconPadding = 5 * _canvasZoom;
            
            if (nodeInfo != null)
            {
                GUIContent iconContent = EditorGUIUtility.IconContent(nodeInfo.Icon);
                if (iconContent != null && iconContent.image != null)
                {
                    Rect iconRect = new Rect(nodeRect.x + iconPadding, nodeRect.y + (headerHeight - iconSize) / 2, iconSize, iconSize);
                    GUI.Label(iconRect, iconContent);
                }
            }
            
            // 节点显示名称（使用节点的Name属性，黑色加粗）
            GUIStyle typeNameStyle = new GUIStyle(EditorStyles.boldLabel);
            typeNameStyle.fontSize = Mathf.RoundToInt(11 * _canvasZoom);
            typeNameStyle.alignment = TextAnchor.MiddleLeft;
            typeNameStyle.normal.textColor = Color.black;
            typeNameStyle.hover.textColor = Color.black;
            typeNameStyle.fontStyle = FontStyle.Bold;
            
            string displayName = nodeInfo != null ? nodeInfo.Name : node.ProcessNode?.GetType().Name;
            Rect typeNameRect = new Rect(nodeRect.x + iconSize + iconPadding * 2, nodeRect.y,
                nodeRect.width - iconSize - iconPadding * 3, headerHeight);
            GUI.Label(typeNameRect, displayName, typeNameStyle);
            
            // === 内容部分（纯白色背景）===
            Rect contentRect = new Rect(nodeRect.x, nodeRect.y + headerHeight, nodeRect.width, nodeRect.height - headerHeight);
            
            // 绘制内容背景（纯白色）
            EditorGUI.DrawRect(contentRect, Color.white);
            
            // 绘制内容
            GUIStyle contentStyle = new GUIStyle(EditorStyles.label);
            contentStyle.fontSize = Mathf.RoundToInt(9 * _canvasZoom);
            contentStyle.alignment = TextAnchor.UpperLeft;
            contentStyle.normal.textColor = Color.black;
            contentStyle.wordWrap = false;
            contentStyle.clipping = TextClipping.Clip;
            contentStyle.hover.textColor = Color.black;
            
            float contentY = nodeRect.y + headerHeight + NodePadding * _canvasZoom;
            float contentPadding = NodePadding * _canvasZoom;
            
            // 获取节点数据
            var nodeData = _selectedTree.Tree.GetAsset()?.GetNode(node.ID);
            if (nodeData != null)
            {
                // 优先显示备注（如果有）
                if (!string.IsNullOrEmpty(nodeData.comment))
                {
                    GUIStyle commentLabelStyle = new GUIStyle(EditorStyles.label);
                    commentLabelStyle.fontSize = Mathf.RoundToInt(8 * _canvasZoom);
                    commentLabelStyle.alignment = TextAnchor.UpperLeft;
                    commentLabelStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f); // 灰色标签
                    commentLabelStyle.hover.textColor = new Color(0.5f, 0.5f, 0.5f);
                    commentLabelStyle.fontStyle = FontStyle.Bold;
                    
                    GUIStyle commentStyle = new GUIStyle(EditorStyles.label);
                    commentStyle.fontSize = Mathf.RoundToInt(8 * _canvasZoom);
                    commentStyle.alignment = TextAnchor.UpperLeft;
                    commentStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f); // 深灰色
                    commentStyle.hover.textColor = new Color(0.3f, 0.3f, 0.3f);
                    commentStyle.wordWrap = true;
                    commentStyle.fontStyle = FontStyle.Italic;
                    
                    float labelWidth = 50 * _canvasZoom;
                    
                    // 绘制"备注:"标签
                    Rect commentLabelRect = new Rect(nodeRect.x + contentPadding, contentY, labelWidth, NodeParamLineHeight * _canvasZoom);
                    GUI.Label(commentLabelRect, "备注:", commentLabelStyle);
                    
                    // 计算备注内容区域
                    float commentContentWidth = nodeRect.width - contentPadding * 2 - labelWidth;
                    float commentHeight = commentStyle.CalcHeight(new GUIContent(nodeData.comment), commentContentWidth);
                    Rect commentRect = new Rect(nodeRect.x + contentPadding + labelWidth, contentY, commentContentWidth, commentHeight);
                    GUI.Label(commentRect, nodeData.comment, commentStyle);
                    contentY += Mathf.Max(commentHeight, NodeParamLineHeight * _canvasZoom) + NodePadding * _canvasZoom * 0.5f;
                }
                
                // 显示参数
                if (nodeData.parametersList != null && nodeData.parametersList.Count > 0)
                {
                    foreach (var param in nodeData.parametersList)
                    {
                        if (!string.IsNullOrEmpty(param.value))
                        {
                            float labelWidth = 70 * _canvasZoom;
                            
                            // 绘制参数名（左侧）- 确保不换行
                            GUIStyle paramLabelStyle = new GUIStyle(contentStyle);
                            paramLabelStyle.fontStyle = FontStyle.Bold;
                            paramLabelStyle.wordWrap = false;
                            paramLabelStyle.clipping = TextClipping.Clip;
                            
                            Rect paramLabelRect = new Rect(nodeRect.x + contentPadding, contentY, labelWidth, NodeParamLineHeight * _canvasZoom);
                            GUI.Label(paramLabelRect, $"{param.key}:", paramLabelStyle);
                            
                            // 绘制参数值（右侧）- 确保不换行
                            GUIStyle paramValueStyle = new GUIStyle(contentStyle);
                            paramValueStyle.wordWrap = false;
                            paramValueStyle.clipping = TextClipping.Clip;
                            
                            Rect paramValueRect = new Rect(nodeRect.x + contentPadding + labelWidth, contentY,
                                nodeRect.width - contentPadding * 2 - labelWidth, NodeParamLineHeight * _canvasZoom);
                            string displayValue = param.value.Length > 12 ? param.value.Substring(0, 9) + "..." : param.value;
                            GUI.Label(paramValueRect, displayValue, paramValueStyle);
                            contentY += NodeParamLineHeight * _canvasZoom;
                        }
                    }
                }
            }
            
            // 处理点击
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 mousePos = e.mousePosition;
                if (nodeRect.Contains(mousePos))
                {
                    _selectedNodeId = node.ID;
                    e.Use();
                    Repaint();
                    return true; // 返回true表示节点被点击
                }
            }
            
            return false; // 返回false表示节点未被点击
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
            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.normal.textColor = new Color(0.3f, 0.6f, 1f); // 蓝色标题
            EditorGUILayout.LabelField("上下文信息", titleStyle);
            EditorGUILayout.Space(5);

            var context = _selectedTree.Tree?.GetContext();
            if (context == null)
            {
                EditorGUILayout.HelpBox("上下文不可用", MessageType.Warning);
                return;
            }

            // 基本信息区域
            DrawSectionBox("基本信息", new Color(0.85f, 0.95f, 1f), () =>
            {
                DrawInfoRow("执行栈深度", context.GetStackCount().ToString(), new Color(0.2f, 0.5f, 0.8f));
                DrawInfoRow("最后结果", context.GetLastRet().ToString(), GetResultColor(context.GetLastRet()));
                DrawInfoRow("是否中断", context.IsAbort().ToString(), context.IsAbort() ? Color.red : Color.green);
            });

            EditorGUILayout.Space(5);

            // 绑定对象区域
            var transform = context.GetBindTransform();
            if (transform != null)
            {
                DrawSectionBox("绑定对象", new Color(0.95f, 0.95f, 0.85f), () =>
                {
                    EditorGUILayout.ObjectField("Transform", transform, typeof(Transform), true);
                    DrawInfoRow("GameObject", transform.gameObject.name, new Color(0.4f, 0.6f, 0.4f));
                    DrawInfoRow("位置", transform.position.ToString(), new Color(0.4f, 0.5f, 0.6f));
                });
                
                EditorGUILayout.Space(5);
            }
            
            // 显示所有黑板数据
            DrawBlackboardData(context);
        }
        
        /// <summary>
        /// 显示所有黑板数据
        /// </summary>
        private void DrawBlackboardData(BehaviorContext context)
        {
            try
            {
                // 通过反射获取_blackBoards字段
                var blackBoardsField = typeof(BehaviorContext).GetField("_blackBoards",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (blackBoardsField != null)
                {
                    var blackBoards = blackBoardsField.GetValue(context) as System.Collections.IDictionary;
                    
                    if (blackBoards != null && blackBoards.Count > 0)
                    {
                        DrawSectionBox("黑板数据", new Color(1f, 0.9f, 0.85f), () =>
                        {
                            foreach (System.Collections.DictionaryEntry entry in blackBoards)
                            {
                                var type = entry.Key as System.Type;
                                var blackboard = entry.Value as BlackboardBase;
                                
                                if (type != null && blackboard != null)
                                {
                                    // 显示黑板类型
                                    GUIStyle blackboardTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                                    blackboardTitleStyle.normal.textColor = new Color(0.8f, 0.4f, 0.2f);
                                    EditorGUILayout.LabelField($"◆ {type.Name}", blackboardTitleStyle);
                                    
                                    EditorGUI.indentLevel++;
                                    
                                    // 获取黑板的所有公共字段
                                    var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    
                                    if (fields.Length > 0)
                                    {
                                        foreach (var field in fields)
                                        {
                                            try
                                            {
                                                object value = field.GetValue(blackboard);
                                                
                                                // 如果是UnityEngine.Object类型，显示为ObjectField
                                                if (value is UnityEngine.Object unityObj)
                                                {
                                                    EditorGUILayout.BeginHorizontal();
                                                    GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                                                    labelStyle.fontStyle = FontStyle.Bold;
                                                    EditorGUILayout.LabelField(field.Name + ":", labelStyle, GUILayout.Width(100));
                                                    EditorGUILayout.ObjectField(unityObj, field.FieldType, true);
                                                    EditorGUILayout.EndHorizontal();
                                                }
                                                else
                                                {
                                                    string valueStr = value != null ? value.ToString() : "null";
                                                    DrawInfoRow(field.Name, valueStr, new Color(0.6f, 0.4f, 0.2f));
                                                }
                                            }
                                            catch (System.Exception ex)
                                            {
                                                DrawInfoRow(field.Name, "(读取失败)", Color.red);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField("  (无公共字段)", EditorStyles.miniLabel);
                                    }
                                    
                                    EditorGUI.indentLevel--;
                                    EditorGUILayout.Space(3);
                                }
                            }
                        });
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("当前没有黑板数据", MessageType.Info);
                    }
                }
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.HelpBox($"读取黑板数据失败: {ex.Message}", MessageType.Warning);
            }
        }

        private void DrawNodeProperties()
        {
            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.normal.textColor = new Color(0.3f, 0.85f, 0.3f); // 绿色标题
            EditorGUILayout.LabelField("节点属性", titleStyle);
            EditorGUILayout.Space(5);

            var nodeDict = _selectedTree.Tree?.GetNodeDict();
            if (nodeDict == null || !nodeDict.TryGetValue(_selectedNodeId, out var node))
            {
                EditorGUILayout.HelpBox("节点不存在", MessageType.Warning);
                return;
            }

            // 基本信息区域
            var nodeInfo = BehaviorNodeRegistry.GetNodeInfo(node.ProcessNode?.GetType().Name);
            Color nodeColor = nodeInfo != null ? nodeInfo.Color : Color.gray;
            
            DrawSectionBox("基本信息", new Color(nodeColor.r * 0.3f + 0.7f, nodeColor.g * 0.3f + 0.7f, nodeColor.b * 0.3f + 0.7f), () =>
            {
                DrawInfoRow("节点ID", node.ID.ToString(), nodeColor);
                DrawInfoRow("类型", node.ProcessNode?.GetType().Name, nodeColor);
                DrawInfoRow("是否挂起", node.IsResume().ToString(), node.IsResume() ? new Color(0.3f, 0.6f, 1f) : Color.gray);
            });

            EditorGUILayout.Space(5);
            
            // 显示黑板依赖关系
            DrawNodeBlackboardIO(node, nodeInfo);

            EditorGUILayout.Space(5);

            if (node.ProcessNode != null)
            {
                var processNode = node.ProcessNode;
                
                // 获取所有字段（公共+私有）
                var allFields = processNode.GetType().GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                
                // 分类字段
                var publicFields = allFields.Where(f => f.IsPublic).ToList();
                var privateFields = allFields.Where(f => !f.IsPublic).ToList();
                
                // 显示公共字段
                if (publicFields.Count > 0)
                {
                    DrawSectionBox("公共字段", new Color(0.85f, 1f, 0.85f), () =>
                    {
                        foreach (var field in publicFields)
                        {
                            try
                            {
                                object value = field.GetValue(processNode);
                                DrawInfoRow(field.Name, value?.ToString() ?? "null", new Color(0.2f, 0.6f, 0.2f));
                            }
                            catch (System.Exception ex)
                            {
                                DrawInfoRow(field.Name, "(读取失败)", Color.red);
                            }
                        }
                    });
                    
                    EditorGUILayout.Space(5);
                }
                
                // 显示私有字段
                if (privateFields.Count > 0)
                {
                    DrawSectionBox("私有字段", new Color(0.95f, 0.95f, 0.95f), () =>
                    {
                        foreach (var field in privateFields)
                        {
                            try
                            {
                                object value = field.GetValue(processNode);
                                DrawInfoRow(field.Name, value?.ToString() ?? "null", new Color(0.5f, 0.5f, 0.5f));
                            }
                            catch (System.Exception ex)
                            {
                                DrawInfoRow(field.Name, "(读取失败)", new Color(0.7f, 0.3f, 0.3f));
                            }
                        }
                    });
                    
                    EditorGUILayout.Space(5);
                }
            }

            // 子节点区域
            if (node.Childrens != null && node.Childrens.Count > 0)
            {
                DrawSectionBox("子节点", new Color(1f, 0.95f, 0.85f), () =>
                {
                    foreach (var child in node.Childrens)
                    {
                        var childNode = (BehaviorNode)child;
                        Color buttonColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.7f);
                        if (GUILayout.Button($"节点 {childNode.ID} - {childNode.ProcessNode?.GetType().Name}", GUILayout.Height(25)))
                        {
                            _selectedNodeId = childNode.ID;
                        }
                        GUI.backgroundColor = buttonColor;
                    }
                });
            }
        }
        
        // 辅助方法：绘制带背景的区域
        private void DrawSectionBox(string title, Color backgroundColor, System.Action content)
        {
            // 区域标题
            GUIStyle sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionTitleStyle.fontSize = 11;
            EditorGUILayout.LabelField(title, sectionTitleStyle);
            
            // 带背景的内容区
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = oldBgColor;
            
            content?.Invoke();
            
            EditorGUILayout.EndVertical();
        }
        
        // 辅助方法：绘制信息行
        private void DrawInfoRow(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 标签
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(label + ":", labelStyle, GUILayout.Width(100));
            
            // 值
            GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
            valueStyle.normal.textColor = valueColor;
            EditorGUILayout.LabelField(value, valueStyle);
            
            EditorGUILayout.EndHorizontal();
        }
        
        // 辅助方法：根据结果获取颜色
        private Color GetResultColor(BehaviorRet result)
        {
            switch (result)
            {
                case BehaviorRet.SUCCESS:
                    return new Color(0.2f, 0.7f, 0.2f); // 绿色
                case BehaviorRet.FAIL:
                    return new Color(0.8f, 0.2f, 0.2f); // 红色
                case BehaviorRet.RUNNING:
                    return new Color(0.2f, 0.5f, 0.9f); // 蓝色
                case BehaviorRet.ABORT:
                    return new Color(0.9f, 0.5f, 0.1f); // 橙色
                default:
                    return Color.gray;
           }
       }
       
       /// <summary>
       /// 显示节点的黑板IO依赖关系
       /// </summary>
       private void DrawNodeBlackboardIO(BehaviorNode node, BehaviorNodeTypeInfo nodeInfo)
       {
           if (node == null || node.ProcessNode == null) return;
           
           // 获取节点类型上的BlackboardIO特性
           var nodeType = node.ProcessNode.GetType();
           var blackboardIOAttrs = nodeType.GetCustomAttributes(typeof(BehaviorTree.BlackboardIOAttribute), false);
           
           if (blackboardIOAttrs == null || blackboardIOAttrs.Length == 0)
           {
               // 没有黑板依赖，不显示该区域
               return;
           }
           
           // 分别显示输入和输出
           var inputs = new System.Collections.Generic.List<BehaviorTree.BlackboardIOAttribute>();
           var outputs = new System.Collections.Generic.List<BehaviorTree.BlackboardIOAttribute>();
           
           foreach (BehaviorTree.BlackboardIOAttribute attr in blackboardIOAttrs)
           {
               if (attr.Type == BehaviorTree.BlackboardIOAttribute.IOType.Read)
                   inputs.Add(attr);
               else
                   outputs.Add(attr);
           }
           
           DrawSectionBox("黑板依赖", new Color(1f, 0.95f, 0.85f), () =>
           {
               // 显示输入（读取）
               if (inputs.Count > 0)
               {
                   GUIStyle inputLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                   inputLabelStyle.normal.textColor = new Color(0.3f, 0.6f, 1.0f); // 蓝色
                   inputLabelStyle.fontSize = 11;
                   
                   EditorGUILayout.LabelField("📥 输入（读取）", inputLabelStyle);
                   EditorGUI.indentLevel++;
                   
                   foreach (var input in inputs)
                   {
                       string displayText = $"{input.GetFullPath()}";
                       if (!string.IsNullOrEmpty(input.Description))
                       {
                           displayText += $"\n  {input.Description}";
                       }
                       
                       GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
                       valueStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
                       valueStyle.fontSize = 10;
                       valueStyle.wordWrap = true;
                       EditorGUILayout.LabelField(displayText, valueStyle);
                   }
                   
                   EditorGUI.indentLevel--;
                   EditorGUILayout.Space(3);
               }
               
               // 显示输出（写入）
               if (outputs.Count > 0)
               {
                   GUIStyle outputLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                   outputLabelStyle.normal.textColor = new Color(1.0f, 0.6f, 0.2f); // 橙色
                   outputLabelStyle.fontSize = 11;
                   
                   EditorGUILayout.LabelField("📤 输出（写入）", outputLabelStyle);
                   EditorGUI.indentLevel++;
                   
                   foreach (var output in outputs)
                   {
                       string displayText = $"{output.GetFullPath()}";
                       if (!string.IsNullOrEmpty(output.Description))
                       {
                           displayText += $"\n  {output.Description}";
                       }
                       
                       GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
                       valueStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
                       valueStyle.fontSize = 10;
                       valueStyle.wordWrap = true;
                       EditorGUILayout.LabelField(displayText, valueStyle);
                   }
                   
                   EditorGUI.indentLevel--;
                   EditorGUILayout.Space(3);
               }
               
               // 提示信息
               GUIStyle hintStyle = new GUIStyle(EditorStyles.miniLabel);
               hintStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
               hintStyle.fontStyle = FontStyle.Italic;
               EditorGUILayout.LabelField("显示该节点读取和写入的黑板数据", hintStyle);
           });
       }
   }
}
#endif