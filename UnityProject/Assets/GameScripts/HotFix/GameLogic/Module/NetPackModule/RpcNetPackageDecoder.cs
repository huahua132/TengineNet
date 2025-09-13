using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 网络包解码器。
    /// </summary>
    public class RpcNetPackageDecoder : INetPackageDecoder
    {
        private HandleErrorDelegate _handleErrorCallback;
        private const int HeaderFiledsSize = 8; //包头里其他字段占用8字节
        private const int HeaderMsgBodyLengthFiledSize = 2; //包头里的包体长度（ushort类型）。

        /// <summary>
        /// 获取包头的尺寸。
        /// </summary>
        public int GetPackageHeaderSize()
        {
            return HeaderFiledsSize + HeaderMsgBodyLengthFiledSize;
        }

        /// <summary>
        /// 注册异常错误回调方法。
        /// </summary>
        public void RegisterHandleErrorCallback(HandleErrorDelegate callback)
        {
            _handleErrorCallback = callback;
        }

        /// <summary>
        /// 网络消息解码。
        /// </summary>
        /// <param name="packageBodyMaxSize">包体的最大尺寸。</param>
        /// <param name="ringBuffer">解码需要的字节缓冲区。</param>
        /// <param name="outputPackages">接收的包裹列表。</param>
        public void Decode(int packageBodyMaxSize, RingBuffer ringBuffer, List<INetPackage> outputPackages)
        {
            // 循环解包。
            while (true)
            {
                // 如果数据不够一个包头。
                if (ringBuffer.ReadableBytes < GetPackageHeaderSize())
                    break;
                ringBuffer.MarkReaderIndex();
                // 读取消息包长度
                ushort msgsz = ringBuffer.ReadUShort(ByteOrder.BigEndian);

                // 如果剩余可读数据小于包体长度。
                if (ringBuffer.ReadableBytes < msgsz)
                {
                    ringBuffer.ResetReaderIndex();
                    break; //需要退出读够数据再解包。
                }

                RpcNetPackage package = new RpcNetPackage();
                package.packtype = ringBuffer.ReadByte(); //包类型
                package.msgtype = ringBuffer.ReadByte();  //消息类型
                package.packid = ringBuffer.ReadUShort(ByteOrder.BigEndian); //协议号
                package.session = ringBuffer.ReadUInt(ByteOrder.BigEndian);  //会话号

                int msgBodyLength = msgsz - HeaderFiledsSize;

                // 检测包体长度。
                if (msgBodyLength > packageBodyMaxSize)
                {
                    _handleErrorCallback(true, $"The decode package {package.packid} body size is larger than {packageBodyMaxSize} !");
                    break;
                }

                // 读取包体。
                if ((PACK_TYPE)package.packtype != PACK_TYPE.HEAD)
                {
                    package.msgbody = ringBuffer.ReadBytes(msgBodyLength);
                }
                else
                {
                    package.sz = ringBuffer.ReadUInt(ByteOrder.BigEndian);
                }
                    
                outputPackages.Add(package);
            }

            // 注意：将剩余数据移至起始。
            ringBuffer.DiscardReadBytes();
        }
    }
}