
using System.Collections.Generic;
using System;
using TEngine;

namespace BehaviorTree
{
    public interface IBehaviorNode
    {
        public int ID {get;}
        public List<IBehaviorNode> Childrens {get;}
        public T GetBlackBoardData<T>() where T : BlackboardBase;
        public BehaviorRet TickRun();
        public BehaviorRet Yield();
        public bool IsResume();
    }

    //行为树结点
    public class BehaviorNode: IBehaviorNode, IMemory
    {
        public int ID {get; private set;}                                          //结点ID
        List<IBehaviorNode> IBehaviorNode.Childrens => Childrens;
        public List<IBehaviorNode> Childrens = new();                              //子节点
        public BehaviorProcessNodeBase ProcessNode {get; private set;}             //执行结点
        private Dictionary<Type, BlackboardBase> _blackBoards = new();             //黑板
        private bool _isYield  = false;                                            //是否挂起
        private IBehaviorContext _context;

        public void Clear()
        {
            ID = 0;
            _isYield = false;
            if (ProcessNode != null)
            {
                MemoryPool.Release(ProcessNode);
                ProcessNode = null;
            }
            for (int i = Childrens.Count - 1; i >= 0; i--)
            {
                var node = Childrens[i];
                MemoryPool.Release((BehaviorNode)node);
            }
            Childrens.Clear();
            foreach (var kv in _blackBoards)
            {
                MemoryPool.Release(kv.Value);
            }
            _blackBoards.Clear();
        }

        public void Init(int id, Type processType, IBehaviorContext context)
        {
            ID = id;
            _context = context;
            ProcessNode = (BehaviorProcessNodeBase)MemoryPool.Acquire(processType);
            ProcessNode.Create(this, _context);
        }

        public BehaviorRet TickRun()
        {
            if (_context.IsAbort()) return BehaviorRet.RUNNING;

            if (!_isYield)
            {
                _context.PushStackNode(this);
            }
            
            var ret = ProcessNode.OnTickRun();
            if (ret == BehaviorRet.ABORT)
            {
                _context.SetAbort();
                return BehaviorRet.RUNNING;
            }

            if (ret != BehaviorRet.RUNNING)
            {
                _isYield = false;
                _context.PopStackNode();
            }
            else
            {
                _isYield = true;
            }

            _context.SetLastRet(ret);
            return ret;
        }

        public void AddChild(BehaviorNode node)
        {
            Childrens.Add(node);
        }

        public T GetBlackBoardData<T>() where T : BlackboardBase
        {
            Type type = typeof(T);
            if (_blackBoards.TryGetValue(type, out var blackboard))
            {
                return (T)blackboard;
            }

            var board = (T)MemoryPool.Acquire(type);
            board.Create();
            return board;
        }

        public BehaviorRet Yield()
        {
            _isYield = true;
            return BehaviorRet.RUNNING;
        }

        public bool IsResume()
        {
            return _isYield;
        }
    }
}