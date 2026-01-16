using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("Repeat",
    "+ 只能有一个子节点，多个仅执行第一个\n+ 当子节点返回「失败」时，退出遍历并返回「失败」状态\n+ 其它情况返回成功/正在运行",
    BehaviorProcessType.action)]
    public class RepeatNode : BehaviorProcessNodeBase
    {
        [Tooltip("重复次数")]
        public int count = 3;
        
        private int _lastIndex = 0;
        
        public override void OnCreate()
        {
            _lastIndex = 0;
        }
        
        public override void OnRemove()
        {
            _lastIndex = 0;
        }
        
        public override BehaviorRet OnTickRun()
        {
            // 检查是否有子节点
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                Debug.LogWarning("Repeat Node has no children");
                return BehaviorRet.FAIL;
            }
            
            // 如果是恢复执行（Resume）
            if (_Node.IsResume())
            {
                var resumeRet = _Context.GetLastRet();
                if (resumeRet == BehaviorRet.RUNNING)
                {
                    Debug.LogError($"Repeat Node #{_Node.ID}: unexpected status error - last return was RUNNING");
                    return BehaviorRet.FAIL;
                }
                else if (resumeRet == BehaviorRet.FAIL)
                {
                    _lastIndex = 0;
                    return BehaviorRet.FAIL;
                }
                _lastIndex++;
            }
            else
            {
                _lastIndex = 0;
            }
            
            // 循环执行子节点
            for (int i = _lastIndex; i < count; i++)
            {
                var child = _Node.Childrens[0];
                var r = child.TickRun();
                
                if (r == BehaviorRet.RUNNING)
                {
                    _lastIndex = i;
                    return _Node.Yield();
                }
                else if (r == BehaviorRet.FAIL)
                {
                    _lastIndex = 0;
                    return BehaviorRet.FAIL;
                }
            }
            
            _lastIndex = 0;
            return BehaviorRet.SUCCESS;
        }
    }
}
