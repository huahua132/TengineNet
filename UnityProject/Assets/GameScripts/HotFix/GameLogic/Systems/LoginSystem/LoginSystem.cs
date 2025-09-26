using TEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    [System((int)SystemPriority.Login)]
    public class LoginSystem : ILoginSystem
    {
        public void OnInit()
        {
            GameModule.System.AddEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
            GameModule.System.AddEvent(ILoginUI_Event.CloseLoginUI, OnCloseLoginUI);
            GameModule.NetHall.RegisterAuthSuccessCallback(OnHallLoginAuthSuccess);
            GameModule.NetHall.RegisterAuthFailureCallback(OnHallLoginAuthFailed);
        }

        public void OnStart()
        {
        }

        public void OnDestroy()
        {
            GameModule.NetHall.UnregisterAuthSuccessCallback(OnHallLoginAuthSuccess);
            GameModule.NetHall.UnregisterAuthFailureCallback(OnHallLoginAuthFailed);
        }

        //大厅登录认证成功
        private void OnHallLoginAuthSuccess(INetResponse response)
        {
            login.LoginRes loginRes = response.GetResponse<login.LoginRes>();
            GameModule.CommonUI.ShowToast($"登录认证成功 是否 重连 {loginRes.isreconnect}");
            bool isreconnect = loginRes.isreconnect == 1 ? true : false;
            GameEvent.Get<ILoginLogic>().HallLoginAuthSuccess(isreconnect);
        }

        //大厅登录认证失败
        private void OnHallLoginAuthFailed(INetResponse response)
        {
            GameModule.CommonUI.ShowAlert("登录认证失败", $"code={response.ErrorCode} msg={response.ErrorMsg}");
            GameEvent.Get<ILoginLogic>().HallLoginAuthFailed();
        }

        private void OnShowLoginUI()
        {
            GameModule.UI.ShowUI<LoginUI>();
        }

        private void OnCloseLoginUI()
        {
            GameModule.UI.CloseUI<LoginUI>();
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