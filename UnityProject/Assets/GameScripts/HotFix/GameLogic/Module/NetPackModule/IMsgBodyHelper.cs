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
        void ClearAll();
        void CheckTimeouts(); // 新增：检查超时
    }

    public class ProtoBufMsgBodyHelper : IMsgBodyHelper
    {
        private uint _incrSession = 1;
        private readonly Dictionary<uint, RpcNetPackage> _pushPackageHeads = new();   
        private readonly Dictionary<uint, MemoryStream> _pushStreams = new();
        private readonly Dictionary<uint, RpcNetPackage> _rspPackageHeads = new();    
        private readonly Dictionary<uint, MemoryStream> _rspStreams = new();
        
        // 新增：记录每个session的开始时间
        private readonly Dictionary<uint, DateTime> _pushSessionTimes = new();
        private readonly Dictionary<uint, DateTime> _rspSessionTimes = new();
        
        // 新增：超时配置（秒）
        private readonly int _timeoutSeconds = 30; // 默认30秒超时

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

            if (msgType == MSG_TYPE.CLIENT_REQ || msgType == MSG_TYPE.CLIENT_PUSH)
            {
                _errHandle?.Invoke($"handleNetPack invalid msgType from server? packId={packId} msgType={msgType}");
                return;
            }

            uint session = pack.session;
            if (msgType == MSG_TYPE.SERVER_RSP && (session == 0 || session % 2 == 1))
            {
                _errHandle?.Invoke($"handleNetPack rpc rsp err session packId={packId} msgType={msgType} session={session}");
                return;
            }

            Log.Info($"handleNetPack >>> packId={packId} packType={pack.packtype} msgType={msgType} session={session}");
            var packType = (PACK_TYPE)pack.packtype;

            var packages = _rspPackageHeads;
            var streams = _rspStreams;
            var sessionTimes = _rspSessionTimes; // 新增
            if (msgType == MSG_TYPE.SERVER_PUSH)
            {
                packages = _pushPackageHeads;
                streams = _pushStreams;
                sessionTimes = _pushSessionTimes; // 新增
            }

            switch (packType)
            {
                case PACK_TYPE.WHOLE:
                    {
                        bool isErr = msgType == MSG_TYPE.SERVER_ERR;
                        bool isPush = msgType == MSG_TYPE.SERVER_PUSH;
                        var rsp = MemoryPool.Acquire<ProtoBufResponse>();
                        rsp.Init(packId, pack.session, isErr, isPush, pack.msgbody);
                        MemoryPool.Release(pack);
                        _handleCb?.Invoke(rsp);
                        MemoryPool.Release(rsp);
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
                        sessionTimes[session] = DateTime.Now; // 新增：记录开始时间
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
                            ClearSessionInternal(session, packages, streams, sessionTimes); // 修改：使用内部清理方法
                            return;
                        }

                        stream.Write(pack.msgbody, 0, pack.msgbody.Length);
                        MemoryPool.Release(pack);
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
                            ClearSessionInternal(session, packages, streams, sessionTimes); // 修改
                            return;
                        }

                        streamTail.Write(pack.msgbody, 0, pack.msgbody.Length);
                        MemoryPool.Release(pack);
                        uint expectedSz = headPackTail.sz;
                        if ((uint)streamTail.Length != expectedSz)
                        {
                            _errHandle?.Invoke($"handleNetPack TAIL size mismatch packId={packId} session={session} expected={expectedSz} real={streamTail.Length}");
                            ClearSessionInternal(session, packages, streams, sessionTimes); // 修改
                            return;
                        }

                        var rsp = MemoryPool.Acquire<ProtoBufResponse>();
                        var msgTypeHead = (MSG_TYPE)headPackTail.msgtype;
                        bool isErr = msgTypeHead == MSG_TYPE.SERVER_ERR;
                        bool isPush = msgTypeHead == MSG_TYPE.SERVER_PUSH;
                        streamTail.Position = 0;
                        rsp.Init(headPackTail.packid, headPackTail.session, isErr, isPush, streamTail);

                        ClearSessionInternal(session, packages, streams, sessionTimes, false); // 修改：完成后清理

                        _handleCb?.Invoke(rsp);
                        MemoryPool.Release(rsp);
                    }
                    break;

                default:
                    _errHandle?.Invoke($"handleNetPack unknown packType packId={packId} packType={pack.packtype} msgType={msgType}");
                    return;
            }
        }

        // 新增：内部清理方法
        private void ClearSessionInternal(uint session, 
            Dictionary<uint, RpcNetPackage> packages, 
            Dictionary<uint, MemoryStream> streams,
            Dictionary<uint, DateTime> sessionTimes, bool isClearStream = true)
        {
            if (streams.TryGetValue(session, out var stream))
            {
                if (isClearStream)
                {
                    stream?.Dispose();
                }
                streams.Remove(session);
            }
            if (packages.TryGetValue(session, out var pack))
            {
                MemoryPool.Release(pack);
                packages.Remove(session);
                sessionTimes.Remove(session);
            }
        }

        public void ClearAll()
        {
            // 释放所有流资源
            foreach (var stream in _pushStreams.Values)
            {
                stream?.Dispose();
            }
            foreach (var stream in _rspStreams.Values)
            {
                stream?.Dispose();
            }

            _pushPackageHeads.Clear();
            _pushStreams.Clear();
            _rspPackageHeads.Clear();
            _rspStreams.Clear();
            _pushSessionTimes.Clear(); // 新增
            _rspSessionTimes.Clear(); // 新增
        }

        // 新增：检查并清理超时的包
        public void CheckTimeouts()
        {
            var now = DateTime.Now;
            var timeoutSessions = new List<uint>();

            // 检查推送包的超时
            foreach (var kvp in _pushSessionTimes)
            {
                if ((now - kvp.Value).TotalSeconds > _timeoutSeconds)
                {
                    timeoutSessions.Add(kvp.Key);
                }
            }

            foreach (uint session in timeoutSessions)
            {
                Log.Warning($"CheckTimeouts: Clearing timeout push session {session}");
                ClearSessionInternal(session, _pushPackageHeads, _pushStreams, _pushSessionTimes);
                _errHandle?.Invoke($"Push session {session} timeout and cleared");
            }

            timeoutSessions.Clear();

            // 检查回复包的超时
            foreach (var kvp in _rspSessionTimes)
            {
                if ((now - kvp.Value).TotalSeconds > _timeoutSeconds)
                {
                    timeoutSessions.Add(kvp.Key);
                }
            }

            foreach (uint session in timeoutSessions)
            {
                Log.Warning($"CheckTimeouts: Clearing timeout rsp session {session}");
                ClearSessionInternal(session, _rspPackageHeads, _rspStreams, _rspSessionTimes);
                _errHandle?.Invoke($"Rsp session {session} timeout and cleared");
            }
        }

        // 新增：设置超时时间（可选）
        public void SetTimeout(int timeoutSeconds)
        {
            if (timeoutSeconds > 0)
            {
                System.Reflection.FieldInfo field = typeof(ProtoBufMsgBodyHelper).GetField("_timeoutSeconds", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(this, timeoutSeconds);
            }
        }
    }
}

