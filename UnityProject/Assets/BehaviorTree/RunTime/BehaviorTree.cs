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
        
        // 类型缓存，避免重复反射查找
        private static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static bool _typeCacheInitialized = false;

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
            node.Init(nodeData.id, processType, _context, nodeData);
            return node;
        }

        /// <summary>
        /// 获取处理节点类型（带缓存优化）
        /// </summary>
        private Type GetProcessNodeType(string typeName)
        {
            // 初始化类型缓存
            if (!_typeCacheInitialized)
            {
                InitializeTypeCache();
            }
            
            // 从缓存中查找
            if (_typeCache.TryGetValue(typeName, out Type cachedType))
            {
                return cachedType;
            }
            
            // 缓存中没有，尝试动态查找（用于运行时动态加载的类型）
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType($"BehaviorTree.{typeName}");
                if (type != null && typeof(BehaviorProcessNodeBase).IsAssignableFrom(type))
                {
                    // 找到后添加到缓存
                    _typeCache[typeName] = type;
                    return type;
                }
            }
            
            Debug.LogError($"ProcessNode type not found: {typeName}");
            return null;
        }
        
        /// <summary>
        /// 初始化类型缓存
        /// 一次性扫描所有程序集，建立类型名到Type的映射
        /// </summary>
        private static void InitializeTypeCache()
        {
            if (_typeCacheInitialized) return;
            
            _typeCache.Clear();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            int typeCount = 0;
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // 只缓存继承自 BehaviorProcessNodeBase 的类型
                        if (type.IsClass && !type.IsAbstract &&
                            typeof(BehaviorProcessNodeBase).IsAssignableFrom(type))
                        {
                            // 使用简单类名作为键
                            _typeCache[type.Name] = type;
                            typeCount++;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                }
            }
            
            _typeCacheInitialized = true;
            Debug.Log($"[BehaviorTree] Type cache initialized with {typeCount} node types");
        }
        
        /// <summary>
        /// 清理类型缓存（用于热重载等场景）
        /// </summary>
        public static void ClearTypeCache()
        {
            _typeCache.Clear();
            _typeCacheInitialized = false;
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