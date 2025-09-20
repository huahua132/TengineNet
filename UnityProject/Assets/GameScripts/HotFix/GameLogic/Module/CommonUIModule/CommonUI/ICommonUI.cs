using UnityEngine;
using TEngine;

namespace GameLogic
{
    public interface ICommonUI
    {
        float _RecycleTime { get; set; }
        void Init(Transform trans);
        void Recycle();
        void Reuse();
        void Release();
        bool Update();
    }

    public abstract class CommonUIBase : ICommonUI
    {
        protected Transform _Trf;
        public float _RecycleTime { get; set; }                         //回收时间
        public float _ShowTime { get; private set; }                    //显示时间
        public void Init(Transform trans)
        {
            _Trf = trans;
            _Trf.gameObject.SetActive(true);
            _RecycleTime = 0;
            _ShowTime = GameTime.time;
            OnInit();
        }
        public void Recycle()
        {
            _Trf.gameObject.SetActive(false);
            OnRecycle();
            _RecycleTime = GameTime.time;
        }
        public void Reuse()
        {
            _ShowTime = GameTime.time;
            _Trf.gameObject.SetActive(true);
            _Trf.SetAsLastSibling();
            OnReuse();
        }
        public void Release()
        {
            OnRelease();
            if (_Trf)
            {
                Object.Destroy(_Trf.gameObject);
                _Trf = null;
            }
            _RecycleTime = 0;
            _ShowTime = 0;
        }
        public bool Update()
        {
            return OnUpdate();
        }

        protected virtual void OnInit()
        {

        }
        protected virtual void OnRecycle()
        {

        }
        protected virtual void OnReuse()
        {

        }
        protected virtual void OnRelease()
        {

        }
        protected virtual bool OnUpdate()
        {
            return false;
        }      
    }
}