using TEngine;
using System.Collections.Generic;
using System;

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
        public override void OnInit()
        {
            _preCheckTime = 0;
            _toastCreater = new CommonToastCreater();
            _toastCreater.Init();
            _idlePools[typeof(Toast)] = new List<ICommonUI>();
            _popupCreater = new CommonPopupCreater();
            _popupCreater.Init();
            _idlePools[typeof(Popup)] = new List<ICommonUI>();
        }

        public override void Shutdown()
        {
            if (_toastCreater != null)
            {
                _toastCreater.Release();
                _toastCreater = null;
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

        #endregion
    }
}