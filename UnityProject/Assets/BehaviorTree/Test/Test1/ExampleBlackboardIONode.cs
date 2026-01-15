using UnityEngine;

namespace BehaviorTree.Test1
{
    /// <summary>
    /// 黑板IO标记使用示例节点
    /// 
    /// 使用说明：
    /// 1. 等待Unity完成编译
    /// 2. 取消下面的注释
    /// 3. 保存文件
    /// 4. 在编辑器中创建此节点并查看属性面板的"黑板依赖"区域
    /// </summary>
    
    
    // ==================== 示例1: 只有输出（写入）的节点 ====================
    [BehaviorProcessNode("Example Write Only", "示例：只写入黑板", BehaviorProcessType.action)]
    [BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "target", "将找到的目标写入黑板")]
    public class ExampleWriteOnlyNode : BehaviorProcessNodeBase
    {
        public override void OnCreate() { }
        public override void OnRemove() { }
        
        public override BehaviorRet OnTickRun()
        {
            // 写入黑板
            var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
            blackboard.target = GameObject.FindGameObjectWithTag("Enemy")?.transform;
            
            return BehaviorRet.SUCCESS;
        }
    }
    
    // ==================== 示例2: 只有输入（读取）的节点 ====================
    [BehaviorProcessNode("Example Read Only", "示例：只读取黑板", BehaviorProcessType.action)]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "从黑板读取目标进行处理")]
    public class ExampleReadOnlyNode : BehaviorProcessNodeBase
    {
        public override void OnCreate() { }
        public override void OnRemove() { }
        
        public override BehaviorRet OnTickRun()
        {
            // 从黑板读取
            var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
            if (blackboard?.target != null)
            {
                Debug.Log($"读取到目标: {blackboard.target.name}");
                return BehaviorRet.SUCCESS;
            }
            
            return BehaviorRet.FAIL;
        }
    }
    
    // ==================== 示例3: 既读又写的节点 ====================
    [BehaviorProcessNode("Example Read Write", "示例：读取并更新黑板", BehaviorProcessType.action)]
    [BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "读取当前目标")]
    [BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "target", "更新新目标")]
    public class ExampleReadWriteNode : BehaviorProcessNodeBase
    {
        public override void OnCreate() { }
        public override void OnRemove() { }
        
        public override BehaviorRet OnTickRun()
        {
            var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
            
            // 读取当前目标
            Transform oldTarget = blackboard.target;
            Debug.Log($"旧目标: {oldTarget?.name ?? "无"}");
            
            // 写入新目标
            blackboard.target = GameObject.FindGameObjectWithTag("Player")?.transform;
            Debug.Log($"新目标: {blackboard.target?.name ?? "无"}");
            
            return BehaviorRet.SUCCESS;
        }
    }
    
    // ==================== 示例4: 没有黑板依赖的节点 ====================
    [BehaviorProcessNode("Example No Blackboard", "示例：不使用黑板", BehaviorProcessType.action)]
    // 注意：这个节点不需要BlackboardIO标记，因为它不使用黑板
    public class ExampleNoBlackboardNode : BehaviorProcessNodeBase
    {
        public float waitTime = 1f;
        
        public override void OnCreate() { }
        public override void OnRemove() { }
        
        public override BehaviorRet OnTickRun()
        {
            // 这个节点不使用黑板，只是简单的等待
            Debug.Log($"等待 {waitTime} 秒");
            return BehaviorRet.SUCCESS;
        }
    }
    
    // ==================== 提示信息 ====================
    // 要查看黑板IO标记的效果：
    // 1. 确保Unity已完成编译（没有错误）
    // 2. 取消上面示例代码的注释
    // 3. 保存文件，等待Unity重新编译
    // 4. 打开行为树编辑器
    // 5. 添加示例节点到画布
    // 6. 选中节点
    // 7. 在右侧属性面板查看"黑板依赖"区域
    //
    // 你会看到：
    // - 📥 输入（读取）列表
    // - 📤 输出（写入）列表
    // - 每个依赖的完整路径和描述
}