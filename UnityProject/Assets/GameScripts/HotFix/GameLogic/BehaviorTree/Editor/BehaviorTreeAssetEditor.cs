#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BehaviorTree.Editor
{
    /// <summary>
    /// 行为树资源编辑器 - 支持双击打开
    /// </summary>
    [CustomEditor(typeof(BehaviorTreeAsset))]
    public class BehaviorTreeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BehaviorTreeAsset asset = (BehaviorTreeAsset)target;

            EditorGUILayout.Space(10);
            
            // 显示资源信息
            EditorGUILayout.LabelField("Behavior Tree Asset", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (asset.treeData != null)
            {
                EditorGUILayout.LabelField("Tree Name:", asset.treeData.treeName);
                EditorGUILayout.LabelField("Root ID:", asset.treeData.rootId.ToString());
                EditorGUILayout.LabelField("Node Count:", asset.treeData.nodes != null ? asset.treeData.nodes.Count.ToString() : "0");
            }
            else
            {
                EditorGUILayout.HelpBox("Tree data is null. This asset may be corrupted.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // 打开编辑器按钮
            if (GUILayout.Button("Open in Behavior Tree Editor", GUILayout.Height(40)))
            {
                OpenEditor(asset);
            }

            EditorGUILayout.Space(10);

            // 显示原始数据
            if (GUILayout.Button("Show Raw Data"))
            {
                DrawDefaultInspector();
            }
        }

        /// <summary>
        /// 双击资源时打开编辑器
        /// </summary>
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            
            if (obj is BehaviorTreeAsset asset)
            {
                OpenEditor(asset);
                return true;
            }
            
            return false;
        }

        private static void OpenEditor(BehaviorTreeAsset asset)
        {
            var window = EditorWindow.GetWindow<BehaviorTreeEditorWindow>("Behavior Tree Editor");
            window.minSize = new Vector2(1200, 600);
            
            // 通过反射设置当前资源（因为字段是私有的）
            var field = typeof(BehaviorTreeEditorWindow).GetField("_currentAsset", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(window, asset);
                
                // 触发资源变更
                var method = typeof(BehaviorTreeEditorWindow).GetMethod("OnAssetChanged", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(window, null);
                }
            }
            
            window.Show();
            window.Focus();
        }
    }
}
#endif