# 开发进度

## 2026-10-24 / 2.5D 越肩视角优化

- 根据 `CAMERA_2_5D_OVERSHOULDER_PLAN.md` 修改实际主工程：越肩前导、侧肩偏移、28° 正交倾角、右键/LT 开镜、中心准星/暗角、三枪后坐力、相机相对 WASD、广告牌与背面占位。
- 修正倾斜相机固定深度投影错误、右向量符号和镜头安全距离；竞技场边界优先保证玩家可见与瞄准不反向。详情、文件清单、Inspector 参数和验收步骤：`CAMERA_IMPLEMENTATION_AND_TESTS.md`。
- 原方案文档与现有场景已通过备份哈希核对；AI/升级/波次/Boss 参数未改。旧包保留，主工程修改前备份：`Validation/CameraBackup_20261024_010538`。
- 中间版本 `MoriEchoes_20261024_011958_046` 已实际构建成功（0 错误/0 警告），720p/900p/1080p 各 91 项检查通过。此前保留两次输入失败和一次跨时段超时记录；隔离诊断进程内真实输入后重跑通过。
- 最终补充静止无自转、后坐力自然恢复和 SR 开镜实弹命中检查；实际最终 EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261024_101932_609\MoriEchoes.exe`，BuildPipeline Succeeded，0 错误/0 警告。启动脚本已切到此包。
- 最终 1280×720、1600×900、1920×1080 可见窗口各 **94 项通过、0 运行错误、退出码 0**；每档保存 8 张截图。汇总：`TestResults/latest-windows-smoke-summary.json`；实际目录与失败历史见相机报告。
- 尚未执行 Editor Play Mode、人工默认十波通关、真实手柄手感、听感、跨机或性能验收。当前仍为程序化灰盒。下方首次集成与阶段记录保留为历史。

## 当前结果：2026-10-24 / 已集成真实 Unity 与 Windows 包

- 真实工程：`C:\UnityProjects\MoriEchoes`；Editor 6000.6.7f1，Input System 1.20.0、URP 17.6.0、UGUI 2.6.0。
- 安全导入、包补齐、原生 URP 2D 配置、实际编译、场景生成和引用验证已执行。修复新版 Unity API 及未命名场景搭建问题。
- 最终 BuildPipeline Succeeded，0 错误 / 0 警告；Windows x64 / Mono / Development。实际 EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261023_235738_697\MoriEchoes.exe`。
- 720p、900p、1080p 可见窗口各 51 项自动检查通过，0 运行错误，退出码均 0。真实截图/JSON/Player.log 已保存并抽查画面。
- 测试使用临时一波 5 秒及合成输入，Boss 阶段有测试无敌/伤害 API；不等同默认十波通关。Editor Play Mode、人工默认难度、听感、跨机、长时性能、正式精美动漫素材尚未完成。
- Inspector、逐项证据、失败历史、源码变更和下一阶段：`UNITY_INTEGRATION_REPORT.md`。使用 `HOW_TO_PLAY.md` 与 `PLAY_MoriEchoes.cmd` 启动；下面保留首次交付历史。

## 2026-10-23 / A：工作区审计

- 工作区：`MoriEchoes_Codex_Starter`。这是源码交接骨架，不是已创建的 Unity 项目。
- 已读取原 README、原 docs/prompts 和本次完整交接包的全部 9 份文档。交接文档仅作为开发规格，当前用户请求决定执行范围。
- 本次交接文档采用“不覆盖同名文件”的方式导入。Windows 下 `Docs` 与 `docs` 指向同一目录，保留原 `docs` 命名。
- 缺少 `Packages/manifest.json`、`ProjectSettings/ProjectVersion.txt`，因此 Unity 版本和 Input System 已安装版本无法确定。
- PATH、常见安装目录、注册表及常见项目位置未找到 Unity Editor / Unity Hub / 有效 Unity 工程；不代表已穷举所有磁盘上的自定义安装。
- 存在 Visual Studio 2022 Community 及 Roslyn 编译器，可用于独立 C# 测试，不可代替 Unity 编译。
- 原 `Assets` 中未发现代码、场景、Prefab 或 `.meta`，不涉及覆盖已有游戏实现。
- 父目录的 HTML/CSS/Python/JS 文件不属于本游戏，禁止改动。

## 实施边界

本轮继续创建可导入 Unity 6 Universal 2D 工程的真实源码、编辑器搭建工具和操作文档。不伪造 Unity 工程配置、场景 YAML、资源 GUID、编译成功记录或 Windows 可执行文件。没有 Unity Editor 时，场景生成、真实编译、Play Mode、图像质量与 Windows 构建均为待验证。

## 阶段状态

| 阶段 | 交付内容 | 当前状态 | 实际验证 |
| --- | --- | --- | --- |
| A | 环境与既有文件检查 | 真实 Editor/工程/依赖已核验 | Unity 日志与包版本 |
| B | 编辑器安全搭建、战斗场景与竞技场 | 原生配置/场景已生成 | 绑定验证与 Player 启动通过 |
| C | 移动、瞄准、健康与闪避 | 已集成 | 独立 Player 合成输入通过 |
| D | 敌人 AI 与 Boss | 已集成 | 追击/远程/Boss 活动与结算通过，完整招式人工待测 |
| E | 经验、升级池与三选一 | 已集成 | 模型与拾取/队列/UI 回调通过 |
| F | 波次与菜单、胜负、重开 | 短波闭环通过 | 默认十波正常难度仍待人工通关 |
| G | 三种武器与弹药 | 已集成 | 换枪/射击碰撞/装填通过，真人枪感待调 |
| H | 自适应 UI | 三档已运行 | 51 项 × 3 分辨率与截图，Start 实际鼠标点击 |
| I | 程序化占位视觉与音效 | 可见渲染已抽查 | 听感与 Profiler 未执行 |
| J | 正式美术导入规范 | 规范、槽位已写入 | 正式素材/动画/高级画面未完成 |
| K | 源码检查与测试报告 | 独立检查、真实 Unity 编译与 Player 测试通过 | 完整完成定义仍有人工/美术/性能缺项 |
| L | Windows x64 构建 | 实际成功，0 错误 / 0 警告 | 实际 EXE 三档独立运行通过 |

## 2026-10-23 / B–C：运行骨架与玩家

- 新增 GameConfig、GameBootstrap、GameManager、ProceduralArt、CameraRig、InputReader、PlayerController、Health、Projectile/ProjectilePool。
- 添加 Runtime 竞技场与边界、相机、状态机、移动瞄准、射击基础、生命与闪避；源文件均在 `Assets/Scripts` 对应子目录。
- Editor 工具只补缺失配置/材质/入口场景，检查已有场景绑定，保留原场景。当前还没有实际 `.unity` 或 `.asset` 文件。
- 手动配置、测试、验收见 `INSPECTOR_AND_TESTS.md` 的 B/C；Unity 未执行。

