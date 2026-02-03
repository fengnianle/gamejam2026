# Unity Script Execution Order 配置

## 🎯 为什么需要配置执行顺序？

自动反制功能需要确保 `PlayerController` 的 `Update()` 在 `AttackWindow` 的 `Update()` **之前**执行，防止时序问题：

- ❌ **错误顺序**：AttackWindow检查超时 → 扣血 → PlayerController执行反制（已经晚了）
- ✅ **正确顺序**：PlayerController执行反制 → AttackWindow检查超时（已被反制，不扣血）

## 📋 配置步骤

### 在Unity编辑器中设置

1. **打开 Project Settings**
   - 菜单栏：`Edit` → `Project Settings`
   - 或快捷键：`Ctrl+Shift+,` (Windows) / `Cmd+,` (Mac)

2. **找到 Script Execution Order**
   - 在左侧列表中找到并点击 `Script Execution Order`

3. **添加脚本并设置优先级**
   
   点击 `+` 按钮，按以下顺序添加脚本：

   ```
   ┌─────────────────────────────────────────┐
   │ Script Execution Order                  │
   ├─────────────────────────────────────────┤
   │ PlayerController         → -100         │  ← 最先执行（自动反制）
   │ CounterInputDetector     → -50          │  ← 第二执行（反制检测）
   │ AttackWindow            → 0 (Default)   │  ← 默认执行（攻击判定）
   │ BossController          → 0 (Default)   │  ← 默认执行
   └─────────────────────────────────────────┘
   ```

   **数值越小，执行越早！**

4. **应用设置**
   - 设置完成后会自动保存
   - 关闭 Project Settings 窗口

## 🔧 详细配置值

| 脚本名称 | 执行顺序值 | 说明 |
|---------|----------|------|
| `PlayerController` | **-100** | 最高优先级，确保自动反制先执行 |
| `CounterInputDetector` | **-50** | 中等优先级，处理反制输入 |
| `AttackWindow` | **0** | 默认优先级，检查窗口超时 |
| `BossController` | **0** | 默认优先级 |
| `GameManager` | **0** | 默认优先级 |

## 📸 配置截图参考

### 步骤1：打开 Project Settings
```
Edit → Project Settings → Script Execution Order
```

### 步骤2：添加脚本
1. 点击 `+` 按钮
2. 在弹出窗口中搜索 `PlayerController`
3. 选中后点击确认

### 步骤3：设置执行顺序
1. 在 `PlayerController` 右侧输入框输入 `-100`
2. 按 Enter 确认

### 步骤4：重复添加其他脚本
按相同方式添加 `CounterInputDetector` 并设置为 `-50`

## ✅ 验证配置

配置完成后，执行顺序应该如下：

```
每帧执行流程：
1. PlayerController.Update()      [-100] ← 检测Boss窗口并立即反制
2. CounterInputDetector.Update()  [-50]  ← 处理反制输入检测
3. AttackWindow.Update()          [0]    ← 检查窗口超时（已被反制）
4. BossController.Update()        [0]    ← Boss动作更新
```

## 🐛 如果不配置会发生什么？

**问题现象**：
- 自动反制功能失效
- Player仍然会被扣血
- 日志显示"反制成功"但实际已经受伤

**原因**：
- AttackWindow的Update先执行，检测到超时就立即扣血
- PlayerController的Update后执行，反制已经来不及了

## 🎮 测试自动反制

配置完成后：
1. 勾选 Player 的 `Auto Counter Enabled`
2. 运行游戏
3. 观察 Console 日志，应该看到：
   ```
   [自动反制] 检测到Boss的AttackX攻击，立即执行Q反制
   [自动反制] 反制判定完成：压制成功
   [Combat] 压制成功！Boss受到伤害，Player安全
   ```
4. Player不应该被扣血 ✅

## 📝 注意事项

- **负数表示提前执行**：-100 比 -50 更早，-50 比 0 更早
- **相同数值的脚本**：执行顺序不确定（随机）
- **不要设置过大的差值**：通常 -100 到 100 的范围就足够了
- **项目协作**：这个配置保存在 `ProjectSettings/EditorBuildSettings.asset`，记得提交到版本控制

## 🔄 重置配置

如果需要恢复默认设置：
1. 在 Script Execution Order 中选中脚本
2. 点击 `-` 按钮移除
3. 或将执行顺序改回 `0`

---

**配置完成后**，自动反制功能将完美工作！🎉
