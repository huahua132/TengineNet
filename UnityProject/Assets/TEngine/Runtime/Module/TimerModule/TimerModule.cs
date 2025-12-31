using System;
using System.Collections.Generic;
using UnityEngine;

namespace TEngine
{
    public class Params
    {
        private readonly List<IParam> list = new(6);

        private static readonly Dictionary<Type, Stack<IParam>> paramPool = new();

        private Params() { }

        public bool CanRecycle = true;

        private static readonly Stack<Params> pool = new();
        interface IParam
        {
            object GetObject();
            void Reset();
        }
        protected class Param<T> : IParam
        {
            private T val;
            public Param(T param)
            {
                val = param;
            }

            public static Param<T> Pop(T value)
            {
                if (paramPool.TryGetValue(typeof(Param<T>), out var p))
                {
                    if (p.Count > 0)
                    {
                        var param = (Param<T>)p.Pop();
                        param.Set(value);
                        return param;
                    }
                }
                return new Param<T>(value);
            }

            public Param<T> Set(T param)
            {
                val = param;
                return this;
            }
            public T Get()
            {
                return val;
            }

            public void Reset()
            {
                val = default;
            }

            public object GetObject()
            {
                return val;
            }
        }
        public static Params Create()
        {
            if (pool.TryPop(out var p))
            {
                return p;
            }
            return new Params();
        }
        public static void Recycle(Params p)
        {
            if (!p.CanRecycle)
                return;
            for (int i = 0; i < p.list.Count; i++)
            {
                var param = p.list[i];
                var tp = param.GetType();
                if (paramPool.TryGetValue(tp, out var p2) == false)
                {
                    p2 = new Stack<IParam>();
                    paramPool.Add(tp, p2);
                }
                param.Reset();
                p2.Push(param);
            }

            p.list.Clear();
            pool.Push(p);
        }
        public Params AddParam<T>(T param)
        {
            list.Add(Param<T>.Pop(param));
            return this;
        }
        public T GetParam<T>(int index = 0)
        {
            return (list[index] as Param<T>).Get();
        }
        public object GetObject(int index = 0)
        {
            return list[index].GetObject();
        }
        public string GetString(int index = 0)
        {
            return GetParam<string>(index);
        }
        public Params AddString(string value)
        {
            return AddParam(value);
        }
        public bool GetBool(int index = 0)
        {
            return GetParam<bool>(index);
        }
        public Params AddBool(bool value)
        {
            return AddParam(value);
        }
        public uint GetUInt(int index = 0)
        {
            return GetParam<uint>(index);
        }
        public Params AddUInt(uint value)
        {
            return AddParam(value);
        }
        public int GetInt(int index = 0)
        {
            return GetParam<int>(index);
        }

        public Params AddInt(int value)
        {
            return AddParam(value);
        }
        public ulong GetULong(int index = 0)
        {
            return GetParam<ulong>(index);
        }
        public Params AddULongt(ulong value)
        {
            return AddParam(value);
        }
        public long GetLong(int index = 0)
        {
            return GetParam<long>(index);
        }
        public Params AddLong(long value)
        {
            return AddParam(value);
        }
        public float GetFloat(int index = 0)
        {
            return GetParam<float>(index);
        }
        public Params AddFloat(float value)
        {
            return AddParam(value);
        }
        public Vector3 GetVector3(int index = 0)
        {
            return GetParam<Vector3>(index);
        }
        public Params AddVector3(Vector3 value)
        {
            return AddParam(value);
        }

        public Vector2 GetVector2(int index = 0)
        {
            return GetParam<Vector2>(index);
        }
        public Params AddVector2(Vector2 value)
        {
            return AddParam(value);
        }

    }
    public class TimerModule :  Module, IUpdateModule, ITimerModule
    {
        #region 定时器节点定义
        
