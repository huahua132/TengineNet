# 行为树程序集隔离测试指南

## 测试目的
验证行为树编辑器的程序集隔离功能，确保每个行为树只能访问其配置的程序集中的节点。

## 测试程序集结构

### 1. SharedTree (共享程序集)
- **程序集名称**: `BehaviorTree.SharedTree`
- **命名空间**: `BehaviorTree.SharedTree`
- **节点**: `SharedLogNode` - 可被所有树共享使用

### 2. Test1 (测试程序集1)
- **程序集名称**: `BehaviorTree.Test1`
- **命名空间**: `BehaviorTree.Test1`
- **依赖**: BehaviorTree.SharedTree
- **节点**: `Test1ActionNode` - 仅供Test1树使用

### 3. Test2 (测试程序集2)
- **程序集名称**: `BehaviorTree.Test2`
- **命名空间**: `BehaviorTree.Test2`
- **依赖**: BehaviorTree.SharedTree
- **节点**: `Test2ActionNode` - 仅供Test2树使用

## 测试步骤

### 步骤1: 创建测试行为树资产

1. 在Unity编辑器中，右键点击 `Assets/BehaviorTree/Test` 文件夹
2. 选择 `Create > BehaviorTree > Tree Asset`
3. 创建两个资产:
   - `Test1BehaviorTree.asset`
   - `Test2BehaviorTree.asset`

### 步骤2: 配置Test1行为树

1. 选中 `Test1BehaviorTree.asset`
2. 在Inspector面板中或打开行为树编辑器
3. 配置程序集:
   - **归属程序集**: `BehaviorTree.Test1`
   - **共享程序集**: 添加 `BehaviorTree.SharedTree`

### 步骤3: 配置Test2行为树

1. 选中 `Test2BehaviorTree.asset`
2. 在Inspector面板中或打开行为树编辑器
3. 配置程序集:
   - **归属程序集**: `BehaviorTree.Test2`
   - **共享程序集**: 添加 `BehaviorTree.SharedTree`

### 步骤4: 验证节点可见性

#### Test1行为树应该能看到:
- ✅ `SharedLogNode` (来自SharedTree)
- ✅ `Test1ActionNode` (来自Test1)
- ❌ `Test2ActionNode` (来自Test2 - 应该被隐藏)

#### Test2行为树应该能看到:
- ✅ `SharedLogNode` (来自SharedTree)
- ✅ `Test2ActionNode` (来自Test2)
- ❌ `Test1ActionNode` (来自Test1 - 应该被隐藏)

### 步骤5: 测试编辑器行为

1. 打开 `Tools > BehaviorTree > Editor Window`
2. 加载 `Test1BehaviorTree`
3. 查看左侧节点面板，确认只显示Test1和SharedTree的节点
4. 尝试添加节点到画布
5. 切换到 `Test2BehaviorTree`
6. 确认节点列表自动更新，只显示Test2和SharedTree的节点

### 步骤6: 测试运行时验证

1. 创建一个测试场景
2. 添加GameObject并挂载测试脚本
3. 尝试加载配置了错误程序集节点的行为树
4. 确认控制台输出错误信息，阻止加载

## 预期结果

### 编辑器行为
- 节点库根据当前加载的行为树资产动态过滤
- 只显示允许的程序集中的节点
- 切换行为树时自动刷新节点列表

### 运行时行为
- 加载行为树时验证所有节点的程序集归属
- 如果节点来自未授权的程序集，输出错误并拒绝创建节点
- 错误信息应清楚说明哪个节点被拒绝以及原因

## 故障排查

### 节点没有被过滤
- 检查程序集名称是否正确（区分大小写）
- 确认.asmdef文件已被Unity识别（查看Project窗口图标）
- 尝试刷新节点注册表：`BehaviorNodeRegistry.Refresh()`

### 所有节点都不可见
- 检查是否正确配置了归属程序集或共享程序集
- 如果两者都为空，应显示所有节点（默认行为）

### 运行时错误
- 确认行为树资产的程序集配置与实际使用的节点匹配
- 检查节点类型名称是否正确

## 扩展测试

### 测试无配置的行为树
1. 创建一个新的行为树，不配置任何程序集
2. 应该能看到所有可用的节点（默认行为）

### 测试程序集名称错误
1. 在配置中输入不存在的程序集名称
2. 确认该行为树没有节点可用（除非有其他有效的程序集配置）

### 测试运行时加载
1. 尝试从代码动态加载行为树
2. 确认程序集验证在运行时也生效