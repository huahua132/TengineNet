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

        #region 消息发送

        /// <summary>
        /// 发送消息（Send模式，不等待响应）
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <returns>是否发送成功</returns>
        bool SendMessage(INetRequest request);

        /// <summary>
        /// 发送ProtoBuf消息（Send模式）
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <returns>是否发送成功</returns>
        bool SendProtoBufMessage(ushort packId, ProtoBuf.IExtensible msgBody);

        /// <summary>
        /// 发送RPC请求（等待响应）
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>响应对象</returns>
        UniTask<INetResponse> SendRpcRequest(INetRequest request, int timeoutMs = 5000);

        /// <summary>
        /// 发送ProtoBuf RPC请求
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>响应对象</returns>
        UniTask<INetResponse> SendProtoBufRpcRequest(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 5000);

        /// <summary>
        /// 发送ProtoBuf RPC请求（泛型版本）
        /// </summary>
        /// <typeparam name="T">响应消息类型</typeparam>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>解析后的响应对象</returns>
        UniTask<T> SendProtoBufRpcRequest<T>(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 5000) where T : class;

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

