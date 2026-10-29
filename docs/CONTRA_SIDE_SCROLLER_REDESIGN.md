# 《三角洲契约 / DELTA PACT》横版闯关化整改方案
### —— 从「俯视波次竞技场」转向「魂斗罗式主题关卡 + BOSS 战」

> **给 Codex 的执行摘要**：本方案把现有俯视 twin-stick 游戏改造为**横版卷轴闯关射击（Contra-like）**。
> 核心工作量在四处：① `PlayerController` 重写为平台跳跃物理（重力/跳跃/平台）；② 新增 `LevelManager` 分段式关卡系统（5 主题关卡）；③ 新增 `CharacterDefinition` 多角色选择；④ `BossController` 重写为多阶段攻击模式。
> 视角方案采用 2D 语境下的三档切换（远景横版 / 越肩近景 / 第一人称开镜），全部保留现有正交 2D 管线与程序化美术。
> **允许替换/删除的现有文件**：`WaveManager.cs`（退役）、`CameraRig.cs`（重写）、`RunModel.cs`（改造成关卡进度）。
> **保留复用**：`GameConfig`、`WeaponController`、`Health`、`Projectile(Pool)`、`CombatEffects`、`GameAudio`、`ExperienceManager`、`HUDController`、`InputReader`（扩展）。

---

## 0. 当前实现状态与测试边界（重要，避免误读验收结论）

以下为移交时游戏的**真实状态**，Codex 在评估"已验证"范围时必须据此收敛断言：

### 0.1 自动化测试覆盖边界（`RuntimeSmokeTest` / `-moriSmoke`）
- 在 **Unity Editor Play Mode** 下运行，使用**临时「一波 5 秒」配置**（非默认十波、非默认难度）
- Boss 阶段测试依赖**测试无敌 + 伤害 API**（已内置），因此**不代表人工默认难度下真实通关**
- **未验证项**：① 真人鼠标 / 实体手柄的长时间手感；② 真实十波正常难度完整通关；③ 失焦 / 多显示器下的实际人工操作；④ 音效听感；⑤ 帧率与长时运行性能；⑥ 跨电脑兼容性；⑦ 非 16:9 分辨率下的布局
- 结论：自动检查通过 = 冒烟级稳定性达标，**不可解读为「默认难度已人工通关 / 手感已认证」**

### 0.2 资产与表现层仍为占位
- **角色、场景、特效目前全部是程序化占位**（对应 `ProceduralArt` 203 行级生成），**尚未达到参考图或《和平精英》级别的精致度**
- **三档视角为 2D 正交模拟**（远景 / 越肩 / 开镜），**真正的三维第一人称尚未实现**（§3 已说明真 3D 第一人称与 2D 精灵管线不兼容，需整体转 3D 管线才可达标）
- **尚未完成**：正式动漫风格素材、角色逐帧/骨骼动画、精细 3D 建模、制作级后期处理（Bloom/色彩分级/LUT）
- **后续仍需制作与验收**：正式动漫素材替换、人工手感平衡、实体手柄长时间实测、跨电脑性能验收
- 本方案 §7 的精致化路线（URP 2D 光照 / 法线 / 后期）正是对这部分占位的升级，属 **P2 待办**，当前默认资产未改

> 移交原则：任何"通关验证 / 手感认证 / 画面评级"结论，必须由人工在默认难度下实测得出，自动化结果仅作冒烟参考。

---

## 1. 现状诊断（基于代码审计，2026-10-24）

| 模块 | 现状 | 与目标的差距 |
|---|---|---|
| 移动 | `PlayerController`（90 行）：Rigidbody2D + `gravityScale=0`，俯视八向移动 | **缺重力、跳跃、平台碰撞**，需重写 |
| 关卡结构 | `WaveManager`：单一竞技场（`arenaHalfSize` 22×16）+ 10 波刷怪 + 1 Boss | **无关卡概念、无推进、无主题**，整体退役 |
| 相机 | `CameraRig`：越肩 2.5D 正交（已按上一轮方案改造） | 需改为横版跟随 + 视角切换 |
| Boss | `BossController`（81 行）：**无阶段、无攻击模式切换**，纯站桩输出 | 需重写：3 阶段 + 独立招式库 |
| 敌人 | `EnemyAI`（114 行）：5 类（近战/远程/狙击/自爆/Boss），俯视索敌 | 需加地面巡逻/跳跃/飞行等横版行为 |
| 角色 | 单一角色，`GameConfig.playerSprite/playerBackSprite` 两张程序化精灵 | **无多角色系统** |
| 美术 | `ProceduralArt`（203 行）纯程序化精灵；无视差、无 2D 光照、无后期 | 距「和平精英级精致度」差距最大，见 §7 |
| 成长 | `ExperienceManager` 升级三选一（火焰弹/冰冻/电磁脉冲） | 可保留，改为关卡内掉落 |