## 2026-10-23 / D–E：敌人与升级

- 新增 EnemyAI、BossController、ExperienceManager、ExperiencePickup、PickupPool 和纯数据 RunModel。
- 实现追击/远程/突袭/精英、Boss 区域攻击/冲锋、一次性死亡掉落、经验吸附、连续等级队列、九种三选一与属性效果。
- 独立模型断言和随机抽样已运行通过，不能替代 Unity 中的 AI/按钮/伤害集成验证。
- 手动配置、测试、验收见 D/E；没有标记可玩通过。

## 2026-10-23 / F–H：流程、三枪与 UI

- 新增 WaveManager、WeaponController、HUDController；扩展 GameManager/GameConfig。
- 实现十波计时、Boss、胜负、重开、菜单与暂停；三枪不同弹匣/射速/散布/装填/穿透；运行时构建 UGUI。
- 源码使用同场景状态切换，三个资源场景是不同入口，不假称已做跨场景异步加载。
- 手动配置、测试、验收见 F/G/H；尚无真实流程或 UI 图像结果。

## 2026-10-23 / I–J：占位表现与素材管线

- 新增 CharacterVisual、CombatEffects、GameAudio；增加材质和 Sprite 配置槽位。
- 有界弹丸、拾取、特效、数字和音效资源；预警独立保留槽；合成音效和程序化轮廓均为灰盒。
- 已写入 `ART_INTEGRATION.md`，明确尺寸、Pivot、各数组槽位、授权、动画和正式画面缺项。
- 未制作/导入正式动漫动画、精细场景建模或高级光照后处理；无截图、听感或性能实测。

## 2026-10-23 / K：实际校验与修复

- `Tools/Validate-Source.ps1` 已实际运行。纯 C# 模型：**10,040 断言通过，5,000 次固定种子三选一抽样**。
- Roslyn C# 9：**23 个 Unity 源文件 × 3 组宏，0 语法错误**；不是 Unity API/类型检查，也不是 Unity 工程编译。
- PowerShell：**3 个脚本 0 解析错误**。导入安全回归：**13 项通过**，使用隔离临时合成夹具，不是 Unity 导入运行。
- 修复静态发现的枚举比较、Unity 6 物理查询重载、弹道起点、玩家受击节流、血条比例、特效缩放、URP Unlit 材质引用及场景绑定验证问题；这些修复仍需真实 Unity 编译与行为验证。
- Roslyn 首次启动的依赖重定向故障已修复并复跑，不掩盖为游戏编译问题。
- 原 ZIP 8+9 个文件 SHA256 未变；父目录 4 个无关文件 SHA256 未变。
- 结果：`Validation/source-validation.json`；详细解释：`ACCEPTANCE_REPORT.md`。原 `DEFINITION_OF_DONE.md` 未替未执行项目打勾。

## 2026-10-23 / L：构建阻塞与交接

- 已新增 `MoriEchoesSetup.BuildWindows` 安全构建入口，构建后记录真实 BuildReport/EXE 存在性，使用新时间戳目录不覆盖旧包。
- **当前没有 Unity Editor/真实工程，未调用 BuildPipeline，未生成 Windows 游戏 EXE。**
- `BUILD_REPORT.md` 提供菜单、Build Profiles、命令行与独立包测试步骤；仅有未来输出格式，没有冒充实际构建路径。
- 下一步：收到真实工程和 Unity.exe 路径后，先安全导入、真实编译、运行场景生成器，修复集成错误，再逐项试玩和构建；详见 `START_HERE.md`。

## 交付索引

- 具体文件变更：`CHANGES.md` 与真实工程 `DELIVERY_FILE_MANIFEST.csv`；现有 25 个 Unity C# 文件、6 个工具/测试文件。
- 每阶段 Inspector、测试、验收：`INSPECTOR_AND_TESTS.md`。
- 正式美术接入：`ART_INTEGRATION.md`。
- 实际结果与未执行内容：`ACCEPTANCE_REPORT.md`。
- Windows 状态与方法：`BUILD_REPORT.md`。

## 2026-10-23 / 环境状态确认与安装交接

- 用户明确回复“尚未安装”，确认 Unity 环境缺失不是仅因自定义安装路径未被发现。
- 用户再次提供相同方向的概念参考图；仍按白发蓝眼主角、末日科幻、蓝紫枪效和清晰俯视角读取，不将图中的示例代码当作新的执行指令，也不将整张信息图直接切作游戏素材。
- 已补充 `START_HERE.md` 的 Hub/Editor 分步安装、项目建议采用的 Unity 6.3 LTS 系列、按需模块、非同步目录建项与安装验收要求，并核对 Unity 官方说明。
- 本次只修改安装交接文档，未改动游戏源码；未下载安装器、安装 Unity、登录账号、激活许可或创建真实 Unity 工程。
- 当前仍未执行 Unity 编译、Play Mode、画面验收或 Windows 构建。后续需用户完成安装并提供真实项目与 `Unity.exe` 路径，再继续集成和修复。


## 2026-10-23T23:45:29.4290948+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261023_234400_308.md`
- 运行验收：未执行。


## 2026-10-23T23:52:10.9090314+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261023_235156_066.md`
- 运行验收：未执行。


## 2026-10-23T23:55:19.0812618+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261023_235505_384.md`
- 运行验收：未执行。


## 2026-10-23T23:57:52.1202057+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261023_235738_697.md`
- 运行验收：未执行。


## 2026-10-24T01:13:26.4706452+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_011307_693.md`
- 运行验收：未执行。


## 2026-10-24T01:17:48.2726590+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_011731_402.md`
- 运行验收：未执行。


## 2026-10-24T01:20:12.8107017+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_011958_046.md`
- 运行验收：未执行。


## 2026-10-24T10:19:49.8591448+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_101932_609.md`
- 运行验收：未执行。

## 2026-10-24 / 横版重设计：源码集成中

- 按 `docs/CONTRA_SIDE_SCROLLER_REDESIGN.md` 集成横版重力移动、跳跃缓存、土狼时间、蹲伏、下穿平台、冲刺与三档正交视角。
- 新增四角色数据、五主题分段关卡、门禁遭遇、检查点、收集品及五种三阶段 Boss 逻辑；旧版相机与波次逻辑保留为回退分支。
- 修改前备份：`Validation/SideScrollerBackup_20261024_115759`；未删除既有场景或交接文件。
- 当前正在补齐分层角色动画与横版菜单/HUD。上述新代码尚未编译或运行，不标记为通过。
- 视角为 2D 正交战术／肩后／机瞄模拟，不是真正的三维第一人称；程序化美术不等同于商业级正式素材。

