# 横版战役实现、设置与验收

更新：2026-10-24。以 `CONTRA_SIDE_SCROLLER_REDESIGN.md` 为实施规格；旧的俯视角和肩后相机文档仅供回退参考。

## 交付边界

本次把既有原型改为独立横版战役分支：五主题、四角色、三武器、分区门禁、平台跳跃、检查点重试、升级、三阶段 Boss、关卡解锁与评分。运行时生成场景，不要求手工拖入数百个物件。原场景、旧波次逻辑及原 Unlit 材质保留。

三档视角是 **2D 正交战术 / 第三人称近景 / 机瞄模拟**。不把缩放镜头称为真实三维第一人称。本轮仍使用程序化贴图和分层关节动画，未制作或采购商业级动漫角色、全套逐帧动画或 3D 模型；没有达到《和平精英》的美术完成度。

## 文件与运行入口

- 工程：`C:/UnityProjects/MoriEchoes`。
- 配置：`Assets/Settings/MoriEchoesConfig.asset`，原场景 `Assets/Scenes/Boot/Boot.unity`、`Assets/Scenes/MainMenu/MainMenu.unity`、`Assets/Scenes/Battle_01/Battle_01.unity`。
- 编辑器入口：`Assets/Editor/SideScrollerSetup.cs`。Tools → Mori Echoes → Side Scroller → Prepare Config / Build Windows。
- 运行入口：Boot/MainMenu → 选择角色、关卡、难度 → 部署。Battle_01 直接启动当前默认关卡。
- 回退备份：`Validation/SideScrollerBackup_20261024_115759`。旧 Windows 包另存于原有 Builds 目录；新构建始终使用不同时间戳目录。

## 阶段 P0：移动、视角、首关闭环

源码：`Assets/Scripts/Player/SidePlayerMotor.cs`、`Core/SideCameraDirector.cs`、`Core/CameraRig.cs`、`Level/LevelManager.cs`、`Level/SegmentBuilder.cs`、`Enemies/SideBossController.cs`。

Inspector：

1. 打开 MainMenu，选中 MoriEchoesBootstrap，确认 Config 仍引用 `MoriEchoesConfig`，Start In Menu 开启；Battle_01 该选项关闭。
2. 选中配置，Side Scroller 开启；Gravity Scale=3.2，Jump Velocity=13，Coyote Time=0.09，Jump Buffer=0.12，Jump Cut Multiplier=0.45。
3. Default View=Tactical，View Tactical Size=6.2，Shoulder=4.3，Iron Sight=2.8。缩放需满足 IronSight ≤ Shoulder ≤ Tactical。
4. 运行时 Player 由脚本创建 Rigidbody2D、冻结旋转的 CapsuleCollider2D 与 FeetSensor。旧 CircleCollider2D 在横版中禁用，不手工重复添加。

测试：AD 移动；Space 长按/短按比较高度；走出平台边缘后立即跳跃；落地前按跳；S 蹲伏、S+Space 下穿、Shift 冲刺；鼠标瞄准、左键射击；Tab 连续切三档、右键临时机瞄；清完四个封锁区后进入 Boss。

验收：三种跳跃情况可复现；碰撞体符合蹲伏状态；三档保持正交；清敌开门；检查点重试恢复血量/弹药，保留等级、未消费经验减半；破碎者在 60%/30% 血量转换阶段并拥有 2 秒保护。

## 阶段 P1：五关、角色、存档

源码：`Level/CharacterDefinition.cs`、`ThemeProfile.cs`、`SegmentDefinition.cs`、`PlatformSurface.cs`、`StageHazard.cs`、`StageInteractable.cs`、`Enemies/SideEnemyMotor.cs`、`UI/HUDController.cs`。

Inspector：配置 Characters 必须正好 4 项，Themes 必须正好 5 项。预置属性保存在配置中，可独立修改。头像和 Body Sprite 插槽可接入合法的正式素材。初始 DELTA/VANGUARD；通关第 2/4 关解锁 WRAITH/HAVOC。

