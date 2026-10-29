# 《三角洲契约》可玩性详细优化方案（实现级）

> 适用：Codex / 后续制作。所有改动点均基于当前仓库代码复核（`LevelManager` / `WeaponController` / `PickupPool` / `EnemyAI` / `SideEnemyMotor` / `SegmentBuilder` / `PlatformSurface` / `StageHazard` / `StageInteractable` / `GameConfig`）。
> 配套文档：`PLAYABILITY_VS_CONTRA.md`（对比表，已更正）、`PERFORMANCE_OPTIMIZATION_PLAN.md`（性能）。

---

## 0. 更正与现状复核（先读这段）

**已证实"主题几何/机关"其实已经落地**（`SegmentBuilder.cs`）：
| 关卡 | 已实现机制 |
|---|---|
| Stage1 城市废墟 | `upper.Crumbling=true`（崩塌跳台）+ 爆燃油桶 `ConfigureBarrel` |
| Stage2 工厂蜂巢 | 传送带 `ConveyorBelt` + 液压冲柱 `HydraulicPress` + 电磁门 `ElectromagneticDoor`+开关 |
| Stage3 寒峰隧道 | `upper.Crumbling+Ice`（冰面惯性）+ 冰面接口 `surface.Ice` 已接通 `SidePlayerMotor` |
| Stage4 未来都市 | 电梯 `Elevator` + 激光栅栏 `TimedLaserGrid` + 全息墙 |
| Stage5 最终要塞 | 电梯 + 液压 + 激光 + 油桶 + 混合 |

`PlatformSurface` 的崩塌（踩 0.6s 塌、3s 复原）、`StageHazard` 的液压/激光伤害（12×DamageScale）均已实现。

**因此真正剩余的差距只剩 4 项（比初判小）**：
1. **封锁门硬卡死**（最致命）：`LevelManager.Update` 用 `GateLocked = encounter.Count > 0` 把玩家挡在物理墙后，必须清空敌人才放行 → 抹掉"向前冲"的爽感。
2. **敌兵阵容不随主题变化**：`LevelManager.BeginEncounter` 写死 `Chaser/Gunner/Elite/Runner`，5 关打起来战斗体验一样。
3. **缺掉落式武器胶囊**（S 散弹效应）：`PickupPool` 只管经验，`EnemyAI.OnDied→Pickup.Drop` 只掉经验，没有"捡到即强、限时消失"的火力爆发。
4. **敌兵行为单一**：现有 4 种都是"朝你移动+射击"，缺炮塔/狙击/飞行编队这种"逼你换打法"的单位（机关已能逼跳，但敌兵不会）。

---

## 1. P0-A｜解除封锁门 → 软推进（体感提升最大，风险最低）

### 1.1 现状代码
- `LevelManager.Update`（66–87）：`GateLocked = encounter.Count > 0;` 且 `gates[SegmentIndex]` 是带 `BoxCollider2D` 的物理墙，清空才 `SetActive(false)`。
- `LevelManager.BeginEncounter`（89–109）：到 `segment.start+5` 触发，刷一波敌。

### 1.2 设计
把"清空才放行"改为"推进解锁 + 压迫式残留"：
- **Gate 不再阻挡**：初始化时给 `gates[index]` 的 `BoxCollider2D` 设 `enabled=false`，仅保留半透明视觉作"进度门"。
- **推进看位置不看清场**：玩家 `x ≥ segment.End - 1.8` 即进入下一段（Boss 段 `segment.boss` 除外，保留硬锁）。
- **旧敌回收**：玩家越过段末后，仍活着的旧段敌人若落在相机左边界外（`x < CameraLeftX() - 4`），静默回收（不计击杀、不掉落），避免堆积与回头刷怪。
- **维持压迫**：新段 `start+3` 处预刷 1–2 个追兵（复用 `BeginEncounter` 的精简版）。

