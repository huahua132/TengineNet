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
}