**结论**：玩法骨架（武器/弹道/特效/成长/HUD）质量尚可，可整体保留；**移动、关卡、Boss、角色、视觉**五条线需要新建或重写。

---

## 2. 核心玩法转换：俯视 → 横版魂斗罗

### 2.1 PlayerController 重写清单
- **物理**：`gravityScale` 恢复（建议 `4.5`），新增 `jumpVelocity=11`、二段跳（可配置开关）
- **手感三件套（必做）**：
  - **Coyote Time** `0.09s`（离开平台后仍可跳）
  - **Jump Buffer** `0.12s`（落地前按跳缓存）
  - **可变跳跃高度**（松开跳跃键时 `velocity.y *= 0.45f`）
- **横版动作**：跑/跳/下蹲（S 或↓）/ 下平台（下+跳）/ 空中翻滚（保留原 Dodge，改为水平闪避 + 无敌帧）
- **八向瞄准**：保留指针瞄准 + 新增手柄右摇杆；站立时上下斜瞄准（↑=45°↑，↗=斜上），下蹲时贴地平射
- **碰撞**：`CircleCollider2D`（身体）+ `BoxCollider2D`（脚底传感器 `feetSensor`），平台用 `CompositeCollider2D` + 单向平台（`PlatformEffector2D`）

### 2.2 关卡推进模型（替代 WaveManager）
```
WorldSelect → Stage(1..5) → Checkpoint(每关2-3个) → BossGate → Clear
```
- 横向卷轴推进，右边界解锁式（击杀区域敌人/摧毁封锁门后推进，Contra 式锁敌设计）
- **死亡=回检查点**（掉落收集品保留 50%），通关星级按「用时/死亡数/击杀数」评定 S/A/B/C

---

## 3. 视角切换方案（2D 语境，诚实可行）

真 3D 第一人称与 2D 精灵管线不兼容，本方案给出**三档正交视角切换**（`Tab` / `View` 键循环），全部保留现有正交管线：

| 模式 | 视野 | 实现要点 |
|---|---|---|
| **战术远景**（默认第三人称） | `camSize≈9`，角色居中偏左 30% | 横版跟随 + 前进方向前导 `lead≈2.2` + 垂直死区 |
| **越肩近景**（第三人称近战） | `camSize≈5.5`，角色居中 | 瞄准时自动微推近，增强代入感 |
| **第一人称开镜**（ADS） | `camSize≈2.8` + 瞄准镜暗角遮罩 | 复用已实现的 `camAdsSize` 逻辑；镜内显示激光测距与弱点标记 |

> 演进路径备注：若未来追求真 3D 视角，需整体转 3D 管线（本方案不含）。当前三档切换已覆盖「第三人称全景 ↔ 第一人称瞄准」的核心体验诉求。

---

## 4. 多角色系统（CharacterDefinition）

新增 `CharacterDefinition`（ScriptableObject 或并入 GameConfig 数组），**4 名可选角色**：

| 角色 | 定位 | 数值差异 | 程序化视觉差异 |
|---|---|---|---|
| **DELTA / 契约执行者** | 均衡 | 无 | 现有蓝白配色 |
| **VANGUARD / 重装壁垒** | 坦克 | HP×1.4，移速×0.85，闪避冷却+0.5s | 重甲体型×1.15，橙红配色，肩甲像素 |
| **WRAITH / 幽影侦察** | 敏捷 | 移速×1.2，二段跳默认开启，HP×0.75 | 纤瘦体型×0.92，青紫配色，围巾拖尾 |
| **HAVOC / 爆破专家** | 火力 | 投掷物×2，弹匣×1.3，换弹+20% | 宽体+背挂弹箱，黄黑警示纹 |

- 解锁条件：初始 2 名，通关第 2/4 关各解锁 1 名（`PlayerPrefs` 持久化）
- `ProceduralArt` 扩展 `GenerateCharacter(palette, build)`：体型系数 + 3 色板 + 配件层（肩甲/围巾/背包），生成 idle/run/jump/crouch 四态帧（程序化骨骼摆动即可，无需逐帧绘制）