### 1.3 改动清单（`LevelManager.cs`）
```csharp
// Initialize() 中建 gate 后：gate.GetComponent<BoxCollider2D>().enabled = false;  // 不再挡路

private void Update()
{
    if (!game.IsPlaying || game.Player == null) return;
    SegmentDefinition segment = Segments[SegmentIndex];
    float position = game.Player.transform.position.x;
    if (!EncounterStarted && position >= segment.start + 5) BeginEncounter();
    if (!EncounterStarted || segment.boss) return;

    // —— 软推进：不靠清场解锁，越过段末即前进 ——
    float leftBound = game.CameraRig.CameraLeftX() - 4f;
    for (int i = game.Enemies.Count - 1; i >= 0; i--)
    {
        EnemyAI e = game.Enemies[i];
        if (e != null && e.Kind != EnemyKind.Boss && e.transform.position.x < leftBound) e.Recycle();
    }
    if (position >= segment.End - 1.8f && SegmentIndex < Segments.Length - 1)
    {
        SegmentIndex++; CheckpointSegment = SegmentIndex;
        Checkpoint = new Vector2(Segments[SegmentIndex].start + 2, 1);
        checkpointFlag.position = Checkpoint + Vector2.down * 0.3f;
        checkpointHealth = Mathf.Max(game.Player.Health.Current, game.Player.Health.Maximum * 0.5f);
        checkpointAmmo = game.Player.Weapon.CaptureAmmo();
        game.Player.Health.Restore(12);
        EncounterStarted = false;
        game.Hud.Announce("推进 / 第 " + (SegmentIndex + 1) + " 区", 2);
    }
}
```

### 1.4 配套新增
- `CameraRig` 暴露 `public float CameraLeftX() => Camera.transform.position.x - Camera.orthographicSize * Camera.aspect;`
- `EnemyAI` 新增 `public void Recycle()`：从 `game.Enemies` 移除、`Destroy(gameObject)`，但**不调用** `RecordKill`/`Pickups.Drop`/`Effects.Burst`（静默回收）。

### 1.5 验收
- 玩家从起点到 Boss 全程不被墙阻挡，可保持向右节奏；
- 越过后旧敌不在屏幕外堆积、不回头刷；
- Boss 段仍硬锁；检查点/死亡重置逻辑不受影响（`Retry` 仍按 `CheckpointSegment` 还原）。

---

## 2. P0-B｜掉落式武器胶囊（S 散弹效应）

### 2.1 现状
- `WeaponDefinition` 字段：`displayName, damage, shotsPerSecond, magazine, reloadSeconds, pellets, spreadDegrees, targetsPerBullet, projectileSpeed, color, explosionRadius, recoil`。
- `PickupPool` 仅管理 `ExperiencePickup`；掉落入口 `EnemyAI.OnDied → game.Pickups.Drop(pos, exp)`。
- `WeaponController.Fire()` 已支持按 `definition` 生成多弹丸/散射/穿透。

### 2.2 设计：三层
**(a) 胶囊种类（P0 先做 3 种，足够出彩）**
| 种类 | 效果（在 Fire 时临时叠加） | 对标魂斗罗 |
|---|---|---|
| Spread 散弹 | `pellets = max(pellets,5)`，`spreadDegrees=26` → 扇形齐射 | S 散弹枪 |
| Machine 机枪 | `shotsPerSecond×2.2`，`damage×0.6`，`pellets=1` | M 机枪 |
| Laser 激光 | `targetsPerBullet+=4`（高穿透），`projectileSpeed×1.6`，`spread=0`，青色 | L 激光 |

（Missile 追踪弹留 P1，需要 `Projectile` 加转向支持。）

