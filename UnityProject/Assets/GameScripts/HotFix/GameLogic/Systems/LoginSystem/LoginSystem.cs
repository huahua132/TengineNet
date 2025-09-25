using TEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    [System(1)]
    public class LoginSystem : ILoginSystem
    {
        public void OnInit()
        {
            GameEvent.AddEventListener(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
        }

        public void OnStart()
        {
        }

        public void OnDestroy()
        {
            GameEvent.RemoveEventListener(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
        }

        private void OnShowLoginUI()
        {
            GameModule.UI.ShowUI<LoginUI>();
        }

        public async UniTask Login(string account, string password)
        {
            //GameModule.CommonUI.ShowToast($"LoginSystem OnStart {GameTime.time}");
            HttpLoginReq req = new HttpLoginReq();
            req.account = account;
            req.password = password;
            var rsp = await HttpAPI.Request<HttpLoginRes>("/user/login", "POST", req);
            if (rsp.code == HttpCode.DOBILE_REQ) return;
            if (rsp.code != HttpCode.OK)
            {
                GameModule.CommonUI.ShowToast($"网络错误 请求登录失败 {rsp.code} {rsp.message}");
                Log.Error($"Login err {rsp.code} {rsp.message}");
                return;
            }
            else
            {
                Log.Info($" httpRsp >>> {rsp.code}   {rsp.message} {rsp.data}");
                HttpLoginRes HttpLoginRes = rsp.data;
                Log.Info($"Login res {HttpLoginRes.player_id}  {HttpLoginRes.host} {HttpLoginRes.token}");
                var host = HttpLoginRes.host;
                var (ip, port) = (host.Split(':')[0], int.Parse(host.Split(':')[1]));
                GameModule.NetHall.SetConnect("ws://" + ip, port, HttpLoginRes.player_id, HttpLoginRes.token);
            }
        }

        public async UniTask SignUp(string account, string password)
        {
            HttpSignUpReq req = new HttpSignUpReq();
            req.account = account;
            req.password = password;
            req.channel = 1;
            var rsp = await HttpAPI.Request<HttpSignUpRes>("/user/signup", "POST", req);
            if (rsp.code != HttpCode.OK)
            {
                GameModule.CommonUI.ShowToast($"网络错误 请求注册失败 {rsp.code} {rsp.message}");
                Log.Error($"SignUp err {rsp.code} {rsp.message}");
                return;
            }
            else
            {
                GameModule.CommonUI.ShowToast("注册成功!");
            }
        }
    }
}