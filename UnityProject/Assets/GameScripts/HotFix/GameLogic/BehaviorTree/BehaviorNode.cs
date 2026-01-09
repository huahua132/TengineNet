
using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{
    public interface IBehaviorNode
    {
        public int ID {get;}
        public List<IBehaviorNode> Childrens {get;}
    }

    //行为树结点
    public class BehaviorNode: IBehaviorNode, IMemory
    {
        public int ID {get; private set;}                                          //结点ID
        List<IBehaviorNode> IBehaviorNode.Childrens => Childrens;
        public List<IBehaviorNode> Childrens = new();                              //子节点
        public BehaviorProcessNodeBase ProcessNode {get; private set;}             //执行结点

        internal static BehaviorNode Create()
        {
            return MemoryPool.Acquire<BehaviorNode>();
        }

        internal static void Destory(BehaviorNode node)
        {
            MemoryPool.Release(node);
        }

        public void Clear()
        {
            ID = 0;
        }

        public void Init(int id, BehaviorProcessNodeBase processNode)
        {
            ID = id;
            ProcessNode = processNode;
        }

        public BehaviorRet TickRun(IBehaviorContext context)
        {
            if (context.IsAbort()) return BehaviorRet.RUNNING;

            
            return ProcessNode.TickRun(this, context);
        }

        public void AddChild(BehaviorNode node)
        {
            Childrens.Add(node);
        }
    }
}