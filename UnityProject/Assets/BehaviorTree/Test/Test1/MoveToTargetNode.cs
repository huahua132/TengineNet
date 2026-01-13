using UnityEngine;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 移动目标类型
    /// </summary>
    public enum MoveTargetType
    {
        BlackboardTarget,   // 从黑板获取目标对象
        FixedPosition       // 使用固定坐标点
    }
    
    [BehaviorProcessNode("Move To Target", "移动到目标位置（支持黑板目标或固定坐标）", BehaviorProcessType.action)]
    public class MoveToTargetNode : BehaviorProcessNodeBase
    {
        [Tooltip("目标类型")]
        public MoveTargetType targetType = MoveTargetType.BlackboardTarget;
        
        [Tooltip("固定目标位置的X坐标（当targetType=FixedPosition时使用）")]
        public float targetX = 0f;
        
        [Tooltip("固定目标位置的Y坐标（当targetType=FixedPosition时使用）")]
        public float targetY = 0f;
        
        [Tooltip("固定目标位置的Z坐标（当targetType=FixedPosition时使用）")]
        public float targetZ = 0f;
        
        [Tooltip("移动速度（单位/秒）")]
        public float speed = 5f;
        
        [Tooltip("到达目标的距离阈值")]
        public float arrivalThreshold = 0.5f;
        
        private Transform _transform;
        private Vector3 _fixedTargetPosition;

        public override void OnCreate()
        {
            _transform = _Context.GetBindTransform();
            _fixedTargetPosition = new Vector3(targetX, targetY, targetZ);
        }

        public override void OnRemove()
        {
            _transform = null;
        }

        public override BehaviorRet OnTickRun()
        {
            if (_transform == null)
            {
                Debug.LogWarning("[MoveToTargetNode] Transform is null");
                return BehaviorRet.FAIL;
            }
            
            Vector3 targetPosition;
            string targetName;
            
            // 根据类型获取目标位置
            if (targetType == MoveTargetType.BlackboardTarget)
            {
                // 从黑板获取目标
                var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
                if (blackboard == null || blackboard.target == null)
                {
                    Debug.LogWarning("[MoveToTargetNode] 未找到黑板目标");
                    return BehaviorRet.FAIL;
                }
                
                targetPosition = blackboard.target.position;
                targetName = blackboard.target.name;
            }
            else // FixedPosition
            {
                // 使用固定坐标
                targetPosition = _fixedTargetPosition;
                targetName = $"固定点({targetX:F1}, {targetY:F1}, {targetZ:F1})";
            }
            
            // 计算距离
            float distance = Vector3.Distance(_transform.position, targetPosition);
            
            // 已到达目标
            if (distance <= arrivalThreshold)
            {
                Debug.Log($"[MoveToTargetNode] 已到达目标 {targetName} 距离: {distance:F2}");
                return BehaviorRet.SUCCESS;
            }
            
            // 移动朝向目标
            Vector3 direction = (targetPosition - _transform.position).normalized;
            _transform.position += direction * speed * Time.deltaTime;
            
            Debug.Log($"[MoveToTargetNode] 正在移动到 {targetName} 距离: {distance:F2}");
            return BehaviorRet.RUNNING;
        }
    }
}