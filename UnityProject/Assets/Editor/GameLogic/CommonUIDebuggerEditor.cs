#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GameLogic;

namespace GameLogicEditor
{
    /// <summary>
    /// CommonUIDebugger的自定义Inspector面板
    /// </summary>
    [CustomEditor(typeof(CommonUIDebugger))]
    public class CommonUIDebuggerEditor : Editor
    {
        private CommonUIDebugger _debugger;
        private Vector2 _activeScrollPos;
        private Vector2 _idleScrollPos;
        private bool[] _poolFoldouts;
        
        // 样式
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _boldLabelStyle;
        
        private void OnEnable()
        {
            _debugger = (CommonUIDebugger)target;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    normal = { textColor = new Color(0.2f, 0.6f, 1f) }
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            if (_boldLabelStyle == null)
            {
                _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            InitStyles();

            // 头部信息
            DrawHeader();

            EditorGUILayout.Space(10);

            // 刷新设置
            DrawRefreshSettings();

            EditorGUILayout.Space(10);

            // 统计概览
            DrawStatistics();

            EditorGUILayout.Space(10);

            // 活跃对象
            DrawActiveObjects();

            EditorGUILayout.Space(10);

            // 空闲对象池
            DrawIdlePool();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 绘制头部
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("CommonUI 模块调试器", _headerStyle);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请在运行时查看调试信息", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制刷新设置
        /// </summary>
        private void DrawRefreshSettings()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRefreshInterval"), 
                new GUIContent("自动刷新间隔"));
            
            if (GUILayout.Button("手动刷新", GUILayout.Width(100)))
            {
                _debugger.RefreshData();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制统计概览
        /// </summary>
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("📊 统计概览", _headerStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            // 活跃数量
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(150));
            EditorGUILayout.LabelField("总活跃数", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(_debugger.totalActiveCount.ToString(), 
                _boldLabelStyle, GUILayout.Height(25));
            EditorGUILayout.EndVertical();
            
            // 空闲数量
            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(150));
            EditorGUILayout.LabelField("总空闲数", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(_debugger.totalIdleCount.ToString(), 
                _boldLabelStyle, GUILayout.Height(25));
            EditorGUILayout.EndVertical();
            
            GUI.backgroundColor = oldColor;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制活跃对象列表
        /// </summary>
        private void DrawActiveObjects()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("🟢 活跃对象", _headerStyle);

            if (_debugger.activeObjects == null || _debugger.activeObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("当前没有活跃对象", MessageType.Info);
            }
            else
            {
                _activeScrollPos = EditorGUILayout.BeginScrollView(_activeScrollPos, 
                    GUILayout.MaxHeight(200));
                
                foreach (var info in _debugger.activeObjects)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    
                    EditorGUILayout.LabelField("▶", GUILayout.Width(20));
                    EditorGUILayout.LabelField(info.typeName, GUILayout.Width(150));
                    
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                    EditorGUILayout.LabelField($"数量: {info.count}", _boldLabelStyle);
                    GUI.backgroundColor = oldColor;
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制空闲对象池
        /// </summary>
        private void DrawIdlePool()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("💤 空闲对象池", _headerStyle);

            if (_debugger.idlePoolObjects == null || _debugger.idlePoolObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("当前没有空闲对象", MessageType.Info);
            }
            else
            {
                if (_poolFoldouts == null || _poolFoldouts.Length != _debugger.idlePoolObjects.Length)
                {
                    _poolFoldouts = new bool[_debugger.idlePoolObjects.Length];
                }

                _idleScrollPos = EditorGUILayout.BeginScrollView(_idleScrollPos, 
                    GUILayout.MaxHeight(400));

                for (int i = 0; i < _debugger.idlePoolObjects.Length; i++)
                {
                    var poolInfo = _debugger.idlePoolObjects[i];
                    
                    EditorGUILayout.BeginVertical("box");
                    
                    // 池类型头部
                    EditorGUILayout.BeginHorizontal();
                    
                    _poolFoldouts[i] = EditorGUILayout.Foldout(_poolFoldouts[i], 
                        $"{poolInfo.typeName}", true, EditorStyles.foldoutHeader);
                    
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                    GUILayout.Label($"[{poolInfo.count}]", "box", GUILayout.Width(50));
                    GUI.backgroundColor = oldColor;
                    
                    EditorGUILayout.EndHorizontal();

                    // 展开显示详细信息
                    if (_poolFoldouts[i] && poolInfo.items != null && poolInfo.items.Length > 0)
                    {
                        EditorGUI.indentLevel++;
                        
                        // 表头
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField("索引", EditorStyles.boldLabel, GUILayout.Width(50));
                        EditorGUILayout.LabelField("回收时间", EditorStyles.boldLabel, GUILayout.Width(100));
                        EditorGUILayout.LabelField("销毁倒计时", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();
                        
                        // 数据行
                        foreach (var item in poolInfo.items)
                        {
                            EditorGUILayout.BeginHorizontal("box");
                            
                            // 索引
                            EditorGUILayout.LabelField($"#{item.index}", GUILayout.Width(50));
                            
                            // 回收时间
                            EditorGUILayout.LabelField($"{item.recycleTime:F1}s", GUILayout.Width(100));
                            
                            // 销毁倒计时（带颜色）
                            Color oldLabelColor = GUI.contentColor;
                            if (item.remainDestroyTime < 10f)
                                GUI.contentColor = Color.red;
                            else if (item.remainDestroyTime < 30f)
                                GUI.contentColor = new Color(1f, 0.6f, 0f); // 橙色
                            else
                                GUI.contentColor = Color.green;
                            
                            string timeText = $"⏱ {item.remainDestroyTime:F1}s";
                            EditorGUILayout.LabelField(timeText, _boldLabelStyle);
                            
                            GUI.contentColor = oldLabelColor;
                            
                            // 进度条
                            float progress = Mathf.Clamp01(item.remainDestroyTime / 60f);
                            Rect rect = GUILayoutUtility.GetRect(100, 18);
                            EditorGUI.ProgressBar(rect, progress, "");
                            
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif

