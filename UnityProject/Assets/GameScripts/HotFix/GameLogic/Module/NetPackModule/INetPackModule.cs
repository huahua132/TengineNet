using TEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

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
        /// <param name="msgBodyHelper">消息体帮助器</param>
        /// <returns>节点ID</returns>
        uint Connect(uint nodeId, NetworkType networkType, string ip, int port, IMsgBodyHelper msgBodyHelper);

        /// <summary>
        /// 重连指定节点
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否成功开始重连</returns>
        bool Reconnect(uint nodeId);

        /// <summary>
        /// 主动断开指定连接
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        void Disconnect(uint nodeId);

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>连接状态</returns>
        ConnectState GetConnectState(uint nodeId);

        /// <summary>
        /// 检查节点ID是否存在
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否存在</returns>
        bool IsNodeExists(uint nodeId);

        /// <summary>
        /// 获取所有连接的节点ID
        /// </summary>
        /// <returns>节点ID列表</returns>
        List<uint> GetAllNodeIds();

        /// <summary>
        /// 关闭指定节点, 下一帧删除
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否成功标记关闭</returns>
        bool Close(uint nodeId);

        /// <summary>
        /// 获取重连失败次数，成功后次数会重置
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>重连尝试次数</returns>
        int GetReconnectAttempts(uint nodeId);

        #endregion

        #region 回调管理

        /// <summary>
        /// 注册连接成功回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">回调函数</param>
        void RegisterConnectCallback(uint nodeId, ConnectCallback callback);

        /// <summary>
        /// 注销连接成功回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">回调函数</param>
        void UnregisterConnectCallback(uint nodeId, ConnectCallback callback);

        /// <summary>
        /// 注册断线回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">回调函数</param>
        void RegisterDisconnectCallback(uint nodeId, DisconnectCallback callback);

        /// <summary>
        /// 注销断线回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">回调函数</param>
        void UnregisterDisconnectCallback(uint nodeId, DisconnectCallback callback);

        #endregion

        #region 消息处理功能

        /// <summary>
        /// 注册消息监听器
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">回调函数</param>
        void RegisterMessageListener(uint nodeId, ushort packId, MessageCallback callback);

        /// <summary>
        /// 注销消息监听器
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">回调函数</param>
        void UnregisterMessageListener(uint nodeId, ushort packId, MessageCallback callback);

        /// <summary>
        /// 发送消息（Send模式，不等待响应）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <param name="requireAuth">是否需要认证（默认false）</param>
        /// <returns>操作结果</returns>
        RpcResult SendMessage(uint nodeId, INetRequest request, bool requireAuth = false);

        /// <summary>
        /// 发送RPC请求（等待响应）- Result模式
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="requireAuth">是否需要认证（默认false）</param>
        /// <returns>RPC结果</returns>
        UniTask<RpcResult<INetResponse>> SendRpcRequest(uint nodeId, INetRequest request, int timeoutMs = 10000, bool requireAuth = false);

        /// <summary>
        /// 发送RPC请求并自动重试
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="maxRetries">最大重试次数（默认3次）</param>
        /// <param name="retryDelayMs">重试延迟（毫秒，默认1000ms）</param>
        /// <param name="requireAuth">是否需要认证（默认false）</param>
        /// <returns>RPC结果</returns>
        UniTask<RpcResult<INetResponse>> SendRpcRequestWithRetry(
            uint nodeId, 
            INetRequest request, 
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = false);

        #endregion

        #region 认证功能

        /// <summary>
        /// 设置节点的认证请求提供者
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="authRequestProvider">认证请求提供者</param>
        /// <param name="config">认证配置</param>
        void SetAuthRequestProvider(uint nodeId, AuthRequestProvider authRequestProvider, AuthConfig config = default);

        /// <summary>
        /// 注册认证成功回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">认证成功回调</param>
        void RegisterAuthSuccessCallback(uint nodeId, AuthSuccessCallback callback);

        /// <summary>
        /// 注销认证成功回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">认证成功回调</param>
        void UnregisterAuthSuccessCallback(uint nodeId, AuthSuccessCallback callback);

        /// <summary>
        /// 注册认证失败回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">认证失败回调</param>
        void RegisterAuthFailureCallback(uint nodeId, AuthFailureCallback callback);

        /// <summary>
        /// 注销认证失败回调
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="callback">认证失败回调</param>
        void UnregisterAuthFailureCallback(uint nodeId, AuthFailureCallback callback);

        /// <summary>
        /// 获取节点认证状态
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否已认证</returns>
        bool IsNodeAuthenticated(uint nodeId);

        #endregion

        #region 心跳功能

        /// <summary>
        /// 设置节点的心跳请求提供者
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="heartbeatRequestProvider">心跳请求提供者</param>
        /// <param name="config">心跳配置</param>
        void SetHeartbeatRequestProvider(uint nodeId, HeartbeatRequestProvider heartbeatRequestProvider, HeartbeatConfig config = default);

        /// <summary>
        /// 更新节点心跳配置
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="config">新的心跳配置</param>
        void UpdateHeartbeatConfig(uint nodeId, HeartbeatConfig config);

        /// <summary>
        /// 获取节点心跳配置
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>心跳配置</returns>
        HeartbeatConfig? GetHeartbeatConfig(uint nodeId);

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