**(b) WeaponController 增加覆盖层**（临时、不污染弹药/换弹系统）
```csharp
public enum CapsuleKind { None, Spread, Machine, Laser }
private float capsuleUntil; private CapsuleKind capsule;
public void ApplyCapsule(CapsuleKind kind) { capsule = kind; capsuleUntil = Time.time + 10f;
    game.Hud.Announce("武器胶囊 / " + kind, 1.5f); }
```
在 `Fire()` 开头计算有效参数：
```csharp
WeaponDefinition d = Definition;
int pellets = d.pellets; float spread = d.spreadDegrees; int pierce = d.targetsPerBullet;
float speed = d.projectileSpeed; float dmg = d.damage; float rate = d.shotsPerSecond;
if (Time.time < capsuleUntil)
{
    if (capsule == CapsuleKind.Spread) { pellets = 5; spread = 26f; }
    else if (capsule == CapsuleKind.Machine) { rate *= 2.2f; dmg *= 0.6f; pellets = 1; }
    else if (capsule == CapsuleKind.Laser) { pierce += 4; speed *= 1.6f; spread = 0; }
}
cooldown = 1f / (rate * game.Stats.FireRateMultiplier * ...);
// 循环用 pellets/spread/pierce/speed/dmg 替代 definition.*（其余不变）
```
`ResetAt`/`RestoreAmmo` 时 `capsuleUntil = 0`（死亡/检查点清胶囊）。HUD 的 `weaponText` 追加 `capsule != None ? " ⚡"+capsule : ""` 并显示剩余秒数（`Mathf.CeilToInt(capsuleUntil-Time.time)`）。

**(c) 胶囊拾取物**
- 新建 `CapsulePickup.cs`（轻量，仿 `ExperiencePickup` 激活/合并模式）：玩家靠近（`<1.2`）即 `game.Player.Weapon.ApplyCapsule(kind)` 并自身灭活。
- `PickupPool` 增加第二池 `CapsulePickup[]`（同样用 `maximumPickups` 一半容量），新增 `DropCapsule(Vector2, CapsuleKind)`。
- 掉落：在 `EnemyAI.OnDied` 按概率（如 `Random.value < 0.12f`）或精英/油桶必掉，`game.Pickups.DropCapsule(pos, kind)`；`kind` 随机或按主题偏好。

### 2.3 验收
- 击杀敌人有概率掉胶囊，玩家踩到即时切换火形、HUD 显示倒计时 10s；
- 10s 后自动恢复原本武器，不影响弹药/换弹；
- 死亡/检查点后胶囊清除；`maximumPickups` 池容量足够（当前 128，分一半给胶囊绰绰有余）。

---

## 3. P0-C｜敌兵阵容随主题变化（修正后的"主题差异化"）

### 3.1 现状
`LevelManager.BeginEncounter`（100–107）写死 `Chaser/Gunner/Elite/Runner`，与 `SelectedStage` 仅微调数量。

### 3.2 设计：每关招牌阵容表
```csharp
// 在 LevelManager 内定义（或并入 ThemeProfile）
private static readonly EnemyKind[][] Roster = new[]
{
    new[]{ EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Runner },        // S1 废墟：近战压制+飞行骚扰
    new[]{ EnemyKind.Gunner, EnemyKind.Runner, EnemyKind.Gunner },        // S2 工厂：火力+传送带冲锋
    new[]{ EnemyKind.Runner, EnemyKind.Runner, EnemyKind.Gunner },        // S3 寒峰：飞行编队+冰面
    new[]{ EnemyKind.Gunner, EnemyKind.Elite, EnemyKind.Runner },         // S4 未来：狙压+精英
    new[]{ EnemyKind.Elite, EnemyKind.Gunner, EnemyKind.Chaser },         // S5 要塞：重装混编
};
```
`BeginEncounter` 改为按 `Roster[Stage]` 取 kind；数量仍随 `Stage>1`/`Difficulty==2` 增加。配合 §4 引入 Turret/Sniper 后，再把招牌敌编入对应关。

### 3.3 验收
- 每关第一段 encounter 的敌兵种类肉眼可辨不同；
- 阵容表可经 `GameConfig`/`ThemeProfile` 调整，不硬编码死。

---

## 4. P1｜敌人行为扩展（逼玩家换打法）

