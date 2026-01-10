using System;
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
        public void SetLastRet(BehaviorRet ret);
        public BehaviorRet GetLastRet();
        public T GetBlackBoardData<T>() where T : BlackboardBase;
    }

    public class BehaviorContext : IBehaviorContext, IMemory
    {
        private BehaviorRet _lastRet;
        private bool _isAbort = false;
        private Stack<BehaviorNode> _runingStack = new Stack<BehaviorNode>(); // 确保初始化
        private Dictionary<Type, BlackboardBase> _blackBoards = new Dictionary<Type, BlackboardBase>();

        public void Clear()
        {
            _isAbort = false;
            if (_runingStack != null)
            {
                _runingStack.Clear();
            }
            else
            {
                _runingStack = new Stack<BehaviorNode>();
            }
            
            if (_blackBoards != null)
            {
                foreach (var kv in _blackBoards)
                {
                    MemoryPool.Release(kv.Value);
                }
                _blackBoards.Clear();
            }
            else
            {
                _blackBoards = new Dictionary<Type, BlackboardBase>();
            }
        }

        public void AbortDoing()
        {
            _isAbort = false;
            if (_runingStack == null)
            {
                _runingStack = new Stack<BehaviorNode>();
            }
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
            return _runingStack?.Count ?? 0;
        }

        public BehaviorNode GetStackPeekNode()
        {
            if (_runingStack == null || _runingStack.Count == 0)
                return null;
            return _runingStack.Peek();
        }

        public void PushStackNode(BehaviorNode node)
        {
            if (_runingStack == null)
            {
                _runingStack = new Stack<BehaviorNode>();
            }
            _runingStack.Push(node);
        }

        public void PopStackNode()
        {
            if (_runingStack == null || _runingStack.Count == 0)
                return;
            _runingStack.Pop();
        }

        public void SetLastRet(BehaviorRet ret)
        {
            _lastRet = ret;
        }

        public BehaviorRet GetLastRet()
        {
            return _lastRet;
        }

        public T GetBlackBoardData<T>() where T : BlackboardBase
        {
            if (_blackBoards == null)
            {
                _blackBoards = new Dictionary<Type, BlackboardBase>();
            }

            Type type = typeof(T);
            if (_blackBoards.TryGetValue(type, out var blackboard))
            {
                return (T)blackboard;
            }

            var board = (T)MemoryPool.Acquire(type);
            board.Create();
            _blackBoards[type] = board;
            return board;
        }
    }
}