## 2026-10-24 / 横版 P0-P2 集成与首次实测

- Unity 6000.6.7f1 已成功编译并产出横版 Development 包；源码静态检查：10,040 项纯 C# 断言、41 个 Unity 源文件×3 宏分支、13 项导入保护检查通过。
- 已接入四角色选择、五关选择、难度、检查点重试/下一关 UI、分层关节动画、角色法线、主题后期、五层视差与 Boss 分阶段机制。
- 720p 首轮可见窗口实测：35 项通过，含长短跳、蹲伏、冲刺、单向平台下穿、土狼时间、落地缓冲、三档视角、三武器和检查点恢复；首关推进随后超时，报告为失败，继续定位。
- 测试报告：`TestResults/Side_1280x720_20261024_123541_222/stage-smoke.json`。不删除失败记录。
- 新增 `docs/SIDE_SCROLLER_DELIVERY.md`，列出实际路径、Inspector 设置、每阶段测试和验收边界。


## 2026-10-24T12:29:49.7623804+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_122921_601.md`
- 运行验收：未执行。


## 2026-10-24T12:33:50.0085160+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_123331_096.md`
- 运行验收：未执行。


## 2026-10-24T12:43:27.8903342+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_124309_705.md`
- 运行验收：未执行。


## 2026-10-24T12:47:55.5707676+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_124736_933.md`
- 运行验收：未执行。


## 2026-10-24T12:54:16.3945337+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_125400_912.md`
- 运行验收：未执行。


## 2026-10-24T12:59:44.8917521+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_125929_111.md`
- 运行验收：未执行。


## 2026-10-24T13:09:17.0420775+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_130901_218.md`
- 运行验收：未执行。


## 2026-10-24T13:16:04.4920388+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_131549_133.md`
- 运行验收：未执行。

## 2026-10-24 / 横版最终实测交付（覆盖上述构建时的未运行状态）

- 最终 Windows EXE：`Builds/Windows/MoriEchoes_20261024_131549_133/MoriEchoes.exe`；Unity BuildPipeline 成功，0 错误、0 警告；启动器已更新。
- 可见独立 Player：1280×720 与 1920×1080 各 107 项检查通过、0 错误、退出码 0。报告分别在 `TestResults/Side_1280x720_20261024_131712_404` 和 `TestResults/Side_1920x1080_20261024_131843_327`。
- 覆盖首关闭环、检查点恢复、五关/五 Boss 三阶段、四角色解锁与差异、三档视角、武器/升级/暂停。测试有无敌、后关传送和 Boss 伤害辅助；未标记真人默认难度通关或正式画质验收。
- 修正自动测试对油桶障碍、输入冷却时序、升级暂停、偶发失焦暂停的处理；同时修正生产代码中的跳跃截断、平台可达高度、Boss 构图和 HUD 截断。
- 42 个源文件语法三宏分支、10,040 项纯 C# 数值断言、13 项导入保护检查通过。39 个原有场景/Packages/ProjectSettings 文件散列未变；参考设计文档并行更新已保留。
- 实现、Inspector、操作/测试/验收及明确待办：`docs/SIDE_SCROLLER_DELIVERY.md`；完整文件清单：`docs/SIDE_SCROLLER_FILE_CHANGES.md` / `.csv`。
- 仍为程序化美术；真实三维第一人称、商业级动漫素材、人工平衡、实体手柄长测、音效听感、性能和跨机均未验收。
- 同一构建的旧模式回归：1600×900、`-moriSmoke` 临时配置，94 项通过、0 错误、退出码 0；报告 `TestResults/Visible_1600x900_20261024_132143_174/runtime-smoke.json`。不将其算作横版或人工难度验收。


## 2026-10-24T19:37:10.1971757+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_193646_578.md`
- 运行验收：未执行。


## 2026-10-24T19:49:02.6020169+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_194845_230.md`
- 运行验收：未执行。


## 2026-10-24T20:22:06.4427113+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_202142_325.md`
- 运行验收：未执行。


## 2026-10-24T20:36:29.3729445+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_203611_055.md`
- 运行验收：未执行。


## 2026-10-24T20:50:41.4326612+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_205025_560.md`
- 运行验收：未执行。

## 2026-10-24 / 第一批动漫素材实际生成、导入与最终验收

- 用户附件中的新凭据成功调用图像接口，旧凭据的 403 阻塞解除。未将密钥写入项目。使用网关请求标识 gpt-image-2.5 制作透明素材、gpt-image-2 制作五关背景，原始 CLI 提示/记录在 `ArtProduction/anime-v1`。
- 保存 17 张原图 + 3 张 Boss 修订图；加工并接入 80 个精灵、63 个近似法线图。包含四角色逐帧动画/头像、四敌人、五 Boss、五背景、三武器、八特效。原图、旧场景、旧构建保留。
- 新增精灵动画、透明切帧/尺寸处理、导入保护、血条/残影/落地/枪焰和阶段图集接入；同步体型落脚位置、菜单文案。13 个原场景相关文件散列未变，Config 改动前已备份。
- 最终包：`Builds/Windows/MoriEchoes_20261024_205025_560/MoriEchoes.exe`，0 编译/构建错误，0 构建警告；`PLAY_MoriEchoes.cmd` 已更新。
- 最终包实测：720p 战役回归 124 项通过；1080p 素材专项 47 项通过、17 张非纯色截图；均 0 运行错误/退出码 0。报告：`TestResults/Side_1280x720_20261024_205304_928/stage-smoke.json`、`TestResults/ProductionArt_1920x1080_20261024_205442_612/stage-smoke.json`。
- 加工器 11 项测试、10,040 项纯 C# 断言、45 个源文件三宏语法分支、13 项原导入保护检查通过。曾遇到隐藏窗口黑图、土狼跳/二段跳自动输入时序失败，失败记录保留，解决经过及测试边界见 `docs/ANIME_ART_DELIVERY.md`。
- 未验收：真人平衡、实体手柄长测、跨机性能、商业最终画质。地面平台/机关仍有程序化表现；仍为 2D 精灵与正交视角，不声称真正三维第一人称或 AAA 品质。


## 2026-10-24T22:08:16.4021221+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_220748_896.md`
- 运行验收：未执行。


## 2026-10-24T22:16:54.9948399+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_221633_563.md`
- 运行验收：未执行。

## 2026-10-24 / Arsenal V2：四角色技能、六武器、动态场景与美术更新

