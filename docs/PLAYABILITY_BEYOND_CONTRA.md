# 《三角洲契约》超越魂斗罗 · 新增系统方案（实现级）

> 配套：`PLAYABILITY_VS_CONTRA.md`（对比+追平）、`PLAYABILITY_OPTIMIZATION_DETAILED.md`（P0 软推进/胶囊/阵容）、`PERFORMANCE_OPTIMIZATION_PLAN.md`（性能）。
> 本文只谈**新增、且魂斗罗没有**的系统。这些与"追平"方案**叠加**，而非替代——先追平手感节奏，再用本文件把上限拉到魂斗罗之上。
>
> 设计原则：每条都建立在已存在的地基上（Q 技能冷却体系、经验升级 `Stats`、评级、检查点、胶囊池、上/下分层关卡几何），不另起炉灶。

---

## 0. 为什么"追平"之后还要"超越"

经典魂斗罗的体验上限是：2 个角色、1 把枪（捡到限时变种）、纯线性、死了从头、无成长、无分数深度、无合作救援。你的工程**已经领先**的部分：4 角色专属技能、闪避无敌帧、经验/roguelite 升级、S/A/B/C 评级解锁配色、三档视角、Boss 三阶段。**真正还缺的是"魂斗罗从未提供过的现代爽点"**——下面 8 条按"杠杆 / 风险 / 工作量"排序。

| # | 系统 | 对标魂斗罗缺失 | 杠杆 | 风险 | 工作量 |
|---|---|---|---|---|---|
| B1 | 能量终结技 Ultimate | 完全没有"大招"概念 | ★★★★★ | 低 | 中 |
| B2 | 连击 / 风格评分 | 无连击、无分数深度 | ★★★★★ | 低 | 中 |
| B3 | 胶囊融合 | 只能单武器切换 | ★★★★ | 低 | 小 |
| B4 | 分支密道 + 秘密奖励 | 严格线性 | ★★★★ | 低 | 中 |
| B5 | 突变器 / 每日挑战 | 无任何变体 | ★★★ | 低 | 中 |
| B6 | 开局配装 Loadout | 固定开局 | ★★★ | 低 | 中 |
| B7 | Boss 处决演出 | 无"演出感"收尾 | ★★★★ | 中 | 中 |
| B8 | 双人合作 + 倒地救援 | 2P 只是复制 | ★★★ | 高 | 大 |

---

## B1｜能量终结技（Ultimate）—— 最大的"比魂斗罗爽"来源

### 设计定位
魂斗罗一辈子只有"手里的枪"。给每个角色一个**可积攒、可主动释放的终结技**：打敌人/挨打都涨能量，满了按一个键放出**角色专属的清屏大招**。这是现代动作游戏最强的 dopamine 源，且你的 `CharacterSkill` 冷却体系、`CombatEffects.SkillWave/Ghost`、`Projectiles.Fire` 都现成。

### 现有地基
- `PlayerController`：有 `Health / Weapon / Motor / Skill`，无能量字段。
- `CharacterSkill`：纯冷却（`readyAt`），不是资源型——**不动它**，Ultimate 用独立资源，避免和 Q 技能冲突。
- `GameManager`：`Projectiles / Effects / Pulse(pos,r,dmg) / Stats.DamageMultiplier` 全部可用。

