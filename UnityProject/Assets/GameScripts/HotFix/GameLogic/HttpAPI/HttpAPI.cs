using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System;
using Utility = TEngine.Utility;

namespace GameLogic
{
    public enum HttpCode
    {
        OK = 20000,
        ILLEGAL_TOKEN = 50008,        //无效token
        OTHER_LOGINED = 50012,        //重复登录
        TOKEN_EXPIRED = 50014,        //token过期
        NOT_USER = 50016,             //用户不存在
        ERR_PASSWORD = 50018,         //密码错误
        ERR_SERVER = 50020,           //服务器出错
        NOT_PERMISSION = 50022,       //没有权限
        NOT_HANDSHAKE = 50023,        //还没有握手
        ERR_PARAM = 50033,            //参数错误
        EXISTS_USER = 50034,          //用户已存在
        SERVER_BUZY = 50035,          //服务器繁忙
        ACCOUNT_LEN = 50036,          //账号长度不符合要求
        SERVER_CLOSE = 50037,         //已关服
    }
    public interface HttpData
    {

    }

    [Serializable]
    public class httpRsp
    {
        public HttpCode code;
        public string message;
        public HttpData data;
    }

    [Serializable]
    public class HttpLoginReq
    {
        public string account;
        public string password;
        public int channel;
    }

    [Serializable]
    public class HttpLoginRes : HttpData
    {
        public string token;
        public string host;
        public long player_id;
    }
    public class HttpAPI
    {
        static httpRsp NULLRESULT = new httpRsp();
        public static async UniTask<httpRsp> Request(string url, string method, object reqData)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(10f));
            string jsonStr = Utility.Json.ToJson(reqData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonStr);
            var host = ConfigSystem.Instance.Tables.TbAppConfig.ServerHost + url;
            UnityWebRequest unityWebRequest = new UnityWebRequest(host, method);
            unityWebRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            unityWebRequest.SetRequestHeader("Content-Type", "application/json");

            var rspStr = await Utility.Http.SendWebRequest(unityWebRequest, cts);
            if (rspStr == string.Empty)
            {
                return NULLRESULT;
            }

            return Utility.Json.ToObject<httpRsp>(rspStr);
        }
    }
}