using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 并行节点：同时执行所有子节点，全部成功才返回成功
    /// </summary>
    [BehaviorProcessNode("ParallelNode",
        "并行执行所有子节点，全部完成后返回成功",
        BehaviorProcessType.composite)]
    public class ParallelNode : BehaviorProcessNodeBase
    {
        private Dictionary<int, BehaviorRet> _childrenStatus = new();
        private int _runningCount = 0;
        
        public override void OnCreate()
        {
            _childrenStatus.Clear();
            _runningCount = 0;
        }

        public override void OnRemove()
        {
            _childrenStatus.Clear();
            _runningCount = 0;
        }

        public override BehaviorRet OnTickRun()
        {
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                return BehaviorRet.SUCCESS;
            }

            // 首次执行，初始化所有子节点状态
            if (!_Node.IsResume())
            {
                _childrenStatus.Clear();
                _runningCount = 0;
            }

            int successCount = 0;
            int failCount = 0;
            _runningCount = 0;

            // 执行所有子节点
            for (int i = 0; i < _Node.Childrens.Count; i++)
            {
                var child = _Node.Childrens[i];
                BehaviorRet ret;

                // 如果子节点已经完成，使用缓存的结果
                if (_childrenStatus.TryGetValue(child.ID, out var cachedRet))
                {
                    if (cachedRet == BehaviorRet.SUCCESS)
                    {
                        successCount++;
                        continue;
                    }
                    else if (cachedRet == BehaviorRet.FAIL)
                    {
                        failCount++;
                        continue;
                    }
                }

                // 执行子节点
                ret = child.TickRun();
                _childrenStatus[child.ID] = ret;

                switch (ret)
                {
                    case BehaviorRet.SUCCESS:
                        successCount++;
                        break;
                    case BehaviorRet.FAIL:
                        failCount++;
                        break;
                    case BehaviorRet.RUNNING:
                        _runningCount++;
                        break;
                    case BehaviorRet.ABORT:
                        return BehaviorRet.ABORT;
                }
            }

            // 如果有任何一个失败，返回失败
            if (failCount > 0)
            {
                _childrenStatus.Clear();
                return BehaviorRet.FAIL;
            }

            // 全部成功
            if (successCount == _Node.Childrens.Count)
            {
                _childrenStatus.Clear();
                return BehaviorRet.SUCCESS;
            }

            // 还有子节点在运行
            if (_runningCount > 0)
            {
                return _Node.Yield();
            }

            return BehaviorRet.SUCCESS;
        }
    }
}

