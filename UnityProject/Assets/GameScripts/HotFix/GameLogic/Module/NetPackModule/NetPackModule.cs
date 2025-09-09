using System;
using System.Collections.Generic;
using TEngine;

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
    public delegate void ConnectCallback(ulong nodeId, bool success, string errorMsg = "");
    
    /// <summary>
    /// 断线回调委托
    /// </summary>
    public delegate void DisconnectCallback(ulong nodeId, DisconnectType disconnectType, string reason = "");

    /// <summary>
    /// 网络包模块管理类
    /// </summary>
    public class NetPackModule : Module, IUpdateModule, INetPackModule
    {
        private Dictionary<ulong, NetNode> _netNodes = new Dictionary<ulong, NetNode>();
        private Dictionary<ulong, bool> _closeMap = new Dictionary<ulong, bool>();
        private List<ConnectCallback> _connectCallbacks = new List<ConnectCallback>();
        private List<DisconnectCallback> _disconnectCallbacks = new List<DisconnectCallback>();

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
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var nodeId in _closeMap.Keys)
            {
                if (_netNodes.TryGetValue(nodeId, out var node))
                {
                    node.Disconnect();
                    _netNodes.Remove(nodeId);
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

        #region 连接管理
        
        /// <summary>
        /// 使用指定ID创建并连接到服务器
        /// </summary>
        /// <param name="nodeId">指定的节点ID</param>
        /// <param name="networkType">网络类型</param>
        /// <param name="ip">服务器IP</param>
        /// <param name="port">服务器端口</param>
        /// <returns>节点ID</returns>
        public ulong Connect(ulong nodeId, NetworkType networkType, string ip, int port)
        {
            // 检查ID是否已被使用
            if (_netNodes.ContainsKey(nodeId))
            {
                Log.Error($"节点ID {nodeId} 已存在，无法重复创建");
                return 0;
            }

            var node = MemoryPool.Acquire<NetNode>();
            node.Init(nodeId, networkType, ip, port);
            node.SetCallbacks(OnNodeConnected, OnNodeDisconnected);

            _netNodes[nodeId] = node;
            //删除之后又添加了，就移出标记删除
            if (_closeMap.ContainsKey(nodeId))
            {
                _closeMap.Remove(nodeId);
            }
            node.Connect();

            Log.Info($"开始连接到服务器 {ip}:{port}，节点ID: {nodeId}");
            return nodeId;
        }

        /// <summary>
        /// 重连指定节点
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否成功开始重连</returns>
        public bool Reconnect(ulong nodeId)
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

        /// <summary>
        /// 主动断开指定连接
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        public void Disconnect(ulong nodeId)
        {
            if (_netNodes.TryGetValue(nodeId, out var node))
            {
                node.Disconnect(); // 这会触发主动断开回调
                Log.Info($"主动断开连接节点 {nodeId}");
            }
        }
        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>连接状态</returns>
        public ConnectState GetConnectState(ulong nodeId)
        {
            if (_netNodes.TryGetValue(nodeId, out var node))
            {
                return node.ConnectState;
            }
            return ConnectState.Disconnected;
        }

        /// <summary>
        /// 检查节点ID是否存在
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否存在</returns>
        public bool IsNodeExists(ulong nodeId)
        {
            return _netNodes.ContainsKey(nodeId);
        }

        /// <summary>
        /// 获取所有连接的节点ID
        /// </summary>
        /// <returns>节点ID列表</returns>
        public List<ulong> GetAllNodeIds()
        {
            return new List<ulong>(_netNodes.Keys);
        }

        /// <summary>
        /// 关闭指定节点, 下一帧删除
        /// </summary>
        public bool Close(ulong nodeId)
        {
            if (!_netNodes.TryGetValue(nodeId, out var netNode)) return false;
            if (_closeMap.ContainsKey(nodeId)) return true;
            _closeMap[nodeId] = true;
            return true;
        }

        /// <summary
        /// 获取重连失败次数，成功后次数会重置
        /// </summary> 
        public int GetReconnectAttempts(ulong nodeId)
        {
            if (!_netNodes.TryGetValue(nodeId, out var netNode)) return 0;
            return netNode._ReconnectAttempts;
        }
        #endregion

        #region 回调管理

        /// <summary>
        /// 注册连接成功回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        public void RegisterConnectCallback(ConnectCallback callback)
        {
            if (callback != null && !_connectCallbacks.Contains(callback))
            {
                _connectCallbacks.Add(callback);
            }
        }

        /// <summary>
        /// 注销连接成功回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        public void UnregisterConnectCallback(ConnectCallback callback)
        {
            if (callback != null)
            {
                _connectCallbacks.Remove(callback);
            }
        }

        /// <summary>
        /// 注册断线回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        public void RegisterDisconnectCallback(DisconnectCallback callback)
        {
            if (callback != null && !_disconnectCallbacks.Contains(callback))
            {
                _disconnectCallbacks.Add(callback);
            }
        }

        /// <summary>
        /// 注销断线回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        public void UnregisterDisconnectCallback(DisconnectCallback callback)
        {
            if (callback != null)
            {
                _disconnectCallbacks.Remove(callback);
            }
        }

        #endregion

        #region 内部回调处理

        private void OnNodeConnected(ulong nodeId, bool success, string errorMsg)
        {
            Log.Info($"节点 {nodeId} 连接结果: {(success ? "成功" : "失败")} {errorMsg}");
            
            // 通知所有注册的回调
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

        private void OnNodeDisconnected(ulong nodeId, DisconnectType disconnectType, string reason)
        {
            Log.Info($"节点 {nodeId} {disconnectType}: {reason}");
            // 通知所有注册的回调
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

        #endregion

        #region 调试信息

        /// <summary>
        /// 获取所有连接信息
        /// </summary>
        /// <returns>连接信息字符串</returns>
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

