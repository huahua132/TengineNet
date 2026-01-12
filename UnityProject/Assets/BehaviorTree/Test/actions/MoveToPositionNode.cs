using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("MoveToPositionNode",
    "移动绑定的Transform到目标位置（测试示例节点）",
    BehaviorProcessType.action)]
    public class MoveToPositionNode : BehaviorProcessNodeBase
    {
        // 可配置参数
        public float targetX = 0f;
        public float targetY = 0f;
        public float targetZ = 0f;
        public float speed = 5f;
        public float threshold = 0.1f; // 到达阈值
        
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            // 获取绑定的Transform
            Transform transform = _Context.GetBindTransform();
            if (transform == null)
            {
                Debug.LogError("[MoveToPositionNode] 没有绑定Transform对象！");
                return BehaviorRet.FAIL;
            }
            
            Vector3 targetPosition = new Vector3(targetX, targetY, targetZ);
            Vector3 currentPosition = transform.position;
            
            // 检查是否已经到达目标位置
            float distance = Vector3.Distance(currentPosition, targetPosition);
            if (distance <= threshold)
            {
                // 到达目标
                transform.position = targetPosition;
                return BehaviorRet.SUCCESS;
            }
            
            // 向目标移动
            Vector3 direction = (targetPosition - currentPosition).normalized;
            float moveStep = speed * Time.deltaTime;
            
            if (moveStep >= distance)
            {
                // 一次性到达
                transform.position = targetPosition;
                return BehaviorRet.SUCCESS;
            }
            else
            {
                // 移动一步，继续执行
                transform.position += direction * moveStep;
                return _Node.Yield();
            }
        }
    }
}