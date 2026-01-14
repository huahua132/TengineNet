using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 子节点执行栈（使用对象池重用，零GC）
    /// 完全对应Lua版本的nodes表
    /// </summary>
    public class ChildExecutionStack : IMemory
    {
        // 使用List存储节点栈，对应Lua的nodes表
        public List<IBehaviorNode> nodes = new List<IBehaviorNode>();
        
        public void Clear()
        {
            nodes.Clear();
        }
        
        public int Count => nodes.Count;
    }
    
    /// <summary>
    /// 并行节点：同时执行所有子节点，全部成功才返回成功
    /// 完全按照Lua版本的behavior3.Parallel实现，保证零GC
    /// </summary>
    [BehaviorProcessNode("ParallelNode",
        "并行执行所有子节点，全部完成后返回成功",
        BehaviorProcessType.composite)]
    public class ParallelNode : BehaviorProcessNodeBase
    {
        // 子节点执行栈数组（对应Lua的last表）
        private ChildExecutionStack[] _childStacks = null;
        
        public override void OnCreate()
        {
            _childStacks = null;
        }

        public override void OnRemove()
        {
            // 释放所有执行栈到对象池
            if (_childStacks != null)
            {
                for (int i = 0; i < _childStacks.Length; i++)
                {
                    if (_childStacks[i] != null)
                    {
                        MemoryPool.Release(_childStacks[i]);
                    }
                }
                _childStacks = null;
            }
        }
        
        /// <summary>
        /// 首次执行时初始化（对应Lua的 last = last or {}）
        /// </summary>
        public override void OnStart()
        {
            int childCount = _Node.Childrens?.Count ?? 0;
            _childStacks = new ChildExecutionStack[childCount];
        }

        public override BehaviorRet OnTickRun()
        {
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                return BehaviorRet.SUCCESS;
            }

            int childCount = _Node.Childrens.Count;
            int currentStackLevel = _Context.GetStackCount();  // 对应Lua的level
            int completedCount = 0;  // 对应Lua的count

            // 遍历所有子节点（对应Lua的for i = 1, #node.children）
            for (int i = 0; i < childCount; i++)
            {
                BehaviorRet status;
                var child = _Node.Childrens[i];
                var childStack = _childStacks[i];  // 对应Lua的nodes = last[i]

                // 三种状态判断（完全对应Lua的三个if分支）
                if (childStack == null)
                {
                    // 首次执行该子节点（对应Lua的 if nodes == nil）
                    status = child.TickRun();
                }
                else if (childStack.Count > 0)
                {
                    // 有保存的执行栈，恢复执行（对应Lua的 elseif #nodes > 0）
                    status = BehaviorRet.SUCCESS;  // 默认值，如果栈清空则返回SUCCESS
                    
                    while (childStack.Count > 0)
                    {
                        // table.remove(nodes) - 从尾部移除
                        int lastIndex = childStack.nodes.Count - 1;
                        var savedNode = childStack.nodes[lastIndex];
                        childStack.nodes.RemoveAt(lastIndex);
                        
                        _Context.PushStackNode((BehaviorNode)savedNode);
                        status = savedNode.TickRun();

                        if (status == BehaviorRet.RUNNING)
                        {
                            // 保存执行栈到当前位置（对应Lua的table.insert(nodes, p, ...)）
                            int insertPos = childStack.Count;
                            while (_Context.GetStackCount() > currentStackLevel)
                            {
                                var node = _Context.GetStackPeekNode();
                                _Context.PopStackNode();
                                childStack.nodes.Insert(insertPos, node);
                            }
                            break;
                        }
                    }
                }
                else
                {
                    // childStack存在但为空，已完成（对应Lua的 else status = bret.SUCCESS）
                    status = BehaviorRet.SUCCESS;
                }

                // 处理子节点执行结果（对应Lua的结果处理）
                if (status == BehaviorRet.RUNNING)
                {
                    // 保存执行栈（对应Lua的 if status == bret.RUNNING）
                    if (childStack == null)
                    {
                        childStack = MemoryPool.Acquire<ChildExecutionStack>();
                        _childStacks[i] = childStack;
                    }
                    
                    // 插入到位置0（栈底）
                    while (_Context.GetStackCount() > currentStackLevel)
                    {
                        var node = _Context.GetStackPeekNode();
                        _Context.PopStackNode();
                        childStack.nodes.Insert(0, node);
                    }
                }
                else
                {
                    // 非RUNNING：清空栈，计数+1（对应Lua的 else nodes = {}; count = count + 1）
                    if (childStack == null)
                    {
                        childStack = MemoryPool.Acquire<ChildExecutionStack>();
                        _childStacks[i] = childStack;
                    }
                    else
                    {
                        childStack.Clear();
                    }
                    completedCount++;
                }
            }

            // 所有子节点完成（对应Lua的 if count == #node.children）
            if (completedCount == childCount)
            {
                // 释放栈对象，但保留数组供下次使用
                for (int i = 0; i < _childStacks.Length; i++)
                {
                    if (_childStacks[i] != null)
                    {
                        MemoryPool.Release(_childStacks[i]);
                        _childStacks[i] = null;
                    }
                }
                return BehaviorRet.SUCCESS;
            }

            // 还有子节点在运行（对应Lua的 return node:yield(env, last)）
            return _Node.Yield();
        }
    }
}
