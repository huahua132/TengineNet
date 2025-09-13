using System.IO;
using TEngine;

namespace GameLogic
{
    public interface INetResponse
    {
        ushort _PackId { get; set; }
        uint _Session { get; set; }
        bool _IsError { get; set; }
        bool _IsPush { get; set; }
        T GetResponse<T>();
    }

    public class ProtoBufResponse : INetResponse
    {
        public ushort _PackId { get; set; }
        public uint _Session { get; set; }
        public bool _IsError { get; set; }
        public bool _IsPush { get; set; }
        private byte[] _msgBody;
        private MemoryStream _stream;

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
            if (_msgBody != null)
            {
                return ProtoBufHelper.FromBytes<T>(_msgBody);
            }
            else
            {
                return ProtoBufHelper.FromStream<T>(_stream);
            }
        }
    }
}