- 按用户确认保留 2D 横版，增加六种独立武器前景、近景瞄准、后坐/装填表现；没有伪称真正三维第一人称。Windows 可缩放/最大化，F11 或 Alt+Enter 切换无边框全屏。
- 四角色立即可选，各有主动技能：DELTA 专注、VANGUARD 护盾、WRAITH 疾行冻结、HAVOC 三连爆破；原脉冲升级作为附加效果保留。六武器独立弹药、菜单出战配置、数字/滚轮/手柄切换、专精增伤；新增 SMG、穿透轨道枪、范围榴弹枪。
- 已实际生成 13 张素材原图和一张 HAVOC 修订图，接入 50 精灵/40 近似法线：四立绘、五 Boss 四帧、六侧视枪、六瞄准前景、八场景部件、六平台。旧人物八帧战斗动画、背景和敌人保留。图像通过已授权的 bundled imagegen CLI API 路径生成，未保存凭据。
- 环境加入近场部件摆动、冲击闪亮、48 个运动扰动微粒、奔跑扬尘、表面命中痕迹和爆炸反馈；Boss 攻击帧同步实际攻击，阶段切换增加波纹/弱点晶体。
- 备份：`Validation/ArsenalBackup_20261024_213751`，配置另备份 `Validation/ArsenalConfig_20261024_220520_367.asset`；13 个旧场景文件散列不变。
- 首轮专项在第六枪合成按键处失败（294 项已通过），报告保留；增强输入就绪等待与诊断后复测通过。原始 HAVOC 头发裁切通过实际图像编辑修复，其他图集按目检透明间隙处理，未覆盖原图。
- 最终实际构建：`Builds/Windows/MoriEchoes_20261024_221633_563/MoriEchoes.exe`，Unity 6000.6.7f1，0 错误/0 警告。根启动器和 HOW_TO_PLAY 已更新。
- 最终包实际运行：1080p 新功能 301 项、720p 战役 124 项、1080p 素材 47 项通过，均 0 运行错误/退出码 0；三份报告分别在 `TestResults/Arsenal_1920x1080_20261024_222044_083`、`TestResults/Side_1280x720_20261024_222255_471`、`TestResults/ProductionArt_1920x1080_20261024_222726_381`。
- 加工器 11 项测试、49 文件 ×3 宏语法检查、10,040 纯模型断言、13 导入保护检查及本批加工幂等重跑通过。自动运行使用合成输入/无敌与伤害辅助，不替代人工平衡、实体手柄长测或跨机性能验收。
- 具体文件、Inspector、测试步骤、验收标准、回退和限制：`docs/ARSENAL_DELIVERY.md`；完整变更清单：`docs/ARSENAL_FILE_CHANGES.csv`。


## 2026-10-24T23:51:18.9222170+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261024_235053_776.md`
- 运行验收：未执行。


## 2026-10-25T00:00:34.4574334+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261025_000013_650.md`
- 运行验收：未执行。


## 2026-10-25T00:32:57.6793764+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261025_003235_211.md`
- 运行验收：未执行。


## 2026-10-25T00:40:37.2357567+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261025_004020_691.md`
- 运行验收：未执行。

## 2026-10-25 / Encounter V3 与四角色特色技能：最终实际验收

- 五关独立20种小兵，40帧敌人动画、20张分关技能特效和40张近似法线图已生成、目检、导入；新增四张角色技能特效。原始图像API输出、修图提示和加工工具保存在 ArtProduction/encounter-v3、ArtProduction/skills-v1；凭据未写入工程。
- Boss 增加蓄力预警、主题弹体/拖尾/命中、落石/冰锥/熔池/激光/召唤/弱点破裂/阶段爆发。修复技能区随 Boss 移动的问题，增加暂停、死亡和重试清理，普通拖尾与预警使用独立固定池。
- 按用户追加反馈移除 Boss 周期性上下浮动；地面 Boss 固定高度，冰霜稳定悬停，终末第二阶段升空一次后稳定。五套独有状态表现为装甲电弧、炉心喷焰、冰晶星环、全息扫描、赤红翼阵，只动画特效子物体。
- 按用户追加要求四人各有独立技能：DELTA战术专注、VANGUARD复苏壁垒（恢复最大HP25%+70%减伤）、WRAITH相位跃迁（瞄准方向最多5.5单位+到达冻结/疾行）、HAVOC三连范围爆破。治疗不超上限，传送检测墙与终点，完全受阻不耗冷却。
- 最终实际构建 `Builds/Windows/MoriEchoes_20261025_004020_691/MoriEchoes.exe`；Unity6000.6.7f1，0错误/0警告，424534219字节。根启动器和HOW_TO_PLAY指向该包。
- 同一最终包实际运行通过：角色/武器专项312项（TestResults/Arsenal_1280x720_20261025_004829_422）、Boss专项164项（TestResults/Encounter_1280x720_20261025_005518_738）、完整战役124项（TestResults/Side_1280x720_20261025_011117_626）。均0运行错误、退出码0；自动测试使用无敌/传送/血量阈值和合成输入辅助。
- Boss专项35截图，19张1280×720/16张最大化2880×1479；战役29次截图记录，3张1280×720/26张2880×1479。不能称为全程固定分辨率性能测试。角色专项14截图含三档主动窗口切换。
- 最终战役首轮缓冲跳合成输入断言失败（19项后），同包未改代码重跑124项通过；测试时序敏感性未根治，失败证据保留。先前角色专项因传送后碰到随机障碍，已将加速验证移到固定空地，墙体阻挡单独实测通过。未将旧包结果冒充最终包结果。
- 54源文件×3宏语法、10040纯模型断言、13既有导入保护、14美术加工测试通过；四技能图散列/原生alpha验证通过。13个原场景/meta散列不变；旧素材和时间戳构建保留。
- 具体文件、Inspector、技能数值、测试步骤、验收标准、备份和测试边界：docs/ENCOUNTER_DELIVERY.md；全部文件变更：docs/ENCOUNTER_FILE_CHANGES.csv。人工难度、实体手柄长测、跨机性能与商业最终美术未验收。


## 2026-10-25T11:26:13.1439996+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261025_112559_527.md`
- 运行验收：未执行。

## 2026-10-25 / 最新Windows便携ZIP实际交付

