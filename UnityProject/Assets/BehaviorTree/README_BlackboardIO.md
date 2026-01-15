# 黑板依赖标记功能使用说明

## 📖 概述

黑板依赖标记功能允许你在节点类上标注该节点读取和写入哪些黑板数据。编辑器会在属性面板中自动显示这些依赖关系，帮助你更好地理解行为树的数据流。

## ✨ 功能特性

- **输入标记（📥）**: 显示节点从黑板读取的数据
- **输出标记（📤）**: 显示节点向黑板写入的数据
- **自动显示**: 在编辑器属性面板自动显示黑板依赖信息
- **多黑板支持**: 一个节点可以标记多个黑板IO关系

## 🔧 使用方法

### 1. 基本语法

```csharp
[BlackboardIO(IOType, "BlackboardTypeName", "FieldName", "描述信息")]
public class YourNode : BehaviorProcessNodeBase
{
    // 节点实现
}
```

**参数说明**:
- `IOType`: IO类型，可选值：
  - `BlackboardIOAttribute.IOType.Read` - 读取（输入）
  - `BlackboardIOAttribute.IOType.Write` - 写入（输出）
- `BlackboardTypeName`: 黑板类型名称（如"TargetBlackboard"）
- `FieldName`: 访问的黑板字段名（如"target"）
- `描述信息`: 可选，描述该依赖的用途

### 2. 示例：只有输出的节点

```csharp
using UnityEngine;
using BehaviorTree;

namespace BehaviorTree.Test1
{
    // 查找敌人节点 - 将最近的敌人写入黑板
    [BehaviorProcessNode("Find Nearby Enemies", "在指定范围内查找敌人", BehaviorProcessType.condition)]
    [BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "target", "将最近的敌人Transform存储到黑板")]
    public class FindNearbyEnemiesNode : BehaviorProcessNodeBase
    {
        public float searchRadius = 10f;
        public string enemyTag = "Enemy";
        
        public override void OnCreate() { }
        public override void OnRemove() { }
        
        public override BehaviorRet OnTickRun()
        {
            // ... 查找敌人逻辑 ...
            
            // 写入黑板
            var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
            blackboard.target = closestEnemy.transform;
            
            return BehaviorRet.SUCCESS;
        }
    }
}
```

### 3. 示例：只有输入的节点

```csharp
// 攻击节点 - 从黑板读取目标并攻击
[BehaviorProcessNode("Attack", "攻击目标敌人", BehaviorProcessType.action)]
[BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "从黑板读取攻击目标")]
public class AttackNode : BehaviorProcessNodeBase
{
    public float attackRange = 2f;
    public float attackDamage = 10f;
    
    public override void OnCreate() { }
    public override void OnRemove() { }
    
    public override BehaviorRet OnTickRun()
    {
        // 从黑板读取目标
        var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
        if (blackboard == null || blackboard.target == null)
        {
            return BehaviorRet.FAIL;
        }
        
        Transform target = blackboard.target;
        // ... 攻击逻辑 ...
        
        return BehaviorRet.SUCCESS;
    }
}
```

### 4. 示例：既有输入又有输出的节点

```csharp
// 检查并更新目标节点 - 读取旧目标，写入新目标
[BehaviorProcessNode("Update Target", "更新目标信息", BehaviorProcessType.action)]
[BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "读取当前目标")]
[BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "lastTarget", "保存上一个目标")]
[BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "target", "更新新目标")]
public class UpdateTargetNode : BehaviorProcessNodeBase
{
    public override void OnCreate() { }
    public override void OnRemove() { }
    
    public override BehaviorRet OnTickRun()
    {
        var blackboard = _Context.GetBlackBoardData<TargetBlackboard>();
        
        // 读取当前目标
        Transform oldTarget = blackboard.target;
        
        // 写入上一个目标
        blackboard.lastTarget = oldTarget;
        
        // 写入新目标
        blackboard.target = FindNewTarget();
        
        return BehaviorRet.SUCCESS;
    }
}
```

### 5. 示例：多黑板依赖

```csharp
// 复杂节点 - 使用多个黑板
[BehaviorProcessNode("Complex Action", "复杂行为", BehaviorProcessType.action)]
[BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "读取目标")]
[BlackboardIO(BlackboardIOAttribute.IOType.Read, "StateBlackboard", "currentState", "读取当前状态")]
[BlackboardIO(BlackboardIOAttribute.IOType.Write, "ResultBlackboard", "actionResult", "写入行为结果")]
public class ComplexActionNode : BehaviorProcessNodeBase
{
    public override void OnCreate() { }
    public override void OnRemove() { }
    
    public override BehaviorRet OnTickRun()
    {
        var targetBB = _Context.GetBlackBoardData<TargetBlackboard>();
        var stateBB = _Context.GetBlackBoardData<StateBlackboard>();
        var resultBB = _Context.GetBlackBoardData<ResultBlackboard>();
        
        // 读取输入
        Transform target = targetBB.target;
        string state = stateBB.currentState;
        
        // 执行逻辑...
        
        // 写入输出
        resultBB.actionResult = "Success";
        
        return BehaviorRet.SUCCESS;
    }
}
```

