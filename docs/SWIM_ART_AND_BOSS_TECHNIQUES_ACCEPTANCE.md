# 第 9、10 关正式素材与十关 Boss 新技能验收

日期：2026-10-28；工程 `C:\UnityProjects\MoriEchoes`；Unity 6000.6.7f1。此文档替代旧水关报告中“无新生图素材”和“释放前约 0.22 秒锁定”的状态描述。旧报告保留用于追溯。

## 本轮交付

全部十关 Boss 各新增 **3 招**，合计 30 招；第一、二、三形态各解锁一招，已解锁招式轮换。每两次原有固定攻击后穿插一次新招，不叠加同时发动，不无限增加招式数量。各招名称、释放方式、视觉表现和闪避方法详见 `docs/BOSS_TECHNIQUE_DESIGN.md`。

Image API 已实际成功调用。请求模型为 `gpt-image-2.5`，使用用户指定的兼容网关和未经修改的内置 `image_gen.py` CLI；不能独立认证兼容网关后台的真实模型版本。共生成 24 张原始图：水关 14 张、十关技能图集 10 张。使用原生透明通道，未用程序绘图冒充生成美术；原图完整保留。每招有独立释放图与命中图，合计 60 张技能 Sprite。

水关另有 86 张切片：2 张背景、16 张小兵姿态、12 张 Boss 三相姿态、8 张环境模块、16 张战斗效果、24 张实体形态部件、8 张大招帧。60 张辅助法线由插画亮度/轮廓加工得到，属于 2D 光照辅助，不是真实 3D 模型法线。没有将 2D 插画称为可旋转的三维建模。

第 9 关增加 12 组共 24 块上下错层浮台，可从水中跃上、随浮台上下浮动、连续换台及按 S+Space 下穿；浅潜与深水运动分开。海面上界调整为基准水面 +6.2 米，允许完整上层平台跳跃。第 10 关维持全水层游泳、氧气、热泉补氧和水压。两关均没有隐藏路线。

Boss 常规锁定先短暂跟踪，再固定世界落点：前五关至少约 0.85 秒、竖井约 0.95 秒、水关 1.15 秒。新增 30 招在预警开始时就固定位置/完整路线，最短预警 1.1 秒；不会持续追着玩家平移，不施加冻结或强吸附。移动攻击的透明红色路径表示未来扫过的区域；实际危险体沿该固定路径移动。华丽装饰性碎屑、烟雾和尾光可超出判定体，红色区域和运行碰撞几何为避让依据。

新增技能基础伤害 17/20/23（空心环 22），继续经过关卡伤害倍率与既有玩家承伤上限，不另设满血秒杀。原八人、九把武器、原八关美术和防御配置保留；新增的是技能字段与运行调度。

## 文件与溯源

| 范围 | 路径 |
|---|---|
| 水关原图 / 提示词 | `output/imagegen/swim-anime-v1/` / `ArtProduction/swim-anime-v1/prompts/` |
| 技能原图 / 提示词与中文规格 | `output/imagegen/boss-techniques-v1/` / `ArtProduction/boss-techniques-v1/` |
| 已绑定水关素材 | `Assets/Art/Production/SwimAnimeV2/` |
| 已绑定技能素材 | `Assets/Art/Production/BossTechniquesV1/` |
| 图像预览 | 两个已绑定素材目录中的 `review.jpg` |
| 原图/输出 SHA-256 | 两个已绑定素材目录中的 `art-manifest.json` |
| 计划 / 原生透明切片工具 | `Tools/Plan-SwimArt.py`、`Plan-BossTechniqueArt.py`、`Prepare-SwimAndTechniqueArt.py` |
| 安全导入与绑定 | `Assets/Editor/SwimAndTechniqueArtSetup.cs` |
| 新技能调度与独立轨迹 | `Assets/Scripts/Enemies/BossTechniqueController.cs` |
| 危险体与固定锁定 | `Assets/Scripts/Enemies/BossField.cs` |
| 运行特效池与水中尾迹 | `Assets/Scripts/Core/CombatEffects.cs` |
| 水关环境、浮台、角色运动 | `Assets/Scripts/Level/SwimStageDirector.cs`、`PlatformSurface.cs`、`Assets/Scripts/Player/SidePlayerMotor.cs` |
| 自动验收 | `Assets/Scripts/Diagnostics/BossTechniqueSmokeTest.cs`、`SwimCampaignSmokeTest.cs` |

首次切片因透明分隔检查过严而中断，`SwimAnimeV1` 是未绑定的加工中间结果；完整资产放在 `SwimAnimeV2`，没有覆盖旧文件。切片按原生 alpha 寻找分隔线，并对输出进行实际预览；原始发射姿态里的远端尾光可能触及图集边缘，独立技能图用于完整攻击表现。

API 凭据不进入工程、素材清单、构建或日志。本机 image 配置与加密凭据保存在 `%LOCALAPPDATA%\MoriEchoes\ImageAPI`；DPAPI 密文只供当前 Windows 用户解密，不是可分享的游戏依赖。运行游戏完全离线，不需要该配置。

## Unity Inspector 与启动

