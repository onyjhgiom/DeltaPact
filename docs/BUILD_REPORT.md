# Windows 构建状态与执行方法

## 当前交付：2026-10-27 平台统一、受击与通路修复

当前 EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261027_202840_130\MoriEchoes.exe`，`PLAY_MoriEchoes.cmd` 已指向此版本。Unity 6000.6.7f1 / StandaloneWindows64 Development，实际 BuildPipeline 成功，0 错误 / 0 警告。

完整运行结果、Inspector 设置、复测步骤和测试边界见 `FEEDBACK_AND_TRAVERSAL_ACCEPTANCE.md`；构建原始记录见 `BuildReports/Windows_20261027_202840_130.md`；可追溯的 EXE 哈希、各组测试路径和源码变更清单见 `../Validation/FeedbackTraversal_20261027_Delivery/`。本轮未制作 ZIP。以下记录为历史版本，不能替代本轮运行结果。

## 历史越肩版本：2026-10-24

`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261024_101932_609\MoriEchoes.exe` 当时已实际构建并运行；0 编译/构建错误、0 构建警告，720p/900p/1080p 各 94 项自动检查通过。启动器现已更新，不再指向此包。原始构建报告：`BuildReports/Windows_20261024_101932_609.md`；具体范围、截图、Inspector 与限制：`CAMERA_IMPLEMENTATION_AND_TESTS.md`。下节旧包为首次集成历史，仍保留。

## 本次实际结果

- 更新：2026-10-24。真实工程：`C:\UnityProjects\MoriEchoes`。
- Editor：`C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`。
- Unity 编译、BuildPipeline：**实际成功，最终构建 0 错误 / 0 警告**。Editor Play Mode 未执行，运行测试来自独立 Player。
- Windows 游戏 EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261023_235738_697\MoriEchoes.exe`，已实际生成并运行。
- 原始报告：真实工程 `docs/BuildReports/Windows_20261023_235738_697.md`。三档分辨率各通过 51 项可见窗口自动检查。
- 实际范围、失败历史、日志/截图与命令见 `UNITY_INTEGRATION_REPORT.md`。短波自动测试不等于默认十波通关或最终美术验收。
- `Validation/RunModelTests.exe` 与 `Validation/SourceValidator.exe` 是校验工具，不是《三角洲契约》。

## 构建前置

按 `START_HERE.md` 导入真实 Universal 2D 工程，使用该工程匹配的 Unity 6 Editor。Unity Hub 中为同一版本安装 Windows Build Support；选择 IL2CPP 时还需对应模块与 Windows C++ 工具链，不能把本机存在 Visual Studio 当作模块已安装。

先完成 Console 无红错、菜单/战斗/升级/胜负/重开/音画实测。确认有可用 Unity 许可、磁盘空间与可写目录。退出 Play Mode，保存场景。在 Config 指定可随包分发的中文字体，并确认 Sprite Material 已引用 Unlit 材质。

## 推荐：项目内安全构建工具

1. `Tools > Mori Echoes > Verify Scene Bindings`。检查每个入口场景只有一个有效 GameBootstrap，正确引用 Config/Material；Boot/MainMenu Start In Menu 勾选，Battle_01 不勾选。
2. `Tools > Mori Echoes > 4. Build Windows x64 Prototype`。
3. 工具构建 Development / StandaloneWindows64，场景列表仅为 Boot、MainMenu、Battle_01，Boot 首场景。既有 Build Settings 的其他场景不会被删除，但不自动进入本游戏测试包。
4. 工具创建**全新时间戳目录**，避免覆盖已有输出。下面只是未来输出格式，不是已存在的交付：

```text
<真实 Unity 工程>/Builds/Windows/MoriEchoes_<yyyyMMdd_HHmmss_fff>/MoriEchoes.exe
```

5. 实际构建结束后记录 `docs/BuildReports/Windows_<时间戳>.md`，包含 Unity、结果、错误/警告、EXE 存在性、耗时和包体大小，并追加 `docs/PROGRESS.md`。
6. BuildPipeline 成功但游戏未经运行时，报告仍明确标注“未运行验收”。工具前置检查抛错时可能尚未进入 BuildPipeline，应保留 Console 日志，不得伪造构建报告。

## 备选：Unity 6 Build Profiles 手动操作

1. 打开 `File > Build Profiles`，添加/选择 Windows 桌面配置，Architecture 选 Intel 64-bit / x86_64（具体标签依安装版本），切换激活平台并等待重新编译。
2. 将 Boot、MainMenu、Battle_01 添加到该配置的 Scene List，勾选启用，确保 Boot 排在首位。不要删除其他项目配置中的场景。
3. Player Settings 中确认 Active Input Handling = Input System Package (New)，Default Screen Width/Height 可设 1920/1080，允许窗口测试。脚本后端采用该工程已配置并安装支持的后端，不盲目切换或升级。
4. 启用 Development Build 供测试，选择一个新空输出目录，Build。Console/Editor.log 无构建错误且真实 EXE 存在才记录成功。
5. 保留全部相邻文件，如 `UnityPlayer.dll`、`MoriEchoes_Data`、运行时目录等；不能只交付单个 EXE。

## 已有工程的命令行入口

以下参数路径均为你替换的示例，不是本机已发现路径。先手工配置包、Input Handling、材质和场景并通过编译，关闭该工程其他 Editor 实例，避免锁冲突。

```powershell
& '<实际 Unity.exe 路径>' -batchmode -quit -projectPath '<真实工程完整路径>' -executeMethod MoriEchoes.EditorTools.MoriEchoesSetup.PreparePrototypeScenes -logFile '<新建日志完整路径>'
& '<实际 Unity.exe 路径>' -batchmode -quit -projectPath '<真实工程完整路径>' -executeMethod MoriEchoes.EditorTools.MoriEchoesSetup.BuildWindows -logFile '<另一份新建日志完整路径>'
```

这里没有实际调用上述命令。批处理工具不能自动取得 Unity 许可、安装 Editor/模块或修复未知旧工程。必须检查进程退出码、日志、BuildReport 和真实文件，不能只看命令已经发送。

## 独立 Windows 包验收

| 检查 | 操作与通过标准 | 当前结果 |
| --- | --- | --- |
| 启动 | 在 Editor 关闭后从 EXE 启动，菜单无异常/缺字 | 未执行 |
| 操作 | 移动/鼠标瞄准/三枪/装弹/闪避正常 | 未执行 |
| 流程 | 升级、波次、Boss、胜利/失败、重开、退出均可达 | 未执行 |
| 分辨率 | 1080p、900p、720p UI 不重叠/截断 | 未执行 |
| 音画 | 特效、敌弹、预警清晰，音量有效 | 未执行 |
| 保存 | 退出再开，最高击杀和音量保留 | 未执行 |
| 性能 | 记录目标机、CPU/GPU、帧率及长帧，达到约定标准 | 未执行 |
| 日志 | Unity Player.log 无未处理异常 | 未执行 |

上表保留完整人工发布验收要求。当前自动测试覆盖启动、操作、短波 Boss、升级、胜负、重开及三档 UI 截图，具体见 `UNITY_INTEGRATION_REPORT.md`；人工默认难度通关、听感、跨机保存/字体、长时间性能仍待验收。
