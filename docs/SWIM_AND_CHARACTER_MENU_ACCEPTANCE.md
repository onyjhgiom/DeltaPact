# 角色技能菜单与第 9、10 关验收（2026-10-28）

工程：`C:\UnityProjects\MoriEchoes`。Unity 6000.6.7f1，Input System 1.20.0。当前保留 DELTA、VANGUARD、WRAITH、HAVOC、KALT、VEIL、SERA、ROOK 八人、九种武器与前八关的原配置，补齐海面和深渊两个配置项。第 9、10 关没有隐藏路线。

## 实际文件

| 范围 | 工程内具体文件 |
|---|---|
| 首页菜单、技能说明、冷却 | `Assets/Scripts/UI/HUDController.cs`、`Assets/Scripts/Player/CharacterAbilityCatalog.cs`、`CharacterSkill.cs`、`CharacterTechniqueController.cs` |
| 水中运动和相机 | `Assets/Scripts/Player/SidePlayerMotor.cs`、`Assets/Scripts/Core/SideCameraDirector.cs` |
| 水关布局、局部环境效果、氧气和水压 | `Assets/Scripts/Level/SwimStageDirector.cs` |
| 敌人水层、控制效果、Boss 三相 | `Assets/Scripts/Enemies/SwimEnemyController.cs`、`SwimBossController.cs`、`EnemyAI.cs`；复用既有 `BossController.cs` 的分流 |
| 原有关卡 Boss 锁血通关和追踪预警 | `Assets/Scripts/Core/GameManager.cs`、`Assets/Scripts/Enemies/SideBossController.cs`、`VerticalBossController.cs`、`BossField.cs` |
| 水中弹幕、冲击和有限池 | `Assets/Scripts/Core/CombatEffects.cs`、`Assets/Scripts/Combat/Projectile.cs`、`ProjectilePool.cs`、`Assets/Scripts/Core/SideSpriteAnimator.cs` |
| 浮动补给 | `Assets/Scripts/Weapons/WeaponCapsule.cs` |
| 主 Boss 登记、保留原八关后补齐新关 | `Assets/Scripts/Level/LevelManager.cs`、`VerticalStageDirector.cs`、`Assets/Editor/SideScrollerSetup.cs`、`Assets/Settings/MoriEchoesConfig.asset` |
| 实际运行诊断 | `Assets/Scripts/Diagnostics/SwimCampaignSmokeTest.cs`、`VerticalCampaignSmokeTest.cs`、`SideScrollerSmokeTest.cs`、`Tools/Test-SwimCampaign.ps1` |

表格中缩写文件名和前一个完整文件同目录；交付文件清单和哈希另见 `Validation/SwimMenu_20261028_Delivery/file-changes.json`。

## 首页人物选择

点击左侧人物卡，右侧立即显示所选人物的 Q 主技能、F 战术技能、C 战术技能和 X 终结技。每项均显示技能名、作用说明；Q/F/C 显示实际冷却，X 显示满 100 能量释放以及攻击/受击充能。面板始终可见，无须悬停。人物基础生命、速度、弹匣与装填倍率也随选择刷新。

说明由 `CharacterAbilityCatalog` 提供，冷却直接调用运行组件的 `CooldownFor`，保持战斗实际值和菜单值一致。现有角色名称、原图和数值未因菜单重排而替换。界面使用 1920×1080 参考尺寸；已检查 1280×720 和 1920×1080 截图，无技能说明截断。1280 窗口文字较小，可最大化或按 F11。

Inspector / Editor 步骤：

1. 用指定 Unity 打开工程，打开 `Assets/Scenes/MainMenu/MainMenu.unity`。
2. 选中 `MoriEchoesBootstrap`，确认 `Config` 指向 `Assets/Settings/MoriEchoesConfig.asset`，`Start In Menu` 勾选。配置的 Characters 应为 8 项，Themes 应为 10 项。
3. 进入 Play，UI 由 `HUDController` 运行生成。检查 Canvas 下 `MainMenu` 的 `CharacterAbilityPanel`、`AbilityKey_0..3`、`AbilityTitle_0..3`、`AbilityDescription_0..3`；不需要给八人手工绑定 32 个回调。
4. 逐个选择八人，核对右侧名称、四项技能与冷却。退出 Play 后修改说明应编辑目录类，修改冷却应编辑对应技能组件，不能只改运行中的 Text。

