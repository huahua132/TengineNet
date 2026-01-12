# Test1程序集节点使用指南

## 节点列表

### 1. FindNearbyEnemiesNode - 查找附近敌人（条件节点）
**功能**: 在指定半径内搜索带"Enemy"标签的敌人

**参数**:
- `searchRadius` (float): 搜索半径，默认10.0
- `enemyTag` (string): 敌人标签，默认"Enemy"

**返回值**:
- `SUCCESS` - 找到敌人，最近的敌人存储到黑板
- `FAIL` - 未找到敌人

**黑板数据**:
- 存储最近敌人的Transform到 `TransformBlackboard.target`

### 2. AttackNode - 攻击（行为节点）
**功能**: 攻击黑板中存储的目标敌人

**参数**:
- `attackRange` (float): 攻击范围，默认2.0
- `attackDamage` (float): 攻击伤害，默认10.0
- `attackCooldown` (float): 攻击冷却时间（秒），默认1.0

**返回值**:
- `SUCCESS` - 成功执行攻击
- `FAIL` - 目标不存在或超出范围
- `RUNNING` - 攻击冷却中

**依赖**:
- 需要从黑板读取 `TransformBlackboard.target`
- 目标需要有Monster组件才能造成伤害

### 3. MoveToTargetNode - 移动到目标位置（行为节点）
**功能**: 移动到指定的目标位置

**参数**:
- `targetX`, `targetY`, `targetZ` (float): 目标坐标
- `speed` (float): 移动速度，默认5.0

**返回值**:
- `SUCCESS` - 到达目标位置（距离<0.1）
- `RUNNING` - 移动中
- `FAIL` - Transform为null

### 4. Test1ActionNode - 测试行为（行为节点）
**功能**: 简单的测试节点，输出日志

**参数**:
- `actionName` (string): 行为名称，默认"Test1 Action"

**返回值**:
- `SUCCESS` - 总是成功

## 角色脚本

### Hero - 英雄角色
**功能**: 可配置行为树的玩家角色

**属性**:
- `maxHP` / `currentHP` - 生命值
- `attackPower` - 攻击力
- `moveSpeed` - 移动速度
- `behaviorTreeAsset` - 行为树资产

**方法**:
- `TakeDamage(float)` - 受到伤害
- `Heal(float)` - 恢复生命
- `OnDeath()` - 死亡处理

**Gizmos**:
- 红色圈 - 攻击范围(2f)
- 黄色圈 - 搜索范围(10f)

### Monster - 怪物
**功能**: 作为敌人的AI角色

**属性**:
- `maxHP` / `currentHP` - 生命值
- `attackPower` - 攻击力  
- `moveSpeed` - 移动速度
- `behaviorTreeAsset` - 行为树资产（可选）

**标签**: 自动添加"Enemy"标签

**方法**:
- `TakeDamage(float)` - 受到伤害
- `Heal(float)` - 恢复生命
- `OnDeath()` - 死亡处理（销毁GameObject）

**Gizmos**:
- HP条显示（Gizmos方式）
- 红色圈 - 攻击范围(2f)
- 黄色圈 - 警戒范围(8f)

## 使用示例

### 示例1：简单的战斗AI
创建一个行为树，让Hero自动寻找并攻击敌人：

```
行为树结构:
Root (SequenceNode)
  ├─ FindNearbyEnemiesNode (查找敌人)
  │    searchRadius: 10
  │    enemyTag: Enemy
  └─ AttackNode (攻击敌人)
       attackRange: 2
       attackDamage: 10
       attackCooldown: 1
```

**配置步骤**:
1. 创建行为树资产
2. 配置程序集：
   - 归属: `BehaviorTree.Test1`
   - 共享: `BehaviorTree.SharedTree`
3. 添加节点并连接
4. 在Hero GameObject上挂载Hero脚本
5. 指定行为树资产
6. 创建Monster GameObject

### 示例2：巡逻+战斗AI
结合移动和战斗：

```
Root (SelectorNode)
  ├─ 战斗分支 (SequenceNode)
  │    ├─ FindNearbyEnemiesNode (查找敌人)
  │    └─ AttackNode (攻击)
  └─ 巡逻分支 (SequenceNode)
       ├─ MoveToTargetNode (移动到点1)
       ├─ WaitNode (等待)
       ├─ MoveToTargetNode (移动到点2)
       └─ WaitNode (等待)
```

**逻辑说明**:
- 优先检查并攻击附近敌人
- 没有敌人时执行巡逻

### 示例3：场景设置

1. **创建Hero**:
   ```
   GameObject -> Create Empty
   Name: Hero
   Add Component -> Hero
   Position: (0, 0, 0)
   配置行为树资产
   ```

2. **创建Monster**:
   ```
   GameObject -> 3D Object -> Cube
   Name: Monster
   Tag: Enemy (自动添加)
   Add Component -> Monster
   Position: (5, 0, 0)
   ```

3. **运行场景**:
   - Hero会自动寻找附近的Monster
   - 在攻击范围内时会攻击
   - 查看Console日志了解行为树执行情况

## 黑板数据流

```
FindNearbyEnemiesNode
    ↓ 写入
TransformBlackboard.target (最近敌人的Transform)
    ↓ 读取
AttackNode (对目标进行攻击)
```

## 调试提示

1. **可视化范围**:
   - 选中Hero/Monster查看Gizmos
   - 红圈 = 攻击范围
   - 黄圈 = 搜索/警戒范围

2. **日志输出**:
   - 所有节点都有详细的日志输出
   - 标签格式: `[NodeName] 信息`

3. **行为树调试**:
   - 启用编辑器的Debug模式
   - 可以看到节点的实时状态

## 扩展建议

1. **添加巡逻节点** - 在预定路径点间移动
2. **添加追踪节点** - 持续跟随目标
3. **添加逃跑节点** - HP低时远离敌人
4. **添加技能节点** - 使用特殊技能
5. **添加状态检查** - HP、距离等条件判断