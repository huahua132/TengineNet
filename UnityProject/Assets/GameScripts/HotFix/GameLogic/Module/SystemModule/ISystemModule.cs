namespace GameLogic
{
    public interface ISystemModule
    {
        // 手动注册系统（可选功能）
        void RegisterSystem<T>() where T : class, ISystem, new();
        // 获取指定类型的系统
        T GetSystem<T>() where T : class, ISystem;
    }
}