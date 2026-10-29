# MoriEchoes 视角整改方案：2.5D 越肩第三人称（保留 2D）

> 文档用途：优化与整改建议，供 Codex 实现。
> 目标：在**不改动 2D 程序化美术 / 正交管线 / Rigidbody2D 玩法**的前提下，把当前「俯视正中心」的视角，改成「和平精英 / PUBG Mobile」式的**第三人称越肩（over-the-shoulder）**观感：角色在画面偏下、前方视野更大、可开镜缩放、有后坐力冲击、准星居中。

---

## 0. 给 Codex 的执行摘要（可直接粘贴）

> 把 `Assets/Scripts/Core/CameraRig.cs` 从「正交俯视正中心跟随」改为「正交 + 沿瞄准方向前导 + 身后偏移（越肩）+ 可选俯倾做 2.5D + 开镜缩放 + 后坐力/抖动」。保持 `Camera.orthographic = true`，不引入 3D 资源。
> 同步改动：`GameConfig.cs`（新增相机参数段）、`InputReader.cs`（新增开镜输入 `AimDownSights`）、`PlayerController.cs`（移动改为「以瞄准方向为前」的相机相对移动，并驱动 `CameraRig.SetAds`）、`WeaponController.cs`（开火时调用 `CameraRig.AddRecoil`）、`CharacterVisual.cs`（2.5D 时精灵做广告牌朝向相机，可选背面英雄精灵）、`HUDController.cs`（中心准星 + 开镜瞄准镜暗角）。
> 关键约束：瞄准仍走 `CameraRig.Camera.ScreenToWorldPoint`（正交相机前导/俯倾下依然成立，无需重写命中逻辑）。所有新参数进 `GameConfig` 并加 `Validate`。分阶段实现，Phase 1 即可上手体验，Phase 2 增强 2.5D 纵深。

---

## 1. 现状诊断（为什么现在不像和平精英）

| 项 | 当前实现 | 位置 | 问题 |
|---|---|---|---|
| 投影 | 正交、俯视正中心 | `CameraRig.cs:17-21` `orthographicSize=7.7f`，相机在 `(0,0,-10)` 看向 +Z | 纯俯视，无「身后/肩上」视角，无纵深 |
| 跟随 | 玩家位置 + `Aim*1.1` 前导，竞技场夹紧，lerp 跟随 | `CameraRig.cs:28-46` | 仅小幅前导，玩家永远居中，不像第三人称 |
| 缩放/开镜 | 无 | — | 没有 PUBG 的「开镜 FOV 变化」 |
| 后坐力 | 仅有受击 `Shake` | `PlayerController.OnDamaged` → `CameraRig.Shake(0.22f)` | 开火没有冲击感 |
| 移动 | 世界轴 `WASD`（W=+Y） | `PlayerController.cs:51` `move = input.Move` | 不是「相机相对」（PUBG 的 W=朝视角前方） |
| 角色表现 | 顶面精灵，XY 平面朝相机 | `CharacterVisual.cs` | 没有「背面/第三人称」观感 |
| 准星 | 无中心准星，靠鼠标位置 | `HUDController` 仅 HUD 文本 | 缺瞄准镜/准星 UI |

**结论**：当前是标准 twin-stick 俯视；要「像和平精英」需要做的是**重新设计相机取景（越肩 + 前导 + 缩放 + 后坐力）与相机相对移动、补准星 UI**，而非把游戏 3D 化。所有 2D 资源可保留。

---

## 2. 目标视角设计（2.5D 越肩第三人称）

