# Inspector、逐阶段测试与验收

本次相机优化优先阅读 `CAMERA_IMPLEMENTATION_AND_TESTS.md`：含具体文件、Inspector 全部相机参数、操作变化、测试步骤与验收标准。新版本采用鼠标水平转向、中心准星、右键/LT 开镜以及相对 WASD，三档分辨率各 94 项独立 Player 检查通过。下面旧输入说明属于首次俯视版历史。

更新：2026-10-24。真实工程 `C:\UnityProjects\MoriEchoes` 已在 Unity 6000.6.7f1 编译并构建，三档分辨率独立 Player 各通过 51 项自动检查。实际范围见 `UNITY_INTEGRATION_REPORT.md`；以下“应当”为完整验收标准，未逐项全部通过。Editor Play Mode 与默认十波正常难度人工通关仍未执行。

## 通用前置与运行结构

- 使用 Universal 2D，安装兼容 Input System、Universal RP、Unity UI，Active Input Handling = Input System Package (New)。
- 运行搭建工具后，选中 `Assets/Settings/MoriEchoesConfig.asset`。Inspector 的 `Sprite Material` 应引用 `MoriEchoesSpriteUnlit.mat`；材质 Shader 为 `Universal Render Pipeline/2D/Sprite-Unlit-Default`。
- 可以把合法中文字体资源拖入 `Ui Font`。留空时尝试操作系统中文字体，目标 Windows 机器可能缺字，发行前必须固定嵌入字体并核实授权。
- 打开每个入口场景，选中 `MoriEchoesBootstrap`：`Config` 引用配置；Boot/MainMenu 的 `Start In Menu` 勾选，Battle_01 不勾选。根对象启用、位置 `(0,0,0)`、旋转零、缩放一。
- 不要添加第二个 GameManager、相机、AudioListener 或 EventSystem。自动生成场景本来不包含这些运行对象。
- 运行层级：`MoriEchoesBootstrap` 下有 Arena、MoriEchoesCamera、Audio、Effects、UI，开始后新增 Run/Player、Projectiles、Pickups 和敌人。
- Play 中改动的运行对象不持久化。修改默认参数必须退出 Play，编辑 GameConfig 资产并保存。

## A. 环境审计

**文件**：`Tools/Import-IntoUnityProject.ps1`、`Assets/Editor/MoriEchoesSetup.cs`、原 `docs/ENVIRONMENT_CHECK.md`。

**Inspector/Editor**：检查工程根目录三要素及 ProjectVersion；执行 Check Environment，记录 Console 的实际 Unity/Input System 版本。包管理器确认 UGUI/URP。

**测试**：先对导入脚本执行默认 dry-run；有同名冲突时保持原文件。对原文件做 SHA256 对比。

**验收**：真实工程可打开，版本属于 6000.x，Console 无红错，未覆盖原代码/场景。当前仅完成文件系统审计；缺 Unity 是明确阻塞。

## B. 场景与核心

**文件**：`Assets/Scripts/Core/GameBootstrap.cs`、`GameManager.cs`、`GameConfig.cs`、`ProceduralArt.cs`、`CameraRig.cs`；`Assets/Editor/MoriEchoesSetup.cs`；工具生成的三个 `.unity` 场景。

**Inspector/Editor**：执行 Prepare Prototype Scenes 和 Verify Scene Bindings；按通用前置检查 Config。Arena Half Size 默认 `(22,16)`。自动相机 Orthographic，默认 Size 7.7；四面墙有 BoxCollider2D，无需在编辑状态逐个搭建。

**测试**：分别单独打开菜单和战斗入口；场地、角色、相机、边界均应出现。朝四面墙持续走、闪避，检查不能越界。检查相机边缘、重复 AudioListener、Missing Script 和 Console。

**验收**：一个有效 Bootstrap，边界阻挡，游戏对象只生成一套；旧场景不被覆盖；重开不增加重复世界。当前场景生成未执行。

## C. 玩家移动、瞄准、生命、闪避、基础射击

**文件**：`Assets/Scripts/Player/PlayerController.cs`、`Assets/Scripts/Core/InputReader.cs`、`Assets/Scripts/Combat/Health.cs`、`Projectile.cs`、`ProjectilePool.cs`、`Assets/Scripts/Weapons/WeaponController.cs`。

**Inspector/Editor**：GameConfig：Player Health 100、Move Speed 5、Dodge Duration 0.18、Dodge Cooldown 2.5、Dodge Speed Multiplier 3.2。Player 的 Rigidbody2D、CircleCollider2D、Health 由 RequireComponent 自动添加；运行时 Gravity Scale 0、Freeze Rotation、Continuous、Interpolate、半径 0.29。

