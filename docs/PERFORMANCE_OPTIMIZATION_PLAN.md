# 三角洲契约 / DELTA PACT — 性能优化方案（PERFORMANCE OPTIMIZATION PLAN）

> 适用对象：Codex / 后续制作阶段。本文把优化点落到具体代码（文件 + 行号 + 改法），
> 并区分「本次已实施（已改代码）」与「待实施（建议）」，避免重复劳动。
> 所有改动目标：**行为不变、只降开销**。游戏当前仍是程序化美术原型，正式素材接入后再做一轮针对性调优。

---

## 0. 本次已实施（已改代码，待 Unity 构建验证）

两处均为**行为保留**的低风险改动，已直接落盘：

### 0.1 `Assets/Scripts/Core/CombatEffects.cs`
- **问题**：`Update()` 每帧无条件遍历全部 224 个特效槽（即便大多空闲）；
  `Acquire()` 在每个分类区间线性扫描找空槽（爆炸/Burst 多次调用 → 最坏 O(n²)）。
- **改法**：
  - 224 槽按类别固定分区（warning ≥192 / encounter 128–191 / normal 0–127），
    因此类别可由下标推导，新增三个 `Stack<int>` 空闲栈 + 一个 `List<int> active` 活跃表。
  - `Acquire` 改为 O(1) 弹栈；`Update` 只遍历 `active` 表（倒序 + `RemoveAt`）；
    过期槽归还对应空闲栈。`ClearAll` 同步重置。
  - 24 个伤害数字同理改用 `freeNumbers` 空闲栈。
- **收益**：特效系统从「每帧固定 224 次迭代 + 发射时线性扫描」降到「仅活跃数」，
  大规模爆炸/弹幕时收益明显；GC 与分支基本不变。
- **风险**：低。类别分区与原 `start/end` 区间完全等价，仅复用顺序由 FIFO 变 LIFO（视觉无差异）。

### 0.2 `Assets/Scripts/UI/HUDController.cs`
- **问题**：`Update()` 每帧重建约 8 个字符串（含横版模式下 `game.Character.name.Split('/')`
  每帧分配数组 + 字符串）；UGUI 在内容相同时虽跳过网格重建，但 `new string` 每帧仍产生 GC 压力，
  倒计时类文本（技能/装填）每帧都在变，是主要分配源。
- **改法**：
  - 新增 `SetText(Text, ref string cache, string value)` 脏检查 helper：内容不变则不赋值、不分配。
  - 静态文本（HP、武器名、关卡/击破、波次、技能、Boss 阶段、弱点）仅在所依赖的原始值变化时才重建字符串。
  - 横版 `callsign` 的 `Split('/')` 改为按 `SelectedCharacter` 缓存，**只算一次**。
  - 倒计时/波次等高频文本节流到 **10 Hz**（`hudThrottle` / `sideThrottle`）；
    **HP 条、经验条、准星、开镜武器摇摆保持每帧**，手感不变。
- **收益**：稳态下 HUD 每帧字符串分配从 ~8 降到 ~0–2（仅倒计时），GC 压力大幅下降；
  横版每帧少一次 `Split` 分配。
- **风险**：低。唯一可感知差异是部分 HUD 读数刷新率从 60Hz 降到 10Hz（对读数是正向，无副作用）。

---

## 1. 已具备的良好基础（无需动）

- **失焦自动暂停**：`GameManager.OnApplicationFocus` 在失焦且游玩中调用 `Pause()`，
  避免后台空转（`Time.timeScale=0`）。保留。
- **对象池已存在**：`ProjectilePool`、`PickupPool`、`CombatEffects` 本身就是池，
  未走 `Instantiate` 热路径（除首帧初始化）。保留。
- **固定数组而非 List 动态扩容**：`Projectile[]`、`Slot[]` 预分配，无运行时扩容分配。保留。
- **物理查询用预分配缓冲**：`Projectile` 的 `hits[128]` / `blastHits[96]` 为复用数组，无 per-call 分配。保留。
- **材质复用**：`ProceduralArt` 共享 `spriteMaterial`，`HUDController` 复用 `Art.Square` 等。保留。

---

## 2. 关键热点分析（每帧成本）

