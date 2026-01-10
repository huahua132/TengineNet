# 行为树系统使用说明

## 📖 概述

这是一个功能完整的Unity行为树系统，具有可视化编辑器、运行时调试和自动节点发现功能。

## ✨ 主要特性

### 1. **可视化编辑器**
- 上方菜单栏：资源管理、调试开关
- 左侧节点面板：按类型分类的节点库，支持拖拽
- 中间画布：节点编辑区域，支持缩放和拖动
- 右侧属性面板：节点属性检查器

### 2. **自动节点发现**
使用`BehaviorProcessNodeAttribute`标记节点类，系统会自动发现并注册：
```csharp
[BehaviorProcessNode("节点名称", "节点描述", BehaviorProcessType.composite)]
public class MyNode : BehaviorProcessNodeBase
{
    // 实现逻辑
}
```

### 3. **节点类型与颜色**
- **Composite (蓝色)** - 组合节点：控制子节点执行流程
- **Decorator (橙色)** - 装饰节点：修改子节点行为
- **Condition (黄色)** - 条件节点：判断条件
- **Action (绿色)** - 行为节点：执行具体动作

### 4. **运行时调试**
- 实时显示节点执行状态
- 颜色指示：绿色(成功)、红色(失败)、黄色(运行中)、紫色(中断)

## 🚀 快速开始

### 1. 创建行为树资源
```
右键 -> Create -> BehaviorTree -> Tree Asset
```

### 2. 打开编辑器
**方式一：** 双击行为树资源文件
**方式二：** 菜单栏 -> Tools -> BehaviorTree -> Editor Window

### 3. 添加节点
- 从左侧节点面板点击或拖拽节点到画布
- 右键节点可以设置为根节点、连接、删除等操作

### 4. 连接节点
- 右键父节点 -> "Connect To..."
- 右键子节点 -> "Connect Here"

### 5. 运行行为树
```csharp
public class MyScript : MonoBehaviour
{
    public BehaviorTreeAsset treeAsset;
    private BehaviorTree.BehaviorTree _tree;
    
    void Start()
    {
        _tree = new BehaviorTree.BehaviorTree();
        _tree.Init();
        _tree.InitFromAsset(treeAsset);
        
        // 启用调试
        _tree.OnNodeStatusChanged += (nodeId, status) => {
            Debug.Log($"Node {nodeId}: {status}");
        };
    }
    
    void Update()
    {
        _tree?.TickRun();
    }
}
```

## 📦 内置节点

### 组合节点 (Composite)
- **SequenceNode** - 顺序执行，全部成功才返回成功 (AND)
- **SelectorNode** - 选择执行，有一个成功就返回成功 (OR)
- **ParallelNode** - 并行执行所有子节点
- **IfElseNode** - 条件分支节点

### 装饰节点 (Decorator)
- **RepeatNode** - 重复执行子节点

### 条件节点 (Condition)
- **AlwaysTrueNode** - 总是返回成功

### 行为节点 (Action)
- **LogNode** - 打印日志

## 🔧 编辑器快捷键

- **鼠标中键拖动** - 移动画布
- **鼠标滚轮** - 缩放画布
- **ESC** - 取消连接操作
- **左键点击** - 选择节点

## 📝 自定义节点

### 1. 创建节点类
```csharp
using BehaviorTree;
using TEngine;

[BehaviorProcessNode("MyCustomNode", "自定义节点描述", BehaviorProcessType.action)]
public class MyCustomNode : BehaviorProcessNodeBase
{
    public override void OnCreate()
    {
        // 节点创建时调用
    }
    
    public override void OnRemove()
    {
        // 节点移除时调用
    }
    
    public override BehaviorRet OnTickRun()
    {
        // 节点执行逻辑
        return BehaviorRet.SUCCESS;
    }
}
```

### 2. 节点会自动出现在编辑器中
系统会自动扫描所有带有`BehaviorProcessNodeAttribute`的类并注册。

## 🐛 运行时调试

### 1. 启用调试模式
在编辑器工具栏点击 "Debug: OFF" 切换为 "Debug: ON"

### 2. 在测试脚本中启用调试
```csharp
public BehaviorTreeAsset treeAsset;
public bool enableDebug = true;

void Start()
{
    _behaviorTree = new BehaviorTree.BehaviorTree();
    _behaviorTree.Init();
    _behaviorTree.InitFromAsset(treeAsset);
    
    if (enableDebug)
    {
        _behaviorTree.OnNodeStatusChanged += OnNodeStatusChanged;
    }
}

void OnNodeStatusChanged(int nodeId, BehaviorRet status)
{
    // 状态会自动发送到编辑器窗口
#if UNITY_EDITOR
    var window = EditorWindow.GetWindow<BehaviorTree.Editor.BehaviorTreeEditorWindow>();
    window?.UpdateNodeStatus(nodeId, status);
#endif
}
```

### 3. 查看实时状态
- 运行游戏时，节点会根据状态显示不同颜色边框
- 绿色：成功
- 红色：失败
- 黄色：运行中
- 紫色：中断

## 📋 最佳实践

1. **根节点标记** - 第一个添加的节点自动成为根节点，也可以手动设置
2. **节点命名** - 给节点起有意义的名称，便于理解
3. **模块化设计** - 将复杂逻辑拆分成多个小的子树
4. **调试先行** - 开发时启用调试模式，实时查看执行状态
5. **保存习惯** - 经常保存资源文件

## 🔗 文件结构

```
BehaviorTree/
├── RunTime/                    # 运行时代码
│   ├── BehaviorTree.cs        # 主控制器
│   ├── BehaviorNode.cs        # 节点容器
│   ├── BehaviorContext.cs     # 执行上下文
│   ├── BehaviorNodeData.cs    # 数据结构
│   ├── BehaviorProcessNodeBase.cs  # 节点基类
│   ├── Blackboard/            # 黑板系统
│   └── ProcessNodes/          # 节点实现
│       ├── composites/        # 组合节点
│       ├── decorators/        # 装饰节点
│       ├── conditions/        # 条件节点
│       └── actions/           # 行为节点
├── Editor/                    # 编辑器代码
│   ├── BehaviorTreeEditorWindow.cs    # 主编辑器窗口
│   ├── BehaviorNodeRegistry.cs        # 节点注册系统
│   └── BehaviorTreeAssetEditor.cs     # 资源编辑器
└── Test/                      # 测试代码
    └── BehaviorTreeTest.cs    # 测试脚本
```

## 💡 提示

- 双击行为树资源可直接打开编辑器
- 使用右键菜单快速操作节点
- 调试模式下可以实时查看节点执行状态
- 节点库会自动发现所有自定义节点
- 支持多个行为树同时编辑

## 🎯 示例场景

参考 `BehaviorTreeTest.cs` 查看完整的使用示例。

## ⚠️ 注意事项

1. 确保节点类继承自`BehaviorProcessNodeBase`
2. 必须添加`BehaviorProcessNodeAttribute`属性
3. 行为树资源保存为`.asset`文件
4. 运行时调试需要Unity编辑器环境

---

**版本：** 1.0  
**作者：** Kilo Code  
**更新日期：** 2026-01-10