- 根据用户要求制作完整可分享压缩包，未改变已有战斗规则或场景。嵌入Noto Sans CJK SC静态中文字体，随包保留OFL许可，补齐相对路径启动脚本和中文说明。
- 最新实际构建 `Builds/Windows/MoriEchoes_20261025_112559_527/MoriEchoes.exe`，Unity6000.6.7f1，Windows x64/Mono/Development，0错误/0警告；日志 `Logs/Portable_Font_Build_20261025.log` 最终退出码0。
- ZIP：`Releases/MoriEchoes_20261025_112559/MoriEchoes_最新版_Windows64_20261025_112559.zip`，149697668字节，194个文件/188个运行文件，解压430697335字节。SHA256：`02c81d87dd4cac0f1eb700fb7081ae97fd9f4366c9bd9a2982cea36c1d08a05d`。桌面当前文件夹另提供同一ZIP。
- CRC和逐文件SHA256校验通过；从ZIP解压到工程外“中文 空格路径测试”目录后，对这个解压副本实际运行：角色武器312项、Boss与小兵164项、五关战役124项通过，均0运行错误、退出码0。新报告分别为 `TestResults/Arsenal_1280x720_20261025_113206_614`、`Encounter_1280x720_20261025_113455_632`、`Side_1280x720_20261025_113826_594`。
- 从C:\Windows工作目录实测包内START_GAME.cmd，正常打开响应窗口并正常退出、退出码0；`TestResults/PortableLauncher_20261025_1145/launcher-check.json`通过。首轮测试未提前持有进程句柄，退出码null而判失败，原1142记录保留；修复测试器后同包复测通过。
- 自动验证使用合成输入与战斗辅助，不替代真人平衡、实体手柄长测或跨机器性能。实际本机Windows11中文版64位；没有在别人电脑实测。当前交付为Development试玩构建。
- 完整文件、Inspector、复现步骤与验收标准：`docs/PORTABLE_DISTRIBUTION.md`。修改前备份 `Validation/PortablePackageBackup_20261025_112119_298`；旧构建均保留。


## 2026-10-26T03:29:07.5879344+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261026_032843_413.md`
- 运行验收：未执行。


## 2026-10-26T03:33:20.1262964+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261026_033301_344.md`
- 运行验收：未执行。


## 2026-10-26T03:59:56.4430588+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261026_035936_850.md`
- 运行验收：未执行。


## 2026-10-26T04:09:50.3808197+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261026_040931_483.md`
- 运行验收：未执行。

## 2026-10-26 / 垂直推进三关实际验收

- 新增第 6～8 关：`废墟墓窟`、`沉没圣堂`、`星坠核心井`。三关均为 8 房间、112 单位向下推进路线，包含 7 扇可破坏实体主题闸门、14 个主题机关、地底上涌敌潮和独立三阶段 Boss 攻击/形态/死亡特效。
- 关键实现：`Assets/Scripts/Level/VerticalStageDirector.cs`、`VerticalSkullGate.cs`、`VerticalHazardEmitter.cs`、`VerticalShaftParallax.cs`、`Assets/Scripts/Enemies/VerticalEnemyController.cs`、`VerticalBossController.cs`、`Assets/Scripts/Core/SideCameraDirector.cs`。
- 场景、敌人和特效采用运行时程序化生成；旧横版关卡、场景和已有美术资源保留。竖井分支新增 URP 2D 全局光与玩家轮廓光，已修复新分支 Sprite-Lit 黑屏问题。
- Unity `6000.6.7f1` BuildPipeline 实际成功，构建路径：`Builds/Windows/MoriEchoes_20261026_040931_483/MoriEchoes.exe`；构建报告：`docs/BuildReports/Windows_20261026_040931_483.md`。
- Windows 玩家进程实际运行验收通过：`TestResults/Vertical_20261026_041000_000/vertical-campaign-smoke.json`，53 项通过、0 错误、退出码 0；截图验证：同目录 `stage_6_shaft.png`、`stage_7_shaft.png`、`stage_8_shaft.png`。
- 自动验收使用无敌、血量阈值与位置辅助，不等同于真人通关；人工难度、实体手柄长测、跨机性能和最终商业级美术仍未验收。详细布局、Inspector、操作、测试和验收标准见 `docs/VERTICAL_CAMPAIGN.md`。
- 最终交付：运行标题统一为《末日回响》，竖井入口提示改为简短层数标记。Unity 构建 `Builds/Windows/MoriEchoes_20261026_051615_432/MoriEchoes.exe`，BuildPipeline 0 错误/0 警告。最终构建三关验收 53 项通过、0 错误，报告为 `TestResults/Vertical_20261026_051700/vertical-campaign-smoke.json`。
- 最新分享包：`Releases/MoriEchoes_20261026_052000/MoriEchoes_最新版_Windows64_20261026_052000.zip`，149,775,164 字节，SHA256 `8c0e2d52a91a56e155bfe572026668522b26d584d548278748bc567a8678e375`；CRC、194 个归档文件和 188 个运行文件校验通过，逐文件解压哈希通过。最终包解压副本三关验收报告为 `TestResults/PortableVertical_20261026_052200/vertical-campaign-smoke.json`（53 项通过、0 错误）。桌面工作目录有同名副本。


## 2026-10-26T04:34:18.4832526+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261026_043359_427.md`
- 运行验收：最终构建三关测试 53 项通过、0 错误；最终便携 ZIP 解压副本三关测试 53 项通过、0 错误、退出码 0，结果见下方交付记录。


## 2026-10-26T05:16:33.8547008+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261026_051615_432.md`
- 运行验收：三关测试 53 项通过、0 错误；解压分享包副本三关测试 53 项通过、0 错误、退出码 0，结果见垂直推进交付记录。


## 2026-10-27T00:23:10.7719433+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_002242_471.md`
- 运行验收：未执行。


## 2026-10-27T00:37:36.2875271+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_003710_704.md`
- 运行验收：未执行。


## 2026-10-27T00:43:54.7360530+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_004337_248.md`
- 运行验收：未执行。


## 2026-10-27T00:52:27.3545673+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_005210_989.md`
- 运行验收：未执行。


## 2026-10-27T00:59:37.8437108+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_005921_232.md`
- 运行验收：未执行。


## 2026-10-27T01:19:23.8357449+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_011908_275.md`
- 运行验收：未执行。


## 2026-10-27T01:23:30.7283445+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_012313_084.md`
- 运行验收：未执行。


## 2026-10-27T01:28:36.8051034+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_012821_693.md`
- 运行验收：未执行。


## 2026-10-27T01:50:42.6946329+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_015026_550.md`
- 运行验收：未执行。


## 2026-10-27T02:02:27.9813631+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_020212_473.md`
- 运行验收：未执行。


## 2026-10-27T02:03:43.7829997+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_020328_234.md`
- 运行验收：未执行。


## 2026-10-27T02:11:42.0370729+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_021126_076.md`
- 运行验收：未执行。


## 2026-10-27T02:19:39.3008863+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_021923_891.md`
- 运行验收：未执行。


## 2026-10-27T02:30:54.7768502+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_023038_851.md`
- 运行验收：未执行。


## 2026-10-27T02:37:36.9544526+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_023721_551.md`
- 运行验收：未执行。


