# 游戏手感与视觉打磨方案（对标《无畏契约 / Valorant》）

> 用途：优化与整改建议，供 Codex 实现。配合 `docs/CAMERA_2_5D_OVERSHOULDER_PLAN.md`（视角整改）一起构成「像和平精英 / 无畏契约」的整体体验目标。
> 核心结论：**工程已有扎实的打击感地基**（枪口火光、屏幕震动、后坐力、拖影、伤害飘字、预警、大招环都已实现），本方案重点是**对标 Valorant 的「命中确认 + 可读性 + UI 克制」三件套做补齐与调优**，而非推倒重来。

---

## 0. 给 Codex 的执行摘要（可直接粘贴）

> 提升整体「游戏感」到《无畏契约》水准，同时保持 2D 程序化美术与现有玩法。三类硬性补齐：
> 1) **命中确认**：准星命中标记（hit marker）+ 敌人命中白闪 + 击杀弹出（kill pop）+ 弹道拖尾（tracer）+ 命中火花；
> 2) **可读性**：敌人/子弹/预警的高对比描边与配色统一（玩家青、敌方危险橙红、背景墨黑），子弹与敌人轮廓强化；
> 3) **UI 克制与信息密度**：击杀信息流（kill feed）、技能/大招充能 HUD、更干净的极简战术 HUD、统一配色语言。
> 已有系统（CombatEffects / WeaponController.Fire 的 Muzzle+Shake+AddRecoil / CharacterVisual 命中闪 / Health.Damaged|Died）**只调不重写**；所有新增手感参数进 `GameConfig` 的 `[Header("Game Feel / Juice")]`。分阶段：P1 命中确认+可读性（无畏契约感最强、风险最低），P2 技能/武器反馈与音频分层，P3 UI 清晰度与色彩分级，P4 调优与性能验收。

---

## 1. 目标与解读

用户要求：**整体游戏效果要和无畏契约一样「不错」**。在「保留 2D」前提下，这指的是 Valorant 的**制作完成度（game feel / juice）**，而非 3D 化。Valorant 的「好手感」可拆为：

1. **命中确认极致清晰**：每发命中都有即时、克制的反馈（准星 hit marker + 敌人白闪 + 击杀特殊标记），让玩家 100% 知道「打中没、打死没」。
2. **可读性（Readability）**：角色/敌人是高对比、无噪声的清晰剪影；子弹、技能、预警一眼可分；颜色语言统一。
3. **克制的 UI**：信息密度高但不乱，极简战术风，配色克制（青=友方、橙红=威胁、墨黑=背景）。
4. **干脆的音频**：开枪、命中、击杀、换弹、闪避各有清晰分层音色。
5. **稳定的 60fps**：轻量 2D + 对象池，已具备。

---

## 2. 现状打击感审计（已有哪些，避免重复造轮子）

| 系统 | 现状（已存在，引用位置） | 评价 |
|---|---|---|
| 枪口火光 | `WeaponController.Fire` → `game.Effects.Muzzle(...)` | ✅ 已有 |
| 屏幕震动 | `Fire` → `CameraRig.Shake(...)`；受击 `PlayerController.OnDamaged`→`Shake` | ✅ 已有 |
| 后坐力 | `Fire` → `CameraRig.AddRecoil(game.Config.camWeaponRecoil[Selected])` | ✅ 已有（需在 GameConfig 确认 `camWeaponRecoil` 数组已定义） |
| 拖影（闪避） | `CombatEffects.Ghost(...)`（闪避时生成残影） | ✅ 已有 |
| 伤害飘字 | `CombatEffects.DamageNumber(...)`（含暴击配色） | ✅ 已有，但偏「装饰」，未与准星确认联动 |
| 命中闪 | `CharacterVisual.OnDamaged` → `flash` 红闪 | ⚠️ 偏红、偏弱，不是 Valorant 的「白闪」 |
| 敌人预警 | `CombatEffects.Telegraph` / `WarningLine` | ✅ 已有 |
| 大招环 | `CombatEffects.Pulse` + `GameManager.Pulse`（环+爆发+震屏） | ✅ 已有，可增强为「终极感」 |
| 受击/死亡事件 | `Health.Damaged(amount)` / `Health.Died` | ✅ 已有，可作钩子 |
| HUD | `HUDController`：血/弹/波次/技能/Boss/播报 + 操作提示 | ⚠️ 功能完整但非「极简战术风」，缺 hit marker / kill feed / 技能充能 UI |
| 音频 | `GameAudio` + `SoundKind`（Shot/Shotgun/Sniper/Reload/Blast/PlayerHit/Dodge…） | ✅ 已有，缺命中确认 ping、分层 |

