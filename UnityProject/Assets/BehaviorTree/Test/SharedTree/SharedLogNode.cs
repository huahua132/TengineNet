using UnityEngine;

namespace BehaviorTree.SharedTree
{
    [BehaviorProcessNode("共享日志", "共享程序集中的日志节点", BehaviorProcessType.action)]
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