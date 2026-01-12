using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("OnceNode",
    "只执行子节点一次，后续调用返回第一次的结果",
    BehaviorProcessType.decorator)]
    public class OnceNode : BehaviorProcessNodeBase
    {
        private bool _hasExecuted = false;
        private BehaviorRet _cachedResult = BehaviorRet.SUCCESS;
        
        public override void OnCreate()
        {
            _hasExecuted = false;
            _cachedResult = BehaviorRet.SUCCESS;
        }
        
        public override void OnRemove()
        {
            _hasExecuted = false;
            _cachedResult = BehaviorRet.SUCCESS;
        }
        
        public override BehaviorRet OnTickRun()
        {
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                Debug.LogWarning("OnceNode has no children");
                return BehaviorRet.FAIL;
            }

            // 如果已经执行过，直接返回缓存的结果
            if (_hasExecuted)
            {
                return _cachedResult;
            }

            // 如果是恢复执行，检查子节点返回值
            if (_Node.IsResume())
            {
                var lastRet = _Context.GetLastRet();
                if (lastRet != BehaviorRet.RUNNING)
                {
                    // 子节点执行完成，缓存结果
                    _hasExecuted = true;
                    _cachedResult = lastRet;
                    return _cachedResult;
                }
            }

            // 首次执行或子节点仍在运行
            var child = _Node.Childrens[0];
            var ret = child.TickRun();

            if (ret == BehaviorRet.RUNNING)
            {
                return _Node.Yield();
            }

            // 子节点执行完成，缓存结果
            _hasExecuted = true;
            _cachedResult = ret;
            return _cachedResult;
        }
    }
}