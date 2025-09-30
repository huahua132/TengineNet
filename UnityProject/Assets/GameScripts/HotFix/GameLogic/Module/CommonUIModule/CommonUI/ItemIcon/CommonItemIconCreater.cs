using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;
using Object = UnityEngine.Object;
using GameConfig;

namespace GameLogic
{
    public interface IItemIcon
    {
        void SetParent(GameObject gameObject);
        void SetItemID(item_ID itemId);
        void SetItemNum(int num);
    }

    public class itemIcon : CommonUIBase, IItemIcon
    {
        private GameObject _parent;
        private Text _itemName;
        private Text _itemNum;
        private RectTransform _rt;
        protected override void OnInit()
        {
            _rt = _Trf.GetComponent<RectTransform>();
            _itemName = _Trf.Find("m_textName").GetComponent<Text>();
            _itemNum = _Trf.Find("m_textNum").GetComponent<Text>();
        }

        protected override void OnRecycle()
        {
            _Trf.SetParent(null);
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
    }

    public class CommonItemIconCreater
    {
        public void Init()
        {

        }

        public void Release()
        {

        }

        public async UniTaskVoid Create(Action<ICommonUI> callback)
        {
            var gameObject = await UIModule.Resource.LoadGameObjectAsync("CommonItemIcon");
            var itemIcon = new itemIcon();
            itemIcon.Init(gameObject.transform);
            callback(itemIcon);
        }
    }
}

