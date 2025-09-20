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
        private Vector2 _startPos;              // 改为Vector2，RectTransform用的是Vector2
        private Vector2 _currentPos;            // 缓存当前位置，避免重复创建
        private float _waitTime = 1.0f;         // 停顿时间
        private float _animTime = 0.5f;         // 向上移动动画时间
        private float _moveDistance = 150f;     // 向上移动距离

        protected override void OnInit()
        {
            _txt = _Trf.GetComponentInChildren<Text>();
            
            // 添加动画组件初始化
            _rectTransform = _Trf.GetComponent<RectTransform>();
            _canvasGroup = _Trf.GetComponent<CanvasGroup>();
            _startPos = _rectTransform.anchoredPosition;
            _currentPos = _startPos;                    // 初始化当前位置
            _canvasGroup.alpha = 1f;
        }

        protected override void OnRecycle()
        {
            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _startPos;
            _currentPos = _startPos;                    // 重置当前位置
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
                // 向上移动 - 直接修改缓存的Vector2
                _currentPos.x = _startPos.x;
                _currentPos.y = _startPos.y + _moveDistance * animProgress;
                _rectTransform.anchoredPosition = _currentPos;

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

        public async UniTaskVoid Create(Action<ICommonUI> callback, string txt)
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