1. 用指定 Unity 打开工程，进入 `Assets/Scenes/MainMenu/MainMenu.unity`，确认启动组件引用 `Assets/Settings/MoriEchoesConfig.asset`。
2. 选择该配置资产，Themes 保持 10 项，Characters 8 项、Weapons 9 项。每个 Theme 的 Boss Techniques 应恰有 3 项；每项 Name/Release/Visual/Dodge 非空、Frames 为 2 张。
3. Themes 索引 8/9 的 Background、Enemy Animations、Boss Forms、Architecture、Boss Ornaments、Boss Ultimate Frames 指向 `SwimAnimeV2`；前三相各 2 帧、每相 4 个实体部件。
4. 图像导入为 Sprite / Single / Center / Full Rect，PPU 256，Alpha Is Transparency 开启；Bilinear、Clamp、无 Mipmap、关闭自动 Physics Shape。法线作为 `_NormalMap` 次级纹理绑定。一般无需逐张手工操作。
5. 若新工作区尚未绑定，退出 Play 后运行菜单 `Tools > Mori Echoes > Production Art > Apply Swim And Boss Techniques`。工具先验证素材哈希并备份配置，只更新本轮字段，不重置旧关数值。
6. 双击根目录 `PLAY_MoriEchoes.cmd` 或运行本轮 EXE。A/D 移动，Space 跳跃，S+Space 下穿浮台；深水 WASD；鼠标瞄准、左键射击、右键精瞄、Shift 闪避、1–9 换枪、Q/F/C 技能、X 大招、Tab 视角、F11 全屏。

## 实际构建与运行

最终程序：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261028_171506_913\MoriEchoes.exe`。

BuildPipeline 实际成功，Windows x64 / Development，0 错误、0 警告，19.813 秒，1,625,746,162 字节。构建报告 `docs/BuildReports/Windows_20261028_171506_913.md`，日志 `Logs/SwimTechniqueBuildFinal_20261028.log`。

以下对同一最终 EXE 实际运行完成，四组通过、错误列表为空、进程退出码均为 0：

| 专项 | 通过检查 | 报告目录（位于 TestResults） |
|---|---:|---|
| 水关、角色菜单、浮台连续跳跃、Boss 固定落点闪避 | 193 | `Swim_20261028_172640_621` |
| 十关 30 招名称/独立图像/预警/实际激活/结束回收 | 389 | `BossTechniques_20261028_172804_287` |
| 前五关敌兵阵容、原有 Boss 攻击/三相/处决/通关 | 410 | `Encounter_1280x720_20261028_173006_158` |
| 第六至八关攀升与 Boss 三相/大招/通关 | 99 | `Vertical_20261028_173510_486` |

共 1091 项。技能专项生成 60 张预警/激活截图，水关专项生成 16 张截图，已实际查看素材总览、两关 Boss、海面浮台和数个技能实战画面。首轮检查后修复海面硬边水色遮罩与细长攻击插画过小，最终 EXE 上重新完成上述检查。

静态检查另有 10040 条纯模型断言、5000 次抽样、91 个 C# 文件 × 3 套定义的语法检查及 13 项隔离导入检查；这些不代替实际编译/运行。最终 EXE 已完成 Unity 编译及 Player 验收。

复测命令：

```powershell
$game='C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261028_171506_913\MoriEchoes.exe'
& 'C:\UnityProjects\MoriEchoes\Tools\Test-SwimCampaign.ps1' -Executable $game
& 'C:\UnityProjects\MoriEchoes\Tools\Test-BossTechniques.ps1' -Executable $game
& 'C:\UnityProjects\MoriEchoes\Tools\Test-AnimeArt.ps1' -Mode Encounter -Executable $game
& 'C:\UnityProjects\MoriEchoes\Tools\Test-VerticalCampaign.ps1' -Executable $game
```

## 人工验收步骤与边界

- 每关正常战斗至三相，核对一、二、三招依次解锁，每两次原有攻击后穿插一次；只有三招新增，不应连续堆叠全部技能。对照设计文档检查各 Boss 轮廓、色彩、材质与轨迹。
- 每种红区出现后持续移动或跳跃，确认固定落点不会追上角色；行进攻击可从路径之外闪开，空心环中心和路线空隙保留通路。
- 观察技能高光局限于核心和边缘，背景、玩家与敌弹仍清楚，不允许整屏白闪遮蔽操作。自动截图不等于最终专业美术审定。
- 第 9 关依次完成水面跃上低台、跳上高台、站稳随波、S+Space 下穿。第 10 关检查专属小兵、热泉与三相母巢，浮动胶囊仍可拾取。
- Boss 仍有血量时不可胜利，打空血条后才结算；受击应扣可辨认的中上额度，不从满血秒杀。

自动测试使用合成输入、位置/血量辅助与无敌；30 招专项用反射设置形态和出招计数以覆盖全部内容，不代表真人正常难度打通。原有招式回归与水关专项另行使用自然攻击调度。人工平衡、实体手柄长测、Editor Play Mode、最终美术审定尚未完成；跨机测试按用户要求取消。未制作压缩包。
