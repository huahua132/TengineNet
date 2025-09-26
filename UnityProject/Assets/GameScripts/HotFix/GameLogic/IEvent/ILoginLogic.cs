using TEngine;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ILoginLogic
    {
        //大厅登录认证成功
        void HallLoginAuthSuccess(bool isReconnect);
        //大厅登录认证失败
        void HallLoginAuthFailed();
        //游戏登录认证成功
        void GameLoginAuthSuccess(bool isReconnect);
        //游戏登录认证失败
        void GameLoginAuthFailed();
    }
}