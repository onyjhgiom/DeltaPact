# 从下向上推进式关卡（Ascent）完整设计与实施方案

> **目标**：在现有横版（向右推进）架构之上，新增 **3 个从下向上推进式闯关关卡**，并达到**制作级高级感**（不是"能玩"，而是"看起来就贵、玩起来上瘾"）。
>
> **三大招牌**：① 镜头/推进改为竖直向上；② **可破坏的骷髅头填充墙**封住上升通道，必须向上打穿才放行；③ 敌人**从天而降/攀壁而下**，配合底部"上升深渊"压迫，形成"被上下夹击、必须不停向上"的张力。
>
> **四大高级化维度**（§8）：美术方向 · 光影材质 · 特效演出 · 镜头/音效/UI 质感。
>
> **本文性质**：实现级规格。所有字段名、文件名、行号均来自实际代码核对，可直接转交 Codex 施工。
> **核对基准**：`LevelManager` / `SegmentBuilder` / `ThemeProfile` / `CameraRig` / `SideCameraDirector` / `SidePlayerMotor` / `EncounterDirector` / `EnemyRoster` / `GameConfig` / `GameManager` / `HUDController` / `CombatEffects` / `ProceduralArt` / `SideArt`。
>
> **本文取代** `DESCENT_LEVELS_DESIGN.md`（向下潜行版）。两者架构共享，仅推进轴方向相反。

---

## 目录