测试 / 验收标准：八人均可选择；切换后无上一人物残留文案；四个按键和效果可读；治疗、位移、弹幕和防御说明与实际组件相符；缩放窗口不使技能说明溢出。自动测试调用真实按钮回调并检查所有八人文本，真人鼠标点选体验仍需人工复核。

## 第 9、10 关结构与控制

每关 8 段，每段 30 米，全长 240 米。前七段按所在段刷出 8–11 名敌人，间隔由约 1.5 秒逐段压缩至约 0.9 秒，并受难度和活跃敌人数上限约束。最后一段进入 Boss 战。敌人按露头、潜行、全水层三类目标深度形成纵向分工。当前基础关卡尚不代表完整手工编排和最终难度验收。

| 关卡 | 运动 / 镜头 | 生存 / 演出 |
|---|---|---|
| 09 风暴海面 | A/D 推进、S 小潜、Space 跃出水面、Shift 水中闪避；波面由 `SurfaceHeightAt` 提供，不读取地面引用。水层限制在波面 −2.2 至 +4.2 米，镜头只作小幅垂直跟随。 | 天光光束、波峰、喷雾；Boss 三相弹幕、浪击和落点轰击；死亡时连续巨浪爆破。 |
| 10 深渊热泉 | WASD / 左摇杆全向游泳，渐进加速，越深阻尼越强；镜头跟随上浮下潜。 | 氧气随深度消耗，热泉附近补氧；缺氧每 2.5 秒或高压深度停留 4 秒触发可承受伤害。生物荧光色调、局部气泡和热泉；Boss 三相扇射、包围弹与热泉柱；死亡时热泉连爆。 |

两关补给在当前水层悬浮，可接触拾取。弹幕带气泡尾迹，射击带喷溅。Boss 在水域/相机纵向软边界内缓动；切相有过渡。半透明红色蓄力区域跟随玩家，到攻击前约 0.22 秒锁定位置，保留最终躲避窗口。普通非 Boss 控制技能能冻结水中小兵。

Boss 仍有生命时调用胜利被阻止；只有主 Boss 生命归零、死亡演出触发后才结算。三相以 66% / 30% 血量分界，测试使用人工设定阈值，不代表真人已正常打通。

Inspector / Editor 步骤：

1. 在配置 Themes 索引 8/9 确认标题为“风暴海面 / 深渊热泉”。原八项保留；准备工具仅在原配置恰为八项时追加默认两项。
2. Play 后菜单选择 09 或 10，点击部署。检查 `LevelManager.Swim` 存在、`SecretRoutes` 为空；第 10 关 `IsUnderwaterStage` 为真，第 9 关为假。
3. 运行时玩家 Rigidbody2D 应为 Gravity Scale 0；水关不可再配置地面跌落死亡规则。海面上下限来自 `SwimStageDirector`；水下范围 −7..8 米。
4. 运行中查看 `SwimEnemyController.DepthRole / PreferredDepth` 与 `SwimBossController.Phase`。这些为运行计算值，不需保存到 Prefab。
5. 当前新两关尚无单独生成的正式场景/Boss 贴图绑定，使用程序化环境和现有兜底 Sprite；需后续美术制作，不能将灰盒表现视作最终动漫美术完成。

手动测试 / 验收标准：

- 海面连续跃出、落回、浅潜，无空引用和穿水坠落；游泳不蹲姿。水下上下反向推进和松开阻尼正常，镜头持续跟随。
- 待氧气降低后靠近底部热泉，看到 +40% 补氧；在高压区观察周期受伤和受击保护，无满血瞬杀。
- 等补给浮在水层，接触即可拾取，不沉到最底部。
- 两关把 Boss 打至三相，观察预警锁定、实际攻击、完整死亡演出；HP > 0 不能提前通过。
- 验证局部气泡和光柱随相机移动，活跃特效和子弹保持池容量内。专项仅证明所测窗口未满池，不是长时压力测试。