### 改动清单
**(a) `PlayerController` 加能量字段与充能**
```csharp
public float Energy { get; private set; }          // 0..1
public float EnergyRemaining => Energy;             // HUD 用
public void AddEnergy(float amount) => Energy = Mathf.Clamp01(Energy + amount);
public bool TryUltimate()                           // 绑定新 InputReader.Ultimate
{
    if (Energy < 1 || Health.IsDead || !game.ControlsReady) return false;
    Energy = 0;
    int c = game.SelectedCharacter;
    game.Effects.SkillWave(transform.position, game.Character.accent, 5);
    game.CameraRig.Shake(0.5f);
    if (c == 0) game.Pulse(transform.position, 7f, 120);                       // 契约：聚变脉冲
    else if (c == 1) { player.Health.Restore(player.Health.Maximum);            // 壁垒：满血+护盾
        player.Health.DamageReduction = 0.9f; Invoke(nameof(ClearDR), 6f); }
    else if (c == 2) for (int i=0;i<24;i++){ var h=Quaternion.Euler(0,0,i*15)*Vector2.right;  // 幽影：环爆
        game.Projectiles.Fire(transform.position, h, 18, 30*game.Stats.DamageMultiplier, Team.Player,2,game.Character.accent,false,0,3);}
    else for (int i=0;i<3;i++) game.Projectiles.Fire(...);                      // 爆破：三发巨型爆破弹
    game.Hud.Announce(game.Character.name.Split('/')[0].Trim()+" / ULTIMATE",1.6f);
    return true;
}
```
**(b) 充能来源**（在 `EnemyAI.OnDied` 与 `PlayerController.OnDamaged` 里各加一句）
```csharp
game.Player.AddEnergy(0.06f);            // 击杀 +6%
game.Player.AddEnergy(0.025f);           // 受击 +2.5%（上限 1，避免挨打刷大招过快可设上限 0.1/次）
```
**(c) `InputReader` 新增 `public bool Ultimate`**（建议键位 `E` 或 `F`，手柄 `RB+Y`）。
**(d) HUD：右下角能量环**——复用 `HUDController` 现有 `skillText` 旁新增能量条/环，满时闪烁提示。

### 验收
- 击杀约 16 个敌充满能量；满后按键放出角色专属大招、能量清零；
- 四人大招手感/视觉明显不同；不干扰 Q 技能冷却；
- 死亡/检查点 `ResetForCheckpoint` 一并清能量。

---

## B2｜连击 / 风格评分（Combo & Style）—— 让"一直冲一直打"上瘾

### 设计定位
魂斗罗没有连击、没有分数深度，打 1 个和打 10 个没有任何正反馈差异。加一套**击杀连段 + 风格倍率**：短时间内连续击杀累积 combo，倍率抬升总分与评级，并弹出 `PERFECT / x12 CHAIN` 飘字。直接奖励"向前冲、不站桩"的核心循环。

### 现有地基
- `GameManager.RecordKill()`（198 行）是统一击杀入口 → 在此挂连击逻辑最干净。
- `CombatEffects` 已有 `DamageNumber` / `Burst` / 飘字通道。
- `LevelManager` 已有 `Rating`（S/A/B/C）与 `Chips` → 把风格分并入评级权重。

### 改动清单
**(a) 新增 `ComboTracker`（轻量，挂在 GameManager）**
```csharp
private float comboExpire; public int Combo { get; private set; } public float StyleMultiplier => 1 + Mathf.Min(Combo,30)*0.05f;
public void OnKill()                                       // 在 RecordKill 内调用
{
    if (Time.time > comboExpire) Combo = 0;
    Combo++; comboExpire = Time.time + 2.5f;               // 2.5s 窗口
    int bonus = Mathf.RoundToInt(10 * StyleMultiplier);
    score += bonus;
    if (Combo >= 3 && Combo % 3 == 0) game.Effects.FloatText(transform.position, "x"+Combo+" CHAIN", game.Character.accent);
}
```
**(b) `Update` 内归零**：`if (Time.time > comboExpire && Combo>0){ Combo=0; game.Hud.Announce("连击中断",0.8f);}`
**(c) 评级权重**：`LevelManager.Rating` 计算时把 `game.Score` 纳入（或新增 `StyleScore`），S 评价要求"高连击 + 低死亡"。
**(d) HUD**：顶部中央加 `comboText`（`x12` 放大跳动，倒计时环），复用 `announce` 的排版。

### 验收
- 连续击杀 combo 累加、2.5s 不击杀清零；
- 风格倍率随 combo 提升总分；S 评价更难、需维持高连击；
- 飘字不引起 GC（复用 CombatEffects 池，见性能方案已做空闲栈）。

---

## B3｜胶囊融合（Capsule Fusion）—— 给 P0-B 加深度

### 设计定位
魂斗罗只能"换一把枪"。P0-B 落地"掉落胶囊限时切换"后，再加**融合**：身上已有胶囊时再吃不同种，合成混合形态（如 散弹+激光 = 穿透扇）。深度远超魂斗罗的单武器切换，且复用 P0-B 的 `CapsuleKind` 与 `WeaponController` 覆盖层。