1. [现状盘点（代码事实）](#1)
2. [总体架构方案](#2)
3. [竖直推进系统](#3)
4. [相机系统](#4)
5. [招牌机制：骷髅头填充墙](#5)
6. [敌人：从天而降](#6)
7. [三关详细设计](#7)
8. [**高级化：制作级表现**](#8) ★
9. [玩家操作与手感](#9)
10. [关卡几何构建](#10)
11. [战役与 UI 集成](#11)
12. [性能预算](#12)
13. [实施计划](#13)
14. [验收与测试](#14)
15. [风险与应对](#15)
16. [附录：文件改动清单总表](#16)

---

<a id="1"></a>
## 1. 现状盘点（代码事实）

### 1.1 推进系统 — `Assets/Scripts/Level/LevelManager.cs`

```csharp
// 现状：段是 X 区段
Segments[index] = new SegmentDefinition {
    start = index * 34, width = index == combatSegments ? 38 : 34,
    layout = random.Next(3), boss = index == combatSegments
};                                          // combatSegments = 7 → 共 8 段
public float Length => Segments[Segments.Length - 1].End;   // End = start + width

// 现状：Update 用 X 推进（软推进已落地，门常开）
float position = game.Player.transform.position.x;
if (!EncounterStarted && position >= segment.start + 5) BeginEncounter();
...
if (position >= segment.End - 1.8f && SegmentIndex < Segments.Length - 1) { SegmentIndex++; ... }
```
- 软推进回收：`cam.transform.position.x - orthoSize*aspect - 4` 作左界 → `enemy.Recycle()`。
- `Retry()` → `Motor.ResetAt(Checkpoint)`、`CameraRig.ResetFollow()`、`SecretRoutes.ResetForRetry()`、`Weapon.RestoreAmmo(checkpointAmmo)`。
- `RecordClear()` → `CampaignProgress.Grade(Deaths, Elapsed, Kills)`、`Unlock(Stage + 2)`、Best/Time 存档。

### 1.2 相机 — `Core/CameraRig.cs` + `Core/SideCameraDirector.cs`

```csharp
if (game.Config.sideScroller) Side = new SideCameraDirector(game, Camera);

// SideCameraDirector.Tick —— 两处必须翻转的横版假设
desired.x = Mathf.Clamp(desired.x, extent, Mathf.Max(extent, game.Level.Length - extent)); // X 按关卡长度夹
desired.y = Mathf.Max(1.9f, desired.y);                                                     // Y 钉死 ≥1.9 ← 致命
desired = player + new Vector2(lookAhead, Mode == ViewMode.Tactical ? 1.6f : 0.55f);
```
- `CameraRig.SortOrder(pos)` 在 `Side != null` 时恒返回 100（用 `transparencySortAxis = Vector3.forward`）。
- 三档视角 `ViewMode`：Tactical / Shoulder / IronSight。
- `CameraRig` 已有 `Shake(float)`、`AddRecoil(float)`、`ResetFollow()`、`GroundPoint()`。

### 1.3 关卡构建 — `Level/SegmentBuilder.cs`

每段固定铺：`Foundation` 地面 + 4 个 scenery + 6 块平台（ApproachStep / OneWayPlatform / MidRoute / UpperRoute / ReturnPlatform / ExitStep），再按 `level.Stage` 加机关：

| Stage | 机关 |
|---|---|
| 0 | `upper.Crumbling = true`、爆燃油桶（`StageInteractable.ConfigureBarrel()`） |
| 1 | 传送带 `ConveyorSpeed ±2`、液压冲柱、电磁门 + `E` 开关 |
| 2 | `upper.Crumbling + Ice`、`surface.Ice` |
| 3/4 | 电梯 `Elevator`、激光栅栏 `kind=2`、全息墙（无碰撞） |
| 4 | 同 1、3 混合 + 空降增援 |

`Platform(name, pos, size, oneWay)` 创建 `BoxCollider2D` +（`oneWay` 时）`PlatformEffector2D`(surfaceArc 165) + `PlatformSurface`。

### 1.4 敌人生成 — `Enemies/EncounterDirector.cs`

```csharp
public const int MaximumEncounterEnemies = 8;
stage = Mathf.Clamp(stage, 0, 4);                 // ← 硬编码上界
int variant = Mathf.Abs(segmentIndex % 4);
float horizontal = Mathf.Lerp(segment.start + 2, segment.End - 2, Mathf.InverseLerp(2, 26, offset));
Vector2 anchor = new Vector2(segment.start + segment.width * 0.65f, 5);
int targetCount = difficulty >= 2 ? 8 : difficulty == 1 ? 7 : 6;
```
- `SideEnemyMotor` 已有 11 种 `AttackPattern`（LaneVolley / Leap / Dash / Laser / Bomb …）、`StartCharge()`、`lockedAim`、`PatrolLeft/Right`。

### 1.5 主题与资产 — `Level/ThemeProfile.cs`

```csharp
public string title, boss, mechanic;
public Color sky, distance, structure, accent, danger;
public Sprite background;
public SideSpriteAnimation bossAnimation, bossForms[3], enemyAnimations[];
public Sprite[] encounterEffects;
public static ThemeProfile[] Defaults() => new[] { 城市废墟, 工厂蜂巢, 寒峰隧道, 未来都市, 最终要塞 };
```
`GameConfig.Validate()`（第 113 行）：`themes == null || themes.Length != 5` → 报错。**新增主题不能动这个数组长度。**

### 1.6 美术与特效底座（高级化的关键可复用资产）

| 资产 | 位置 | 可复用性（高级化直接吃） |
|---|---|---|
| `ProceduralArt.Paint(name, res, func, normalMap?)` | `Core/ProceduralArt.cs:39+` | **支持法线贴图**（第 140 行附近 `normals[...]`）→ 骷髅头/岩层可做凹凸受光 |
| `ProceduralArt` 形状库 | `Square / Disc / Ring / Glow / Diamond / SideMountain / SideSky` | 做粒子、光晕、粉尘、辉光 |
| `SideArt` 高质量程序化图 | `Architecture / Concrete / Cloud / Limb / Boot / Helmet / Scarf / Head / Portrait` | 骷髅头可复用 `Head()` 像素函数做骨感造型 |
| `CombatEffects` 特效池 | `Burst / Muzzle / Debris / Explosion / Ghost / DamageNumber / WarningLine / FloatText / CharacterCast` | 224 槽空闲栈 + 24 飘字，零 GC |
| `game.Config.sideLightPrefab` / `sideSpriteMaterial` | `GameConfig` | **已预留 2D 光照与侧视材质** → 高级化光影直接接 |
| `CameraRig.Shake` / `AddRecoil` | `CameraRig` | 演出级震屏 |
| `LevelManager.SlowProjectiles` | `LevelManager:25` | **子弹时间**（高级化处决/破墙瞬间可用） |

### 1.7 玩家机动 — `Player/SidePlayerMotor.cs`

已有：土狼时间、跳跃缓冲、可变跳高、二段跳、`DropThrough()`、闪避无敌帧、冰面/传送带/电梯。
死亡边界：`if (transform.position.y < -7) player.Health.TakeDamage(10000, Team.Enemy);`

### 1.8 战役 / 存档 / UI

- `GameManager.NextStage()`：`if (SelectedStage >= 4) { ReturnToMenu(); return; }`
- `LevelManager.RecordClear()`：`CampaignProgress.Unlock(Stage + 2)`
- `HUDController`：`stageButtons[i].interactable = i < CampaignProgress.Unlocked`

### 1.9 关键约束汇总（必须全部处理）

| # | 约束 | 位置 | 处理 |
|---|---|---|---|
| C1 | `themes.Length != 5` 校验 | `GameConfig.cs:113` | **不动** `themes`，新增 `ascentThemes[3]` 单独校验 |
| C2 | `Mathf.Clamp(stage, 0, 4)` | `EncounterDirector.cs:38` | `ThemeIndexFor(stage, o)` + Ascent 分支 |
| C3 | `Mathf.Clamp(SelectedStage, 0, 4)` | `BossSignatureVfx:44`、`SideBossController:111`、`CombatEffects:110`、`SolidWallModule:26` | 同上替换 |
| C4 | `SelectedStage >= 4` 结束战役 | `GameManager.cs:270` | 改为 8 关（5 横 + 3 纵） |
| C5 | X 轴推进 | `LevelManager.Update` | Ascent 分支用 Y |
| C6 | `desired.y = Max(1.9f, …)` | `SideCameraDirector.cs:79` | 新增 `AscentCameraDirector` |
| C7 | `Unlock(Stage + 2)` | `LevelManager.cs` | Ascent 下解锁下一纵关 |
| C8 | `stageButtons` 数量 | `HUDController.cs:292` | 扩到 8 格 |

---

<a id="2"></a>
## 2. 总体架构方案

### 2.1 两条路线

| 维度 | **路线 A（采用）** 新增 `ascentThemes[3]` | 路线 B 复用 5 主题做"逆方向" |
|---|---|---|
| 主题辨识度 | 3 套全新配色/机关/Boss 名，特色鲜明 | 与正篇撞脸 |
| 改动面 | `GameConfig` + `LevelManager` + 8 处 Clamp 替换 | 极小 |
| 资产工作量 | 3 套主题配置（颜色即可，程序化美术自动套用） | 0 |
| 风险 | 低（不动 `themes` 校验） | 中 |
| **结论** | **采用** | 备选（时间极紧时用） |

### 2.2 `LevelOrientation`（新增 `Level/LevelOrientation.cs`）

```csharp
namespace MoriEchoes
{
    public enum LevelOrientation { Horizontal = 0, Ascent = 1 }

    public static class LevelOrientationExtensions
    {
        // 推进轴：Horizontal 取 x；Ascent 取 +y（向上为正进度）
        public static float Progress(this LevelOrientation o, UnityEngine.Vector2 p)
            => o == LevelOrientation.Ascent ? p.y : p.x;
    }
}
```

`LevelManager` 新增：
```csharp
public LevelOrientation Orientation { get; private set; } = LevelOrientation.Horizontal;
public int AscentIndex { get; private set; }      // ascentThemes 下标 0–2
public float AscentWidth { get; private set; } = 20f;   // 竖井宽度
public float Height => Segments[Segments.Length - 1].End;
```

### 2.3 主题解析

```csharp
public int ThemeIndexFor(int stage, LevelOrientation o)
    => o == LevelOrientation.Ascent
        ? Mathf.Clamp(AscentIndex, 0, game.Config.ascentThemes.Length - 1)
        : Mathf.Clamp(stage, 0, game.Config.themes.Length - 1);

// Initialize：
Theme = Orientation == LevelOrientation.Ascent
    ? game.Config.ascentThemes[AscentIndex] : game.Config.themes[Stage];
```

`GameConfig` 新增（`themes` 保持 5）：
```csharp
[Header("Ascent（从下向上推进）主题 — 3 关")]
public ThemeProfile[] ascentThemes = ThemeProfile.AscentDefaults();
// Validate() 追加：ascentThemes == null || ascentThemes.Length != 3 → 报错
```

### 2.4 需替换的 `Clamp(…, 0, 4)` 全清单

| 文件 | 行 | 改为 |
|---|---|---|
| `Enemies/EncounterDirector.cs` | 38 | `ThemeIndexFor` + Ascent 分支 |
| `Enemies/BossSignatureVfx.cs` | 44 | `game.Level.ThemeIndexFor(game.SelectedStage, game.Level.Orientation)` |
| `Enemies/SideBossController.cs` | 111 | 同上 |
| `Core/CombatEffects.cs` | 110 | 按 Orientation 选数组再比长度 |
| `Level/SolidWallModule.cs` | 26 | `ThemeIndexFor` |
| `Core/SideCharacterVisual.cs` | 85–96 | Ascent 下走 `ascentThemes` 分支 |
| `Level/SceneFeedback.cs` | 25/32 | `ThemeIndexFor` + Orientation |
| `Weapons/CombatPowerup.cs` / `WeaponCapsuleDirector.cs` | 69/81 | 追加 `|| Orientation != orientation` |

---

<a id="3"></a>
## 3. 竖直推进系统

### 3.1 `SegmentDefinition` 语义重定义

| 字段 | Horizontal | **Ascent** |
|---|---|---|
| `start` | 段左端 X | 段**底部 Y**（0, 30, 60 …） |
| `width` | X 跨度（34/38） | **Y 跨度（正值 26 ~ 34）** |
| `End` | `start + width` | `start + width` → 天然指向更高 Y |

> **技巧**：Ascent 下 `width` 传**正值**，`End = start + width` 自动向上，所有 `Lerp(start, End, …)` 逻辑无需改动即可复用（与 Descent 用负值相反，更直观）。

### 3.2 `Initialize` Ascent 分支

```csharp
for (int index = 0; index < Segments.Length; index++)
{
    bool boss = index == combatSegments;
    if (Orientation == LevelOrientation.Ascent)
        Segments[index] = new SegmentDefinition {
            start = index * 30f, width = boss ? 34f : 30f,
            layout = random.Next(3), boss = boss };
    else
        Segments[index] = new SegmentDefinition {
            start = index * 34, width = boss ? 38 : 34,
            layout = random.Next(3), boss = boss };
}
Checkpoint = Orientation == LevelOrientation.Ascent ? new Vector2(0, 2) : new Vector2(3, 1);
```
- 几何：`Orientation == Ascent` 用 **`AscentBuilder`**（§10）替代 `SegmentBuilder.Build`。
- 门：Ascent 下用**水平横梁**（同 `SolidWallModule`，`Open()` 常开作进度标记），位置 `(0, Segments[i].End)`，尺寸 `(AscentWidth, 0.85)`。

### 3.3 `Update` 核心改写

```csharp
private void Update()
{
    if (!game.IsPlaying || game.Player == null) return;
    SegmentDefinition segment = Segments[SegmentIndex];
    bool ascent = Orientation == LevelOrientation.Ascent;
    float progress = ascent ?  game.Player.transform.position.y
                            :  game.Player.transform.position.x;
    float begin = segment.start;   // Ascent/Horizontal 同语义
    float end   = segment.End;

    if (!EncounterStarted && progress >= begin + 5) BeginEncounter();
    if (!EncounterStarted || segment.boss) return;
    GateLocked = false;

    Camera cam = game.CameraRig.Camera;
    float lowerBound = cam.transform.position.y - cam.orthographicSize - 4f;  // Ascent：下方越界即回收
    for (int i = encounter.Count - 1; i >= 0; i--)
    {
        EnemyAI enemy = encounter[i];
        if (enemy == null || enemy.Health.IsDead) { encounter.RemoveAt(i); continue; }
        bool escaped = ascent ? enemy.transform.position.y < lowerBound        // 被甩在下方
                              : enemy.transform.position.x < (cam.transform.position.x - cam.orthographicSize * cam.aspect - 4f);
        if (escaped) { enemy.Recycle(); encounter.RemoveAt(i); }
    }

    if (progress >= end - 1.8f && SegmentIndex < Segments.Length - 1)
    {
        SegmentIndex++; CheckpointSegment = SegmentIndex;
        Checkpoint = ascent ? new Vector2(game.Player.transform.position.x, Segments[SegmentIndex].start + 2f)
                            : new Vector2(Segments[SegmentIndex].start + 2, 1);
        checkpointFlag.position = Checkpoint + Vector2.down * 0.3f;
        checkpointHealth = Mathf.Max(game.Player.Health.Current, game.Player.Health.Maximum * 0.5f);
        checkpointAmmo = game.Player.Weapon.CaptureAmmo();
        game.Player.Health.Restore(12);
        EncounterStarted = false;
        game.Hud.Announce(ascent ? "攀升 / 第 " + (SegmentIndex + 1) + " 层"
                                  : "推进 / 第 " + (SegmentIndex + 1) + " 区", 2);
    }
}
```

### 3.4 Boss 段与检查点

- Boss 在**最顶层**：`Spawn(EnemyKind.Boss, new Vector2(0, Segments[last].End + 6))`。
- `Retry()`：额外调 `SkullFillerDirector.RestoreForCheckpoint()` 复原该段骷髅墙（§5）。
- `Objective`：Ascent 返回 `"向上攀升 / 清除封路骷髅"`。
- **死亡边界翻转**（§9）：Ascent 下玩家**跌出当前段底部一定距离**即死（坠落深渊），而非冲出顶部。

---

<a id="4"></a>
## 4. 相机系统

### 4.1 现状问题

1. `desired.y = Mathf.Max(1.9f, …)` → 玩家攀升到 Y > 1.9 后相机**不再跟随**，直接飞出屏幕。
2. `desired.x = Clamp(…, Level.Length)` → Ascent 无"X 长度"概念，会把相机钉死在 0。

### 4.2 新增 `Core/AscentCameraDirector.cs`（约 90 行）

```csharp
public sealed class AscentCameraDirector
{
    public ViewMode Mode { get; private set; }
    public bool IsAds { get; private set; }
    public float AdsAmount { get; private set; }
    private readonly GameManager game; private readonly Camera camera;
    private Vector2 focus, focusVelocity;
    private float lookUp, lookUpVelocity;   // 向上预读（替代 lookAhead）
    private bool snap;

    public void Tick(float recoil, float shake)
    {
        float delta = Mathf.Clamp(Time.unscaledDeltaTime, 0.0001f, 0.05f);
        bool playing = game.IsPlaying && game.Player != null;
        if (playing && game.ControlsReady && game.Input.ChangeView) Mode = (ViewMode)(((int)Mode + 1) % 3);

        IsAds = playing && (Mode == ViewMode.IronSight || game.ControlsReady && game.Input.AimDownSights);
        AdsAmount = Mathf.Lerp(AdsAmount, IsAds ? 1 : 0, 1 - Mathf.Exp(-game.Config.camZoomSpeed * Time.unscaledDeltaTime));
        float normal = Mode == ViewMode.Tactical ? game.Config.viewTacticalSize : game.Config.viewShoulderSize;
        camera.orthographicSize = Mathf.Lerp(camera.orthographicSize,
            Mathf.Lerp(normal, game.Config.viewIronSightSize, AdsAmount), 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));

        Vector2 desired = new Vector2(0, 2);
        if (game.Player != null)
        {
            Vector2 player = game.Player.transform.position;
            float lead = Mode == ViewMode.Tactical ? 2.2f : 0.9f;
            float rise = Mathf.Clamp(game.Player.Motor.Velocity.y * 0.12f, 0, 2.2f);  // 需暴露 Velocity
            lookUp = Mathf.SmoothDamp(lookUp, lead + rise, ref lookUpVelocity, 0.28f, Mathf.Infinity, delta);

            float halfWidth = game.Level != null ? game.Level.AscentWidth * 0.5f : 10f;
            float extentX = camera.orthographicSize * camera.aspect;
            float clampedX = Mathf.Clamp(player.x, -halfWidth + extentX, halfWidth - extentX);
            if (-halfWidth + extentX > halfWidth - extentX) clampedX = 0;
            desired = new Vector2(clampedX, player.y + lookUp);   // ★ Y 无上限：允许持续攀升

            if (Mode == ViewMode.Tactical && game.Level?.Boss != null && !game.Level.Boss.Health.IsDead)
            {
                Vector2 boss = game.Level.Boss.transform.position;
                desired.x = Mathf.Lerp(player.x, boss.x, 0.42f);
                desired.y = Mathf.Lerp(player.y, boss.y, 0.30f) + 1.2f;   // 仰视 Boss
            }
            if (!snap)
            {
                float zoneX = IsAds ? 0.18f : Mode == ViewMode.Tactical ? 0.65f : 0.30f;
                desired.x = focus.x + Mathf.Sign(desired.x - focus.x) * Mathf.Max(0, Mathf.Abs(desired.x - focus.x) - zoneX);
                desired.y = focus.y + Mathf.Sign(desired.y - focus.y) * Mathf.Max(0, Mathf.Abs(desired.y - focus.y) - 0.7f);
            }
        }
        focus = snap ? desired : Vector2.SmoothDamp(focus, desired, ref focusVelocity,
                    Mathf.Max(0.08f, 1.6f / game.Config.camFollow), Mathf.Infinity, delta);
        snap = false;
        Vector2 jitter = playing ? Random.insideUnitCircle * shake : Vector2.zero;
        camera.transform.SetPositionAndRotation(new Vector3(focus.x + jitter.x, focus.y + recoil + jitter.y, -24), Quaternion.identity);
        camera.transparencySortAxis = Vector3.forward;
    }
}
```

### 4.3 接入 `CameraRig`

```csharp
public AscentCameraDirector Ascent { get; private set; }

// Initialize（注意：CameraRig.Initialize 早于 LevelManager.Initialize）
if (game.Config.sideScroller)
{
    if (game.Level != null && game.Level.Orientation == LevelOrientation.Ascent)
        Ascent = new AscentCameraDirector(game, Camera);
    else Side = new SideCameraDirector(game, Camera);
}

// 新增：供 LevelManager.Initialize 末尾调用
public void SetupAscentIfNeeded()
{
    if (game.Config.sideScroller && Ascent == null && Side == null
        && game.Level != null && game.Level.Orientation == LevelOrientation.Ascent)
    { Ascent = new AscentCameraDirector(game, Camera); Ascent.Reset(); }
}

// Update
if (Ascent != null) { /*recoil/shake衰减*/ Ascent.Tick(recoil, shake); IsAds = Ascent.IsAds; AdsAmount = Ascent.AdsAmount; return; }
```

### 4.4 背景与高度视差

`SidePresentation` 现有天地/远景层。Ascent 下：
- 背景改为**竖直视差**（岩壁/熔岩/能量脉络随高度变化），复用 `theme.sky/distance` 双色渐变。
- **越高背景越向 `theme.danger` / `theme.sky` 偏移** → 营造"越接近天顶越危险/越明亮"（高级化氛围关键，见 §8.2）。

---

<a id="5"></a>
## 5. 招牌机制：骷髅头填充墙 `SkullFiller`（★ 核心）

### 5.1 体验曲线（单道墙）

1. **看见** — 头顶通道被整片骷髅头堵死，视觉上明确"此路不通（向上）"。
2. **凿穿** — 向上射击，骷髅头逐块碎裂，每碎一块露一个洞。
3. **攀升** — 洞口足够大时玩家跳/冲刺穿过洞（失重上涌感）。
4. **奖励** — 打穿整墙触发连击暴涨 + 概率掉胶囊。
5. **威胁** — 凿墙时头顶持续有敌**下扑** → "打墙 vs 打人"的资源取舍。

### 5.2 组件设计

| 组成 | 说明 |
|---|---|
| 父物体 `SkullFiller` | 持 `SkullBlock[]` + `remaining`；`Breached` 时坍塌特效 + 掉落 |
| 子格 `SkullBlock` | `BoxCollider2D`（阻挡）+ `SpriteRenderer` + `HitPoints`；HP 归零 → 关碰撞/隐藏/碎裂 |
| 顶部横梁 | 用 `SegmentBuilder.Platform(...)` 建的**不可破坏**梁（视觉锚点） |
| 贴图 | 复用 `ProceduralArt.Paint` 生成骷髅头（可借 `SideArt.Head()` 像素函数做骨感）+ **法线贴图**受光（§8.3） |

### 5.3 代码骨架

```csharp
public sealed class SkullFiller : MonoBehaviour
{
    public bool Breached => remaining <= 0;
    private GameManager game; private LevelManager level;
    private SkullBlock[] blocks; private int remaining;

    public void Initialize(GameManager owner, LevelManager campaign, Vector2 center,
                           float width, int columns, int rows, int hpPerBlock)
    {
        game = owner; level = campaign;
        blocks = new SkullBlock[columns * rows]; remaining = blocks.Length;
        float cellW = width / columns, cellH = 0.9f;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                var go = new GameObject("SkullBlock_" + r + "_" + c);
                go.transform.SetParent(transform, false);
                // 墙在玩家头顶：中心向上铺 rows 层
                go.transform.position = center + new Vector2(-width * 0.5f + cellW * (c + 0.5f), cellH * (r + 0.5f));
                var solid = go.AddComponent<BoxCollider2D>();
                solid.size = new Vector2(cellW * 0.96f, cellH * 0.96f);
                var block = go.AddComponent<SkullBlock>();
                block.Initialize(game, solid, hpPerBlock, this);
                blocks[r * columns + c] = block;
            }
    }

    public void OnBlockDestroyed(SkullBlock block)
    {
        remaining--;
        game.Effects.Burst(block.transform.position, level.Theme.accent, 6);
        game.RegisterSkullBreak();                    // 新增：喂连击但不计入 Kills
        if (remaining <= 0) OnBreached();
    }

    private void OnBreached()
    {
        game.Effects.Explosion(transform.position, level.Theme.danger, 1.6f);
        game.CameraRig.Shake(0.35f);
        game.Hud.Announce("通道打开 / 继续攀升", 1.5f);
        level.GetComponent<WeaponCapsuleDirector>()?.DropCapsule(transform.position, 0.55f);
    }

    public void RestoreForCheckpoint()
    { remaining = blocks.Length; foreach (SkullBlock b in blocks) b.Restore(); }
}

public sealed class SkullBlock : MonoBehaviour
{
    private GameManager game; private BoxCollider2D solid; private SpriteRenderer art;
    private SkullFiller owner; private int hp, maxHp;
    public bool Destroyed { get; private set; }

    public bool TakeHit(int damage)
    {
        if (Destroyed) return false;
        hp -= damage;
        if (hp > 0) { art.color = Color.Lerp(Color.white, new Color(1, 0.6f, 0.5f), 1 - (float)hp / maxHp); return false; }
        Destroyed = true; solid.enabled = false; art.enabled = false;
        owner.OnBlockDestroyed(this); return true;
    }
    public void Restore() { Destroyed = false; hp = maxHp; solid.enabled = true; art.enabled = true; art.color = Color.white; }
}
```

**子弹接线**：`Projectile.cs` 命中回调加 `hit.collider.GetComponent<SkullBlock>()` 分支 → `TakeHit(damage)` 并消耗弹丸。

### 5.4 三关骷髅墙参数

| 关卡 | 列×层 | 每格 HP | 每段墙数 | 特色（高级化变体） |
|---|---|---|---|---|
| **A1 残破钟楼** | 5×2（10） | 2 | 2（首道 4×1 教学） | 最纯凿墙教学；掉胶囊 0.6 |
| **A2 熔火竖井** | 6×2（12） | 3 | 1–2 | **熔岩核**：打碎喷浆 1.2s 伤玩家 → 逼玩家选凿哪块 |
| **A3 虚空尖塔** | 6×3（18） | 3 | 2 | **相位格**：半透明、周期消失/重现 → 时机凿墙 |

### 5.5 性能

- 同屏可见仅 1–2 道墙（≤ 36 格），其余 `SetActive(false)`（按 `SegmentIndex ± 1` 懒激活）。
- 整墙共用一张贴图 + 一个材质 → 单道墙 ≈ 1–2 Draw Call。
- 碎片走对象池（参照 `CombatEffects` 空闲栈），禁止 `Instantiate`。
- 骷髅格用 `BoxCollider2D`（静态、无 Rigidbody）→ 不进物理模拟。

---

<a id="6"></a>
## 6. 敌人：从天而降

### 6.1 设计原则

Ascent 的敌人**不是"从右边来"，而是"从上面来"**，三重新鲜感：
1. **来源翻转** — 从玩家头顶坠下，无法靠"向右跑"甩开（玩家正在向上爬）。
2. **上下夹击** — 头顶俯冲编队不断下扑 + 底部"上升深渊"压迫 → 立体战场。
3. **凿墙压力** — 凿墙时头顶持续下扑敌 → 资源取舍。

### 6.2 新原型 `SkyDiver`（天降兵）

| 属性 | 行为 |
|---|---|
| 出场 | 玩家**上方 6–9 单位**生成，不可见，地面/空中留"阴影预警"（`CombatEffects.WarningLine`/自定义圈） |
| 下扑 | 玩家 5 单位内或 1.2s 后下扑：复用 `SideEnemyMotor.StartCharge(new Vector2(0, -8.5f), 0.65f)`（现有 `Leap` 参数）向下俯冲 |
| 攻击 | 落地走 `Chaser` 行为（`AttackPattern.LaneVolley` 三连射） |
| 死亡 | 碎裂 + 掉胶囊 0.25 |
| 弱点 | 下扑蓄力 0.65s 内**受伤 ×2** → 奖励"预判射击" |

`SideEnemyMotor` 新增 `AttackPattern.Dive`：
```csharp
case AttackPattern.Dive:
    if (!diving) { /* 空中悬停 + 阴影预警 */ }
    else StartCharge(new Vector2(0, -8.5f), 0.65f);   // 复用现有 StartCharge
    break;
```

### 6.3 `EncounterDirector` Ascent 分支

```csharp
public static EncounterSpawn[] BuildAscent(int ascentIndex, SegmentDefinition segment, int segmentIndex, int difficulty = 0)
{
    float HeightOf(float offset) => segment.start + segment.width * Mathf.InverseLerp(2, 26, offset);
    var roster = new List<EncounterSpawn>(MaximumEncounterEnemies);
    int variant = Mathf.Abs(segmentIndex % 4);
    // 主力来自 offset 14–24（段中上部 = 玩家上方）
    AddAscent(roster, segment, AscentRoster(ascentIndex)[0], 14, "天降突袭");
    AddAscent(roster, segment, AscentRoster(ascentIndex)[1], 22, "上层包抄");
    if (variant % 2 == 0) AddAscent(roster, segment, AscentRoster(ascentIndex)[2], 24, "底部攀袭");
    ...
}
// AddAscent：pos = new Vector2(RandomXInShaft(), HeightOf(offset) + 8); anchor = (0, start + width*0.65f)
```

### 6.4 三关敌人名册

| 关卡 | 原型 | 行为要点 |
|---|---|---|
| **A1 残破钟楼** | `BoneDiver` 骷髅天降兵 | 下扑 + 三连射；HP 低量大 |
| | `CryptWing` 墓穴炮蜂 | 从上方飞入，绕顶投弹 |
| | `Crawler` 自爆虫 | 贴平台滚来接触爆炸（配合软推进最爽） |
| **A2 熔火竖井** | `MagmaDiver` 熔岩天降重甲 | HP 高、下扑慢 1.2s（易被打断） |
| | `LavaTurret` 喷浆炮塔 | 固定壁面扇形喷浆，须绕开 |
| | `AshBomber` 编队轰炸 | 上方梯次投弹（复用 `FactoryBomber` 的 `Wing`） |
| **A3 虚空尖塔** | `PhaseDiver` 相位天降 | 半透明、穿墙（忽略 `SkullBlock`） |
| | `VoidLaser` 激光哨 | 竖直激光栅栏（复用 `CityLaser` + `StageHazard kind=2`） |
| | `Summoner` 召唤师 | 每 4s 从上方拉 1 只小怪，优先击杀目标 |

### 6.5 底部压迫机关：上升深渊（Ascent 灵魂张力）

| 关卡 | 机关 | 效果 |
|---|---|---|
| A2 熔火竖井 | **上升岩浆**（底部 `StageHazard` 持续上移） | 玩家必须在被追上**之前**凿墙攀升 → 全关最强压迫感（对标 Descent 的"岩浆追你下潜"） |
| A3 虚空尖塔 | **上升虚空雾**（低重力+缓慢上移伤害场） | 同上，且雾中视野受限（高级化 §8.2 深度色偏强化） |

---

<a id="7"></a>
## 7. 三关详细设计

### 7.1 A1「残破钟楼」（教学 · 骷髅墙密度最高）

```
title = "残破钟楼"   boss = "镇塔机偶"   mechanic = "骷髅填充墙 / 骷髅天降兵 / 落梁"
sky = #2A2418  distance = #3D3423  structure = #4A4030  accent = #E8DCC0  danger = #C4593A
```

| 段 | 内容 | 骷髅墙 | 敌人 |
|---|---|---|---|
| 0 | 教学：单层薄墙（4×1，HP 2）+ 顶部梁指示 | 1（薄） | 2 BoneDiver |
| 1 | 标准墙（5×2）+ 左右可选通道（一条藏芯片） | 1 | 3 Diver + 1 CryptWing |
| 2 | 双墙连击 + 间歇落梁（顶部砸下） | 2 | 2 Diver + 2 Crawler |
| 3 | 墙 + 崩塌平台（`Crumbling`） | 1 | 3 Diver + 2 CryptWing |
| 4 | 宽井（6×2）+ 自爆虫混战 | 1 | 4 Crawler + 2 Diver |
| 5 | 垂直长井（连续 3 道薄墙） | 3（薄） | 2 Diver/道 |
| 6 | 缓冲段 + 检查点 + 补给 | 0 | 1 CryptWing 编队 |
| 7 | **Boss：镇塔机偶** | — | Boss |

**Boss「镇塔机偶」**（复用 `SideBossController` 30%/60% 切阶段）
| 阶段 | HP | 攻击 | 预警 | 应对 |
|---|---|---|---|---|
| P1 | 100–60% | 双臂下扫（左右各一次） | `WarningLine` 1.2s | 跳平台或闪避 |
| P2 | 60–30% | 从**顶部**震波（三段）+ 召唤 2 天降兵 | 地面裂纹 0.8s | 保持移动躲震波 |
| P3 | 30–0% | 底部核心暴露（`Arms` 弱点），须到其**正下方**上射核心 | 核心发光 | 站平台仰射核心 |

---

### 7.2 A2「熔火竖井」（机关关 · 上升岩浆）

```
title = "熔火竖井"   boss = "熔核巨钻"   mechanic = "上升岩浆 / 喷浆柱 / 熔岩核骷髅墙"
sky = #1E0D0A  distance = #4A1A10  structure = #3A2018  accent = #FF9C39  danger = #FF3B1F
```

**机关**：升降台（`PlatformSurface.Elevator` 竖直上移）、喷浆柱（复用 `StageHazard kind=1` 改橙红）、熔岩核骷髅墙、**上升岩浆**（§6.5）。

| 段 | 内容 | 骷髅墙 | 敌人 |
|---|---|---|---|
| 0 | 升降台教学 + 薄墙 | 1（5×2，含 1 熔岩核） | 2 MagmaDiver |
| 1 | 2 座喷浆炮塔封锁中段 | 1 | 2 Turret + 2 Diver |
| 2 | 连续升降台跳跃（3 台） | 1 | 3 AshBomber 梯次轰炸 |
| 3 | 熔岩核墙（3 核）+ **上升岩浆（限时攀升）** ★ | 1（6×2） | 2 Diver |
| 4 | 宽井 + 双炮塔 + 编队轰炸 | 1 | 2 Turret + 3 Bomber |
| 5 | 崩塌 + 岩浆交替 | 2 | 3 Diver + 2 Bomber |
| 6 | 缓冲 + 检查点 + 补给 | 0 | 1 Bomber 编队 |
| 7 | **Boss：熔核巨钻** | — | Boss |

★ 段 3「上升岩浆」：底部 `StageHazard` 持续上移，玩家必须在被追上前凿穿墙攀升 → **全关最强压迫感**。

**Boss「熔核巨钻」**
| 阶段 | 行为 | 应对 |
|---|---|---|
| P1 | 悬于顶部只露钻头；周期**下钻**（无敌）→**上浮**（核心暴露 2.5s） | 抓上浮窗口上射核心 |
| P2 | 钻头横扫 + 喷浆幕（三段扇形） | 站平台边缘躲扇形空隙 |
| P3 | 召唤天降小队（每 6s 3 只）+ 核心常露但移动加快 | 先清小怪再打核心 |

---

### 7.3 A3「虚空尖塔」（终局 · 相位地板 + 双形态 Boss）

```
title = "虚空尖塔"   boss = "终末回响·天顶"   mechanic = "相位地板 / 骷髅相位墙 / 全息围栏 / 上升虚空雾"
sky = #0E0A1E  distance = #241540  structure = #1A1330  accent = #B07CFF  danger = #FF4FD8
```

**机关**：相位地板（`PlatformSurface` 加 `Phase` 字段，周期开关 `BoxCollider2D.enabled`，"可穿不可站"）、相位骷髅墙、全息围栏（复用 `Hologram` + `TimedLaserGrid`）、上升虚空雾（§6.5）。

| 段 | 内容 | 骷髅墙 | 敌人 |
|---|---|---|---|
| 0 | 相位地板教学 + 薄墙 | 1（5×2） | 2 PhaseDiver |
| 1 | 激光哨封锁 + 相位格墙 | 1（含 3 相位格） | 2 VoidLaser + 2 Diver |
| 2 | 全息迷宫 + 召唤师登场 | 1 | 1 Summoner + 3 Diver |
| 3 | 相位地板长井（连续踩空风险）★ | 2 | 3 PhaseDiver |
| 4 | 三向激光 + 编队 | 1（6×3） | 2 VoidLaser + 2 Bomber |
| 5 | 全机关混合（相位+激光+崩塌） | 2 | Summoner + 4 Diver |
| 6 | 缓冲 + 检查点 + 最终补给 | 0 | 1 编队 |
| 7 | **Boss：终末回响·天顶** | — | Boss |

**Boss「终末回响·天顶」双形态**
| 形态 | HP | 行为 | 应对 |
|---|---|---|---|
| 天顶形态 | 100–50% | 全屏弹幕 + 激光栅栏（复用 Stage4 双臂弱点） | 打双臂弱点 |
| 深渊形态 | 50–0% | Boss **下沉**至中段只露上半身；玩家须在其下方上射核心，脚下持续落梁 | 仰射核心；躲落梁 |

---

<a id="8"></a>
## 8. ★ 高级化：制作级表现（让这三关"看起来就贵"）

> 现有工程是**程序化美术原型**，本节给出把它抬到"制作级"的具体手段，**全部基于现有可复用资产**（§1.6），不要求先换美术资源。

### 8.1 美术方向（Art Direction）

| 手段 | 具体做法 | 复用 |
|---|---|---|
| **剪影优先** | 骷髅头、天降敌、Boss 都先保证"一眼能认出的剪影"：骷髅头用 `SideArt.Head()` 像素函数改骨感（凹眼窝 + 颧骨高光），天降敌做"倒锥钻 + 甲壳"轮廓，Boss 做"巨大不对称"轮廓（一侧钻头/一侧爪） | `SideArt.Head/Portrait/ArmorPixel` |
| **配色克制（3 色原则）** | 每关只主导 3 色：`structure`(大面积) + `accent`(主体高光) + `danger`(威胁/特效)。**避免五色齐飞** | `ThemeProfile` 已有 5 色，按此规则分配 |
| **高度色偏** | 越往上背景越向 `danger`/`sky` 偏移、饱和度变化 → 玩家"感觉到自己接近天顶" | §4.4 高度视差 |
| **材质分区** | 岩石/金属/骨/能量各用不同 `Paint` 噪声频率（`Mathf.Sin(horizontal*487...)` 已有先例）→ 表面辨识度 | `ProceduralArt.Paint`、`SideArt.Concrete/Architecture` |
| **边缘光 Rim** | 角色/Boss/骷髅头边缘加一圈 `accent` 色描边（1–2px），把主体从背景里"拔"出来 | 复用 `SideArt` 的 `Outline` 常量 |

### 8.2 光影（Lighting）— 已有 `sideLightPrefab` 可接

| 手段 | 做法 | 效果 |
|---|---|---|
| **主光源随高度** | 一盏 `Light2D`（`sideLightPrefab`）跟随玩家，颜色 = `theme.accent`，强度随高度略变 | 玩家永远在"光里"，周围是黑暗/虚空 → 探险感 |
| **危险区自发光** | 岩浆/激光/相位格用**自发光材质**（不受光、纯 emissive）+ 微弱 `Light2D` 点光 | 威胁一眼可见，且不依赖贴图精度 |
| **骷髅头受光** | 用 `ProceduralArt.Paint` 的**法线贴图**选项生成骷髅头法线 → 主光源扫过时眼窝/颧骨有明暗 | **这是"高级感"最廉价也最有效的一招**（凹凸比颜色更能骗过眼睛） |
| **破洞透光** | 骷髅墙被凿穿的洞，在洞后放一盏 `Light2D`（暖色）→ "上一层的光透下来" | 指示前进方向 + 氛围 |
| **God Ray / 体积光** | 竖井顶部投一道 `Glow` 拉伸的半透明光柱（`ProceduralArt.Glow`）缓慢摆动 | 尘埃感、纵深、暗示"出口在上方" |

### 8.3 特效（VFX）— 复用 `CombatEffects` 224 槽池

| 时刻 | 特效 | 高级化要点 |
|---|---|---|
| 骷髅块受击 | `Burst` 小碎裂 + 骷髅头**裂纹递进**（HP 越低越裂） | 裂纹用 3 帧贴图切换，别只变色 |
| 骷髅块碎裂 | `Debris` 骨渣**带物理上抛 + 旋转**，落地后淡出 | 有重量感才像"砸碎"而非"消失" |
| 整墙打穿 | `Explosion` + 大范围粉尘 `Glow` 扩散 + **0.25s 慢动作**（`LevelManager.SlowProjectiles`）+ `Shake(0.35)` | **这是全关最高光的瞬间，必须"顿一下"** |
| 天降敌下扑 | 阴影预警 → 俯冲残影 → 敌跃出（三拍，0.65s 蓄力期给预警） | 三拍演出让"突然砸下来"变成"可反应" |
| 攀升穿越破洞 | 轻微失重 + `Ghost` 残影 + 相机上拉 | 强化"钻进新一层"的腾跃感 |
| 环境氛围 | 每层持续飘落/上浮颗粒（A1 尘埃 / A2 火星 / A3 能量粒子），走对象池 | **氛围粒子是"贵"的最低成本来源** |

### 8.4 镜头演出（Cinematics）

| 时刻 | 演出 |
|---|---|
| **关卡开场** | 镜头从竖井底部**缓慢上摇** 1.5s（展示高塔 + 顶部光柱），然后交给玩家（复用 `SetCinematicLocked(true)`） |
| **每层达成** | 短暂拉远 0.3s 再回位 + 屏幕边缘 `accent` 微光 → "又上一层"的仪式感 |
| **整墙打穿** | 0.25s 慢动作（`SlowProjectiles`）+ 轻微推近 + `Shake(0.35)` |
| **Boss 登场** | 镜头**上仰**到 Boss 处停留 1.2s + Boss 现身动画 + `WarningLine` 预警，再交还控制权 |
| **Boss 处决** | 复用 `SlowProjectiles` + 推近 + 全屏 `accent` 闪白 0.15s |

### 8.5 音效设计（Audio）— `AudioManager` / `SoundKind`

| 类别 | 设计 |
|---|---|
| **高度氛围** | 每层一种环境底噪（A1 空穴回响 / A2 低频岩浆轰鸣 / A3 电磁嗡鸣），**随高度升高而改变音色**（底部闷、顶部开阔） |
| **高度滤波** | 越接近天顶把 BGM/音效做**高通/低通切换**（高处更明亮开阔）→ 物理上"接近地表"的听感（低成本高感知） |
| **骷髅碎裂** | 干脆"骨裂"声（高频短促）+ 低频"塌落"垫底 → 脆 + 沉双层 |
| **天降预警** | 下扑前 0.65s 给"呼啸"渐进音（音量随距离）→ 听觉也能反应 |
| **整墙打穿** | 低频轰塌 + 短暂**静音 0.15s** 再进下一层 → 静默对比放大冲击 |
| **Boss** | 三个 Boss 各一条专属动机（机偶金属摩擦 / 巨钻低频钻鸣 / 回响电子和声） |

### 8.6 UI / 反馈质感（Juice）

| 元素 | 高级化 |
|---|---|
| **高度计** | HUD 顶部/侧边一条**竖式高度条**（当前层 / 总层），随攀升填充 + 数字跳动 → 强化"攀升进度"身份感 |
| **凿墙计数** | 打穿时飘金色 `"贯通 ×N"`（`CombatEffects.FloatText` 已有）+ 连击数联动 |
| **HUD 配色** | HUD 描边/高光用 `theme.accent`，三关各自一套 HUD 色调 → 关卡辨识度 |
| **字体层级** | 层数/高度用大号描边字，其余小号 → 信息层级清晰 |
| **命中反馈** | 骷髅块受击给**极短**白闪（0.05s）+ 命中音，让射击"有牙" |

### 8.7 高级化优先级（成本 vs 观感提升）

| 优先级 | 手段 | 成本 | 观感提升 |
|---|---|---|---|
| **P0** | 骷髅头/岩层**法线贴图**受光 + 主光源随玩家 | 低 | ★★★★★（凹凸感直接把"扁平"变"立体"） |
| **P0** | 每层**氛围粒子**（尘埃/火星/能量） | 低 | ★★★★★ |
| **P0** | 整墙打穿的**慢动作 + 震屏 + 粉尘** | 低 | ★★★★★ |
| **P1** | 高度色偏 + 高度滤波 | 低 | ★★★★ |
| **P1** | 剪影重做（骷髅头/天降敌/Boss） | 中 | ★★★★ |
| **P1** | 破洞透光 + God Ray | 低 | ★★★ |
| **P2** | 开场/每层/Boss 三段镜头演出 | 中 | ★★★ |
| **P2** | 高度计 HUD + 凿墙飘字 | 低 | ★★★ |

> **一句话**：高级感 ≈ **受光（法线）+ 氛围粒子 + 关键瞬间的慢动作/震屏 + 高度带来的视听渐变**。这四件用现有 `ProceduralArt`/`CombatEffects`/`Light2D`/`SlowProjectiles` 就能做，**不必等正式美术资源**。

---

<a id="9"></a>
## 9. 玩家操作与手感

`SidePlayerMotor` 已具备向上所需全部能力，改动极小：

| 能力 | 现状 | Ascent 适配 |
|---|---|---|
| 跳跃/二段跳 | `airJumpUsed` | **主操作**（爬竖井靠连续跳/冲） |
| 闪避无敌 | `dodgeDuration`/`dodgeCooldown` | 穿过骷髅墙破洞、躲天降下扑 |
| 重力/上跃 | `gravityScale` | 直接用 |
| 下穿平台 | `DropThrough()` | 少用（仅回落时）；竖井以"向上跳+冲刺"为主 |
| 死亡边界 | `y < -7` | **翻转**：Ascent 改为"跌出当前段底部（`Segments[SegmentIndex].start - 6`）即死"（坠入深渊） |

**增强（可选）**
1. **蹬墙跳**：靠近竖井壁时按跳 → 沿壁小跳（攀爬手感关键，低成本高爽点）。
2. **快速攀升**：贴墙/连续跳时小幅加速，老手速通路感。
3. **攀升缓冲**：从破洞穿出时 0.3s 轻微腾跃 + `Shake(0.15)`。
4. 需给 `SidePlayerMotor` 暴露 `public Vector2 Velocity => body.linearVelocity;`（供 §4.2 相机预读）。

---

<a id="10"></a>
## 10. 关卡几何构建

新增 `Level/AscentBuilder.cs`（约 150 行）：

```csharp
public void Build(GameManager owner, LevelManager campaign)
{
    // 每段：
    //   1. 左右竖井壁（不可破坏）→ Platform(…, size=(1.5, 30), oneWay=false)
    //   2. 攀升平台 2–4 块（交替左右，oneWay=true 便于下落缓冲；主要供落脚）
    //   3. 骷髅填充墙 SkullFiller（按 §5.4 参数，置于段上部挡路）
    //   4. 主题机关（A1 崩塌/落梁；A2 升降台/喷浆柱/上升岩浆；A3 相位地板/激光）
    //   5. 隐藏芯片（复用 StageInteractable.Chip，每段 ≤1，共 3）
    //   6. 底部压迫机关（A2/A3：上升岩浆/虚空雾）
    // 全局：BottomBoundary（深渊致死）、TopBoundary（Boss 段顶）
}
```
- 竖井宽度建议 **18–22**（比横版窄 → 强化"井"的压迫感）→ `LevelManager.AscentWidth`。
- **建议把 `SegmentBuilder.Platform/Decoration/Hazard` 提升为 `protected static` 或抽 `LevelGeometryHelper`**，供两个 Builder 共用，避免重复代码。

---

<a id="11"></a>
## 11. 战役与 UI 集成

| 项 | 改动 |
|---|---|
| `GameManager.BeginRun` | 增加 `BeginRun(int stage, LevelOrientation o)` |
| `GameManager.NextStage` | 8 关流程：stage 0→4 Horizontal → Ascent 0→2 → `ReturnToMenu()` |
| `CampaignProgress.Unlock` | Horizontal `Unlock(Stage+2)`；Ascent `Unlock(5 + AscentIndex + 1)`（6/7/8） |
| `HUDController` 关卡按钮 | 扩到 8 格；前 5 标"横版 1–5"，后 3 标"攀升 1–3" |
| `Objective` | Ascent：`"向上攀升 / 清除封路骷髅"` |
| 结算 | 复用 `CampaignProgress.Grade(Deaths, Elapsed, Kills)` → S/A/B/C |
| **高度计 HUD** | 新增竖式高度条（§8.6） |

---

<a id="12"></a>
## 12. 性能预算

| 指标 | 目标 | 措施 |
|---|---|---|
| 同屏 `SkullBlock` | ≤ 36 | 按 `SegmentIndex ± 1` 懒激活 |
| 单道墙 Draw Call | ≤ 2 | 共贴图 + 共材质 |
| 破碎碎片 | 0 GC | 对象池（参照 `CombatEffects` 空闲栈） |
| 活跃敌人 | ≤ `Config.maximumEnemies` | 上界 `Recycle()` |
| 物理体 | 与横版相当 | 骷髅格静态 `BoxCollider2D`，无 Rigidbody |
| 氛围粒子 | 单屏 ≤ 120 粒 | 对象池 + 屏幕外自动回收 |
| 纵向 Draw Call | ≤ 横版 1.5× | 背景视差 ≤ 3 层；`Light2D` ≤ 3 盏同屏 |

---

<a id="13"></a>
## 13. 实施计划

| 阶段 | 任务 | 涉及文件 | 依赖 | 优先级 |
|---|---|---|---|---|
| **P0** | `LevelOrientation` + `ascentThemes[3]` + `ThemeProfile.AscentDefaults()` + `ThemeIndexFor` 替换 8 处 Clamp | `LevelOrientation.cs`(新)、`LevelManager.cs`、`GameConfig.cs`、`ThemeProfile.cs` + §2.4 表 | — | **高** |
| **P1** | 竖直推进：Y 化 + 检查点 + 下方越界回收 + `Objective` | `LevelManager.cs` | P0 | **高** |
| **P2** | `AscentCameraDirector` + `CameraRig` 接入 + `Motor.Velocity` | `AscentCameraDirector.cs`(新)、`CameraRig.cs`、`SidePlayerMotor.cs` | P0 | **高** |
| **P3** | `SkullFiller`/`SkullBlock` + 子弹接线 + 连击/掉落 + 懒激活/复原 | `SkullFiller.cs`(新)、`Projectile.cs`、`GameManager.cs`、`WeaponCapsuleDirector.cs` | P1/P2 | **高（招牌）** |
| **P4** | `EncounterDirector.BuildAscent` + `SkyDiver` + `AttackPattern.Dive` + `AscentRoster` | `EncounterDirector.cs`、`EnemyRoster.cs`、`SideEnemyMotor.cs` | P0 | **高** |
| **P5** | `AscentBuilder` 几何 + 三关机关（含上升岩浆/虚空雾）+ 芯片 | `AscentBuilder.cs`(新)、`SegmentBuilder.cs`(抽 Helper)、`PlatformSurface.cs`(Phase/ElevatorDirection) | P1/P3 | **中** |
| **P6** | 三关 Boss + `ThemeIndexFor` 映射 Boss 资产 | `SideBossController.cs`、`BossController.cs`、`BossSignatureVfx.cs` | P0/P5 | **中** |
| **P7** | 战役/UI：8 关流程、`Unlock`、HUD 8 格、高度计 | `GameManager.cs`、`HUDController.cs`、`CampaignProgress.cs` | P0 | **中** |
| **P8** | **高级化 P0**：骷髅头/岩层法线贴图 + 主光源随高度 + 每层氛围粒子 + 打穿慢动作/震屏 | `ProceduralArt.cs`、`AscentBuilder.cs`、`SkullFiller.cs`、光照预制体 | P3/P5 | **高（观感）** |
| **P9** | **高级化 P1/P2**：高度色偏/滤波、剪影重做、破洞透光/God Ray、三段镜头演出 | `SideArt.cs`、`AudioManager`、`AscentCameraDirector.cs`、`HUDController.cs` | P8 | **中** |
| **P10** | 手感增强（蹬墙跳/快速攀升/攀升缓冲）+ 死亡边界翻转 | `SidePlayerMotor.cs` | P1 | **低** |
| **P11** | 性能收尾 + 冒烟测试接入 | §12 | P3/P5/P8 | **中** |

**施工顺序**：P0 → P1 → P2 → P3 → P4 → P5 → P6 → P7 → **P8（高级化）** → P9 → P10 → P11

---

<a id="14"></a>
## 14. 验收与测试

**人工验收**
- [ ] 镜头**持续向上跟随**，玩家不会因 Y 被钉死卡在下方。
- [ ] 每段至少一道骷髅墙；打穿后通道打开、可继续攀升；打穿计入连击、有飘字与碎片。
- [ ] 敌人**从上方/侧上方坠下**，且存在"上下夹击"（顶部有俯冲编队 + 底部有上升压迫）。
- [ ] 死亡/检查点重试后进度不回退、骷髅墙按段复原、弹药按 `checkpointAmmo` 恢复。
- [ ] 三关 Boss 阶段/形态切换正常，弱点可打、预警可见。
- [ ] Ascent 通关后正确解锁；正篇 5 关**不受影响**（`themes.Length != 5` 仍通过）。
- [ ] **高级化**：骷髅头有凹凸受光、每层有氛围粒子、打穿有慢动作+震屏、越高背景/音效越亮/开阔。
- [ ] Profiler：纵向关 Draw Call / GC ≤ 横版 1.5×。

**自动化冒烟**（新增 `AscentSmokeTest`）
- 遍历 ascent 0–2，断言：`Orientation == Ascent`、`Segments.Length == 8`、末段 `boss`。
- 镜头 `camera.transform.position.y` 随玩家攀升**单调上升**（关键回归点）。
- 调用测试伤害 API 后骷髅墙 `Breached == true`。
- 敌人生成位置 `y > player.y`（验证"从天而降"）。

---

<a id="15"></a>
## 15. 风险与应对

| 风险 | 影响 | 应对 |
|---|---|---|
| `Clamp(stage,0,4)` 漏改 → Ascent 读到错误主题资产 | 配色/Boss 名错乱 | §2.4 已列全 8 处，替换后全局搜索 `, 0, 4)` 复查 |
| `CameraRig.Initialize` 早于 `LevelManager.Initialize` → Ascent 相机未建 | 相机不跟随 | `LevelManager.Initialize` 末尾调 `CameraRig.SetupAscentIfNeeded()` |
| 骷髅格过多 → 物理/Draw Call 飙升 | 卡顿 | 懒激活 + 共材质 + 静态碰撞体 |
| 纵向关"冲太快"失去控制感 | 手感差 | 段跨度 26–34（比横版短）+ 平台密度提高 |
| `End` 语义（正 width）被其他系统误用 | 越界/反向 | 复查所有 `Lerp(start, End)` 用法 |
| 子弹未接 `SkullBlock` | 打不碎墙 | `Projectile.cs` 命中回调加分支 |
| 法线贴图/光照加太多 → 移动端掉帧 | 性能 | 高级化分档：低端机关闭法线与 God Ray，保留粒子 |
| 上升压迫机关过强 → 玩家无法反应 | 挫败 | 上升速度按难度调参，给明确预警色带 |

---

<a id="16"></a>
## 16. 附录：文件改动清单总表

| 文件 | 动作 | 说明 |
|---|---|---|
| `Level/LevelOrientation.cs` | **新增** | 枚举 + 扩展 |
| `Level/LevelManager.cs` | 改 | `Orientation`/`AscentIndex`/`AscentWidth`/`Height`；`Initialize`/`Update`/`Objective`/`RecordClear` |
| `Core/GameConfig.cs` | 改 | `ascentThemes[3]` + 校验（不动 `themes`） |
| `Level/ThemeProfile.cs` | 改 | `AscentDefaults()`（3 套） |
| `Core/AscentCameraDirector.cs` | **新增** | 竖直向上跟随相机 |
| `Core/CameraRig.cs` | 改 | `Ascent` 字段、`SetupAscentIfNeeded()`、`Update`/`ResetFollow` |
| `Level/SkullFiller.cs` | **新增** | 骷髅墙 + `SkullBlock` |
| `Level/SkullFillerDirector.cs` | **新增** | 懒激活 + 检查点复原 |
| `Combat/Projectile.cs` | 改 | 命中 `SkullBlock` 分支 |
| `Core/GameManager.cs` | 改 | `BeginRun(stage,o)`、`NextStage` 8 关、`RegisterSkullBreak()` |
| `Enemies/EncounterDirector.cs` | 改 | `BuildAscent()` + 替换 Clamp |
| `Enemies/EnemyRoster.cs` | 改 | 三关 Ascent 原型 |
| `Enemies/SideEnemyMotor.cs` | 改 | `AttackPattern.Dive` |
| `Level/AscentBuilder.cs` | **新增** | 纵向几何 + 机关 + 上升压迫 |
| `Level/SegmentBuilder.cs` | 改 | 抽 `LevelGeometryHelper` |
| `Level/PlatformSurface.cs` | 改 | `Phase`、`ElevatorDirection` |
| `Player/SidePlayerMotor.cs` | 改 | `Velocity`、蹬墙跳/快速攀升、死亡边界翻转 |
| `Enemies/SideBossController.cs` | 改 | 三关 Ascent Boss + `ThemeIndexFor` |
| `BossSignatureVfx.cs`/`CombatEffects.cs`/`SolidWallModule.cs`/`SceneFeedback.cs` | 改 | `ThemeIndexFor` 替换 Clamp |
| `UI/HUDController.cs` | 改 | 8 格按钮 + 高度计 + Ascent 文案 |
| `RunModel.cs`/`CampaignProgress.cs` | 改 | 解锁位宽容纳 8 关 |
| `Core/ProceduralArt.cs` | 改（高级化） | 骷髅头/岩层 `Paint` + **法线贴图** |
| `Core/SideArt.cs` | 改（高级化） | 骷髅头/天降敌/Boss 剪影像素函数 |
| `Diagnostics/SideScrollerSmokeTest.cs` | 改 | 新增 `AscentSmokeTest` |

---

## 一句话总结

Ascent 关**不需要重写引擎**：给关卡加 `LevelOrientation`（P0）+ 竖直向上相机（P2）+ 骷髅头填充墙（P3，挡在头顶向上凿穿）+ 从天而降的敌人（P4，配合底部上升深渊压迫），再填三关主题/机关/Boss（P5–P6），最后用**法线受光 + 氛围粒子 + 关键瞬间慢动作/震屏 + 高度视听渐变**把它抬到制作级（P8–P9）。这四样凑齐就是"从下向上凿穿、被天降包抄、被深渊追赶"的招牌攀升体验，且完全复用已有的软推进、连击、胶囊掉落、Boss 三阶段与 S/A/B/C 评级。相比旧版 Descent（向下潜行），方向反转但架构完全共享——只是推进轴 Y 的符号、骷髅墙位置、敌人来源、Boss 位置、压迫来源全部镜像。
