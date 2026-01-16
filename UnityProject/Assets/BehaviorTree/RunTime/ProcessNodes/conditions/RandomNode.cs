using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("Random",
    "随机条件节点，根据设定的成功概率返回成功或失败",
    BehaviorProcessType.condition)]
    public class RandomNode : BehaviorProcessNodeBase
    {
        [Tooltip("成功概率 (0.0 - 1.0)，例如0.5表示50%的概率返回成功")]
        [Range(0f, 1f)]
        public float successProbability = 0.5f;
        
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            // 生成0-1之间的随机数
            float randomValue = Random.value;
            
            // 如果随机值小于成功概率，返回成功，否则返回失败
            if (randomValue < successProbability)
            {
                return BehaviorRet.SUCCESS;
            }
            else
            {
                return BehaviorRet.FAIL;
            }
        }
    }
}