| 热点 | 文件 | 每帧成本 | 严重度 |
|---|---|---|---|
| 敌人分离 O(N²) | `Enemies/EnemyAI.cs` `FixedUpdate` 内 `foreach (EnemyAI other in game.Enemies)` | N=40 → 1600 次/固定步（~50Hz）→ 8万次/秒；N 更大时平方增长 | 高（敌潮/精英群） |
| 弹丸物理 CircleCast | `Combat/Projectile.cs` `FixedUpdate` 每活跃弹丸一次 `Physics2D.CircleCast` + `Array.Sort` | 弹幕多时叠加；`GetComponentInParent<Health>()` 每次命中走反射查找 | 高（高射速/多发） |
| 竞技场实例化 | `Core/ProceduralArt.cs` `BuildArena`（仅非横版） | 每局重开重建 ~500+ 个 `SpriteRenderer` + `BoxCollider2D` GameObject；各自独立 Draw Call | 中（启动/重开卡顿、Draw Call） |
| 特效/弹丸池尺寸 | `GameConfig.maximumProjectiles` + 224 特效槽 | 池过大占用内存；过小则丢发射 | 低-中（按需调） |
| 2D 光照（枪口 Light2D） | `CombatEffects` `sideLightPrefab` / `Light2D` | URP 2D 光照有额外 overdraw 与批次成本 | 中（低端机） |
| HUD 字符串（已修） | `UI/HUDController.cs` | 见 0.2（已缓解） | 低（已修） |

---

## 3. 待实施优化（建议，按优先级）

### P0 — 敌人分离改用空间哈希 / 邻域查询
- 文件：`Enemies/EnemyAI.cs` `FixedUpdate`。
- 改法：把 `foreach` 全量两两比较，改为基于网格的空间哈希（按 `Mathf.Floor(pos/cell)`
  入桶，`cell≈0.7`）。每个敌人只比对同桶+邻桶敌人，复杂度从 O(N²) 降到 O(N·k)。
- 或：用 `Physics2D.OverlapCircle` 一次性取邻近碰撞体（复用 `ContactFilter2D`），
  避免手写双层循环。注意复用缓冲数组，不要每帧 new。
- 验收：同屏 60+ 敌人时 `EnemyAI.FixedUpdate` 总耗时应明显下降。

### P0 — 弹丸命中查找去反射 + 减少排序
- 文件：`Combat/Projectile.cs`。
- 改法：
  1. `count <= 1` 时跳过 `Array.Sort`（绝大多数单发命中无需排序）。
  2. 命中目标改为在发射/初始化时缓存 `Health` 引用（敌人/玩家各自挂 `Health`，
     可在 `OnTriggerEnter2D`/碰撞阶段用 `collider.attachedRigidbody.GetComponent<Health>()`
     一次性取并缓存到弹丸命中表），避免每帧 `GetComponentInParent`。
  3. 若弹量极大，评估把 `CircleCast` 改为更便宜的 `OverlapCircle` + 距离筛选（牺牲一点精度）。
- 验收：高射速武器连射时 `Projectile.FixedUpdate` 占比下降、GC 0 分配。

### P1 — 竞技场地面/碎屑合并，减少 Draw Call 与启动卡顿
- 文件：`Core/ProceduralArt.cs` `BuildArena`（非横版模式）。
- 改法（任选其一，推荐 A）：
  - **A（最小改动）**：把逐格 `ConcreteTile`（~400 个 1.96 单位 SpriteRenderer）替换为
    **一张平铺纹理的大地板**（`Texture2D` 程序化生成一次，wrapMode=Repeat，单 SpriteRenderer），
    裂缝/碎石用少量装饰对象或并入同一图集；四壁保留（含 `BoxCollider2D`）。
  - **B**：用 `Tilemap` + `TilemapCollider2D`（Composite），Unity 会自动合并碰撞与渲染批次。
  - **C**：对所有静态装饰调用 `StaticBatchingUtility.Combine` 合并为静态批次。
- 收益：启动实例化从 ~500 GameObject 降到个位数，Draw Call 大幅减少，重开更顺。
- 横版模式不使用 `BuildArena`（走 `LevelManager`/`SegmentBuilder`），不受影响，但其 Segment 构建同理建议复用合并方案（见 `Level/SegmentBuilder.cs`）。

### P1 — 运行时纹理缓存（启动加速）
- 文件：`Core/ProceduralArt.cs` 构造函数。
- 现状：`Paint()` 在每次 `new ProceduralArt` 时重算 ~15 张纹理（最大 256×256），量级小但每次启动都算。
- 改法：首启生成后序列化到 `Application.persistentDataPath` 的缓存目录，
  后续启动若哈希一致则直接 `Sprite.Create` 从磁盘加载；或至少把 `Shader.Find` 结果缓存为静态字段。
