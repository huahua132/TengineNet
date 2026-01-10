using UnityEngine;
using BehaviorTree;

public class BehaviorTreeTest : MonoBehaviour
{
    public BehaviorTreeAsset treeAsset;
    private BehaviorTree.BehaviorTree _behaviorTree;

    private void Start()
    {
        // 初始化行为树
        _behaviorTree = new BehaviorTree.BehaviorTree();
        _behaviorTree.Init();
        
        // 从资产加载
        if (!_behaviorTree.InitFromAsset(treeAsset))
        {
            Debug.LogError("Failed to initialize behavior tree");
            return;
        }

        Debug.Log("Behavior tree initialized successfully");
    }

    private void Update()
    {
        if (_behaviorTree != null)
        {
            // 每帧执行行为树
            var ret = _behaviorTree.TickRun();
            
            // 可以根据返回值做处理
            switch (ret)
            {
                case BehaviorRet.SUCCESS:
                    Debug.Log("Tree completed successfully");
                    break;
                case BehaviorRet.FAIL:
                    Debug.Log("Tree failed");
                    break;
                case BehaviorRet.RUNNING:
                    // 继续运行
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (_behaviorTree != null)
        {
            _behaviorTree.Clear();
        }
    }
}