**结论**：地基扎实。本方案主要做**补齐（hit marker / tracer / kill feed / 白闪 / 击杀弹出）** 与**调优（可读性、UI 克制、音频分层）**。

---

## 3. 缺口 → 实现（按系统）

### 3.1 命中确认（Valorant 的灵魂，P1 必做）

**a) 准星命中标记（Hit Marker）** — 新增 `HUDController`：
```csharp
// HUDController 内
private Image[] hitTicks = new Image[4];
private float hitMarkerUntil;
public void HitConfirm(bool killed)
{
    hitMarkerUntil = Time.unscaledTime + (killed ? 0.18f : 0.09f);
    foreach (Image t in hitTicks) t.color = killed ? ProceduralArt.Danger : Color.white;
}
// Update 中：
bool show = Time.unscaledTime < hitMarkerUntil;
foreach (Image t in hitTicks) t.gameObject.SetActive(show);
```
在 `BuildHud()` 于屏幕中心十字处放 4 个短刻度 `Image`（命中时闪现；击杀变红/变 X）。

**触发点**：在子弹命中并成功造成伤害的分支（`ProjectilePool` 命中结算 / `Projectile.Hit` 中 `Health.TakeDamage` 成功后）调用：
```csharp
game.Hud.HitConfirm(critical, killed);
```
`killed` 由 `Health.Died` 是否已触发判定（可用 `!enemy.Health.IsDead` 的前后对比）。

**b) 敌人命中白闪 + 击杀弹出（Kill Pop）** — 调 `CharacterVisual`：
```csharp
private void OnDamaged(float damage)
{
    flash = 0.09f;                 // 保留
    popScale = 1.18f;             // 新增：命中瞬间放大
}
private void OnDied()             // 新增：health.Died += OnDied
{
    game.Effects.Burst(transform.position, ProceduralArt.Danger, 14);
    game.Hud.KillFeed("目标清除");
    popScale = 0f;                // 死亡收缩
}
// LateUpdate：sprite.color 命中瞬间用纯白（Valorant 风），而非红
sprite.color = flash > 0 ? Color.white : tint;
float s = baseScale.x * (1 + Mathf.Max(0, popScale - 1)) ;
sprite.transform.localScale = baseScale * Mathf.Lerp(1f, popScale, flash > 0 ? 1 : 0); // 简化：命中放大回落
```
Initialize 里补 `health.Died += OnDied;`。

**c) 弹道拖尾（Tracer）** — 新增 `CombatEffects.Tracer(Vector2 from, Vector2 to, Color color)`（复用 `Art.Square` 拉伸成细线、快速淡出）。在命中结算处从枪口世界坐标画到命中点：
```csharp
game.Effects.Tracer(muzzleWorld, hitPoint, definition.color);
```
> 注：本作为抛射物（有飞行时间），tracer 在「子弹落地瞬间」绘制比开火瞬间更符合观感；也可在子弹自身后加拖影。二选一即可。

**d) 命中火花** — 命中点在 `CombatEffects.Burst(position, color, 6)` 小爆发（已有 Burst，确保命中时触发，而非仅死亡）。

