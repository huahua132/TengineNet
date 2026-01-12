using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    [System.Serializable]
    public class SerializableParameter
    {
        public string key = "";
        public string value = "";
    }

    [System.Serializable]
    public class BehaviorNodeData
    {
        public int id;
        public string name = "";
        public string processTypeName = "";
        public int parentId = -1;
        public List<int> childrenIds = new List<int>();
        public Vector2 editorPosition;
        public string comment = "";
        public List<SerializableParameter> parametersList = new List<SerializableParameter>();
        
        public string GetParameter(string key)
        {
            if (parametersList == null) return "";
            var param = parametersList.Find(p => p.key == key);
            return param != null ? param.value : "";
        }
        
        public void SetParameter(string key, string value)
        {
            if (parametersList == null)
            {
                parametersList = new List<SerializableParameter>();
            }
            
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
        
        public bool HasParameter(string key)
        {
            if (parametersList == null) return false;
            return parametersList.Exists(p => p.key == key);
        }
    }

    [CreateAssetMenu(fileName = "BehaviorTreeAsset", menuName = "BehaviorTree/Tree Asset")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        // 直接存储，不使用中间层包装
        public string treeName = "";
        public int rootId;
        public List<BehaviorNodeData> nodes = new List<BehaviorNodeData>();

        public BehaviorNodeData GetNode(int id)
        {
            if (nodes == null) return null;
            return nodes.Find(n => n.id == id);
        }

        public void AddNode(BehaviorNodeData node)
        {
            if (nodes == null)
            {
                nodes = new List<BehaviorNodeData>();
            }
            nodes.Add(node);
        }

        public void RemoveNode(int id)
        {
            if (nodes == null) return;
            nodes.RemoveAll(n => n.id == id);
        }
    }
}