### 改动清单（`WeaponController`）
```csharp
private CapsuleKind capsule, capsule2;   // 第二槽
public void ApplyCapsule(CapsuleKind k)
{
    if (capsule == CapsuleKind.None) capsule = k;
    else if (capsule2 == CapsuleKind.None && k != capsule) capsule2 = k;  // 异种融合
    else capsule = k;                                                     // 同种刷新
    capsuleUntil = Time.time + 10f;
}
// Fire() 内：若 capsule2 != None，套用融合表（如 Spread+Laser → pierce+=4 & pellets=5 & spread=26）
```
融合表（建议）：`Spread+Machine=暴雨(高射速扇形)`、`Spread+Laser=光扇(穿透扇形)`、`Machine+Laser=光刃(高射速穿透)`。HUD 显示两个胶囊图标 + 剩余秒。

### 验收
- 异种胶囊叠加出第三种形态、HUD 双图标；
- 10s 后两层同时清空；不影响弹药/换弹。

---

## B4｜分支密道 + 秘密奖励（Branching Routes）—— 打破线性

### 设计定位
魂斗罗严格线性，没有"你选不走哪条路"。`SegmentBuilder` 已支持 `upper/lower` 分层几何——把**上层路线做成可选密道**：入口隐蔽、走过有风险但必定掉 1 个胶囊 / 秘密芯片 / 满能量。制造探索与重玩动机。

### 现有地基
- `SegmentBuilder`：每关已有 `upper`（崩塌/冰面/电梯等）几何。
- `LevelManager`：`Chips`（3 收集）、`Checkpoint` 机制。
- `PickupPool.DropCapsule`（P0-B 落地后）可直接在密道末投放。

### 改动清单
- `SegmentBuilder` 给每段 `upper` 加一个**非必经**入口（需二段跳/闪避到达），末端放 `DropCapsule` 或 `Chips+1` 或 `Energy=1` 的秘密奖励台。
- `LevelManager`：密道奖励不强制，但计入 `Chips` 与"探索率"（新增 `ExploredSecrets`），S 评价可要求"全密道"。
- HUD 加 `secretText`："密道 2/5 已探"。

### 验收
- 每关至少 1 条肉眼可辨的上层密道，末端有奖励；
- 不走密道也能通关（不卡进度）；走密道提升评分/资源。

---

## B5｜突变器 / 每日挑战（Mutators & Daily Run）—— 重玩性发动机

### 设计定位
魂斗罗一局永远一样。加**局内全局修饰符** + **每日种子**：开局部署 1–2 个突变（低重力、敌人爆炸、仅近战、双倍能量、镜面视角），每日挑战用日期哈希固定种子与突变组合，排行榜可比。直接建在现有 `GameManager.Difficulty`（三级）之上。

### 现有地基
- `GameManager.Difficulty` 已支持 0/1/2 三档 → 突变器是"第四维"变体，不改难度轴。
- `SidePlayerMotor` 已有 `gravityScale`/冰面/传送带接口，突变可复用。
- `RunModel`（Stats）是所有数值的汇聚点 → 突变在此乘算最干净。

### 改动清单
- 新增 `Mutator` 枚举 + `GameManager.mutators[]`（菜单或每日种子填充）。
- 示例实现钩子：
  - `LowGravity`：`Motor` 起跳初速 ×1.6、重力感 ×0.6（用现有 `jumpVelocity`/表面系统）。
  - `ExplosiveEnemies`：`EnemyAI.OnDied` 额外 `game.Pulse(pos,3,40)`。
  - `MeleeOnly`：禁用远程，强制胶囊/近战（改 `WeaponController` 开火许可）。
  - `DoubleEnergy`：`AddEnergy` 倍率 ×2（配合 B1）。
- 菜单加"每日挑战"按钮：`Seed = (int)(DateTime.Today...)` 决定突变组合。

### 验收
- 选突变后整局生效且 HUD 标注；
- 每日挑战同日同种子可复现；不影响主线通关与评级。

---

## B6｜开局配装（Pre-run Loadout）——  Roguelite 前戏

### 设计定位
魂斗罗开局固定。你在菜单已有"角色 + 武器"选择（`HUDController` 角色/武器按钮、`GameConfig.weapons` 6 把、`characters` 4 个）→ 再扩成**三选一配装**：起始武器 + 1 个起始胶囊（P0-B 落地后）+ 1 个起始 perk（如 +1 闪避次数 / 起始能量 50% / 起始移速）。让每次开局有构筑感。

