using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("AlwaysTrueNode",
    "总是返回成功的条件节点",
    BehaviorProcessType.condition)]
    public class AlwaysTrueNode : BehaviorProcessNodeBase
    {
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            return BehaviorRet.SUCCESS;
        }
    }
}