# 暗影系统 (Shadow System)

## 📋 系统概述

暗影系统是一个预判机制，通过在场景中显示Boss（或Player）的影子，提前播放动作序列，让玩家能够预判即将到来的攻击并做出反制。

### 核心特点
- ✅ **提前预判**：影子提前1秒（可配置）播放动作
- ✅ **纯视觉效果**：影子只播放动画，不造成伤害
- ✅ **无战斗逻辑**：影子没有血量、攻击判定、碰撞等战斗相关功能
- ✅ **完美同步**：影子自动跟随主体的动作序列
- ✅ **可扩展**：同一套系统可用于Boss和Player

---

## 🎮 Boss影子系统

### 已实现的脚本

#### `BossShadowController.cs`
Boss专用的影子控制器，负责：
- 从BossController复制动作序列
- 提前指定时间播放相同的动画
- 只播放动画，不触发任何战斗逻辑

---

## 🔧 在Unity中设置Boss影子

### 步骤1：创建Boss影子GameObject

1. **复制Boss对象**
   - 在Hierarchy中右键点击Boss对象 → `Duplicate`
   - 将复制的对象重命名为 `Boss_Shadow`

2. **调整影子外观**（推荐设置）
   - 修改材质颜色为半透明黑色或蓝色
   - 调整透明度（Alpha）约50-70%
   - 可选：添加发光效果（Emission）
   - 建议：略微缩小影子的Scale（如0.95）以示区分

3. **移除不需要的战斗组件**
   从Boss_Shadow上移除以下组件：
   - ❌ `AttackWindow`（影子不造成伤害）
   - ❌ `HPBar`（影子不显示血条）
   - ❌ 任何碰撞器/触发器（影子不参与物理交互）

4. **保留必要组件**
   确保Boss_Shadow上保留：
   - ✅ `Animator`（播放动画必需）
   - ✅ `SpriteRenderer`或模型渲染器（显示影子）

### 步骤2：添加BossShadowController组件

1. **添加脚本**
   - 选中`Boss_Shadow`对象
   - 点击`Add Component`
   - 搜索并添加`BossShadowController`

2. **配置Inspector参数**

   **动画绑定区域**（与Boss保持一致）：
   - `Idle Animation`：拖拽Boss的待机动画clip
   - `Attack X Animation`：拖拽Boss的X攻击动画clip
   - `Attack Y Animation`：拖拽Boss的Y攻击动画clip
   - `Attack B Animation`：拖拽Boss的B攻击动画clip

   **影子配置区域**：
   - `Boss Controller`：拖拽场景中的Boss对象（必须）
   - `Lead Time`：设置提前时间，默认1秒
     - 1.0 = 影子提前Boss 1秒执行动作
     - 0.5 = 提前0.5秒（更短的预判时间，增加难度）
     - 1.5 = 提前1.5秒（更长的预判时间，降低难度）
   - `Auto Follow`：保持勾选（自动跟随Boss）

### 步骤3：在BossController中关联影子

1. 选中原始的`Boss`对象
2. 在Inspector中找到`BossController`组件
3. 在`场景对象引用`区域，找到`Shadow Controller`字段
4. 将场景中的`Boss_Shadow`对象拖拽到这个字段

### 步骤4：测试

运行游戏后：
- ✅ Boss影子应该提前1秒播放相同的动作
- ✅ 影子不会造成任何伤害
- ✅ 影子不会触发攻击判定窗口
- ✅ 玩家可以通过观察影子预判Boss的下一步动作

---

## 🎨 视觉效果建议

### 材质设置（推荐）

1. **创建影子专用材质**
   - 复制Boss的材质
   - 重命名为`Boss_Shadow_Material`

2. **调整材质属性**
   - **颜色**：深蓝色或黑色 (RGB: 0, 50, 100)
   - **Alpha透明度**：50-70%
   - **Emission发光**（可选）：淡蓝色发光
   - **渲染模式**：设为`Transparent`

3. **位置调整**
   - 将影子放在Boss前方或旁边
   - 避免完全重叠（便于玩家区分）
   - 可选：Z轴偏移到前景或背景层

### Shader Graph特效（进阶）

