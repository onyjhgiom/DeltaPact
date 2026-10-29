# 角色、武器与动态场景升级 / Arsenal V2

本轮保留 2D 横版关卡、碰撞和瞄准规则。经用户确认，Tab 的“第一人称”是横版场景上的武器前景瞄准表现，不改造成三维射击游戏。四角色现可直接选择，关卡仍按通关顺序解锁。

## 角色与武器

| 角色 | Q 主动技能 | 持续 / 冷却 | 专精武器（伤害 +12%） |
| --- | --- | --- | --- |
| DELTA | 战术专注：射速 +60%、暴击率 +20% | 5 / 14 秒 | AR、SMG |
| VANGUARD | 堡垒护盾：受到伤害 -70%，启动时近身冲击 | 5 / 16 秒 | SG、GL |
| WRAITH | 幽影疾行：速度 +50%、减伤 40%，周围普通敌人冻结 2 秒；保留二段跳 | 4 / 12 秒 | SR、RG |
| HAVOC | 蜂群轰击：三发范围爆破弹，不消耗枪械弹药；保留弹匣 +30% | 即时 / 14 秒 | SMG、GL |

原电磁脉冲升级保留，获得后使用角色技能会附加脉冲伤害。暂停时技能计时停止，检查点重试清除技能状态和冷却。护盾减伤与冲刺无敌分离。

| 按键 | 武器 | 基础伤害 | 射速 | 弹匣 / 装填 | 区别 |
| --- | --- | --- | --- | --- | --- |
| 1 | AR-07 突击步枪 | 12 | 7 发/秒 | 28 / 1.2 秒 | 均衡点射 |
| 2 | SG-12 霰弹枪 | 5 × 6 | 1.1 | 6 / 1.5 秒 | 近距离散射 |
| 3 | SR-09 精准步枪 | 45 | 1.2 | 8 / 1.6 秒 | 贯穿 2 个目标 |
| 4 | SMG-21 疾风 | 7 | 13 | 42 / 1.05 秒 | 高频压制、较大散布 |
| 5 | RG-08 电磁轨道 | 82 | 0.7 | 4 / 1.9 秒 | 高速、贯穿 4 个目标 |
| 6 | GL-40 烈焰榴弹 | 42 | 1.2 | 5 / 1.8 秒 | 2.5 单位爆炸半径，外缘伤害衰减 |

每个角色都能使用六把枪；主菜单选择出战武器，战斗中 1–6、滚轮或手柄 LB/RB 切换，弹药分别记录。武器专精与肉鸽强化叠加。这些参数已经实现，但不是人工平衡完成的结论。

## 美术与场景

- `ArtProduction/arsenal-v2/prompts/production-batch.jsonl`：13 个实际生图任务，沿用已授权的本地图像 API 配置和 bundled imagegen CLI。请求模型标识为网关的 `gpt-image-2.5`，不推断其底层模型。
- `ArtProduction/arsenal-v2/output/imagegen/`：四张精致角色立绘、五张 Boss 四帧图集、六武器侧视图、六武器前景、八场景部件、六平台材质图集。另保存 HAVOC 发梢构图修正版，旧图保留。
- `Assets/Art/Production/ArsenalV2/`：50 个精灵、40 张近似法线图；`art-manifest.json` 记录原图与切片 SHA256、PPU 和配置槽位。法线来自亮度/轮廓近似，不是手工雕刻法线。
- 四名玩家的战斗八帧动画、四种敌人、五关背景与八类特效沿用 AnimeV1；本轮替换角色选择立绘、全部 Boss 帧与武器，增加可互动部件和平台图像。
- 五 Boss 分别保留重盾骑士、熔炉重装、冰晶翼骑士、霓虹幽影、赤红帝王的视觉身份。攻击帧与真实攻击时机同步，阶段切换有波纹与场景震动，最终弱点采用发光晶体形象。
- 五层视差保留；原本的大块前景建筑改为灯柱、霓虹牌与残骸层。近场装饰会随角色经过和冲击摆动、闪亮；48 个复用环境微粒随移动扰动，奔跑扬尘、地表命中火花/痕迹、爆炸冲击和弹壳均复用效果池。
- 第一人称瞄准表现使用六张独立武器前景，包含移动轻摆、射击后坐与装填下沉；仍按鼠标在 2D 场景的实际位置计算命中。

