using UnityEngine;
using BehaviorTree;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BehaviorTreeTest : MonoBehaviour
{
    public BehaviorTreeAsset treeAsset;
    private BehaviorTree.BehaviorTree _behaviorTree;
    
    [Header("Debug Settings")]
    public bool enableDebug = true;
    public bool logNodeStatus = false;

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

        // 设置调试回调
        if (enableDebug)
        {
            _behaviorTree.OnNodeStatusChanged += OnNodeStatusChanged;
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
            if (logNodeStatus)
            {
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
    }

    private void OnNodeStatusChanged(int nodeId, BehaviorRet status)
    {
        if (logNodeStatus)
        {
            Debug.Log($"Node {nodeId} status: {status}");
        }

#if UNITY_EDITOR
        // 在编辑器模式下，将状态发送到编辑器窗口
        if (EditorWindow.HasOpenInstances<BehaviorTree.Editor.BehaviorTreeEditorWindow>())
        {
            var window = EditorWindow.GetWindow<BehaviorTree.Editor.BehaviorTreeEditorWindow>(null, false);
            if (window != null)
            {
                window.UpdateNodeStatus(nodeId, status);
            }
        }
#endif
    }

    private void OnDestroy()
    {
        if (_behaviorTree != null)
        {
            if (enableDebug)
            {
                _behaviorTree.OnNodeStatusChanged -= OnNodeStatusChanged;
            }
            _behaviorTree.Clear();
        }
        
#if UNITY_EDITOR
        // 清除编辑器中的运行时状态
        if (EditorWindow.HasOpenInstances<BehaviorTree.Editor.BehaviorTreeEditorWindow>())
        {
            var window = EditorWindow.GetWindow<BehaviorTree.Editor.BehaviorTreeEditorWindow>(null, false);
            if (window != null)
            {
                window.ClearRuntimeStatus();
            }
        }
#endif
    }
    
#if UNITY_EDITOR
    [ContextMenu("Open in Editor")]
    private void OpenInEditor()
    {
        if (treeAsset != null)
        {
            var window = EditorWindow.GetWindow<BehaviorTree.Editor.BehaviorTreeEditorWindow>("Behavior Tree Editor");
            window.minSize = new Vector2(1200, 600);
            
            // 通过反射设置当前资源
            var field = typeof(BehaviorTree.Editor.BehaviorTreeEditorWindow).GetField("_currentAsset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(window, treeAsset);
                
                var method = typeof(BehaviorTree.Editor.BehaviorTreeEditorWindow).GetMethod("OnAssetChanged",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(window, null);
                }
            }
            
            // 设置运行时树
            window.SetRuntimeTree(_behaviorTree);
            
            window.Show();
            window.Focus();
        }
    }
#endif
}

