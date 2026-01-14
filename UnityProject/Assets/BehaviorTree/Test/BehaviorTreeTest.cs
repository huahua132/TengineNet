using UnityEngine;
using BehaviorTree;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 行为树测试脚本
/// 演示如何在运行时使用行为树系统
/// </summary>
public class BehaviorTreeTest : MonoBehaviour
{
    [Header("行为树配置")]
    [Tooltip("要运行的行为树资源")]
    public BehaviorTreeAsset treeAsset;
    
    [Tooltip("绑定的Transform对象，如果为空则使用当前对象的Transform")]
    public Transform bindTransform;
    
    [Header("运行配置")]
    [Tooltip("是否在 Start 时自动运行")]
    public bool autoRun = true;
    
    [Tooltip("是否启用调试模式")]
    public bool enableDebug = true;
    
    [Tooltip("每帧执行行为树")]
    public bool runEveryFrame = true;
    
    [Header("手动控制")]
    [Tooltip("使用空格键手动执行一次")]
    public KeyCode manualRunKey = KeyCode.Space;
    
    [Header("状态显示")]
    [Tooltip("显示最后一次执行结果")]
    public BehaviorRet lastResult = BehaviorRet.SUCCESS;
    
    [Tooltip("显示总执行次数")]
    public int executeCount = 0;
    
    private BehaviorTree.Tree _behaviorTree;
    private bool _isInitialized = false;

    void Start()
    {
        if (autoRun)
        {
            InitializeBehaviorTree();
        }
    }

    void Update()
    {
        // 手动执行
        if (Input.GetKeyDown(manualRunKey))
        {
            if (!_isInitialized)
            {
                InitializeBehaviorTree();
            }
            ExecuteBehaviorTree();
        }
        
        // 每帧执行
        if (runEveryFrame && _isInitialized)
        {
            ExecuteBehaviorTree();
        }
    }

    /// <summary>
    /// 初始化行为树
    /// </summary>
    public void InitializeBehaviorTree()
    {
        if (treeAsset == null)
        {
            Debug.LogError("[BehaviorTreeTest] 请先设置 treeAsset!");
            return;
        }

        // 确定绑定的Transform
        Transform targetTransform = bindTransform != null ? bindTransform : transform;

        // 创建行为树实例，并绑定Transform
        _behaviorTree = new BehaviorTree.Tree();
        _behaviorTree.Init(targetTransform);
        
        // 从资源加载
        bool success = _behaviorTree.InitFromAsset(treeAsset);
        if (!success)
        {
            Debug.LogError("[BehaviorTreeTest] 行为树初始化失败!");
            return;
        }

        _isInitialized = true;
        executeCount = 0;

        Debug.Log($"[BehaviorTreeTest] 行为树初始化成功: {treeAsset.name}, 绑定Transform: {targetTransform.name}");
    }

    /// <summary>
    /// 执行行为树
    /// </summary>
    public void ExecuteBehaviorTree()
    {
        if (!_isInitialized || _behaviorTree == null)
        {
            Debug.LogWarning("[BehaviorTreeTest] 行为树未初始化!");
            return;
        }

        lastResult = _behaviorTree.TickRun();
        executeCount++;
        
        if (enableDebug)
        {
            Debug.Log($"[BehaviorTreeTest] 执行 #{executeCount}, 结果: {lastResult}");
        }
    }

    /// <summary>
    /// 节点状态变化回调
    /// </summary>
    private void OnNodeStatusChanged(int nodeId, BehaviorRet status)
    {
        if (enableDebug)
        {
            Debug.Log($"[BehaviorTreeTest] 节点 {nodeId} 状态: {status}");
        }

#if UNITY_EDITOR
        // 在编辑器中更新可视化状态
        UpdateEditorWindow(nodeId, status);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 更新编辑器窗口状态（仅在编辑器中可用）
    /// 使用反射避免直接引用编辑器类型
    /// </summary>
    private void UpdateEditorWindow(int nodeId, BehaviorRet status)
    {
        try
        {
            // 使用反射获取编辑器窗口类型
            var editorWindowType = System.Type.GetType("BehaviorTree.Editor.BehaviorTreeEditorWindow, Assembly-CSharp-Editor");
            if (editorWindowType == null) return;

            // 获取 GetWindow 方法
            var getWindowMethod = typeof(EditorWindow).GetMethod("GetWindow",
                new System.Type[] { typeof(System.Type), typeof(string), typeof(bool) });
            if (getWindowMethod == null) return;

            // 调用 GetWindow 获取窗口实例
            var window = getWindowMethod.Invoke(null, new object[] { editorWindowType, null, false });
            if (window == null) return;

            // 调用 UpdateNodeStatus 方法
            var updateMethod = editorWindowType.GetMethod("UpdateNodeStatus");
            if (updateMethod != null)
            {
                updateMethod.Invoke(window, new object[] { nodeId, status });
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[BehaviorTreeTest] 更新编辑器窗口失败: {ex.Message}");
        }
    }
#endif

    /// <summary>
    /// 重置行为树
    /// </summary>
    public void ResetBehaviorTree()
    {
        if (_behaviorTree != null)
        {
            _behaviorTree.Clear();
        }
        _isInitialized = false;
        executeCount = 0;
        Debug.Log("[BehaviorTreeTest] 行为树已重置");
    }

    void OnDestroy()
    {
        // 清理资源
        if (_behaviorTree != null)
        {
            _behaviorTree.Clear();
        }
    }

    void OnGUI()
    {
        if (!_isInitialized) return;

        // 在屏幕上显示状态信息
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Box("Behavior Tree Status");
        GUILayout.Label($"Asset: {treeAsset?.name ?? "None"}");
        GUILayout.Label($"Execute Count: {executeCount}");
        GUILayout.Label($"Last Result: {lastResult}");
        GUILayout.Label($"Debug Mode: {(enableDebug ? "ON" : "OFF")}");
        
        if (GUILayout.Button("Reset Tree"))
        {
            ResetBehaviorTree();
            InitializeBehaviorTree();
        }
        
        GUILayout.EndArea();
    }
}

#if UNITY_EDITOR
/// <summary>
/// 自定义编辑器，提供更好的测试界面
/// </summary>
[CustomEditor(typeof(BehaviorTreeTest))]
public class BehaviorTreeTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("测试控制", EditorStyles.boldLabel);
        
        BehaviorTreeTest script = (BehaviorTreeTest)target;
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("初始化行为树", GUILayout.Height(30)))
        {
            script.InitializeBehaviorTree();
        }
        
        if (GUILayout.Button("执行一次", GUILayout.Height(30)))
        {
            script.ExecuteBehaviorTree();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("重置行为树", GUILayout.Height(30)))
        {
            script.ResetBehaviorTree();
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "使用说明：\n" +
            "1. 拖入一个 BehaviorTreeAsset 到 treeAsset 字段\n" +
            "2. 点击 '初始化行为树' 或运行游戏自动初始化\n" +
            "3. 点击 '执行一次' 手动执行，或设置 runEveryFrame 每帧执行\n" +
            "4. 启用 enableDebug 可以在 Console 看到详细日志\n" +
            "5. 运行时可以打开行为树编辑器看到实时状态",
            MessageType.Info);
    }
}
#endif