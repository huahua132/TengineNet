using System.Diagnostics;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 网络包编码器。
    /// </summary>
    public class RpcNetPackageEncoder : INetPackageEncoder
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
        /// 编码。
        /// </summary>
        /// <param name="packageBodyMaxSize">包体的最大尺寸。</param>
        /// <param name="ringBuffer">编码填充的字节缓冲区。</param>
        /// <param name="encodePackage">发送的包裹。</param>
        public void Encode(int packageBodyMaxSize, RingBuffer ringBuffer, INetPackage encodePackage)
        {
            if (encodePackage == null)
            {
                _handleErrorCallback(false, "The encode package object is null");
                return;
            }

            RpcNetPackage package = (RpcNetPackage)encodePackage;
            if (package == null)
            {
                _handleErrorCallback(false, $"The encode package object is invalid : {encodePackage.GetType()}");
                return;
            }

            // 检测逻辑是否合法。
            if (package.msgtype != (byte)MSG_TYPE.CLIENT_REQ && package.msgtype != (byte)MSG_TYPE.CLIENT_PUSH)
            {
                _handleErrorCallback(false, $"The encode package msgtype field is invalid : {package.msgtype}");
                return;
            }
            // 客户端必须发送整包
            if (package.packtype != (byte)PACK_TYPE.WHOLE)
            {
                _handleErrorCallback(false, $"The encode package packtype field is invalid : {package.packtype}");
                return;
            }

            // 获取包体数据。
            byte[] bodyData = package.msgbody;

            // 检测包体长度.
            if (bodyData.Length > packageBodyMaxSize)
            {
                _handleErrorCallback(false, $"The encode package {package.packid} body size is larger than {packageBodyMaxSize}");
                return;
            }
            //写入消息总长度 
            ringBuffer.WriteUShort((ushort)(HeaderFiledsSize + bodyData.Length), ByteOrder.BigEndian);
            // 写入包类型
            ringBuffer.WriteByte(package.packtype);
            // 写入消息类型
            ringBuffer.WriteByte(package.msgtype);
            // 写入消息ID
            ringBuffer.WriteUShort(package.packid, ByteOrder.BigEndian);
            // 写入session
            ringBuffer.WriteUInt(package.session, ByteOrder.BigEndian);
            //Log.Info($" {package.packtype} {package.msgtype} {package.packid} {package.session} {ringBuffer}");
            // 写入包体
            ringBuffer.WriteBytes(bodyData, 0, bodyData.Length);
        }
    }
}