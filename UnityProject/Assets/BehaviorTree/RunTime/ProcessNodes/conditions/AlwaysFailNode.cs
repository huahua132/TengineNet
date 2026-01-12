using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("AlwaysFailNode",
    "总是返回失败的条件节点",
    BehaviorProcessType.condition)]
    public class AlwaysFailNode : BehaviorProcessNodeBase
    {
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            return BehaviorRet.FAIL;
        }
    }
}