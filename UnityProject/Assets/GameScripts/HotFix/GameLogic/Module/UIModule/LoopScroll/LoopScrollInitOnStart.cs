using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TEngine;
using System;

namespace GameLogic
{
    public interface ILoopScrollDataGeter
    {
        T GetData<T>(int idx);
    }

    public abstract class LoopCellBase
    {
        public Transform _Trf { get; private set; }
        protected ILoopScrollDataGeter _DataGeter;
        public bool _IsRecycle { get; private set; }
        public void Init(Transform trans, ILoopScrollDataGeter dataGeter = null)
        {
            _Trf = trans;
            _DataGeter = dataGeter;
            _IsRecycle = false;
            _Trf.gameObject.SetActive(true);
            OnInit();
        }
        public void Recycle()
        {
            OnRecycle();
            _Trf.gameObject.SetActive(false);
            _IsRecycle = true;
        }
        public void Reuse()
        {
            _IsRecycle = false;
            _Trf.gameObject.SetActive(true);
            OnReuse();
        }

        public void Refresh(int index)
        {
            OnRefresh(index);
        }

        public void Release()
        {
            OnRelease();
            RemoveAllUIEvent();
        }

                #region Event

        private GameEventMgr _eventMgr;

        protected GameEventMgr EventMgr
        {
            get
            {
                if (_eventMgr == null)
                {
                    _eventMgr = MemoryPool.Acquire<GameEventMgr>();
                }

                return _eventMgr;
            }
        }

        protected void AddEvent(int eventType, Action handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddEvent<T>(int eventType, Action<T> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddEvent<T, U>(int eventType, Action<T, U> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddEvent<T, U, V>(int eventType, Action<T, U, V> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddEvent<T, U, V, W>(int eventType, Action<T, U, V, W> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        private void RemoveAllUIEvent()
        {
            if (_eventMgr != null)
            {
                MemoryPool.Release(_eventMgr);
                _eventMgr = null;
            }
        }

        #endregion

        protected virtual void OnInit()
        {

        }
        protected virtual void OnRecycle()
        {

        }
        protected virtual void OnReuse()
        {

        }
        protected abstract void OnRefresh(int index);

        protected virtual void OnRelease()
        {

        }
    }

    public delegate LoopCellBase LoopCellBaseCreater();

    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class LoopScrollInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        public GameObject item;
        private ILoopScrollDataGeter _DataGeter;
        private LoopCellBaseCreater _creater;

        // Implement your own Cache Pool here. The following is just for example.

        Stack<LoopCellBase> pool = new Stack<LoopCellBase>();
        Dictionary<GameObject, LoopCellBase> _objCellBases = new();

        private LoopCellBase CreateCell(GameObject go)
        {
            LoopCellBase cell = _creater();
            _objCellBases[go] = cell;
            cell.Init(go.transform, _DataGeter);
            return cell;
        }
        public GameObject GetObject(int index)
        {
            //Log.Info($"GetObject {_DataGeter} {pool.Count}");
            if (pool.Count == 0)
            {
                GameObject go = Instantiate(item);
                CreateCell(go);
                return go;
            }
            LoopCellBase cell = pool.Pop();
            cell.Reuse();
            return cell._Trf.gameObject;
        }

        public void ReturnObject(Transform trans)
        {
            var cell = _objCellBases[trans.gameObject];
            //Log.Info($"ReturnObject {_DataGeter} {trans} {cell} {cell._IsRecycle}");
            if (cell._IsRecycle) return;
            cell.Recycle();
            trans.SetParent(transform, false);
            pool.Push(cell);
        }

        public void Init(LoopCellBaseCreater creater, ILoopScrollDataGeter dataGeter)
        {
            //Log.Info($"Init>>>>>>>>>>> {_DataGeter} {item}");
            var ls = GetComponent<LoopScrollRect>();
            _creater = creater;
            _DataGeter = dataGeter;
            item.SetActive(false);
            item.transform.SetParent(transform, false);
            ls.prefabSource = this;
            ls.dataSource = this;
        }
        public void ProvideData(Transform transform, int idx)
        {
            //Log.Info($"ProvideData {_DataGeter} {transform} {idx} {_objCellBases.ContainsKey(transform.gameObject)}");
            var cell = _objCellBases[transform.gameObject];
            cell.Refresh(idx);
        }

        public void OnDestroy()
        {
            foreach (var kv in _objCellBases)
            {
                kv.Value.Release();
            }
        }
    }
}