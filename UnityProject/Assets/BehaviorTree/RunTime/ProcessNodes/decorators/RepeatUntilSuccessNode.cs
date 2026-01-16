using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("RepeatUntilSuccess",
    "+ 只能有一个子节点，多个仅执行第一个\n+ 只有当子节点返回成功时，才返回成功，其它情况返回运行中状态\n+ 如果设定了尝试次数，超过指定次数则返回失败",
    BehaviorProcessType.decorator)]
    public class RepeatUntilSuccessNode : BehaviorProcessNodeBase
    {
        [Tooltip("最大循环次数，0表示无限制")]
        public int maxLoop = 0;
        
        private int _count = 0;
        
        public override void OnCreate()
        {
            _count = 0;
        }
        
        public override void OnRemove()
        {
            _count = 0;
        }
        
        public override BehaviorRet OnTickRun()
        {
            // 检查是否有子节点
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                Debug.LogWarning("RepeatUntilSuccess Node has no children");
                return BehaviorRet.FAIL;
            }
            
            int effectiveMaxLoop = maxLoop > 0 ? maxLoop : int.MaxValue;
            
            // 如果是恢复执行（Resume）
            if (_Node.IsResume())
            {
                var resumeRet = _Context.GetLastRet();
                
                // 如果子节点返回成功，整个节点返回成功
                if (resumeRet == BehaviorRet.SUCCESS)
                {
                    _count = 0;
                    return BehaviorRet.SUCCESS;
                }
                
                // 检查是否超过最大循环次数
                if (_count >= effectiveMaxLoop)
                {
                    _count = 0;
                    return BehaviorRet.FAIL;
                }
                
                // 继续尝试
                _count++;
            }
            else
            {
                _count = 1;
            }
            
            // 执行第一个子节点
            var child = _Node.Childrens[0];
            var r = child.TickRun();
            
            // 如果子节点返回成功，整个节点返回成功
            if (r == BehaviorRet.SUCCESS)
            {
                _count = 0;
                return BehaviorRet.SUCCESS;
            }
            
            // 其它情况（FAIL或RUNNING）返回挂起，继续等待下次尝试
            return _Node.Yield();
        }
    }
}