- 收益：启动再快一点；对低端机更友好。

### P2 — 弹丸/特效池尺寸按质量分级（见第 4 节）

### P2 — 大数字/计数类文本继续节流
- 横版 `UpdateSideHud` 中 `stateText`/`skillText` 已节流到 10Hz；若需更稳，可把 `armStatus` 的弱点血条也并入。
- 另：`FormatTime(game.Elapsed)` 在 `levelText` 内每 0.1s 一次，可接受；若想进一步，缓存到「秒」变化。

---

## 4. 移动端 / 跨平台可扩展性（质量分级 Quality Tiers）

当前无任何画质分级，直接搬上 Android/iOS 会过热降频。建议在 `GameConfig` 或 `DisplaySettings`
增加 **QualityTier（Low / Medium / High）**，并在启动时按设备能力（或玩家设置）选定：

| 项目 | Low（入门机） | Medium | High（PC/旗舰） |
|---|---|---|---|
| 目标帧率 `Application.targetFrameRate` | 30 | 60 | 60（或 120 若支持） |
| 渲染分辨率缩放 `UniversalRenderPipelineAsset.renderScale` | 0.7 | 0.85 | 1.0 |
| 枪口 `Light2D` | 关闭 | 关闭/降低半径 | 开启 |
| 特效池上限（`CombatEffects` 槽数） | 96 | 160 | 224 |
| `maximumProjectiles` | 128 | 256 | 512（按武器实测） |
| 敌人同屏上限 / 分离网格粒度 | 收紧 | 默认 | 默认 |
| 后处理（若接入制作级） | 关 | 轻量 | 全开 |
| 阴影 / 2D 软阴影 | 关 | 关 | 开 |

实现要点：
- 用 `QualitySettings` 档位或自管 ScriptableObject，避免 `if (tier==...)` 散落各系统。
- 移动端额外：禁用鼠标相关准星逻辑的分支开销、用 `UnityEngine.InputSystem` 的触控，
  并关闭桌面专属 `DisplaySettings` 的 F11 全屏分支（用 Safe Area 适配刘海）。
- **iOS 硬约束**：必须在 Mac + Xcode + $99/年 开发者账号下打包，Windows 无法出 iOS 包；
  Android 需 Unity 安装 Android 模块 + 签名密钥。详见 `docs/` 中跨平台说明。

---

## 5. 启动与加载优化

- `GameManager.Initialize` 同步生成纹理 + 构建竞技场 + 建 HUD/菜单。当前体量可接受；
  若后续素材变大，建议：
  - 主菜单先出，再 **异步**（`AsyncOperation` / 协程）加载对局资源；
  - `ProceduralArt` 纹理缓存（见 3.P1）；
  - 重开（`BeginRun`）复用已生成的 `Art` 实例，不要每次 `new ProceduralArt`（当前 `Art` 在 `Initialize` 建一次、跨局复用，已正确；确认 `ClearRun` 不 Dispose `Art` —— 现状确实只 Dispose 在 `OnDestroy`，正确）。

---

## 6. 验证建议（交付前的硬指标）

用 Unity **Profiler**（CPU + GC Alloc + Rendering Draw Calls）对比改动前后，重点看：
1. 同屏 40+ 敌人、全屏弹幕时 `EnemyAI.FixedUpdate` 与 `Projectile.FixedUpdate` 的 ms 占比与 GC 0 分配。
2. 稳态游玩 `HUDController.Update` 的 GC Alloc 应趋近 0（已修）；`CombatEffects.Update` 仅随活跃特效数增长。
3. 启动/重开时长与实例化 GameObject 数量（看 P1 合并后的改善）。
4. 低端档（renderScale 0.7、关 Light2D）在目标机上能否稳 30fps。

> 说明：本环境（WorkBuddy 沙箱）无法保持游戏进程常驻，上述需由你在本地 Unity Editor
> 用 Play Mode + Profiler 或出包后在真机验证；自动化冒烟测试（`RuntimeSmokeTest` / `-moriSmoke`）
> 仍用临时一波 5 秒配置 + Boss 测试无敌/伤害 API，不等同默认十波人工通关验收。
