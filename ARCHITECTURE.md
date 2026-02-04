# 游戏架构设计文档

## 架构概述

本游戏采用**单Scene + 状态重置**的架构设计，避免了Scene切换带来的复杂性和引用丢失问题。

### 核心原则

1. **单Scene运行**：整个游戏在一个Unity Scene中运行，不进行Scene切换
2. **状态管理**：通过组件内部状态重置实现游戏重启，而非销毁重建
3. **引用稳定**：所有对象引用在游戏生命周期中保持稳定，避免引用丢失
4. **不使用DontDestroyOnLoad**：所有对象都属于当前Scene，简化生命周期管理

## 管理类设计

### GameManager（游戏管理器）

**职责**：
- 管理游戏状态（MainMenu、Playing、Victory、Defeat）
- 协调各组件的启动和重置
- 管理UI显示
- 处理游戏流程

**单例模式**：Scene内唯一单例，不跨Scene持久化

**核心方法**：
```csharp
void StartGame()        // 开始游戏：进入Playing状态
void RestartGame()      // 重启游戏：重置各组件，保留最远路径
void EndGame()          // 结束游戏：完全重置，清空路径，返回主菜单
```

**状态转换流程**：

```
MainMenu → (StartGame) → Playing
Playing → (OnPlayerDeath) → Defeat
Playing → (OnBossDeath) → Victory
Defeat → (RestartGame) → Playing      // 保留最远路径
Defeat/Victory → (EndGame) → MainMenu // 完全重置
```

### PlayerController（玩家控制器）

**状态重置方法**：
```csharp
public void ResetState()
{
    // 取消所有延迟调用
    // 重置状态标记（isAttacking, canAcceptInput等）
    // 重置生命值到最大值
    // 更新血条显示
    // 重置动画为Idle
    // 重置攻击窗口和反制检测器
}
```

**重置时机**：
- RestartGame()：重置状态，准备新一轮对战
- EndGame()：重置状态，返回初始状态

### BossController（Boss控制器）

**状态重置方法**：
```csharp
public void ResetState()
{
    // 取消所有延迟调用
    // 重置状态标记（isPlaying, isPerformingAction等）
    // 重置动作索引
    // 重置生命值到最大值
    // 更新血条显示
    // 重置动画为Idle
    // 重置攻击窗口
    // 重置影子控制器
}
```

### PlayerPathRecorder（路径记录器）

**职责**：
- 记录玩家在每一局的输入序列
- 维护玩家的最远进度路径（历史最佳表现）

**单例模式**：Scene内唯一单例

**关键方法**：
```csharp
void RecordInput(AttackType attackType)     // 记录玩家输入
void OnPlayerDeath()                         // 玩家死亡时更新最远路径
void OnRestart()                            // Restart时清空当前小局，保留最远路径
void ClearAllPathData()                     // EndGame时完全清空
```

**数据管理**：
- `currentSessionInputs`：当前小局的输入（临时数据）
- `playerMaxPath`：最远进度路径（持久数据，Restart时保留）

### Shadow控制器（影子系统）

**PlayerShadowController**：
- 播放玩家的最远进度路径
- 提前leadTime秒启动，与Boss影子同步

**BossShadowController**：
- 播放Boss的攻击序列
- 提前leadTime秒启动，为玩家提供预判

**状态重置方法**：
```csharp
public void ResetState()
{
    // 停止当前播放
    // 清空动作序列
    // 重置状态标记
    // 播放Idle动画
}
```

## 游戏流程

### 1. 游戏启动
```
Scene加载
→ GameManager.Awake()：初始化单例
→ GameManager.Start()：进入MainMenu状态
→ 显示StartButton，禁用Player/Boss
```

### 2. 开始游戏
```
玩家点击StartButton
→ GameManager.StartGame()
→ ChangeState(Playing)
→ 启用Player输入
→ 启动Boss序列（包含Boss影子）
→ 启动Player影子（提前leadTime秒）
```

### 3. 游戏进行中
```
Player: 接收输入 → 播放动画 → 记录到PathRecorder
Boss: 按序列播放攻击 → 触发攻击判定窗口
攻击判定: 判断压制关系 → 计算伤害
```

### 4. 玩家死亡
```
PlayerController.Die()
→ PathRecorder.OnPlayerDeath()：更新最远路径
→ GameManager.OnPlayerDeath()
→ ChangeState(Defeat)
→ 显示RestartButton和EndButton
```

### 5. 重启游戏（RestartGame）
```
玩家点击RestartButton
→ GameManager.RestartGame()
→ PathRecorder.OnPlayerDeath()：确保更新最远路径
→ PathRecorder.OnRestart()：清空当前小局
→ PlayerController.ResetState()：重置玩家
→ BossController.ResetState()：重置Boss
→ PlayerShadowController.ResetState()：重置玩家影子
→ ChangeState(Playing)：重新开始游戏
```

### 6. 结束游戏（EndGame）
```
玩家点击EndButton
→ GameManager.EndGame()
→ PathRecorder.ClearAllPathData()：清空所有路径
→ PlayerController.ResetState()：重置玩家
→ BossController.ResetState()：重置Boss
→ PlayerShadowController.ResetState()：重置玩家影子
→ ChangeState(MainMenu)：返回主菜单
```

## 优势

### 相比Scene切换的优势

1. **引用稳定**：所有Inspector中拖拽的引用始终有效，不会因Scene切换而丢失
2. **代码简洁**：不需要处理Scene加载事件、DontDestroyOnLoad等复杂逻辑
3. **调试方便**：所有对象都在同一Scene中，方便在Editor中查看和调试
4. **性能更好**：避免Scene加载开销，重置操作比重新创建更轻量
5. **易于维护**：状态管理清晰，每个组件负责重置自己的状态

### 设计哲学

- **明确的所有权**：每个组件负责管理自己的状态
- **可预测的行为**：重置操作明确定义，不依赖Scene生命周期
- **解耦合**：GameManager协调，但不直接操作组件内部细节
- **易扩展**：添加新组件时，只需实现ResetState()方法

## 注意事项

### 1. 单例模式
- 所有单例都是Scene内单例，不跨Scene持久化
- 场景中只能有一个该组件的实例
- 在OnDestroy中清理Instance引用

### 2. 引用管理
- 所有引用在Inspector中拖拽赋值
- 不使用FindObjectOfType等运行时查找（性能考虑）
- 在ResetState()中不需要重新获取引用

### 3. 状态重置
- 每个组件必须实现完整的ResetState()方法
- 重置时需要取消所有延迟调用（CancelInvoke）
- 重置顺序：PathRecorder → Player → Boss → Shadows

### 4. 测试建议
- 多次Restart测试状态重置的完整性
- 验证EndGame后所有数据都被清空
- 检查没有内存泄漏（对象池、事件监听等）

## 未来扩展

如果需要添加新的管理类或功能组件，遵循以下原则：

1. **实现ResetState()方法**：提供完整的状态重置逻辑
2. **在GameManager中调用**：在RestartGame()和EndGame()中调用该组件的ResetState()
3. **使用Scene内单例**：避免DontDestroyOnLoad
4. **保持引用稳定**：不在运行时动态创建/销毁核心组件

## 总结

这种架构设计通过**状态管理**替代**Scene管理**，大幅简化了游戏的生命周期管理，提高了代码的可维护性和稳定性。所有的复杂性都被封装在各个组件的ResetState()方法中，使得系统行为可预测、易调试。
