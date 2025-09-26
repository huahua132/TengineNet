using TEngine;
using System;
using UnityEngine;

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
            var authConfig = new AuthConfig(10000, 3, 3000);
            GameModule.NetPack.SetAuthRequestProvider((uint)_nodeID, (nodeId) =>
            {
                ProtoBufRequest netReq = new ProtoBufRequest();
                netReq.PackId = 201;
                var loginReq = new login.LoginReq();
                loginReq.player_id = _playerId;
                loginReq.token = _token;
                netReq.MsgBody = loginReq;
                return netReq;
            }, authConfig);

            var heartbeatConfig = new HeartbeatConfig();
            GameModule.NetPack.SetHeartbeatRequestProvider((uint)_nodeID, (nodeId) =>
            {
                ProtoBufRequest netReq = new ProtoBufRequest();
                netReq.PackId = 203;
                var heartReq = new login.HeartReq();
                heartReq.time = DateTime.Now.Second;
                netReq.MsgBody = heartReq;
                return netReq;
            }, heartbeatConfig);
            INetResponse.GetRspErrCode = NetResponseErrCode;
            INetResponse.GetRspErrMsg = NetResponseErrMsg;
            GameModule.NetPack.RegisterConnectCallback(ConnectCallback);
            GameModule.NetPack.RegisterDisconnectCallback(DisconnectCallback);
        }

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

        private void ConnectCallback(uint nodeId, bool success, string errorMsg = "")
        {
            if (!success)
            {
                int attempts = GameModule.NetPack.GetReconnectAttempts(nodeId);
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

        private void DisconnectCallback(uint nodeId, DisconnectType disconnectType, string reason = "")
        {
            GameModule.NetPack.Close(nodeId);
        }

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
    }
}