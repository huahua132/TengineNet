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

        public override void OnInit()
        {
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
        }

        public override void Shutdown()
        {
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

            _preCheckTime = 0;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            for (int i = _activeList.Count - 1; i >= 0; i--)
            {
                var ui = _activeList[i];
                bool isRecycle = ui.Update();
                if (isRecycle)
                {
                    _activeList.RemoveAt(i);
                    ui.Recycle();
                    var list = _idlePools[ui.GetType()];
                    list.Add(ui);
                }
            }

            var curTime = GameTime.time;
            if (curTime > _preCheckTime)
            {
                _preCheckTime = curTime + _releaseCheckInval;
                foreach (var kv in _idlePools)
                {
                    for (int i = kv.Value.Count - 1; i >= 0; i--)
                    {
                        var ui = kv.Value[i];
                        if (ui._RecycleTime + _releaseTime > curTime)
                        {
                            kv.Value.RemoveAt(i);
                            ui.Release();
                        }
                    }
                }
            }
        }

        private void CreateSuccCallback(ICommonUI ui)
        {
            _activeList.Add(ui);
        }

        #region Toast提示
        public void ShowToast(string txt)
        {
            var list = _idlePools[typeof(Toast)];
            ICommonUI toast = null;
            if (list.Count > 0)
            {
                toast = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                toast.Reuse();
                if (toast is IToast itoast)
                {
                    itoast.SetTxt(txt);
                }
                CreateSuccCallback(toast);
            }
            else
            {
                _toastCreater.Create(CreateSuccCallback, txt).Forget();
            }
        }
        #endregion

        #region Popup弹窗

        /// <summary>
        /// 显示确认弹窗（有取消和确定按钮）
        /// </summary>
        public void ShowConfirm(string title, string content,
            Action onConfirm = null, Action onCancel = null,
            string confirmText = "确定", string cancelText = "取消")
        {
            ShowPopup(popup =>
            {
                popup.SetTitle(title);
                popup.SetContent(content);
                popup.SetButtons(cancelText, confirmText);
                popup.SetCallbacks(onCancel, onConfirm);
            });
        }

        /// <summary>
        /// 显示提示弹窗（只有确定按钮）
        /// </summary>
        public void ShowAlert(string title, string content,
            Action onConfirm = null, string confirmText = "确定")
        {
            ShowPopup(popup =>
            {
                popup.SetTitle(title);
                popup.SetContent(content);
                popup.SetButtons("", confirmText);
                popup.SetCallbacks(null, onConfirm);
                popup.HideCancelButton();
            });
        }

        /// <summary>
        /// 显示自定义弹窗
        /// </summary>
        public void ShowPopup(Action<IPopup> setupCallback)
        {
            var list = _idlePools[typeof(Popup)];
            ICommonUI popup = null;

            if (list.Count > 0)
            {
                popup = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                popup.Reuse();

                if (popup is IPopup ipopup)
                {
                    setupCallback?.Invoke(ipopup);
                }
                CreateSuccCallback(popup);
            }
            else
            {
                _popupCreater.Create(popup =>
                {
                    setupCallback?.Invoke((IPopup)popup);
                    CreateSuccCallback(popup);
                }).Forget();
            }
        }

        /// <summary>
        /// 显示简单确认弹窗（快捷方法）
        /// </summary>
        public void ShowConfirm(string content, Action onConfirm = null, Action onCancel = null)
        {
            ShowConfirm("确认", content, onConfirm, onCancel);
        }

        /// <summary>
        /// 显示简单提示弹窗（快捷方法）
        /// </summary>
        public void ShowAlert(string content, Action onConfirm = null)
        {
            ShowAlert("提示", content, onConfirm);
        }

        #endregion

        #region ItemIcon
        /// <summary>
        /// 获取一个ItemIcon
        /// </summary>
        public void GetItemIcon(Action<IItemIcon> callback)
        {
            var list = _idlePools[typeof(itemIcon)];
            ICommonUI item = null;
            if (list.Count > 0)
            {
                item = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                item.Reuse();
                CreateSuccCallback(item);
                callback((IItemIcon)item);
            }
            else
            {
                _itemIconCreater.Create((ICommonUI item)=>
                {
                    CreateSuccCallback(item);
                    callback((IItemIcon)item);
                }).Forget();
            }
        }
        #endregion
    }
}

