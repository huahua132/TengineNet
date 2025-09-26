using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TEngine;

namespace GameLogic
{
    // 系统接口，定义基础生命周期
    public interface ISystem
    {
        void OnInit();
        void OnStart();
        void OnDestroy();
    }

    // 可选的更新接口
    public interface ISystemUpdate
    {
        void OnUpdate(float elapseSeconds, float realElapseSeconds);
    }

    // 系统特性标注，用于标记子系统并设置优先级
    [AttributeUsage(AttributeTargets.Class)]
    public class SystemAttribute : Attribute
    {
        public int Priority { get; }
        
        public SystemAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }

    // 系统模块管理器
    public class SystemModule : TEngine.Module, IUpdateModule, ISystemModule
    {
        // 存储所有系统实例
        private readonly List<ISystem> _systems = new List<ISystem>();
        private readonly List<ISystemUpdate> _updateSystems = new List<ISystemUpdate>();

        // 系统是否已初始化和启动
        private bool _isInitialized = false;
        public override void OnInit()
        {
            if (_isInitialized) return;

            // 自动发现并注册所有标注了SystemAttribute的系统
            DiscoverAndRegisterSystems();

            // 初始化所有系统
            InitializeSystems();
            StartSystems();

            _isInitialized = true;
            Log.Info("SystemModule OnInit completed");
        }

        public override void Shutdown()
        {
            if (!_isInitialized) return;

            // 销毁所有系统（逆序）
            DestroySystems();

            _systems.Clear();
            _updateSystems.Clear();
            RemoveAllUIEvent();
            _isInitialized = false;
            Log.Info("SystemModule Shutdown completed");
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 更新所有实现了ISystemUpdate接口的系统
            for (int i = 0; i < _updateSystems.Count; i++)
            {
                try
                {
                    _updateSystems[i].OnUpdate(elapseSeconds, realElapseSeconds);
                }
                catch (Exception ex)
                {
                    Log.Error($"System update error: {ex.Message} {ex.StackTrace}");
                }
            }
        }

        // 自动发现并注册系统
        private void DiscoverAndRegisterSystems()
        {
            var systemInfos = new List<(Type type, int priority)>();

            // 获取当前程序集中所有标注了SystemAttribute的类型
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var systemAttr = type.GetCustomAttribute<SystemAttribute>();
                if (systemAttr != null && typeof(ISystem).IsAssignableFrom(type))
                {
                    systemInfos.Add((type, systemAttr.Priority));
                }
            }

            // 按优先级排序（优先级越高越先初始化）
            systemInfos.Sort((a, b) => b.priority.CompareTo(a.priority));

            // 创建系统实例
            foreach (var (type, priority) in systemInfos)
            {
                try
                {
                    var instance = GetOrCreateSingleton(type);
                    if (instance is ISystem system)
                    {
                        _systems.Add(system);

                        // 如果也实现了更新接口，添加到更新列表
                        if (instance is ISystemUpdate updateSystem)
                        {
                            _updateSystems.Add(updateSystem);
                            Log.Info($"Registered system _updateSystems Add: {type.Name} (Priority: {priority})");
                        }

                        Log.Info($"Registered system: {type.Name} (Priority: {priority})");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to create system {type.Name}: {ex.Message} {ex.StackTrace}");
                }
            }
        }

        // 获取或创建单例实例
        private object GetOrCreateSingleton(Type type)
        {
            // 尝试获取单例实例（通过Instance属性或字段）
            var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProperty != null)
            {
                var instance = instanceProperty.GetValue(null);
                if (instance != null) return instance;
            }

            var instanceField = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceField != null)
            {
                var instance = instanceField.GetValue(null);
                if (instance != null) return instance;
            }

            // 如果没有现成的单例，尝试创建新实例
            return Activator.CreateInstance(type);
        }

        // 初始化所有系统
        private void InitializeSystems()
        {
            foreach (var system in _systems)
            {
                try
                {
                    system.OnInit();
                    Log.Info($"Initialized system: {system.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to initialize system {system.GetType().Name}: {ex.Message} {ex.StackTrace}");
                }
            }
        }

        // 启动所有系统
        private void StartSystems()
        {
            foreach (var system in _systems)
            {
                try
                {
                    system.OnStart();
                    Log.Info($"Started system: {system.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to start system {system.GetType().Name}: {ex.Message} {ex.StackTrace}");
                }
            }
        }

        // 销毁所有系统（逆序）
        private void DestroySystems()
        {
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                try
                {
                    _systems[i].OnDestroy();
                    Log.Info($"Destroyed system: {_systems[i].GetType().Name}");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to destroy system {_systems[i].GetType().Name}: {ex.Message} {ex.StackTrace}");
                }
            }
        }

        // 获取指定类型的系统
        public T GetSystem<T>() where T : class, ISystem
        {
            return _systems.OfType<T>().FirstOrDefault();
        }
        
        #region Event

        private GameEventMgr _eventMgr;

        protected GameEventMgr EventMgr
        {
            get
            {
                if (_eventMgr == null)
                {
                    _eventMgr = MemoryPool.Acquire<GameEventMgr>();
                }

                return _eventMgr;
            }
        }

        public void AddEvent(int eventType, Action handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        public void AddEvent<T>(int eventType, Action<T> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        public void AddEvent<T, U>(int eventType, Action<T, U> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        public void AddEvent<T, U, V>(int eventType, Action<T, U, V> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        public void AddEvent<T, U, V, W>(int eventType, Action<T, U, V, W> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        private void RemoveAllUIEvent()
        {
            if (_eventMgr != null)
            {
                MemoryPool.Release(_eventMgr);
                _eventMgr = null;
            }
        }

        #endregion
    }
}