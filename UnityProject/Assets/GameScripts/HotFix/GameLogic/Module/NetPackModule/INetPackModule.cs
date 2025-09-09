using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 网络包模块接口
    /// </summary>
    public interface INetPackModule
    {
        #region 连接管理
        
        /// <summary>
        /// 使用指定ID创建并连接到服务器
        /// </summary>
        /// <param name="nodeId">指定的节点ID</param>
        /// <param name="networkType">网络类型</param>
        /// <param name="ip">服务器IP</param>
        /// <param name="port">服务器端口</param>
        /// <returns>节点ID</returns>
        ulong Connect(ulong nodeId, NetworkType networkType, string ip, int port);

        /// <summary>
        /// 重连指定节点
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否成功开始重连</returns>
        bool Reconnect(ulong nodeId);

        /// <summary>
        /// 主动断开指定连接
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        void Disconnect(ulong nodeId);

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>连接状态</returns>
        ConnectState GetConnectState(ulong nodeId);

        /// <summary>
        /// 检查节点ID是否存在
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否存在</returns>
        bool IsNodeExists(ulong nodeId);

        /// <summary>
        /// 获取所有连接的节点ID
        /// </summary>
        /// <returns>节点ID列表</returns>
        List<ulong> GetAllNodeIds();

        #endregion

        #region 回调管理

        /// <summary>
        /// 注册连接成功回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        void RegisterConnectCallback(ConnectCallback callback);

        /// <summary>
        /// 注销连接成功回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        void UnregisterConnectCallback(ConnectCallback callback);

        /// <summary>
        /// 注册断线回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        void RegisterDisconnectCallback(DisconnectCallback callback);

        /// <summary>
        /// 注销断线回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        void UnregisterDisconnectCallback(DisconnectCallback callback);

        /// <summary>
        /// 关闭指定节点, 下一帧删除
        /// </summary>
        bool Close(ulong nodeId);

        /// <summary
        /// 获取重连失败次数，成功后次数会重置
        /// </summary> 
        int GetReconnectAttempts(ulong nodeId);

        #endregion

        #region 调试信息

        /// <summary>
        /// 获取所有连接信息
        /// </summary>
        /// <returns>连接信息字符串</returns>
        string GetConnectionsInfo();

        #endregion
    }
}

