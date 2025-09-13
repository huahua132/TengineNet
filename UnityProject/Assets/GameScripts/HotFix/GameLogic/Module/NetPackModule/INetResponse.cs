using System.IO;
using TEngine;

namespace GameLogic
{
    public interface INetResponse
    {
        T GetResponse<T>();
    }

    public class ProtoBufResponse : INetResponse
    {
        public ushort _PackId { get; private set; }
        public uint _Session { get; private set; }
        public bool _IsError { get; private set; }
        public bool _IsPush { get; private set; }
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