using System.Collections.Generic;
using UnityEngine;
using TEngine;
using System;

namespace BehaviorTree
{   
    public enum BehaviorRet
    {
        SUCCESS,                           //成功
        FAIL,                              //失败
        RUNNING,                           //正在运行
        ABORT,                             //中断执行
    }

    public class BehaviorTree : IMemory
    {
        private BehaviorNode _root;
        private BehaviorContext _context;
        private BehaviorTreeAsset _asset;
        private Dictionary<int, BehaviorNode> _nodeDict = new();
        
        // 运行时调试回调
        public System.Action<int, BehaviorRet> OnNodeStatusChanged;

        public void Clear()
        {
            if (_context != null)
            {
                MemoryPool.Release(_context);
                _context = null;
            }

            if (_root != null)
            {
                MemoryPool.Release(_root);
                _root = null;
            }

            _nodeDict.Clear();
            _asset = null;
        }

        public void Init()
        {
            _context = MemoryPool.Acquire<BehaviorContext>();
        }

        /// <summary>
        /// 从资产初始化行为树
        /// </summary>
        public bool InitFromAsset(BehaviorTreeAsset asset)
        {
            if (asset == null || asset.nodes == null || asset.nodes.Count == 0)
            {
                Debug.LogError("BehaviorTree InitFromAsset failed: asset is null or empty");
                return false;
            }

            _asset = asset;
            _nodeDict.Clear();

            // 第一步：创建所有节点
            foreach (var nodeData in asset.nodes)
            {
                var node = CreateNode(nodeData);
                if (node == null)
                {
                    Debug.LogError($"Create node failed: {nodeData.name}");
                    return false;
                }
                _nodeDict[nodeData.id] = node;
            }

            // 第二步：建立父子关系
            foreach (var nodeData in asset.nodes)
            {
                if (nodeData.childrenIds.Count > 0)
                {
                    var parentNode = _nodeDict[nodeData.id];
                    foreach (var childId in nodeData.childrenIds)
                    {
                        if (_nodeDict.TryGetValue(childId, out var childNode))
                        {
                            parentNode.AddChild(childNode);
                        }
                    }
                }
            }

            // 第三步：设置根节点
            if (_nodeDict.TryGetValue(asset.rootId, out var root))
            {
                _root = root;
                return true;
            }

            Debug.LogError("Root node not found");
            return false;
        }

        /// <summary>
        /// 创建节点
        /// </summary>
        private BehaviorNode CreateNode(BehaviorNodeData nodeData)
        {
            // 根据类型名获取类型
            Type processType = GetProcessNodeType(nodeData.processTypeName);
            if (processType == null)
            {
                Debug.LogError($"Process node type not found: {nodeData.processTypeName}");
                return null;
            }

            var node = MemoryPool.Acquire<BehaviorNode>();
            node.Init(nodeData.id, processType, _context);
            return node;
        }

        /// <summary>
        /// 获取处理节点类型
        /// </summary>
        private Type GetProcessNodeType(string typeName)
        {
            // 这里可以使用反射或预注册的类型字典
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType($"BehaviorTree.{typeName}");
                if (type != null && typeof(BehaviorProcessNodeBase).IsAssignableFrom(type))
                {
                    return type;
                }
            }
            return null;
        }

        public BehaviorRet TickRun()
        {
            if (_root == null)
            {
                Debug.LogError("BehaviorTree TickRun failed: root is null");
                return BehaviorRet.FAIL;
            }

            BehaviorRet lastRet = BehaviorRet.SUCCESS;
            if (_context.GetStackCount() > 0)
            {
                BehaviorNode lastNode = _context.GetStackPeekNode();
                while (lastNode != null)
                {
                    lastRet = lastNode.TickRun();
                    
                    // 报告节点状态
                    OnNodeStatusChanged?.Invoke(lastNode.ID, lastRet);
                    
                    if (lastRet == BehaviorRet.RUNNING)
                    {
                        break;
                    }
                    lastNode = _context.GetStackPeekNode();
                }
            }
            else
            {
                lastRet = _root.TickRun();
                
                // 报告根节点状态
                OnNodeStatusChanged?.Invoke(_root.ID, lastRet);
            }

            if (_context.IsAbort())
            {
                _context.AbortDoing();
                return BehaviorRet.ABORT;
            }

            return lastRet;
        }

        public BehaviorContext GetContext()
        {
            return _context;
        }
        
        public BehaviorTreeAsset GetAsset()
        {
            return _asset;
        }
        
        public Dictionary<int, BehaviorNode> GetNodeDict()
        {
            return _nodeDict;
        }
    }
}