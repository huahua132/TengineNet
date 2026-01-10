#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree.Editor
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        private BehaviorTreeAsset _currentAsset;
        private Vector2 _scrollPosition;
        private Vector2 _offset;
        private float _zoom = 1.0f;
        
        private BehaviorNodeData _selectedNode;
        private BehaviorNodeData _connectingNode;
        private int _nextNodeId = 1;
        private Vector2 _lastMousePosition; // 保存最后的鼠标位置

        private const float NODE_WIDTH = 200f;
        private const float NODE_HEIGHT = 60f;
        private const float GRID_SIZE = 20f;

        private static readonly string[] NodeTypes = new[]
        {
            "SequenceNode",
            "SelectorNode",
            "ParallelNode"
        };

        [MenuItem("Tools/BehaviorTree/Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<BehaviorTreeEditorWindow>("Behavior Tree Editor");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            _offset = Vector2.zero;
            _lastMousePosition = Vector2.zero;
        }

        private void OnGUI()
        {
            // 保存当前鼠标位置
            if (Event.current != null && Event.current.type == EventType.MouseMove)
            {
                _lastMousePosition = Event.current.mousePosition;
            }

            DrawToolbar();
            DrawGrid();
            
            BeginWindows();
            DrawNodes();
            DrawConnections();
            EndWindows();

            ProcessEvents();
            DrawInspector();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _currentAsset = (BehaviorTreeAsset)EditorGUILayout.ObjectField(
                _currentAsset, typeof(BehaviorTreeAsset), false, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                CreateNewAsset();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                SaveAsset();
            }

            if (GUILayout.Button("Add Node", EditorStyles.toolbarDropDown, GUILayout.Width(80)))
            {
                ShowAddNodeMenu();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            Rect rect = new Rect(0, 20, position.width, position.height - 20);
            
            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            float gridSpacing = GRID_SIZE * _zoom;
            int widthDivs = Mathf.CeilToInt(rect.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(rect.height / gridSpacing);

            Vector2 gridOffset = new Vector2(_offset.x % gridSpacing, _offset.y % gridSpacing);

            for (int i = 0; i < widthDivs + 1; i++)
            {
                Handles.DrawLine(
                    new Vector3(gridSpacing * i + gridOffset.x, 20, 0),
                    new Vector3(gridSpacing * i + gridOffset.x, rect.height, 0));
            }

            for (int i = 0; i < heightDivs + 1; i++)
            {
                Handles.DrawLine(
                    new Vector3(0, gridSpacing * i + gridOffset.y + 20, 0),
                    new Vector3(rect.width, gridSpacing * i + gridOffset.y + 20, 0));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            if (_currentAsset == null || _currentAsset.treeData == null || _currentAsset.treeData.nodes == null)
                return;

            for (int i = 0; i < _currentAsset.treeData.nodes.Count; i++)
            {
                var node = _currentAsset.treeData.nodes[i];
                DrawNode(node, i);
            }
        }

        private void DrawNode(BehaviorNodeData node, int index)
        {
            Vector2 pos = node.editorPosition * _zoom + _offset;
            Rect rect = new Rect(pos.x, pos.y, NODE_WIDTH * _zoom, NODE_HEIGHT * _zoom);

            GUIStyle style = new GUIStyle(GUI.skin.window);
            if (node == _selectedNode)
            {
                style.normal.background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.8f, 1f));
            }

            Color originalColor = GUI.backgroundColor;
            if (_currentAsset.treeData != null && node.id == _currentAsset.treeData.rootId)
            {
                GUI.backgroundColor = Color.green;
            }

            rect = GUI.Window(index, rect, (id) =>
            {
                GUILayout.Label(node.name, EditorStyles.boldLabel);
                GUILayout.Label(node.processTypeName, EditorStyles.miniLabel);

                if (Event.current.type == EventType.ContextClick)
                {
                    ShowNodeContextMenu(node);
                    Event.current.Use();
                }

                GUI.DragWindow();
            }, GUIContent.none, style);

            GUI.backgroundColor = originalColor;

            node.editorPosition = (rect.position - _offset) / _zoom;
        }

        private void DrawConnections()
        {
            if (_currentAsset == null || _currentAsset.treeData == null)
                return;

            Handles.BeginGUI();
            
            foreach (var node in _currentAsset.treeData.nodes)
            {
                if (node.childrenIds == null) continue;
                
                foreach (var childId in node.childrenIds)
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
                Vector2 mousePos = Event.current != null ? Event.current.mousePosition : _lastMousePosition;
                DrawBezier(startPos, mousePos, Color.yellow);
            }

            Handles.EndGUI();
        }

        private void DrawConnection(BehaviorNodeData from, BehaviorNodeData to)
        {
            Vector2 startPos = GetNodeCenter(from);
            Vector2 endPos = GetNodeCenter(to);
            DrawBezier(startPos, endPos, Color.white);
        }

        private void DrawBezier(Vector2 start, Vector2 end, Color color)
        {
            Vector2 startTangent = start + Vector2.down * 50;
            Vector2 endTangent = end + Vector2.up * 50;
            
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 3f);
        }

        private Vector2 GetNodeCenter(BehaviorNodeData node)
        {
            Vector2 pos = node.editorPosition * _zoom + _offset;
            return new Vector2(
                pos.x + NODE_WIDTH * _zoom * 0.5f,
                pos.y + NODE_HEIGHT * _zoom);
        }

        private void ProcessEvents()
        {
            Event e = Event.current;
            if (e == null) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _selectedNode = GetNodeAtPosition(e.mousePosition);
                Repaint();
            }

            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                _offset += e.delta;
                Repaint();
            }

            if (e.type == EventType.ScrollWheel)
            {
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.01f, 0.5f, 2f);
                Repaint();
            }

            // 取消连接
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _connectingNode = null;
                Repaint();
            }
        }

        private BehaviorNodeData GetNodeAtPosition(Vector2 mousePosition)
        {
            if (_currentAsset == null || _currentAsset.treeData == null || _currentAsset.treeData.nodes == null)
                return null;

            for (int i = _currentAsset.treeData.nodes.Count - 1; i >= 0; i--)
            {
                var node = _currentAsset.treeData.nodes[i];
                Vector2 pos = node.editorPosition * _zoom + _offset;
                Rect rect = new Rect(pos.x, pos.y, NODE_WIDTH * _zoom, NODE_HEIGHT * _zoom);
                
                if (rect.Contains(mousePosition))
                {
                    return node;
                }
            }

            return null;
        }

        private void DrawInspector()
        {
            if (_selectedNode == null)
                return;

            Rect inspectorRect = new Rect(position.width - 250, 20, 250, position.height - 20);
            GUILayout.BeginArea(inspectorRect, GUI.skin.box);

            EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _selectedNode.name = EditorGUILayout.TextField("Name", _selectedNode.name);
            
            EditorGUILayout.LabelField("Type", _selectedNode.processTypeName);
            EditorGUILayout.LabelField("ID", _selectedNode.id.ToString());

            EditorGUILayout.Space();

            if (GUILayout.Button("Set As Root"))
            {
                if (_currentAsset != null && _currentAsset.treeData != null)
                {
                    _currentAsset.treeData.rootId = _selectedNode.id;
                    EditorUtility.SetDirty(_currentAsset);
                }
            }

            if (GUILayout.Button("Delete Node"))
            {
                DeleteNode(_selectedNode);
                _selectedNode = null;
            }

            GUILayout.EndArea();
        }

        private void ShowAddNodeMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (var nodeType in NodeTypes)
            {
                menu.AddItem(new GUIContent(nodeType), false, (userData) => AddNode((string)userData), nodeType);
            }
            menu.ShowAsContext();
        }

        private void ShowNodeContextMenu(BehaviorNodeData node)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Set as Root"), false, () =>
            {
                if (_currentAsset != null && _currentAsset.treeData != null)
                {
                    _currentAsset.treeData.rootId = node.id;
                    EditorUtility.SetDirty(_currentAsset);
                }
            });

            menu.AddItem(new GUIContent("Connect To..."), false, () =>
            {
                _connectingNode = node;
            });

            if (_connectingNode != null && _connectingNode != node)
            {
                menu.AddItem(new GUIContent("Connect Here"), false, () =>
                {
                    ConnectNodes(_connectingNode, node);
                    _connectingNode = null;
                });
            }

            menu.AddItem(new GUIContent("Cancel Connect"), _connectingNode != null, () =>
            {
                _connectingNode = null;
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteNode(node));

            menu.ShowAsContext();
        }

        private void AddNode(string nodeType)
        {
            if (_currentAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or select a BehaviorTreeAsset first!", "OK");
                return;
            }

            // 确保数据结构正确初始化
            if (_currentAsset.treeData == null)
            {
                _currentAsset.treeData = new BehaviorTreeData();
            }
            if (_currentAsset.treeData.nodes == null)
            {
                _currentAsset.treeData.nodes = new List<BehaviorNodeData>();
            }

            // 使用保存的鼠标位置或默认位置
            Vector2 mousePos = _lastMousePosition;
            if (mousePos == Vector2.zero)
            {
                mousePos = new Vector2(position.width * 0.5f, position.height * 0.5f);
            }

            var node = new BehaviorNodeData
            {
                id = _nextNodeId++,
                name = $"{nodeType}_{_nextNodeId}",
                processTypeName = nodeType,
                editorPosition = (mousePos - _offset) / _zoom,
                childrenIds = new List<int>()
            };

            _currentAsset.AddNode(node);
            
            // 如果是第一个节点，自动设为根节点
            if (_currentAsset.treeData.nodes.Count == 1)
            {
                _currentAsset.treeData.rootId = node.id;
            }

            EditorUtility.SetDirty(_currentAsset);
            Repaint();
            
            Debug.Log($"Added node: {node.name} at position {node.editorPosition}");
        }

        private void DeleteNode(BehaviorNodeData node)
        {
            if (_currentAsset == null || node == null)
                return;

            if (_currentAsset.treeData == null || _currentAsset.treeData.nodes == null)
                return;

            // 移除所有指向该节点的连接
            foreach (var n in _currentAsset.treeData.nodes)
            {
                if (n.childrenIds != null)
                {
                    n.childrenIds.RemoveAll(id => id == node.id);
                }
            }

            _currentAsset.RemoveNode(node.id);
            EditorUtility.SetDirty(_currentAsset);
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
                EditorUtility.SetDirty(_currentAsset);
            }

            Repaint();
        }

        private void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Behavior Tree Asset",
                "NewBehaviorTree",
                "asset",
                "Create a new behavior tree asset");

            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<BehaviorTreeAsset>();
                asset.treeData = new BehaviorTreeData
                {
                    treeName = System.IO.Path.GetFileNameWithoutExtension(path),
                    nodes = new List<BehaviorNodeData>()
                };
                
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                _currentAsset = asset;
                _nextNodeId = 1;
                
                Debug.Log($"Created new asset: {path}");
            }
        }

        private void SaveAsset()
        {
            if (_currentAsset != null)
            {
                EditorUtility.SetDirty(_currentAsset);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Success", $"Saved: {_currentAsset.name}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No asset selected!", "OK");
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
#endif