## 文件与 Inspector

| 阶段 | 主要文件 | Inspector / Editor 步骤 | 测试与验收标准 |
| --- | --- | --- | --- |
| 角色技能 | `Assets/Scripts/Player/CharacterSkill.cs`、`SidePlayerMotor.cs`、`PlayerController.cs`、`Assets/Scripts/Combat/Health.cs` | 选中 `Assets/Settings/MoriEchoesConfig.asset`，确认 All Characters Available 勾选，Characters 为四项；玩家组件运行时生成，无需拖引用 | 四角色从菜单可选；Q 各自生效，冷却不可连发；暂停不计时；护盾结束后恢复正常伤害 |
| 六武器 | `Assets/Scripts/Weapons/WeaponController.cs`、`Assets/Scripts/Combat/Projectile.cs`、`ProjectilePool.cs`、`Assets/Scripts/Core/GameConfig.cs`、`InputReader.cs` | Config → Weapons 六项、Weapon Sprites 六项、Weapon View Sprites 六项；GL Explosion Radius=2.5；保留原前三把枪参数 | 1–6 和循环切枪正确；六枪分别消耗/补充弹药；SR/RG 穿透，GL 对邻近敌人产生范围伤害 |
| 美术导入 | `Assets/Editor/ArsenalSetup.cs`、`Tools/Prepare-ArsenalArt.py` | 已自动导入；复现时菜单 `Tools > Mori Echoes > Production Art > Import Reviewed Arsenal V2`。先检查清单为 visually-reviewed。导入器会备份 Config，仅允许替换 AnimeV1 或本版本对应槽位，自定义外部素材会拒绝覆盖 | 50 槽位完整、哈希匹配、原图与旧图保留；精灵 Single、Bilinear、无 Mipmap，法线绑定 `_NormalMap`；不修改场景 |
| 场景/Boss | `Assets/Scripts/Level/SceneFeedback.cs`、`SidePresentation.cs`、`SegmentBuilder.cs`、`StageInteractable.cs`、`LevelManager.cs`、`Assets/Scripts/Core/CombatEffects.cs`、`SideSpriteAnimator.cs`、`Assets/Scripts/Enemies/SideBossController.cs` | Config → Scenery Sprites 八项、Platform Sprites 六项；Themes 的 Boss Animation 各四帧且 Faces Right 关闭。Play 后 Run/StageGeometry 中查看 ReactiveScenery、AtmosphericMote | 五主题有导入的平台和场景部件；走近飘带/射击地面产生反馈；五 Boss 的三阶段规则、弱点与胜利流程仍可达 |
| 视角/UI/窗口 | `Assets/Scripts/UI/HUDController.cs`、`Assets/Scripts/Core/DisplaySettings.cs`、`SideCameraDirector.cs`、`ProjectSettings/ProjectSettings.asset` | Player Settings → Resolution and Presentation → Resizable Window 勾选、Fullscreen Mode=Windowed，默认 1280×720。CanvasScaler 使用 Expand；UI 运行时创建 | Tab 切三档，右键临时瞄准；不同枪有对应前景；F11/Alt+Enter 切无边框与窗口、恢复窗口尺寸；4:3、16:9、宽屏菜单按钮不越界 |
| 运行回归 | `Assets/Scripts/Diagnostics/SideScrollerSmokeTest.cs`、`Tools/Test-AnimeArt.ps1`、`Tools/Test-SideScroller.ps1` | 用独立可见 Windows Player 运行，不要求手工编辑场景；测试专用开关只在 Editor/Development 包生效 | 查看 JSON passed、errors、截图非空；所有结论限定在本机自动化测试 |

## 启动与操作

Unity Hub 添加 `C:\UnityProjects\MoriEchoes`，使用 `6000.6.7f1`，打开 `Assets/Scenes/MainMenu/MainMenu.unity`，确认唯一 GameBootstrap 绑定 `Assets/Settings/MoriEchoesConfig.asset` 后 Play。场景未重建，不需要手工拖拽角色或武器。

