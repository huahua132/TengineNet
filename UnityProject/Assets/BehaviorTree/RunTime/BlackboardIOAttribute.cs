using System;

namespace BehaviorTree
{
    /// <summary>
    /// 黑板输入输出标记 - 用于描述节点对黑板数据的依赖关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class BlackboardIOAttribute : Attribute
    {
        /// <summary>
        /// IO类型
        /// </summary>
        public enum IOType
        {
            Read,   // 读取（输入）
            Write   // 写入（输出）
        }
        
        /// <summary>
        /// IO类型（读/写）
        /// </summary>
        public IOType Type { get; private set; }
        
        /// <summary>
        /// 黑板类型名称
        /// </summary>
        public string BlackboardTypeName { get; private set; }
        
        /// <summary>
        /// 访问的字段名称
        /// </summary>
        public string FieldName { get; private set; }
        
        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; private set; }
        
        /// <summary>
        /// 创建黑板IO标记
        /// </summary>
        /// <param name="type">IO类型（读/写）</param>
        /// <param name="blackboardTypeName">黑板类型名称（如"TargetBlackboard"）</param>
        /// <param name="fieldName">访问的字段名（如"target"）</param>
        /// <param name="description">描述信息</param>
        public BlackboardIOAttribute(IOType type, string blackboardTypeName, string fieldName, string description = "")
        {
            Type = type;
            BlackboardTypeName = blackboardTypeName;
            FieldName = fieldName;
            Description = description;
        }
        
        /// <summary>
        /// 获取完整的黑板字段路径
        /// </summary>
        public string GetFullPath()
        {
            return $"{BlackboardTypeName}.{FieldName}";
        }
        
        /// <summary>
        /// 获取显示文本
        /// </summary>
        public string GetDisplayText()
        {
            string typeIcon = Type == IOType.Read ? "📥" : "📤";
            string desc = string.IsNullOrEmpty(Description) ? "" : $" - {Description}";
            return $"{typeIcon} {GetFullPath()}{desc}";
        }
    }
}