取景规则（对应 PUBG 越肩）：
1. **角色偏下居中**：相机目标点 = 玩家 + 沿瞄准方向的前导量 `camLead`，使玩家落在画面下方约 1/3，前方（瞄准方向）留出更大视野。
2. **越肩偏移**：相机再向「瞄准反方向」后退 `camBack`，形成肩部视差（角色略偏一侧）。
3. **开镜缩放（ADS）**：右键 / 手柄 LT 触发，`orthographicSize` 从 `camBaseSize` 平滑 lerp 到 `camAdsSize`（等效 PUBG 的 FOV 收窄），并叠加瞄准镜暗角。
4. **后坐力**：每次开火 `AddRecoil`，产生一个沿瞄准反方向的上抬 + 衰减，配合现有 `Shake` 做屏幕冲击。
5. **相机相对移动**：`W` = 沿瞄准方向前进，`S` = 后退，`A/D` = 左右平移（strafe），与 PUBG 一致。
6. **可选 2.5D 纵深**：`camPitchDeg > 0` 时让相机绕本地 X 轴俯倾，并把精灵做广告牌朝向相机，地面（`ProceduralArt.BuildArena`）自然呈现「向后延伸」的纵深。该项是增强项，Phase 1 可先 `camPitchDeg = 0`。

> 为什么坚持**正交**而非透视：正交保留 2D 观感、精灵不变形、`ScreenToWorldPoint` 命中逻辑零改动、缩放只需 lerp `orthographicSize`。透视（FOV 缩放）是可选增强，见 §7 风险。

---

## 3. 实施方案（按文件）

### 3.1 `GameConfig.cs` — 新增相机参数段

在 `[Header("Audio / presentation")]` 之后追加：

```csharp
[Header("Camera / 2.5D 越肩")]
[Min(1f)] public float camBaseSize = 7.7f;     // 常态正交半高
[Range(1f, 20f)] public float camAdsSize = 4.2f; // 开镜后正交半高（越小越近）
[Range(0f, 6f)] public float camLead = 2.2f;    // 沿瞄准方向前导（玩家偏下）
[Range(0f, 8f)] public float camBack = 1.2f;    // 身后偏移（越肩）
[Range(0f, 55f)] public float camPitchDeg = 0f; // >0 开启 2.5D 俯倾
[Range(0f, 12f)] public float camHeight = 0f;   // 2.5D 抬高（XY 平面的“上”）
[Min(0.5f)] public float camFollow = 6f;        // 跟随刚度
[Min(0.5f)] public float camZoomSpeed = 12f;    // 开镜/收镜速度
[Min(0f)] public float camRecoilMax = 0.25f;    // 后坐力上限
[Min(0f)] public float camRecoilDecay = 1.6f;   // 后坐力衰减/秒
[Range(0f, 2f)] public float camRecoilPitch = 0.4f; // 后坐力俯仰系数
public Sprite playerBackSprite;                 // 可选：背面/第三人称英雄精灵
```

并在 `Validate` 中补充（可选但建议）：
```csharp
if (camAdsSize >= camBaseSize) { reason = "camAdsSize 必须小于 camBaseSize。"; return false; }
if (camLead < 0 || camBack < 0) { reason = "相机前导/身后偏移不能为负。"; return false; }
```

### 3.2 `CameraRig.cs` — 重写为越肩 + 缩放 + 后坐力

