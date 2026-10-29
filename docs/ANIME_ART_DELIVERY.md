# 动漫素材接入与实际验收

工程：`C:\UnityProjects\MoriEchoes`；引擎：Unity 6000.6.7f1。本文只报告本轮素材工作，不把程序化原型或自动输入测试等同于商业最终画质、真人平衡、实体手柄长测或跨机性能验收。

## 实际生图

用户授权使用兼容图像 API。先前本机凭据返回 HTTP 403 `Image generation is not enabled for this group`；新增 actor 请求头也返回相同错误。随后用户附件提供了不同凭据：2026-10-24 19:48 请求 `gpt-image-2.5` 成功返回 200，猫图已保存到 `Validation/ImageApiProbe`，仅用于接口验证，不导入游戏。凭据不写入项目。

游戏资产均由 imagegen 技能原装 CLI 实际请求生成。透明素材请求模型名 `gpt-image-2.5`，背景请求 `gpt-image-2`；名称是网关接受的标识，无法独立证明其实际后端模型版本。原始生成 17 张，另对 3 张 Boss 排版进行图像编辑，合计 20 张原始/修订 PNG。原始图不覆盖，`ArtProduction/anime-v1/output/imagegen/source-selection.json` 指定选用的修订图。部分网关返回分辨率不同于请求值，以 PNG 实际像素尺寸为准；没有声称 4K。

- 原图：`ArtProduction/anime-v1/output/imagegen/`。
- 实际提示：`ArtProduction/anime-v1/prompts/delta.txt`、`production-batch.jsonl`、`boss-layout-repair.txt`。
- 加工总览：`ArtProduction/anime-v1/contact-sheet.png`。
- 游戏资源：`Assets/Art/Production/AnimeV1/`，80 个精灵 + 63 个近似法线图，共 143 个 PNG。
- 清单：`Assets/Art/Production/AnimeV1/art-manifest.json`，包含来源文件、来源/输出 SHA-256、分类、角色/帧序号及 PPU。

| 内容 | 数量 | 运行时用途 |
| --- | ---: | --- |
| 四名角色动画 | 32 | 每人待机、五帧跑动、跳跃、蹲伏 |
| 四名角色头像 | 4 | 角色选择/HUD |
| 四种常规敌人 | 8 | 每种两帧，保留血条 |
| 五种 Boss | 20 | 每种四帧，阶段切换/暴走 |
| 五关环境 | 5 | 废城、工厂、冰窟、霓虹、要塞视差背景 |
| 武器 | 3 | 突击步枪、霰弹枪、狙击枪独立挂点 |
| 特效 | 8 | 三种枪焰、两种冲击、电磁环、爆炸、落地尘雾 |

统一采用透明间隙辅助网格切分；相同比例缩放角色帧，统一脚底基线，避免因裁切导致奔跑尺寸变化。法线由 alpha 与亮度梯度近似生成，用于 URP 2D 灯光，不是人工精修的真实体积法线。角色仍是 2D 精灵，没有新增 3D 建模或真实三维第一人称。

## 文件与保护

- 修改前备份：`Validation/AnimeArtBackup_20261024_184904`，含旧脚本、配置、文档及原 Assets 散列。
- 新增 `Assets/Scripts/Core/SideSpriteAnimation.cs` 与 `SideSpriteAnimator.cs`；更新 `SideCharacterVisual.cs` 接入可选图集，缺素材时保留程序化回退。
- 更新 `Assets/Scripts/Level/CharacterDefinition.cs`、`ThemeProfile.cs` 与 `Assets/Scripts/Core/GameConfig.cs` 的素材引用。
- 更新 `Assets/Scripts/Level/SidePresentation.cs` 的背景层与 `Assets/Scripts/Core/CombatEffects.cs` 的特效池，重用粒子时重置翻转状态。
- 新增 `Tools/Prepare-AnimeArt.py`、`Tools/Tests/Test_AnimeArt.py`、`Assets/Editor/ProductionArtSetup.cs`、`Tools/Test-AnimeArt.ps1`。
- 更新 `Assets/Scripts/Diagnostics/SideScrollerSmokeTest.cs`，新增安装素材截图与内存合成图回归探针，测试进度只在内存。
- 真实配置绑定：`Assets/Settings/MoriEchoesConfig.asset`。导入器先校验 80 个槽位、路径、图像/法线散列及已有引用，拒绝覆盖不同既有素材；绑定前另备份配置到 `Validation/ArtConfig_*.asset`。

