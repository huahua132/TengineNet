using TEngine;
using System.Collections.Generic;
using System;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    public class CommonUIModule : Module, IUpdateModule, ICommonUIModule
    {
        private const float _releaseTime = 60f;
        private const float _releaseCheckInval = 60f;
        private float _preCheckTime;
        private List<ICommonUI> _activeList = new List<ICommonUI>();
        private Dictionary<Type, List<ICommonUI>> _idlePools = new();
        private CommonToastCreater _toastCreater;
        private CommonPopupCreater _popupCreater;
        private CommonItemIconCreater _itemIconCreater;

        #region 调试用数据
#if UNITY_EDITOR
        private static CommonUIModule _instance;
        public static CommonUIModule Instance => _instance;
        
        public List<ICommonUI> ActiveList => _activeList;
        public Dictionary<Type, List<ICommonUI>> IdlePools => _idlePools;
        public float ReleaseTime => _releaseTime;
#endif
        #endregion

        public override void OnInit()
        {
#if UNITY_EDITOR
            _instance = this;
#endif
            _preCheckTime = 0;

            _toastCreater = new CommonToastCreater();
            _toastCreater.Init();
            _idlePools[typeof(Toast)] = new List<ICommonUI>();

            _popupCreater = new CommonPopupCreater();
            _popupCreater.Init();
            _idlePools[typeof(Popup)] = new List<ICommonUI>();

            _itemIconCreater = new CommonItemIconCreater();
            _itemIconCreater.Init();
            _idlePools[typeof(itemIcon)] = new List<ICommonUI>();
            
#if UNITY_EDITOR
            if (CommonUIDebugger.Instance == null)
            {
                var go = new GameObject("CommonUIDebugger");
                go.AddComponent<CommonUIDebugger>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
#endif
        }

        public override void Shutdown()
        {
#if UNITY_EDITOR
            _instance = null;
#endif      
            Log.Info("CommonUIModule Shutdown");
            if (_toastCreater != null)
            {
                _toastCreater.Release();
                _toastCreater = null;
            }

            if (_popupCreater != null)
            {
                _popupCreater.Release();
                _popupCreater = null;
            }

            if (_itemIconCreater != null)
            {
                _itemIconCreater.Release();
                _itemIconCreater = null;
            }

            foreach (var kv in _activeList)
            {
                kv.Release();
            }
            _activeList.Clear();

            foreach (var kv in _idlePools)
            {
                foreach (var lv in kv.Value)
                {
                    lv.Release();
                }
            }
            _idlePools.Clear();

            _preCheckTime = 0;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 处理活动UI列表
            UpdateActiveList();
            
            // 定期清理过期的闲置对象
            CheckAndReleaseIdleObjects();
        }

        #region 私有通用方法

        /// <summary>
        /// 更新活动UI列表
        /// </summary>
        private void UpdateActiveList()
        {
            for (int i = _activeList.Count - 1; i >= 0; i--)
            {
                var ui = _activeList[i];
                if (ui.IsValid())
                {
                    bool isRecycle = ui.Update();
                    if (isRecycle)
                    {
                        RecycleUI(i, ui);
                    }
                }
                else
                {
                    _activeList.RemoveAt(i);
                    ui.Release();
                }
            }
        }

        /// <summary>
        /// 回收UI到对象池
        /// </summary>
        private void RecycleUI(int index, ICommonUI ui)
        {
            _activeList.RemoveAt(index);
            ui.Recycle();
            var list = _idlePools[ui.GetType()];
            list.Add(ui);
        }

        /// <summary>
        /// 检查并释放过期的闲置对象
        /// </summary>
        private void CheckAndReleaseIdleObjects()
        {
            var curTime = GameTime.time;
            if (curTime > _preCheckTime)
            {
                _preCheckTime = curTime + _releaseCheckInval;
                foreach (var kv in _idlePools)
                {
                    for (int i = kv.Value.Count - 1; i >= 0; i--)
                    {
                        var ui = kv.Value[i];
                        if (ui._RecycleTime + _releaseTime < curTime)
                        {
                            kv.Value.RemoveAt(i);
                            ui.Release();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从对象池获取UI，如果池中没有则返回null
        /// </summary>
        private ICommonUI TryGetFromPool(Type uiType)
        {
            var list = _idlePools[uiType];
            if (list.Count > 0)
            {
                var ui = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                ui.Reuse();
                return ui;
            }
            return null;
        }

        /// <summary>
        /// 将UI添加到活动列表
        /// </summary>
        private void AddToActiveList(ICommonUI ui)
        {
            _activeList.Add(ui);
        }

        #endregion

        #region Toast提示
        public void ShowToast(string txt)
        {
            if (IsShutDown) return;
            var toast = TryGetFromPool(typeof(Toast));
            if (toast != null)
            {
                if (toast is IToast itoast)
                {
                    itoast.SetTxt(txt);
                }
                AddToActiveList(toast);
            }
            else
            {
                _toastCreater.Create(AddToActiveList, txt).Forget();
            }
        }
        #endregion

        #region Popup弹窗

        public void ShowConfirm(string title, string content,
            Action onConfirm = null, Action onCancel = null,
            string confirmText = "确定", string cancelText = "取消")
        {
            if (IsShutDown) return;
            ShowPopup(popup =>
            {
                popup.SetTitle(title);
                popup.SetContent(content);
                popup.SetButtons(cancelText, confirmText);
                popup.SetCallbacks(onCancel, onConfirm);
            });
        }

        public void ShowAlert(string title, string content,
            Action onConfirm = null, string confirmText = "确定")
        {
            if (IsShutDown) return;
            ShowPopup(popup =>
            {
                popup.SetTitle(title);
                popup.SetContent(content);
                popup.SetButtons("", confirmText);
                popup.SetCallbacks(null, onConfirm);
                popup.HideCancelButton();
            });
        }

        public void ShowPopup(Action<IPopup> setupCallback)
        {
            if (IsShutDown) return;
            var popup = TryGetFromPool(typeof(Popup));
            if (popup != null)
            {
                if (popup is IPopup ipopup)
                {
                    setupCallback?.Invoke(ipopup);
                }
                AddToActiveList(popup);
            }
            else
            {
                _popupCreater.Create(ui =>
                {
                    setupCallback?.Invoke((IPopup)ui);
                    AddToActiveList(ui);
                }).Forget();
            }
        }

        public void ShowConfirm(string content, Action onConfirm = null, Action onCancel = null)
        {
            if (IsShutDown) return;
            ShowConfirm("确认", content, onConfirm, onCancel);
        }

        public void ShowAlert(string content, Action onConfirm = null)
        {
            if (IsShutDown) return;
            ShowAlert("提示", content, onConfirm);
        }

        #endregion

        #region ItemIcon
        public void GetItemIcon(Action<IItemIcon> callback)
        {
            if (IsShutDown) return;
            var item = TryGetFromPool(typeof(itemIcon));
            if (item != null)
            {
                AddToActiveList(item);
                callback?.Invoke((IItemIcon)item);
            }
            else
            {
                _itemIconCreater.Create(ui =>
                {
                    AddToActiveList(ui);
                    callback?.Invoke((IItemIcon)ui);
                }).Forget();
            }
        }
        #endregion
    }
}

