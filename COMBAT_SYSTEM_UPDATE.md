# 游戏机制更新说明

## 更新日期
2026年2月3日

## 更新内容

### 1. 玩家死亡后禁用输入系统 ✅

**修改文件**: `PlayerController.cs`

**变更内容**:
- 添加了 `canAcceptInput` 字段来控制玩家输入
- 修改 `HandleAttackInput()` 方法，增加输入控制检查
- 修改 `Die()` 方法：
  - 立即禁用玩家输入 (`canAcceptInput = false`)
  - 设置 `isAttacking = true` 防止任何攻击状态重置
  - 确保死亡动画播放不会被打断

**效果**: 玩家死亡后无法再进行任何操作，死亡动画会完整播放。

---

### 2. 攻击压制机制（剪刀石头布系统） ✅

**新增文件**: `AttackRelationship.cs`

**压制规则**:
```
AttackX (Q键-突刺) 克制 AttackY (W键-下压)
AttackY (W键-下压) 克制 AttackB (E键-防御)
AttackB (E键-防御) 克制 AttackX (Q键-突刺)
```

形成循环压制关系：**X → Y → B → X**

**设计理念**:
- X(突刺)快速，能在Y(下压)前摇时击中
- Y(下压)重击，势大力沉能击破B(防御)
- B(防御)稳固，能挡住X(突刺)

**三种攻击结果**:

1. **压制成功 (Counter)**
   - 条件：玩家出对应的压制招式
   - 例如：Boss出X，玩家出Y
   - 效果：
     - 玩家不减血
     - Boss减血（使用 `attackDamage`）
     - 玩家获得短暂无敌时间

2. **同时攻击 (Clash)**
   - 条件：玩家和Boss出相同招式
   - 例如：Boss出X，玩家也出X
   - 效果：
     - 双方都减血（使用 `clashDamage`）
     - 无无敌时间

3. **被击中 (Hit)**
   - 条件：玩家出被压制的招式或不出招
   - 例如：Boss出X，玩家出B 或不出招
   - 效果：
     - 玩家减血（使用 `attackDamage`）
     - Boss不减血

---

### 3. 角色属性配置更新 ✅

**修改文件**: `CharacterStats.cs`

**新增字段**:
- `clashDamage` (float): 同时攻击时的伤害值（默认40）

**使用方式**:
在Unity Inspector中为Player和Boss的CharacterStats配置：
- `maxHealth`: 最大生命值
- `attackDamage`: 普通攻击伤害（压制/被击中时使用）
- `clashDamage`: 同时攻击伤害（建议设置为 attackDamage 的 80%）

---

### 4. 攻击判定系统更新 ✅

**修改文件**: `AttackWindow.cs`

**主要变更**:
- 添加 `OnPlayerResponse()` 方法处理玩家响应
- 添加 `HandleDamageByResult()` 方法根据攻击结果处理伤害
- 保留 `OnCounterSuccess()` 方法以向后兼容

**工作流程**:
1. Boss开启攻击窗口
2. 玩家在窗口内做出响应
3. `AttackRelationship` 判定攻击结果
4. 根据结果处理伤害：
   - Counter: Boss受伤
   - Clash: 双方都受伤
   - Hit: 玩家受伤

---

### 5. 反制输入检测器更新 ✅

**修改文件**: `CounterInputDetector.cs`

**主要变更**:
- 修改 `TryCounter()` 方法使用 `AttackRelationship` 判定
- 添加 `OnPlayerAction()` 方法处理玩家攻击响应
- 只有压制成功时才给予无敌时间

---

## 游戏玩法示例

### 场景1: 压制成功
```
Boss: 发动 AttackY (下压)
Player: 及时按下 Q键 (AttackX - 突刺)
结果: Player压制成功！
- Player: 不减血，获得无敌时间
- Boss: 减血 (attackDamage)
解释: 突刺快速击中下压前摇
```

### 场景2: 同时攻击
```
Boss: 发动 AttackX (石头)
Player: 按下 Q键 (AttackX - 石头)
结果: 同时攻击！
- Player: 减血 (clashDamage)
- Boss: 减血 (clashDamage)
```

### 场景3: 被击中
```
Boss: 发动 AttackX (突刺)
Player: 按下 W键 (AttackY - 下压) 或 不出招
结果: Player被击中！
- Player: 减血 (attackDamage)
- Boss: 不减血
解释: 下压前摇慢，被突刺击中
```

---

## 配置建议

在Unity Inspector中设置Character Stats：

**Player Stats**:
- maxHealth: 100
- attackDamage: 50
- clashDamage: 40

**Boss Stats**:
- maxHealth: 200
- attackDamage: 50
- clashDamage: 40

---

## 测试要点

1. ✅ 测试玩家死亡后无法再输入
2. ✅ 测试死亡动画完整播放
3. ✅ 测试三种攻击结果的伤害计算
4. ✅ 测试压制成功时的无敌时间
5. ✅ 测试同时攻击双方都扣血
6. ✅ 测试日志输出是否正确显示战斗信息

---

## 注意事项

- 确保在Unity Scene中正确配置所有Character Stats
- 确保Boss的AttackWindow组件正确绑定了targetObject (Player)
- 确保GameLogger在场景中存在以查看战斗日志
- clashDamage建议设置为attackDamage的70-80%以保持平衡