## Unity Inspector 与启动

1. 在 Unity Hub 打开 `C:\UnityProjects\MoriEchoes`，使用 6000.6.7f1，等待导入/编译结束。
2. 打开 `Assets/Scenes/MainMenu/MainMenu.unity`；GameBootstrap 的 Config 应为 `Assets/Settings/MoriEchoesConfig.asset`。
3. 选中该 Config，确认 Side Scroller 勾选，Characters 四项的 Animation/Frames 各 8 个、Portrait 非空；Themes 五项的 Background 与 Boss Animation 非空；Enemy Animations 四组各 2 帧，Weapon Sprites 3 个，Combat Effect Sprites 8 个。
4. 选中 `Assets/Art/Production/AnimeV1/Characters/delta-00.png`：Texture Type 为 Sprite、Single、PPU 266.66667、Bilinear、无 Mip Map、Compression None，Secondary Textures 中 `_NormalMap` 指向对应法线。Boss PPU 288.58。程序已完成这些设置，无需手工逐图重复。
5. 点击 Play，从菜单开始。A/D 移动、Space 跳跃、S 蹲伏、S+Space 下穿、Shift 闪避、鼠标瞄准/左键射击、右键 ADS、1/2/3 切枪、R 换弹、Tab 切换三档 2D 视角、Esc 暂停。
6. 重新加工已审核原图可运行 `Tools/Prepare-AnimeArt.py --source ArtProduction/anime-v1/output/imagegen --output Assets/Art/Production/AnimeV1`，需要 Python、Pillow、numpy。不联网、不产生 API 费用，拒绝覆盖不同像素。
7. 重新导入使用 `Tools > Mori Echoes > Production Art > Import Reviewed Anime V1`。只有实际审看原图及总览后才设置 manifest 状态为 `visually-reviewed`；该状态不是游戏内验收结论。不同一批素材应另建版本，不直接覆盖。

## 验收步骤与边界

1. 检查四名角色的身份、透明边缘和脚底基线；运行、跳跃、蹲伏、左右瞄准时不应缺帧/拉伸；切机瞄时隐藏玩家身体，切回恢复，武器跟随瞄准。
2. 五关逐一检查背景比例、视差和碰撞平台的区分；常规敌人受伤血条变化；Boss 二/三阶段画面随状态改变。
3. 三枪射击、命中、击杀、电磁脉冲、冲刺残影与落地尘雾应有对应特效；对象池复用不继承残影翻转。
4. 全战役自动测试：`Tools/Test-SideScroller.ps1 -Executable <新包完整路径>`；素材专项：`Tools/Test-AnimeArt.ps1 -Executable <新包完整路径> -Width 1280`（或 1920）。所有自动输入、传送/伤害辅助、截图及失败记录都不能当作真人通关证明。
5. 若回滚素材，先备份当前 Config，再恢复导入前 `Validation/ArtConfig_*.asset` 到 Config 的内容，保留它的 `.meta`；新增 PNG 可以保留，旧程序化表现仍存在。

## 实际测试结果

