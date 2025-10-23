using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using GameConfig;
using TMPro;

namespace GameLogic
{
    public interface IItemIcon
    {
        void SetParent(GameObject gameObject);
        void SetItemID(item_ID itemId);
        void SetItemNum(int num);
        void SetRecycle();
    }

    public class itemIcon : CommonUIBase, IItemIcon
    {
        private Transform _poolParent;
        private GameObject _parent;
        private TextMeshProUGUI _itemName;
        private TextMeshProUGUI _itemNum;
        private RectTransform _rt;
        protected override void OnInit()
        {
            _rt = _Trf.GetComponent<RectTransform>();
            _itemName = _Trf.Find("m_textName").GetComponent<TextMeshProUGUI>();
            _itemNum = _Trf.Find("m_textNum").GetComponent<TextMeshProUGUI>();
        }

        protected override void OnRecycle()
        {
            _Trf.SetParent(_poolParent);
        }

        protected override void OnRelease()
        {
           
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

        public void SetPoolParent(Transform poolParent)
        {
            if (_poolParent == null) return;
            _poolParent = poolParent;
            _Trf.SetParent(_poolParent);
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

        public void SetItemID(item_ID itemId)
        {
            var cfg = GameModule.GameConfig.TbItemInfo.Get(itemId);
            _itemName.text = cfg.ItemName;
        }
        public void SetItemNum(int num)
        {
            _itemNum.text = num.ToString();
        }

        public void SetRecycle()
        {
            if (_poolParent == null || _Trf == null) return;
            _Trf.SetParent(_poolParent);
            _parent = null;
        }
    }

    public class CommonItemIconCreater
    {
        private GameObject _itemIconPool;
        public void Init()
        {
            _itemIconPool = new GameObject("ItemIconPool");
            Object.DontDestroyOnLoad(_itemIconPool);
        }

        public void Release()
        {
            Object.Destroy(_itemIconPool);
        }

        public async UniTaskVoid Create(Action<ICommonUI> callback)
        {
            var gameObject = await UIModule.Resource.LoadGameObjectAsync("CommonItemIcon");
            var itemIcon = new itemIcon();
            itemIcon.Init(gameObject.transform);
            itemIcon.SetPoolParent(_itemIconPool.transform);
            callback(itemIcon);
        }
    }
}