## 2026-10-27T11:14:04.9812323+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_111348_759.md`
- 运行验收：未执行。


## 2026-10-27T11:41:12.4435668+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_114056_372.md`
- 运行验收：未执行。


## 2026-10-27T11:49:34.3817967+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_114917_419.md`
- 运行验收：未执行。


## 2026-10-27T12:44:39.6211625+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_124413_210.md`
- 运行验收：未执行。


## 2026-10-27 / Boss 实体装备与高防御：运行回归中

- 已生成 16 张 gpt-image-2.5 请求设计板，最终接入 96 个实体装备部件、32 个终极特效帧、96 张加工法线。12 张 RGB 棋盘底已本地抠图，原图和修订记录保留。
- Boss 减伤 62%、生命 ×1.5、单次命中上限 0.8%；跳字显示实际伤害。常规/终极技能伤害乘区 2.25/3.25，横版范围乘区 1.65/1.9；处决保留。
- R20 构建被既有 8 关配置与新 10 关校验冲突阻断，已修复为兼容 8/10 关；保留正在添加的新关卡文件。本轮验收当前 8 个已配置主题。
- R21 实际 Windows 构建成功，0 错误/0 警告：Builds/Windows/MoriEchoes_20261027_124413_210/MoriEchoes.exe。源码 83 文件三套定义语法、10040 模型断言和 13 导入安全检查通过。
- 同一 EXE 正在运行 Encounter、Arsenal、Art、Vertical、Side 五组测试；尚未全部完成，不标记最终通过。已目视检查新实体部件与实际大招截图，HUD 生命/弹药文字正常。
- 完整 Inspector、数值、测试和边界见 BOSS_ASCENSION_ACCEPTANCE.md。最终启动路径将在所有运行检查后更新；不制作 ZIP。


## 2026-10-27T13:09:26.7052210+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_130910_559.md`
- 运行验收：未执行。

## 2026-10-27 / R21 复核发现与 R22 修正

- R21 的 Encounter / Arsenal / Art / Vertical 分别通过 345 / 577 / 48 / 78 项；横版自动驾驶在第一关高台阻挡处重复跳跃，300 秒超时失败，不能作为最终完整通关证据。
- 自动驾驶加入真实 S+Space 下穿输入，不跳过该路线。目视发现的竖井粗略骷髅/圆环已换成主题实体浮雕；SceneFeedback 保留原透明度与朝向；演出锁定同时停止重力；镜头修正 Boss 取景。
- R22 构建 Windows_20261027_130910_559 成功，0 错误/0 警告。五组最终测试正重新针对同一 EXE 执行。结果以之后的最终验收记录为准。


## 2026-10-27T13:17:39.8894287+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_131724_160.md`
- 运行验收：未执行。

- R22 横版全流程 132 项通过；新增竖井锁定检查失败后定位到测试读了 Rigidbody2D 插值尚未同步的 Transform。比较对象已改为同一 Rigidbody2D.position，并把起止高度写入检查描述；不放宽允许漂移阈值。R23 将重新运行最终测试。


## 2026-10-27T13:33:20.186006+08:00 / 生成美术、高难度与最终运行回归

- 原装 imagegen CLI 经指定网关请求 gpt-image-2.5；56 张原图，313 个 Sprite，234 张加工法线，提示词、源图、裁切修订和配置备份已保留。
- Boss 新增 16 张设计原图、96 个实体装饰部件、32 个大招帧、96 张加工法线；12 张 RGB 棋盘底原图本地抠图。减伤 62%、生命 ×1.5、单次上限 0.8%，实际伤害跳字、处决和自然大招已验证。
- 前五关四类敌兵、机关、实体模块、独立守卫、Boss 后两阶段、攻击/命中/死亡效果接入；新增四人及后三关/三武器生成素材接入。名单严格 8 人，原四人配置和资源保留。
- 提高敌伤、机动与 Boss 区域覆盖；能量胶囊基础总弹丸伤害只 +8%～10%。位移技能统一实体碰撞保护，受阻保留冷却/能量。
- 按最新追加要求移除八关攻击预判图示与下一招攻略文字，实际攻击/命中效果保留；Boss 伤害区域蓄力时不可见，出招后仅展示攻击纹理。
- 实际构建 `Builds/Windows/MoriEchoes_20261027_131724_160/MoriEchoes.exe`，Unity 6000.6.7f1，0 错误/0 警告。五组运行报告如下，均 passed=true、0 错误、退出码 0。

| 运行组 | 通过检查 | 错误 | 证据 |
|---|---:|---:|---|
| arsenal | 577 | 0 | `TestResults/Arsenal_1280x720_20261027_132455_096/stage-smoke.json` |
| encounter | 345 | 0 | `TestResults/Encounter_1280x720_20261027_132017_952/stage-smoke.json` |
| art | 48 | 0 | `TestResults/ProductionArt_1280x720_20261027_132704_012/stage-smoke.json` |
| campaign | 132 | 0 | `TestResults/Side_1280x720_20261027_132806_955/stage-smoke.json` |
| vertical | 87 | 0 | `TestResults/Vertical_20261027_131844_844/vertical-campaign-smoke.json` |

- 源码模型、语法和导入安全检查另已通过。启动器已指向本轮验证构建，未生成新 ZIP。
- Inspector、重测步骤、验收标准、完整变更和限制见 `docs/PREMIUM_ART_ACCEPTANCE.md`。人工平衡、实体手柄、跨机性能、长时间内存和 Editor Play Mode 尚未验证。


## 2026-10-27T17:51:44.9036030+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_175111_260.md`
- 运行验收：未执行。


## 2026-10-27T18:18:11.5005956+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_181744_077.md`
- 运行验收：未执行。


## 2026-10-27T18:23:17.3403094+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_182246_974.md`
- 运行验收：未执行。


## 2026-10-27T18:32:07.8396129+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_183143_926.md`
- 运行验收：未执行。


## 2026-10-27T18:39:50.1494750+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_183925_524.md`
- 运行验收：未执行。


## 2026-10-27T18:53:07.1090751+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_185243_819.md`
- 运行验收：未执行。


## 2026-10-27T19:18:40.1126181+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_191752_350.md`
- 运行验收：未执行。


## 2026-10-27T19:23:27.3286550+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_192303_908.md`
- 运行验收：未执行。


## 2026-10-27T19:26:52.5524397+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_192630_786.md`
- 运行验收：未执行。


## 2026-10-27T19:37:35.7636371+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_193708_374.md`
- 运行验收：未执行。


## 2026-10-27T19:47:45.6584950+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_194725_078.md`
- 运行验收：未执行。


## 2026-10-27T19:57:14.3341955+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_195651_295.md`
- 运行验收：未执行。


