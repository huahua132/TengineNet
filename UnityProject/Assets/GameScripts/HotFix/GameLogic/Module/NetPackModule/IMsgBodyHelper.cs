using TEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace GameLogic
{
    public delegate void MsgBodyErrHandle(string errorMsg = "");
    public delegate void MsgBodyHandleCb(INetResponse response);

    public interface IMsgBodyHelper
    {
        void Init(MsgBodyErrHandle errhandle, MsgBodyHandleCb handleCb);
        INetPackage EncodePush(INetRequest Request);
        INetPackage EncodeRpc(INetRequest Request);
        void handleNetPack(INetPackage netPackage);
        void ClearSession(uint session);
        void ClearAll();
    }

    public class ProtoBufMsgBodyHelper : IMsgBodyHelper
    {
        private uint _incrSession = 1;
        private readonly Dictionary<uint, RpcNetPackage> _pushPackageHeads = new();   // 服务器推送的消息头
        private readonly Dictionary<uint, MemoryStream> _pushStreams = new();
        private readonly Dictionary<uint, RpcNetPackage> _rspPackageHeads = new();    // 服务器回复的消息头
        private readonly Dictionary<uint, MemoryStream> _rspStreams = new();

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
        private MsgBodyHandleCb _handleCb;

        public void Init(MsgBodyErrHandle errhandle, MsgBodyHandleCb handleCb)
        {
            _errHandle = errhandle;
            _handleCb = handleCb;
        }

        public INetPackage EncodeRpc(INetRequest Request)
        {
            var req = (ProtoBufRequest)Request;
            var pack = new RpcNetPackage
            {
                packtype = (byte)PACK_TYPE.WHOLE,
                packid = req.PackId,
                msgtype = (byte)MSG_TYPE.CLIENT_REQ,
                session = allocSession(),
                msgbody = ProtoBufHelper.ToBytes(req.MsgBody)
            };
            return pack;
        }

        public INetPackage EncodePush(INetRequest Request)
        {
            var req = (ProtoBufRequest)Request;
            var pack = new RpcNetPackage
            {
                packid = req.PackId,
                packtype = (byte)PACK_TYPE.WHOLE,
                msgtype = (byte)MSG_TYPE.CLIENT_PUSH,
                session = 0,
                msgbody = ProtoBufHelper.ToBytes(req.MsgBody)
            };
            return pack;
        }

        public void handleNetPack(INetPackage netPackage)
        {
            if (netPackage == null)
            {
                _errHandle?.Invoke("handleNetPack got null netPackage");
                return;
            }

            var pack = (RpcNetPackage)netPackage;
            var packId = pack.packid;
            var msgType = (MSG_TYPE)pack.msgtype;

            // 服务端不应发送 CLIENT_* 类型
            if (msgType == MSG_TYPE.CLIENT_REQ || msgType == MSG_TYPE.CLIENT_PUSH)
            {
                _errHandle?.Invoke($"handleNetPack invalid msgType from server? packId={packId} msgType={msgType}");
                return;
            }

            uint session = pack.session;
            // 按约定：服务端回复为偶数 session
            if (msgType == MSG_TYPE.SERVER_RSP && (session == 0 || session % 2 == 1))
            {
                _errHandle?.Invoke($"handleNetPack rpc rsp err session packId={packId} msgType={msgType} session={session}");
                return;
            }

            Log.Info($"handleNetPack >>> packId={packId} packType={pack.packtype} msgType={msgType} session={session}");
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
                    {
                        bool isErr = msgType == MSG_TYPE.SERVER_ERR;
                        bool isPush = msgType == MSG_TYPE.SERVER_PUSH;
                        var rsp = new ProtoBufResponse();
                        rsp.Init(packId, pack.session, isErr, isPush, pack.msgbody);
                        _handleCb?.Invoke(rsp);
                    }
                    break;

                case PACK_TYPE.HEAD:
                    {
                        if (packages.ContainsKey(session))
                        {
                            _errHandle?.Invoke($"handleNetPack HEAD already exists packId={packId} session={session}");
                            return;
                        }

                        uint expectedSize = pack.sz;
                        if (expectedSize == 0)
                        {
                            _errHandle?.Invoke($"handleNetPack HEAD expected size zero packId={packId} session={session}");
                            return;
                        }

                        packages[session] = pack;
                        streams[session] = new MemoryStream((int)expectedSize);
                    }
                    break;

                case PACK_TYPE.BODY:
                    {
                        if (pack.msgbody == null || pack.msgbody.Length == 0)
                        {
                            _errHandle?.Invoke($"handleNetPack BODY msgbody null/empty packId={packId} session={session}");
                            return;
                        }

                        if (!packages.TryGetValue(session, out var headPack))
                        {
                            _errHandle?.Invoke($"handleNetPack BODY head not exists packId={packId} session={session}");
                            return;
                        }
                        if (!streams.TryGetValue(session, out var stream))
                        {
                            _errHandle?.Invoke($"handleNetPack BODY stream missing packId={packId} session={session}");
                            return;
                        }

                        uint expectedSz = headPack.sz;
                        if ((ulong)stream.Length + (ulong)pack.msgbody.Length > expectedSz)
                        {
                            _errHandle?.Invoke($"handleNetPack BODY overflow packId={packId} session={session} expected={expectedSz} wouldBe={(stream.Length + pack.msgbody.Length)}");
                            packages.Remove(session);
                            streams.Remove(session);
                            return;
                        }

                        // 正确地把当前 BODY 包的数据写入流
                        stream.Write(pack.msgbody, 0, pack.msgbody.Length);
                    }
                    break;

                case PACK_TYPE.TAIL:
                    {
                        if (!packages.TryGetValue(session, out var headPackTail))
                        {
                            _errHandle?.Invoke($"handleNetPack TAIL head not exists packId={packId} session={session}");
                            return;
                        }
                        if (!streams.TryGetValue(session, out var streamTail))
                        {
                            _errHandle?.Invoke($"handleNetPack TAIL stream missing packId={packId} session={session}");
                            packages.Remove(session);
                            return;
                        }

                        streamTail.Write(pack.msgbody, 0, pack.msgbody.Length);
                        uint expectedSz = headPackTail.sz;
                        if ((uint)streamTail.Length != expectedSz)
                        {
                            _errHandle?.Invoke($"handleNetPack TAIL size mismatch packId={packId} session={session} expected={expectedSz} real={streamTail.Length}");
                            packages.Remove(session);
                            streams.Remove(session);
                            return;
                        }

                        var rsp = new ProtoBufResponse();
                        var msgTypeHead = (MSG_TYPE)headPackTail.msgtype;
                        bool isErr = msgTypeHead == MSG_TYPE.SERVER_ERR;
                        bool isPush = msgTypeHead == MSG_TYPE.SERVER_PUSH;
                        streamTail.Position = 0;
                        rsp.Init(headPackTail.packid, headPackTail.session, isErr, isPush, streamTail);

                        packages.Remove(session);
                        streams.Remove(session);

                        _handleCb?.Invoke(rsp);
                    }
                    break;

                default:
                    _errHandle?.Invoke($"handleNetPack unknown packType packId={packId} packType={pack.packtype} msgType={msgType}");
                    return;
            }
        }

        public void ClearSession(uint session)
        {
            if (_pushStreams.TryGetValue(session, out var pushStream))
            {
                pushStream?.Dispose();
                _pushStreams.Remove(session);
                _pushPackageHeads.Remove(session);
            }

            if (_rspStreams.TryGetValue(session, out var rspStream))
            {
                rspStream?.Dispose();
                _rspStreams.Remove(session);
                _rspPackageHeads.Remove(session);
            }
        }

        public void ClearAll()
        {
            _pushPackageHeads.Clear();
            _pushStreams.Clear();
            _rspPackageHeads.Clear();
            _rspStreams.Clear();
        }
    }
}

