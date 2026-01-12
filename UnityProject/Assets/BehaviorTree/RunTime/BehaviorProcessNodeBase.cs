using System;
using TEngine;

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
    public class BehaviorProcessNodeAttribute : Attribute
    {
        public string Name {get; private set;}                       //名称
        public string Desc {get; private set;}                       //描述
        public BehaviorProcessType Type {get; private set;}          //类型
        
        public BehaviorProcessNodeAttribute(string name, string desc, BehaviorProcessType type)
        {
            Name = name;
            Desc = desc;
            Type = type;
        }
    }

    public abstract class BehaviorProcessNodeBase: IMemory
    {
        protected IBehaviorNode _Node;
        protected IBehaviorContext _Context;
        public void Clear()
        {
            OnRemove();
            _Node = null;
            _Context = null;
        }
        public void Create(IBehaviorNode node, IBehaviorContext context)
        {
            _Node = node;
            _Context = context;
            OnCreate();
        }
        public BehaviorRet TickRun()
        {
            return TickRun();
        }

        public abstract BehaviorRet OnTickRun();
        public abstract void OnCreate();
        public abstract void OnRemove();
    }
}