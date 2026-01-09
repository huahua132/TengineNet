using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{   
    public enum BehaviorRet
    {
        SUCCESS,                           //成功
        FAIL,                              //失败
        RUNNING,                           //正在运行
        ABORT,                             //中断执行
    }
    //行为树
    public class BehaviorTree: IMemory
    {
        private BehaviorNode _root;                     //根结点
        private BehaviorContext _context;              //上下文
        
        public void Clear()
        {
            MemoryPool.Release(_context);
        }

        public void Init()
        {
            _context = MemoryPool.Acquire<BehaviorContext>();
        }
        
        public BehaviorRet TickRun()
        {
            BehaviorRet lastRet = BehaviorRet.SUCCESS;
            if (_context.GetStackCount() > 0)
            {
                BehaviorNode lastNode = _context.GetStackPeekNode();
                while (lastNode != null)
                {
                    lastRet = lastNode.TickRun(_context);
                    if (lastRet == BehaviorRet.RUNNING)
                    {
                        break;
                    }
                    lastNode = _context.GetStackPeekNode();
                }
            }
            else
            {
                lastRet = _root.TickRun(_context);
            }

            if (_context.IsAbort())
            {
                _context.AbortDoing();
                return BehaviorRet.ABORT;
            }
            
            return lastRet;
        }
    }
}