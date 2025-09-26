using System;
namespace GameLogic
{
    public interface ISystemModule
    {
        // 获取指定类型的系统 T 建议规范为接口，保证system的封装性
        T GetSystem<T>() where T : class, ISystem;
        void AddEvent(int eventType, Action handler);

        void AddEvent<T>(int eventType, Action<T> handler);

        void AddEvent<T, U>(int eventType, Action<T, U> handler);

        void AddEvent<T, U, V>(int eventType, Action<T, U, V> handler);

        void AddEvent<T, U, V, W>(int eventType, Action<T, U, V, W> handler);
    }
}