**测试**：WASD 正交/斜向各移动相同时间，斜向不加速；绕鼠标瞄准 360°，枪朝正确方向；按住左键；墙前射击不能穿墙。受击减血；Space 有短暂免伤与冷却，连按不能无限闪避；暂停时不能受伤/移动。

**验收**：输入正常，无旧 Input Manager 依赖；子弹命中而非穿过目标；生命只结算一次死亡；无越界。当前仅模型/语法检查，物理与输入未验证。

## D. 敌人、精英与 Boss

**文件**：`Assets/Scripts/Enemies/EnemyAI.cs`、`BossController.cs`、`Assets/Scripts/Core/CharacterVisual.cs`、`WaveManager.cs`。

**Inspector/Editor**：不需手工敌人 Prefab。敌人运行时创建动态 Rigidbody2D、圆形碰撞与 Health。默认近战 35HP/2.3速/10伤，远程 25HP/1.5速/8伤，突袭 18HP/3.4速/8伤，精英 240HP，Boss 900HP。普通敌人按波次增长生命。

**测试**：观察追击、远程保持距离并射击、突袭速度、精英三发；确认出生预警约 0.85s、期间不主动攻击。远程弹不能打同阵营。Boss 区域轰击预警后命中锁定区域，另有锁定方向冲锋；降低至 40%HP 以下检查节奏加快。

**验收**：至少三类普通敌人行为可辨；接触伤害有节流；死亡只计一次击杀/掉落；Boss 至少两种预警攻击且可回避。当前 AI/预警/难度未试玩。

## E. 经验与九种随机升级

**文件**：`Assets/Scripts/Roguelite/ExperienceManager.cs`、`ExperiencePickup.cs`、`PickupPool.cs`、`Assets/Scripts/Core/RunModel.cs`。

**Inspector/Editor**：Maximum Pickups 默认 128。阈值为 12、20、28……；6 单位内吸附，0.55 单位内拾取。没有单独 UpgradeData 资产，当前九种目录由 RunModel 明确定义，参数不散落到 Inspector。

**测试**：拾取到阈值，三张卡片应互不重复并暂停所有战斗；选择一次只消耗一次待选次数；大额经验保留溢出并逐次选择。逐项核对：射速、穿透、伤害、闪避冷却、弹匣、精度/暴击、Q 脉冲、生命上限/回血、移动与射速。重开后全部清零。

**验收**：九种升级均改变真实行为，不只是文案；同次三选一无重复；连升无丢失。纯模型测试已覆盖抽样/属性/经验队列，Unity 实际应用仍未验证。

## F. 波次与游戏流程

**文件**：`Assets/Scripts/Core/WaveManager.cs`、`GameManager.cs`、`RunModel.cs`、`Assets/Scripts/UI/HUDController.cs`。

**Inspector/Editor**：Waves Before Boss 10、Wave Seconds 55、Intermission Seconds 4、Maximum Enemies 36。首波前 2 秒准备。计时型生存波次，不要求清空全部存活敌人才进入下一波，间歇仍可能有残余敌人。

**测试**：正式配置走完菜单→战斗→升级→十波→Boss→胜利→重开；故意死亡验证失败→菜单。Esc 暂停/继续至少十次，重开三局，时间/等级/掉落/旧敌人不能残留。

快速冒烟：仅在测试副本 Config 将 Waves Before Boss 改为 1、Wave Seconds 改为 5、Player Health 提高后测试 Boss；不得把这条快速路径当作正式十波平衡验收。测试后恢复默认或改回原引用。

**验收**：全部出口可用，暂停冻结战斗计时、技能冷却、弹道、敌人；Boss 死亡胜利，玩家死亡失败，无结果界面软锁。当前完整流程未执行。

## G. 三种武器

**文件**：`Assets/Scripts/Weapons/WeaponController.cs`、`Assets/Scripts/Core/GameConfig.cs`、`Assets/Scripts/Combat/Projectile.cs`、`ProjectilePool.cs`。

**Inspector/Editor**：Config 的 Weapons 必须是三个元素，顺序 AR/SG/SR：

| 武器 | 伤害 | 每秒发射 | 弹匣 | 装弹秒 | 弹丸/枪 | 散布度 | 命中目标数 | 弹速 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AR-07 | 12 | 7 | 28 | 1.2 | 1 | 2 | 1 | 24 |
| SG-12 | 5 | 1.1 | 6 | 1.5 | 6 | 22 | 1 | 20 |
| SR-09 | 45 | 1.2 | 8 | 1.6 | 1 | 0.4 | 2 | 38 |

散布为左右总角范围；暴击基础 5%，伤害倍率 1.8。弹药有限/备弹无限。换枪会取消正在装填但不凭空补弹；升级弹匣需下一次装填获得新增空间。

