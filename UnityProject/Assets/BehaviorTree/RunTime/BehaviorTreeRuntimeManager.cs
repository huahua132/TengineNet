using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 行为树运行时管理器 - 追踪所有运行中的行为树实例
    /// </summary>
    public static class BehaviorTreeRuntimeManager
    {
        private static List<TreeInstance> _runningTrees = new List<TreeInstance>();
        private static int _nextInstanceId = 1;

        /// <summary>
        /// 行为树实例信息
        /// </summary>
        public class TreeInstance
        {
            public int InstanceId { get; set; }
            public string Name { get; set; }
            public Tree Tree { get; set; }
            public GameObject BoundGameObject { get; set; }
            public BehaviorTreeAsset Asset { get; set; }
            public float RegisterTime { get; set; }
            public int TickCount { get; set; }
            public BehaviorRet LastResult { get; set; }

            public string GetDisplayName()
            {
                if (BoundGameObject != null)
                    return $"{Name} ({BoundGameObject.name})";
                return Name;
            }
        }

        /// <summary>
        /// 注册行为树实例
        /// </summary>
        public static int Register(Tree tree, string name = null, GameObject boundGameObject = null)
        {
            if (tree == null) return -1;

            var instance = new TreeInstance
            {
                InstanceId = _nextInstanceId++,
                Name = name ?? "Behavior Tree",
                Tree = tree,
                BoundGameObject = boundGameObject,
                Asset = tree.GetAsset(),
                RegisterTime = Time.time,
                TickCount = 0,
                LastResult = BehaviorRet.SUCCESS
            };

            _runningTrees.Add(instance);
            return instance.InstanceId;
        }

        /// <summary>
        /// 注销行为树实例
        /// </summary>
        public static void Unregister(int instanceId)
        {
            _runningTrees.RemoveAll(t => t.InstanceId == instanceId);
        }

        /// <summary>
        /// 注销行为树实例
        /// </summary>
        public static void Unregister(Tree tree)
        {
            if (tree == null) return;
            _runningTrees.RemoveAll(t => t.Tree == tree);
        }

        /// <summary>
        /// 更新行为树执行信息
        /// </summary>
        public static void UpdateTreeTick(int instanceId, BehaviorRet result)
        {
            var instance = _runningTrees.Find(t => t.InstanceId == instanceId);
            if (instance != null)
            {
                instance.TickCount++;
                instance.LastResult = result;
            }
        }

        /// <summary>
        /// 获取所有运行中的行为树
        /// </summary>
        public static List<TreeInstance> GetRunningTrees()
        {
            // 清理已经销毁的实例
            _runningTrees.RemoveAll(t => 
                t.Tree == null || 
                (t.BoundGameObject != null && t.BoundGameObject == null)
            );
            
            return new List<TreeInstance>(_runningTrees);
        }

        /// <summary>
        /// 根据实例ID获取行为树
        /// </summary>
        public static TreeInstance GetTreeInstance(int instanceId)
        {
            return _runningTrees.Find(t => t.InstanceId == instanceId);
        }

        /// <summary>
        /// 清空所有注册的行为树
        /// </summary>
        public static void Clear()
        {
            _runningTrees.Clear();
            _nextInstanceId = 1;
        }

        /// <summary>
        /// 获取运行中的行为树数量
        /// </summary>
        public static int GetRunningTreeCount()
        {
            // 清理已经销毁的实例
            _runningTrees.RemoveAll(t => 
                t.Tree == null || 
                (t.BoundGameObject != null && t.BoundGameObject == null)
            );
            
            return _runningTrees.Count;
        }
    }
}