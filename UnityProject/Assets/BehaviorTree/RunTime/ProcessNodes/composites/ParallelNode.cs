using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 子节点执行状态数据（使用对象池重用）
    /// </summary>
    public class ChildNodeState : IMemory
    {
        public Stack<IBehaviorNode> nodeStack = new Stack<IBehaviorNode>();  // 子节点的执行栈
        public bool isCompleted = false;  // 是否已完成
        
        public void Clear()
        {
            nodeStack.Clear();
            isCompleted = false;
        }
    }
    
    /// <summary>
    /// 并行节点：同时执行所有子节点，全部成功才返回成功
    /// 支持正确的嵌套执行栈保存和恢复，使用对象池优化内存
    /// </summary>
    [BehaviorProcessNode("ParallelNode",
        "并行执行所有子节点，全部完成后返回成功",
        BehaviorProcessType.composite)]
    public class ParallelNode : BehaviorProcessNodeBase
    {
        private Dictionary<int, ChildNodeState> _childrenStates = new Dictionary<int, ChildNodeState>();
        
        public override void OnCreate()
        {
            _childrenStates.Clear();
        }

        public override void OnRemove()
        {
            // 释放所有子节点状态到对象池
            foreach (var state in _childrenStates.Values)
            {
                MemoryPool.Release(state);
            }
            _childrenStates.Clear();
        }

        public override BehaviorRet OnTickRun()
        {
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                return BehaviorRet.SUCCESS;
            }

            // 首次执行，初始化所有子节点状态（从对象池获取）
            if (!_Node.IsResume())
            {
                // 先释放旧状态
                foreach (var state in _childrenStates.Values)
                {
                    MemoryPool.Release(state);
                }
                _childrenStates.Clear();
                
                // 从对象池获取新状态
                for (int i = 0; i < _Node.Childrens.Count; i++)
                {
                    _childrenStates[i] = MemoryPool.Acquire<ChildNodeState>();
                }
            }

            int completedCount = 0;
            int currentStackLevel = _Context.GetStackCount();

            // 执行所有子节点
            for (int i = 0; i < _Node.Childrens.Count; i++)
            {
                var childState = _childrenStates[i];
                BehaviorRet status;

                // 子节点已完成，跳过
                if (childState.isCompleted)
                {
                    completedCount++;
                    continue;
                }

                var child = _Node.Childrens[i];

                // 如果有保存的执行栈，需要先恢复
                if (childState.nodeStack.Count > 0)
                {
                    // 从栈中恢复节点并执行
                    while (childState.nodeStack.Count > 0)
                    {
                        var savedNode = childState.nodeStack.Pop();
                        _Context.PushStackNode((BehaviorNode)savedNode);
                        status = savedNode.TickRun();

                        if (status == BehaviorRet.RUNNING)
                        {
                            // 还在运行中，保存当前执行栈
                            SaveExecutionStack(childState, currentStackLevel);
                            break;
                        }
                        
                        // 清理栈
                        while (_Context.GetStackCount() > currentStackLevel)
                        {
                            _Context.PopStackNode();
                        }
                    }

                    // 如果栈已清空，说明子树执行完成
                    if (childState.nodeStack.Count == 0)
                    {
                        childState.isCompleted = true;
                        completedCount++;
                    }
                }
                else
                {
                    // 首次执行该子节点
                    status = child.TickRun();

                    if (status == BehaviorRet.RUNNING)
                    {
                        // 子节点挂起，保存执行栈
                        SaveExecutionStack(childState, currentStackLevel);
                    }
                    else if (status == BehaviorRet.ABORT)
                    {
                        return BehaviorRet.ABORT;
                    }
                    else
                    {
                        // SUCCESS 或 FAIL，标记完成
                        childState.isCompleted = true;
                        completedCount++;
                    }
                }
            }

            // 所有子节点都完成了
            if (completedCount == _Node.Childrens.Count)
            {
                // 释放所有子节点状态到对象池
                foreach (var state in _childrenStates.Values)
                {
                    MemoryPool.Release(state);
                }
                _childrenStates.Clear();
                return BehaviorRet.SUCCESS;
            }

            // 还有子节点在运行，挂起当前节点
            return _Node.Yield();
        }

        /// <summary>
        /// 保存子节点的执行栈
        /// </summary>
        private void SaveExecutionStack(ChildNodeState childState, int baseStackLevel)
        {
            childState.nodeStack.Clear();
            
            // 从context栈中提取超出基准层级的节点
            var tempStack = new Stack<IBehaviorNode>();
            while (_Context.GetStackCount() > baseStackLevel)
            {
                tempStack.Push(_Context.GetStackPeekNode());
                _Context.PopStackNode();
            }
            
            // 反向存入childState的栈中（保持原有顺序）
            while (tempStack.Count > 0)
            {
                childState.nodeStack.Push(tempStack.Pop());
            }
        }
    }
}

