namespace GameLogic
{
    public interface INetRequest
    {
        ushort PackId { get; set; }
    }

    public class ProtoBufRequest : INetRequest {
        public ushort PackId { get; set; } = 0;
        public ProtoBuf.IExtensible MsgBody;
    }
}