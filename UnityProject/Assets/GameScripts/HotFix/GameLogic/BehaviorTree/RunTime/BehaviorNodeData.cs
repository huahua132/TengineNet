using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    [Serializable]
    public class SerializableParameter
    {
        public string key;
        public string value;
    }

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
        
        // 节点参数（使用可序列化的列表代替Dictionary）
        public List<SerializableParameter> parametersList = new List<SerializableParameter>();
        
        // 辅助属性：提供Dictionary接口（不序列化）
        [System.NonSerialized]
        private Dictionary<string, string> _parametersCache;
        
        public Dictionary<string, string> parameters
        {
            get
            {
                if (_parametersCache == null)
                {
                    _parametersCache = new Dictionary<string, string>();
                    if (parametersList != null)
                    {
                        foreach (var param in parametersList)
                        {
                            _parametersCache[param.key] = param.value;
                        }
                    }
                }
                return _parametersCache;
            }
        }
        
        // 保存参数到列表
        public void SaveParameters(Dictionary<string, string> dict)
        {
            parametersList.Clear();
            foreach (var kv in dict)
            {
                parametersList.Add(new SerializableParameter { key = kv.Key, value = kv.Value });
            }
            _parametersCache = null; // 清除缓存
        }
        
        // 设置单个参数
        public void SetParameter(string key, string value)
        {
            // 更新缓存
            if (_parametersCache == null)
            {
                _parametersCache = new Dictionary<string, string>();
            }
            _parametersCache[key] = value;
            
            // 更新列表
            var existing = parametersList.Find(p => p.key == key);
            if (existing != null)
            {
                existing.value = value;
            }
            else
            {
                parametersList.Add(new SerializableParameter { key = key, value = value });
            }
        }
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
            
            // 迁移旧数据：确保所有节点的新字段都已初始化
            foreach (var node in treeData.nodes)
            {
                if (node.parametersList == null)
                {
                    node.parametersList = new List<SerializableParameter>();
                }
                if (node.comment == null)
                {
                    node.comment = "";
                }
                if (node.childrenIds == null)
                {
                    node.childrenIds = new List<int>();
                }
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