        private class TimerNode
        {
            public long timerId;
            public float curTime;
            public float intervalTime;
            public bool isLoop;
            public bool isUnscaled;
            public bool isActive;
            
            public Action<Params> callback;
            public Params args; // 缓存的参数，循环和非循环都复用
            
            public TimerNode next;
            public TimerNode prev;
            
            /// <summary>
            /// 执行回调
            /// </summary>
            public void Invoke()
            {
                if (callback == null) return;
                
                try
                {
                    callback(args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Timer {timerId} callback error: {e}");
                }
            }
            
            /// <summary>
            /// 重置节点
            /// </summary>
            public void Reset()
            {
                timerId = 0;
                curTime = 0;
                intervalTime = 0;
                isLoop = false;
                isUnscaled = false;
                isActive = false;
                callback = null;
                
                // 回收Params
                if (args != null)
                {
                    Params.Recycle(args);
                    args = null;
                }
                
                next = null;
                prev = null;
            }
        }
        
        #endregion
        
        #region 对象池
        
        private class ObjectPool<T> where T : class, new()
        {
            private readonly Stack<T> _pool;
            private readonly Action<T> _onReturn;
            
            public ObjectPool(int capacity, Action<T> onReturn = null)
            {
                _pool = new Stack<T>(capacity);
                _onReturn = onReturn;
                
                // 预分配对象
                for (int i = 0; i < capacity; i++)
                {
                    _pool.Push(new T());
                }
            }
            
            public T Get()
            {
                return _pool.Count > 0 ? _pool.Pop() : new T();
            }
            
            public void Return(T obj)
            {
                _onReturn?.Invoke(obj);
                _pool.Push(obj);
            }
            
            public void Clear() => _pool.Clear();
        }
        
        #endregion
        
        #region 字段定义
        
        private long _curTimerId;
        
        // TimerNode对象池
        private readonly ObjectPool<TimerNode> _nodePool;
        
        // 定时器链表（按时间排序）
        private TimerNode _scaledHead;
        private TimerNode _unscaledHead;
        
        // 快速索引字典
        private readonly Dictionary<long, TimerNode> _timerMap;
        
        // 待处理队列（用于在Update中添加/删除定时器）
        private readonly Queue<TimerNode> _pendingAdd;
        private readonly Queue<long> _pendingRemove;
        
        // 更新状态标记
        private bool _isUpdating;
        
        #endregion
        
        #region 构造函数
        
        public TimerModule()
        {
            _nodePool = new ObjectPool<TimerNode>(64, node => node.Reset());
            _timerMap = new Dictionary<long, TimerNode>(128);
            _pendingAdd = new Queue<TimerNode>(32);
            _pendingRemove = new Queue<long>(32);
        }
        
        #endregion
        
        #region 添加定时器（无参数）
        
        /// <summary>
        /// 添加定时器（无参数）
        /// </summary>
        public long AddTimer(Action<Params> callback, float time, bool isLoop = false, bool isUnscaled = false)
        {
            var node = _nodePool.Get();
            node.timerId = ++_curTimerId;
            node.curTime = time;
            node.intervalTime = time;
            node.callback = callback;
            node.args = null;
            node.isLoop = isLoop;
            node.isUnscaled = isUnscaled;
            node.isActive = true;
            
            AddNode(node);
            return node.timerId;
        }
        
        #endregion
        
        #region 添加定时器（1-5个参数）
        
        /// <summary>
        /// 添加定时器（1个参数）
        /// </summary>
        public long AddTimer<T>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T arg)
        {
            var node = _nodePool.Get();
            node.timerId = ++_curTimerId;
            node.curTime = time;
            node.intervalTime = time;
            node.callback = callback;
            node.args = Params.Create().AddParam(arg);
            node.isLoop = isLoop;
            node.isUnscaled = isUnscaled;
            node.isActive = true;
            
            AddNode(node);
            return node.timerId;
        }
        
