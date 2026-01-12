using UnityEngine;

namespace BehaviorTree.Test2
{
    [BehaviorProcessNode("测试2行为", "Test2程序集专属的行为节点", BehaviorProcessType.action)]
    public class Test2ActionNode : BehaviorProcessNodeBase
    {
        public string actionName = "Test2 Action";

        public override void OnCreate()
        {
        }

        public override void OnRemove()
        {
        }

        public override BehaviorRet OnTickRun()
        {
            Debug.Log($"[Test2] Executing action: {actionName}");
            return BehaviorRet.SUCCESS;
        }
    }
}