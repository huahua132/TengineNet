namespace GameLogic
{
    public interface INetProxy
    {
        //设置连接
        void SetConnect(string host, int port, long playerId, string token);
        //关闭连接
        void Close();
    }
}