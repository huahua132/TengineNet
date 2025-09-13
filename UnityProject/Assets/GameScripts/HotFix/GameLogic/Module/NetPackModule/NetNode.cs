using System;
using System.Net.Sockets;
using TEngine;

namespace GameLogic
{
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
        private ConnectCallback _connectCallback;
        private DisconnectCallback _disconnectCallback;
        public int _ReconnectAttempts { get; private set; } = 0;
        private bool _isActiveDisconnect = false; // 标记是否为主动断开

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
            _isActiveDisconnect = false;
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
        public void Init(uint guid, NetworkType networkType, string ip, int port)
        {
            _guid = guid;
            _networkType = networkType;
            _ip = ip;
            _port = port;
            _connectState = ConnectState.Disconnected;
            _isActiveDisconnect = false;
        }

        public void SetCallbacks(ConnectCallback connectCallback, DisconnectCallback disconnectCallback)
        {
            _connectCallback = connectCallback;
            _disconnectCallback = disconnectCallback;
        }

        // 新增：设置消息处理回调
        public void SetMessageHandleCallback(MessageHandleCallback callback)
        {
            _messageHandleCallback = callback;
        }

        private void MsgBodyErrHandle(string errorMsg = "")
        {
            Log.Error($"MsgBodyErrHandle nodeId = {_guid} connectState= {_connectState} errorMsg = {errorMsg}");
            if (_connectState != ConnectState.Connected) return;
            DisconnectInternal(true);
        }

        private void MsgBodyHandleCb(INetResponse response)
        {
            _messageHandleCallback?.Invoke(_guid, response);
        }

        // 新增：发送消息（Send模式）
        public void SendMessage(INetRequest request)
        {
            var package = _msgBodyHelper.EncodePush(request);
            _conn.SendPackage(package);
        }

        // 新增：发送RPC请求（返回session用于匹配响应）
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
                _msgBodyHelper = new ProtoBufMsgBodyHelper();
                _msgBodyHelper.Init(MsgBodyErrHandle, MsgBodyHandleCb);
                _connectState = ConnectState.Connecting;
                _isActiveDisconnect = false; // 重置主动断开标记
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
            DisconnectInternal(false);
            _connectState = ConnectState.Reconnecting;
            _ReconnectAttempts++;
        }

        /// <summary>
        /// 主动断开连接
        /// </summary>
        public void Disconnect()
        {
            DisconnectInternal(true);
        }

        /// <summary>
        /// 内部断开连接方法
        /// </summary>
        /// <param name="triggerCallback">是否触发断线回调</param>
        private void DisconnectInternal(bool triggerCallback)
        {
            var oldState = _connectState;

            if (_conn != null)
            {
                try
                {
                    _conn.Dispose();
                    _conn = null;
                }
                catch (Exception ex)
                {
                    Log.Error($"断开连接异常: {ex}");
                }
            }

            _connectState = ConnectState.Disconnected;

            // 只有在已连接状态下的断开才触发回调
            if (triggerCallback && oldState == ConnectState.Connected)
            {
                _isActiveDisconnect = true;
                _disconnectCallback?.Invoke(_guid, DisconnectType.Active, "主动断开连接");
            }
        }

        #endregion

        #region 更新逻辑

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 处理重连逻辑
            if (_connectState == ConnectState.Reconnecting)
            {
                Log.Info($"尝试重连节点 {_guid}，第 {_ReconnectAttempts} 次");
                Connect();
            }

            // 检查连接状态
            if (_conn != null && _connectState == ConnectState.Connected)
            {
                if (!_conn.IsConnected())
                {
                    OnConnectionLost("连接丢失");
                }
                else
                {
                    INetPackage netPack = _conn.PickPackage();
                    if (netPack != null)
                    {
                        _msgBodyHelper.handleNetPack(netPack);
                    }
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
                _ReconnectAttempts = 0; // 重置重连计数
                _isActiveDisconnect = false; // 重置主动断开标记
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

        private void OnConnectionLost(string reason)
        {
            if (_connectState == ConnectState.Connected && !_isActiveDisconnect)
            {
                _connectState = ConnectState.Disconnected;
                _conn = null;
                Log.Warning($"节点 {_guid} 被动断线: {reason}");
                _disconnectCallback?.Invoke(_guid, DisconnectType.Passive, reason);
            }
        }

        #endregion
    }
}