### 3.2 可读性（Readability，P1）

- **配色语言统一**（`ProceduralArt` 已是青/危险红/墨黑基调）：确保敌人用 `ProceduralArt.Danger`、玩家/友方用青；子弹与敌人**描边加深**（可在 `CharacterVisual` 给 sprite 额外叠一层略大深色底，或用 `Art.Glow` 描边）。
- **子弹可见性**：抛射物提速或加拖影/发光，确保快节奏下可追踪（复用 `CombatEffects` 或 `Projectile` 自身拖影）。
- **预警强化**：`Telegraph`/`WarningLine` 已存在，调高对比与脉动频率，使攻击前摇一眼可辨（Valorant 的「读招」核心）。

### 3.3 技能 / 大招 juice（P2）

- `GameManager.Pulse` / `CombatEffects.Pulse`：增加**施法前摇提示**（短预警环）+ 命中时更强 `Shake` + 屏幕轻微白闪（经 `CameraRig` 已有 shake，可加一次性 `Flash`）。
- `HUDController`：把现有 `Q 脉冲就绪` 文本升级为**技能/大招充能图标 + 进度环**（参考 Valorant 底部技能栏），让「大招就绪」一目了然。

### 3.4 武器反馈（P2）

- **换弹**：现有 HUD 百分比；增加换弹「咔哒」音（`SoundKind.Reload` 已有）与枪体轻微下摇动画（`WeaponController` 的 `pivot` 在换弹时下移再回弹）。
- **切枪**：现有 0.2s 切换；加切枪音与枪体缩放 pop。
- **空仓**：弹尽时补「空击」音与准星微抖。

### 3.5 UI 克制与信息密度（P3）

- **击杀信息流（Kill Feed）**：右上角淡入淡出列表（`HUDController.KillFeed`）。
- **极简战术 HUD**：收紧间距、统一字号层级、危险信息（低血/Boss）用 `Danger` 强调；把操作提示改为淡色小字常驻底部。
- **配色与字体**：沿用 `ProceduralArt.Cyan`/`Danger`/`Ink`，避免杂色；数字用等宽对齐（弹药 `00 / 28` 已做）。
- **菜单/结算**：主菜单与胜负界面的极简风对齐（标题已改为《三角洲契约 / DELTA PACT》）。

### 3.6 音频分层（P2–P3）

- 新增**命中确认 ping**（区别于开枪声，短促高频）、击杀特殊音、闪避 whoosh、大招 whoosh、UI 点击 tick。
- 确保混音清晰：枪声不过载、命中 ping 不被掩盖。

### 3.7 色彩分级 / 后期（P3，可选）

- 常驻低强度 **暗角（vignette）**（ADS 时已在相机方案中加强）；关键特效（大招环、枪口）加轻微 **bloom**（2D 可用 additive 叠加近似，不必上 URP 后处理，控制风险）。
- 背景 `ProceduralArt.Ink` 保持墨黑，突出前景高对比。

### 3.8 `GameConfig` 手感参数（贯穿各阶段）

```csharp
[Header("Game Feel / Juice")]
[Range(0.02f, 0.2f)] public float hitMarkerDuration = 0.09f;
[Range(0.02f, 0.25f)] public float enemyHitFlash = 0.1f;
[Range(1f, 1.6f)] public float killPopScale = 1.25f;
[Range(0.03f, 0.2f)] public float tracerLife = 0.07f;
[Range(0f, 0.3f)] public float damageNumberPop = 0.12f;
[Range(0f, 1f)] public float vignetteAlways = 0.18f;
public float[] camWeaponRecoil = new float[] { 0.06f, 0.14f, 0.1f }; // 已存在则复用
```
`Validate` 补非负校验即可。

---

## 4. 分阶段实施计划

