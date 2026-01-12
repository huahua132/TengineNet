using UnityEngine;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 怪物脚本，作为行为树的攻击目标
    /// </summary>
    public class Monster : MonoBehaviour
    {
        [Header("怪物属性")]
        public float maxHP = 50f;
        public float currentHP = 50f;
        public float attackPower = 5f;
        public float moveSpeed = 3f;
        
        [Header("AI配置")]
        public BehaviorTreeAsset behaviorTreeAsset;
        
        private Tree _behaviorTree;
        
        private void Start()
        {
            // 确保怪物有Enemy标签
            if (!gameObject.CompareTag("Enemy"))
            {
                gameObject.tag = "Enemy";
                Debug.Log($"[Monster] 为 {gameObject.name} 自动添加Enemy标签");
            }
            
            InitializeBehaviorTree();
        }
        
        /// <summary>
        /// 初始化行为树
        /// </summary>
        private void InitializeBehaviorTree()
        {
            if (behaviorTreeAsset == null)
            {
                Debug.LogWarning($"[Monster] {gameObject.name} 未配置行为树资产，使用简单AI");
                return;
            }
            
            _behaviorTree = new Tree();
            _behaviorTree.Init(transform);
            
            bool success = _behaviorTree.InitFromAsset(behaviorTreeAsset);
            if (success)
            {
                Debug.Log($"[Monster] {gameObject.name} 行为树初始化成功");
            }
            else
            {
                Debug.LogError($"[Monster] {gameObject.name} 行为树初始化失败");
            }
        }
        
        private void Update()
        {
            if (_behaviorTree != null)
            {
                _behaviorTree.TickRun();
            }
        }
        
        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            currentHP -= damage;
            currentHP = Mathf.Max(currentHP, 0);
            
            Debug.Log($"[Monster] {gameObject.name} 受到伤害: {damage} 剩余HP: {currentHP}/{maxHP}");
            
            if (currentHP <= 0)
            {
                OnDeath();
            }
        }
        
        /// <summary>
        /// 死亡处理
        /// </summary>
        private void OnDeath()
        {
            Debug.Log($"[Monster] {gameObject.name} 已死亡");
            
            // 播放死亡效果
            // 这里可以添加死亡动画、粒子特效等
            
            // 延迟销毁
            Destroy(gameObject, 0.5f);
        }
        
        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(float amount)
        {
            currentHP += amount;
            currentHP = Mathf.Min(currentHP, maxHP);
            Debug.Log($"[Monster] {gameObject.name} 恢复HP: {amount} 当前HP: {currentHP}");
        }
        
        private void OnDestroy()
        {
            if (_behaviorTree != null)
            {
                _behaviorTree.Clear();
                _behaviorTree = null;
            }
        }
        
        private void OnDrawGizmos()
        {
            // 绘制HP条（Gizmos方式）
            Vector3 hpBarPos = transform.position + Vector3.up * 2f;
            float hpPercent = currentHP / maxHP;
            
            // HP条背景
            Gizmos.color = Color.red;
            Gizmos.DrawCube(hpBarPos, new Vector3(1f, 0.1f, 0.01f));
            
            // HP条前景
            if (hpPercent > 0)
            {
                Gizmos.color = Color.green;
                Vector3 hpBarSize = new Vector3(1f * hpPercent, 0.1f, 0.01f);
                Vector3 hpBarOffset = new Vector3((1f - 1f * hpPercent) * -0.5f, 0, 0);
                Gizmos.DrawCube(hpBarPos + hpBarOffset, hpBarSize);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 2f);
            
            // 绘制警戒范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 8f);
        }
    }
}