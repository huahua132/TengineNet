using System.Collections.Generic;
using System.Reflection;
using GameLogic;
using TEngine;
#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
public partial class GameApp
{
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

    private static void ConnectCallback(ulong nodeId, bool success, string errorMsg = "")
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

    private static void DisconnectCallback(ulong nodeId, DisconnectType disconnectType, string reason = "")
    {
        Log.Info($"ConnectCallback {nodeId} {disconnectType} {reason}");
        GameModule.NetPack.Close(nodeId);
    }

    private static void StartGameLogic()
    {
        GameEvent.Get<ILoginUI>().ShowLoginUI();
        GameModule.UI.ShowUIAsync<BattleMainUI>();
        ModuleSystem.RegisterModule<INetPackModule>(new NetPackModule());
        GameModule.NetPack.RegisterConnectCallback(ConnectCallback);            
        GameModule.NetPack.RegisterDisconnectCallback(DisconnectCallback);
        GameModule.NetPack.Connect(1, NetworkType.WebSocket, "ws://127.0.0.1", 11012);
    }
    
    private static void Release()
    {
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}