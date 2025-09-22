using TEngine;
using System;

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

        private static void ConnectCallback(uint nodeId, bool success, string errorMsg = "")
        {
            Log.Info($"ConnectCallback {nodeId} {success} {errorMsg}");
            if (!success)
            {
                int attempts = GameModule.NetPack.GetReconnectAttempts(nodeId);
                if (attempts <= 3)
                {
                    GameModule.NetPack.Reconnect(nodeId);
                }
            }
        }

        private static void DisconnectCallback(uint nodeId, DisconnectType disconnectType, string reason = "")
        {
            Log.Info($"ConnectCallback {nodeId} {disconnectType} {reason}");
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
    }
}