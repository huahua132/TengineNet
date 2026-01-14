using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 等待模式
    /// </summary>
    public enum WaitMode
    {
        Time,      // 时间模式：等待指定的秒数
        RunCount   // 次数模式：返回RUNNING指定的次数
    }

    [BehaviorProcessNode("WaitNode",
    "等待指定的时间或次数",
    BehaviorProcessType.action)]
    public class WaitNode : BehaviorProcessNodeBase
    {
        // 公共字段，可以在编辑器中配置
        [Tooltip("等待模式：Time=时间模式, RunCount=次数模式")]
        public WaitMode mode = WaitMode.Time;
        
        [Tooltip("时间模式：等待的秒数")]
        public float time = 1.0f;  // 默认等待1秒
        
        [Tooltip("次数模式：返回RUNNING的次数")]
        public int count = 1;  // 默认返回RUNNING 1次
        
        private float _endTime = 0f;
        private int _runCount = 0;
        
        public override void OnCreate()
        {
            _endTime = 0f;
            _runCount = 0;
        }
        
        public override void OnRemove()
        {
            _endTime = 0f;
            _runCount = 0;
        }
        
        public override BehaviorRet OnTickRun()
        {
            if (mode == WaitMode.Time)
            {
                return WaitByTime();
            }
            else
            {
                return WaitByRunCount();
            }
        }
        
        /// <summary>
        /// 时间模式：等待指定的秒数
        /// </summary>
        private BehaviorRet WaitByTime()
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
            _endTime = Time.time + time;
            return _Node.Yield();
        }
        
        /// <summary>
        /// 次数模式：返回RUNNING指定的次数后返回SUCCESS
        /// </summary>
        private BehaviorRet WaitByRunCount()
        {
            // 如果是恢复执行（Resume），增加计数
            if (_Node.IsResume())
            {
                _runCount++;
            }
            else
            {
                // 首次执行，初始化计数
                _runCount = 0;
            }
            
            // 检查是否达到指定次数
            if (_runCount >= count)
            {
                // 完成等待
                _runCount = 0;
                return BehaviorRet.SUCCESS;
            }
            
            // 继续返回RUNNING
            return _Node.Yield();
        }
    }
}