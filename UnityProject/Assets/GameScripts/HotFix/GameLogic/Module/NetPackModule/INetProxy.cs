namespace GameLogic
{
    public interface INetProxy
    {
        void SetConnect(string host, int port, long playerId, string token);
    }
}