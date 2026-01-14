using System.Collections.Generic;
using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("SequenceNode",
    "+ 一直往下执行，只有当所有子节点都返回成功, 才返回成功 + 子节点是与（AND）的关系",
    BehaviorProcessType.composite)]
    public class SequenceNode : BehaviorProcessNodeBase
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
                //Debug.Log($"SequenceNode[{_Node.ID}] {lastRet}");
                if (lastRet == BehaviorRet.FAIL)
                {
                    
                    return lastRet;
                }
                else if (lastRet == BehaviorRet.SUCCESS)
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

            //Debug.Log($"SequenceNode lastIdx[{_Node.ID}] {lastIdx}");

            for (int i = lastIdx; i < _Node.Childrens.Count; i++)
            {
                var child = _Node.Childrens[i];
                var r = child.TickRun();
                if (r == BehaviorRet.RUNNING)
                {
                    lastIdx = i;
                    return _Node.Yield();
                }
                if (r == BehaviorRet.FAIL)
                {
                    return r;
                }
            }
            return BehaviorRet.SUCCESS;
        }
    }
}