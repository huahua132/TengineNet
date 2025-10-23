using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TEngine;
using Object = UnityEngine.Object;
using TMPro;

namespace GameLogic
{
    public interface IToast
    {
        void SetTxt(string txt);
    }

    public class Toast : CommonUIBase, IToast
    {
        private TextMeshProUGUI _txt;
        // 添加动画相关变量
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector2 _startPos;
        private Vector2 _currentPos;
        
        // 动画时间配置
        private float _initialWaitTime = 0.0f;   // 初始停顿时间
        private float _moveTime = 0.5f;          // 向上移动时间
        private float _pauseTime = 0.3f;         // 移动后停顿时间
        private float _fadeTime = 0.5f;          // 淡出时间
        private float _moveDistance = 150f;      // 向上移动距离

        protected override void OnInit()
        {
            _txt = _Trf.GetComponentInChildren<TextMeshProUGUI>();
            
            // 添加动画组件初始化
            _rectTransform = _Trf.GetComponent<RectTransform>();
            _canvasGroup = _Trf.GetComponent<CanvasGroup>();
            _startPos = _rectTransform.anchoredPosition;
            _currentPos = _startPos;
            _canvasGroup.alpha = 1f;
        }

        protected override void OnRecycle()
        {
            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _startPos;
            _currentPos = _startPos;
        }

        protected override bool OnUpdate()
        {
            float elapsedTime = GameTime.time - _ShowTime;

            // 阶段1：初始停顿
            if (elapsedTime <= _initialWaitTime)
            {
                return false;
            }

            // 阶段2：向上移动（保持不透明）
            float moveStartTime = _initialWaitTime;
            float moveEndTime = _initialWaitTime + _moveTime;
            
            if (elapsedTime <= moveEndTime)
            {
                float moveProgress = (elapsedTime - moveStartTime) / _moveTime;
                
                // 向上移动（可选使用缓动函数让移动更平滑）
                float easedProgress = EaseOutQuad(moveProgress);
                _currentPos.x = _startPos.x;
                _currentPos.y = _startPos.y + _moveDistance * easedProgress;
                _rectTransform.anchoredPosition = _currentPos;
                
                // 保持完全不透明
                _canvasGroup.alpha = 1f;
                
                return false;
            }

            // 阶段3：移动后停顿
            float pauseEndTime = moveEndTime + _pauseTime;
            
            if (elapsedTime <= pauseEndTime)
            {
                // 保持在移动后的位置，保持不透明
                _currentPos.x = _startPos.x;
                _currentPos.y = _startPos.y + _moveDistance;
                _rectTransform.anchoredPosition = _currentPos;
                _canvasGroup.alpha = 1f;
                
                return false;
            }

            // 阶段4：淡出
            float fadeStartTime = pauseEndTime;
            float fadeEndTime = pauseEndTime + _fadeTime;
            
            if (elapsedTime <= fadeEndTime)
            {
                float fadeProgress = (elapsedTime - fadeStartTime) / _fadeTime;
                
                // 保持位置不变，只改变透明度
                _currentPos.x = _startPos.x;
                _currentPos.y = _startPos.y + _moveDistance;
                _rectTransform.anchoredPosition = _currentPos;
                
                // 淡出
                _canvasGroup.alpha = 1f - fadeProgress;
                
                return false;
            }

            // 动画完成，可以销毁
            return true;
        }
        
        // 缓动函数：让移动更平滑（可选）
        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
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
            GameModule.UI.ShowUIAsync<CommonToastUI>();
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

