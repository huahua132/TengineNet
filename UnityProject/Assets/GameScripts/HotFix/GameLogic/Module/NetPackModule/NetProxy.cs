using TEngine;
using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    public enum NetNodeID
    {
        hall = 1,               //大厅
        game = 2,               //游戏
    }
    
    /// <summary>
    /// 请求结果包装类，支持链式调用
    /// </summary>
    public class NetRequestResult<T> where T : class
    {
        private RpcResult<T> _result;
        private Dictionary<int, Action<string>> _errorHandlers;
        private Action<T> _successHandler;
        private Action<RpcErrorType, string> _failureHandler;
        private bool _errorHandled;

        public NetRequestResult(RpcResult<T> result)
        {
            _result = result;
            _errorHandlers = new Dictionary<int, Action<string>>();
            _errorHandled = false;
        }

        /// <summary>
        /// 获取原始结果
        /// </summary>
        public RpcResult<T> Result => _result;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => _result.IsSuccess;

        /// <summary>
        /// 数据（仅成功时有效）
        /// </summary>
        public T Data => _result.Data;

        /// <summary>
        /// 错误类型
        /// </summary>
        public RpcErrorType ErrorType => _result.ErrorType;

        /// <summary>
        /// 错误码
        /// </summary>
        public int ErrorCode => _result.ErrorCode;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMsg => _result.ErrorMsg;

        /// <summary>
        /// 链式处理特定错误码
        /// </summary>
        public void OnError(int errorCode, Action<string> handler)
        {
            if (handler != null)
            {
                _errorHandlers[errorCode] = handler;
            }
        }

        /// <summary>
        /// 链式处理特定错误码（无参数版本）
        /// </summary>
        public void OnError(int errorCode, Action handler)
        {
            if (handler != null)
            {
                _errorHandlers[errorCode] = (msg) => handler();
            }
        }

        /// <summary>
        /// 成功时执行回调
        /// </summary>
        public void OnSuccess(Action<T> handler)
        {
            _successHandler = handler;
        }

        /// <summary>
        /// 失败时执行回调
        /// </summary>
        public void OnFailure(Action<RpcErrorType, string> handler)
        {
            _failureHandler = handler;
        }

        /// <summary>
        /// 尝试使用链式处理器处理错误
        /// </summary>
        public bool TryHandleError(int errorCode, string errorMsg)
        {
            if (_errorHandlers.TryGetValue(errorCode, out var handler))
            {
                handler?.Invoke(errorMsg);
                _errorHandled = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 执行成功回调
        /// </summary>
        public void ExecuteSuccessHandler()
        {
            if (IsSuccess && _successHandler != null)
            {
                _successHandler(Data);
            }
        }

        /// <summary>
        /// 执行失败回调
        /// </summary>
        public void ExecuteFailureHandler()
        {
            if (!IsSuccess && _failureHandler != null && !_errorHandled)
            {
                _failureHandler(ErrorType, ErrorMsg);
                _errorHandled = true;
            }
        }

        /// <summary>
        /// 是否已被处理
        /// </summary>
        public bool IsErrorHandled => _errorHandled;

        /// <summary>
        /// 标记错误已处理
        /// </summary>
        public void MarkErrorHandled()
        {
            _errorHandled = true;
        }
    }

    /// <summary>
    /// 网络请求任务包装类，支持真正的链式调用和 await
    /// </summary>
    public class NetRequestTask<T> where T : class
    {
        private readonly NetProxy _proxy;
        private readonly UniTask<NetRequestResult<T>> _task;
        private readonly List<Action<NetRequestResult<T>>> _configurators = new List<Action<NetRequestResult<T>>>();

        public NetRequestTask(NetProxy proxy, UniTask<NetRequestResult<T>> task)
        {
            _proxy = proxy;
            _task = task;
        }

        /// <summary>
        /// 链式处理特定错误码
        /// </summary>
        public NetRequestTask<T> OnError(int errorCode, Action<string> handler)
        {
            _configurators.Add(result => result.OnError(errorCode, handler));
            return this;
        }

        /// <summary>
        /// 链式处理特定错误码（无参数版本）
        /// </summary>
        public NetRequestTask<T> OnError(int errorCode, Action handler)
        {
            _configurators.Add(result => result.OnError(errorCode, handler));
            return this;
        }

        /// <summary>
        /// 成功时执行回调
        /// </summary>
        public NetRequestTask<T> OnSuccess(Action<T> handler)
        {
            _configurators.Add(result => result.OnSuccess(handler));
            return this;
        }

        /// <summary>
        /// 失败时执行回调
        /// </summary>
        public NetRequestTask<T> OnFailure(Action<RpcErrorType, string> handler)
        {
            _configurators.Add(result => result.OnFailure(handler));
            return this;
        }

        /// <summary>
        /// 获取 Awaiter，支持 await 语法（UniTask 版本）
        /// </summary>
        public UniTask<NetRequestResult<T>>.Awaiter GetAwaiter()
        {
            return ExecuteAsync().GetAwaiter();
        }

        /// <summary>
        /// 执行请求并应用所有配置
        /// </summary>
        private async UniTask<NetRequestResult<T>> ExecuteAsync()
        {
            // 等待请求完成
            var result = await _task;

            // 应用所有链式配置
            foreach (var configurator in _configurators)
            {
                configurator(result);
            }

            // 执行成功回调
            result.ExecuteSuccessHandler();

            // 自动执行 Complete（处理未处理的服务器错误）
            _proxy.HandleServerError(result);

            // 执行失败回调
            result.ExecuteFailureHandler();

            return result;
        }

        /// <summary>
        /// 显式转换为 UniTask（可选）
        /// </summary>
        public UniTask<NetRequestResult<T>> AsUniTask()
        {
            return ExecuteAsync();
        }
    }
    
    public class NetProxy : INetProxy
    {
        private bool _disposed = false;
        private NetNodeID _nodeID;
        private string _host;
        private int _port;
        private long _playerId;
        private string _token;
        private ProtoBufMsgBodyHelper _bufHelper;
        
        /// <summary>
        /// 通用错误码处理器字典
        /// </summary>
        private Dictionary<int, Action<string>> _commonErrorHandlers = new Dictionary<int, Action<string>>();
        
        /// <summary>
        /// 正在进行的请求字典（key: packId）
        /// </summary>
        private Dictionary<ushort, UniTask> _ongoingRequests = new Dictionary<ushort, UniTask>();
        
        /// <summary>
        /// 全局重复请求回调
        /// </summary>
        private Action<ushort> _duplicateRequestCallback;
        
        public NetProxy(NetNodeID nodeID)
        {
            _nodeID = nodeID;
            _bufHelper = new ProtoBufMsgBodyHelper();
            
            // 设置认证配置
            var authConfig = new AuthConfig(10000, 0, 0);
            GameModule.NetPack.SetAuthRequestProvider((uint)_nodeID, (nodeId) =>
            {
                ProtoBufRequest netReq = new ProtoBufRequest();
                netReq.PackId = login.MessageId.LoginReq;
                var loginReq = new login.LoginReq();
                loginReq.player_id = _playerId;
                loginReq.token = _token;
                netReq.MsgBody = loginReq;
                return netReq;
            }, authConfig);

            // 设置心跳配置
            var heartbeatConfig = new HeartbeatConfig(10, true, 10000, 3);
            GameModule.NetPack.SetHeartbeatRequestProvider((uint)_nodeID, (nodeId) =>
            {
                ProtoBufRequest netReq = new ProtoBufRequest();
                netReq.PackId = login.MessageId.HeartReq;
                var heartReq = new login.HeartReq();
                heartReq.time = DateTime.Now.Second;
                netReq.MsgBody = heartReq;
                return netReq;
            }, heartbeatConfig);
            
            // 设置错误码和错误消息获取方法
            INetResponse.GetRspErrCode = NetResponseErrCode;
            INetResponse.GetRspErrMsg = NetResponseErrMsg;
            
            // 注册连接和断线回调
            GameModule.NetPack.RegisterConnectCallback((uint)_nodeID, ConnectCallback);
            GameModule.NetPack.RegisterDisconnectCallback((uint)_nodeID, DisconnectCallback);
            
            // 注册认证回调
            GameModule.NetPack.RegisterAuthSuccessCallback((uint)_nodeID, AuthSuccessCallback);
            GameModule.NetPack.RegisterAuthFailureCallback((uint)_nodeID, AuthFailureCallback);
            
            // 注册常见的通用错误处理器
            RegisterCommonErrorHandlers();
            
            // 注册默认的重复请求回调
            RegisterDuplicateRequestCallback((packId) =>
            {
                GameModule.CommonUI.ShowToast("请求处理中，请稍候...");
                Log.Warning($"检测到重复请求: PackId={packId}");
            });
        }

        #region 连接相关回调

        private void ReconnectTimeOut(object[] args)
        {
            GameModule.CommonUI.ShowLoading();
            int attempts = GameModule.NetPack.GetReconnectAttempts((uint)_nodeID);
            GameModule.CommonUI.ShowToast($"重试重连中第{attempts}次");
            GameModule.NetPack.Reconnect((uint)_nodeID);
        }

        private void OnBtnConfirm()
        {
            GameModule.NetPack.Reconnect((uint)_nodeID);
        }

        private void OnBtnCancel()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ConnectCallback(bool success, string errorMsg = "")
        {
            if (!success)
            {
                int attempts = GameModule.NetPack.GetReconnectAttempts((uint)_nodeID);
                if (attempts % 3 == 0)
                {
                    GameModule.Timer.AddTimer(ReconnectTimeOut, 1f, false);
                }
                else
                {
                    GameModule.CommonUI.HideLoading();
                    GameModule.CommonUI.ShowConfirm("网络连接异常", "是否重试？", OnBtnConfirm, OnBtnCancel, "重试", "退出");
                }
            }
            else
            {
                GameModule.CommonUI.HideLoading();
            }
        }

        private void DisconnectCallback(DisconnectType disconnectType, string reason = "")
        {
            Log.Info($"DisconnectCallback >>> {disconnectType}  {reason}");
            GameModule.CommonUI.ShowToast($"网络断开连接 结点{_nodeID}: {reason} {disconnectType}");
            if (disconnectType == DisconnectType.Active)
            {
                GameModule.NetPack.Close((uint)_nodeID);
            } 
            else
            {
                if (GameModule.NetPack.IsNodeAuthenticated((uint)_nodeID))
                {
                    int attempts = GameModule.NetPack.GetReconnectAttempts((uint)_nodeID);
                    if (attempts == 0)
                    {
                        ReconnectTimeOut(null);
                    }
                }
            }
        }

        #endregion

        #region 认证相关回调

        private void AuthSuccessCallback(RpcResult<INetResponse> result)
        {
            Log.Info($"节点 {_nodeID} 认证成功");
        }

        private void AuthFailureCallback(RpcResult<INetResponse> result)
        {
            var errorMsg = $"认证失败 结点{_nodeID}: {result.ErrorMsg}";
            GameModule.CommonUI.ShowToast(errorMsg);
            Log.Error($"节点 {_nodeID} 认证失败: 错误码={result.ErrorCode}, 错误信息={result.ErrorMsg}");
        }
        #endregion

        #region 错误码处理

        private static int NetResponseErrCode(INetResponse response)
        {
            errors.Error err = response.GetResponse<errors.Error>();
            return err.code;
        }

        private static string NetResponseErrMsg(INetResponse response)
        {
            errors.Error err = response.GetResponse<errors.Error>();
            return err.msg;
        }

        #endregion

        #region 通用错误码处理器

        private void RegisterCommonErrorHandlers()
        {
            // 示例：注册一些常见的业务错误码
        }

        public void RegisterCommonErrorHandler(int errorCode, Action<string> handler)
        {
            if (handler != null)
            {
                _commonErrorHandlers[errorCode] = handler;
            }
        }

        public void RegisterCommonErrorHandler(int errorCode, Action handler)
        {
            if (handler != null)
            {
                _commonErrorHandlers[errorCode] = (msg) => handler();
            }
        }

        public void UnregisterCommonErrorHandler(int errorCode)
        {
            _commonErrorHandlers.Remove(errorCode);
        }

        public void ClearCommonErrorHandlers()
        {
            _commonErrorHandlers.Clear();
        }

        #endregion

        #region 重复请求处理

        /// <summary>
        /// 注册全局重复请求回调
        /// </summary>
        /// <param name="callback">回调函数（参数为 packId）</param>
        public void RegisterDuplicateRequestCallback(Action<ushort> callback)
        {
            _duplicateRequestCallback = callback;
        }

        /// <summary>
        /// 注销全局重复请求回调
        /// </summary>
        public void UnregisterDuplicateRequestCallback()
        {
            _duplicateRequestCallback = null;
        }

        /// <summary>
        /// 检查是否存在重复请求
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <returns>是否重复</returns>
        private bool IsDuplicateRequest(ushort packId)
        {
            return _ongoingRequests.ContainsKey(packId);
        }

        /// <summary>
        /// 添加正在进行的请求
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="task">请求任务</param>
        private void AddOngoingRequest(ushort packId, UniTask task)
        {
            _ongoingRequests[packId] = task;
        }

        /// <summary>
        /// 移除正在进行的请求
        /// </summary>
        /// <param name="packId">消息包ID</param>
        private void RemoveOngoingRequest(ushort packId)
        {
            _ongoingRequests.Remove(packId);
        }

        /// <summary>
        /// 触发重复请求回调
        /// </summary>
        /// <param name="packId">消息包ID</param>
        private void OnDuplicateRequest(ushort packId)
        {
            _duplicateRequestCallback?.Invoke(packId);
        }

        #endregion

        #region 连接管理

        public void SetConnect(string host, int port, long playerId, string token)
        {
            ThrowIfDisposed();
            _host = host;
            _port = port;
            _playerId = playerId;
            _token = token;
            GameModule.NetPack.Connect((uint)_nodeID, NetworkType.WebSocket, host, port, _bufHelper);
        }

        public void Close()
        {
            GameModule.NetPack.Close((uint)_nodeID);
        }

        #endregion

        #region 认证回调管理

        public void RegisterAuthSuccessCallback(AuthSuccessCallback callback)
        {
            GameModule.NetPack.RegisterAuthSuccessCallback((uint)_nodeID, callback);
        }

        public void UnregisterAuthSuccessCallback(AuthSuccessCallback callback)
        {
            GameModule.NetPack.UnregisterAuthSuccessCallback((uint)_nodeID, callback);
        }

        public void RegisterAuthFailureCallback(AuthFailureCallback callback)
        {
            GameModule.NetPack.RegisterAuthFailureCallback((uint)_nodeID, callback);
        }

        public void UnregisterAuthFailureCallback(AuthFailureCallback callback)
        {
            GameModule.NetPack.UnregisterAuthFailureCallback((uint)_nodeID, callback);
        }

        #endregion

        #region 消息监听管理

        public void RegisterMessageListener(ushort packId, MessageCallback callback)
        {
            GameModule.NetPack.RegisterMessageListener((uint)_nodeID, packId, callback);
        }

        public void UnregisterMessageListener(ushort packId, MessageCallback callback)
        {
            GameModule.NetPack.UnregisterMessageListener((uint)_nodeID, packId, callback);
        }

        public void RegisterMessageListener<T>(ushort packId, Action<T> callback) where T : class
        {
            RegisterMessageListener(packId, (response) =>
            {
                try
                {
                    var msg = response.GetResponse<T>();
                    callback?.Invoke(msg);
                }
                catch (Exception ex)
                {
                    Log.Error($"消息解析异常 PackId={packId}: {ex}");
                }
            });
        }

        #endregion

        #region 消息发送 - Result模式

        public RpcResult SendMessage(INetRequest request, bool requireAuth = true)
        {
            ThrowIfDisposed();
            return GameModule.NetPack.SendMessage((uint)_nodeID, request, requireAuth);
        }

        public RpcResult SendProtoBufMessage(ushort packId, ProtoBuf.IExtensible msgBody, bool requireAuth = true)
        {
            var request = new ProtoBufRequest
            {
                PackId = packId,
                MsgBody = msgBody
            };
            return SendMessage(request, requireAuth);
        }

        public async UniTask<RpcResult<INetResponse>> SendRpcRequest(INetRequest request, int timeoutMs = 10000, bool requireAuth = true)
        {
            ThrowIfDisposed();
            return await GameModule.NetPack.SendRpcRequest((uint)_nodeID, request, timeoutMs, requireAuth);
        }

        public async UniTask<RpcResult<INetResponse>> SendProtoBufRpcRequest(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 10000, bool requireAuth = true)
        {
            var request = new ProtoBufRequest
            {
                PackId = packId,
                MsgBody = msgBody
            };
            return await SendRpcRequest(request, timeoutMs, requireAuth);
        }

        public async UniTask<RpcResult<T>> SendProtoBufRpcRequest<T>(ushort packId, ProtoBuf.IExtensible msgBody, int timeoutMs = 10000, bool requireAuth = true) where T : class
        {
            var result = await SendProtoBufRpcRequest(packId, msgBody, timeoutMs, requireAuth);
            return result.Map(response => response.GetResponse<T>());
        }

        public async UniTask<RpcResult<INetResponse>> SendRpcRequestWithRetry(
            INetRequest request, 
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = true)
        {
            ThrowIfDisposed();
            return await GameModule.NetPack.SendRpcRequestWithRetry((uint)_nodeID, request, timeoutMs, maxRetries, retryDelayMs, requireAuth);
        }

        public async UniTask<RpcResult<INetResponse>> SendProtoBufRpcRequestWithRetry(
            ushort packId, 
            ProtoBuf.IExtensible msgBody, 
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = true)
        {
            var request = new ProtoBufRequest
            {
                PackId = packId,
                MsgBody = msgBody
            };
            return await SendRpcRequestWithRetry(request, timeoutMs, maxRetries, retryDelayMs, requireAuth);
        }

        public async UniTask<RpcResult<T>> SendProtoBufRpcRequestWithRetry<T>(
            ushort packId, 
            ProtoBuf.IExtensible msgBody, 
            int timeoutMs = 10000,
            int maxRetries = 3,
            int retryDelayMs = 1000,
            bool requireAuth = true) where T : class
        {
            var result = await SendProtoBufRpcRequestWithRetry(packId, msgBody, timeoutMs, maxRetries, retryDelayMs, requireAuth);
            return result.Map(response => response.GetResponse<T>());
        }

        #endregion

        #region 便捷的业务方法

        /// <summary>
        /// 发送请求并显示错误提示（支持链式调用，自动执行Complete）
        /// </summary>
        /// <param name="packId">消息包ID</param>
        /// <param name="msgBody">消息体</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="showLoading">是否显示Loading</param>
        /// <param name="preventDuplicate">是否防止重复请求（默认true）</param>
        public NetRequestTask<T> SendRequestWithUI<T>(
            ushort packId,
            ProtoBuf.IExtensible msgBody,
            bool showLoading = true,
            bool preventDuplicate = true,
            int timeoutMs = 10000) where T : class
        {
            ThrowIfDisposed();
            
            var task = ExecuteSendRequestWithUI<T>(packId, msgBody, timeoutMs, showLoading, preventDuplicate);
            return new NetRequestTask<T>(this, task);
        }

        /// <summary>
        /// 内部执行方法
        /// </summary>
        private async UniTask<NetRequestResult<T>> ExecuteSendRequestWithUI<T>(
            ushort packId, 
            ProtoBuf.IExtensible msgBody, 
            int timeoutMs,
            bool showLoading,
            bool preventDuplicate) where T : class
        {
            // 防重复请求检查
            if (preventDuplicate)
            {
                if (IsDuplicateRequest(packId))
                {
                    // 触发重复请求回调
                    OnDuplicateRequest(packId);
                    
                    // 返回一个表示重复请求的失败结果
                    var duplicateResult = RpcResult<T>.Failure(
                        RpcErrorType.Unknown,
                        "请求处理中，请稍候..."
                    );
                    var duplicateWrappedResult = new NetRequestResult<T>(duplicateResult);
                    duplicateWrappedResult.MarkErrorHandled(); // 标记已处理，不显示错误提示
                    return duplicateWrappedResult;
                }
            }

            if (showLoading)
            {
                GameModule.CommonUI.ShowLoading();
            }

            try
            {
                // 创建请求任务
                var requestTask = SendProtoBufRpcRequest<T>(packId, msgBody, timeoutMs);
                
                // 如果需要防重复，记录正在进行的请求
                if (preventDuplicate)
                {
                    AddOngoingRequest(packId, requestTask.AsUniTask());
                }

                var result = await requestTask;
                
                if (showLoading)
                {
                    GameModule.CommonUI.HideLoading();
                }

                var wrappedResult = new NetRequestResult<T>(result);

                // 自动处理常见的RPC错误（非业务错误码）
                if (!result.IsSuccess && result.ErrorType != RpcErrorType.ServerError)
                {
                    HandleCommonRpcError(result);
                    wrappedResult.MarkErrorHandled();
                }

                return wrappedResult;
            }
            catch (Exception ex)
            {
                if (showLoading)
                {
                    GameModule.CommonUI.HideLoading();
                }

                Log.Error($"发送请求异常: {ex}");
                var failureResult = RpcResult<T>.Failure(
                    RpcErrorType.Unknown,
                    $"请求异常: {ex.Message}"
                );
                return new NetRequestResult<T>(failureResult);
            }
            finally
            {
                // 请求完成后移除记录
                if (preventDuplicate)
                {
                    RemoveOngoingRequest(packId);
                }
            }
        }

        /// <summary>
        /// 处理常见的RPC错误（非业务错误码）
        /// </summary>
        private void HandleCommonRpcError<T>(RpcResult<T> result) where T : class
        {
            switch (result.ErrorType)
            {
                case RpcErrorType.NodeNotFound:
                    GameModule.CommonUI.ShowToast("网络节点未找到");
                    break;

                case RpcErrorType.NotConnected:
                    GameModule.CommonUI.ShowToast("网络未连接");
                    break;

                case RpcErrorType.Timeout:
                    GameModule.CommonUI.ShowToast("请求超时，请检查网络");
                    break;

                case RpcErrorType.NotAuthenticated:
                    GameModule.CommonUI.ShowToast("未认证，请重新登录");
                    break;

                case RpcErrorType.NetworkError:
                    GameModule.CommonUI.ShowToast("网络异常，请稍后重试");
                    break;

                default:
                    GameModule.CommonUI.ShowToast($"请求失败: {result.ErrorMsg}");
                    break;
            }
        }

        /// <summary>
        /// 处理业务错误码（先检查链式处理器，再使用通用处理器）
        /// </summary>
        public void HandleServerError<T>(NetRequestResult<T> result) where T : class
        {
            if (result.IsSuccess || result.IsErrorHandled)
            {
                return;
            }

            if (result.ErrorType == RpcErrorType.ServerError)
            {
                int errorCode = result.ErrorCode;
                string errorMsg = result.ErrorMsg;

                // 先尝试链式处理器
                if (result.TryHandleError(errorCode, errorMsg))
                {
                    return;
                }

                // 再尝试通用处理器
                if (_commonErrorHandlers.TryGetValue(errorCode, out var handler))
                {
                    handler?.Invoke(errorMsg);
                    result.MarkErrorHandled();
                    return;
                }

                // 默认提示
                GameModule.CommonUI.ShowToast($"服务器错误 [{errorCode}]: {errorMsg}");
                result.MarkErrorHandled();
            }
        }

        #endregion

        #region 状态查询

        public ConnectState GetConnectState()
        {
            return GameModule.NetPack.GetConnectState((uint)_nodeID);
        }

        public bool IsAuthenticated()
        {
            return GameModule.NetPack.IsNodeAuthenticated((uint)_nodeID);
        }

        public bool IsReady()
        {
            return GetConnectState() == ConnectState.Connected && IsAuthenticated();
        }

        #endregion

        #region 析构清理

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NetProxy));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    GameModule.NetPack.UnregisterConnectCallback((uint)_nodeID, ConnectCallback);
                    GameModule.NetPack.UnregisterDisconnectCallback((uint)_nodeID, DisconnectCallback);
                    GameModule.NetPack.UnregisterAuthSuccessCallback((uint)_nodeID, AuthSuccessCallback);
                    GameModule.NetPack.UnregisterAuthFailureCallback((uint)_nodeID, AuthFailureCallback);
                    
                    ClearCommonErrorHandlers();
                    UnregisterDuplicateRequestCallback();
                    _ongoingRequests?.Clear();
                    Close();
                }
                catch (Exception ex)
                {
                    Log.Error($"Dispose error: {ex}");
                }
            }

            _bufHelper = null;
            _commonErrorHandlers = null;
            _ongoingRequests = null;
            _duplicateRequestCallback = null;
            _disposed = true;
        }

        #endregion
    }
}

