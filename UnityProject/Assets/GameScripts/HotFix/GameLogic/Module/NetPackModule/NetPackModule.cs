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
    /// 网络包模块管理类
    /// </summary>
    public class NetPackModule : Module, IUpdateModule, INetPackModule
    {
        private Dictionary<uint, NetNode> _netNodes = new Dictionary<uint, NetNode>();
        private Dictionary<uint, bool> _closeMap = new Dictionary<uint, bool>();
        private List<ConnectCallback> _connectCallbacks = new List<ConnectCallback>();
        private List<DisconnectCallback> _disconnectCallbacks = new List<DisconnectCallback>();
        
        // 新增：消息监听器
        private Dictionary<ushort, List<MessageCallback>> _messageListeners = new Dictionary<ushort, List<MessageCallback>>();
        
        // 新增：RPC等待队列
        private Dictionary<uint, Dictionary<uint, UniTaskCompletionSource<INetResponse>>> _rpcWaitingMap = new Dictionary<uint, Dictionary<uint, UniTaskCompletionSource<INetResponse>>>();

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

        #region 连接管理 (保持原有代码不变)
        
        public uint Connect(uint nodeId, NetworkType networkType, string ip, int port, IMsgBodyHelper msgBodyHelper)
        {
            if (_netNodes.ContainsKey(nodeId))
            {
                Log.Error($"节点ID {nodeId} 已存在，无法重复创建");
                return 0;
            }

            var node = MemoryPool.Acquire<NetNode>();
            node.Init(nodeId, networkType, ip, port, msgBodyHelper);
            node.SetCallbacks(OnNodeConnected, OnNodeDisconnected);
            
            // 新增：设置消息处理回调
            node.SetMessageHandleCallback(OnMessageReceived);

            _netNodes[nodeId] = node;
            if (_closeMap.ContainsKey(nodeId))
            {
                _closeMap.Remove(nodeId);
            }
            node.Connect();

            Log.Info($"开始连接到服务器 {ip}:{port}，节点ID: {nodeId}");
            return nodeId;
        }

        public bool Reconnect(uint nodeId)
        {
            if (_netNodes.TryGetValue(nodeId, out var node))
            {
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

        #region 回调管理 (保持原有代码不变)

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

        #region 新增：消息处理功能

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

        #region 内部回调处理

        private void OnNodeConnected(uint nodeId, bool success, string errorMsg)
        {
            Log.Info($"节点 {nodeId} 连接结果: {(success ? "成功" : "失败")} {errorMsg}");
            
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
        /// 新增：处理接收到的消息
        /// </summary>
        private void OnMessageReceived(uint nodeId, INetResponse response)
        {
            ushort packId = response._PackId;
            uint session = response._Session - 1;

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

        #region 调试信息 (保持原有代码不变)

        public string GetConnectionsInfo()
        {
            var info = $"当前连接数: {_netNodes.Count}\n";
            foreach (var kvp in _netNodes)
            {
                var node = kvp.Value;
                info += $"节点 {kvp.Key}: {node.Ip}:{node.Port} - {node.ConnectState}\n";
            }
            return info;
        }

        #endregion
    }
}

