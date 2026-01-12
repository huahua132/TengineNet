using UnityEngine;
using BehaviorTree;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 目标黑板 - 用于存储行为树的目标对象
    /// </summary>
    public class TargetBlackboard : BlackboardBase
    {
        public Transform target;  // 目标Transform
        
        protected override void OnCreate()
        {
            target = null;
        }
        
        protected override void OnRelease()
        {
            target = null;
        }
    }
}