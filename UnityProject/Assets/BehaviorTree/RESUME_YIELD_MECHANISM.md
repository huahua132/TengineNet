# 行为树 Resume/Yield 机制详解

## 核心执行流程

### 1. BehaviorTree.TickRun() - 主循环

```csharp
public BehaviorRet TickRun()
{
    // 关键判断：栈中是否有节点
    if (_context.GetStackCount() > 0)
    {
        // 有节点在栈中 = 有节点处于挂起状态
        // 从栈顶取节点继续执行
        BehaviorNode lastNode = _context.GetStackPeekNode();
        while (lastNode != null)
        {
            lastRet = lastNode.TickRun();
            if (lastRet == BehaviorRet.RUNNING)
            {
                break;  // 仍在运行，停止本次Tick
            }
            lastNode = _context.GetStackPeekNode();
        }
    }
    else
    {
        // 栈为空 = 没有挂起的节点
        // 从根节点开始新的执行
        lastRet = _root.TickRun();
    }
    return lastRet;
}
```

### 2. BehaviorNode.TickRun() - 节点执行

```csharp
public BehaviorRet TickRun()
{
    // 步骤1: 如果不是挂起状态，压入栈
    if (!_isYield)
    {
        _context.PushStackNode(this);  // 将自己压入栈
    }
    
    // 步骤2: 调用实际的逻辑节点执行
    var ret = ProcessNode.OnTickRun();
    
    // 步骤3: 根据返回值决定栈操作
    if (ret != BehaviorRet.RUNNING)
    {
        _isYield = false;
        _context.PopStackNode();  // 执行完成，从栈中移除
    }
    else
    {
        _isYield = true;  // 挂起，保持在栈中
    }
    
    // 步骤4: 保存返回值供其他节点使用
    _context.SetLastRet(ret);
    return ret;
}
```

### 3. IsResume() 和 Yield()

```csharp
// 判断节点是否处于恢复执行状态
public bool IsResume()
{
    return _isYield;  // true = 上次返回了RUNNING，这次是恢复执行
}

// 节点调用此方法表示需要挂起
public BehaviorRet Yield()
{
    _isYield = true;
    return BehaviorRet.RUNNING;
}
```

## 执行时序图

```
首次执行节点 A:
1. _isYield = false
2. PushStackNode(A)       <- A 进栈
3. OnTickRun()            <- 执行 A 的逻辑
4. 返回 RUNNING
5. _isYield = true        <- 标记为挂起
6. Stack: [A]             <- A 保持在栈中

下一次 TickRun:
1. GetStackPeekNode() = A <- 从栈顶获取 A
2. _isYield = true        <- A 处于挂起状态
3. 不会再次 PushStack    <- 因为已经在栈中
4. OnTickRun()            <- 恢复执行 A
5. IsResume() = true      <- A 知道这是恢复执行

如果 A 返回 SUCCESS:
1. _isYield = false
2. PopStackNode()         <- A 从栈中移除
3. Stack: []              <- 栈变空
4. 下次从根节点重新开始
```

## 典型节点实现模式

### 模式1: 简单同步节点（LogNode）

```csharp
public override BehaviorRet OnTickRun()
{
    // 立即执行并返回，不需要挂起
    Debug.Log(message);
    return BehaviorRet.SUCCESS;
}
```

**特点**：
- 不需要检查 IsResume()
- 不需要调用 Yield()
- 立即返回结果

### 模式2: 异步等待节点（WaitNode）

```csharp
public override BehaviorRet OnTickRun()
{
    // Resume 路径：检查是否已经等待完成
    if (_Node.IsResume())
    {
        if (Time.time >= _endTime)
        {
            return BehaviorRet.SUCCESS;  // 等待完成
        }
        else
        {
            return _Node.Yield();  // 继续等待
        }
    }
    
    // 首次执行路径：记录结束时间并挂起
    _endTime = Time.time + duration;
    return _Node.Yield();
}
```

**特点**：
- 首次执行：设置状态，调用 Yield() 返回 RUNNING
- Resume 执行：检查条件，决定继续等待还是完成

### 模式3: 有子节点的组合节点（SequenceNode）

```csharp
public override BehaviorRet OnTickRun()
{
    // Resume 路径：处理子节点的返回值
    if (_Node.IsResume())
    {
        var lastRet = _Context.GetLastRet();  // 获取子节点返回值
        if (lastRet == BehaviorRet.FAIL)
        {
            return lastRet;  // 子节点失败，序列失败
        }
        else if (lastRet == BehaviorRet.SUCCESS)
        {
            lastIdx++;  // 子节点成功，继续下一个
        }
    }
    else
    {
        lastIdx = 0;  // 首次执行，从第一个子节点开始
    }
    
    // 执行子节点
    for (int i = lastIdx; i < _Node.Childrens.Count; i++)
    {
        var child = _Node.Childrens[i];
        var r = child.TickRun();
        if (r == BehaviorRet.RUNNING)
        {
            return _Node.Yield();  // 子节点挂起，自己也挂起
        }
        if (r == BehaviorRet.FAIL)
        {
            return r;  // 子节点失败
        }
    }
    return BehaviorRet.SUCCESS;  // 所有子节点成功
}
```

**特点**：
- 首次执行：初始化索引
- Resume 执行：通过 GetLastRet() 获取子节点结果
- 子节点返回 RUNNING 时，调用 Yield() 挂起自己

## 关键概念总结

### 1. 栈的作用
- 保存所有处于 RUNNING 状态的节点链
- 例如：Root -> Sequence -> Child 都在执行时，栈中会有这三个节点

### 2. _isYield 标志
- `true` = 节点处于挂起状态（上次返回了 RUNNING）
- `false` = 节点是新执行或已完成

### 3. GetLastRet() 的用途
- 获取子节点的返回值
- 只在 Resume 状态下有意义
- 用于父节点根据子节点结果决定下一步

### 4. Yield() 的时机
- 需要等待某个条件（时间、事件等）
- 子节点返回 RUNNING
- 需要多次 Tick 才能完成的操作

### 5. IsResume() 的意义
- 区分首次执行和恢复执行
- 首次执行：初始化状态
- 恢复执行：检查进度、获取子节点结果

## 常见错误

❌ **错误1**：忘记调用 Yield()
```csharp
// 错误：直接返回 RUNNING，但没有调用 Yield()
return BehaviorRet.RUNNING;
```

✅ **正确**：
```csharp
return _Node.Yield();
```

❌ **错误2**：没有区分首次和 Resume
```csharp
// 错误：每次都重新初始化，导致永远完不成
_startTime = Time.time;
```

✅ **正确**：
```csharp
if (!_Node.IsResume())
{
    _startTime = Time.time;  // 只在首次执行时初始化
}
```

❌ **错误3**：在同步节点中使用 Resume/Yield
```csharp
// 错误：Log 节点不需要挂起
if (_Node.IsResume()) { ... }
```

✅ **正确**：
```csharp
// 简单直接
Debug.Log(message);
return BehaviorRet.SUCCESS;
```

## 总结

Resume/Yield 机制的本质是：
1. **栈管理**：跟踪所有正在执行的节点
2. **状态保持**：允许节点跨帧执行
3. **结果传递**：父节点获取子节点结果
4. **执行控制**：区分首次执行和恢复执行

这个机制使得行为树可以：
- 支持异步操作（等待、延迟）
- 支持复杂的控制流（序列、选择）
- 支持中断和恢复
- 支持协程式的执行模式