using UnityEngine;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 战斗决策节点 - 演示多输入输出的用法
    /// 
    /// 这个节点展示了如何：
    /// 1. 从多个黑板读取数据（多输入）
    /// 2. 向多个黑板写入数据（多输出）
    /// 3. 混合读写操作
    /// </summary>
    [BehaviorProcessNode("Combat Decision", "战斗决策节点（多输入输出示例）", BehaviorProcessType.action)]
    // 📥 输入1: 读取当前目标
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "读取当前攻击目标")]
    // 📥 输入2: 读取自身状态（假设有状态黑板）
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "lastTarget", "读取上一个目标用于对比")]
    // 📤 输出1: 更新决策结果
    [BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "lastTarget", "保存当前目标作为历史")]
    // 📤 输出2: 写入是否应该攻击的决策
    [BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "shouldAttack", "写入是否应该发起攻击的决策")]
    public class CombatDecisionNode : BehaviorProcessNodeBase
    {
        [Tooltip("攻击距离阈值")]
        public float attackDistanceThreshold = 5f;
        
        [Tooltip("是否考虑目标切换")]
        public bool considerTargetSwitch = true;
        
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
                Debug.LogWarning("[CombatDecisionNode] Transform is null");
                return BehaviorRet.FAIL;
            }
            
            // 获取黑板
            var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
            if (blackboard == null)
            {
                Debug.LogWarning("[CombatDecisionNode] 黑板为空");
                return BehaviorRet.FAIL;
            }
            
            // === 📥 读取输入 ===
            Transform currentTarget = blackboard.target;
            Transform lastTarget = blackboard.lastTarget;
            
            Debug.Log($"[CombatDecisionNode] 📥 读取 - 当前目标: {currentTarget?.name ?? "无"}, 上次目标: {lastTarget?.name ?? "无"}");
            
            // 决策逻辑
            bool shouldAttack = false;
            
            if (currentTarget != null)
            {
                float distance = Vector3.Distance(_transform.position, currentTarget.position);
                shouldAttack = distance <= attackDistanceThreshold;
                
                // 检查是否切换了目标
                if (considerTargetSwitch && lastTarget != null && currentTarget != lastTarget)
                {
                    Debug.Log($"[CombatDecisionNode] 🔄 目标已切换: {lastTarget.name} -> {currentTarget.name}");
                }
                
                Debug.Log($"[CombatDecisionNode] 💭 决策 - 距离: {distance:F2}, 阈值: {attackDistanceThreshold}, 是否攻击: {shouldAttack}");
            }
            
            // === 📤 写入输出 ===
            // 输出1: 保存当前目标作为历史
            blackboard.lastTarget = currentTarget;
            
            // 输出2: 写入攻击决策（这里简化处理，实际应该用专门的决策黑板）
            blackboard.shouldAttack = shouldAttack;
            
            Debug.Log($"[CombatDecisionNode] 📤 写入 - lastTarget: {currentTarget?.name ?? "无"}, shouldAttack: {shouldAttack}");
            
            return shouldAttack ? BehaviorRet.SUCCESS : BehaviorRet.FAIL;
        }
    }
}