**测试**：1/2/3 切换，三枪独立剩余弹药；R 装填、空仓触发装填，期间不射击；切枪不可绕过冷却。SG 六弹消耗一发弹药；SR 同弹最多命中两个不同目标，升级后增加；墙体仍阻挡。池满不能扣弹但不发射。

**验收**：参数和观感可辨、无重复命中同一敌人、无负弹药、同队免伤。当前枪感与碰撞未实测。

## H. UI

**文件**：`Assets/Scripts/UI/HUDController.cs`、`Assets/Scripts/Core/GameManager.cs`。

**Inspector/Editor**：运行时 Canvas：Screen Space Overlay，CanvasScaler Scale With Screen Size，1920×1080，Match 0.5。EventSystem + InputSystemUIInputModule 自动生成，不要放旧 StandaloneInputModule。Ui Font 推荐填有授权的中文字体。

**测试**：1920×1080、1600×900、1280×720，窗口切换与字体缺字；核对血条、经验、弹药、装弹、波次、Boss 条、冷却；逐个点开始、退出、升级、继续、重开、返回菜单与音量。可选手柄测试导航/确认及游戏输入串扰。

**验收**：主要目标分辨率无截字/遮挡，鼠标和导航可选择，升级/结算按钮均有效。4:3、超宽、移动端不在本轮保证范围。尚无画面或 UI 截图验收。

## I. 特效、音频与资源生命周期

**文件**：`Assets/Scripts/Core/CombatEffects.cs`、`GameAudio.cs`、`ProceduralArt.cs`、`CharacterVisual.cs`、`CameraRig.cs`；弹丸与拾取对象池。

**Inspector/Editor**：Master Volume 0.6、Screen Shake 0.35，暂停可调音量；Maximum Projectiles 192、Maximum Pickups 128。特效固定 144 槽，其中 16 预警专用；伤害数字 24 槽，音效 12 音源。占位画面使用 Unlit，不需 2D Light 即可显示，不代表正式灯光管线已完成。

**测试**：持续射击并频繁闪避，检查枪火、命中、飘字、受击闪色、残影、脉冲和预警；预警不被普通粒子挤占。音量保存/恢复、暂停后音效恢复、不爆音。Profiler 检查十分钟对象数、分配、CPU/GPU，重开和退出后无资源持续增长。

**验收**：没有无界子弹/拾取/特效增长；目标机按实测达到稳定 60FPS 才能标记性能通过。敌人目前限量生成/销毁而非敌人池；高频 UI 字符串仍可能分配，需要真实 Profiler 后优化。尚未实测性能与音画。

## J. 正式美术接入

**文件**：`docs/ART_INTEGRATION.md`、原 `docs/ART_PIPELINE.md`、Config 的 Player Sprite、Enemy Sprites[5]、Weapon Sprites[3]。

**Inspector/Editor**：按接入文档导入单帧透明 PNG 并赋槽位，确认 PPU、尺寸与 pivot；多帧动画另建 Animator 后接入，当前没有假装已接好的动画状态机。

**测试**：替换每类精灵后复测轮廓、脚底、武器原点、朝向、碰撞与 UI 头像；授权、来源、版本完整登记。

**验收**：正式素材存在、已正确绑定、有运行截图与授权记录后才通过。当前只有占位素材，此阶段未完成正式导入。

## K. 验收

**文件**：`Tools/Validate-Source.ps1`、`Tools/SourceValidator.cs`、`Tools/Tests/RunModelTests.cs`、`docs/ACCEPTANCE_REPORT.md`。

**Inspector/Editor**：Console 关闭 Collapse 方便排查；每项实测记录 Editor 版本、Input/URP/UGUI 版本、机器与分辨率，附日志和截图。不要用诊断工具结果替代游戏运行证据。

**测试/验收**：按上述逐项和原 `DEFINITION_OF_DONE.md` 复测。全部主流程通过、无红错，才标记可玩 MVP 完成。当前独立模型与语法通过；Unity 项全部未执行。

## L. Windows 构建

**文件**：`Assets/Editor/MoriEchoesSetup.cs`、`docs/BUILD_REPORT.md`；未来输出 `Builds/Windows/MoriEchoes_<时间戳>/MoriEchoes.exe`。

**Inspector/Editor**：安装该版本 Windows Build Support；执行 Verify Scene Bindings；固定合法中文字体，按构建文档设置 Build Profiles Windows x64。

**测试**：运行真实生成的 EXE，走完完整流程，关闭重开，检查无 Editor 依赖、音量/最高击杀本地保存，分发时携带整个输出目录。

**验收**：BuildPipeline 成功且 EXE 存在只代表构建成功，独立运行验收需另记。当前既未构建，也未运行 Windows 游戏。
