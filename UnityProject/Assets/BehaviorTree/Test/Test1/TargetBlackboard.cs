using UnityEngine;
using BehaviorTree;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 目标黑板 - 用于存储行为树的目标对象和决策信息
    /// </summary>
    public class TargetBlackboard : BlackboardBase
    {
        public Transform target;        // 当前目标Transform
        public Transform lastTarget;    // 上一个目标Transform（用于追踪目标切换）
        public bool shouldAttack;       // 是否应该攻击的决策标志
        
        protected override void OnCreate()
        {
            target = null;
            lastTarget = null;
            shouldAttack = false;
        }
        
        protected override void OnRelease()
        {
            target = null;
            lastTarget = null;
            shouldAttack = false;
        }
    }
}