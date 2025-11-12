using System;
using System.Net.Sockets;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 连接回调委托
    /// </summary>
    public delegate void NodeConnectCallback(uint nodeID, bool success, string errorMsg = "");
    
    /// <summary>
    /// 断线回调委托
    /// </summary>
    public delegate void NodeDisconnectCallback(uint nodeID, DisconnectType disconnectType, string reason = "");
    public delegate void MessageHandleCallback(uint nodeId, INetResponse response);
    
    /// <summary>
    /// 网络节点 - 内部实现，完全封装
    /// </summary>
    internal class NetNode : IMemory
    {
        public static readonly int PackageBodyMaxSize = ushort.MaxValue - 8;

        private static readonly RpcNetPackageEncoder _netPackEncoder = new RpcNetPackageEncoder();
        private static readonly RpcNetPackageDecoder _netPackDecoder = new RpcNetPackageDecoder();
        private IMsgBodyHelper _msgBodyHelper;
        private MessageHandleCallback _messageHandleCallback; 

        private uint _guid;
        private NetworkType _networkType;
        private string _ip;
        private int _port;
        private AClient _conn;
        private ConnectState _connectState = ConnectState.Disconnected;
        private NodeConnectCallback _connectCallback;
        private NodeDisconnectCallback _disconnectCallback;
        public int _ReconnectAttempts { get; private set; } = 0;
        private bool _isProcessingMessage = false; // 消息处理中标记
        private bool _pendingDisconnect = false;   // 待断开标记

        #region 属性访问器

        public uint Guid => _guid;
        public NetworkType NetworkType => _networkType;
        public string Ip => _ip;
        public int Port => _port;
        public ConnectState ConnectState => _connectState;

        #endregion

        #region IMemory实现

        public void Clear()
        {
            Disconnect();
            _guid = 0;
            _networkType = 0;
            _ip = "";
            _port = 0;
            _connectState = ConnectState.Disconnected;
            _connectCallback = null;
            _disconnectCallback = null;
            _ReconnectAttempts = 0;
            _isProcessingMessage = false;
            _pendingDisconnect = false;
        }

        #endregion

        #region 初始化和连接

        /// <summary>
        /// 初始化网络节点
        /// </summary>
        /// <param name="guid">节点唯一ID，由外部传入</param>
        /// <param name="networkType">网络类型</param>
        /// <param name="ip">服务器IP</param>
        /// <param name="port">服务器端口</param>
        public void Init(uint guid, NetworkType networkType, string ip, int port, IMsgBodyHelper msgBodyHelper)
        {
            _guid = guid;
            _networkType = networkType;
            _ip = ip;
            _port = port;
            _connectState = ConnectState.Disconnected;
            _msgBodyHelper = msgBodyHelper;
            _msgBodyHelper.Init(MsgBodyErrHandle, MsgBodyHandleCb);
        }

        public void SetCallbacks(NodeConnectCallback connectCallback, NodeDisconnectCallback disconnectCallback)
        {
            _connectCallback = connectCallback;
            _disconnectCallback = disconnectCallback;
        }

        // 设置消息处理回调
        public void SetMessageHandleCallback(MessageHandleCallback callback)
        {
            _messageHandleCallback = callback;
        }

        private void MsgBodyErrHandle(string errorMsg = "")
        {
            Log.Error($"MsgBodyErrHandle nodeId = {_guid} connectState= {_connectState} errorMsg = {errorMsg}");
            if (_connectState != ConnectState.Connected) return;
            _pendingDisconnect = true; // 标记待断开
        }

        private void MsgBodyHandleCb(INetResponse response)
        {
            _messageHandleCallback?.Invoke(_guid, response);
        }

        // 发送消息（Send模式）
        public void SendMessage(INetRequest request)
        {
            var package = _msgBodyHelper.EncodePush(request);
            _conn.SendPackage(package);
        }

        // 发送RPC请求（返回session用于匹配响应）
        public uint SendRpcRequest(INetRequest request)
        {
            var package = (RpcNetPackage)_msgBodyHelper.EncodeRpc(request);
            _conn.SendPackage(package);
            return package.session;
        }

        public void Connect()
        {
            if (_connectState == ConnectState.Connected || _connectState == ConnectState.Connecting)
            {
                return;
            }

            try
            {
                _connectState = ConnectState.Connecting;
                _pendingDisconnect = false; // 重置待断开标记
                _conn = GameModule.Network.CreateNetworkClient(_networkType, PackageBodyMaxSize, _netPackEncoder, _netPackDecoder);
                _conn.Connect(_ip, _port, OnConnectResult);
            }
            catch (Exception ex)
            {
                _connectState = ConnectState.Disconnected;
                _connectCallback?.Invoke(_guid, false, $"连接异常: {ex.Message}");
            }
        }

        public void Reconnect()
        {
            if (_connectState == ConnectState.Connecting || _connectState == ConnectState.Reconnecting)
            {
                return;
            }

            // 重连时先主动断开当前连接
            _pendingDisconnect = true;
            _connectState = ConnectState.Reconnecting;
            _ReconnectAttempts++;
        }

        /// <summary>
        /// 主动断开连接 - 简化：只设置标记，真正断开在Update中处理
        /// </summary>
        public void Disconnect()
        {
            if (_connectState == ConnectState.Disconnected)
            {
                return;
            }
            
            Log.Info($"节点 {_guid} 请求主动断开");
            _pendingDisconnect = true;
        }

        /// <summary>
        /// 内部断开连接方法
        /// </summary>
        /// <param name="isActive">是否为主动断开</param>
        private void DisconnectInternal(bool isActive)
        {
            var oldState = _connectState;

            if (_conn != null)
            {
                try
                {
                    // 断开前快速处理剩余消息
                    if (!_isProcessingMessage && _conn != null)
                    {
                        INetPackage netPack;
                        int count = 0;
                        while ((netPack = _conn.PickPackage()) != null && count < 100)
                        {
                            try
                            {
                                _msgBodyHelper.handleNetPack(netPack);
                                count++;
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"断开前处理消息异常: {ex}");
                            }
                        }
                        
                        if (count > 0)
                        {
                            Log.Info($"节点 {_guid} 断开前处理了 {count} 条剩余消息");
                        }
                    }
                    
                    _msgBodyHelper.ClearAll();
                    _conn.Dispose();
                    _conn = null;
                }
                catch (Exception ex)
                {
                    Log.Error($"断开连接异常: {ex}");
                }
            }

            _connectState = ConnectState.Disconnected;
            _pendingDisconnect = false;

            // 只有在已连接状态下的断开才触发回调
            if (oldState == ConnectState.Connected)
            {
                var disconnectType = isActive ? DisconnectType.Active : DisconnectType.Passive;
                var reason = isActive ? "主动断开连接" : "连接丢失";
                _disconnectCallback?.Invoke(_guid, disconnectType, reason);
            }
        }

        #endregion

        #region 更新逻辑

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 处理重连逻辑
            if (_connectState == ConnectState.Reconnecting)
            {
                // 先执行断开，再重连
                if (_conn != null || _connectState == ConnectState.Connected)
                {
                    DisconnectInternal(false);
                }
                
                Log.Info($"尝试重连节点 {_guid}，第 {_ReconnectAttempts} 次");
                Connect();
                return;
            }

            // 检查连接状态
            if (_conn != null && _connectState == ConnectState.Connected)
            {
                // 1. 先处理消息
                _isProcessingMessage = true;
                try
                {
                    INetPackage netPack;
                    while ((netPack = _conn.PickPackage()) != null)
                    {
                        _msgBodyHelper.handleNetPack(netPack);
                    }
                    _msgBodyHelper.CheckTimeouts();
                }
                catch (Exception ex)
                {
                    Log.Error($"消息处理异常: {ex}");
                }
                finally
                {
                    _isProcessingMessage = false;
                }
                
                // 2. 检查主动断开请求
                if (_pendingDisconnect)
                {
                    Log.Info($"节点 {_guid} 消息处理完成，执行主动断开");
                    DisconnectInternal(true);
                    return;
                }
                
                // 3. 检查被动断线
                if (!_conn.IsConnected())
                {
                    Log.Warning($"节点 {_guid} 检测到连接丢失");
                    DisconnectInternal(false);
                    return;
                }
            }
        }

        #endregion

        #region 回调处理

        private void OnConnectResult(SocketError error)
        {
            bool success = error == SocketError.Success;

            if (success)
            {
                _connectState = ConnectState.Connected;
                _ReconnectAttempts = 0;
                Log.Info($"节点 {_guid} 连接成功");
            }
            else
            {
                _connectState = ConnectState.Disconnected;
                _conn = null;
                Log.Error($"节点 {_guid} 连接失败: {error}");
            }

            _connectCallback?.Invoke(_guid, success, success ? "" : error.ToString());
        }

        #endregion
    }
}

