using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    [Serializable]
    public class BehaviorNodeData
    {
        public int id;
        public string name;
        public string processTypeName;
        public int parentId = -1;
        public List<int> childrenIds = new List<int>();
        public Vector2 editorPosition;
        
        // 节点备注
        public string comment = "";
        
        // 节点参数（用于序列化节点的配置参数）
        public Dictionary<string, string> parameters = new Dictionary<string, string>();
    }

    [Serializable]
    public class BehaviorTreeData
    {
        public string treeName;
        public int rootId;
        public List<BehaviorNodeData> nodes = new List<BehaviorNodeData>(); // 确保初始化
    }

    [CreateAssetMenu(fileName = "BehaviorTreeAsset", menuName = "BehaviorTree/Tree Asset")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        public BehaviorTreeData treeData = new BehaviorTreeData(); // 确保初始化

        // 确保数据结构正确初始化
        private void OnEnable()
        {
            if (treeData == null)
            {
                treeData = new BehaviorTreeData();
            }
            if (treeData.nodes == null)
            {
                treeData.nodes = new List<BehaviorNodeData>();
            }
        }

        public BehaviorNodeData GetNode(int id)
        {
            if (treeData?.nodes == null) return null;
            return treeData.nodes.Find(n => n.id == id);
        }

        public void AddNode(BehaviorNodeData node)
        {
            if (treeData == null)
            {
                treeData = new BehaviorTreeData();
            }
            if (treeData.nodes == null)
            {
                treeData.nodes = new List<BehaviorNodeData>();
            }
            treeData.nodes.Add(node);
        }

        public void RemoveNode(int id)
        {
            if (treeData?.nodes == null) return;
            treeData.nodes.RemoveAll(n => n.id == id);
        }
    }
}

