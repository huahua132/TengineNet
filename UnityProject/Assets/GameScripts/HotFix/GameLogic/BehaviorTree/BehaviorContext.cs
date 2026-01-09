using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{
    public interface IBehaviorContext
    {
        public bool IsAbort();
        public void SetAbort();
        public int GetStackCount();
        public BehaviorNode GetStackPeekNode();
        public void PushStackNode(BehaviorNode node);
        public void PopStackNode();
    }

    //行为树上下文
    public class BehaviorContext: IBehaviorContext, IMemory
    {
        private bool _isAbort = false;                          //树是否中断执行
        private Stack<BehaviorNode> _runingStack;               //运行中的结点栈
        public void Clear()
        {
            _isAbort = false;
            _runingStack.Clear();
        }

        public void AbortDoing()
        {
            _isAbort = false;
            _runingStack.Clear();
        }

        public void SetAbort()
        {
            _isAbort = true;
        }
        public bool IsAbort()
        {
            return _isAbort;
        }

        public int GetStackCount()
        {
            return _runingStack.Count;
        }
        public BehaviorNode GetStackPeekNode()
        {
            return _runingStack.Peek();
        }
        public void PushStackNode(BehaviorNode node)
        {
            _runingStack.Push(node);
        }
        public void PopStackNode()
        {
            _runingStack.Pop();
        }
    }
}