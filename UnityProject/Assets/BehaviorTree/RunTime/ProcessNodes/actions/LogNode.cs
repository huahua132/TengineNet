using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("LogNode",
    "打印日志消息",
    BehaviorProcessType.action)]
    public class LogNode : BehaviorProcessNodeBase
    {
        public string message = "Log message";
        
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            Debug.Log($"[LogNode] {message}");
            return BehaviorRet.SUCCESS;
        }
    }
}