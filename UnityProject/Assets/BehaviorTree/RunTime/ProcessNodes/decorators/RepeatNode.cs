using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    [BehaviorProcessNode("RepeatNode",
    "重复执行子节点指定次数，全部成功返回成功",
    BehaviorProcessType.decorator)]
    public class RepeatNode : BehaviorProcessNodeBase
    {
        private int _repeatCount = 3; // 默认重复3次
        private int _currentCount = 0;
        
        public override void OnCreate()
        {
            _currentCount = 0;
        }
        
        public override void OnRemove()
        {
            _currentCount = 0;
        }
        
        public override BehaviorRet OnTickRun()
        {
            if (_Node.Childrens == null || _Node.Childrens.Count == 0)
            {
                Debug.LogWarning("RepeatNode has no children");
                return BehaviorRet.FAIL;
            }

            if (!_Node.IsResume())
            {
                _currentCount = 0;
            }

            while (_currentCount < _repeatCount)
            {
                var child = _Node.Childrens[0];
                var ret = child.TickRun();

                if (ret == BehaviorRet.RUNNING)
                {
                    return _Node.Yield();
                }

                if (ret == BehaviorRet.FAIL)
                {
                    _currentCount = 0;
                    return BehaviorRet.FAIL;
                }

                _currentCount++;
            }

            _currentCount = 0;
            return BehaviorRet.SUCCESS;
        }
    }
}