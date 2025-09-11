namespace GameLogic
{
    public interface INetRequst {}

    public class ProtoBufRequst : INetRequst {
        public ProtoBuf.IExtensible MsgBody;
    }
}