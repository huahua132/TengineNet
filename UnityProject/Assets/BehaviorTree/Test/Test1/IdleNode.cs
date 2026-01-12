using UnityEngine;

namespace BehaviorTree.Test1
{
    [BehaviorProcessNode("Idle", "Remain idle for a specified duration", BehaviorProcessType.action)]
    public class IdleNode : BehaviorProcessNodeBase
    {
        public float idleDuration = 2f; // 待机时长（秒）
        
        private float _startTime;
        private bool _isIdling;

        public override void OnCreate()
        {
            _isIdling = false;
        }

        public override void OnRemove()
        {
            _isIdling = false;
        }

        public override BehaviorRet OnTickRun()
        {
            // 首次执行，开始计时
            if (!_isIdling)
            {
                _startTime = Time.time;
                _isIdling = true;
                Debug.Log($"[IdleNode] 开始待机 时长: {idleDuration}秒");
                return BehaviorRet.RUNNING;
            }
            
            // 计算已经过去的时间
            float elapsedTime = Time.time - _startTime;
            
            // 检查是否完成待机
            if (elapsedTime >= idleDuration)
            {
                Debug.Log($"[IdleNode] 待机完成 总时长: {elapsedTime:F2}秒");
                _isIdling = false;
                return BehaviorRet.SUCCESS;
            }
            
            // 继续待机
            Debug.Log($"[IdleNode] 待机中... 已等待: {elapsedTime:F2}秒 / {idleDuration}秒");
            return BehaviorRet.RUNNING;
        }
    }
}