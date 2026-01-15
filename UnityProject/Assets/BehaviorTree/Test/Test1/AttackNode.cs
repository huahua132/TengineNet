using UnityEngine;

namespace BehaviorTree.Test1
{
    [BehaviorProcessNode("Attack", "攻击目标敌人", BehaviorProcessType.action)]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "从黑板读取攻击目标")]
    public class AttackNode : BehaviorProcessNodeBase
    {
        [Tooltip("攻击范围（单位）")]
        public float attackRange = 2f;
        
        [Tooltip("攻击伤害值")]
        public float attackDamage = 10f;
        
        [Tooltip("攻击冷却时间（秒）")]
        public float attackCooldown = 1f;
        
        private Transform _transform;
        private float _lastAttackTime;

        public override void OnCreate()
        {
            _transform = _Context.GetBindTransform();
            _lastAttackTime = -attackCooldown; // 初始可立即攻击
        }

        public override void OnRemove()
        {
            _transform = null;
        }

        public override BehaviorRet OnTickRun()
        {
            if (_transform == null)
            {
                Debug.LogWarning("[AttackNode] Transform is null");
                return BehaviorRet.FAIL;
            }
            
            // 从黑板获取目标
            var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
            if (blackboard == null || blackboard.target == null)
            {
                Debug.LogWarning("[AttackNode] 未找到攻击目标");
                return BehaviorRet.FAIL;
            }
            
            Transform target = blackboard.target;
            float distance = Vector3.Distance(_transform.position, target.position);
            
            // 检查是否在攻击范围内
            if (distance > attackRange)
            {
                Debug.Log($"[AttackNode] 目标超出攻击范围 距离: {distance:F2} 范围: {attackRange}");
                return BehaviorRet.FAIL;
            }
            
            // 检查攻击冷却
            float currentTime = Time.time;
            if (currentTime - _lastAttackTime < attackCooldown)
            {
                Debug.Log($"[AttackNode] 攻击冷却中 剩余: {(attackCooldown - (currentTime - _lastAttackTime)):F2}秒");
                return BehaviorRet.RUNNING;
            }
            
            // 执行攻击
            _lastAttackTime = currentTime;
            
            // 尝试对目标造成伤害
            var monster = target.GetComponent<Monster>();
            if (monster != null)
            {
                monster.TakeDamage(attackDamage);
                Debug.Log($"[AttackNode] 攻击目标 {target.name} 造成伤害: {attackDamage}");
            }
            
            return BehaviorRet.SUCCESS;
        }
    }
}