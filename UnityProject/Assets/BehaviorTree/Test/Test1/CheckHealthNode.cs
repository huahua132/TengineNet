using UnityEngine;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 血量检查类型
    /// </summary>
    public enum HealthCheckType
    {
        Self,       // 检查自身血量
        Target      // 检查目标血量（从黑板获取）
    }
    
    /// <summary>
    /// 血量比较方式
    /// </summary>
    public enum HealthCompareType
    {
        LessThan,           // 小于
        LessThanOrEqual,    // 小于等于
        GreaterThan,        // 大于
        GreaterThanOrEqual  // 大于等于
    }
    
    [BehaviorProcessNode("Check Health", "检查血量是否满足条件", BehaviorProcessType.condition)]
    public class CheckHealthNode : BehaviorProcessNodeBase
    {
        [Tooltip("检查类型")]
        public HealthCheckType checkType = HealthCheckType.Self;
        
        [Tooltip("血量阈值（百分比，0-100）")]
        public float healthThreshold = 30f;
        
        [Tooltip("比较方式")]
        public HealthCompareType compareType = HealthCompareType.LessThan;
        
        private Transform _transform;

        public override void OnCreate()
        {
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
                Debug.LogWarning("[CheckHealthNode] Transform is null");
                return BehaviorRet.FAIL;
            }
            
            float currentHPPercent = 0f;
            string targetName = "";
            
            // 获取血量
            if (checkType == HealthCheckType.Self)
            {
                // 检查自身血量
                var hero = _transform.GetComponent<Hero>();
                var monster = _transform.GetComponent<Monster>();
                
                if (hero != null)
                {
                    currentHPPercent = (hero.currentHP / hero.maxHP) * 100f;
                    targetName = "自身(Hero)";
                }
                else if (monster != null)
                {
                    currentHPPercent = (monster.currentHP / monster.maxHP) * 100f;
                    targetName = "自身(Monster)";
                }
                else
                {
                    Debug.LogWarning("[CheckHealthNode] 自身没有Hero或Monster组件");
                    return BehaviorRet.FAIL;
                }
            }
            else // Target
            {
                // 检查目标血量
                var blackboard = _Node.GetBlackBoardData<TargetBlackboard>();
                if (blackboard == null || blackboard.target == null)
                {
                    Debug.LogWarning("[CheckHealthNode] 未找到目标");
                    return BehaviorRet.FAIL;
                }
                
                Transform target = blackboard.target;
                var hero = target.GetComponent<Hero>();
                var monster = target.GetComponent<Monster>();
                
                if (hero != null)
                {
                    currentHPPercent = (hero.currentHP / hero.maxHP) * 100f;
                    targetName = $"目标({target.name}/Hero)";
                }
                else if (monster != null)
                {
                    currentHPPercent = (monster.currentHP / monster.maxHP) * 100f;
                    targetName = $"目标({target.name}/Monster)";
                }
                else
                {
                    Debug.LogWarning($"[CheckHealthNode] 目标 {target.name} 没有Hero或Monster组件");
                    return BehaviorRet.FAIL;
                }
            }
            
            // 执行比较
            bool result = false;
            string compareSymbol = "";
            
            switch (compareType)
            {
                case HealthCompareType.LessThan:
                    result = currentHPPercent < healthThreshold;
                    compareSymbol = "<";
                    break;
                case HealthCompareType.LessThanOrEqual:
                    result = currentHPPercent <= healthThreshold;
                    compareSymbol = "<=";
                    break;
                case HealthCompareType.GreaterThan:
                    result = currentHPPercent > healthThreshold;
                    compareSymbol = ">";
                    break;
                case HealthCompareType.GreaterThanOrEqual:
                    result = currentHPPercent >= healthThreshold;
                    compareSymbol = ">=";
                    break;
            }
            
            Debug.Log($"[CheckHealthNode] {targetName} 血量: {currentHPPercent:F1}% {compareSymbol} {healthThreshold}% = {result}");
            
            return result ? BehaviorRet.SUCCESS : BehaviorRet.FAIL;
        }
    }
}