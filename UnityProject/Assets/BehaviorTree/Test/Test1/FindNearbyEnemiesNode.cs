using UnityEngine;
using System.Collections.Generic;

namespace BehaviorTree.Test1
{
    [BehaviorProcessNode("Find Nearby Enemies", "Search for enemies within specified range", BehaviorProcessType.condition)]
    public class FindNearbyEnemiesNode : BehaviorProcessNodeBase
    {
        public float searchRadius = 10f;
        public string enemyTag = "Enemy";
        
        private Transform _transform;

        public override void OnCreate()
        {
            _transform = _Context.GetBindTransform();
        }

        public override void OnRemove()
        {
            _transform = null;
        }

        public override BehaviorRet OnTickRun()
        {
            if (_transform == null)
            {
                Debug.LogWarning("[FindNearbyEnemiesNode] Transform is null");
                return BehaviorRet.FAIL;
            }
            
            // 查找所有带指定Tag的敌人
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            
            if (enemies == null || enemies.Length == 0)
            {
                Debug.Log("[FindNearbyEnemiesNode] 未找到任何敌人");
                return BehaviorRet.FAIL;
            }
            
            // 过滤范围内的敌人
            List<GameObject> nearbyEnemies = new List<GameObject>();
            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(_transform.position, enemy.transform.position);
                if (distance <= searchRadius)
                {
                    nearbyEnemies.Add(enemy);
                }
            }
            
            if (nearbyEnemies.Count > 0)
            {
                // 找到最近的敌人并存储到黑板
                GameObject closestEnemy = nearbyEnemies[0];
                float closestDistance = Vector3.Distance(_transform.position, closestEnemy.transform.position);
                
                foreach (var enemy in nearbyEnemies)
                {
                    float distance = Vector3.Distance(_transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestEnemy = enemy;
                        closestDistance = distance;
                    }
                }
                
                // 将最近的敌人存储到黑板
                var blackboard = _Node.GetBlackBoardData<TargetBlackboard>();
                blackboard.target = closestEnemy.transform;
                
                Debug.Log($"[FindNearbyEnemiesNode] 找到{nearbyEnemies.Count}个敌人，最近敌人距离: {closestDistance:F2}");
                return BehaviorRet.SUCCESS;
            }
            
            Debug.Log("[FindNearbyEnemiesNode] 范围内无敌人");
            return BehaviorRet.FAIL;
        }
    }
}