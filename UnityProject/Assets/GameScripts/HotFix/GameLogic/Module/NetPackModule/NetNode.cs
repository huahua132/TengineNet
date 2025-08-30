using System.Net.Sockets;
using TEngine;

namespace GameLogic
{
    internal class NetNodeIDGenerator
    {
        private static ulong _incrID = 0;
        public static ulong Next()
        {
            _incrID++;
            return _incrID;
        }
    }
    //网络节点
    public class NetNode : IMemory
    {
        public void Clear()
        {
            _Guid = 0;
            _NetWorkType = 0;
            _Ip = "";
            _Port = 0;
        }

        public static readonly int PackageBodyMaxSize = ushort.MaxValue - 8;
        public ulong _Guid { get; private set; }        //唯一ID
        public NetworkType _NetWorkType { get; private set; }

        private static readonly RpcNetPackageEncoder _netPackEncoder = new RpcNetPackageEncoder();
        private static readonly RpcNetPackageDecoder _netPackDecoder = new RpcNetPackageDecoder();
        private AClient _conn;                         //网络连接
        public string _Ip { get; private set; }
        public int _Port { get; private set; }

        public void Init(NetworkType networkType, string ip, int port)
        {
            _Guid = NetNodeIDGenerator.Next();
            _NetWorkType = networkType;
            _Ip = ip;
            _Port = port;
        }

        public void Connect()
        {
            _conn = GameModule.Network.CreateNetworkClient(_NetWorkType, PackageBodyMaxSize, _netPackEncoder, _netPackDecoder);
            _conn.Connect(_Ip, _Port, ConnectCallBack);
        }

        public void Reconnect()
        {
            _conn = GameModule.Network.CreateNetworkClient(_NetWorkType, PackageBodyMaxSize, _netPackEncoder, _netPackDecoder);
            _conn.Connect(_Ip, _Port, ConnectCallBack);
        }

        private void ConnectCallBack(SocketError error)
        {

        }
    }
}