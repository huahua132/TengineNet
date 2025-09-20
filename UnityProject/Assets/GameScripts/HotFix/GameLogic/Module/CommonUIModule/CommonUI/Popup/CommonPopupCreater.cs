using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public interface IPopup
    {
    }

    public class Popup : CommonUIBase, IPopup
    {
        private Text _titile;
        private Text _content;
        private Button _cancel;
        private Text _cancelTxt;
        private Button _confirm;
        private Text _confirmTxt;
        //初始化
        protected override void OnInit()
        {
            _titile = _Trf.Find("Titile").GetComponent<Text>();
            _content = _Trf.Find("Content").GetComponent<Text>();
            _cancel = _Trf.Find("CancelBtn").GetComponent<Button>();
            _cancelTxt = _Trf.Find("CancelBtn").GetComponentInChildren<Text>();
            _confirm = _Trf.Find("ConfirmBtn").GetComponent<Button>();
            _confirmTxt = _Trf.Find("ConfirmBtn").GetComponentInChildren<Text>();
        }
        //回收
        protected override void OnRecycle()
        {

        }
        //重用
        protected override void OnReuse()
        {

        }
        //更新为true则回收
        protected override bool OnUpdate()
        {
            return false;
        }
        //释放
        protected override void OnRelease()
        {
            
        }
    }

    public class CommonPopupCreater
    {
        private CommonPopupUI _window;
        private bool isRelease = false;
        public void Init()
        {
            GameModule.UI.ShowUI<CommonPopupUI>();
        }

        public void Release()
        {
            GameModule.UI.CloseUI<CommonPopupUI>();
            _window = null;
            isRelease = true;
        }

        public async UniTaskVoid Create(Action<Popup> callback)
        {
            if (isRelease) return;
            if (_window == null)
            {
                _window = await GameModule.UI.GetUIAsyncAwait<CommonPopupUI>();
            }
            if (isRelease)
            {
                _window = null;
                return;
            }
            Transform trf = Object.Instantiate(_window.Popup, _window.transform);
            var popup = new Popup();
            popup.Init(trf);
            callback(popup);
        }
    }
}