## 实际测试记录

最终 Windows 程序：`Builds/Windows/MoriEchoes_20261028_012757_512/MoriEchoes.exe`。Unity BuildPipeline 实际结果 Succeeded，StandaloneWindows64 / Development，0 错误、0 警告，耗时 17.286 秒；构建记录 `docs/BuildReports/Windows_20261028_012757_512.md`，日志 `Logs/Build_SwimMenu_20261028_Final2.log`。构建记录中的“运行未测试”只表示构建工具当时的状态，以下为随后对同一 EXE 的实际 Player 测试。

| 专项 | 通过检查数 | 实际报告目录（均位于 TestResults） |
|---|---:|---|
| 首页八人技能说明、窗口与两水关 | 168 | `Swim_20261028_013538_609` |
| 前五关 Boss 三相、追踪红区、死亡与通关门槛 | 410 | `Encounter_1280x720_20261028_013005_457` |
| 八人、九武器和菜单窗口回归 | 601 | `Arsenal_1280x720_20261028_013840_902` |
| 第六至八关连续攀升与 Boss | 99 | `Vertical_20261028_014411_766` |

共 **1278 项**，四组 `passed=true`、错误列表为空、进程退出码均为 0；各目录的 `process-evidence.json` 记录实际 EXE 路径。检查数是自动断言数量，不是人工测试用例或真人通关次数。水关专项输出 15 张非空运行截图；实际查看了 1280×720 与 1920×1080 的角色技能菜单截图，未见说明截断。

菜单截图：`TestResults/Swim_20261028_013538_609/01_menu_skills_1280.png`、`02_menu_skills_1920.png`。后者为 KALT 的四项技能，前者为 ROOK。全八人的四项说明和文本尺寸均有自动检查。

源码验证 `Validation/source-validation.json`：10040 条纯模型断言、5000 次固定种子抽样、88 个 C# 文件 × 3 套编译条件的语法检查、10 份 PowerShell 解析及 13 项隔离导入安全检查通过。88 个源码哈希与最终工程一致。这些静态检查本身不代替实际 Unity 编译和上述 Player 测试。

历史失败保留：旧构建的 `TestResults/Encounter_1280x720_20261028_011245_692` 未通过，因为测试手工生成 Boss 后未登记为主 Boss，生命归零仍被新的胜利门槛拦截。已统一生产和测试的 `LevelManager.SetBoss` 登记入口，保留生命归零限制，最终同一 EXE 的 410 项复测通过。`Logs/Build_SwimMenu_20261028_Final.log` 为调用 Editor 方法时命名空间错误，修正为 `MoriEchoes.EditorTools` 后在 Final2 日志构建成功；旧失败不记作通过。

复测命令（工程根目录 PowerShell）：

```powershell
.\Tools\Test-SwimCampaign.ps1 -Executable 'C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261028_012757_512\MoriEchoes.exe'
.\Tools\Test-AnimeArt.ps1 -Mode Encounter -Executable 'C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261028_012757_512\MoriEchoes.exe'
.\Tools\Test-AnimeArt.ps1 -Mode Arsenal -Executable 'C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261028_012757_512\MoriEchoes.exe'
.\Tools\Test-VerticalCampaign.ps1 -Executable 'C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261028_012757_512\MoriEchoes.exe'
```

以上命令按顺序执行，测试会显示游戏窗口并自动退出，避免并行可见窗口争抢焦点。也可自行用 Development Player 参数 `-moriSwimSmoke <绝对报告目录>` 启动。测试使用合成键盘、按钮回调、位置/血量/无敌辅助以及加速氧气计时，不写通关存档。真人正常难度通关、实体手柄长测、Editor Play Mode、跨机性能和新两关正式美术均尚未验收。未制作压缩包。

双击根目录 `PLAY_MoriEchoes.cmd` 启动本轮程序，选择人物即可查看技能。完整文件变更清单（25 个 C# 文件、配置、元数据、测试入口、说明与启动器）及 SHA-256 见 `Validation/SwimMenu_20261028_Delivery/file-changes.json` / `.csv`；最终构建和测试证据索引见同目录 `delivery-verification.json`。