- 加工器测试：11 项通过，见 `Validation/anime-art-processing-tests.log`，覆盖透明度、缺帧、裁切边界、自适应分割、脚底基线、法线、已有文件保护、80 个槽位/143 张纹理、清单审核状态保持及源图路径保护。使用隔离合成测试图，不算正式美术验收。
- 首次 Unity 编译通过：`Logs/AnimeArtCompile_01.log`。
- Unity 正式素材导入成功：`Logs/AnimeArtImport_01.log`，80 个精灵绑定完成。
- 素材前的 18 项专项探针通过：`TestResults/ArtOnly_1280x720_20261024_195152_586/stage-smoke.json`。
- 素材前首次完整回归在土狼跳测试失败：`TestResults/Anime_1280x720_20261024_194507_232/stage-smoke.json`，18 项通过后失败，保留记录。已将测试中依赖 40 个渲染帧的等待改为时间上限，避免高帧率下还未走出平台便发起跳跃；未修改生产移动参数。
- 首次正式素材 Windows 包：`Builds/Windows/MoriEchoes_20261024_202142_325/MoriEchoes.exe`；720p 可见窗口 47 项专项检查与 123 项战役回归通过，0 错误。对应报告：`TestResults/ProductionArt_1280x720_20261024_203009_471/stage-smoke.json`、`TestResults/Side_1280x720_20261024_203248_568/stage-smoke.json`。
- 隐藏窗口的两轮专项测试虽逻辑通过，但截图全黑，不计入画面验收：`TestResults/ProductionArt_1280x720_20261024_202511_877`、`ProductionArt_1920x1080_20261024_202843_727`。测试工具现使用可见窗口，并拒绝纯色截图。
- 中间包 `MoriEchoes_20261024_203611_055`：1080p 素材专项 47 项通过，但完整回归 102 项后在二段跳探针失败。报告 `TestResults/Side_1280x720_20261024_204344_795/stage-smoke.json` 保留。测试已先检查第一次跳跃进入空中，再保持释放 0.08 秒后发起第二次按下，不再只等待一渲染帧。生产移动参数未改动。
- 最终包：`Builds/Windows/MoriEchoes_20261024_205025_560/MoriEchoes.exe`；构建成功，0 错误/0 警告，317,630,732 字节，见 `docs/BuildReports/Windows_20261024_205025_560.md`。包含体型脚底/残影位置校正、解锁配色、菜单文案和测试时序修正。
- 最终包 1280×720 完整战役回归：124 项通过，0 错误、退出码 0；报告 `TestResults/Side_1280x720_20261024_205304_928/stage-smoke.json`。覆盖五关/五 Boss 三阶段、首关闭环、升级/武器/检查点、四角色、三档视角与素材探针；包含输入、传送/无敌和伤害辅助。
- 最终包 1920×1080 素材专项：47 项通过、0 错误、退出码 0，并检查 17 张截图非纯色；报告 `TestResults/ProductionArt_1920x1080_20261024_205442_612/stage-smoke.json`。已目视检查同版生产逻辑的角色菜单、工厂/废城战斗画面、冰关 Boss 和四种敌人；五关逐关截图均保存于报告目录。
- 最新源码静态检查：10,040 项纯 C# 断言、45 个 Unity 源文件 × 3 宏分支与 13 项既有导入保护通过。真正 Unity 编译/构建依据上列 Unity 日志，不以静态检查替代。
- 原 13 个场景及场景 meta 文件散列未变。没有删除旧构建、源图或场景。

双击项目根目录 `PLAY_MoriEchoes.cmd` 启动最终包。完整源文件及素材变更路径/散列见 `docs/ANIME_ART_FILE_CHANGES.csv`；该清单排除 Library、编译缓存、测试输出、Validation 依赖和构建二进制，后者以上述实际目录为准。

尚未验收：真人默认难度全程平衡、实体手柄长测、跨机帧率/显存/兼容性，以及商业最终画质。平台、门禁、油桶等仍有程序化表现；角色五帧跑动与 Boss 四帧状态图不是完整手工骨骼动画。不能宣称达到《和平精英》画质或真实三维第一人称。