---

## 5. 关卡系统：5 大主题关卡（分区分段装配）

### 5.1 技术方案：分段式关卡（Segment-based）
- 每关由 **关卡片段（Segment）** 拼接：每个 Segment 是一段 24~40m 宽的预制布局（平台阵列+敌人波+机关），数据驱动（`SegmentDefinition` 数组），同主题 Segment 池内随机抽取 + 手工 Boss 前固定段
- 新增文件：`Level/LevelManager.cs`、`Level/SegmentDefinition.cs`、`Level/SegmentBuilder.cs`、`Level/ThemeProfile.cs`

### 5.2 五关主题与专属机制

| 关 | 主题 | 色板基调 | 专属机制 | 关卡 BOSS |
|---|---|---|---|---|
| 1 | **城市废墟**（教学） | 灰蓝+余烬橙 | 崩塌楼房跳台、燃烧油桶（可引爆清场） | **装甲蛮兵 "破碎者"** |
| 2 | **工厂蜂巢** | 冷钢青+荧光绿 | 传送带（顺/逆向影响移速）、液压冲程柱、电磁门开关 | **铸造母巢 "熔核"** |
| 3 | **寒峰隧道** | 冰蓝+暗紫 | 滑冰地面（摩擦×0.3）、暴风雪周期遮蔽视野、易碎冰台 | **轨道猎手 "霜牙"** |
| 4 | **未来都市** | 霓虹粉+深蓝 | 垂直电梯段（竖版攀爬）、激光栅栏时序、全息假墙 | **歼灭核心 "零点"**（现有 Boss 重制） |
| 5 | **最终要塞** | 暗红+合金黑 | 前四关机制混合轮转、敌兵空投走廊 | **要塞主宰 "终末回响"**（三形态） |

### 5.3 场景层次（每关必备 5 层视差）
`天空层(0.05) → 远景剪影(0.2) → 中景建筑(0.5) → 玩法层(1.0) → 前景遮挡(1.3，半透明)`
程序化生成：`ProceduralArt.GenerateParallax(theme)` 按主题色板输出各层纹理（窗格/管道/山脊剪影），一次生成、滚动复用。

---

## 6. BOSS 战设计规范（每关 1 个，3 阶段制）

统一骨架：`BossController` 重写为状态机（`Idle → PatternX → Recover → ... → PhaseShift`），血量 100%/60%/30% 切换阶段（换阶段：无敌 2s + 全屏闪光 + 阶段名弹出 + 音乐分层切换）。

| BOSS | 阶段1 | 阶段2（60%） | 阶段3（30%） |
|---|---|---|---|
| 破碎者 | 冲锋撞 + 落石 | 冲锋×2 连击 + 震地波（需跳跃） | 狂暴连冲 + 顶部坍塌（落点预警圈） |
| 熔核 | 伸缩铆钉炮（扇形弹幕） | 熔岩池（地面禁区）+ 召唤维修蜂 | 过热自爆倒计时 + 全场传送带反转 |
| 霜牙 | 轨道滑行扫射 | 冰锥天降（预警影子）+ 冻结射线（冻结操作 1s） | 隧道黑暗 + 声呐圈暴露位置 |
| 零点 | 激光栅栏矩阵 | 分身（2 假 1 真，真身受击闪白） | 时间迟滞场（玩家弹速×0.6） |
| 终末回响 | 前四 Boss 招式轮换 | 二形态：飞行 + 追踪导弹群 | 三形态：心核暴露 + 双臂独立血条 |

**设计红线**：每阶段至少 1 个「需要玩家换打法」的机制（强制跳跃/强制走位/弱点转移），禁止纯站桩。

---

## 7. 画面精致度路线（对标和平精英的可达方案）

**现实说明**：程序化美术有上限。建议**双轨**：本方案先做「程序化精致化」（零资产依赖），Codex 完成后若不够，再外购/生成精灵资产替换（`GameConfig` 已有 sprite 槽位，直接可换）。

