using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public interface IToast
    {
        void SetTxt(string txt);
    }

    public class Toast : CommonUIBase, IToast
    {
        private Text _txt;
        // 添加动画相关变量
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector3 _startPos;
        private float _waitTime = 1.0f;      // 停顿时间
        private float _animTime = 0.5f;      // 向上移动动画时间
        private float _moveDistance = 150f;   // 向上移动距离

        protected override void OnInit()
        {
            _txt = _Trf.GetComponentInChildren<Text>();
            
            // 添加动画组件初始化
            _rectTransform = _Trf.GetComponent<RectTransform>();
            _canvasGroup = _Trf.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _Trf.gameObject.AddComponent<CanvasGroup>();
            }
            _startPos = _rectTransform.anchoredPosition;
            _canvasGroup.alpha = 1f;
        }

        protected override bool OnUpdate()
        {
            float elapsedTime = GameTime.time - _ShowTime;
            
            // 前1秒停顿显示
            if (elapsedTime <= _waitTime)
            {
                return false;
            }
            
            // 1秒后开始向上移动并淡出
            float animProgress = (elapsedTime - _waitTime) / _animTime;
            
            if (animProgress <= 1f)
            {
                // 向上移动
                float moveY = _startPos.y + _moveDistance * animProgress;
                _rectTransform.anchoredPosition = new Vector3(_startPos.x, moveY, _startPos.z);
                
                // 淡出
                _canvasGroup.alpha = 1f - animProgress;
                
                return false;
            }
            
            // 动画完成，可以销毁
            return true;
        }

        public void SetTxt(string txt)
        {
            _txt.text = txt;
        }
    }

    public class CommonToastCreater
    {
        private CommonToastUI _window;
        private bool isRelease = false;
        public void Init()
        {
            GameModule.UI.ShowUI<CommonToastUI>();
        }

        public void Release()
        {
            GameModule.UI.CloseUI<CommonToastUI>();
            _window = null;
            isRelease = true;
        }

        public async UniTaskVoid Create(Action<Toast> callback, string txt)
        {
            if (isRelease) return;
            if (_window == null)
            {
                _window = await GameModule.UI.GetUIAsyncAwait<CommonToastUI>();
            }
            if (isRelease)
            {
                _window = null;
                return;
            }
            Transform trf = Object.Instantiate(_window.Toast, _window.transform);
            var toast = new Toast();
            toast.Init(trf);
            toast.SetTxt(txt);
            callback(toast);
        }
    }
}

