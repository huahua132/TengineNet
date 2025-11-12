using System;
using System.Collections.Generic;
using TEngine;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace GameLogic
{
    #region 枚举定义

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectState
    {
        Disconnected,   // 断开连接
        Connecting,     // 连接中
        Connected,      // 已连接
        Reconnecting    // 重连中
    }

    /// <summary>
    /// 断线类型
    /// </summary>
    public enum DisconnectType
    {
        Active,     // 主动断开
        Passive     // 被动断线
    }

    /// <summary>
    /// RPC错误类型
    /// </summary>
    public enum RpcErrorType
    {
        None = 0,               // 无错误
        NodeNotFound = 1,       // 节点不存在
        NotConnected = 2,       // 未连接
        Timeout = 3,            // 超时
        NetworkError = 4,       // 网络错误
        ServerError = 5,        // 服务器错误
        NodeClosed = 6,         // 节点已关闭
        NotAuthenticated = 7,   // 未认证
        RequestFailed = 8,      // 请求失败
        Unknown = 99            // 未知错误
    }

    #endregion

    #region 委托定义

    /// <summary>
    /// 连接回调委托
    /// </summary>
    public delegate void ConnectCallback(bool success, string errorMsg = "");
    
    /// <summary>
    /// 断线回调委托
    /// </summary>
    public delegate void DisconnectCallback(DisconnectType disconnectType, string reason = "");
    
    /// <summary>
    /// 消息监听回调委托
    /// </summary>
    public delegate void MessageCallback(INetResponse response);

    /// <summary>
    /// 认证函数委托 - 返回认证请求对象
    /// </summary>
    public delegate INetRequest AuthRequestProvider(uint nodeId);
    
    /// <summary>
    /// 心跳函数委托 - 返回心跳请求对象
    /// </summary>
    public delegate INetRequest HeartbeatRequestProvider(uint nodeId);
    
    /// <summary>
    /// 认证成功回调委托
    /// </summary>
    public delegate void AuthSuccessCallback(RpcResult<INetResponse> result);

    /// <summary>
    /// 认证失败回调委托
    /// </summary>
    public delegate void AuthFailureCallback(RpcResult<INetResponse> result);

    #endregion

    #region 配置结构

    /// <summary>
    /// 心跳配置
    /// </summary>
    public struct HeartbeatConfig
    {
        /// <summary>
        /// 心跳间隔时间（秒）
        /// </summary>
        public float IntervalSeconds;
        
        /// <summary>
        /// 是否启用心跳
        /// </summary>
        public bool Enabled;
        
        /// <summary>
        /// 心跳超时时间（毫秒）
        /// </summary>
        public int TimeoutMs;
        
        /// <summary>
        /// 心跳失败重试次数
        /// </summary>
        public int MaxRetryCount;

        public HeartbeatConfig(float intervalSeconds = 30f, bool enabled = true, int timeoutMs = 10000, int maxRetryCount = 3)
        {
            IntervalSeconds = intervalSeconds;
            Enabled = enabled;
            TimeoutMs = timeoutMs;
            MaxRetryCount = maxRetryCount;
        }
    }

    /// <summary>
    /// 认证配置
    /// </summary>
    public struct AuthConfig
    {
        /// <summary>
        /// 认证超时时间（毫秒）
        /// </summary>
        public int TimeoutMs;
        
        /// <summary>
        /// 认证失败重试次数
        /// </summary>
        public int MaxRetryCount;
        
        /// <summary>
        /// 重试间隔时间（毫秒）
        /// </summary>
        public int RetryIntervalMs;

        public AuthConfig(int timeoutMs = 10000, int maxRetryCount = 3, int retryIntervalMs = 3000)
        {
            TimeoutMs = timeoutMs;
            MaxRetryCount = maxRetryCount;
            RetryIntervalMs = retryIntervalMs;
        }
    }

    #endregion

#region Result结果类

/// <summary>
/// RPC结果包装类
/// </summary>
public class RpcResult<T> where T : class
{
    public bool IsSuccess { get; protected set; }  // 改成 protected
    public T Data { get; protected set; }
    public RpcErrorType ErrorType { get; protected set; }
    public string ErrorMsg { get; protected set; }
    public int ErrorCode { get; protected set; }
    public uint NodeId { get; protected set; }
    public uint Session { get; protected set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static RpcResult<T> Success(T data, uint nodeId = 0, uint session = 0)
    {
        return new RpcResult<T>
        {
            IsSuccess = true,
            Data = data,
            ErrorType = RpcErrorType.None,
            NodeId = nodeId,
            Session = session
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static RpcResult<T> Failure(RpcErrorType errorType, string errorMsg, uint nodeId = 0, uint session = 0, int errorCode = 0)
    {
        return new RpcResult<T>
        {
            IsSuccess = false,
            ErrorType = errorType,
            ErrorMsg = errorMsg,
            ErrorCode = errorCode,
            NodeId = nodeId,
            Session = session
        };
    }

    // 便捷的判断属性
    public bool IsNodeNotFound => ErrorType == RpcErrorType.NodeNotFound;
    public bool IsNotConnected => ErrorType == RpcErrorType.NotConnected;
    public bool IsTimeout => ErrorType == RpcErrorType.Timeout;
    public bool IsNetworkError => ErrorType == RpcErrorType.NetworkError;
    public bool IsServerError => ErrorType == RpcErrorType.ServerError;
    public bool IsNotAuthenticated => ErrorType == RpcErrorType.NotAuthenticated;
    public bool IsNodeClosed => ErrorType == RpcErrorType.NodeClosed;

    /// <summary>
    /// 链式调用 - 成功时执行
    /// </summary>
    public RpcResult<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && action != null)
        {
            try
            {
                action(Data);
            }
            catch (Exception ex)
            {
                Log.Error($"OnSuccess回调执行异常: {ex}");
            }
        }
        return this;
    }

    /// <summary>
    /// 链式调用 - 失败时执行
    /// </summary>
    public RpcResult<T> OnFailure(Action<RpcErrorType, string> action)
    {
        if (!IsSuccess && action != null)
        {
            try
            {
                action(ErrorType, ErrorMsg);
            }
            catch (Exception ex)
            {
                Log.Error($"OnFailure回调执行异常: {ex}");
            }
        }
        return this;
    }

    /// <summary>
    /// 链式调用 - 特定错误类型时执行
    /// </summary>
    public RpcResult<T> OnError(RpcErrorType errorType, Action<string> action)
    {
        if (!IsSuccess && ErrorType == errorType && action != null)
        {
            try
            {
                action(ErrorMsg);
            }
            catch (Exception ex)
            {
                Log.Error($"OnError回调执行异常: {ex}");
            }
        }
        return this;
    }

    /// <summary>
    /// 转换Result类型
    /// </summary>
    public RpcResult<TNew> Map<TNew>(Func<T, TNew> mapper) where TNew : class
    {
        if (!IsSuccess)
        {
            return RpcResult<TNew>.Failure(ErrorType, ErrorMsg, NodeId, Session, ErrorCode);
        }

        try
        {
            var newData = mapper(Data);
            return RpcResult<TNew>.Success(newData, NodeId, Session);
        }
        catch (Exception ex)
        {
            return RpcResult<TNew>.Failure(RpcErrorType.Unknown, $"转换失败: {ex.Message}", NodeId, Session);
        }
    }

    /// <summary>
    /// 获取数据或默认值
    /// </summary>
    public T GetDataOrDefault(T defaultValue = null)
    {
        return IsSuccess ? Data : defaultValue;
    }

    /// <summary>
    /// 获取数据或抛出异常
    /// </summary>
    public T GetDataOrThrow()
    {
        if (IsSuccess)
            return Data;
        
        throw new Exception($"RPC错误 [{ErrorType}]: {ErrorMsg}");
    }
}

    /// <summary>
    /// 简单操作结果（不需要返回数据的情况）
    /// </summary>
    public class RpcResult : RpcResult<object>
    {
        public static RpcResult SuccessResult(uint nodeId = 0, uint session = 0)
        {
            return new RpcResult
            {
                IsSuccess = true,
                Data = new object(),
                ErrorType = RpcErrorType.None,
                NodeId = nodeId,
                Session = session
            };
        }

        public static new RpcResult Failure(RpcErrorType errorType, string errorMsg, uint nodeId = 0, uint session = 0, int errorCode = 0)
        {
            return new RpcResult
            {
                IsSuccess = false,
                ErrorType = errorType,
                ErrorMsg = errorMsg,
                ErrorCode = errorCode,
                NodeId = nodeId,
                Session = session
            };
        }
    }

    #endregion

    #region 内部状态类

    /// <summary>
    /// 节点心跳状态
    /// </summary>
    internal class NodeHeartbeatState
    {
        public DateTime LastHeartbeatTime;
        public bool WaitingResponse;
        public HeartbeatConfig Config;
        public int FailureCount; // 连续失败次数
    }

    /// <summary>
    /// 节点认证状态
    /// </summary>
    internal class NodeAuthState
    {
        public AuthConfig Config;
        public int RetryCount; // 重试次数
        public bool IsAuthenticating; // 是否正在认证中
    }

    #endregion

    /// <summary>
    /// 网络包模块管理类
    /// </summary>
    public class NetPackModule : Module, IUpdateModule, INetPackModule
    {
        #region 字段定义

        private Dictionary<uint, NetNode> _netNodes = new Dictionary<uint, NetNode>();
        private Dictionary<uint, bool> _closeMap = new Dictionary<uint, bool>();
        
        // 待处理的节点操作队列
        private List<uint> _pendingRemoveNodes = new List<uint>();
        private Dictionary<uint, NetNode> _pendingAddNodes = new Dictionary<uint, NetNode>();
        
        // 基于nodeId的回调管理
        private Dictionary<uint, List<ConnectCallback>> _connectCallbacks = new Dictionary<uint, List<ConnectCallback>>();
        private Dictionary<uint, List<DisconnectCallback>> _disconnectCallbacks = new Dictionary<uint, List<DisconnectCallback>>();
        
        // 消息监听器 - nodeId -> packId -> callbacks
        private Dictionary<uint, Dictionary<ushort, List<MessageCallback>>> _messageListeners = new Dictionary<uint, Dictionary<ushort, List<MessageCallback>>>();
        
        // RPC等待队列
        private Dictionary<uint, Dictionary<uint, UniTaskCompletionSource<INetResponse>>> _rpcWaitingMap = new Dictionary<uint, Dictionary<uint, UniTaskCompletionSource<INetResponse>>>();

        // 认证相关
        private Dictionary<uint, AuthRequestProvider> _authRequestProviders = new Dictionary<uint, AuthRequestProvider>();
        private Dictionary<uint, NodeAuthState> _authStates = new Dictionary<uint, NodeAuthState>();
        private Dictionary<uint, List<AuthSuccessCallback>> _authSuccessCallbacks = new Dictionary<uint, List<AuthSuccessCallback>>();
        private Dictionary<uint, List<AuthFailureCallback>> _authFailureCallbacks = new Dictionary<uint, List<AuthFailureCallback>>();
        private Dictionary<uint, bool> _nodeAuthStatus = new Dictionary<uint, bool>(); // 节点认证状态
        
        // 心跳相关
        private Dictionary<uint, HeartbeatRequestProvider> _heartbeatRequestProviders = new Dictionary<uint, HeartbeatRequestProvider>();
        private Dictionary<uint, NodeHeartbeatState> _heartbeatStates = new Dictionary<uint, NodeHeartbeatState>();

        #endregion

        #region 模块生命周期

        public override void OnInit()
        {
            Log.Info("NetPackModule初始化完成");
        }
        
        public override void Shutdown()
        {
            // 清理所有连接
            foreach (var node in _netNodes.Values)
            {
                node.Disconnect();
                node.Clear();
            }
            _netNodes.Clear();
            _closeMap.Clear();
            _connectCallbacks.Clear();
            _disconnectCallbacks.Clear();
            _messageListeners.Clear();
            
            // 取消所有等待的RPC
            foreach (var nodeRpcs in _rpcWaitingMap.Values)
            {
                foreach (var rpc in nodeRpcs.Values)
                {
                    rpc.TrySetCanceled();
                }
            }
            _rpcWaitingMap.Clear();
            
            // 清理认证和心跳相关数据
            _authRequestProviders.Clear();
            _authStates.Clear();
            _authSuccessCallbacks.Clear();
            _authFailureCallbacks.Clear();
            _nodeAuthStatus.Clear();
            _heartbeatRequestProviders.Clear();
            _heartbeatStates.Clear();
            
            // 清理待处理队列
            _pendingRemoveNodes.Clear();
            _pendingAddNodes.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 第一步：处理待关闭的节点
            foreach (var nodeId in _closeMap.Keys)
            {
                if (_netNodes.TryGetValue(nodeId, out var node))
                {
                    _pendingRemoveNodes.Add(nodeId);
                    
                    // 取消该节点所有等待的RPC
                    if (_rpcWaitingMap.TryGetValue(nodeId, out var nodeRpcs))
                    {
                        foreach (var rpc in nodeRpcs.Values)
                        {
                            rpc.TrySetException(new Exception($"节点 {nodeId} 已关闭"));
                        }
                        _rpcWaitingMap.Remove(nodeId);
                    }
                    
                    MemoryPool.Release(node);
                }
            }
            _closeMap.Clear();
            
            // 第二步：更新所有NetNode（此时不会修改字典）
            foreach (var node in _netNodes.Values)
            {
                node.Update(elapseSeconds, realElapseSeconds);
            }
            
            // 第三步：统一处理删除操作
            foreach (var nodeId in _pendingRemoveNodes)
            {
                _netNodes.Remove(nodeId);
            }
            _pendingRemoveNodes.Clear();
            
            // 第四步：统一处理添加操作
            foreach (var kvp in _pendingAddNodes)
            {
                _netNodes[kvp.Key] = kvp.Value;
            }
            _pendingAddNodes.Clear();
        }

        #endregion

        #region 连接管理
        
        public uint Connect(uint nodeId, NetworkType networkType, string ip, int port, IMsgBodyHelper msgBodyHelper)
        {
            if (_netNodes.ContainsKey(nodeId))
            {
                Disconnect(nodeId);
            }

            var node = MemoryPool.Acquire<NetNode>();
            node.Init(nodeId, networkType, ip, port, msgBodyHelper);
            node.SetCallbacks(OnNodeConnected, OnNodeDisconnected);
            
            // 设置消息处理回调
            node.SetMessageHandleCallback(OnMessageReceived);

            _pendingAddNodes[nodeId] = node;
            if (_closeMap.ContainsKey(nodeId))
            {
                _closeMap.Remove(nodeId);
            }
            
            // 初始化节点状态
            _nodeAuthStatus[nodeId] = false;

            node.Connect();

            Log.Info($"开始连接到服务器 {ip}:{port}，节点ID: {nodeId}");
            return nodeId;
        }

        public bool Reconnect(uint nodeId)
        {
            if (_netNodes.TryGetValue(nodeId, out var node))
            {
                // 重置认证状态
                _nodeAuthStatus[nodeId] = false;
                if (_authStates.TryGetValue(nodeId, out var authState))
                {
                    authState.RetryCount = 0;
                    authState.IsAuthenticating = false;
                }
                if (_heartbeatStates.TryGetValue(nodeId, out var heartbeatState))
                {
                    heartbeatState.LastHeartbeatTime = DateTime.MinValue;
                    heartbeatState.WaitingResponse = false;
                    heartbeatState.FailureCount = 0;
                }
                
                node.Reconnect();
                Log.Info($"开始重连节点 {nodeId}");
                return true;
            }
            Log.Warning($"重连失败，找不到节点 {nodeId}");
            return false;
        }

        public void Disconnect(uint nodeId)
        {
            if (_netNodes.TryGetValue(nodeId, out var node))
            {
                node.Disconnect();
                Log.Info($"主动断开连接节点 {nodeId}");
            }
        }

        public ConnectState GetConnectState(uint nodeId)
        {
            if (_netNodes.TryGetValue(nodeId, out var node))
            {
                return node.ConnectState;
            }
            return ConnectState.Disconnected;
        }

        public bool IsNodeExists(uint nodeId)
        {
            return _netNodes.ContainsKey(nodeId);
        }

        public List<uint> GetAllNodeIds()
        {
            return new List<uint>(_netNodes.Keys);
        }

        public bool Close(uint nodeId)
        {
            if (!_netNodes.TryGetValue(nodeId, out var netNode)) return false;
            if (_closeMap.ContainsKey(nodeId)) return true;
            _closeMap[nodeId] = true;
            netNode.Disconnect();
            return true;
        }

        public int GetReconnectAttempts(uint nodeId)
        {
            if (!_netNodes.TryGetValue(nodeId, out var netNode)) return 0;
            return netNode._ReconnectAttempts;
        }

        #endregion

        #region 回调管理

        public void RegisterConnectCallback(uint nodeId, ConnectCallback callback)
        {
            if (callback == null) return;
            
            if (!_connectCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks = new List<ConnectCallback>();
                _connectCallbacks[nodeId] = callbacks;
            }
            
            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }

        public void UnregisterConnectCallback(uint nodeId, ConnectCallback callback)
        {
            if (callback == null) return;
            
            if (_connectCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                {
                    _connectCallbacks.Remove(nodeId);
                }
            }
        }

        public void RegisterDisconnectCallback(uint nodeId, DisconnectCallback callback)
        {
            if (callback == null) return;
            
            if (!_disconnectCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks = new List<DisconnectCallback>();
                _disconnectCallbacks[nodeId] = callbacks;
            }
            
            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }

        public void UnregisterDisconnectCallback(uint nodeId, DisconnectCallback callback)
        {
            if (callback == null) return;
            
            if (_disconnectCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                {
                    _disconnectCallbacks.Remove(nodeId);
                }
            }
        }

        #endregion

        #region 消息处理功能

        /// <summary>
        /// 注册消息监听器
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">回调函数</param>
        public void RegisterMessageListener(uint nodeId, ushort packId, MessageCallback callback)
        {
            if (callback == null) return;
            
            if (!_messageListeners.TryGetValue(nodeId, out var nodeListeners))
            {
                nodeListeners = new Dictionary<ushort, List<MessageCallback>>();
                _messageListeners[nodeId] = nodeListeners;
            }
            
            if (!nodeListeners.TryGetValue(packId, out var listeners))
            {
                listeners = new List<MessageCallback>();
                nodeListeners[packId] = listeners;
            }
            
            if (!listeners.Contains(callback))
            {
                listeners.Add(callback);
            }
        }

        /// <summary>
        /// 注销消息监听器
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">回调函数</param>
        public void UnregisterMessageListener(uint nodeId, ushort packId, MessageCallback callback)
        {
            if (callback == null) return;
            
            if (_messageListeners.TryGetValue(nodeId, out var nodeListeners) &&
                nodeListeners.TryGetValue(packId, out var listeners))
            {
                listeners.Remove(callback);
                if (listeners.Count == 0)
                {
                    nodeListeners.Remove(packId);
                    if (nodeListeners.Count == 0)
                    {
                        _messageListeners.Remove(nodeId);
                    }
                }
            }
        }

        /// <summary>
        /// 发送消息（Send模式，不等待响应）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <param name="requireAuth">是否需要认证</param>
        /// <returns>操作结果</returns>
        public RpcResult SendMessage(uint nodeId, INetRequest request, bool requireAuth = false)
        {
            // 检查节点是否存在
            if (!_netNodes.TryGetValue(nodeId, out var node))
            {
                return RpcResult.Failure(
                    RpcErrorType.NodeNotFound,
                    $"找不到节点 {nodeId}",
                    nodeId
                );
            }

            // 检查节点是否已关闭
            if (_closeMap.ContainsKey(nodeId))
            {
                return RpcResult.Failure(
                    RpcErrorType.NodeClosed,
                    $"节点 {nodeId} 已关闭",
                    nodeId
                );
            }

            // 检查连接状态
            if (node.ConnectState != ConnectState.Connected)
            {
                return RpcResult.Failure(
                    RpcErrorType.NotConnected,
                    $"节点 {nodeId} 未连接，当前状态: {node.ConnectState}",
                    nodeId
                );
            }

            // 检查认证状态（如果需要）
            if (requireAuth && !IsNodeAuthenticated(nodeId))
            {
                return RpcResult.Failure(
                    RpcErrorType.NotAuthenticated,
                    $"节点 {nodeId} 未认证",
                    nodeId
                );
            }

            try
            {
                node.SendMessage(request);
                return RpcResult.SuccessResult(nodeId);
            }
            catch (Exception ex)
            {
                Log.Error($"SendMessage异常: {ex}");
                return RpcResult.Failure(
                    RpcErrorType.NetworkError,
                    $"发送消息失败: {ex.Message}",
                    nodeId
                );
            }
        }

        /// <summary>
        /// 发送RPC请求（等待响应）- Result模式
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="requireAuth">是否需要认证</param>
        /// <returns>RPC结果</returns>
        public async UniTask<RpcResult<INetResponse>> SendRpcRequest(uint nodeId, INetRequest request, int timeoutMs = 10000, bool requireAuth = false)
        {
            // 检查节点是否存在
            if (!_netNodes.TryGetValue(nodeId, out var node))
            {
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.NodeNotFound,
                    $"找不到节点 {nodeId}",
                    nodeId
                );
            }

            // 检查节点是否已关闭
            if (_closeMap.ContainsKey(nodeId))
            {
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.NodeClosed,
                    $"节点 {nodeId} 已关闭",
                    nodeId
                );
            }

            // 检查连接状态
            if (node.ConnectState != ConnectState.Connected)
            {
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.NotConnected,
                    $"节点 {nodeId} 未连接，当前状态: {node.ConnectState}",
                    nodeId
                );
            }

            // 检查认证状态（如果需要）
            if (requireAuth && !IsNodeAuthenticated(nodeId))
            {
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.NotAuthenticated,
                    $"节点 {nodeId} 未认证",
                    nodeId
                );
            }

            uint session = 0;
            try
            {
                // 发送请求并获取session
                session = node.SendRpcRequest(request);
                
                // 创建等待任务
                var completionSource = new UniTaskCompletionSource<INetResponse>();
                
                if (!_rpcWaitingMap.TryGetValue(nodeId, out var nodeRpcs))
                {
                    nodeRpcs = new Dictionary<uint, UniTaskCompletionSource<INetResponse>>();
                    _rpcWaitingMap[nodeId] = nodeRpcs;
                }
                
                nodeRpcs[session] = completionSource;

                try
                {
                    // 等待响应或超时
                    var response = await completionSource.Task.Timeout(TimeSpan.FromMilliseconds(timeoutMs));
                    
                    // 检查响应是否有错误
                    if (response._IsError)
                    {
                        return RpcResult<INetResponse>.Failure(
                            RpcErrorType.ServerError,
                            response.ErrorMsg ?? "服务器返回错误",
                            nodeId,
                            session,
                            response.ErrorCode
                        );
                    }
                    
                    return RpcResult<INetResponse>.Success(response, nodeId, session);
                }
                catch (TimeoutException)
                {
                    // 清理超时的RPC
                    if (nodeRpcs.TryGetValue(session, out var timeoutRpc))
                    {
                        nodeRpcs.Remove(session);
                    }
                    
                    return RpcResult<INetResponse>.Failure(
                        RpcErrorType.Timeout,
                        $"RPC请求超时 ({timeoutMs}ms)",
                        nodeId,
                        session
                    );
                }
                finally
                {
                    // 清理完成的RPC
                    if (nodeRpcs.TryGetValue(session, out var finishedRpc))
                    {
                        nodeRpcs.Remove(session);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.RequestFailed,
                    "请求已取消",
                    nodeId,
                    session
                );
            }
            catch (Exception ex)
            {
                Log.Error($"SendRpcRequest异常: {ex}");
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.NetworkError,
                    $"网络异常: {ex.Message}",
                    nodeId,
                    session
                );
            }
        }

        /// <summary>
        /// 发送RPC请求并自动重试
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelayMs">重试延迟（毫秒）</param>
        /// <param name="requireAuth">是否需要认证</param>
        /// <returns>RPC结果</returns>
        public async UniTask<RpcResult<INetResponse>> SendRpcRequestWithRetry(
            uint nodeId, 
            INetRequest request, 
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = false)
        {
            RpcResult<INetResponse> result = null;
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                result = await SendRpcRequest(nodeId, request, timeoutMs, requireAuth);
                
                if (result.IsSuccess)
                {
                    return result;
                }
                
                // 某些错误类型不适合重试
                if (result.ErrorType == RpcErrorType.NodeNotFound ||
                    result.ErrorType == RpcErrorType.NodeClosed ||
                    result.ErrorType == RpcErrorType.NotAuthenticated ||
                    result.ErrorType == RpcErrorType.ServerError)
                {
                    Log.Warning($"节点 {nodeId} RPC请求失败 [{result.ErrorType}]，不适合重试: {result.ErrorMsg}");
                    break;
                }
                
                if (attempt < maxRetries)
                {
                    Log.Warning($"节点 {nodeId} RPC请求失败 [{result.ErrorType}]，{retryDelayMs}ms后重试 ({attempt + 1}/{maxRetries}): {result.ErrorMsg}");
                    await UniTask.Delay(retryDelayMs);
                }
            }
            
            return result;
        }

        #endregion

        #region 认证功能

        /// <summary>
        /// 设置节点的认证请求提供者
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="authRequestProvider">认证请求提供者</param>
        /// <param name="config">认证配置</param>
        public void SetAuthRequestProvider(uint nodeId, AuthRequestProvider authRequestProvider, AuthConfig config = default)
        {
            if (authRequestProvider == null)
            {
                _authRequestProviders.Remove(nodeId);
                _authStates.Remove(nodeId);
                Log.Info($"移除节点 {nodeId} 的认证请求提供者");
                return;
            }

            if (config.TimeoutMs <= 0)
            {
                config = new AuthConfig(); // 使用默认配置
            }

            _authRequestProviders[nodeId] = authRequestProvider;
            _authStates[nodeId] = new NodeAuthState
            {
                Config = config,
                RetryCount = 0,
                IsAuthenticating = false
            };
            Log.Info($"设置节点 {nodeId} 的认证请求提供者");
        }

        /// <summary>
        /// 注册认证成功回调
        /// </summary>
        public void RegisterAuthSuccessCallback(uint nodeId, AuthSuccessCallback callback)
        {
            if (callback == null) return;
            
            if (!_authSuccessCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks = new List<AuthSuccessCallback>();
                _authSuccessCallbacks[nodeId] = callbacks;
            }
            
            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }

        /// <summary>
        /// 注销认证成功回调
        /// </summary>
        public void UnregisterAuthSuccessCallback(uint nodeId, AuthSuccessCallback callback)
        {
            if (callback == null) return;
            
            if (_authSuccessCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                {
                    _authSuccessCallbacks.Remove(nodeId);
                }
            }
        }

        /// <summary>
        /// 注册认证失败回调
        /// </summary>
        public void RegisterAuthFailureCallback(uint nodeId, AuthFailureCallback callback)
        {
            if (callback == null) return;
            
            if (!_authFailureCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks = new List<AuthFailureCallback>();
                _authFailureCallbacks[nodeId] = callbacks;
            }
            
            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }

        /// <summary>
        /// 注销认证失败回调
        /// </summary>
        public void UnregisterAuthFailureCallback(uint nodeId, AuthFailureCallback callback)
        {
            if (callback == null) return;
            
            if (_authFailureCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                {
                    _authFailureCallbacks.Remove(nodeId);
                }
            }
        }

        /// <summary>
        /// 获取节点认证状态
        /// </summary>
        public bool IsNodeAuthenticated(uint nodeId)
        {
            return _nodeAuthStatus.TryGetValue(nodeId, out var authenticated) && authenticated;
        }

        /// <summary>
        /// 内部：发送认证请求（连接成功后自动调用）- Result模式
        /// </summary>
        private async UniTaskVoid SendAuthRequestInternal(uint nodeId)
        {
            if (!_authRequestProviders.TryGetValue(nodeId, out var authProvider) ||
                !_authStates.TryGetValue(nodeId, out var authState))
            {
                Log.Warning($"节点 {nodeId} 未设置认证请求提供者");
                return;
            }

            if (authState.IsAuthenticating)
            {
                Log.Warning($"节点 {nodeId} 正在认证中，跳过重复认证");
                return;
            }

            authState.IsAuthenticating = true;

            while (authState.RetryCount <= authState.Config.MaxRetryCount)
            {
                // 检查连接状态
                if (!_netNodes.TryGetValue(nodeId, out var node) || node.ConnectState != ConnectState.Connected)
                {
                    Log.Warning($"节点 {nodeId} 连接已断开，停止认证");
                    break;
                }

                var authRequest = authProvider(nodeId);
                if (authRequest == null)
                {
                    Log.Error($"节点 {nodeId} 认证请求提供者返回null");
                    break;
                }

                Log.Info($"节点 {nodeId} 开始认证，第 {authState.RetryCount + 1} 次尝试");
                
                // 使用Result模式发送请求（认证请求不需要requireAuth参数）
                var result = await SendRpcRequest(nodeId, authRequest, authState.Config.TimeoutMs, requireAuth: false);
                
                if (result.IsSuccess)
                {
                    // 认证成功
                    _nodeAuthStatus[nodeId] = true;
                    authState.RetryCount = 0;
                    Log.Info($"节点 {nodeId} 认证成功");

                    // 触发认证成功回调
                    if (_authSuccessCallbacks.TryGetValue(nodeId, out var successCallbacks))
                    {
                        foreach (var callback in successCallbacks)
                        {
                            try
                            {
                                callback?.Invoke(result);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"认证成功回调执行异常: {ex}");
                            }
                        }
                    }

                    // 认证成功后启动心跳
                    if (_heartbeatStates.TryGetValue(nodeId, out var heartbeatState) && heartbeatState.Config.Enabled)
                    {
                        heartbeatState.LastHeartbeatTime = DateTime.MinValue;
                        heartbeatState.FailureCount = 0;
                        StartHeartbeatAsync(nodeId).Forget();
                    }

                    break; // 认证成功，退出循环
                }
                else
                {
                    // 认证失败
                    Log.Error($"节点 {nodeId} 认证失败 [{result.ErrorType}]: {result.ErrorMsg}");
                    
                    // 触发认证失败回调
                    if (_authFailureCallbacks.TryGetValue(nodeId, out var failureCallbacks))
                    {
                        foreach (var callback in failureCallbacks)
                        {
                            try
                            {
                                callback?.Invoke(result);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"认证失败回调执行异常: {ex}");
                            }
                        }
                    }
                    
                    // 某些错误类型不适合重试
                    if (result.ErrorType == RpcErrorType.NodeNotFound ||
                        result.ErrorType == RpcErrorType.NodeClosed ||
                        result.ErrorType == RpcErrorType.NotConnected)
                    {
                        Log.Error($"节点 {nodeId} 认证失败，不适合重试");
                        break;
                    }
                    
                    // 增加重试次数
                    authState.RetryCount++;
                    if (authState.RetryCount <= authState.Config.MaxRetryCount)
                    {
                        Log.Info($"节点 {nodeId} 认证失败，{authState.Config.RetryIntervalMs}ms后重试");
                        await UniTask.Delay(authState.Config.RetryIntervalMs);
                    }
                    else
                    {
                        Log.Error($"节点 {nodeId} 认证失败次数超过限制，停止重试");
                        break;
                    }
                }
            }

            authState.IsAuthenticating = false;
        }

        #endregion

        #region 心跳功能

        /// <summary>
        /// 设置节点的心跳请求提供者
        /// </summary>
        public void SetHeartbeatRequestProvider(uint nodeId, HeartbeatRequestProvider heartbeatRequestProvider, HeartbeatConfig config = default)
        {
            if (heartbeatRequestProvider == null)
            {
                _heartbeatRequestProviders.Remove(nodeId);
                _heartbeatStates.Remove(nodeId);
                Log.Info($"移除节点 {nodeId} 的心跳请求提供者");
                return;
            }

            if (config.IntervalSeconds <= 0)
            {
                config = new HeartbeatConfig(); // 使用默认配置
            }

            _heartbeatRequestProviders[nodeId] = heartbeatRequestProvider;
            _heartbeatStates[nodeId] = new NodeHeartbeatState
            {
                Config = config,
                LastHeartbeatTime = DateTime.MinValue,
                WaitingResponse = false,
                FailureCount = 0
            };

            Log.Info($"设置节点 {nodeId} 的心跳请求提供者，间隔: {config.IntervalSeconds}秒");
        }

        /// <summary>
        /// 获取节点心跳配置
        /// </summary>
        public HeartbeatConfig? GetHeartbeatConfig(uint nodeId)
        {
            if (_heartbeatStates.TryGetValue(nodeId, out var state))
            {
                return state.Config;
            }
            return null;
        }

        /// <summary>
        /// 更新节点心跳配置
        /// </summary>
        public void UpdateHeartbeatConfig(uint nodeId, HeartbeatConfig config)
        {
            if (_heartbeatStates.TryGetValue(nodeId, out var state))
            {
                state.Config = config;
                Log.Info($"更新节点 {nodeId} 心跳配置，间隔: {config.IntervalSeconds}秒，启用: {config.Enabled}");
            }
        }

        /// <summary>
        /// 内部：发送心跳请求 - Result模式
        /// </summary>
        private async UniTask<RpcResult<INetResponse>> SendHeartbeatInternal(uint nodeId)
        {
            if (!_heartbeatRequestProviders.TryGetValue(nodeId, out var heartbeatProvider) ||
                !_heartbeatStates.TryGetValue(nodeId, out var state))
            {
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.Unknown,
                    "未设置心跳请求提供者",
                    nodeId
                );
            }

            var heartbeatRequest = heartbeatProvider(nodeId);
            if (heartbeatRequest == null)
            {
                return RpcResult<INetResponse>.Failure(
                    RpcErrorType.RequestFailed,
                    "心跳请求提供者返回null",
                    nodeId
                );
            }

            state.WaitingResponse = true;
            
            var result = await SendRpcRequest(nodeId, heartbeatRequest, state.Config.TimeoutMs, requireAuth: true);
            
            state.WaitingResponse = false;
            
            if (result.IsSuccess)
            {
                state.LastHeartbeatTime = DateTime.Now;
                state.FailureCount = 0;
                //Log.Debug($"节点 {nodeId} 心跳响应正常");
            }
            else
            {
                state.FailureCount++;
                Log.Warning($"节点 {nodeId} 心跳失败 [{result.ErrorType}]: {result.ErrorMsg}，失败次数: {state.FailureCount}");
            }
            
            return result;
        }

        /// <summary>
        /// 内部：启动心跳循环（认证成功后自动调用）
        /// </summary>
        private async UniTaskVoid StartHeartbeatAsync(uint nodeId)
        {
            Log.Info($"节点 {nodeId} 开始心跳循环");

            while (_heartbeatStates.TryGetValue(nodeId, out var state) && 
                   state.Config.Enabled && 
                   _netNodes.TryGetValue(nodeId, out var node) &&
                   node.ConnectState == ConnectState.Connected &&
                   IsNodeAuthenticated(nodeId))
            {
                try
                {
                    // 等待心跳间隔
                    await UniTask.Delay(TimeSpan.FromSeconds(state.Config.IntervalSeconds));
                    
                    // 检查状态是否仍然有效
                    if (!_heartbeatStates.TryGetValue(nodeId, out state) || 
                        !state.Config.Enabled ||
                        !_netNodes.TryGetValue(nodeId, out node) ||
                        node.ConnectState != ConnectState.Connected ||
                        !IsNodeAuthenticated(nodeId))
                    {
                        break;
                    }

                    // 发送心跳
                    var result = await SendHeartbeatInternal(nodeId);
                    
                    if (!result.IsSuccess)
                    {
                        // 检查是否超过最大重试次数
                        if (state.FailureCount >= state.Config.MaxRetryCount)
                        {
                            Log.Error($"节点 {nodeId} 心跳连续失败 {state.FailureCount} 次，停止心跳并断开连接");
                            
                            // 断开连接，触发重连逻辑
                            Disconnect(nodeId);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"节点 {nodeId} 心跳循环异常: {ex.Message}");
                    break;
                }
            }

            Log.Info($"节点 {nodeId} 心跳循环结束");
        }

        #endregion

        #region 内部回调处理

        private void OnNodeConnected(uint nodeId, bool success, string errorMsg)
        {
            Log.Info($"节点 {nodeId} 连接结果: {(success ? "成功" : "失败")} {errorMsg}");
            
            // 连接成功后自动发送认证请求
            if (success && _authRequestProviders.ContainsKey(nodeId))
            {
                SendAuthRequestInternal(nodeId).Forget();
            }
            
            // 触发该节点的连接回调
            if (_connectCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke(success, errorMsg);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"连接回调执行异常: {ex}");
                    }
                }
            }
        }

        private void OnNodeDisconnected(uint nodeId, DisconnectType disconnectType, string reason)
        {
            Log.Info($"节点 {nodeId} {disconnectType}: {reason}");
            
            // 取消该节点所有等待的RPC
            if (_rpcWaitingMap.TryGetValue(nodeId, out var nodeRpcs))
            {
                foreach (var rpc in nodeRpcs.Values.ToList())
                {
                    rpc.TrySetException(new Exception($"节点 {nodeId} 断开连接: {reason}"));
                }
                _rpcWaitingMap.Remove(nodeId);
            }

            // 触发该节点的断线回调
            if (_disconnectCallbacks.TryGetValue(nodeId, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke(disconnectType, reason);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"断线回调执行异常: {ex} {ex.StackTrace}");
                    }
                }
            }
            
            // 重置认证状态和心跳状态
            _nodeAuthStatus[nodeId] = false;
            if (_authStates.TryGetValue(nodeId, out var authState))
            {
                authState.RetryCount = 0;
                authState.IsAuthenticating = false;
            }
            if (_heartbeatStates.TryGetValue(nodeId, out var heartbeatState))
            {
                heartbeatState.WaitingResponse = false;
                heartbeatState.LastHeartbeatTime = DateTime.MinValue;
                heartbeatState.FailureCount = 0;
            }
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void OnMessageReceived(uint nodeId, INetResponse response)
        {
            ushort packId = response._PackId;
            uint session = response._Session - 1;

            // 处理RPC响应
            if (!response._IsPush)
            {
                if (_rpcWaitingMap.TryGetValue(nodeId, out var nodeRpcs) && 
                    nodeRpcs.TryGetValue(session, out var completionSource))
                {
                    completionSource.TrySetResult(response);
                    nodeRpcs.Remove(session);
                    return;
                }
            }

            // 触发该节点的消息监听器
            if (_messageListeners.TryGetValue(nodeId, out var nodeListeners) &&
                nodeListeners.TryGetValue(packId, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"消息监听器执行异常: {ex}");
                    }
                }
            }
        }

        #endregion

        #region 调试信息

        public string GetConnectionsInfo()
        {
            var info = $"当前连接数: {_netNodes.Count}\n";
            foreach (var kvp in _netNodes)
            {
                var node = kvp.Value;
                var authenticated = IsNodeAuthenticated(kvp.Key) ? "已认证" : "未认证";
                var heartbeat = "";
                if (_heartbeatStates.TryGetValue(kvp.Key, out var heartbeatState))
                {
                    heartbeat = $"心跳开启(失败次数:{heartbeatState.FailureCount})";
                }
                else
                {
                    heartbeat = "心跳关闭";
                }
                info += $"节点 {kvp.Key}: {node.Ip}:{node.Port} - {node.ConnectState} - {authenticated} - {heartbeat}\n";
            }
            return info;
        }

        #endregion
    }
}

