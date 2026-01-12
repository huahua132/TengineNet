using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{
    //黑板基类
    public abstract class BlackboardBase: IMemory
    {
        public void Clear()
        {
            OnRelease();
        }

        public void Create()
        {
            OnCreate();
        }

        protected abstract void OnCreate();
        protected abstract void OnRelease();
    }
}