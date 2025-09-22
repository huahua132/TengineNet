using System;
using System.Collections.Generic;
using TEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
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
    /// 连接回调委托
    /// </summary>
    public delegate void ConnectCallback(uint nodeId, bool success, string errorMsg = "");
    
    /// <summary>
    /// 断线回调委托
    /// </summary>
    public delegate void DisconnectCallback(uint nodeId, DisconnectType disconnectType, string reason = "");
    
    /// <summary>
    /// 消息监听回调委托
    /// </summary>
    public delegate void MessageCallback(uint nodeId, INetResponse response);

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
    public delegate void AuthSuccessCallback(uint nodeId, INetResponse response);

    /// <summary>
    /// 认证失败回调委托
    /// </summary>
    public delegate void AuthFailureCallback(uint nodeId, INetResponse response, string error);

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

    /// <summary>
    /// 网络包模块管理类
    /// </summary>
    public class NetPackModule : Module, IUpdateModule, INetPackModule
    {
        #region 字段定义

        private Dictionary<uint, NetNode> _netNodes = new Dictionary<uint, NetNode>();
        private Dictionary<uint, bool> _closeMap = new Dictionary<uint, bool>();
        private List<ConnectCallback> _connectCallbacks = new List<ConnectCallback>();
        private List<DisconnectCallback> _disconnectCallbacks = new List<DisconnectCallback>();
        
        // 消息监听器
        private Dictionary<ushort, List<MessageCallback>> _messageListeners = new Dictionary<ushort, List<MessageCallback>>();
        
        // RPC等待队列
        private Dictionary<uint, Dictionary<uint, UniTaskCompletionSource<INetResponse>>> _rpcWaitingMap = new Dictionary<uint, Dictionary<uint, UniTaskCompletionSource<INetResponse>>>();

        // 认证相关
        private Dictionary<uint, AuthRequestProvider> _authRequestProviders = new Dictionary<uint, AuthRequestProvider>();
        private Dictionary<uint, NodeAuthState> _authStates = new Dictionary<uint, NodeAuthState>();
        private List<AuthSuccessCallback> _authSuccessCallbacks = new List<AuthSuccessCallback>();
        private List<AuthFailureCallback> _authFailureCallbacks = new List<AuthFailureCallback>();
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
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var nodeId in _closeMap.Keys)
            {
                if (_netNodes.TryGetValue(nodeId, out var node))
                {
                    node.Disconnect();
                    _netNodes.Remove(nodeId);
                    
                    // 取消该节点所有等待的RPC
                    if (_rpcWaitingMap.TryGetValue(nodeId, out var nodeRpcs))
                    {
                        foreach (var rpc in nodeRpcs.Values)
                        {
                            rpc.TrySetException(new Exception($"节点 {nodeId} 已关闭"));
                        }
                        _rpcWaitingMap.Remove(nodeId);
                    }
                    
                    // 清理节点相关状态
                    _nodeAuthStatus.Remove(nodeId);
                    _authStates.Remove(nodeId);
                    _heartbeatStates.Remove(nodeId);
                    
                    MemoryPool.Release(node);
                }
            }
            _closeMap.Clear();
            
            // 更新所有NetNode
            foreach (var node in _netNodes.Values)
            {
                node.Update(elapseSeconds, realElapseSeconds);
            }
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

            _netNodes[nodeId] = node;
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
            return true;
        }

        public int GetReconnectAttempts(uint nodeId)
        {
            if (!_netNodes.TryGetValue(nodeId, out var netNode)) return 0;
            return netNode._ReconnectAttempts;
        }

        #endregion

        #region 回调管理

        public void RegisterConnectCallback(ConnectCallback callback)
        {
            if (callback != null && !_connectCallbacks.Contains(callback))
            {
                _connectCallbacks.Add(callback);
            }
        }

        public void UnregisterConnectCallback(ConnectCallback callback)
        {
            if (callback != null)
            {
                _connectCallbacks.Remove(callback);
            }
        }

        public void RegisterDisconnectCallback(DisconnectCallback callback)
        {
            if (callback != null && !_disconnectCallbacks.Contains(callback))
            {
                _disconnectCallbacks.Add(callback);
            }
        }

        public void UnregisterDisconnectCallback(DisconnectCallback callback)
        {
            if (callback != null)
            {
                _disconnectCallbacks.Remove(callback);
            }
        }

        #endregion

        #region 消息处理功能

        /// <summary>
        /// 注册消息监听器
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">回调函数</param>
        public void RegisterMessageListener(ushort packId, MessageCallback callback)
        {
            if (callback == null) return;
            
            if (!_messageListeners.TryGetValue(packId, out var listeners))
            {
                listeners = new List<MessageCallback>();
                _messageListeners[packId] = listeners;
            }
            
            if (!listeners.Contains(callback))
            {
                listeners.Add(callback);
            }
        }

        /// <summary>
        /// 注销消息监听器
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">回调函数</param>
        public void UnregisterMessageListener(ushort packId, MessageCallback callback)
        {
            if (callback == null) return;
            
            if (_messageListeners.TryGetValue(packId, out var listeners))
            {
                listeners.Remove(callback);
                if (listeners.Count == 0)
                {
                    _messageListeners.Remove(packId);
                }
            }
        }

        /// <summary>
        /// 发送消息（Send模式，不等待响应）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <returns>是否发送成功</returns>
        public bool SendMessage(uint nodeId, INetRequest request)
        {
            if (!_netNodes.TryGetValue(nodeId, out var node))
            {
                Log.Error($"SendMessage失败，找不到节点 {nodeId}");
                return false;
            }

            if (node.ConnectState != ConnectState.Connected)
            {
                Log.Error($"SendMessage失败，节点 {nodeId} 未连接");
                return false;
            }

            try
            {
                node.SendMessage(request);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"SendMessage异常: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 发送RPC请求（等待响应）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>响应对象</returns>
        public async UniTask<INetResponse> SendRpcRequest(uint nodeId, INetRequest request, int timeoutMs = 5000)
        {
            if (!_netNodes.TryGetValue(nodeId, out var node))
            {
                throw new Exception($"SendRpcRequest失败，找不到节点 {nodeId}");
            }

            if (node.ConnectState != ConnectState.Connected)
            {
                throw new Exception($"SendRpcRequest失败，节点 {nodeId} 未连接");
            }

            // 发送请求并获取session
            uint session = node.SendRpcRequest(request);
            
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
                return response;
            }
            catch (TimeoutException)
            {
                // 清理超时的RPC
                if (nodeRpcs.TryGetValue(session, out var timeoutRpc))
                {
                    nodeRpcs.Remove(session);
                }
                throw new Exception($"RPC请求超时，节点 {nodeId}，session {session}");
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
        /// <param name="callback">认证成功回调</param>
        public void RegisterAuthSuccessCallback(AuthSuccessCallback callback)
        {
            if (callback != null && !_authSuccessCallbacks.Contains(callback))
            {
                _authSuccessCallbacks.Add(callback);
            }
        }

        /// <summary>
        /// 注销认证成功回调
        /// </summary>
        /// <param name="callback">认证成功回调</param>
        public void UnregisterAuthSuccessCallback(AuthSuccessCallback callback)
        {
            if (callback != null)
            {
                _authSuccessCallbacks.Remove(callback);
            }
        }

        /// <summary>
        /// 注册认证失败回调
        /// </summary>
        /// <param name="callback">认证失败回调</param>
        public void RegisterAuthFailureCallback(AuthFailureCallback callback)
        {
            if (callback != null && !_authFailureCallbacks.Contains(callback))
            {
                _authFailureCallbacks.Add(callback);
            }
        }

        /// <summary>
        /// 注销认证失败回调
        /// </summary>
        /// <param name="callback">认证失败回调</param>
        public void UnregisterAuthFailureCallback(AuthFailureCallback callback)
        {
            if (callback != null)
            {
                _authFailureCallbacks.Remove(callback);
            }
        }

        /// <summary>
        /// 获取节点认证状态
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否已认证</returns>
        public bool IsNodeAuthenticated(uint nodeId)
        {
            return _nodeAuthStatus.TryGetValue(nodeId, out var authenticated) && authenticated;
        }

        /// <summary>
        /// 内部：发送认证请求（连接成功后自动调用）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
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
                try
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
                    var response = await SendRpcRequest(nodeId, authRequest, authState.Config.TimeoutMs);
                    
                    // 检查响应是否有错误
                    if (response._IsError)
                    {
                        var errorMsg = $"认证失败，错误码: {response.ErrorCode}, 错误信息: {response.ErrorMsg}";
                        Log.Error($"节点 {nodeId} {errorMsg}");
                        
                        // 触发认证失败回调
                        foreach (var callback in _authFailureCallbacks)
                        {
                            try
                            {
                                callback?.Invoke(nodeId, response, errorMsg);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"认证失败回调执行异常: {ex}");
                            }
                        }
                        
                        // 增加重试次数
                        authState.RetryCount++;
                        if (authState.RetryCount <= authState.Config.MaxRetryCount)
                        {
                            Log.Info($"节点 {nodeId} 认证失败，{authState.Config.RetryIntervalMs}ms后重试");
                            await UniTask.Delay(authState.Config.RetryIntervalMs);
                            continue;
                        }
                        else
                        {
                            Log.Error($"节点 {nodeId} 认证失败次数超过限制，停止重试");
                            break;
                        }
                    }
                    
                    // 认证成功
                    _nodeAuthStatus[nodeId] = true;
                    authState.RetryCount = 0;
                    Log.Info($"节点 {nodeId} 认证成功");

                    // 触发认证成功回调
                    foreach (var callback in _authSuccessCallbacks)
                    {
                        try
                        {
                            callback?.Invoke(nodeId, response);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"认证成功回调执行异常: {ex}");
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
                catch (Exception ex)
                {
                    Log.Error($"节点 {nodeId} 认证异常: {ex.Message}");
                    authState.RetryCount++;
                    if (authState.RetryCount <= authState.Config.MaxRetryCount)
                    {
                        await UniTask.Delay(authState.Config.RetryIntervalMs);
                        continue;
                    }
                    else
                    {
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
        /// <param name="nodeId">节点ID</param>
        /// <param name="heartbeatRequestProvider">心跳请求提供者</param>
        /// <param name="config">心跳配置</param>
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
        /// <param name="nodeId">节点ID</param>
        /// <returns>心跳配置</returns>
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
        /// <param name="nodeId">节点ID</param>
        /// <param name="config">新的心跳配置</param>
        public void UpdateHeartbeatConfig(uint nodeId, HeartbeatConfig config)
        {
            if (_heartbeatStates.TryGetValue(nodeId, out var state))
            {
                state.Config = config;
                Log.Info($"更新节点 {nodeId} 心跳配置，间隔: {config.IntervalSeconds}秒，启用: {config.Enabled}");
            }
        }

        /// <summary>
        /// 内部：发送心跳请求（认证成功后自动调用）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        private async UniTask<bool> SendHeartbeatInternal(uint nodeId)
        {
            if (!_heartbeatRequestProviders.TryGetValue(nodeId, out var heartbeatProvider) ||
                !_heartbeatStates.TryGetValue(nodeId, out var state))
            {
                return false;
            }

            var heartbeatRequest = heartbeatProvider(nodeId);
            if (heartbeatRequest == null)
            {
                Log.Error($"节点 {nodeId} 心跳请求提供者返回null");
                return false;
            }

            state.WaitingResponse = true;
            try
            {
                var response = await SendRpcRequest(nodeId, heartbeatRequest, state.Config.TimeoutMs);
                
                // 检查响应是否有错误
                if (response._IsError)
                {
                    var errorMsg = $"心跳失败，错误码: {response.ErrorCode}, 错误信息: {response.ErrorMsg}";
                    Log.Warning($"节点 {nodeId} {errorMsg}");
                    state.FailureCount++;
                    state.WaitingResponse = false;
                    return false;
                }
                
                // 心跳成功
                state.LastHeartbeatTime = DateTime.Now;
                state.WaitingResponse = false;
                state.FailureCount = 0;
                Log.Debug($"节点 {nodeId} 心跳响应正常");
                return true;
            }
            catch (Exception ex)
            {
                state.WaitingResponse = false;
                state.FailureCount++;
                Log.Warning($"节点 {nodeId} 心跳请求失败: {ex.Message}，失败次数: {state.FailureCount}");
                return false;
            }
        }

        /// <summary>
        /// 内部：启动心跳循环（认证成功后自动调用）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
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
                    bool success = await SendHeartbeatInternal(nodeId);
                    
                    if (!success)
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
            
            foreach (var callback in _connectCallbacks)
            {
                try
                {
                    callback?.Invoke(nodeId, success, errorMsg);
                }
                catch (Exception ex)
                {
                    Log.Error($"连接回调执行异常: {ex}");
                }
            }
        }

        private void OnNodeDisconnected(uint nodeId, DisconnectType disconnectType, string reason)
        {
            Log.Info($"节点 {nodeId} {disconnectType}: {reason}");
            
            // 取消该节点所有等待的RPC
            if (_rpcWaitingMap.TryGetValue(nodeId, out var nodeRpcs))
            {
                foreach (var rpc in nodeRpcs.Values)
                {
                    rpc.TrySetException(new Exception($"节点 {nodeId} 断开连接: {reason}"));
                }
                _rpcWaitingMap.Remove(nodeId);
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
            
            foreach (var callback in _disconnectCallbacks)
            {
                try
                {
                    callback?.Invoke(nodeId, disconnectType, reason);
                }
                catch (Exception ex)
                {
                    Log.Error($"断线回调执行异常: {ex}");
                }
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

            // 触发消息监听器
            if (_messageListeners.TryGetValue(packId, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(nodeId, response);
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