## 2026-10-27T19:59:19.6646753+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_195857_445.md`
- 运行验收：未执行。


## 2026-10-27T20:08:13.8679711+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_200756_086.md`
- 运行验收：未执行。


## 2026-10-27T20:12:44.0816777+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_201225_787.md`
- 运行验收：未执行。


## 2026-10-27T20:20:32.3238743+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_202013_818.md`
- 运行验收：未执行。


## 2026-10-27T20:28:59.0785247+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261027_202840_130.md`
- 运行验收：未执行。

## 2026-10-27 / 平台统一、伤害反馈与八关通路：最终运行验收

- 最终程序：`Builds/Windows/MoriEchoes_20261027_202840_130/MoriEchoes.exe`。Unity 6000.6.7f1，实际构建成功，0 错误 / 0 警告；启动器 `PLAY_MoriEchoes.cmd` 已更新。上条“未执行”为构建工具当时的状态，本条记录随后完成的运行结果。未制作 ZIP。
- 八关普通/单向/冰面/传送带平台、地面、墙体、电梯、隐藏桥与密室、Boss 可站立临时结构统一使用所属关卡主材质；复用既有生成素材，不声称本轮新生图。移除电梯行程中的旧短台和无用常开门墙，保留有效边界、落脚点及可破坏掩体。
- 敌兵/机关/Boss 常规技/大招单次伤害限制为基础生命的 24%/28%/36%/45%，再受当前角色最大生命 55% 上限和护盾减伤约束；0.6 秒受击保护。满血普通承伤不会一击毙命，低血仍可死亡；坠落失败独立处理。
- 受击 HP 即时下降、红色损失段延迟平滑消退、人物与屏幕命中特效、致死前完整掉血反馈均已接入。Boss 保留最新要求的半透明红色预警，小兵旧彩色调试线块隐藏。
- 八关可直接选择；每关仅一个中段接触门，守门增援更多；主路与独立密室分离，独特守卫与奖励只结算一次。第 6–8 关上射可破闸门，交错平台可连续跃进，检查点保留破闸状态，最终 Boss 实体平台 y=101.0。
- 同一最终 EXE 的五组可见 Player 测试全部通过：Feedback/Traversal **1258**、Side **132**、Encounter **360**、Arsenal **577**、Vertical **87**，共 **2414** 项，退出码均 0，报告均无错误。八关接触进出密室、伤害矩阵、HP 动画和 Boss 预警均覆盖；三竖井以真实跳跃输入贯通并落到 Boss 平台。使用无敌/伤害辅助，不能等同真人难度通关。
- 最终报告目录：`TestResults/FeedbackTraversal_20261027_203021_529`、`Side_1280x720_20261027_203021_598`、`Encounter_1280x720_20261027_203021_696`、`Arsenal_1280x720_20261027_203440_642`、`Vertical_20261027_204425_115`（后四项同属 `TestResults/`）。每组有 `process-evidence.json` 记录实际 EXE 和退出码。
- 源码检查 PASS：10040 条纯模型断言、5000 次固定种子抽样，86 个 C# 文件 × 3 套语法定义，9 份 PowerShell，13 项导入安全检查。源码校验不代替上述 Unity 构建与 Player 测试。
- 历史失败已保留。最终 Vertical 首轮并行窗口测试 `Vertical_20261027_203440_730` 超时无结果，未记为通过；单独重跑 `Vertical_20261027_204425_115` 完整通过。更早的平台误归类、密室误入及最终跳台问题已修复重测。
- 实际文件、Inspector、手动步骤、验收标准：`docs/FEEDBACK_AND_TRAVERSAL_ACCEPTANCE.md`。完整变更及哈希：`Validation/FeedbackTraversal_20261027_Delivery/file-changes.json` / `.csv`；交付证据：同目录 `delivery-verification.json`；八关截图合图：`stage-surfaces-review.jpg`。
- 未验收：真人默认难度全流程、实体手柄长测、跨机性能与兼容性、Editor Play Mode、商业最终美术认证。保留现有 8 人 9 武器和 8 个配置关卡；第 9、10 关不在本轮范围内。


## 2026-10-28T00:44:40.5838120+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261028_004419_204.md`
- 运行验收：未执行。


## 2026-10-28T00:48:07.6937000+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261028_004734_915.md`
- 运行验收：未执行。


## 2026-10-28T00:57:12.6937539+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261028_005654_534.md`
- 运行验收：未执行。


## 2026-10-28T01:04:13.6956534+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261028_010354_838.md`
- 运行验收：未执行。


## 2026-10-28T01:28:15.2979842+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261028_012757_512.md`
- 运行验收：未执行。

## 2026-10-28 / 首页人物技能说明与第 9、10 关：最终运行验收

- 最终程序：`Builds/Windows/MoriEchoes_20261028_012757_512/MoriEchoes.exe`。Unity 6000.6.7f1，StandaloneWindows64 / Development，实际构建成功，0 错误 / 0 警告，17.286 秒。`PLAY_MoriEchoes.cmd` 已指向本轮程序。上条“未执行”是构建工具当时的状态，本条补录随后完成的实际 Player 测试。按用户要求未制作 ZIP。
- 首页保留八人，新增常驻技能面板：Q 主技能、F/C 战术技能、X 终结技，均显示对应名称与作用；Q/F/C 显示实际冷却，X 显示 100 能量条件和攻击/受击充能。角色切换刷新 32 项技能说明与基础属性，菜单说明通过 `CharacterAbilityCatalog` 集中管理，冷却直接读取运行组件。
- 保留原八人的形象、数值和九把武器；配置保留原八关后追加风暴海面和深渊热泉，现有十关可选。新两关各 8 段、全长 240 米，没有隐藏路线。
- 海面运动不再读取地面波面引用，支持浅潜、跃出与小幅纵向相机跟随；水下采用渐进加速和深度阻尼，取消地面跌落死亡线，接入氧气、高压周期伤害与热泉补氧。胶囊在当前水层悬浮，敌兵区分露头/潜行/全水层，水中控制技能可震慑小兵。
- 水关 Boss 三相在软边界内缓动，弹幕带气泡尾迹、枪口喷溅；环境光柱与气泡仅在相机附近更新。Boss 蓄力红区跟踪到释放前约 0.22 秒，攻击落点有分层冲击效果；海面巨浪和深渊热泉死亡反馈各异。所有主 Boss 必须生命归零才结算，登记入口统一为 `LevelManager.SetBoss`。
- 同一最终 EXE 的四组可见 Player 回归全部通过：Swim/Menu **168**、Encounter **410**、Arsenal **601**、Vertical **99**，共 **1278** 项；四组错误列表为空、退出码均 0。报告目录依次为 `TestResults/Swim_20261028_013538_609`、`Encounter_1280x720_20261028_013005_457`、`Arsenal_1280x720_20261028_013840_902`、`Vertical_20261028_014411_766`（后面三项同属 `TestResults/`）。各 `process-evidence.json` 记录最终 EXE。水关专项输出 15 张非空截图，已实际查看 1280×720、1920×1080 菜单截图。
- 静态检查：10040 条纯模型断言、5000 次固定种子抽样、88 个 C# 文件 × 3 套定义语法检查、10 份 PowerShell 和 13 项隔离导入检查通过。88 个源码哈希与当前工程一致；这些检查不代替实际 Unity 编译和 Player 运行。
- 历史失败保留：旧 Encounter 测试直接生成 Boss 而未登记，触发生命归零通关门槛拦截，旧目录 `Encounter_1280x720_20261028_011245_692` 不算通过。改用统一登记入口后，最终构建 410 项通过。Final 构建调用的 Editor 方法命名空间错误已改正，实际成功日志为 `Logs/Build_SwimMenu_20261028_Final2.log`。更早的编译、配置与测试时序失败也保留，未以源码检查冒充运行通过。
- 具体文件、Inspector 设置、手动步骤、验收标准与复测命令：`docs/SWIM_AND_CHARACTER_MENU_ACCEPTANCE.md`。25 个 C# 文件及配置、元数据、测试入口、说明、启动器的清单和 SHA-256：`Validation/SwimMenu_20261028_Delivery/file-changes.json` / `.csv`；交付证据索引为同目录 `delivery-verification.json`。
- 限制：本轮无新 image 生图；水关使用程序化环境和现有兜底 Sprite，尚不代表正式动漫美术交付。测试使用合成输入、位置/血量/无敌辅助、加速氧气计时与阶段阈值，不等同真人正常难度通关；未验收实体手柄长测、跨机性能、Editor Play Mode 和新两关最终美术。