### 改动清单
- `HUDController` 菜单增加 `perkButtons[]`（`GameConfig` 新增 `perkDefs[]`：名称+效果标签）。
- `GameManager.StartRun` 应用：`player.Weapon.Select(startWeapon)`、`player.ApplyCapsule(startCapsule)`（若 B1/B3 已落地）、`RunModel` 乘算 perk。
- 起始胶囊仅给"预览体验"，10s 后回归正常（与 P0-B 一致）。

### 验收
- 菜单可选 perk，开局即时生效；
- 不改核心平衡（perk 为小幅增益，避免破坏评级）。

---

## B7｜Boss 处决演出（Boss Finisher）—— 收尾的"演出感"

### 设计定位
魂斗罗打死 Boss 就是"它炸了"。加**处决窗口**：Boss 血量 < 15% 时进入 `Execute` 状态（停止攻击、闪烁），玩家持续输出或按住开火可触发**处决演出**（慢动作 + 角色突进终结 + 巨额风格分）。现代 boss 战的"高潮收尾"，也呼应你已有的 `SideBossController` 三阶段与 `SkillWave/Ghost`。

### 现有地基
- `SideBossController`：已有 `Phase`、`PatternName`、`Arms[]` 弱点。
- `CharacterSkill`：已有传送/突进/环爆逻辑可复用做演出。
- `CameraRig.Shake` + `Effects.SkillWave` 可做慢动作（`Time.timeScale` 短暂 0.3）。

### 改动清单
- `SideBossController`：血量 < 0.15 设 `Executable=true`，`game.Hud.Announce("处决窗口 / 全力输出",1.5f)`。
- 玩家在窗口内累计造成阈值伤害 → 触发 `Execute()`：
  ```csharp
  StartCoroutine(Finisher())
  {
      Time.timeScale = 0.35f; game.Effects.SkillWave(bossPos, accent, 6);
      player.Motor.TeleportTo(bossPos+Vector2.left);  // 复用 WRAITH 突进
      game.Projectiles.Fire(...爆发...);
      yield return new WaitForSecondsRealtime(0.8f); Time.timeScale = 1;
      boss.Health.Kill(); game.AddStyle(500);
  }
  ```
- 超时未处决则 Boss 回 15% 并继续（避免卡死）。

### 验收
- Boss <15% 出现处决窗口；
- 触发后有慢动作演出 + 风格分爆发；
- 超时安全回退、不卡流程。

---

## B8｜双人合作 + 倒地救援（Co-op Revive）—— 超越"复制人 2P"

### 设计定位
魂斗罗 2P 只是第二个相同角色。做成**真正合作**：两人独立角色/技能、共享进度、一方倒地进入 `Downed` 状态（可爬行、可被队友靠近救援复活），阵亡才扣命。社交黏性远超魂斗罗。

### 现有地基
- `InputReader` 双设备、`SidePlayerMotor` 第二实例、`CameraRig` 取两人中点+zoom（已在 P2 规划）。
- `Health` 已有 `IsDead` / `Died` 事件 → 扩 `Downed` 状态。

### 改动清单（高风险，放最后）
- `Health`：新增 `Downed` 状态（血量 0 不立即死，进入可救援）。
- `PlayerController`：第二实例 `isPlayer2`，`Downed` 时禁用开火、允许缓慢移动。
- 救援：队友靠近 `<1.5` 按住互动键 `Revive` → `Health.Restore(30%)`、`Downed=false`。
- 仅当两人都 `Downed/Dead` 才 `game.Lose`。

### 验收
- 双人同屏、独立操作、相机自适应；
- 一方倒地可被救援，全部倒地才失败。

---

## 施工顺序建议

**第一批（低风险、高杠杆，建议先做）**：B1 能量终结技 → B2 连击评分 → B3 胶囊融合（依赖 P0-B）→ B4 分支密道。
**第二批**：B5 突变器/每日 → B6 配装 → B7 Boss 处决。
**第三批**：B8 双人合作（跨文件多，留到最后，可独立成里程碑）。

> 与"追平"方案的关系：**P0（软推进/胶囊/阵容）必须先于或并行 B3/B4**，因为 B3 依赖胶囊池、B4 依赖密道投放胶囊。其余互不冲突。
> 性能注意：B1/B2/B7 会增加同屏特效与 `timeScale` 改动，落地后用 Profiler 复核（见 `PERFORMANCE_OPTIMIZATION_PLAN.md`），尤其 B7 慢动作期间避免 GC 尖峰。