1. **URP 2D 光照**（最大画质杠杆）：`Universal Render Pipeline → 2D Renderer`，每关 1 个全局光 + 玩家跟随点光 + 枪口动态光 + 主题色补光；升级包需在 Hub 勾选 2D Light 包
2. **精灵法线贴图**：`ProceduralArt` 生成精灵时同步算法线图（凸起部分亮度偏移即可），配合 2D 光照产生体积感
3. **后期处理 Volume**：Bloom（霓虹/枪口）+ Vignette + 色彩分级（每主题一套 LUT 参数，进 `ThemeProfile`）
4. **角色动画升级**：程序化骨骼（头/躯干/双臂/双腿 7 节点 IK 摆动）替代单帧贴图 → 跑跳蹲平滑过渡；武器独立挂点随瞄准旋转
5. **粒子预算**：击杀爆裂 + 弹壳抛出 + 落地尘土 + 跳跃气浪（复用 `CombatEffects`，扩 2 个粒子预设）
6. **敌人精致化**：现有 5 类加「装甲板分层 + 受击部位闪白 + 死亡解体动画（部件散落）」

---

## 8. 手感与重复游玩价值

- **检查点系统**：旗帜物件，触碰存档（位置+血量+弹药）
- **评分与排行榜**：本地 `PlayerPrefs` 每关最佳 S 评级 + 速通计时
- **难度三档**：新兵/老兵/契约（敌人伤害×1/×1.5/×2.2，Boss 阶段血量阈值提前）
- **解锁循环**：评级 S 解锁角色配色 / 收集品（每关 3 个隐藏芯片）解锁武器皮肤
- **周挑战（可选 P2）**：每日随机 Segment 种子 + 固定角色 + 计分上传本地榜

---

## 9. GameConfig 新增字段（补丁清单）

```csharp
[Header("Platformer movement")]
[Min(0.1f)]  public float gravityScale = 4.5f;
[Min(1f)]    public float jumpVelocity = 11f;
public bool  allowDoubleJump = false;
[Range(0f, 0.3f)] public float coyoteTime = 0.09f;
[Range(0f, 0.3f)] public float jumpBuffer = 0.12f;
[Range(0.5f, 1f)] public float jumpCutMultiplier = 0.45f;
[Range(0.2f, 1f)] public float iceFrictionFactor = 0.3f;   // 主题机制用

[Header("View modes")]
public ViewMode defaultView = ViewMode.Tactical;   // Tactical / Shoulder / IronSight
[Range(2f, 12f)] public float viewTacticalSize = 9f;
[Range(1.5f, 8f)] public float viewShoulderSize = 5.5f;
[Range(1f, 5f)]  public float viewIronSightSize = 2.8f;

[Header("Characters")]
public CharacterDefinition[] characters;   // 4 名，见 §4

[Header("Level / theme")]
public ThemeProfile[] themes;              // 5 关主题，见 §5
[Range(0.5f, 3f)] public float parallaxSky = 0.05f;  // 各层系数或并入 ThemeProfile
```
`Validate()` 同步扩展：四角色非空、五主题非空、跳跃数值有限且为正。

---

## 10. 文件改动清单与分期交付

| 分期 | 内容 | 涉及文件 |
|---|---|---|
| **P0 纵切片**（先做，验收第 1 关） | 平台物理 + 横版相机三档 + 第 1 关 Segment 系统 + 1 号 BOSS 三阶段 + DELTA 单角色 | 重写 `PlayerController`/`CameraRig`/`BossController`；新建 `Level/*`；退役 `WaveManager`（保留类壳以免引用炸裂，标注 Obsolete） |
| **P1 内容补全** | 关卡 2-5 + 主题机制 + 4 角色 + 敌人横版行为扩展 + 解锁存档 | 新建 `CharacterDefinition`、`ThemeProfile` 数据；改 `ProceduralArt`/`EnemyAI` |
| **P2 精致化** | URP 2D 光照 + 法线 + 后期 + 粒子 + 评分/难度/收集品 | `ProceduralArt`/`CombatEffects`/`HUDController`/`GameConfig` |

**验收标准（P0）**：第 1 关可从起点玩到 BOSS 击杀并结算；三档视角切换无跳变； coyote/buffer 手感成立；BOSS 三阶段招式各自可辨识；死亡回检查点正确恢复；720p/1080p 自动检查扩展到「通关冒烟测试」（`RuntimeSmokeTest` 加 `-moriStage1` 模式，模拟：跳跃×N → 检查点 → BOSS 击杀）。

**风险提示**：
1. `WaveManager` 退役时 `GameManager` 引用需同步改（约 6 处调用点）
2. URP 2D 光照需确认工程 URP Asset 的 Renderer 类型（当前为普通 Renderer，切 2D Renderer 后老材质需 `Sprite-Lit-Default`）
3. 程序化角色动画是 P2 中最耗时项，若工期紧可先保 P0/P1，动画沿用双帧摆动