```csharp
using UnityEngine;

namespace MoriEchoes
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        private GameManager game;
        private float shake;
        private float recoil;     // 后坐力累计
        private float zoom;       // 当前缩放插值 0=常态 1=开镜
        private float zoomTarget; // 目标缩放
        private Vector3 position;
        private Quaternion baseRotation = Quaternion.identity;
        private Vector3 aimForward = Vector3.right;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Camera = GetComponent<Camera>();
            Camera.orthographic = true;                 // 保留 2D
            Camera.orthographicSize = game.Config.camBaseSize;
            Camera.backgroundColor = ProceduralArt.Ink;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            float pitch = game.Config.camPitchDeg * Mathf.Deg2Rad;
            baseRotation = (pitch > 0.0001f) ? Quaternion.Euler(pitch, 0, 0) : Quaternion.identity;
            transform.rotation = baseRotation;
            position = Vector3.zero;
            transform.position = position;
            zoom = 0f; recoil = 0f; zoomTarget = 0f;
        }

        public void ResetFollow()
        {
            position = game != null && game.Player != null ? (Vector3)game.Player.transform.position : Vector3.zero;
            shake = 0f; recoil = 0f; zoom = 0f; zoomTarget = 0f;
            transform.position = position;
        }

        public void Shake(float intensity) =>
            shake = Mathf.Min(0.5f, shake + intensity * game.Config.screenShake);

        // 开火时由 WeaponController 调用
        public void AddRecoil(float amount) =>
            recoil = Mathf.Min(game.Config.camRecoilMax, recoil + amount);

        // 由 PlayerController 每帧驱动（读 InputReader.AimDownSights）
        public void SetAds(bool ads) => zoomTarget = ads ? 1f : 0f;

        private void LateUpdate()
        {
            if (game == null || game.Player == null) return;

            aimForward = ((Vector3)game.Player.Aim).normalized;
            if (aimForward == Vector3.zero) aimForward = Vector3.right;

            // 1) 目标点：玩家 + 沿瞄准方向前导（玩家偏下）
            Vector3 target = game.Player.transform.position + aimForward * game.Config.camLead;
            // 2) 越肩：相机退到瞄准反方向
            Vector3 desired = target - aimForward * game.Config.camBack;
            // 3) 2.5D：俯倾时沿 -Z（相机视线方向）抬高，制造身后/上方视角
            if (game.Config.camPitchDeg > 0.0001f)
                desired += Vector3.back * game.Config.camHeight;

            // 竞技场边界夹紧（用当前缩放后的可见半径）
            float size = Camera.orthographicSize;
            float extX = size * Camera.aspect;
            float horiz = Mathf.Max(0, game.Config.arenaHalfSize.x - extX);
            float vert = Mathf.Max(0, game.Config.arenaHalfSize.y - size);
            desired.x = Mathf.Clamp(desired.x, -horiz, horiz);
            desired.y = Mathf.Clamp(desired.y, -vert, vert);

            position = Vector3.Lerp(position, desired, 1f - Mathf.Exp(-game.Config.camFollow * Time.unscaledDeltaTime));

            // 4) 开镜缩放
            zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-game.Config.camZoomSpeed * Time.unscaledDeltaTime));
            Camera.orthographicSize = Mathf.Lerp(game.Config.camBaseSize, game.Config.camAdsSize, zoom);

            // 5) 后坐力 + 抖动
            recoil = Mathf.Max(0f, recoil - Time.unscaledDeltaTime * game.Config.camRecoilDecay);
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 0.7f);
            Vector2 jitter = (game.IsPlaying ? Random.insideUnitCircle * shake : Vector2.zero)
                          + (Vector2)(-aimForward) * recoil * 0.15f; // 后坐力沿瞄准反方向轻抬

            transform.position = position + new Vector3(jitter.x, jitter.y, 0f);
            if (game.Config.camPitchDeg > 0.0001f)
                transform.rotation = baseRotation * Quaternion.Euler(recoil * game.Config.camRecoilPitch * 30f, 0f, 0f);
        }
    }
}
```

> 注意：`aimForward` 使用 `game.Player.Aim`（XY 平面向量）即可；俯倾只改变观感，不影响 2D 玩法平面。

### 3.3 `InputReader.cs` — 新增开镜输入

在属性区加入：
```csharp
public bool AimDownSights { get; private set; }
```
在 `Capture()` 开头 `AimDownSights = false;`，并在 `mouse` 与 `gamepad` 分支赋值：
```csharp
// mouse 分支内
AimDownSights = mouse.rightButton.isPressed;
// gamepad 分支内（左扳机）
if (gamepad.leftTrigger.isPressed) { AimDownSights = true; UsingGamepad = true; }
```

