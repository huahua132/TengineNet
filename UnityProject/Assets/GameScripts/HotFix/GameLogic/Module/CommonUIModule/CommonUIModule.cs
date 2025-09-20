using TEngine;
using System.Collections.Generic;
using System;
using UnityEngine;
using YooAsset;

namespace GameLogic
{
    public class CommonUIModule : Module, IUpdateModule, ICommonUIModule
    {
        private float _releaseTime = 60f;
        private List<ICommonUI> _activeList = new List<ICommonUI>();
        private Dictionary<Type, List<ICommonUI>> _idlePools = new();
        private CommonToastCreater _ToastCreater;
        public override void OnInit()
        {
            _ToastCreater = new CommonToastCreater();
            _ToastCreater.Init();
            _idlePools[typeof(Toast)] = new List<ICommonUI>();
        }

        public override void Shutdown()
        {
            if (_ToastCreater != null)
            {
                _ToastCreater.Release();
                _ToastCreater = null;
            }
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

            var curTime = Time.time;
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

        private void CreateSuccCallback(ICommonUI toast)
        {
            _activeList.Add(toast);
        }

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
            }
            else
            {
                _ToastCreater.Create(CreateSuccCallback, txt).Forget();
            }
        }
    }
}