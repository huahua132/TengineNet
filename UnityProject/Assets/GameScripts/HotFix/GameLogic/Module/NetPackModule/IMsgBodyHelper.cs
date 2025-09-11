using TEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace GameLogic
{
    public delegate void MsgBodyErrHandle(string errorMsg = "");
    public delegate void MsgBodyEncodeCb(INetPackage pack);
    public delegate void MsgBodyHandleCb(INetResponse response);
    
    interface IMsgBodyHelper
    {
        void Init(MsgBodyErrHandle errhandle, MsgBodyEncodeCb encodeCb, MsgBodyHandleCb handleCb);
        void Encode(INetRequst Requst);
        void handleNetPack(INetPackage netPackage);
    }

    public class ProtoBufMsgBodyHelper : IMsgBodyHelper
    {
        private uint _incrSession = 1;
        private Dictionary<uint, RpcNetPackage> _pushPackageHeads = new();          //服务器推送的消息
        private Dictionary<uint, MemoryStream> _pushStreams = new();
        private Dictionary<uint, RpcNetPackage> _rspPackageHeads = new();           //服务器回复的消息
        private Dictionary<uint, MemoryStream> _rspStreams = new();
        private uint allocSession()
        {
            uint ret = _incrSession;
            _incrSession += 2;
            if (_incrSession >= uint.MaxValue)
            {
                _incrSession = 1;
            }
            return ret;
        }

        private MsgBodyErrHandle _errHandle;
        private MsgBodyEncodeCb _encodeCb;
        private MsgBodyHandleCb _handleCb;

        public void Init(MsgBodyErrHandle errhandle, MsgBodyEncodeCb encodeCb, MsgBodyHandleCb handleCb)
        {
            _errHandle = errhandle;
            _encodeCb = encodeCb;
            _handleCb = handleCb;
        }
        public void Encode(INetRequst Requst)
        {
            var req = (ProtoBufRequst)Requst;
            var pack = new RpcNetPackage();
            pack.packid = 201;
            pack.packtype = (byte)PACK_TYPE.WHOLE;
            pack.msgtype = (byte)MSG_TYPE.CLIENT_REQ;
            pack.session = allocSession();
            pack.msgbody = ProtoBufHelper.ToBytes(req.MsgBody);
            _encodeCb(pack);
        }

        public void handleNetPack(INetPackage netPackage)
        {
            var pack = (RpcNetPackage)netPackage;
            var packId = pack.packid;
            var msgType = (MSG_TYPE)pack.msgtype;
            if (msgType == MSG_TYPE.CLIENT_REQ || msgType == MSG_TYPE.CLIENT_PUSH)
            {
                _errHandle($"handleNetPack err msgType msg {packId} {msgType}");
                return;
            }
            uint session = pack.session;
            if (session == 0)
            {
                _errHandle($"handleNetPack err session {packId} {msgType}");
                return;
            }
            var packType = (PACK_TYPE)pack.packtype;

            var packages = _rspPackageHeads;
            var streams = _rspStreams;
            if (msgType == MSG_TYPE.SERVER_PUSH)
            {
                packages = _pushPackageHeads;
                streams = _pushStreams;
            }
            
            switch (packType)
            {
                case PACK_TYPE.WHOLE:
                    var rsp = new ProtoBufResponse();
                    rsp.Init(packId, pack.session, pack.msgbody);
                    _handleCb(rsp);
                    break;
                case PACK_TYPE.HEAD:
                    if (packages.TryGetValue(session, out var b))
                    {
                        _errHandle($"handleNetPack pack head is exists {packId}");
                        return;
                    }
                    packages[session] = pack;
                    uint sz = BitConverter.ToUInt32(pack.msgbody, 0);
                    streams[session] = new MemoryStream((int)sz);
                    break;
                case PACK_TYPE.BODY:
                    if (!packages.TryGetValue(session, out var bb))
                    {
                        _errHandle($"handleNetPack pack body not is exists {packId}");
                        return;
                    }
                    var stream = streams[session];
                    stream.Write(bb.msgbody, (int)stream.Length, bb.msgbody.Length);
                    break;
                case PACK_TYPE.TAIL:
                    if (!packages.TryGetValue(session, out var bbb))
                    {
                        _errHandle($"handleNetPack pack tail not is exists {packId}");
                        return;
                    }
                    uint sz1 = BitConverter.ToUInt32(bbb.msgbody, 0);
                    var stream1 = streams[session];
                    if ((uint)stream1.Length != sz1)
                    {
                        _errHandle($"handleNetPack pack err size {packId} hopeSz{sz1} realSz{stream1.Length}");
                        return;
                    }
                    var rsp1 = new ProtoBufResponse();
                    rsp1.Init(bbb.packid, bbb.session, stream1);
                    _handleCb(rsp1);
                    break;
                default:
                    _errHandle($"handleNetPack err packType msg {packId} {msgType}");
                    return;
            }
        }
    }
}