| 关卡 | 已接入机制 | Boss 核心机制 |
|---|---|---|
| 城市废墟 | 单向平台、崩塌上路、爆燃油桶 | 冲锋、落石、地波、后续连冲 |
| 工厂蜂巢 | 正逆传送带、液压压柱、E 开门 | 扇形炮、熔岩区、维修蜂、过热爆破与反转 |
| 寒峰隧道 | 冰面惯性、碎冰平台、周期风雪 | 滑行扫射、冰锥、冻结、暗场声呐 |
| 未来都市 | 升降台、定时激光、无碰撞全息墙 | 激光矩阵、两分身、弹速迟滞 |
| 最终要塞 | 混合机关 | 前四类招式、飞行导弹、双臂独立血量护盾 |

测试：每关检查特定机关、敌人可追击/远射；Boss 每阶段至少观察一次真实攻击；通关后下一关按钮可用；返回菜单检查解锁；关闭重开应用确认本地保存。难度伤害倍率为 1/1.5/2.2。

验收：五关都有结算和重玩入口；四角色的 HP/速度/二段跳/弹匣差异生效；三枚芯片解锁本关武器色，S 评价解锁当前角色备用配色；本地最佳评分与时间按关卡和难度区分。不能把后续关卡的测试传送或伤害辅助误写成人工完整通关。

## 阶段 P2：程序化表现

源码：`Core/SideArt.cs`、`Core/SideCharacterVisual.cs`、`Level/SidePresentation.cs`、`Core/CombatEffects.cs`、`Core/GameAudio.cs`。

Inspector：运行 Prepare Config 后，Side Sprite Material 使用新增的 Sprite-Lit-Default 材质，旧 Sprite Material 仍用于回退分支。工程使用原有 URP 2D Renderer。运行时主题 Volume 包含 Bloom/Vignette/ColorAdjustments；主相机开启后期。

Side Light Prefab 引用 `Assets/Prefabs/Effects/SideNormalLight.prefab`：Light Type=Point，Normal Map Quality=Accurate，Normal Map Distance=1.5。玩家补光和枪口闪光复用该预制件；不要改动已有 Renderer2D 资源或为此额外引入包。贴图和法线由运行时代码生成，退出时释放。

测试：查看四角色头像与不同体型；跑、跳、蹲时头/身/双臂/双腿关节随动作变化；射击有枪焰、弹壳，死亡有部件粒子；角色精灵拥有 `_NormalMap` 次纹理；五个 ParallaxLayer 的因子为 0.05/0.2/0.5/1/1.3。

验收：720p/1080p 菜单和 HUD 可读；枪口与弹道方向一致；前景不遮挡主要战斗。程序化人像与块面场景仅作原型，正式动漫精灵、完整武器动作、配乐分轨和音效素材仍需后续美术制作及人工体验验收。

## 自动检查与实际记录

- `Tools/Validate-Source.ps1`：纯 C# 数值测试、C# 语法预处理分支扫描、导入保护测试。它不替代 Unity 编译。
- `Tools/Test-SideScroller.ps1 -Executable <实际exe路径>`：以可见窗口运行 1280×720 / 1920×1080，合成键鼠输入；报告写入 `TestResults/Side_*/stage-smoke.json`，截图和 Player.log 同目录。
- `-moriStage1` 仅用于 Development 测试包。测试进度保存在进程内，不覆盖真实战役存档。首关推进使用真实弹道与键盘；测试无敌、后续关卡传送/清敌及 Boss 阶段伤害辅助明确写入报告。
- 为避免开发期间切回桌面打断长测试，测试器会自动恢复偶发失焦暂停；显式 Esc 暂停检查期间关闭此辅助。游戏正常模式的失焦暂停行为保持不变，自动结果不认证真人失焦/多屏体验。
- 首轮构建成功；首轮运行 35 项检查通过，但首关推进超时失败。该结果不是验收通过，正在定位并回归。
- 最终构建路径、分辨率结果与剩余项以本文件后续的实际执行记录为准。

## 人工体验验收（待执行）

正常难度全程不使用无敌/传送完整通关；键盘与手柄输入手感；长时间稳定性、其他 GPU 和电脑；角色/特效是否满足正式美术标准；完整评分平衡和无障碍；真正三维第一人称制作如需启动，须单独投入 3D 角色、关卡和武器动画管线。

## 最终实际执行记录

### Windows 编译和构建

