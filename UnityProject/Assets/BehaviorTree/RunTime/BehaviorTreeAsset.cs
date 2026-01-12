using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 行为树资产文件
    /// 注意：ScriptableObject 必须在独立的文件中，文件名必须与类名匹配
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorTreeAsset", menuName = "BehaviorTree/Tree Asset")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        [Tooltip("行为树名称")]
        public string treeName = "";
        
        [Tooltip("根节点ID")]
        public int rootId;
        
        [Tooltip("所有节点数据")]
        public List<BehaviorNodeData> nodes = new List<BehaviorNodeData>();

        /// <summary>
        /// 根据ID获取节点
        /// </summary>
        public BehaviorNodeData GetNode(int id)
        {
            if (nodes == null) return null;
            return nodes.Find(n => n.id == id);
        }

        /// <summary>
        /// 添加节点
        /// </summary>
        public void AddNode(BehaviorNodeData node)
        {
            if (nodes == null)
            {
                nodes = new List<BehaviorNodeData>();
            }
            nodes.Add(node);
        }

        /// <summary>
        /// 移除节点
        /// </summary>
        public void RemoveNode(int id)
        {
            if (nodes == null) return;
            nodes.RemoveAll(n => n.id == id);
        }
    }
}