using TEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    [System(1)]
    public class LoginSystem : ILoginSystem
    {
        public void OnInit()
        {
            Log.Info("LoginSystem OnInit");
            GameEvent.AddEventListener(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
        }

        public void OnStart()
        {
            Log.Info("LoginSystem OnStart");
        }

        public void OnDestroy()
        {
            Log.Info("LoginSystem OnDestroy");
            GameEvent.RemoveEventListener(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
        }

        private void OnShowLoginUI()
        {
            Log.Info("LoginSystem OnShowLoginUI");
            GameModule.UI.ShowUI<LoginUI>();
        }

        public async UniTask Login(string account, string password)
        {
            HttpLoginReq req = new HttpLoginReq();
            req.account = account;
            req.password = password;
            req.channel = 1;
            httpRsp rsp = await HttpAPI.Request("/user/login", "POST", req);
            if (rsp.code != HttpCode.OK)
            {
                Log.Error($"Login err {rsp.code} {rsp.message}");
                return;
            }
        }
    }
}