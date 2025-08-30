using TEngine;
namespace GameLogic
{
    public enum PACK_TYPE
    {
        WHOLE = 0,  //整包
        HEAD = 1,  //包头
        BODY = 2,  //包体
        TAIL = 3,  //包尾
    }
    public enum MSG_TYPE
    {
        SERVER_PUSH = 0,
        CLIENT_PUSH = 1,
        CLIENT_REQ = 2,
        SERVER_RSP = 3,
        SERVER_ERR = 4,
    }
    /// <summary>
    /// 对接skynet_fly rpc 数据包实现。https://github.com/huahua132/skynet_fly
    /// 用于在网络通信中封装基础消息结构。
    /// </summary>
    /// <remarks>
    /// 实现 <see cref="INetPackage"/> 接口，提供消息ID与二进制载荷的标准容器。
    /// 通常配合编解码器（Encoder/Decoder）进行序列化与反序列化操作。
    /// </remarks>
    public class RpcNetPackage : INetPackage
    {
        /// <summary>
        /// 包类型，字段描述: (0-整包  1包头  2包体  3包尾)
        /// </summary>
        public byte packtype { set; get; }
        /// <summary>
        /// 消息类型，字段描述: (0-服务端推送 1-客户端推送 2-客户端请求 3-服务端回复 4-服务器回复错误)
        /// </summary>
        public byte msgtype { set; get; }
        /// <summary>
        /// 协议码
        /// </summary>
        public ushort packid { set; get; }
        /// <summary>
        /// 会话号 字段描述: (服务端推送时用于标识同一包体，客户端推送为0即可(不能发送大包)，客户端请求(奇数)达到(4,294,967,295)时客户端应该直接切换到1,避免服务端使用溢出后的0进行回复, 服务端回复，服务器回复错误(偶数,奇数基础上1))
        /// </summary>
        public uint session { set; get; }

        /// <summary>
        /// 消息内容 字段描述：包头时为4字节的消息内容长度
        /// </summary>
        public byte[] msgbody { set; get; }
    }
}