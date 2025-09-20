using System.Collections.Generic;
using System.Reflection;
using GameLogic;
using TEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System;
using Utility = TEngine.Utility;

#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
public partial class GameApp
{
    [Serializable]
    public class LoginReq
    {
        public string account;
        public string password;
        public int channel;
    }
    [Serializable]
    public class LoginRes
    {
        public string token;
        public string host;
        public long player_id;
    }
    [Serializable]
    public class httpRsp
    {
        public int code;
        public LoginRes data;
    }

    private static List<Assembly> _hotfixAssembly;

    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        GameEventHelper.Init();
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        Log.Warning("======= Entrance GameApp =======");
        Utility.Unity.AddDestroyListener(Release);
        StartGameLogic();
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

    private static void OnMessageReceived(uint nodeId, INetResponse response)
    {
        var rsp = response.GetResponse<hallserver_player.PlayerInfoNotice>();
        Log.Info("PlayerInfoNotice >>> {0} {1}", rsp.nickname, rsp);
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

    private static void StartGameLogic()
    {
        ModuleSystem.RegisterModule<ICommonUIModule>(new CommonUIModule());
        ModuleSystem.RegisterModule<INetPackModule>(new NetPackModule());
        ModuleSystem.RegisterModule<ISystemModule>(new SystemModule());

        GameEvent.Get<ILoginUI>().ShowLoginUI();

        // var cts = new CancellationTokenSource();
        // cts.CancelAfterSlim(TimeSpan.FromSeconds(5f));
        // var req = new LoginReq();
        // req.account = "player1";
        // req.password = "●●●●●●";
        // req.channel = 1;
        // string jsonStr = Utility.Json.ToJson(req);
        // byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonStr);
        // UnityWebRequest unityWebRequest = new UnityWebRequest("http://127.0.0.1:11014/user/login", "POST");
        // unityWebRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        // unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
        // unityWebRequest.SetRequestHeader("Content-Type", "application/json");

        // var rspStr = await Utility.Http.SendWebRequest(unityWebRequest, cts);
        // httpRsp httpRsp = Utility.Json.ToObject<httpRsp>(rspStr);
        // var loginRes = httpRsp.data;
        // Log.Info($"rsp Str {rspStr} {httpRsp.data.host}");
        // string host = loginRes.host;
        // var (ip, port) = (host.Split(':')[0], int.Parse(host.Split(':')[1]));
        // var authConfig = new AuthConfig(10000, 3, 3000);
        // GameModule.NetPack.SetAuthRequestProvider(1, (nodeId) =>
        // {
        //     ProtoBufRequest netReq = new ProtoBufRequest();
        //     netReq.PackId = 201;
        //     var loginReq = new login.LoginReq();
        //     loginReq.player_id = loginRes.player_id;
        //     loginReq.token = loginRes.token;
        //     netReq.MsgBody = loginReq;
        //     return netReq;
        // }, authConfig);

        // GameModule.NetPack.SetHeartbeatRequestProvider(1, (nodeId) =>
        // {
        //     ProtoBufRequest netReq = new ProtoBufRequest();
        //     netReq.PackId = 203;
        //     var heartReq = new login.HeartReq();
        //     heartReq.time = DateTime.Now.Second;
        //     netReq.MsgBody = heartReq;
        //     return netReq;
        // });
        // INetResponse.GetRspErrCode = NetResponseErrCode;
        // INetResponse.GetRspErrMsg = NetResponseErrMsg;
        // GameModule.NetPack.RegisterConnectCallback(ConnectCallback);
        // GameModule.NetPack.RegisterDisconnectCallback(DisconnectCallback);
        // GameModule.NetPack.RegisterMessageListener(10180, OnMessageReceived);
        // GameModule.NetPack.Connect(1, NetworkType.WebSocket, "ws://127.0.0.1", port, new ProtoBufMsgBodyHelper());
    }

    private static void Release()
    {
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}