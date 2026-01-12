#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace BehaviorTree.Editor
{
    /// <summary>
    /// 行为树资产编辑器 - 处理双击事件
    /// </summary>
    public class BehaviorTreeAssetEditor
    {
        /// <summary>
        /// 处理资产双击事件
        /// OnOpenAsset 属性会在双击资产时被调用
        /// 返回 true 表示已处理，不再使用默认编辑器
        /// </summary>
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            // 获取被双击的对象
            var asset = EditorUtility.InstanceIDToObject(instanceID) as BehaviorTreeAsset;
            
            // 如果是 BehaviorTreeAsset，打开编辑器窗口
            if (asset != null)
            {
                OpenEditorWindow(asset);
                return true; // 返回 true 表示已处理，不使用默认编辑器
            }
            
            return false; // 返回 false 让其他编辑器处理
        }

        /// <summary>
        /// 打开行为树编辑器窗口并加载资产
        /// </summary>
        private static void OpenEditorWindow(BehaviorTreeAsset asset)
        {
            // 获取或创建编辑器窗口
            var window = EditorWindow.GetWindow<BehaviorTreeEditorWindow>("Behavior Tree Editor");
            
            // 加载资产
            window.LoadAsset(asset);
            
            // 聚焦窗口
            window.Focus();
        }
    }
}
#endif