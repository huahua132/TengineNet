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
        private byte[] _msgBody;
        private MemoryStream _stream;

        public void Init(ushort packId, uint session, byte[] msgbody)
        {
            _PackId = packId;
            _Session = session;
            _msgBody = msgbody;
        }

        public void Init(ushort packId, uint session, MemoryStream stream)
        {
            _PackId = packId;
            _Session = session;
            _stream = stream;
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