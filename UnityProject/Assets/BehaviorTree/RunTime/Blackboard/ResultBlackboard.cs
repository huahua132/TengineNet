using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 结果黑板 - 用于存储节点执行结果
    /// </summary>
    public class ResultBlackboard : BlackboardBase
    {
        // 整数结果
        public int intResult = 0;
        
        // 无符号整数结果
        public uint uintResult = 0;
        
        // 长整数结果
        public long longResult = 0;
        
        // 无符号长整数结果
        public ulong ulongResult = 0;
        
        // 布尔结果
        public bool boolResult = false;
        
        // 字符串结果
        public string stringResult = "";
        
        // Transform结果
        public Transform transformResult = null;
        
        protected override void OnCreate()
        {
            intResult = 0;
            uintResult = 0;
            longResult = 0;
            ulongResult = 0;
            boolResult = false;
            stringResult = "";
            transformResult = null;
        }
        
        protected override void OnRelease()
        {
            intResult = 0;
            uintResult = 0;
            longResult = 0;
            ulongResult = 0;
            boolResult = false;
            stringResult = "";
            transformResult = null;
        }
        
        /// <summary>
        /// 设置整数结果
        /// </summary>
        public void SetIntResult(int value)
        {
            intResult = value;
        }
        
        /// <summary>
        /// 设置无符号整数结果
        /// </summary>
        public void SetUintResult(uint value)
        {
            uintResult = value;
        }
        
        /// <summary>
        /// 设置长整数结果
        /// </summary>
        public void SetLongResult(long value)
        {
            longResult = value;
        }
        
        /// <summary>
        /// 设置无符号长整数结果
        /// </summary>
        public void SetUlongResult(ulong value)
        {
            ulongResult = value;
        }
        
        /// <summary>
        /// 设置布尔结果
        /// </summary>
        public void SetBoolResult(bool value)
        {
            boolResult = value;
        }
        
        /// <summary>
        /// 设置字符串结果
        /// </summary>
        public void SetStringResult(string value)
        {
            stringResult = value;
        }
        
        /// <summary>
        /// 设置Transform结果
        /// </summary>
        public void SetTransformResult(Transform value)
        {
            transformResult = value;
        }
    }
}
