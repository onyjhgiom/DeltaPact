# 本轮完整变更清单

## 2026-10-24：越肩相机优化

- 11 个现有 C# 文件及 `Assets/Settings/MoriEchoesConfig.asset` 增量修改，未新建场景或引入依赖。
- 覆盖相机、配置校验、输入、玩家移动、武器后坐力、角色/血条广告牌、背面占位、暗角纹理、伤害文字、游戏状态与诊断测试。
- 具体路径、Inspector、验收步骤和最终测试证据统一见 `CAMERA_IMPLEMENTATION_AND_TESTS.md`；本次哈希变更清单：`CAMERA_CHANGE_MANIFEST.csv`。
- 原用户相机方案保留。现有界面名称保持文件当前内容，本次不改名。旧构建和首次集成记录均保留供回退。

## 2026-10-24：真实工程补充

主工程：`C:\UnityProjects\MoriEchoes`。本节更新优先于下方首次交付历史。

- 修改 `Assets/Scripts/Combat/Projectile.cs`：以 Health 引用判重，修复新版 GetInstanceID 编译错误。
- 修改 `Assets/Scripts/Core/GameBootstrap.cs`：使用 FindAnyObjectByType。
- 修改 `Assets/Editor/MoriEchoesSetup.cs`：安全保存未命名场景后增量搭建。
- 新增 `Assets/Editor/MoriEchoesIntegration.cs`：通过 Unity API 为缺失管线的空工程建立原生 URP 2D 配置。
- 新增 `Assets/Scripts/Diagnostics/RuntimeSmokeTest.cs`：仅在显式参数及开发环境下运行的集成测试、截图、JSON。
- 新增 `Tools/Test-WindowsBuild.ps1`，修改 `Tools/Validate-Source.ps1` 的报告范围说明。现有 25 个 C#、6 个工具文件。
- 必要依赖：Packages/manifest.json、packages-lock.json、官方 Input System 1.20.0 本地 tarball。配置：New Input、2D 编辑默认行为、后台运行、Graphics 管线与追加三个 Build 场景。来源、校验和原设置备份见集成报告。
- Unity 生成 Config、Unlit Material、Renderer2D、URP Pipeline、三个入口场景、SetupRecovery、全局渲染配置和 `.meta`，不是伪造 YAML/GUID。
- 新增 `PLAY_MoriEchoes.cmd`、`HOW_TO_PLAY.md`、`docs/UNITY_INTEGRATION_REPORT.md`；更新启动、Inspector、进度、验收与构建说明。
- 实际产物：四份 Windows BuildReport/时间戳包、编译和构建日志、InputSystemPackageSource.json、各轮 TestResults 的 JSON/Player.log/PNG，保留失败轮次。
- 真实工程 `docs/DELIVERY_FILE_MANIFEST.csv` 为完整交付文件盘点（不含 Library/缓存/早期包，不是 Git 差异）。原 ZIP 文档和无关文件继续保留。

## 首次源码交付历史

工作根目录：`C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes_Codex_Starter`。以下路径相对这个根目录，未改父目录的其他项目。

## 新增 Unity C#：23 个文件

| 路径 | 职责 |
| --- | --- |
| `Assets/Scripts/Core/RunModel.cs` | 无 Unity 依赖的升级属性、随机目录、等级和波次规则 |
| `Assets/Scripts/Core/GameConfig.cs` | 可编辑玩家、武器、波次、池容量、表现与 Sprite 槽位 |
| `Assets/Scripts/Core/InputReader.cs` | Input System 键鼠/可选手柄输入 |
| `Assets/Scripts/Core/GameBootstrap.cs` | 入口配置验证与运行初始化 |
| `Assets/Scripts/Core/GameManager.cs` | 游戏状态、重开、胜负、暂停、生命周期与本地偏好 |
| `Assets/Scripts/Core/ProceduralArt.cs` | 运行时占位纹理、竞技场、四面碰撞墙 |
| `Assets/Scripts/Core/CharacterVisual.cs` | 占位表现、朝向、排序、血条和受击闪色 |
| `Assets/Scripts/Core/CameraRig.cs` | 正交跟随、前视、边界限制和震动 |
| `Assets/Scripts/Core/CombatEffects.cs` | 特效/飘字池、预警、脉冲、残影 |
| `Assets/Scripts/Core/GameAudio.cs` | 合成音效、氛围循环、音量与音源回收 |
| `Assets/Scripts/Core/WaveManager.cs` | 计时波次、出生、人数上限、精英/Boss 触发 |
| `Assets/Scripts/Combat/Health.cs` | 阵营伤害、生命、一次性死亡、短暂受击保护 |
| `Assets/Scripts/Combat/Projectile.cs` | 扫掠碰撞、命中去重、穿透、寿命 |
| `Assets/Scripts/Combat/ProjectilePool.cs` | 复用弹丸、有界容量 |
| `Assets/Scripts/Player/PlayerController.cs` | Rigidbody2D 移动、瞄准、闪避与脉冲 |
| `Assets/Scripts/Weapons/WeaponController.cs` | 三种武器、弹匣、换枪、装填、散布和暴击 |
| `Assets/Scripts/Enemies/EnemyAI.cs` | 三类普通敌人、精英、出生/死亡/接触与远程行为 |
| `Assets/Scripts/Enemies/BossController.cs` | 区域轰击、弹幕、锁定冲锋与低血量节奏 |
| `Assets/Scripts/Roguelite/ExperienceManager.cs` | 等级选择队列、升级应用 |
| `Assets/Scripts/Roguelite/ExperiencePickup.cs` | 经验吸附与回收 |
| `Assets/Scripts/Roguelite/PickupPool.cs` | 有界经验池、池满合并 |
| `Assets/Scripts/UI/HUDController.cs` | 运行时菜单、HUD、升级、暂停和结算 |
| `Assets/Editor/MoriEchoesSetup.cs` | 检查环境、不覆盖生成场景、引用验证、Windows 构建/报告 |

