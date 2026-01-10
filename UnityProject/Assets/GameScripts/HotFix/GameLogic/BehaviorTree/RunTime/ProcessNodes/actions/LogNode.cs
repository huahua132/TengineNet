using System.Collections.Generic;
using UnityEngine;
using TEngine;


namespace BehaviorTree
{
    [BehaviorProcessNode("LogNode",
    "打印日志",
    BehaviorProcessType.composite)]
    public class LogNode : BehaviorProcessNodeBase
    {
        public override void OnCreate()
        {
            
        }
        public override void OnRemove()
        {
            
        }
        public override BehaviorRet OnTickRun()
        {
            Debug.Log("print log");
            return BehaviorRet.SUCCESS;
        }
    }
}