### 3.4 `PlayerController.cs` — 相机相对移动 + 驱动开镜

`Update()` 中，在计算完 `Aim` 之后、`if (!game.ControlsReady) return;` 之前插入：
```csharp
// 驱动相机开镜
game.CameraRig.SetAds(input.AimDownSights);

// 相机相对移动：以瞄准方向为“前”
Vector2 fwd = Aim.normalized;
Vector2 right = new Vector2(-fwd.y, fwd.x); // 屏幕右 = 瞄准逆时针 90°
Vector2 camMove = fwd * move.y + right * move.x;
// 开镜时略微降速（贴近 PUBG 手感，可关：把 1f 改成常量）
float adsFactor = game.CameraRig != null && input.AimDownSights ? 0.6f : 1f;
move = camMove * adsFactor;
```
> 方向符号 `right = (-fwd.y, fwd.x)` 为默认；若实测左右相反，取 `(fwd.y, -fwd.x)`。保留 dodge 方向逻辑（用 `move` 或 `Aim` 均可，原文不变）。

### 3.5 `WeaponController.cs` — 开火注入后坐力

在 `Fire()` 成功发射处（已有发射逻辑之后）调用：
```csharp
if (game.CameraRig != null) game.CameraRig.AddRecoil(0.12f);
```
数值随武器可调（SR 后坐更大，可读取 `Definition.spreadDegrees` 或新增字段）。

### 3.6 `CharacterVisual.cs` — 2.5D 广告牌 + 背面精灵（Phase 2）

- 若 `game.Config.camPitchDeg > 0`，在 `LateUpdate` 让 `sprite` / `halo` 朝向相机，避免精灵被俯倾压扁：
  ```csharp
  if (game.Config.camPitchDeg > 0.0001f && game.CameraRig != null)
      sprite.transform.rotation = game.CameraRig.Camera.transform.rotation;
  ```
  （或使用独立 Billboard：让精灵只绕 Z 轴对齐相机 yaw，保持直立。）
- 若有 `game.Config.playerBackSprite`，且为玩家，则 `sprite` 用背面精灵（`CharacterVisual.Initialize` 中：`custom = friendly ? game.Config.playerBackSprite ?? game.Config.playerSprite : ...`）。
- 阴影（`Shadow`）保留在地面，增强「立于地面」的第三人称感。

### 3.7 `ProceduralArt.BuildArena` — 地面纵深（可选增强）

在 `camPitchDeg > 0` 时，给竞技场地面叠加：
- 淡网格线 / 同心圆 / 径向渐变，使俯倾后呈现「向远处延伸」的地面；
- 远景层（视差）：随相机移动以略慢比例滚动，强化纵深。
（纯视觉，不影响碰撞。）

### 3.8 `HUDController.cs` — 准星 + 瞄准镜暗角

- 在 `BuildHud()` 增加**中心准星**（小十字/圆点，`ScreenSpaceOverlay`，锚定屏幕中心）。
- 新增 `scopeVignette` 全屏暗角 Image，`Update()` 中按 `game.Player.Weapon` 不可用，改为读取相机缩放：
  ```csharp
  // 需要把 CameraRig 的 zoom 暴露为属性，或 HUD 读 Config + 估算
  float ads = game.CameraRig != null ? game.CameraRig.AdsAmount : 0f;
  scopeVignette.color = new Color(0,0,0, 0.55f * ads);
  scopeVignette.SetActive(ads > 0.05f);
  ```
  为此在 `CameraRig` 暴露 `public float AdsAmount => zoom;`。
- 更新底部操作提示（`BuildHud` 中 `Controls` 文案）加入「右键开镜」。

---

## 4. 分阶段实施计划

