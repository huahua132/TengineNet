using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("SelectorNode",
    "+ 一直往下执行，只要有一个子节点返回成功就返回成功 + 子节点是或（OR）的关系",
    BehaviorProcessType.composite)]
    public class SelectorNode : BehaviorProcessNodeBase
    {
        private int lastIdx = 0;
        
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            if (_Node.IsResume())
            {
                var lastRet = _Context.GetLastRet();
                if (lastRet == BehaviorRet.SUCCESS)
                {
                    return lastRet;
                }
                else if (lastRet == BehaviorRet.FAIL)
                {
                    lastIdx++;
                }
                else
                {
                    Debug.LogError("状态错误");
                    return BehaviorRet.FAIL;
                }
            }
            else
            {
                lastIdx = 0;
            }

            for (int i = lastIdx; i < _Node.Childrens.Count; i++)
            {
                var child = _Node.Childrens[i];
                var r = child.TickRun();
                if (r == BehaviorRet.RUNNING)
                {
                    return _Node.Yield();
                }
                if (r == BehaviorRet.SUCCESS)
                {
                    return r;
                }
            }
            return BehaviorRet.FAIL;
        }
    }
}