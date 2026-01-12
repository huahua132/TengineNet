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
        
        [Header("程序集配置")]
        [Tooltip("归属程序集名称（该树只能使用此程序集中的节点）")]
        public string ownerAssembly = "";
        
        [Tooltip("共享程序集列表（除归属程序集外，还可以使用这些程序集中的节点）")]
        public List<string> sharedAssemblies = new List<string>();

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
        
        /// <summary>
        /// 获取允许的程序集列表（归属程序集 + 共享程序集 + Runtime程序集）
        /// Runtime程序集始终包含在内
        /// </summary>
        public List<string> GetAllowedAssemblies()
        {
            var allowed = new List<string>();
            
            // 始终添加Runtime程序集（核心行为树库）
            allowed.Add("BehaviorTree.Runtime");
            
            // 添加归属程序集
            if (!string.IsNullOrEmpty(ownerAssembly))
            {
                if (!allowed.Contains(ownerAssembly))
                {
                    allowed.Add(ownerAssembly);
                }
            }
            
            // 添加共享程序集
            if (sharedAssemblies != null)
            {
                foreach (var assembly in sharedAssemblies)
                {
                    if (!string.IsNullOrEmpty(assembly) && !allowed.Contains(assembly))
                    {
                        allowed.Add(assembly);
                    }
                }
            }
            
            return allowed;
        }
        
        /// <summary>
        /// 检查是否允许使用指定程序集中的节点
        /// </summary>
        public bool IsAssemblyAllowed(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return true; // 空程序集名默认允许
            
            var allowed = GetAllowedAssemblies();
            return allowed.Contains(assemblyName);
        }
    }
}