| 阶段 | 内容 | 交付 |
|---|---|---|
| **Phase 1（必做，可上手）** | `GameConfig` 新参数；`CameraRig` 前导+越肩+开镜缩放+后坐力；`InputReader` 开镜；`PlayerController` 相机相对移动；`WeaponController` 后坐力；HUD 中心准星 | 越肩取景 + 开镜 + 后坐力，2D 不变 |
| **Phase 2（增强 2.5D）** | `camPitchDeg>0` 俯倾；`CharacterVisual` 广告牌 + 背面精灵；`ProceduralArt` 地面纵深/视差；瞄准镜暗角 | 明显纵深，像 PUBG 的 3/4 第三人称 |
| **Phase 3（打磨）** | 参数手感调优（lead/back/pitch/缩放曲线）、不同武器后坐差异、手柄 LT 开镜、结果与暂停界面相机处理 | 验收达标 |

> 暂停/升级/结果界面：相机在 `GameManager.SetState` 中 `timeScale=0` 不更新玩法，但 `LateUpdate` 仍跑；建议这些状态下 `zoomTarget=0`（收镜）并停止后坐力，避免暗角残留。可在 `CameraRig` 检测 `!game.IsPlaying` 时强制 `zoomTarget = 0`。

---

## 5. 验收标准

1. 角色稳定出现在画面偏下、前方视野明显大于身后（越肩感）。
2. 按住右键（/手柄 LT）平滑「开镜」：`orthographicSize` 由 `camBaseSize`→`camAdsSize`，并出现瞄准镜暗角；松开恢复。
3. 每次开火有可感知的后坐力上抬 + 衰减，不漂移、不累积失控。
4. `WASD` 为相机相对：W 朝瞄准方向、S 后退、A/D 平移；移动手感与之前一致（速度/闪避不受影响）。
5. 命中/弹道逻辑不变（仍走 `ScreenToWorldPoint`），射击精度不受影响。
6. 2D 程序化美术、UI、玩法循环（波次/Boss/升级）全部保持可用。
7. 所有新参数在 `GameConfig` 面板可调，且通过 `Validate`。

---

## 6. 风险与回退

- **正交 + 俯倾压扁精灵**：用 §3.6 广告牌朝向相机解决；若仍不理想，Phase 1 先 `camPitchDeg=0`（仅前导+越肩+缩放，零风险即可上手）。
- **前导/偏移方向反了**：`camBack` 取负、`right` 符号翻转即可，均为 `GameConfig`/单符号调整，不影响结构。
- **边界夹紧抖动**：夹紧用「当前 `orthographicSize`」计算可见半径（已在 §3.2 处理），缩放时不会穿帮。
- **想用透视 FOV 缩放（更接近 PUBG 镜头）**：把 `Camera.orthographic=false`，`orthographicSize` 换成 `fieldOfView`（`camBaseFov`/`camAdsFov`），`ScreenToWorldPoint` 改为对地面平面做射线检测。这是更大改动，**不在本次必做范围**，仅在 Phase 2 之后作为可选增强。

---

## 7. 改动文件清单

| 文件 | 改动类型 |
|---|---|
| `Assets/Scripts/Core/GameConfig.cs` | 新增相机参数段 + Validate |
| `Assets/Scripts/Core/CameraRig.cs` | 重写（越肩/缩放/后坐力） |
| `Assets/Scripts/Core/InputReader.cs` | 新增 `AimDownSights` |
| `Assets/Scripts/Player/PlayerController.cs` | 相机相对移动 + `SetAds` |
| `Assets/Scripts/Weapons/WeaponController.cs` | 开火 `AddRecoil` |
| `Assets/Scripts/Core/CharacterVisual.cs` | 广告牌 + 背面精灵（Phase 2） |
| `Assets/Scripts/Core/ProceduralArt.cs` | 地面纵深/视差（Phase 2） |
| `Assets/Scripts/UI/HUDController.cs` | 中心准星 + 瞄准镜暗角 + 提示文案 |

> 不动：`Rigidbody2D` 玩法、敌人 AI、波次/Boss、升级系统、存档。保留全部 2D 资源。
