using UnityEngine;

namespace BehaviorTree.Test2
{
    [BehaviorProcessNode("Test2 Action", "Test2 assembly specific action node", BehaviorProcessType.action)]
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