### 6. 示例：没有黑板依赖的节点

```csharp
// 等待节点 - 不使用黑板
[BehaviorProcessNode("Wait", "等待指定时间", BehaviorProcessType.action)]
// 不需要BlackboardIO标记
public class WaitNode : BehaviorProcessNodeBase
{
    public float duration = 1f;
    
    public override void OnCreate() { }
    public override void OnRemove() { }
    
    public override BehaviorRet OnTickRun()
    {
        // 不涉及黑板操作
        return BehaviorRet.SUCCESS;
    }
}
```

## 📋 编辑器显示效果

当你在编辑器中选中一个节点时，如果该节点有黑板依赖，属性面板会显示：

```
黑板依赖
━━━━━━━━━━━━━━━━━━━

📥 输入（读取）
  TargetBlackboard.target - 从黑板读取攻击目标

📤 输出（写入）
  TargetBlackboard.target - 将最近的敌人存储到黑板

━━━━━━━━━━━━━━━━━━━
ℹ️ 黑板依赖显示该节点读取和写入的黑板数据
```

## 🎯 最佳实践

### 1. **明确标注所有黑板访问**
为每个访问黑板的节点添加BlackboardIO标记，这样可以：
- 快速了解节点的数据依赖
- 避免黑板数据冲突
- 方便调试和维护

### 2. **提供清晰的描述**
在BlackboardIO标记中提供有意义的描述：
```csharp
// ✅ 好的描述
[BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "target", "将最近的敌人存储到黑板")]

// ❌ 不好的描述
[BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "target", "写入")]
```

### 3. **保持一致的命名**
使用一致的黑板类型和字段名：
```csharp
// ✅ 统一使用TargetBlackboard.target
[BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", ...)]

// ❌ 不要混用不同的命名
[BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "currentTarget", ...)]
```

### 4. **按照读写顺序标记**
如果一个节点既读又写同一个黑板字段，先标记读取，再标记写入：
```csharp
[BlackboardIO(BlackboardIOAttribute.IOType.Read, "TargetBlackboard", "target", "读取当前目标")]
[BlackboardIO(BlackboardIOAttribute.IOType.Write, "TargetBlackboard", "target", "更新新目标")]
```

## ⚠️ 注意事项

1. **Unity需要重新编译**: 添加BlackboardIO标记后，Unity需要重新编译才能在编辑器中看到效果

2. **只是标记，不是强制**: BlackboardIO是用于文档和显示的标记，不会影响节点的实际运行逻辑

3. **保持同步**: 如果修改了节点的黑板访问逻辑，记得同步更新BlackboardIO标记

4. **命名空间**: 在Test1等测试程序集中使用时，记得添加正确的命名空间引用

## 🔄 迁移现有节点

如果你有现有的节点需要添加黑板标记，按以下步骤操作：

1. **分析节点代码**，找出所有调用`_Context.GetBlackBoardData<T>()`的地方
2. **确定读写操作**：
   - 只读取黑板数据 → `IOType.Read`
   - 向黑板写入数据 → `IOType.Write`
3. **添加BlackboardIO标记**到节点类声明上
4. **保存文件**，等待Unity重新编译
5. **在编辑器中验证**显示效果

## 📝 完整示例项目

参考以下文件中的完整实现：
- `Assets/BehaviorTree/Test/Test1/FindNearbyEnemiesNode.cs` - 输出示例
- `Assets/BehaviorTree/Test/Test1/AttackNode.cs` - 输入示例  
- `Assets/BehaviorTree/Test/Test1/CheckHealthNode.cs` - 输入示例（读取黑板目标）

## 🐛 常见问题

**Q: 为什么编辑器中看不到黑板依赖信息？**
A: 确保：
1. 已添加BlackboardIO标记
2. Unity已完成编译
3. 在编辑器中选中了该节点

**Q: 编译错误："未能找到类型BlackboardIOAttribute"？**
A: Unity正在编译中，等待编译完成即可。如果持续报错，检查程序集引用。

**Q: 可以不添加BlackboardIO标记吗？**
A: 可以，BlackboardIO只是用于显示的标记，不影响节点功能。但建议添加以提高可维护性。

---

**版本**: 1.0  
**创建日期**: 2026-01-15  
**更新日期**: 2026-01-15