Windows 使用根目录 `PLAY_MoriEchoes.cmd`。A/D 移动，Space 跳跃，S 蹲下，S+Space 下穿，Shift 冲刺，鼠标瞄准/左键射击，右键临时瞄准，Tab 视角，Q 角色技能，1–6/滚轮切枪，R 装弹，E 互动，Esc 暂停，F11 或 Alt+Enter 切换全屏。窗口可拖拽边缘并最大化。

手柄：左摇杆移动、右摇杆瞄准、RT 射击、LT 瞄准、A 跳跃、B 冲刺、X 装弹、Y 技能、LB/RB 循环武器、View 切视角、D-pad↑ 互动、Start 暂停。实体设备长测仍未进行。

## 实际验证记录

- 2026-10-24：素材原图/切片目检完成。HAVOC 原图头发触及上边缘，已实际调用编辑接口修正；立绘保留合理的大腿裁切。武器/道具不等距排版使用经过目检的透明间隙切片，原图未覆盖。
- Unity 6000.6.7f1 实际编译与导入成功，50 个精灵接入，Config 备份 `Validation/ArsenalConfig_20261024_220520_367.asset`。
- 第一构建 `Builds/Windows/MoriEchoes_20261024_220748_896`：0 错误/0 警告。专项运行在 294 项检查后于第六枪合成按键选择检查失败，故此包未标记为最终通过。报告 `TestResults/Arsenal_1920x1080_20261024_220930_459/stage-smoke.json` 保留。
- 修订构建 `Builds/Windows/MoriEchoes_20261024_221633_563/MoriEchoes.exe`：Unity 实际构建 0 错误/0 警告。按键诊断增加 ControlsReady 等待、显式按键及状态记录；复测六枪全部通过。首轮失败保留，不将尚未复现的原因武断归为引擎或设备故障。
- 1080p 专项运行：301 项检查通过、0 运行错误、退出码 0，14 张非纯色截图。覆盖六枪实际命中/弹药/装填、穿透及爆炸、四技能、暂停冻结冷却、护盾到期、角色/武器菜单点击、场景命中反馈、六武器前景、1024×768/1600×720/1600×900 窗口和 F11 全屏往返。报告：`TestResults/Arsenal_1920x1080_20261024_222044_083/stage-smoke.json`。
- 素材加工器针对本批已执行完整加工与幂等重跑；旧加工器 11 项保护测试通过。10,040 项纯 C# 模型断言、13 项原始导入安全检查通过。语法检查不能替代本节记录的 Unity 编译/Player 实测。
- 720p 五关完整战役回归：124 项通过、0 运行错误、退出码 0。覆盖跳跃/冲刺/下穿、三视角、原三武器、升级、五主题机关、五 Boss 三阶段、最终弱点、胜利与检查点死亡重试；使用测试无敌和阶段伤害辅助。报告：`TestResults/Side_1280x720_20261024_222255_471/stage-smoke.json`。
- 1080p 素材回归：47 项通过、0 运行错误、退出码 0，17 张非纯色截图，包含五主题的新平台/部件、五 Boss 普通与狂暴外观。另覆盖临时内存精灵替换的动画回归，测试后恢复配置。报告：`TestResults/ProductionArt_1920x1080_20261024_222726_381/stage-smoke.json`。
- 本轮最终源代码为 49 个 Unity C# 文件，三套预处理宏语法检查全部通过；13 个已有场景相关文件 SHA256 未改变；项目源码/工具/文档/本批生图目录未发现密钥形态字符串。
- `PLAY_MoriEchoes.cmd` 已指向最终通过的 `MoriEchoes_20261024_221633_563`。完整新增/修改路径和散列见 `docs/ARSENAL_FILE_CHANGES.csv`；构建报告 `docs/BuildReports/Windows_20261024_221633_563.md` 的“运行未测试”是构建时状态，本节记录后续实际运行结果。

## 测试范围与回退

自动测试使用合成键鼠、测试无敌、固定目标及阶段伤害辅助，验证行为回归和素材绑定，不证明默认难度人工通关。未完成实体手柄长测、真人平衡、跨机性能认证、商业最终画质审定；不宣称达到《和平精英》制作规格。

本轮备份位于 `Validation/ArsenalBackup_20261024_213751/`；旧素材、旧构建、既有场景均保留。恢复配置/源码应使用同一批备份，不只替换 Config。分发时复制整个时间戳 Windows 目录，不能只复制 EXE；目标机中文字体与显示缩放仍需验收。