| 阶段 | 内容 | 交付（无畏契约信号） |
|---|---|---|
| **P1（必做）** | 3.1 命中确认（hit marker + 白闪 + kill pop + tracer + 火花）；3.2 可读性（描边/配色/预警） | 「打中没、打死没」即时可知，敌人清晰可读 |
| **P2** | 3.3 技能/大招 juice；3.4 武器反馈；3.6 音频分层 | 大招有「终极感」，换弹/切枪/空仓有反馈，声音分层 |
| **P3** | 3.5 UI 克制（kill feed + 技能 HUD + 极简风）；3.7 色彩分级 | 像 Valorant 的干净战术 HUD |
| **P4** | 3.8 参数调优 + 60fps 验收 + 边界（暂停/升级/结算界面手感一致） | 验收达标 |

> 暂停/升级/结算时 `timeScale=0`：hit marker / tracer / 飘字应停用或冻结，避免残留；`HUD.ApplyState` 已按状态显隐，扩展即可。

---

## 5. 验收标准

1. 每发命中：准星 hit marker 闪现；敌人白闪；命中火花出现。
2. 击杀：kill pop（放大/收缩）+ 击杀信息流入流 + 区别于命中的音/标记。
3. 弹道有可见 tracer（或子弹拖影），快节奏下子弹可追踪。
4. 敌人/子弹/预警高对比、无视觉噪声，配色统一（青=友、橙红=敌、墨=底）。
5. 大招有明显「终极感」（前摇+环+震屏+音）；换弹/切枪/空仓有反馈。
6. HUD 极简战术风，含 kill feed 与技能充能指示，信息密度高但不乱。
7. 音频分层清晰，命中 ping 不被枪声掩盖。
8. 全程稳定 60fps（沿用对象池，不引入重后处理）。
9. 所有新手感参数在 `GameConfig` 可调且通过 `Validate`。

---

## 6. 风险与回退

- **手感过噪**：hit marker / 火花 / 飘字叠加易乱 → 全部走 `GameConfig` 强度参数，P4 统一收敛；默认偏克制。
- **可读性翻车**：描边/发光叠加导致性能或杂乱 → 优先「配色 + 预警对比」这类零成本项，发光/bloom 放 P3 且可关。
- **HUD 改坏布局**：`HUDController` 用 `Widget` 锚点系统，新增元素走同一套锚点，避免破坏现有布局。
- **不破坏已验证构建**：仅**扩展/调优** `CombatEffects`、`CharacterVisual`、`HUDController`、`WeaponController`，不重写玩法逻辑；`Health.Damaged|Died` 仅增订阅不改动语义。

---

## 7. 改动文件清单

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/UI/HUDController.cs` | 新增 hit marker、kill feed、技能充能 HUD、极简风调整 |
| `Assets/Scripts/Core/CombatEffects.cs` | 新增 `Tracer`；`DamageNumber` 接入 pop 参数 |
| `Assets/Scripts/Core/CharacterVisual.cs` | 命中白闪 + kill pop + `Health.Died` 订阅 |
| `Assets/Scripts/Weapons/WeaponController.cs` | 换弹/切枪/空仓动画与音；tracer 触发（命中结算处） |
| `Assets/Scripts/Combat/ProjectilePool.cs` / `Projectile.cs` | 命中结算处调用 `Hud.HitConfirm` + `Effects.Tracer/Burst` |
| `Assets/Scripts/Core/GameManager.cs` | `Pulse` 大招增强（前摇/白闪/震屏） |
| `Assets/Scripts/Core/GameConfig.cs` | `[Header("Game Feel / Juice")]` 参数 + Validate |
| `Assets/Scripts/Core/ProceduralArt.cs` | 配色/描边辅助（可选） |
| `Assets/Scripts/Core/GameAudio.cs` | 命中 ping / 击杀 / 闪避 whoosh / UI tick |

> 本方案与 `CAMERA_2_5D_OVERSHOULDER_PLAN.md`（视角）互为补充：视角解决「像和平精英的取景」，本方案解决「像无畏契约的完成度」。
