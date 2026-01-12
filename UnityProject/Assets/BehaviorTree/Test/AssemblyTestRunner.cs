using UnityEngine;
using BehaviorTree;

namespace BehaviorTree.Test
{
    /// <summary>
    /// 程序集隔离功能测试运行器
    /// 用于在运行时验证程序集配置是否正常工作
    /// </summary>
    public class AssemblyTestRunner : MonoBehaviour
    {
        [Header("测试资产")]
        [Tooltip("指定用于测试的行为树资产")]
        public BehaviorTreeAsset testAsset;
        
        [Header("测试选项")]
        [Tooltip("启动时自动运行测试")]
        public bool runOnStart = true;
        
        private Tree _tree;
        
        private void Start()
        {
            if (runOnStart)
            {
                RunTest();
            }
        }
        
        /// <summary>
        /// 运行程序集隔离测试
        /// </summary>
        [ContextMenu("Run Assembly Test")]
        public void RunTest()
        {
            Debug.Log("=== 开始行为树程序集隔离测试 ===");
            
            if (testAsset == null)
            {
                Debug.LogError("测试失败: 未指定测试资产！");
                return;
            }
            
            // 显示资产配置信息
            LogAssetInfo();
            
            // 尝试初始化行为树
            bool success = InitializeBehaviorTree();
            
            if (success)
            {
                Debug.Log("✅ 测试通过: 行为树初始化成功");
                Debug.Log($"已创建 {testAsset.nodes.Count} 个节点");
                
                // 显示所有节点信息
                LogNodesInfo();
            }
            else
            {
                Debug.LogError("❌ 测试失败: 行为树初始化失败，请检查控制台错误信息");
            }
            
            Debug.Log("=== 测试结束 ===");
        }
        
        /// <summary>
        /// 初始化行为树
        /// </summary>
        private bool InitializeBehaviorTree()
        {
            try
            {
                // 清理之前的树
                if (_tree != null)
                {
                    _tree.Clear();
                    _tree = null;
                }
                
                // 创建新的行为树
                _tree = new Tree();
                _tree.Init(transform);
                
                // 从资产初始化
                bool result = _tree.InitFromAsset(testAsset);
                
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"初始化行为树时发生异常: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 记录资产信息
        /// </summary>
        private void LogAssetInfo()
        {
            Debug.Log($"<color=cyan>测试资产信息:</color>");
            Debug.Log($"  名称: {testAsset.treeName}");
            Debug.Log($"  归属程序集: {(string.IsNullOrEmpty(testAsset.ownerAssembly) ? "未设置" : testAsset.ownerAssembly)}");
            
            if (testAsset.sharedAssemblies != null && testAsset.sharedAssemblies.Count > 0)
            {
                Debug.Log($"  共享程序集: {string.Join(", ", testAsset.sharedAssemblies)}");
            }
            else
            {
                Debug.Log("  共享程序集: 无");
            }
            
            var allowedAssemblies = testAsset.GetAllowedAssemblies();
            if (allowedAssemblies.Count > 0)
            {
                Debug.Log($"  <color=green>允许的程序集列表: {string.Join(", ", allowedAssemblies)}</color>");
            }
            else
            {
                Debug.Log("  <color=yellow>无程序集限制（允许所有）</color>");
            }
        }
        
        /// <summary>
        /// 记录节点信息
        /// </summary>
        private void LogNodesInfo()
        {
            Debug.Log($"<color=cyan>节点列表:</color>");
            
            if (testAsset.nodes == null || testAsset.nodes.Count == 0)
            {
                Debug.Log("  无节点");
                return;
            }
            
            foreach (var node in testAsset.nodes)
            {
                Debug.Log($"  [{node.id}] ({node.processTypeName})");
            }
        }
        
        /// <summary>
        /// 执行一次行为树Tick（用于运行时测试）
        /// </summary>
        [ContextMenu("Execute Tree Tick")]
        public void ExecuteTick()
        {
            if (_tree == null)
            {
                Debug.LogWarning("行为树未初始化，请先运行测试");
                return;
            }
            
            Debug.Log("执行行为树Tick...");
            var result = _tree.TickRun();
            Debug.Log($"执行结果: {result}");
        }
        
        private void OnDestroy()
        {
            if (_tree != null)
            {
                _tree.Clear();
                _tree = null;
            }
        }
    }
}