- Unity：6000.6.7f1；StandaloneWindows64 / Development；BuildPipeline **Succeeded，0 错误 / 0 警告**。
- EXE：`C:/UnityProjects/MoriEchoes/Builds/Windows/MoriEchoes_20261024_131549_133/MoriEchoes.exe`。
- 实际包体：174,172,464 字节；BuildPipeline 耗时 14.88 秒（不含编辑器启动和导入）。
- 构建日志：`Logs/SideScrollerBuild_08.log`；报告：`docs/BuildReports/Windows_20261024_131549_133.md`。
- `PLAY_MoriEchoes.cmd` 已指向此实测版本；该目录需整体复制，不能只移动 EXE。

### 可见独立 Player 自动集成

| 实际分辨率 | 检查 | 错误 | 退出码 | 报告 |
|---|---:|---:|---:|---|
| 1280×720 | 107 通过 | 0 | 0 | `TestResults/Side_1280x720_20261024_131712_404/stage-smoke.json` |
| 1920×1080 | 107 通过 | 0 | 0 | `TestResults/Side_1920x1080_20261024_131843_327/stage-smoke.json` |

旧模式回归：同一最终构建使用临时 `sideScroller=false` 配置，以 1600×900 可见窗口执行 `-moriSmoke`，**94 项通过 / 0 错误 / 退出码 0**。报告 `TestResults/Visible_1600x900_20261024_132143_174/runtime-smoke.json`。该测试验证保留的旧相机/波次分支，不把它计入横版通关验收。

真实覆盖：菜单指针选择、角色锁定、重力/胶囊/地面检测、长短跳、土狼时间、落地缓冲、蹲伏、下穿、冲刺无敌、三档正交视角与 ADS、三武器弹药、Esc 暂停恢复、升级三选一、首关真实键鼠推进/弹道击杀/障碍射击、死亡检查点重试、五关装配、五个 Boss 各三阶段与保护时间、Boss 战术视角入镜、迟滞场、最终双臂护盾、胜利结算和解锁、WRAITH 二段跳及角色血量/弹匣差异。

明确辅助：首关推进期间测试无敌；后四关使用传送和伤害 API 加速小怪遭遇；Boss 阈值由伤害 API 设置。上述不是默认难度人工完整通关，不认证所有攻击变体、机关时序/过热倒计时、收集路径平衡、实体手柄、听感、帧率、其他分辨率或跨机。

已逐张抽查菜单、城市战斗和最终 Boss 第三阶段的真实截图，确认主要 UI、玩家与 Boss 可见；截图只是程序化原型的可见性检查，不是商业美术品质验收。所有原始 PNG 和 Player.log 与报告同目录。

### 源码与保护检查

- `Tools/Validate-Source.ps1`：10,040 项纯 C# 断言通过；42 个 Unity C# 文件 × 3 宏分支，0 语法错误；13 项导入保护检查通过。完整报告 `Validation/source-validation.json`。
- 对照本轮备份，39 个原有 Scenes / Packages / ProjectSettings 文件散列保持一致；未删除既有文件。
- 参考设计文档在开发期间新增了“当前实现状态与测试边界”章节；这是并行文件变化，已保留，不回滚、不将其算作本轮代码成果。
- 全量变更列表与 SHA256：`docs/SIDE_SCROLLER_FILE_CHANGES.md` / `.csv`。

### 失败记录与修复范围

保留早期失败报告：首关自动路线遇到可破坏油桶，测试器原先只瞄敌人而没有处理障碍；现补上真实弹道射击油桶与绕障跳跃。武器输入测试延长按键持有并等待冷却，避免把采样/切枪时序当成射击错误；Boss 测试在升级界面暂停期间消费选择后继续；测试器恢复偶发失焦暂停。生产侧另修正跳跃截断只作用一次、平台可达高度、Boss 构图与 HUD 文本溢出。所有最终结果均来自修复后的同一构建，不复用旧构建通过记录。

### 下一阶段

优先进行无辅助真人五关试玩和平衡；接入有授权的正式动漫精灵、逐帧动作/骨骼、主题场景与音效；完善关卡手工编排和更多机关组合。真正三维第一人称是另一套制作管线，本轮未实现；不要将当前正交机瞄模拟宣传为该功能已完成。
