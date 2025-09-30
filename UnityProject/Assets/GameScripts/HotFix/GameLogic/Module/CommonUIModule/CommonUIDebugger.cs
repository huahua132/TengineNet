#if UNITY_EDITOR
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// CommonUI模块调试器
    /// 用于在Inspector中查看活跃对象和对象池状态
    /// </summary>
    public class CommonUIDebugger : MonoBehaviour
    {
        public static CommonUIDebugger Instance { get; private set; }
        
        [Header("刷新设置")]
        [Tooltip("自动刷新间隔（秒）")]
        public float autoRefreshInterval = 0.5f;

        [Header("统计信息")]
        [SerializeField] public int totalActiveCount;
        [SerializeField] public int totalIdleCount;
        
        [Header("活跃对象")]
        public UITypeInfo[] activeObjects = new UITypeInfo[0];
        
        [Header("空闲对象池")]
        public UIPoolInfo[] idlePoolObjects = new UIPoolInfo[0];
        
        private float _lastRefreshTime;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            if (Time.time - _lastRefreshTime >= autoRefreshInterval)
            {
                _lastRefreshTime = Time.time;
                RefreshData();
            }
        }

        /// <summary>
        /// 刷新调试数据
        /// </summary>
        public void RefreshData()
        {
            var module = CommonUIModule.Instance;
            if (module == null)
            {
                totalActiveCount = 0;
                totalIdleCount = 0;
                activeObjects = new UITypeInfo[0];
                idlePoolObjects = new UIPoolInfo[0];
                return;
            }

            // 统计活跃对象
            var activeDict = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var ui in module.ActiveList)
            {
                if (ui == null) continue;
                string typeName = ui.GetType().Name;
                if (!activeDict.ContainsKey(typeName))
                    activeDict[typeName] = 0;
                activeDict[typeName]++;
            }

            activeObjects = new UITypeInfo[activeDict.Count];
            int index = 0;
            totalActiveCount = 0;
            foreach (var kv in activeDict)
            {
                activeObjects[index] = new UITypeInfo
                {
                    typeName = kv.Key,
                    count = kv.Value
                };
                totalActiveCount += kv.Value;
                index++;
            }

            // 统计空闲对象池
            idlePoolObjects = new UIPoolInfo[module.IdlePools.Count];
            index = 0;
            totalIdleCount = 0;
            float currentTime = GameTime.time;
            
            foreach (var kv in module.IdlePools)
            {
                var poolInfo = new UIPoolInfo
                {
                    typeName = kv.Key.Name,
                    count = kv.Value.Count,
                    items = new UIPoolItemInfo[kv.Value.Count]
                };

                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var ui = kv.Value[i];
                    if (ui == null) continue;
                    
                    float remainTime = (ui._RecycleTime + module.ReleaseTime) - currentTime;
                    poolInfo.items[i] = new UIPoolItemInfo
                    {
                        index = i,
                        recycleTime = ui._RecycleTime,
                        remainDestroyTime = Mathf.Max(0, remainTime)
                    };
                }

                idlePoolObjects[index] = poolInfo;
                totalIdleCount += kv.Value.Count;
                index++;
            }
        }

        /// <summary>
        /// UI类型信息（活跃对象统计）
        /// </summary>
        [System.Serializable]
        public class UITypeInfo
        {
            [Tooltip("UI类型名称")]
            public string typeName;
            
            [Tooltip("数量")]
            public int count;
        }

        /// <summary>
        /// UI对象池信息
        /// </summary>
        [System.Serializable]
        public class UIPoolInfo
        {
            [Tooltip("池类型名称")]
            public string typeName;
            
            [Tooltip("池中对象数量")]
            public int count;
            
            [Tooltip("池中的对象详情")]
            public UIPoolItemInfo[] items;
        }

        /// <summary>
        /// 对象池中单个对象的信息
        /// </summary>
        [System.Serializable]
        public class UIPoolItemInfo
        {
            [Tooltip("索引")]
            public int index;
            
            [Tooltip("回收时间点")]
            public float recycleTime;
            
            [Tooltip("距离销毁的剩余时间")]
            public float remainDestroyTime;
        }
    }
}
#endif

