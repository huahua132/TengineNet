using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using Utility = TEngine.Utility;
using TEngine;

namespace GameLogic
{
    public enum HttpCode
    {
        DOBILE_REQ = -1,              //重复请求
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

    public interface IHttpData
    {

    }

    // 新增：泛型版本的httpRsp
    [Serializable]
    public class httpRsp<T> where T : IHttpData
    {
        public HttpCode code;
        public string message;
        public T data;
    }

    // 保留：原有的httpRsp（向后兼容）
    [Serializable]
    public class httpRsp
    {
        public HttpCode code;
        public string message;
        public string data;

        public T GetData<T>() where T : IHttpData
        {
            return Utility.Json.ToObject<T>(data);
        }
    }

    [Serializable]
    public class HttpLoginReq
    {
        public string account;
        public string password;
    }

    [Serializable]
    public class HttpSignUpReq
    {
        public string account;
        public string password;
        public int channel;
    }

    [Serializable]
    public class HttpLoginRes : IHttpData
    {
        public string token;
        public string host;
        public long player_id;
    }
    
    [Serializable]
    public class HttpSignUpRes : IHttpData
    {
        public string account;
        public string password;
        public int channel;
    }

    public class HttpAPI
    {
        private static Dictionary<string, bool> doubleCheck = new();

        public static async UniTask<httpRsp<T>> Request<T>(string url, string method, object reqData, bool isDoubleCheck = true)
            where T : IHttpData
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

            if (isDoubleCheck)
            {
                if (doubleCheck.TryGetValue(url, out var b))
                {
                    GameModule.CommonUI.ShowToast("请求中，请稍等！");
                    // 返回一个空的泛型结果
                    return new httpRsp<T> { code = HttpCode.DOBILE_REQ, message = "重复请求" };
                }
                doubleCheck[url] = true;
            }

            var rspStr = await Utility.Http.SendWebRequest(unityWebRequest, cts);
            if (isDoubleCheck)
            {
                doubleCheck.Remove(url);
            }
            if (rspStr == string.Empty)
            {
                return new httpRsp<T> { code = HttpCode.ERR_SERVER, message = "网络错误" };
            }

            // 直接反序列化为泛型类型，一次性完成
            return Utility.Json.ToObject<httpRsp<T>>(rspStr);
        }
    }
}

