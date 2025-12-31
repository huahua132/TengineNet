using System;

namespace TEngine
{
    /// <summary>
    /// 定时器模块接口
    /// </summary>
    public interface ITimerModule
    {
        #region 添加定时器（无参数）
        
        /// <summary>
        /// 添加定时器（无参数版本）
        /// </summary>
        /// <param name="callback">计时器回调</param>
        /// <param name="time">计时器间隔时间（秒）</param>
        /// <param name="isLoop">是否循环执行</param>
        /// <param name="isUnscaled">是否不受时间缩放影响（使用真实时间）</param>
        /// <returns>计时器ID</returns>
        long AddTimer(Action<Params> callback, float time, bool isLoop = false, bool isUnscaled = false);
        
        #endregion
        
        #region 添加定时器（带参数）
        
        /// <summary>
        /// 添加定时器（1个参数）
        /// </summary>
        long AddTimer<T>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T arg);
        
        /// <summary>
        /// 添加定时器（2个参数）
        /// </summary>
        long AddTimer<T1, T2>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2);
        
        /// <summary>
        /// 添加定时器（3个参数）
        /// </summary>
        long AddTimer<T1, T2, T3>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2, T3 arg3);
        
        /// <summary>
        /// 添加定时器（4个参数）
        /// </summary>
        long AddTimer<T1, T2, T3, T4>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        
        /// <summary>
        /// 添加定时器（5个参数）
        /// </summary>
        long AddTimer<T1, T2, T3, T4, T5>(Action<Params> callback, float time, bool isLoop, bool isUnscaled, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        
        #endregion
        
        #region 定时器控制
        
        /// <summary>
        /// 暂停指定的定时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        void Stop(long timerId);
        
        /// <summary>
        /// 恢复指定的定时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        void Resume(long timerId);
        
        /// <summary>
        /// 检查定时器是否正在运行
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>是否在运行中</returns>
        bool IsRunning(long timerId);
        
        /// <summary>
        /// 获取定时器剩余时间
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>剩余时间（秒）</returns>
        float GetLeftTime(long timerId);
        
        /// <summary>
        /// 重启定时器（重置到初始时间并激活）
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        void Restart(long timerId);
        
        #endregion
        
        #region 移除定时器
        
        /// <summary>
        /// 移除指定的定时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        void RemoveTimer(long timerId);
        
        /// <summary>
        /// 移除所有定时器
        /// </summary>
        void RemoveAllTimer();
        
        #endregion
    }
}

