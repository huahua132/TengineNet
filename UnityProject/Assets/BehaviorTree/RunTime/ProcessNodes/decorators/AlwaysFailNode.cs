using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("AlwaysFail",
    "+ 只能有一个子节点，多个仅执行第一个\n+ 不管子节点是否成功都返回失败",
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
            // 如果是恢复执行（Resume）
            if (_Node.IsResume())
            {
                var lastRet = _Context.GetLastRet();
                if (lastRet == BehaviorRet.RUNNING)
                {
                    Debug.LogError($"AlwaysFail Node #{_Node.ID}: unexpected status error - last return was RUNNING");
                    return BehaviorRet.FAIL;
                }
                // 不管子节点返回什么，都返回失败
                return BehaviorRet.FAIL;
            }

            // 获取第一个子节点
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                return BehaviorRet.FAIL;
            }
            
            var child = _Node.Childrens[0];
            var r = child.TickRun();
            
            // 如果子节点返回RUNNING，挂起等待
            if (r == BehaviorRet.RUNNING)
            {
                return _Node.Yield();
            }
            
            // 不管子节点成功还是失败，都返回失败
            return BehaviorRet.FAIL;
        }
    }
}
