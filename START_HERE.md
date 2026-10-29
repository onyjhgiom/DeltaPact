<!-- premium-current -->
# 当前版本入口 · 2026-10-27

双击 `C:\UnityProjects\MoriEchoes\PLAY_MoriEchoes.cmd`。当前工程已集成生成美术，8 人、9 武器、8 关；实际程序 `C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261027_131724_160\MoriEchoes.exe`。操作见 `HOW_TO_PLAY.md`，本轮所有运行证据、Inspector 与限制见 `docs/PREMIUM_ART_ACCEPTANCE.md`。没有生成新 ZIP。

以下是历史集成记录，其旧版路径、角色数量、灰盒描述与安装建议不代表当前版本。

---

# 末日回响：启动入口

## 最新越肩视角版本

双击 `C:\UnityProjects\MoriEchoes\PLAY_MoriEchoes.cmd`。当前 EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261024_101932_609\MoriEchoes.exe`，已真实构建，三档分辨率各 94 项自动检查通过。鼠标水平转向，左键射击，右键开镜；Esc 释放鼠标。详细操作见 `HOW_TO_PLAY.md`，相机参数和验收证据见 `docs/CAMERA_IMPLEMENTATION_AND_TESTS.md`。以下保留首次集成历史。

## 真实交付状态

**2026-10-24 更新：真实工程已在 `C:\UnityProjects\MoriEchoes` 集成，使用 Unity 6000.6.7f1。Windows 构建成功，0 错误、0 警告；720p/900p/1080p 各通过 51 项可见窗口自动检查。**

先阅读真实工程中的 `HOW_TO_PLAY.md`，双击 `C:\UnityProjects\MoriEchoes\PLAY_MoriEchoes.cmd` 即可本机试玩。实际 EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261023_235738_697\MoriEchoes.exe`。证据和限制见 `docs/UNITY_INTEGRATION_REPORT.md`。本机不需要重复安装、建项或导入。

原始 OneDrive 源码目录仍是交接副本，不包含 Unity 的 Packages/ProjectSettings，也不是 Windows 包目录。下面保留其他机器首次导入的准备步骤；当前已集成工程直接跳到第 3/4/5 节。

- 已写入玩家、三种武器、五类敌人、Boss、经验升级、波次、菜单、HUD、暂停、结算和程序化占位视觉/音效。
- 运行时由 `GameBootstrap` 自动组成游戏对象，不需要逐个拖脚本。编辑器工具负责创建缺失的配置、材质和三个入口场景。
- 原始源码目录没有 `Packages/manifest.json`、`ProjectSettings/ProjectVersion.txt`；真实工程已具备这些文件。已执行真实编译/构建和独立 Player 运行；Editor Play Mode 仍未执行。
- `Validation/*.exe` 仅为开发校验工具，绝不是游戏；实际游戏在前述真实工程的 Builds/Windows 子目录。
- 美术为程序化灰盒；没有正式角色动画、精细场景建模、制作级后处理或已验收的“高级画面”。概念参考图没有被当作 Sprite 图集。
- 2026-10-23，用户确认尚未安装 Unity。再次提供的概念图继续作为美术方向参考，不据此认定已有可用角色动画、场景资源或 Unity 环境。

## 1. 准备真实 Unity 工程

如果已经有 Unity 6 工程，先备份该工程，使用它的原版本，不要为此升级引擎。必须能看到它的 `Assets`、`Packages/manifest.json` 和 `ProjectSettings/ProjectVersion.txt`。

如果没有工程：

1. 从 Unity 官方下载页面安装 **Unity Hub**，启动后自行完成 Unity ID 登录及适用许可的激活。Hub 和 Unity Editor 是两个安装步骤；只有 Hub 不足以打开或编译游戏。本轮没有下载安装软件、代为登录或接受许可条款。
2. Hub 的 **Installs（安装）> Install Editor（安装编辑器）** 中，本项目建议先固定 **Unity 6.3 LTS（6000.3.x）**，选择该系列实际提供的正式补丁，不选 Alpha/Beta。这是本项目为稳定集成作出的版本选择，不是声称已在该版本验证，也不是声称它是最新 Update 版本。安装后以 `ProjectSettings/ProjectVersion.txt` 记录的实际版本为准。
3. 只选择本项目需要的模块，不必安装 Android/iOS/WebGL。若计划采用 IL2CPP，添加该 Editor 对应的 **Windows Build Support (IL2CPP)**，后续还需确认 C++ 工具链。Hub 可通过 **Installs > 对应 Editor 的 Manage > Add modules** 补装；具体可选项以安装界面为准。现有 Visual Studio 不等于 Unity 模块或 C++ 工具链已齐备。
4. 选择 **Universal 2D** 模板，在本源码目录之外的本地非同步位置创建新工程，例如空间足够时使用 `C:\UnityProjects\MoriEchoes`。这是建议路径，尚未创建；也可自行选择其他磁盘。不要直接把本目录交给 Hub 当作现成工程打开，也不要选择 3D 模板。
5. 打开新工程，等待包导入完成。记下实际 Editor 版本和工程完整路径。
6. `Window > Package Manager`，选择 Unity Registry，安装/确认 **Input System**、**Universal RP**、**Unity UI (UGUI)**。使用该 Editor 推荐的兼容版本，不手改未知版本号。
7. `Edit > Project Settings > Player > Other Settings > Configuration > Active Input Handling` 选 **Input System Package (New)**，按提示重启 Editor。
8. 本实现直接读取 Input System 设备；不需要 `PlayerInput` 组件、不需要自行建立 Input Actions。UI 模块在运行时创建默认 Actions。

