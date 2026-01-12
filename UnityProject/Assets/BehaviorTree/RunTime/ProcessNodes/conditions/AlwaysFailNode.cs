using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("AlwaysFailNode",
    "执行子节点，但总是返回失败（无论子节点返回什么）",
    BehaviorProcessType.decorator)]
    public class AlwaysFailNode : BehaviorProcessNodeBase
    {
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                Debug.LogWarning("AlwaysFailNode has no children");
                return BehaviorRet.FAIL;
            }

            // 如果是恢复执行，检查子节点结果
            if (_Node.IsResume())
            {
                var lastRet = _Context.GetLastRet();
                if (lastRet == BehaviorRet.RUNNING)
                {
                    return _Node.Yield();  // 子节点还在运行，继续等待
                }
                // 无论子节点返回什么（SUCCESS 或 FAIL），都返回 FAIL
                return BehaviorRet.FAIL;
            }

            // 首次执行子节点
            var child = _Node.Childrens[0];
            var ret = child.TickRun();

            if (ret == BehaviorRet.RUNNING)
            {
                return _Node.Yield();  // 子节点挂起，自己也挂起
            }

            // 无论子节点返回什么，都返回 FAIL
            return BehaviorRet.FAIL;
        }
    }
}