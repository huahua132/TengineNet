using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;
using Object = UnityEngine.Object;
using TMPro;

namespace GameLogic
{
    public interface IPopup
    {
        void SetTitle(string title);
        void SetContent(string content);
        void SetButtons(string cancelText = "取消", string confirmText = "确定");
        void SetCallbacks(Action onCancel = null, Action onConfirm = null);
        void HideCancelButton();
    }

    public class Popup : CommonUIBase, IPopup
    {
        private TextMeshProUGUI _titile;
        private TextMeshProUGUI _content;
        private Button _cancel;
        private TextMeshProUGUI _cancelTxt;
        private Button _confirm;
        private TextMeshProUGUI _confirmTxt;
        private Action _onCancel;
        private Action _onConfirm;
        private bool _isCanClose;

        //初始化
        protected override void OnInit()
        {
            _titile = _Trf.Find("Titile").GetComponent<TextMeshProUGUI>();
            _content = _Trf.Find("Content").GetComponent<TextMeshProUGUI>();
            _cancel = _Trf.Find("b/CancelBtn").GetComponent<Button>();
            _cancelTxt = _Trf.Find("b/CancelBtn").GetComponentInChildren<TextMeshProUGUI>();
            _confirm = _Trf.Find("b/ConfirmBtn").GetComponent<Button>();
            _confirmTxt = _Trf.Find("b/ConfirmBtn").GetComponentInChildren<TextMeshProUGUI>();
            _isCanClose = false;
            // 绑定按钮事件
            _cancel.onClick.AddListener(OnCancelClick);
            _confirm.onClick.AddListener(OnConfirmClick);
        }

        //回收
        protected override void OnRecycle()
        {
            // 清理回调
            _onCancel = null;
            _onConfirm = null;
            _isCanClose = false;

            // 重置文本
            _titile.text = "";
            _content.text = "";
            _cancelTxt.text = "取消";
            _confirmTxt.text = "确定";
            _cancel.gameObject.SetActive(true);
        }

        //重用
        protected override void OnReuse()
        {

        }

        //更新为true则回收
        protected override bool OnUpdate()
        {
            return _isCanClose;
        }

        //释放
        protected override void OnRelease()
        {
            // 移除按钮监听
            if (_cancel != null)
                _cancel.onClick.RemoveListener(OnCancelClick);
            if (_confirm != null)
                _confirm.onClick.RemoveListener(OnConfirmClick);
        }

        public void SetTitle(string title)
        {
            _titile.text = title;
        }

        public void SetContent(string content)
        {
            _content.text = content;
        }

        public void SetButtons(string cancelText = "取消", string confirmText = "确定")
        {
            _cancelTxt.text = cancelText;
            _confirmTxt.text = confirmText;
        }

        public void SetCallbacks(Action onCancel = null, Action onConfirm = null)
        {
            _onCancel = onCancel;
            _onConfirm = onConfirm;
        }
        private void OnCancelClick()
        {
            _onCancel?.Invoke();
            _isCanClose = true;
        }

        private void OnConfirmClick()
        {
            _onConfirm?.Invoke();
            _isCanClose = true;
        }
        public void HideCancelButton()
        {
            _cancel.gameObject.SetActive(false);
        }
    }

    public class CommonPopupCreater
    {
        private CommonPopupUI _window;
        private bool isRelease = false;

        public void Init()
        {
            GameModule.UI.ShowUIAsync<CommonPopupUI>();
        }

        public void Release()
        {
            GameModule.UI.CloseUI<CommonPopupUI>();
            _window = null;
            isRelease = true;
        }

        public async UniTaskVoid Create(Action<ICommonUI> callback)
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

