using System;
using TEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    public interface INetProxy : IDisposable
    {
        #region 连接管理
        
        /// <summary>
        /// 设置连接
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="token">认证令牌</param>
        void SetConnect(string host, int port, long playerId, string token);
        
        /// <summary>
        /// 关闭连接
        /// </summary>
        void Close();

        #endregion

        #region 认证回调管理

        /// <summary>
        /// 注册认证成功回调
        /// </summary>
        /// <param name="callback">认证成功回调</param>
        void RegisterAuthSuccessCallback(AuthSuccessCallback callback);

        /// <summary>
        /// 注销认证成功回调
        /// </summary>
        /// <param name="callback">认证成功回调</param>
        void UnregisterAuthSuccessCallback(AuthSuccessCallback callback);

        /// <summary>
        /// 注册认证失败回调
        /// </summary>
        /// <param name="callback">认证失败回调</param>
        void RegisterAuthFailureCallback(AuthFailureCallback callback);

        /// <summary>
        /// 注销认证失败回调
        /// </summary>
        /// <param name="callback">认证失败回调</param>
        void UnregisterAuthFailureCallback(AuthFailureCallback callback);

        #endregion

        #region 消息监听管理

        /// <summary>
        /// 注册消息监听器
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">消息回调</param>
        void RegisterMessageListener(ushort packId, MessageCallback callback);

        /// <summary>
        /// 注销消息监听器
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">消息回调</param>
        void UnregisterMessageListener(ushort packId, MessageCallback callback);

        /// <summary>
        /// 注册消息监听器（泛型版本）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">消息回调</param>
        void RegisterMessageListener<T>(ushort packId, Action<T> callback) where T : class;

        #endregion

        #region 通用错误码处理器

        /// <summary>
        /// 注册通用错误码处理器
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="handler">处理器（参数为错误消息）</param>
        void RegisterCommonErrorHandler(int errorCode, Action<string> handler);

        /// <summary>
        /// 注册通用错误码处理器（无参数版本）
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="handler">处理器</param>
        void RegisterCommonErrorHandler(int errorCode, Action handler);

        /// <summary>
        /// 注销通用错误码处理器
        /// </summary>
        /// <param name="errorCode">错误码</param>
        void UnregisterCommonErrorHandler(int errorCode);

        /// <summary>
        /// 清空所有通用错误码处理器
        /// </summary>
        void ClearCommonErrorHandlers();

        /// <summary>
        /// 处理业务错误码（先检查链式处理器，再使用通用处理器）
        /// </summary>
        /// <param name="result">请求结果</param>
        void HandleServerError<T>(NetRequestResult<T> result) where T : class;

        #endregion

        #region 重复请求处理

        /// <summary>
        /// 注册全局重复请求回调
        /// </summary>
        /// <param name="callback">回调函数（参数为 packId）</param>
        void RegisterDuplicateRequestCallback(Action<ushort> callback);

        /// <summary>
        /// 注销全局重复请求回调
        /// </summary>
        void UnregisterDuplicateRequestCallback();

        #endregion

        #region 消息发送 - Result模式

        /// <summary>
        /// 发送消息（Send模式，不等待响应）
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>操作结果</returns>
        RpcResult SendMessage(INetRequest request, bool requireAuth = true);

        /// <summary>
        /// 发送ProtoBuf消息（Send模式）
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>操作结果</returns>
        RpcResult SendProtoBufMessage(ushort packId, ProtoBuf.IExtensible msgBody, bool requireAuth = true);

        /// <summary>
        /// 发送RPC请求（等待响应）- Result模式
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>RPC结果</returns>
        UniTask<RpcResult<INetResponse>> SendRpcRequest(INetRequest request, int timeoutMs = 10000, bool requireAuth = true);

        /// <summary>
        /// 发送ProtoBuf RPC请求 - Result模式
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>RPC结果</returns>
        UniTask<RpcResult<INetResponse>> SendProtoBufRpcRequest(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 10000, bool requireAuth = true);

        /// <summary>
        /// 发送ProtoBuf RPC请求（泛型版本）- Result模式
        /// </summary>
        /// <typeparam name="T">响应消息类型</typeparam>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>解析后的RPC结果</returns>
        UniTask<RpcResult<T>> SendProtoBufRpcRequest<T>(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 10000, bool requireAuth = true) where T : class;

        /// <summary>
        /// 发送RPC请求并自动重试 - Result模式
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="maxRetries">最大重试次数（默认3次）</param>
        /// <param name="retryDelayMs">重试延迟（毫秒，默认1000ms）</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>RPC结果</returns>
        UniTask<RpcResult<INetResponse>> SendRpcRequestWithRetry(
            INetRequest request,
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = true);

        /// <summary>
        /// 发送ProtoBuf RPC请求并自动重试 - Result模式
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="maxRetries">最大重试次数（默认3次）</param>
        /// <param name="retryDelayMs">重试延迟（毫秒，默认1000ms）</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>RPC结果</returns>
        UniTask<RpcResult<INetResponse>> SendProtoBufRpcRequestWithRetry(
            ushort packId,
            ProtoBuf.IExtensible msgBody,
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = true);

        /// <summary>
        /// 发送ProtoBuf RPC请求并自动重试（泛型版本）- Result模式
        /// </summary>
        /// <typeparam name="T">响应消息类型</typeparam>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="maxRetries">最大重试次数（默认3次）</param>
        /// <param name="retryDelayMs">重试延迟（毫秒，默认1000ms）</param>
        /// <param name="requireAuth">是否需要认证（默认true）</param>
        /// <returns>解析后的RPC结果</returns>
        UniTask<RpcResult<T>> SendProtoBufRpcRequestWithRetry<T>(
            ushort packId,
            ProtoBuf.IExtensible msgBody,
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = true) where T : class;

        #endregion

        #region 便捷的业务方法

        /// <summary>
        /// 发送请求并显示错误提示（支持链式调用，自动执行Complete）
        /// </summary>
        /// <typeparam name="T">响应消息类型</typeparam>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="showLoading">是否显示Loading（默认true）</param>
        /// <param name="preventDuplicate">是否防止重复请求（默认true）</param>
        /// <returns>支持链式调用的请求任务</returns>
        NetRequestTask<T> SendRequestWithUI<T>(
            ushort packId,
            ProtoBuf.IExtensible msgBody,
            bool showLoading = true,
            bool preventDuplicate = true,
            int timeoutMs = 10000) where T : class;

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        ConnectState GetConnectState();

        /// <summary>
        /// 是否已认证
        /// </summary>
        /// <returns>是否已认证</returns>
        bool IsAuthenticated();

        /// <summary>
        /// 是否已连接且已认证
        /// </summary>
        /// <returns>是否可用</returns>
        bool IsReady();

        #endregion
    }
}