### 4.1 新增 `EnemyKind`：`Turret`（炮塔）、`Sniper`（狙击）；飞行编队复用 `Runner`+新刷法。
- `EnemyAI.Initialize` 的 `switch` 增加 case（Turret：不动、`gravityScale=0`、可破坏 Health；Sniper：高伤、长蓄力）。
- `SideEnemyMotor.Step` 增加分支：
  - **Turret**：不追玩家，`velocity.x=0`，周期朝玩家 `Fan(3, 18, 6)` 射击；被击毁后 `Burst` 并可能掉胶囊。
  - **Sniper**：保持 `distance>12`，蓄力时用 `game.Effects.WarningLine` 画红线，`resolveAt` 后高伤单发（`damage×3`）；逼迫玩家接近或绕掩体。
  - **Formation**：`BeginEncounter` 一次性刷 3–5 个 `Runner` 沿弧线俯冲（`body.linearVelocity` 给斜向初速），过场后撤离左侧回收。
- `EnemyRoster.Names` 二维数组加列（Turret/Sniper 名称）。

### 4.2 验收
- 至少 Stage2 出现不可动的炮塔（可破坏）、Stage4 出现蓄力狙击；玩家必须用不同走位应对。

---

## 5. P1｜可破坏掩体 / 油桶增强（战术道具）

### 5.1 现状
`StageInteractable.ConfigureBarrel()` 已有爆炸 + `Game.Pulse(4,90)` 清场。

### 5.2 增强
- 爆炸时 `game.Pickups.DropCapsule(pos, Spread)` 概率触发（让"打桶"成为主动战术，呼应魂斗罗打桶掉枪）。
- 新增可破坏木箱（挡子弹 + 死亡掉少量经验），复用 `StageInteractable` 加 `Crate` 分支。
- 让油桶被近战/爆炸连锁（`Explode` 时对半径内其他 Barrel 调用 `Explode`）。

### 5.3 验收
- 玩家可主动打爆油桶清掉一群杂兵并可能掉胶囊；
- 木箱提供临时掩体，增加地形博弈。

---

## 6. P1｜强制跳/躲机关密度

### 6.1 现状
`StageHazard`（液压 Kind1 / 激光 Kind0）已存在且造成伤害；但每关仅 1–2 个。

### 6.2 增强
- 在 `SegmentBuilder` 各关按主题加密 hazard：如 Stage3 增周期落石（复用 `Telegraph`+`BossField` 思路，或新 `FallingRock` 用 `StageHazard` 变形）、Stage1 增周期地刺。
- 让 hazard 与敌兵刷新错峰，制造"先躲机关再清敌"的节奏。

### 6.3 验收
- 每个非 Boss 段至少 2–3 个需跳跃/走位规避的机关，且不与敌人重叠到无解。

---

## 7. P2｜体验上限

- **双人本地合作**：`InputReader` 双设备、`SidePlayerMotor` 第二实例、`CameraRig` 取两人中点+zoom。
- **瞄准模式选项**：保留 twin-stick；额外提供"键盘八向体瞄"（用 `Move` 方向决定 `Aim`，不依赖指针），满足硬核玩家"跳起按住上斜射天花板"的魂斗罗手感。
- **记忆点 set-piece**：载具/电梯演出段、Boss 前过场（复用 `Announce`+`SkillWave`）。

---

## 8. 施工顺序与总体验收

**建议顺序**：`P0-A（软推进）→ P0-B（武器胶囊）→ P0-C（阵容差异化）→ P1（敌人扩展/掩体/机关）→ P2`。
- P0-A/B/C 风险低（不破坏现有对象池/检查点/评级），可并行施工；
- P1 改动 `EnemyAI`/`SideEnemyMotor` 枚举分支，需回归 `SideScrollerSmokeTest -moriStage1`；
- 全部完成后用 Profiler 确认软推进下敌人残留不引起 Draw Call/GC 异常（见性能方案）。

> 说明：本方案只动"可玩性/设计"。性能层优化（敌人分离 O(N²)、弹丸物理、BuildArena）见 `PERFORMANCE_OPTIMIZATION_PLAN.md`，可并行施工、互不冲突。