如果使用URP的Shader Graph，可以添加：
- 扭曲效果（Distortion）
- 边缘发光（Rim Light）
- 波纹效果（Wave）
- 溶解效果（Dissolve）

---

## 🔍 工作原理

### 时间轴示例

假设Boss的动作序列是：`X -> Y -> X -> Idle`，`leadTime = 1秒`

```
时间轴：
T=0s:  Boss影子开始动作X  |  Boss处于Idle
T=1s:  Boss影子开始动作Y  |  Boss开始动作X
T=2s:  Boss影子开始动作X  |  Boss开始动作Y
T=3s:  Boss影子开始Idle   |  Boss开始动作X
T=4s:  Boss影子继续Idle   |  Boss开始Idle
```

### 关键代码逻辑

1. **启动序列**（BossController.StartSequence）
   ```csharp
   // 影子立即启动
   shadowController.StartShadowSequence();
   
   // Boss延迟leadTime秒启动
   Invoke(nameof(StartBossSequence), shadowController.leadTime);
   ```

2. **复制动作序列**（BossShadowController.CopyShadowSequenceFromBoss）
   ```csharp
   // 深度复制，避免引用同一对象
   foreach (var action in bossController.actionSequence)
   {
       shadowActionSequence.Add(new BossAction
       {
           actionType = action.actionType,
           duration = action.duration
       });
   }
   ```

3. **只播放动画**（BossShadowController）
   - ✅ 只有`Animator.Play()`调用
   - ❌ 没有`AttackWindow.StartWindow()`
   - ❌ 没有伤害计算
   - ❌ 没有碰撞检测

---

## 🐛 常见问题排查

### 问题1：影子不显示任何动画
**可能原因**：
- Boss Controller引用未赋值
- 动画clips未拖拽到Inspector
- Animator组件缺失

**解决方法**：
1. 检查Inspector中的`Boss Controller`字段是否已赋值
2. 确认所有动画clips都已正确拖拽
3. 确保影子GameObject上有Animator组件

### 问题2：影子和Boss同时播放动作（没有提前）
**可能原因**：
- Lead Time设置为0
- BossController的shadowController引用未设置

**解决方法**：
1. 检查`Lead Time`是否大于0（建议1.0）
2. 确认Boss对象上的`Shadow Controller`字段已赋值

### 问题3：影子造成了伤害
**可能原因**：
- 影子GameObject上仍然保留了AttackWindow组件
- 影子的碰撞器仍然激活

**解决方法**：
1. 移除影子上的`AttackWindow`组件
2. 移除或禁用影子上的所有碰撞器

### 问题4：影子的动作序列与Boss不一致
**可能原因**：
- Boss在运行时修改了动作序列
- 影子的动画clips与Boss的不一致

**解决方法**：
1. 确保影子的动画clips与Boss完全一致
2. 检查Boss的attackPatterns配置是否正确

---

## 📊 性能优化建议

### 1. 动画优化
- 影子可以使用简化的动画（降低帧率）
- 可以移除影子动画的粒子特效

### 2. 渲染优化
- 影子使用更简单的材质
- 可以降低影子的渲染层级优先级

### 3. 逻辑优化
- 当玩家死亡时，停止影子播放
- 暂停游戏时，停止影子更新

---

## 🚀 下一步：Player影子系统

完成Boss影子后，可以创建类似的`PlayerShadowController.cs`：

### Player影子的特点
- 不是预判用途，而是用于其他玩法（如残影、分身等）
- 可以跟随Player的输入历史
- 可以用于教学模式（显示正确的操作序列）

### 实现思路
1. 创建`PlayerShadowController.cs`
2. 记录Player的输入历史（攻击序列）
3. 让影子回放这些操作
4. 用途：训练模式、回放系统、幽灵对战等

---

## 📝 总结

Boss影子系统已完整实现，提供了：
- ✅ 完整的预判机制
- ✅ 纯视觉效果，不影响游戏逻辑
- ✅ 灵活的配置选项
- ✅ 清晰的代码结构
- ✅ 详细的使用文档

只需按照上述步骤在Unity中配置，即可启用Boss影子系统！

---

**版本**: 1.0  
**创建日期**: 2026-02-03  
**适用于**: Unity 2021.3+, URP
