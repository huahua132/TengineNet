using UnityEngine;
using BehaviorTree;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 英雄角色脚本，用于测试行为树
    /// </summary>
    public class Hero : MonoBehaviour
    {
        [Header("角色属性")]
        public float maxHP = 100f;
        public float currentHP = 100f;
        public float attackPower = 10f;
        public float moveSpeed = 5f;
        
        [Header("行为树配置")]
        public BehaviorTreeAsset behaviorTreeAsset;
        
        private Tree _behaviorTree;
        
        private void Start()
        {
            InitializeBehaviorTree();
        }
        
        /// <summary>
        /// 初始化行为树
        /// </summary>
        private void InitializeBehaviorTree()
        {
            if (behaviorTreeAsset == null)
            {
                Debug.LogWarning($"[Hero] {gameObject.name} 未配置行为树资产");
                return;
            }
            
            _behaviorTree = new Tree();
            _behaviorTree.Init(transform);
            
            bool success = _behaviorTree.InitFromAsset(behaviorTreeAsset);
            if (success)
            {
                Debug.Log($"[Hero] {gameObject.name} 行为树初始化成功");
            }
            else
            {
                Debug.LogError($"[Hero] {gameObject.name} 行为树初始化失败");
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
            
            Debug.Log($"[Hero] {gameObject.name} 受到伤害: {damage} 剩余HP: {currentHP}");
            
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
            Debug.Log($"[Hero] {gameObject.name} 已死亡");
            // 可以添加死亡动画、效果等
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(float amount)
        {
            currentHP += amount;
            currentHP = Mathf.Min(currentHP, maxHP);
            Debug.Log($"[Hero] {gameObject.name} 恢复HP: {amount} 当前HP: {currentHP}");
        }
        
        private void OnDestroy()
        {
            if (_behaviorTree != null)
            {
                _behaviorTree.Clear();
                _behaviorTree = null;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // 绘制攻击范围（如果有）
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 2f);
            
            // 绘制搜索范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 10f);
        }
    }
}