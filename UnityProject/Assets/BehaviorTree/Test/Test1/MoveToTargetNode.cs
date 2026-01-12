using UnityEngine;

namespace BehaviorTree.Test1
{
    [BehaviorProcessNode("移动到目标", "移动到指定的目标位置", BehaviorProcessType.action)]
    public class MoveToTargetNode : BehaviorProcessNodeBase
    {
        public float targetX = 0f;
        public float targetY = 0f;
        public float targetZ = 0f;
        public float speed = 5f;
        
        private Vector3 _targetPosition;
        private Transform _transform;

        public override void OnCreate()
        {
            _targetPosition = new Vector3(targetX, targetY, targetZ);
            _transform = _Context.GetBindTransform();
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
            
            // 计算距离
            float distance = Vector3.Distance(_transform.position, _targetPosition);
            
            // 已到达目标
            if (distance < 0.1f)
            {
                Debug.Log($"[MoveToTargetNode] 已到达目标位置: {_targetPosition}");
                return BehaviorRet.SUCCESS;
            }
            
            // 移动朝向目标
            Vector3 direction = (_targetPosition - _transform.position).normalized;
            _transform.position += direction * speed * Time.deltaTime;
            
            Debug.Log($"[MoveToTargetNode] 移动中... 距离目标: {distance:F2}");
            return BehaviorRet.RUNNING;
        }
    }
}