## 2026-10-28T16:55:39.8744638+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261028_165456_897.md`
- 运行验收：未执行。


## 2026-10-28T17:15:27.3505387+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261028_171506_913.md`
- 运行验收：未执行。

## 2026-10-28 / 水关 image 正式素材、海面浮台与十关 Boss 各新增三招：已完成构建与 Player 验收

- Image API 已实际调用成功，经用户指定兼容网关请求 `gpt-image-2.5`，使用未经修改的内置 CLI。24 张原图完整保留：水关 14 张、技能 10 张；不能独立认证兼容网关后台实际模型版本。历史 401/403 是之前凭据/权限状态，不代表本轮仍未生图。凭据只保存在本机用户目录的 DPAPI 加密文件中，不进入工程或游戏。
- 绑定 `Assets/Art/Production/SwimAnimeV2` 的 86 张 Sprite 与 60 张 2D 辅助法线，以及 `BossTechniquesV1` 的 60 张技能 Sprite；共 146 张 Sprite，其中 144 张为原生透明、2 张为背景。源图及处理后图像均有 SHA-256。原八人、九种武器和原八关的现有美术配置保留，新招独立追加。
- 每关新增 3 招，共 30 招，名称/释放方式/视觉/闪避说明见 `docs/BOSS_TECHNIQUE_DESIGN.md`。三形态各解锁一招，每两次原有攻击后穿插一次，具有独立图像和攻击轨迹。新增招式预警开始即固定世界位置/完整路径，不锁死跟随、不冻结或强吸附角色；基础伤害受既有玩家承伤上限约束。
- 原有 Boss 目标预警仅开始短暂跟踪，前五关/竖井/水关分别提前至少约 0.85/0.95/1.15 秒固定落点；水关实测键盘移动可逃离固定攻击体。原有招式、三相、零血量通关门槛和死亡演出回归通过。
- 第 9 关新增 12 组、共 24 块低/高浮台，支持真实键盘从水跃上、连续换台、站稳随波、S+Space 下穿；海面上界放宽至 +6.2 米以容纳完整跳跃弧线。第 10 关保留深度游泳、氧气与水压、热泉补氧；两关各有专属背景、小兵、三相 Boss、实体装饰、环境模块与大招效果，没有隐藏路线。
- 最终程序 `Builds/Windows/MoriEchoes_20261028_171506_913/MoriEchoes.exe`。Unity 6000.6.7f1 / Windows x64 Development；实际构建成功，0 错误 / 0 警告，19.813 秒。`PLAY_MoriEchoes.cmd`、README 与操作说明已更新到此版本。未生成 ZIP。上条“运行未执行”为构建工具当时状态，本条补录随后验收。
- 同一最终 EXE 的四组可见 Player 测试全部通过：Swim **193**、BossTechniques **389**、Encounter **410**、Vertical **99**，共 **1091** 项；错误列表均为空、进程退出码均 0。实际目录为 `TestResults/Swim_20261028_172640_621`、`BossTechniques_20261028_172804_287`、`Encounter_1280x720_20261028_173006_158`、`Vertical_20261028_173510_486`（后 3 项同属 TestResults）。技能专项 60 张截图，水关专项 16 张截图；实际查看素材总览、两关 Boss、浮台和多个技能画面。
- 首轮成功后进一步修复水面硬边遮罩和细长攻击插画过小，最终 EXE 上重新跑完四组验收。切片早期因分隔线检查过严中断，旧中间产物 `SwimAnimeV1` 保留未绑定；完整绑定版本为 V2，没有覆盖旧图。静态检查另通过 10040 条模型断言、5000 次抽样、91 个 C# 文件 × 3 套定义、13 项隔离导入检查。
- 完整文件清单、哈希及证据索引：`Validation/SwimTechnique_20261028_Delivery`；Inspector/启动/手动步骤/标准见 `docs/SWIM_ART_AND_BOSS_TECHNIQUES_ACCEPTANCE.md`。
- 测试使用合成输入、位置/血量/无敌辅助；30 招专项设置形态和调度计数以覆盖全部技能，不冒充真人正常难度通关。人工平衡、实体手柄长测、Editor Play Mode、最终美术审定尚未验收；跨机测试按用户要求取消。


## 2026-10-29T09:40:20.8554772+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261029_093906_889.md`
- 运行验收：未执行。


## 2026-10-29T09:50:47.3633280+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261029_095017_637.md`
- 运行验收：未执行。


## 2026-10-29T10:51:26.2443915+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261029_105059_224.md`
- 运行验收：未执行。


## 2026-10-29T11:09:01.2062248+08:00 / L：实际构建

- BuildPipeline: Succeeded; EXE exists: True
- Report: `C:\UnityProjects\MoriEchoes\docs\BuildReports\Windows_20261029_110837_855.md`
- 运行验收：未执行。
