using System.Collections.Generic;
using TEngine;

namespace BehaviorTree
{
    // 内部生命周期接口
    internal interface IBlackboardLifecycle
    {
        void Create();
    }
    //黑板基类
    public abstract class BlackboardBase:IBlackboardLifecycle, IMemory
    {
        void IMemory.Clear()
        {
            OnRelease();
        }

        void IBlackboardLifecycle.Create()
        {
            OnCreate();
        }

        protected abstract void OnCreate();
        protected abstract void OnRelease();
    }
}