其他机器的安装验收：Editor 能打开 Universal 2D 空工程，Console 无红色错误；工程中存在 Assets、包清单和 ProjectVersion。提供工程与 Editor/Unity.exe 的实际路径，不要把 Hub 的 EXE 当作 Editor。本机已核验 6000.6.7f1，没有按旧建议切换 6.3。

官方资料（2026-10-23 核对；版本/界面以后可能变化）：

```text
下载 Hub：https://unity.com/download
安装 Hub：https://docs.unity.com/en-us/hub/install-hub-win-mac
Unity 6 支持版本：https://unity.com/releases/unity-6/support
补装 Editor 模块：https://docs.unity.com/en-us/hub/add-modules
```

## 2. 不覆盖地导入源码

从本目录打开 PowerShell，替换下面的工程路径为真实路径。先退出目标工程的 Play Mode，建议关闭 Editor 后再复制。

```powershell
.\Tools\Import-IntoUnityProject.ps1 -ProjectPath 'D:\UnityProjects\MoriEchoes'
```

这是预览，不会修改目标工程。检查输出后执行：

```powershell
.\Tools\Import-IntoUnityProject.ps1 -ProjectPath 'D:\UnityProjects\MoriEchoes' -Apply
```

- 只复制缺失文件，并补齐空目录。原文件相同则跳过；文档同名但内容不同则保留目标版本。
- 任意 `Assets` 同名内容冲突都会在复制前终止。不要用强制覆盖解决冲突，应先逐个比较。
- 不修改 `Packages`、`ProjectSettings`；不复制诊断 EXE 或 Validation 目录。
- 如果 PowerShell 执行策略不允许脚本，请在符合你电脑安全策略的方式下执行或手动复制，勿修改全局执行策略。
- 手动复制也必须保留已有文件：`Assets`、`docs`、`prompts`、`CodexPrompts`、`Tools` 和本启动说明。Windows 上 `Docs` 与 `docs` 是同一目录。

## 3. 在 Unity 生成入口场景

1. 打开目标工程，等待脚本编译结束。先解决 Console 红色错误，不要在编译失败状态下继续。
2. `Tools > Mori Echoes > 1. Check Environment`。Console 应输出实际 Unity 和 Input System 版本。
3. `Tools > Mori Echoes > 2. Prepare Prototype Scenes`。
4. 工具仅在路径空闲时生成：
   - `Assets/Settings/MoriEchoesConfig.asset`
   - `Assets/Settings/MoriEchoesSpriteUnlit.mat`
   - `Assets/Scenes/Boot/Boot.unity`
   - `Assets/Scenes/MainMenu/MainMenu.unity`
   - `Assets/Scenes/Battle_01/Battle_01.unity`
5. 旧场景和配置不会被重建。若验证提示已有同名场景不符合要求，按 `docs/INSPECTOR_AND_TESTS.md` 手动配置，不要删除你的内容。
6. `Tools > Mori Echoes > 3. Open Main Menu`，点击 Play。点击“进入战区”开始。
7. 场景初始只有 `MoriEchoesBootstrap` 是正常现象：游戏对象、相机、碰撞、UI 和对象池均在运行时创建。退出 Play 后运行对象消失，不要将其误判为场景资源遗失。

Boot/MainMenu 是菜单入口，Battle_01 是直接战斗入口。菜单到战斗采用同场景状态切换，当前不是异步场景加载架构。不要同时以 Additive 方式运行三个入口场景。

## 4. 操作

| 操作 | 键鼠 | 可选手柄 |
| --- | --- | --- |
| 移动 | WASD | 左摇杆 |
| 瞄准 | 鼠标位置 | 右摇杆 |
| 射击 | 按住鼠标左键 | 右扳机 |
| 闪避 | Space | 南侧按键 / A |
| 装弹 | R，空仓射击也会触发 | 西侧按键 / X |
| 换枪 | 1 / 2 / 3 | 左右肩键循环 |
| 脉冲 | Q，先在升级中解锁 | 北侧按键 / Y |
| 暂停 | Esc | Start |
| UI 选择 | 鼠标点击 | 方向导航与确认 |

默认 AR-07、SG-12、SR-09 均可用。每波计时推进，拾取碎片触发三选一；第十波结束并经过间歇后出现 Boss。击败 Boss 胜利，玩家生命归零失败。默认流程预计约十分钟以上，实际难度和时长尚未试玩校准。

## 5. 验证与构建

- 精确 Inspector、逐阶段测试及验收：`docs/INSPECTOR_AND_TESTS.md`
- 最新实际 Unity 编译、构建和独立运行：`docs/UNITY_INTEGRATION_REPORT.md`
- 实际测试状态与限制：`docs/ACCEPTANCE_REPORT.md`
- 正式素材接入：`docs/ART_INTEGRATION.md`
- Windows 构建步骤及未构建说明：`docs/BUILD_REPORT.md`
- 完整文件清单：`docs/CHANGES.md`
- 连续开发进度：`docs/PROGRESS.md`

本目录可独立运行的校验：

```powershell
.\Tools\Validate-Source.ps1
```

这只验证无 Unity 依赖的模型及 C# 语法，**不替代 Unity 编译或游戏测试**。真正的 Windows 构建必须在真实 Unity 工程中运行 `Tools > Mori Echoes > 4. Build Windows x64 Prototype`，并继续人工运行验收。
