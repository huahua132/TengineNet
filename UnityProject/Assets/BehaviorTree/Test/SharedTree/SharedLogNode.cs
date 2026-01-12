using UnityEngine;

namespace BehaviorTree.SharedTree
{
    [BehaviorProcessNode("Shared Log", "Log node from shared assembly", BehaviorProcessType.action)]
    public class SharedLogNode : BehaviorProcessNodeBase
    {
        public string message = "SharedTree Log";

        public override void OnCreate()
        {
        }

        public override void OnRemove()
        {
        }

        public override BehaviorRet OnTickRun()
        {
            Debug.Log($"[SharedTree] {message}");
            return BehaviorRet.SUCCESS;
        }
    }
}