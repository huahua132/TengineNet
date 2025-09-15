using System.IO;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 回复错误码委托
    /// </summary>
    public delegate int NetResponseErrCode(INetResponse rsp);

    /// <summary>
    /// 回复错误信息委托
    /// </summary>
    public delegate string NetResponseErrMsg(INetResponse rsp);
    public interface INetResponse
    {
        static public NetResponseErrCode GetRspErrCode;
        static public NetResponseErrMsg GetRspErrMsg;
        ushort _PackId { get; set; }
        uint _Session { get; set; }
        bool _IsError { get; set; }
        bool _IsPush { get; set; }
        int ErrorCode => GetErrorCode();
        string ErrorMsg => GetErrorMsg();
        T GetResponse<T>();
        public int GetErrorCode()
        {
            if (!_IsError) return 0;
            return GetRspErrCode(this);
        }
        public string GetErrorMsg()
        {
            if (!_IsError) return string.Empty;
            return GetRspErrMsg(this);
        }
    }

    public class ProtoBufResponse : INetResponse, IMemory
    {
        public ushort _PackId { get; set; }
        public uint _Session { get; set; }
        public bool _IsError { get; set; }
        public bool _IsPush { get; set; }
        private ProtoBuf.IExtensible _Body;
        private byte[] _msgBody;
        private MemoryStream _stream;
        public void Clear()
        {
            _PackId = 0;
            _Session = 0;
            _IsError = false;
            _IsPush = false;
            _Body = null;
            _msgBody = null;
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        public void Init(ushort packId, uint session, bool isError, bool isPush, byte[] msgbody)
        {
            _PackId = packId;
            _Session = session;
            _msgBody = msgbody;
            _IsError = isError;
            _IsPush = isPush;
        }

        public void Init(ushort packId, uint session, bool isError, bool isPush, MemoryStream stream)
        {
            _PackId = packId;
            _Session = session;
            _stream = stream;
            _IsError = isError;
            _IsPush = isPush;
        }
        public T GetResponse<T>()
        {
            if (_Body != null) return (T)_Body;
            if (_msgBody != null)
            {
                _Body = (ProtoBuf.IExtensible)ProtoBufHelper.FromBytes<T>(_msgBody);
            }
            else
            {
                _Body = (ProtoBuf.IExtensible)ProtoBufHelper.FromStream<T>(_stream);
                _stream.Dispose();
                _stream = null;
            }
            return (T)_Body;
        }
    }
}