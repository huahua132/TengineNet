using UnityEngine;

namespace BehaviorTree.Test1
{
    [BehaviorProcessNode("测试1行为", "Test1程序集专属的行为节点", BehaviorProcessType.action)]
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