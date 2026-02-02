# Unity 游戏组件绑定与设置说明

## 目录
1. [系统概述](#系统概述)
2. [PlayerController 设置说明](#playercontroller-设置说明)
3. [BossController 设置说明](#bosscontroller-设置说明)
4. [AttackWindow 攻击判定窗口说明](#attackwindow-攻击判定窗口说明)
5. [CounterInputDetector 反击输入检测说明](#counterinputdetector-反击输入检测说明)
6. [Animation Event 设置说明](#animation-event-设置说明)
7. [GameLogger 日志系统说明](#gamelogger-日志系统说明)
8. [快速开始检查清单](#快速开始检查清单)

---

## 系统概述

本游戏采用**时间窗口反击判定系统**，不再使用传统的碰撞检测。核心机制类似于《只狼》的弹反或QTE系统：

### 核心流程
1. Boss播放攻击动画
2. 在动画的关键帧触发Animation Event，开启**攻击判定时间窗口**（AttackWindow）
3. 玩家在窗口期内按下对应的按键（Q/W/E）进行反击
4. 反击成功：玩家获得短暂无敌时间，Boss不造成伤害
5. 反击失败：窗口结束时，Boss攻击命中，玩家受到伤害

### 关键组件
- **AttackWindow**: Boss攻击时的判定时间窗口
- **CounterInputDetector**: 玩家的反击输入检测器
- **Animation Event**: 精确控制窗口开启时机

### 与旧系统的区别
| 特性 | 旧系统（碰撞检测） | 新系统（时间窗口） |
|------|------------------|------------------|
| 判定方式 | OnTriggerEnter2D | 时间窗口 + 按键检测 |
| 需要Collider2D | ✅ 必须 | ❌ 不需要 |
| 需要Physics2D Layer | ✅ 必须配置 | ❌ 不需要 |
| 判定精度 | 依赖碰撞体重叠 | 精确到帧 |
| 玩家反应 | 被动承受 | 主动反击 |
| 无敌时间 | 无 | 反击成功后获得 |

---

## PlayerController 设置说明

### 组件说明
`PlayerController` 负责玩家的输入处理、攻击执行、生命管理和反击检测。

### 组件绑定步骤

1. **在Hierarchy中选择Player对象**

2. **添加必需组件**
   - 添加 `Animator` 组件（必需）：用于播放动画
     - 在Animator组件中设置Animator Controller
     - 或者在PlayerController脚本中将动画片段拖拽到对应的字段

3. **挂载PlayerController脚本**
   - 将 `PlayerController.cs` 脚本挂载到Player对象上
   - **脚本会自动添加 `CounterInputDetector` 组件**（不需要手动添加）

4. **在PlayerController的Inspector面板中设置**

   #### 动画绑定区域
   - 将Idle动画片段拖拽到 **Idle Animation** 字段
   - 将Attack1动画片段拖拽到 **Attack X Animation** 字段
   - 将Attack2动画片段拖拽到 **Attack Y Animation** 字段
   - 将Attack3动画片段拖拽到 **Attack B Animation** 字段

   #### 攻击判定区域
   - 将Boss身上的 **AttackWindow** 对象拖拽到 **Attack Window** 字段
     - 这是Boss攻击时的判定窗口组件
     - 用于监听Boss的攻击并触发反击检测

   #### 攻击伤害设置
   - 设置 **Attack Damage** (玩家攻击Boss的伤害值，默认为10)
     - 注意：这是玩家攻击Boss时造成的伤害，不是反击伤害

### 自动添加的组件
- **CounterInputDetector**: 反击输入检测器
  - 脚本会在Start时自动查找或添加此组件
  - 负责检测玩家在窗口期内的按键输入
  - 反击成功后会给玩家短暂的无敌时间（默认0.5秒）

### 玩家操作说明
- **Q键**: 对应Boss的Attack1（重击）
- **W键**: 对应Boss的Attack2（突刺）
- **E键**: 对应Boss的Attack3（横扫）
- 必须在Boss攻击的判定窗口内按下对应的按键才能成功反击

---

## BossController 设置说明

### 组件说明
`BossController` 负责Boss的AI行为、攻击序列管理和动画控制。

### 组件绑定步骤

1. **在Hierarchy中选择Boss对象**

2. **添加必需组件**
   - 添加 `Animator` 组件（必需）：用于播放动画

3. **创建AttackWindow子对象**
   - 在Boss对象下创建子对象，命名为 **AttackWindow**
   - 为子对象添加 `AttackWindow.cs` 脚本

4. **挂载BossController脚本**
   - 将 `BossController.cs` 脚本挂载到Boss对象上

5. **在BossController的Inspector面板中设置**

   #### 动画绑定区域
   - 将Idle动画片段拖拽到 **Idle Animation** 字段
   - 将Attack1动画片段拖拽到 **Attack X Animation** 字段
   - 将Attack2动画片段拖拽到 **Attack Y Animation** 字段
   - 将Attack3动画片段拖拽到 **Attack B Animation** 字段

   #### 攻击判定区域
   - 将之前创建的 **AttackWindow** 子对象拖拽到 **Attack Window** 字段
   - 设置 **Attack Damage** (Boss攻击伤害值，默认为15)

   #### 动作序列设置
   Boss的行为是通过动作序列来控制的：
   
   - 点击 **Action Sequence** 的 + 号添加新动作
   - 为每个动作设置：
     - **Action Type**: 选择动作类型
       - `Idle`: 待机状态
       - `Attack1`: 重击（对应玩家Q键）
       - `Attack2`: 突刺（对应玩家W键）
       - `Attack3`: 横扫（对应玩家E键）
     - **Duration**: 该动作的持续时间（秒）
   
   - 勾选 **Loop Sequence**: Boss会循环执行动作序列
   - 勾选 **Auto Start**: 游戏开始时自动开始执行序列

   #### 示例动作序列
   ```
   1. Idle - 2秒 (Boss待机观察)
   2. Attack1 - 1秒 (重击攻击)
   3. Idle - 1秒 (短暂休息)
   4. Attack2 - 1秒 (突刺攻击)
   5. Attack3 - 1.5秒 (横扫攻击)
   ```

### Boss攻击类型映射
BossController会根据当前执行的动作自动设置AttackWindow的攻击类型：

| 动作类型 | 攻击类型 | 玩家反击按键 |
|---------|---------|-------------|
| Attack1 | Attack1 | Q键 |
| Attack2 | Attack2 | W键 |
| Attack3 | Attack3 | E键 |
| Idle | 无 | 不需要反击 |

---

## AttackWindow 攻击判定窗口说明

### 组件说明
`AttackWindow` 是Boss攻击的时间判定窗口组件，负责：
- 追踪攻击判定窗口的开启和关闭
- 通知玩家的反击检测器
- 处理反击成功或失败的结果
- 在窗口结束时对玩家造成伤害（如果未反击）

### 设置步骤

1. **创建AttackWindow对象**
   - 在Boss对象下创建子对象
   - 命名为 **AttackWindow** 或其他有意义的名称

2. **添加AttackWindow脚本**
   - 将 `AttackWindow.cs` 脚本挂载到该对象上

3. **在Inspector中设置**

   #### 攻击类型
   - **Attack Type**: 选择这个窗口对应的攻击类型
     - `Attack1`: 重击（玩家需按Q键反击）
     - `Attack2`: 突刺（玩家需按W键反击）
     - `Attack3`: 横扫（玩家需按E键反击）
   - 注意：BossController会在攻击时自动设置此类型，通常不需要手动修改

   #### 窗口时长
   - **Window Duration**: 判定窗口持续时间（秒）
     - 默认值：0.3秒
     - 建议范围：0.2-0.5秒
     - 太短：玩家难以反应
     - 太长：过于简单，失去挑战性

   #### 伤害设置
   - **Damage**: 窗口结束时造成的伤害值
     - 通常由BossController设置
     - 如果玩家成功反击，则不会造成伤害

   #### 目标设置
   - **Player**: 拖拽Player对象到此字段
     - 用于在窗口结束时对玩家造成伤害
     - 必须实现 `IDamageable` 接口

   #### 调试选项
   - **Show Debug Info**: 勾选后会在Console显示详细的窗口状态信息
   - 在开发阶段建议开启，发布前关闭

### 工作原理

1. **窗口开启**
   - Boss播放攻击动画
   - Animation Event触发BossController的 `OnAttackWindowStart()`
   - BossController调用AttackWindow的 `StartWindow()`
   - AttackWindow通知CounterInputDetector

2. **窗口期间**
   - CounterInputDetector监听玩家按键
   - 如果按下正确的按键，通知AttackWindow反击成功
   - 如果按键错误或未按键，等待窗口结束

3. **窗口结束**
   - 时间到达 `windowDuration` 后自动关闭
   - 如果反击成功：不造成伤害，触发OnCounterSuccess事件
   - 如果反击失败：对玩家造成伤害，触发OnWindowEnd事件

### 可视化调试

AttackWindow提供了Gizmos可视化：
- **红色圆圈**: 窗口处于激活状态
- **绿色圆圈**: 窗口处于非激活状态

在Scene视图中选中AttackWindow对象可以看到这些提示。

---

## CounterInputDetector 反击输入检测说明

### 组件说明
`CounterInputDetector` 是玩家的反击输入检测器，负责：
- 接收Boss攻击窗口的通知
- 检测玩家在窗口期内的按键输入
- 验证按键是否正确
- 反击成功后给予玩家短暂无敌时间

### 组件添加
**PlayerController会自动添加此组件，无需手动添加！**

### 在Inspector中设置

#### 无敌时间设置
- **Invincibility Duration**: 反击成功后的无敌时间（秒）
  - 默认值：0.5秒
  - 在此期间玩家不会受到任何伤害
  - 可以根据游戏节奏调整

#### 调试选项
- **Show Debug Info**: 勾选后会显示详细的反击检测信息
- 在开发阶段建议开启

### 按键映射

| Boss攻击类型 | 玩家按键 | KeyCode |
|------------|---------|---------|
| Attack1 | Q | KeyCode.Q |
| Attack2 | W | KeyCode.W |
| Attack3 | E | KeyCode.E |

### 工作流程

1. **接收通知**
   - AttackWindow开启时调用 `OnEnemyAttackStart()`
   - 记录当前窗口的引用和攻击类型

2. **检测输入**
   - 在Update中持续检测Q/W/E键的按下
   - 只在窗口激活期间检测输入

3. **验证反击**
   - 检查按下的按键是否与攻击类型匹配
   - 验证窗口是否仍然激活
   - 调用AttackWindow的 `TryCounter()` 方法

4. **反击成功**
   - 接收AttackWindow的 `OnCounterSuccess` 事件
   - 激活无敌状态
   - 在Console中显示成功信息（如果开启调试）

### 无敌机制

反击成功后：
- 玩家获得 `invincibilityDuration` 秒的无敌时间
- 期间 `IsInvincible` 属性返回true
- PlayerController的 `TakeDamage()` 会检查此属性并忽略伤害

---

## Animation Event 设置说明

### 什么是Animation Event？
Animation Event 是Unity动画系统中的功能，可以在动画播放到特定帧时调用脚本中的方法。我们使用它来精确控制攻击判定窗口的开启和关闭时机。

### 为Boss攻击动画添加Event的步骤

#### 1. 打开Animation窗口
- 在Unity顶部菜单栏选择 `Window > Animation > Animation`
- 或使用快捷键 `Ctrl+6` (Windows) / `Cmd+6` (Mac)

#### 2. 选择要编辑的动画
- 在Project窗口中找到Boss的攻击动画片段（例如：Attack1、Attack2、Attack3）
- 点击选中该动画片段
- 或者在Hierarchy中选择Boss对象，然后在Animation窗口中选择对应的动画

#### 3. 添加Animation Event

##### 为 Attack1 动画添加Event：

**Event 1: 开始攻击判定窗口**
- 在Animation窗口的时间轴上，找到Boss攻击的关键时刻（例如：武器即将接触玩家的瞬间）
- 点击时间轴上方的 `Add Event` 按钮（或右键点击时间轴选择 "Add Animation Event"）
- 在弹出的Event窗口中：
  - **Function**: 填写 `OnAttackWindowStart`
  - 点击 "Apply" 保存

**Event 2: 结束攻击判定窗口**
- 在时间轴上找到攻击判定应该结束的帧（通常在攻击动作完成后）
- 添加新的Event
- 在Event窗口中：
  - **Function**: 填写 `OnAttackWindowEnd`
  - 点击 "Apply" 保存

##### 为 Attack2 和 Attack3 动画重复相同步骤
- 每个Boss攻击动画都需要添加两个Event：
  - `OnAttackWindowStart` - 开始攻击判定窗口
  - `OnAttackWindowEnd` - 结束攻击判定窗口

#### 4. 调整Event时机（重要！）

**OnAttackWindowStart的时机选择：**
- 应该在攻击即将命中玩家的瞬间
- 给玩家一个短暂的反应时间窗口
- 太早：玩家会觉得判定不公平
- 太晚：玩家来不及反应

**OnAttackWindowEnd的时机选择：**
- 通常在 `OnAttackWindowStart` 之后 0.3-0.5秒
- 配合AttackWindow的 `windowDuration` 设置
- 确保窗口有合理的持续时间

#### 5. 验证Event设置

在Animation窗口中：
- 小旗帜图标表示Event位置
- 点击小旗帜可以查看和编辑Event
- 播放动画预览，确认Event触发时机正确

### Event命名规范

| Event函数名 | 触发时机 | 作用 |
|------------|---------|------|
| `OnAttackWindowStart` | 攻击判定窗口开始 | 开启AttackWindow，通知玩家准备反击 |
| `OnAttackWindowEnd` | 攻击判定窗口结束 | 关闭AttackWindow，结算伤害 |

### 示例时间点设置参考

假设Boss攻击动画总长度为1秒（60帧@60fps）：

**重击 (Attack1)**
- `OnAttackWindowStart`: 第30帧 (0.5秒) - Boss抬手即将下砸
- `OnAttackWindowEnd`: 第45帧 (0.75秒) - 攻击结束
- 窗口时长：0.25秒

**突刺 (Attack2)**
- `OnAttackWindowStart`: 第20帧 (0.33秒) - 开始冲刺
- `OnAttackWindowEnd`: 第38帧 (0.63秒) - 冲刺结束
- 窗口时长：0.3秒

**横扫 (Attack3)**
- `OnAttackWindowStart`: 第25帧 (0.42秒) - 开始横扫
- `OnAttackWindowEnd`: 第43帧 (0.72秒) - 横扫结束
- 窗口时长：0.3秒

*注意：具体时间点需要根据你的实际动画进行调整，以达到最佳游戏体验*

---


## GameLogger 日志系统说明

### 系统概述
`GameLogger` 是统一的日志管理系统，允许你通过配置控制不同类型的日志输出，避免控制台被大量日志淹没。

### 设置步骤

1. **创建GameLogger对象**
   - 在Hierarchy中创建一个空对象，命名为 "GameLogger"
   - 将 `GameLogger.cs` 脚本挂载到该对象上
   - 该对象会自动设置为 DontDestroyOnLoad（在场景切换时不会被销毁）

2. **配置日志开关**
   在Inspector中，你可以看到以下分类的日志开关：

#### 全局开关
- **Enable Logging**: 主开关，关闭后将禁用所有游戏日志（Unity系统日志不受影响）

#### 战斗系统日志
- **Log Attack Window**: 攻击判定窗口相关（窗口开启/关闭、反击成功/失败等）
- **Log Damage**: 伤害系统相关（造成伤害、受到伤害、生命值变化等）
- **Log Death**: 死亡相关日志

#### 动画系统日志
- **Log Animation**: 动画播放相关（动画切换等）
- **Log Animation Event**: 动画事件相关（Event触发等）

#### 角色控制日志
- **Log Player Action**: 玩家输入和行为日志
- **Log Boss Action**: Boss行为和AI日志

#### 组件验证日志
- **Log Component Validation**: 组件绑定验证日志（警告和错误）

#### 通用日志
- **Log Info**: 一般信息日志
- **Log Warning**: 警告日志
- **Log Error**: 错误日志（建议始终开启）

### 使用方法

在代码中，使用 `GameLogger` 替代 `Debug.Log`：

```csharp
// 攻击窗口日志
GameLogger.LogAttackWindow("攻击判定窗口已开启: Attack1");

// 伤害日志
GameLogger.LogDamageDealt("Player", "Boss", 10f);
GameLogger.LogDamageTaken("Boss", 10f, 90f, 100f);

// 治疗日志
GameLogger.LogHeal("Player", 20f, 80f, 100f);

// 死亡日志
GameLogger.LogDeath("Boss");

// 动画日志
GameLogger.LogAnimation("Player", "Attack1");
GameLogger.LogAnimationEvent("Boss", "OnAttackWindowStart");

// 角色行为日志
GameLogger.LogPlayerAction("按下Q键，尝试反击Attack1");
GameLogger.LogBossAction("执行动作 Attack1，持续时间 1 秒");

// 组件验证日志
GameLogger.LogComponentValidation("未找到 Animator 组件", LogType.Error);

// 通用日志
GameLogger.Log("游戏开始", "Game");
GameLogger.LogWarning("生命值过低", "Player");
GameLogger.LogError("配置文件加载失败", "Config");
```

### 快捷配置方法

GameLogger 提供了一些快捷配置方法：

```csharp
// 启用所有日志
GameLogger.Instance.EnableAllLogs();

// 禁用所有日志
GameLogger.Instance.DisableAllLogs();

// 仅启用错误和警告
GameLogger.Instance.EnableErrorAndWarningOnly();

// 仅启用战斗相关日志
GameLogger.Instance.EnableCombatLogsOnly();
```

### 日志颜色说明

为了便于区分，不同类型的日志使用不同的颜色：

- 🟠 **橙色**: 攻击窗口 `[AttackWindow]`
- 🔴 **红色**: 伤害/死亡 `[Damage]` `[Death]`
- 🟢 **绿色**: 治疗 `[Heal]`
- 🔵 **青色**: 动画 `[Animation]` `[AnimEvent]`
- 🔵 **蓝色**: 玩家 `[Player]`
- 🟣 **紫色**: Boss `[Boss]`
- ⚪ **白色**: 通用信息 `[Info]`
- 🟡 **黄色**: 警告 `[Warning]` `[Validation]`
- 🔴 **红色**: 错误 `[Error]`

### 推荐配置

**开发阶段（调试所有功能）**
- 启用所有日志，观察游戏运行状态

**战斗系统调试**
- 启用：Attack Window, Damage, Death, Boss Action, Player Action
- 禁用：Animation, Animation Event, Info

**性能测试/最终版本**
- 仅启用：Error, Warning
- 禁用其他所有日志

### 注意事项

1. **性能考虑**: 虽然关闭的日志不会输出到Console，但仍会执行判断逻辑。在发布版本中建议完全禁用日志或使用条件编译。

2. **自动创建**: 如果场景中没有GameLogger对象，系统会自动创建一个（使用默认配置）。建议手动创建以便自定义配置。

3. **场景持久化**: GameLogger对象会在场景切换时保持存在（DontDestroyOnLoad），因此只需要在第一个场景中创建一次。

---

## 快速开始检查清单

### Player设置
- [ ] Player对象已添加Animator组件
- [ ] PlayerController脚本已挂载
- [ ] 4个动画片段已绑定（Idle + 3个Attack）
- [ ] Boss的AttackWindow对象已绑定到PlayerController
- [ ] CounterInputDetector会自动添加（无需手动操作）

### Boss设置
- [ ] Boss对象已添加Animator组件
- [ ] BossController脚本已挂载
- [ ] 4个动画片段已绑定（Idle + 3个Attack）
- [ ] 创建了AttackWindow子对象
- [ ] AttackWindow已添加AttackWindow脚本
- [ ] AttackWindow已绑定到BossController
- [ ] AttackWindow的Player字段已绑定Player对象
- [ ] 已设置动作序列（Action Sequence）
- [ ] 3个攻击动画都添加了2个Event（Start和End）

### 系统设置
- [ ] 已在场景中创建GameLogger对象
- [ ] 已配置所需的日志开关
- [ ] 已测试日志输出是否正常

### ❌ 不再需要的设置
- ~~Player/Boss对象的Collider2D组件~~（新系统不需要）
- ~~Layer系统配置~~（新系统不需要）
- ~~Physics 2D碰撞矩阵~~（新系统不需要）
- ~~AttackHitbox子对象的Collider2D~~（旧系统才需要）

---

## 调试技巧

### 可视化攻击判定窗口

1. **在Scene视图中查看**
   - 选中AttackWindow对象
   - 红色圆圈：窗口激活
   - 绿色圆圈：窗口未激活

2. **使用GameLogger调试**
   - 启用 "Log Attack Window" 查看窗口状态
   - 启用 "Log Player Action" 查看玩家输入
   - 启用 "Log Boss Action" 查看Boss行为

3. **在Inspector中观察**
   - AttackWindow的 "Is Active" 显示当前状态
   - CounterInputDetector的 "Is Invincible" 显示无敌状态

### 常见问题排查

**问题1: 反击无效**
- ✅ 检查AttackWindow是否正确绑定到PlayerController
- ✅ 检查Animation Event是否正确设置
- ✅ 检查按键映射是否正确（Q/W/E对应Attack1/2/3）
- ✅ 查看Console日志，确认窗口是否开启

**问题2: 窗口时机不对**
- 调整Animation Event的时间点
- 调整AttackWindow的 `windowDuration` 值
- 在开发阶段启用所有日志观察时序

**问题3: 反击后仍然受伤**
- 检查CounterInputDetector的无敌时间设置
- 检查PlayerController的TakeDamage是否正确检查 `IsInvincible`
- 查看Console确认反击是否真的成功

**问题4: Boss不攻击**
- 检查BossController的动作序列是否设置
- 确认勾选了 "Auto Start"
- 检查3个攻击动画的Event是否都添加了

---

## 系统架构图

```
Boss对象
├── Animator
├── BossController
│   ├── 动画管理
│   ├── 动作序列执行
│   └── AttackWindow控制
└── AttackWindow (子对象)
    ├── 时间窗口追踪
    ├── 通知CounterInputDetector
    └── 伤害结算

Player对象
├── Animator
├── PlayerController
│   ├── 输入处理
│   ├── 攻击执行
│   └── 生命管理
└── CounterInputDetector (自动添加)
    ├── 接收窗口通知
    ├── 检测玩家输入
    └── 无敌时间管理
```

---

## 联系与支持

如有问题，请检查Unity Console窗口的错误和警告信息。本系统包含详细的验证和日志功能，会自动提示配置问题。

**建议在开发阶段启用所有GameLogger日志，以便快速定位问题！**
