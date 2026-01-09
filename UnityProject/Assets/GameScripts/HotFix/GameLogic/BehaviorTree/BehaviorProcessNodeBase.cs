using System;

//执行结点接口
namespace BehaviorTree
{
    public enum BehaviorProcessType
    {
        composite,                      //复合结点
        decorator,                      //装饰结点
        condition,                      //条件结点
        action,                         //行为结点
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BehaviorProcessAttribute : Attribute
    {
        public string Name {get; private set;}                       //名称
        public string Desc {get; private set;}                       //描述
        public BehaviorProcessType Type {get; private set;}          //类型
        public BehaviorProcessAttribute(string name, string desc, BehaviorProcessType type)
        {
            Name = name;
            Desc = desc;
            Type = type;
        }
    }

    public abstract class BehaviorProcessNodeBase
    {
        public abstract BehaviorRet TickRun(IBehaviorNode node, IBehaviorContext context);
    }
}