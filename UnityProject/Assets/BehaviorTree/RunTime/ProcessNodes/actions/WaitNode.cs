using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("WaitNode",
    "等待指定的时间（秒）",
    BehaviorProcessType.action)]
    public class WaitNode : BehaviorProcessNodeBase
    {
        // 公共字段，可以在编辑器中配置
        public float duration = 1.0f;  // 默认等待1秒
        
        private float _endTime = 0f;
        
        public override void OnCreate()
        {
            _endTime = 0f;
        }
        
        public override void OnRemove()
        {
            _endTime = 0f;
        }
        
        public override BehaviorRet OnTickRun()
        {
            // 如果是恢复执行（Resume），检查是否已经到达结束时间
            if (_Node.IsResume())
            {
                if (Time.time >= _endTime)
                {
                    // 等待完成
                    return BehaviorRet.SUCCESS;
                }
                else
                {
                    // 继续等待
                    return _Node.Yield();
                }
            }
            
            // 首次执行，记录结束时间并挂起
            _endTime = Time.time + duration;
            return _Node.Yield();
        }
    }
}