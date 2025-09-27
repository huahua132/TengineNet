using TEngine;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    public enum NetNodeID
    {
        hall = 1,               //大厅
        game = 2,               //游戏
    }
    
    public class NetProxy : INetProxy
    {
        private NetNodeID _nodeID;
        private string _host;
        private int _port;
        private long _playerId;
        private string _token;
        private ProtoBufMsgBodyHelper _bufHelper;
        
        public NetProxy(NetNodeID nodeID)
        {
            _nodeID = nodeID;
            _bufHelper = new ProtoBufMsgBodyHelper();
            
            // 设置认证配置
            var authConfig = new AuthConfig(10000, 3, 3000);
            GameModule.NetPack.SetAuthRequestProvider((uint)_nodeID, (nodeId) =>
            {
                ProtoBufRequest netReq = new ProtoBufRequest();
                netReq.PackId = login.MessageId.LoginReq;
                var loginReq = new login.LoginReq();
                loginReq.player_id = _playerId;
                loginReq.token = _token;
                netReq.MsgBody = loginReq;
                return netReq;
            }, authConfig);

            // 设置心跳配置
            var heartbeatConfig = new HeartbeatConfig();
            GameModule.NetPack.SetHeartbeatRequestProvider((uint)_nodeID, (nodeId) =>
            {
                ProtoBufRequest netReq = new ProtoBufRequest();
                netReq.PackId = login.MessageId.HeartReq;
                var heartReq = new login.HeartReq();
                heartReq.time = DateTime.Now.Second;
                netReq.MsgBody = heartReq;
                return netReq;
            }, heartbeatConfig);
            
            // 设置错误码和错误消息获取方法
            INetResponse.GetRspErrCode = NetResponseErrCode;
            INetResponse.GetRspErrMsg = NetResponseErrMsg;
            
            // 注册连接和断线回调
            GameModule.NetPack.RegisterConnectCallback((uint)_nodeID, ConnectCallback);
            GameModule.NetPack.RegisterDisconnectCallback((uint)_nodeID, DisconnectCallback);
            
            // 注册认证回调
            GameModule.NetPack.RegisterAuthSuccessCallback((uint)_nodeID, AuthSuccessCallback);
            GameModule.NetPack.RegisterAuthFailureCallback((uint)_nodeID, AuthFailureCallback);
        }

        #region 连接相关回调

        private void ReconnectTimeOut(object[] args)
        {
            int attempts = GameModule.NetPack.GetReconnectAttempts((uint)_nodeID);
            GameModule.CommonUI.ShowToast($"重试重连中第{attempts}次");
            GameModule.NetPack.Reconnect((uint)_nodeID);
        }

        private void OnBtnConfirm()
        {
            GameModule.NetPack.Reconnect((uint)_nodeID);
        }

        private void OnBtnCancel()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ConnectCallback(bool success, string errorMsg = "")
        {
            if (!success)
            {
                int attempts = GameModule.NetPack.GetReconnectAttempts((uint)_nodeID);
                if (attempts % 3 == 0)
                {
                    GameModule.Timer.AddTimer(ReconnectTimeOut, 0.1f, false);
                }
                else
                {
                    GameModule.CommonUI.ShowConfirm("网络连接异常", "是否尝试重新连接", OnBtnConfirm, OnBtnCancel, "重试", "退出");
                }
            }
            else
            {
                GameModule.CommonUI.ShowToast($"连接服务器成功 结点{_nodeID}");
            }
        }

        private void DisconnectCallback(DisconnectType disconnectType, string reason = "")
        {
            GameModule.CommonUI.ShowToast($"网络断开连接 结点{_nodeID}: {reason}");
            GameModule.NetPack.Close((uint)_nodeID);
        }

        #endregion

        #region 认证相关回调

        private void AuthSuccessCallback(INetResponse response)
        {
            GameModule.CommonUI.ShowToast($"认证成功 结点{_nodeID}");
            Log.Info($"节点 {_nodeID} 认证成功");
            
            // 认证成功后的处理，比如获取用户信息等
            OnAuthSuccess(response);
        }

        private void AuthFailureCallback(INetResponse response)
        {
            var errorMsg = $"认证失败 结点{_nodeID}: {response.ErrorMsg}";
            GameModule.CommonUI.ShowToast(errorMsg);
            Log.Error($"节点 {_nodeID} 认证失败: 错误码={response.ErrorCode}, 错误信息={response.ErrorMsg}");
            
            // 认证失败后的处理
            OnAuthFailure(response);
        }

        /// <summary>
        /// 认证成功后的处理（子类可重写）
        /// </summary>
        protected virtual void OnAuthSuccess(INetResponse response)
        {
            // 具体的认证成功处理逻辑，子类可以重写
        }

        /// <summary>
        /// 认证失败后的处理（子类可重写）
        /// </summary>
        protected virtual void OnAuthFailure(INetResponse response)
        {
            // 具体的认证失败处理逻辑，子类可以重写
        }

        #endregion

        #region 错误码处理

        private static int NetResponseErrCode(INetResponse response)
        {
            errors.Error err = response.GetResponse<errors.Error>();
            return err.code;
        }

        private static string NetResponseErrMsg(INetResponse response)
        {
            errors.Error err = response.GetResponse<errors.Error>();
            return err.msg;
        }

        #endregion

        #region 连接管理

        public void SetConnect(string host, int port, long playerId, string token)
        {
            _host = host;
            _port = port;
            _playerId = playerId;
            _token = token;
            GameModule.NetPack.Connect((uint)_nodeID, NetworkType.WebSocket, host, port, _bufHelper);
        }

        public void Close()
        {
            GameModule.NetPack.Close((uint)_nodeID);
        }

        #endregion

        #region 认证回调管理

        /// <summary>
        /// 注册认证成功回调
        /// </summary>
        /// <param name="callback">认证成功回调</param>
        public void RegisterAuthSuccessCallback(AuthSuccessCallback callback)
        {
            GameModule.NetPack.RegisterAuthSuccessCallback((uint)_nodeID, callback);
        }

        /// <summary>
        /// 注销认证成功回调
        /// </summary>
        /// <param name="callback">认证成功回调</param>
        public void UnregisterAuthSuccessCallback(AuthSuccessCallback callback)
        {
            GameModule.NetPack.UnregisterAuthSuccessCallback((uint)_nodeID, callback);
        }

        /// <summary>
        /// 注册认证失败回调
        /// </summary>
        /// <param name="callback">认证失败回调</param>
        public void RegisterAuthFailureCallback(AuthFailureCallback callback)
        {
            GameModule.NetPack.RegisterAuthFailureCallback((uint)_nodeID, callback);
        }

        /// <summary>
        /// 注销认证失败回调
        /// </summary>
        /// <param name="callback">认证失败回调</param>
        public void UnregisterAuthFailureCallback(AuthFailureCallback callback)
        {
            GameModule.NetPack.UnregisterAuthFailureCallback((uint)_nodeID, callback);
        }

        #endregion

        #region 消息监听管理

        /// <summary>
        /// 注册消息监听器
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">消息回调</param>
        public void RegisterMessageListener(ushort packId, MessageCallback callback)
        {
            GameModule.NetPack.RegisterMessageListener((uint)_nodeID, packId, callback);
        }

        /// <summary>
        /// 注销消息监听器
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">消息回调</param>
        public void UnregisterMessageListener(ushort packId, MessageCallback callback)
        {
            GameModule.NetPack.UnregisterMessageListener((uint)_nodeID, packId, callback);
        }

        /// <summary>
        /// 注册消息监听器（泛型版本）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="packId">消息包ID</param>
        /// <param name="callback">消息回调</param>
        public void RegisterMessageListener<T>(ushort packId, Action<T> callback) where T : class
        {
            RegisterMessageListener(packId, (response) =>
            {
                try
                {
                    var msg = response.GetResponse<T>();
                    callback?.Invoke(msg);
                }
                catch (Exception ex)
                {
                    Log.Error($"消息解析异常 PackId={packId}: {ex}");
                }
            });
        }

        #endregion

        #region 消息发送

        /// <summary>
        /// 发送消息（Send模式，不等待响应）
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <returns>是否发送成功</returns>
        public bool SendMessage(INetRequest request)
        {
            return GameModule.NetPack.SendMessage((uint)_nodeID, request);
        }

        /// <summary>
        /// 发送ProtoBuf消息（Send模式）
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <returns>是否发送成功</returns>
        public bool SendProtoBufMessage(ushort packId, ProtoBuf.IExtensible msgBody)
        {
            var request = new ProtoBufRequest
            {
                PackId = packId,
                MsgBody = msgBody
            };
            return SendMessage(request);
        }

        /// <summary>
        /// 发送RPC请求（等待响应）
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>响应对象</returns>
        public async UniTask<INetResponse> SendRpcRequest(INetRequest request, int timeoutMs = 5000)
        {
            return await GameModule.NetPack.SendRpcRequest((uint)_nodeID, request, timeoutMs);
        }

        /// <summary>
        /// 发送ProtoBuf RPC请求
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>响应对象</returns>
        public async UniTask<INetResponse> SendProtoBufRpcRequest(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 5000)
        {
            var request = new ProtoBufRequest
            {
                PackId = packId,
                MsgBody = msgBody
            };
            return await SendRpcRequest(request, timeoutMs);
        }

        /// <summary>
        /// 发送ProtoBuf RPC请求（泛型版本）
        /// </summary>
        /// <typeparam name="T">响应消息类型</typeparam>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>解析后的响应对象</returns>
        public async UniTask<T> SendProtoBufRpcRequest<T>(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 5000) where T : class
        {
            var response = await SendProtoBufRpcRequest(packId, msgBody, timeoutMs);
            
            if (response._IsError)
            {
                throw new Exception($"RPC请求失败: 错误码={response.ErrorCode}, 错误信息={response.ErrorMsg}");
            }
            
            return response.GetResponse<T>();
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        public ConnectState GetConnectState()
        {
            return GameModule.NetPack.GetConnectState((uint)_nodeID);
        }

        /// <summary>
        /// 是否已认证
        /// </summary>
        /// <returns>是否已认证</returns>
        public bool IsAuthenticated()
        {
            return GameModule.NetPack.IsNodeAuthenticated((uint)_nodeID);
        }

        /// <summary>
        /// 是否已连接且已认证
        /// </summary>
        /// <returns>是否可用</returns>
        public bool IsReady()
        {
            return GetConnectState() == ConnectState.Connected && IsAuthenticated();
        }

        #endregion

        #region 析构清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            // 注销所有回调
            GameModule.NetPack.UnregisterConnectCallback((uint)_nodeID, ConnectCallback);
            GameModule.NetPack.UnregisterDisconnectCallback((uint)_nodeID, DisconnectCallback);
            GameModule.NetPack.UnregisterAuthSuccessCallback((uint)_nodeID, AuthSuccessCallback);
            GameModule.NetPack.UnregisterAuthFailureCallback((uint)_nodeID, AuthFailureCallback);
            
            // 关闭连接
            Close();
        }

        #endregion
    }
}