职责合并说明：PlayerHealth/PlayerDodge 合并到通用 Health/PlayerController；EnemyRangedAttack 合并到 EnemyAI；UpgradeData 由 RunModel 中的不可变目录数据表达，UpgradeManager 合并到 ExperienceManager；MenuController 合并到 HUDController 与 GameManager。当前都是小型灰盒运行组合，避免额外空壳组件。MonoBehaviour 类均有同名文件。

## 新增工具与测试：5 个文件

- `Tools/Import-IntoUnityProject.ps1`：默认预览，只补缺失文件/目录；Assets 冲突先中止，保护目标包和设置。
- `Tools/Validate-Source.ps1`：可重复的独立模型、Roslyn 语法、PowerShell 解析与导入安全校验，生成带时间/源码哈希的报告。
- `Tools/SourceValidator.cs`：C# 9 三组宏语法检查，不是 Unity 类型检查。
- `Tools/Tests/RunModelTests.cs`：10,040 断言与 5,000 次固定种子抽样。
- `Tools/Tests/ImportSafety.Tests.ps1`：13 个独立临时文件夹安全导入检查，不是 Unity 集成测试。

## 新增/更新开发文档与配置

- `START_HERE.md`：工程准备、安全导入、菜单生成、启动、操作和验证入口。
- `docs/PROGRESS.md`：本轮新增并连续更新的 A–L 真实进度。
- `docs/INSPECTOR_AND_TESTS.md`：A–L 每阶段文件、Inspector、测试与验收。
- `docs/ART_INTEGRATION.md`：正式素材/字体授权、Sprite 尺寸/槽位、动画与高级表现剩余工作。
- `docs/ACCEPTANCE_REPORT.md`：实际测试结果、未执行内容、阻塞和风险。
- `docs/BUILD_REPORT.md`：未构建状态、Unity 菜单/Build Profiles/命令行方法、独立包验收。
- `docs/CHANGES.md`：本清单。
- `.gitignore`：Unity 缓存、Builds、Validation、VS 生成文件。未初始化 Git 仓库、未提交、未切分支。

## 原样导入本次交接包：9 个文件

- `README_NEXT_STEPS.md`
- `docs/FINAL_GAME_SPEC.md`
- `docs/ART_PIPELINE.md`
- `docs/DEFINITION_OF_DONE.md`
- `docs/PROGRESS_TEMPLATE.md`
- `CodexPrompts/MASTER_CONTINUE_ALL.md`
- `CodexPrompts/RESUME.md`
- `CodexPrompts/ART_ASSET_IMPORT.md`
- `CodexPrompts/BUILD_WINDOWS.md`

Windows 大小写不敏感，保留现有 `docs` 文件夹。九份文件均按原 ZIP 比较 SHA256 一致。

## 校验命令生成的非游戏文件

`Validation` 为可再生诊断目录，未作为资源导入：

- `RunModelTests.exe`、`SourceValidator.exe`、`SourceValidator.exe.config`。
- Roslyn 运行依赖：`Microsoft.CodeAnalysis.dll`、`Microsoft.CodeAnalysis.CSharp.dll`、`System.Collections.Immutable.dll`、`System.Memory.dll`、`System.Reflection.Metadata.dll`、`System.Runtime.CompilerServices.Unsafe.dll`、`System.Buffers.dll`、`System.Numerics.Vectors.dll`。
- `source-validation.json`：时间、工具路径、结果、源码哈希。

这些 EXE 不是游戏，没有 `MoriEchoes.exe`。源文件已在工作区实际写入，但场景、配置、材质等正式 Unity 资产必须由 Editor 生成。

## 未覆盖与未生成

Starter ZIP 的 `README.md`、四份原 docs、三份原 prompts 共 8 个文件 SHA256 未变。此前存在的 `docs/ENVIRONMENT_CHECK.md` 保留。父目录 `index.html`、`style.css`、`script.js`、`__init__.py` 未改。

没有删除用户文件，没有编辑原 Unity 配置/场景（审计时本来不存在），没有联网/多人/内购、没有添加第三方 SDK。没有伪造 `.meta`、GUID、场景 YAML、包清单、Unity 项目版本、构建产物或运行成功日志。
