using UnityEngine;

namespace BehaviorTree.Test1
{
    [BehaviorProcessNode("Test1 Action", "Test1 assembly specific action node", BehaviorProcessType.action)]
    public class Test1ActionNode : BehaviorProcessNodeBase
    {
        public string actionName = "Test1 Action";

        public override void OnCreate()
        {
        }

        public override void OnRemove()
        {
        }

        public override BehaviorRet OnTickRun()
        {
            Debug.Log($"[Test1] Executing action: {actionName}");
            return BehaviorRet.SUCCESS;
        }
    }
}