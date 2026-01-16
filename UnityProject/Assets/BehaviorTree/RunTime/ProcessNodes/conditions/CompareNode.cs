using UnityEngine;
using TEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 比较类型
    /// </summary>
    public enum CompareType
    {
        Int,       // 整数比较
        Uint,      // 无符号整数比较
        Long,      // 长整数比较
        Ulong,     // 无符号长整数比较
        Bool,      // 布尔比较
        String,    // 字符串比较
        Transform  // Transform比较（是否为null）
    }
    
    /// <summary>
    /// 比较操作符
    /// </summary>
    public enum CompareOperator
    {
        Equal,           // 等于 ==
        NotEqual,        // 不等于 !=
        Greater,         // 大于 > (仅用于int)
        GreaterOrEqual,  // 大于等于 >= (仅用于int)
        Less,            // 小于 < (仅用于int)
        LessOrEqual,     // 小于等于 <= (仅用于int)
        IsNull,          // 是否为空 (仅用于Transform和String)
        IsNotNull        // 是否不为空 (仅用于Transform和String)
    }
    
    [BehaviorProcessNode("Compare",
    "比较黑板结果值，支持int、uint、long、ulong、bool、string和Transform比较",
    BehaviorProcessType.condition)]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "ResultBlackboard", "intResult", "读取整数结果")]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "ResultBlackboard", "uintResult", "读取无符号整数结果")]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "ResultBlackboard", "longResult", "读取长整数结果")]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "ResultBlackboard", "ulongResult", "读取无符号长整数结果")]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "ResultBlackboard", "boolResult", "读取布尔结果")]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "ResultBlackboard", "stringResult", "读取字符串结果")]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "ResultBlackboard", "transformResult", "读取Transform结果")]
    public class CompareNode : BehaviorProcessNodeBase
    {
        [Tooltip("比较类型")]
        public CompareType compareType = CompareType.Int;
        
        [Tooltip("比较操作符")]
        public CompareOperator compareOperator = CompareOperator.Equal;
        
        [Header("比较值设置")]
        [Tooltip("整数比较值")]
        public int intValue = 0;
        
        [Tooltip("无符号整数比较值")]
        public uint uintValue = 0;
        
        [Tooltip("长整数比较值")]
        public long longValue = 0;
        
        [Tooltip("无符号长整数比较值")]
        public ulong ulongValue = 0;
        
        [Tooltip("布尔比较值")]
        public bool boolValue = false;
        
        [Tooltip("字符串比较值")]
        public string stringValue = "";
        
        [Tooltip("Transform比较值")]
        public Transform transformValue = null;
        
        public override void OnCreate()
        {
            
        }
        
        public override void OnRemove()
        {
            
        }
        
        public override BehaviorRet OnTickRun()
        {
            // 获取结果黑板
            var resultBoard = _Context.GetBlackBoardData<ResultBlackboard>();
            if (resultBoard == null)
            {
                Debug.LogWarning("CompareNode: ResultBlackboard not found");
                return BehaviorRet.FAIL;
            }
            
            bool result = false;
            
            // 根据比较类型执行不同的比较逻辑
            switch (compareType)
            {
                case CompareType.Int:
                    result = CompareInt(resultBoard.intResult, intValue);
                    break;
                    
                case CompareType.Uint:
                    result = CompareUint(resultBoard.uintResult, uintValue);
                    break;
                    
                case CompareType.Long:
                    result = CompareLong(resultBoard.longResult, longValue);
                    break;
                    
                case CompareType.Ulong:
                    result = CompareUlong(resultBoard.ulongResult, ulongValue);
                    break;
                    
                case CompareType.Bool:
                    result = CompareBool(resultBoard.boolResult, boolValue);
                    break;
                    
                case CompareType.String:
                    result = CompareString(resultBoard.stringResult, stringValue);
                    break;
                    
                case CompareType.Transform:
                    result = CompareTransform(resultBoard.transformResult, transformValue);
                    break;
            }
            
            return result ? BehaviorRet.SUCCESS : BehaviorRet.FAIL;
        }
        
        /// <summary>
        /// 整数比较
        /// </summary>
        private bool CompareInt(int a, int b)
        {
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    return a == b;
                case CompareOperator.NotEqual:
                    return a != b;
                case CompareOperator.Greater:
                    return a > b;
                case CompareOperator.GreaterOrEqual:
                    return a >= b;
                case CompareOperator.Less:
                    return a < b;
                case CompareOperator.LessOrEqual:
                    return a <= b;
                default:
                    Debug.LogWarning($"CompareNode: Operator {compareOperator} not supported for int comparison");
                    return false;
            }
        }
        
        /// <summary>
        /// 无符号整数比较
        /// </summary>
        private bool CompareUint(uint a, uint b)
        {
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    return a == b;
                case CompareOperator.NotEqual:
                    return a != b;
                case CompareOperator.Greater:
                    return a > b;
                case CompareOperator.GreaterOrEqual:
                    return a >= b;
                case CompareOperator.Less:
                    return a < b;
                case CompareOperator.LessOrEqual:
                    return a <= b;
                default:
                    Debug.LogWarning($"CompareNode: Operator {compareOperator} not supported for uint comparison");
                    return false;
            }
        }
        
        /// <summary>
        /// 长整数比较
        /// </summary>
        private bool CompareLong(long a, long b)
        {
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    return a == b;
                case CompareOperator.NotEqual:
                    return a != b;
                case CompareOperator.Greater:
                    return a > b;
                case CompareOperator.GreaterOrEqual:
                    return a >= b;
                case CompareOperator.Less:
                    return a < b;
                case CompareOperator.LessOrEqual:
                    return a <= b;
                default:
                    Debug.LogWarning($"CompareNode: Operator {compareOperator} not supported for long comparison");
                    return false;
            }
        }
        
        /// <summary>
        /// 无符号长整数比较
        /// </summary>
        private bool CompareUlong(ulong a, ulong b)
        {
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    return a == b;
                case CompareOperator.NotEqual:
                    return a != b;
                case CompareOperator.Greater:
                    return a > b;
                case CompareOperator.GreaterOrEqual:
                    return a >= b;
                case CompareOperator.Less:
                    return a < b;
                case CompareOperator.LessOrEqual:
                    return a <= b;
                default:
                    Debug.LogWarning($"CompareNode: Operator {compareOperator} not supported for ulong comparison");
                    return false;
            }
        }
        
        /// <summary>
        /// 布尔比较
        /// </summary>
        private bool CompareBool(bool a, bool b)
        {
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    return a == b;
                case CompareOperator.NotEqual:
                    return a != b;
                default:
                    Debug.LogWarning($"CompareNode: Operator {compareOperator} not supported for bool comparison");
                    return false;
            }
        }
        
        /// <summary>
        /// 字符串比较
        /// </summary>
        private bool CompareString(string a, string b)
        {
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    return a == b;
                case CompareOperator.NotEqual:
                    return a != b;
                case CompareOperator.IsNull:
                    return string.IsNullOrEmpty(a);
                case CompareOperator.IsNotNull:
                    return !string.IsNullOrEmpty(a);
                default:
                    Debug.LogWarning($"CompareNode: Operator {compareOperator} not supported for string comparison");
                    return false;
            }
        }
        
        /// <summary>
        /// Transform比较
        /// </summary>
        private bool CompareTransform(Transform a, Transform b)
        {
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    return a == b;
                case CompareOperator.NotEqual:
                    return a != b;
                case CompareOperator.IsNull:
                    return a == null;
                case CompareOperator.IsNotNull:
                    return a != null;
                default:
                    Debug.LogWarning($"CompareNode: Operator {compareOperator} not supported for Transform comparison");
                    return false;
            }
        }
    }
}
