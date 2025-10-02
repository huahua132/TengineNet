using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public enum RedDotType  //红点类型
    {
        RED,                //纯红点
        RED_NUM,            //带数字
    }
    public interface IRedDot
    {
        void SetParent(GameObject gameObject);
        void SetRecycle();
    }

    public class RedDot : CommonUIBase, IRedDot
    {
        private Transform _poolParent;
        private GameObject _parent;
        private string _word;
        private RedDotType _type;
        private Text _num;
        private RectTransform _rt;
        protected override void OnInit()
        {
            _rt = _Trf.GetComponent<RectTransform>();
            _num = _Trf.Find("m_Num").GetComponent<Text>();
        }

        protected override void OnRecycle()
        {
            _Trf.SetParent(_poolParent);
            if (_word != "")
            {
                GameEvent.RemoveEventListener(_word, Refresh);
            }
            _word = "";
            _type = 0;
        }

        protected override void OnRelease()
        {
            if (_word != "")
            {
                GameEvent.RemoveEventListener(_word, Refresh);
            }
        }

        //返回true表示可以回收
        protected override bool OnUpdate()
        {
            if (_parent == null)
            {
                return true;
            }
            return false;
        }

        private void Refresh()
        {
            var isHave = GameModule.RedDot.ContainWord(_word);
            _Trf.gameObject.SetActive(isHave);
            _num.gameObject.SetActive(_type == RedDotType.RED_NUM);
            if (_type == RedDotType.RED_NUM)
            {
                int num = GameModule.RedDot.GetNodeChildCount(_word);
                _num.text = num.ToString();
            }
        }

        public void SetPoolParent(Transform poolParent)
        {
            if (_poolParent == null) return;
            _poolParent = poolParent;
            _Trf.SetParent(_poolParent);
        }

        public void SetWordAndType(string word, RedDotType rtype)
        {
            _word = word;
            _type = rtype;
            Refresh();
            GameEvent.AddEventListener(word, Refresh);
        }

        public void SetParent(GameObject gameObject)
        {
            _parent = gameObject;
            _Trf.SetParent(gameObject.transform);
            _rt.localPosition = Vector3.zero;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
            _rt.localScale = Vector3.one;
        }

        public void SetRecycle()
        {
            if (_poolParent == null || _Trf == null) return;
            _Trf.SetParent(_poolParent);
            _parent = null;
        }
    }

    public class CommonRedDotCreater
    {
        private GameObject _RedDotPool;
        public void Init()
        {
            _RedDotPool = new GameObject("RedDotPool");
            Object.DontDestroyOnLoad(_RedDotPool);
        }

        public void Release()
        {
            Object.Destroy(_RedDotPool);
        }

        public async UniTaskVoid Create(Action<ICommonUI> callback)
        {
            var gameObject = await UIModule.Resource.LoadGameObjectAsync("CommonRedDot");
            var redDot = new RedDot();
            redDot.Init(gameObject.transform);
            redDot.SetPoolParent(_RedDotPool.transform);
            callback(redDot);
        }
    }
}
