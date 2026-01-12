using System.Collections.Generic;
using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("IfElseNode",
    "+ 拥有三个子节点(至少两个) + 当第一个子节点返回SUCCESS的时候执行第二个子节点并返回此子节点的返回值 + 否则执行第三个子节点并返回这个节点的返回值,若无第三个子节点,则返回FAIL",
    BehaviorProcessType.composite)]
    public class IfElseNode : BehaviorProcessNodeBase
    {
        private int _lastIdx = 0;
        public override void OnCreate()
        {
            
        }
        public override void OnRemove()
        {
            
        }
        public override BehaviorRet OnTickRun()
        {
            if (_Node.Childrens.Count < 2)
            {
                Debug.LogError("at least two children");
                return BehaviorRet.FAIL;
            }
            var lastRet = _Context.GetLastRet();
            if (lastRet == BehaviorRet.RUNNING) return BehaviorRet.RUNNING;

            if (!_Node.IsResume())
            {
                _lastIdx = 0;
            } 
            else
            {
                if (_lastIdx == 0)
                {
                    return ifelse(lastRet);
                } else if (_lastIdx == 1 || _lastIdx == 2)
                {
                    return lastRet;
                }
            }
            
            var r = _Node.Childrens[0].TickRun();
            if (r == BehaviorRet.RUNNING)
            {
                _lastIdx = 0;
                return _Node.Yield();
            }
            return ifelse(r);
        }

        private BehaviorRet ifelse(BehaviorRet ret)
        {
            if (ret == BehaviorRet.RUNNING)
            {
                return ret;
            }
            if (ret == BehaviorRet.SUCCESS)
            {
                return childRet(1);
            }
            else if (_Node.Childrens.Count >= 3)
            {
                return childRet(2);
            }
            else
            {
                return BehaviorRet.FAIL;
            }
        }

        private BehaviorRet childRet(int idx)
        {
            var r = _Node.Childrens[idx].TickRun();
            if (r == BehaviorRet.RUNNING)
            {
                return _Node.Yield();
            }
            else
            {
                return r;
            }
        }
    }
}