        /// <summary>
        /// 添加定时器（2个参数）
        /// </summary>
        public long AddTimer<T1, T2>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2)
        {
            var node = _nodePool.Get();
            node.timerId = ++_curTimerId;
            node.curTime = time;
            node.intervalTime = time;
            node.callback = callback;
            node.args = Params.Create().AddParam(arg1).AddParam(arg2);
            node.isLoop = isLoop;
            node.isUnscaled = isUnscaled;
            node.isActive = true;
            
            AddNode(node);
            return node.timerId;
        }
        
        /// <summary>
        /// 添加定时器（3个参数）
        /// </summary>
        public long AddTimer<T1, T2, T3>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2, T3 arg3)
        {
            var node = _nodePool.Get();
            node.timerId = ++_curTimerId;
            node.curTime = time;
            node.intervalTime = time;
            node.callback = callback;
            node.args = Params.Create().AddParam(arg1).AddParam(arg2).AddParam(arg3);
            node.isLoop = isLoop;
            node.isUnscaled = isUnscaled;
            node.isActive = true;
            
            AddNode(node);
            return node.timerId;
        }
        
        /// <summary>
        /// 添加定时器（4个参数）
        /// </summary>
        public long AddTimer<T1, T2, T3, T4>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            var node = _nodePool.Get();
            node.timerId = ++_curTimerId;
            node.curTime = time;
            node.intervalTime = time;
            node.callback = callback;
            node.args = Params.Create().AddParam(arg1).AddParam(arg2).AddParam(arg3).AddParam(arg4);
            node.isLoop = isLoop;
            node.isUnscaled = isUnscaled;
            node.isActive = true;
            
            AddNode(node);
            return node.timerId;
        }
        
        /// <summary>
        /// 添加定时器（5个参数）
        /// </summary>
        public long AddTimer<T1, T2, T3, T4, T5>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            var node = _nodePool.Get();
            node.timerId = ++_curTimerId;
            node.curTime = time;
            node.intervalTime = time;
            node.callback = callback;
            node.args = Params.Create().AddParam(arg1).AddParam(arg2).AddParam(arg3).AddParam(arg4).AddParam(arg5);
            node.isLoop = isLoop;
            node.isUnscaled = isUnscaled;
            node.isActive = true;
            
            AddNode(node);
            return node.timerId;
        }
        
        #endregion
        
        #region 内部方法
        
        private void AddNode(TimerNode node)
        {
            if (_isUpdating)
            {
                _pendingAdd.Enqueue(node);
            }
            else
            {
                AddTimerInternal(node);
            }
        }
        
        private void AddTimerInternal(TimerNode node)
        {
            _timerMap[node.timerId] = node;
            
            if (node.isUnscaled)
            {
                InsertNode(ref _unscaledHead, node);
            }
            else
            {
                InsertNode(ref _scaledHead, node);
            }
        }
        
        /// <summary>
        /// 按剩余时间排序插入链表
        /// </summary>
        private void InsertNode(ref TimerNode head, TimerNode node)
        {
            if (head == null)
            {
                head = node;
                return;
            }
            
            // 插入到头部
            if (node.curTime < head.curTime)
            {
                node.next = head;
                head.prev = node;
                head = node;
                return;
            }
            
            // 查找插入位置
            TimerNode current = head;
            while (current.next != null && current.next.curTime <= node.curTime)
            {
                current = current.next;
            }
            
            // 插入节点
            node.next = current.next;
            node.prev = current;
            if (current.next != null)
            {
                current.next.prev = node;
            }
            current.next = node;
        }
        
        #endregion
        
        #region 移除定时器
        
        public void RemoveTimer(long timerId)
        {
            if (_isUpdating)
            {
                _pendingRemove.Enqueue(timerId);
            }
            else
            {
                RemoveTimerInternal(timerId);
            }
        }
        
        private void RemoveTimerInternal(long timerId)
        {
            if (!_timerMap.TryGetValue(timerId, out TimerNode node))
            {
                return;
            }
            
            _timerMap.Remove(timerId);
            
            // 从链表移除
            if (node.isUnscaled)
            {
                RemoveNode(ref _unscaledHead, node);
            }
            else
            {
                RemoveNode(ref _scaledHead, node);
            }
            
            // 回收到对象池（会自动回收Params）
            _nodePool.Return(node);
        }
        
        private void RemoveNode(ref TimerNode head, TimerNode node)
        {
            if (node == head)
            {
                head = node.next;
                if (head != null)
                {
                    head.prev = null;
                }
                return;
            }
            
            if (node.prev != null)
            {
                node.prev.next = node.next;
            }
            if (node.next != null)
            {
                node.next.prev = node.prev;
            }
        }
        
        #endregion
        
        #region 定时器控制
        
        public void Stop(long timerId)
        {
            if (_timerMap.TryGetValue(timerId, out TimerNode node))
            {
                node.isActive = false;
            }
        }
        
        public void Resume(long timerId)
        {
            if (_timerMap.TryGetValue(timerId, out TimerNode node))
            {
                node.isActive = true;
            }
        }
        
        public bool IsRunning(long timerId)
        {
            return _timerMap.TryGetValue(timerId, out TimerNode node) && node.isActive;
        }
        
        public float GetLeftTime(long timerId)
        {
            return _timerMap.TryGetValue(timerId, out TimerNode node) ? node.curTime : 0;
        }
        
        public void Restart(long timerId)
        {
            if (_timerMap.TryGetValue(timerId, out TimerNode node))
            {
                node.curTime = node.intervalTime;
                node.isActive = true;
            }
        }
        
        public void RemoveAllTimer()
        {
            foreach (var node in _timerMap.Values)
            {
                _nodePool.Return(node);
            }
            
            _timerMap.Clear();
            _scaledHead = null;
            _unscaledHead = null;
            _pendingAdd.Clear();
            _pendingRemove.Clear();
        }
        
        #endregion
        
        #region Update更新
        
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            _isUpdating = true;
            
            UpdateTimerList(ref _scaledHead, elapseSeconds);
            UpdateTimerList(ref _unscaledHead, realElapseSeconds);
            
            _isUpdating = false;
            
            // 处理待添加
            while (_pendingAdd.Count > 0)
            {
                AddTimerInternal(_pendingAdd.Dequeue());
            }
            
            // 处理待删除
            while (_pendingRemove.Count > 0)
            {
                RemoveTimerInternal(_pendingRemove.Dequeue());
            }
        }
        
        private void UpdateTimerList(ref TimerNode head, float deltaTime)
        {
            TimerNode current = head;
            
            while (current != null)
            {
                TimerNode next = current.next;
                
                if (current.isActive)
                {
                    current.curTime -= deltaTime;
                    
                    // 时间到达
                    while (current.curTime <= 0)
                    {
                        // 执行回调
                        current.Invoke();
                        
                        // 检查是否在回调中被删除
                        if (!_timerMap.ContainsKey(current.timerId))
                        {
                            break;
                        }
                        
                        if (current.isLoop)
                        {
                            current.curTime += current.intervalTime;
                            
                            // 防止死循环（间隔时间过小或为负数）
                            if (current.curTime <= 0 && current.intervalTime <= 0)
                            {
                                Debug.LogWarning($"Timer {current.timerId} has invalid interval: {current.intervalTime}");
                                RemoveTimer(current.timerId);
                                break;
                            }
                        }
                        else
                        {
                            // 非循环定时器，删除
                            RemoveTimer(current.timerId);
                            break;
                        }
                    }
                }
                
                current = next;
            }
        }
        
        #endregion
        
        #region 生命周期
        
        public override void OnInit()
        {
        }
        
        public override void Shutdown()
        {
            RemoveAllTimer();
            _nodePool.Clear();
        }
        
        #endregion
    }
}

