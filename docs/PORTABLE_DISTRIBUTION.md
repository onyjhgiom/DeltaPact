# 2026-10-26 最新 Windows 便携分享包

## 当前交付

- 桌面分享文件：`C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes_最新版_Windows64_20261026_052000.zip`。
- 工程内分享文件：`C:\UnityProjects\MoriEchoes\Releases\MoriEchoes_20261026_052000\MoriEchoes_最新版_Windows64_20261026_052000.zip`。
- 实际构建：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261026_051615_432\MoriEchoes.exe`。
- ZIP 大小 149,775,164 字节；解压 430,888,042 字节；194 个归档文件，其中 188 个运行文件。
- SHA256：`8c0e2d52a91a56e155bfe572026668522b26d584d548278748bc567a8678e375`；同目录有 `.sha256.txt` 和 `.package.json`。
- 内容元数据已更新为 12 名角色、9 把武器、8 关、8 个 Boss；含新增 `废墟墓窟`、`沉没圣堂`、`星坠核心井`。
- ZIP CRC、解压逐文件 SHA256 和解压副本实际运行均通过；三关 53 项垂直推进检查通过，报告为 `TestResults/PortableVertical_20261026_052200/vertical-campaign-smoke.json`。

把整个 ZIP 发给对方；对方右键“全部提取”，打开解压后的 `MoriEchoes` 文件夹，双击 `START_GAME.cmd`。无需安装 Unity、Unity Hub、Python、账号或 API 密钥。当前仍是 Windows x64 Development 试玩构建，未在其他电脑、实体手柄或真人难度下验收。最终构建和 ZIP 解压副本各通过 53 项三关运行检查、0 错误；详情见 `docs/VERTICAL_CAMPAIGN.md` 和 `docs/PROGRESS.md`。

## 历史记录

# 2026-10-25 历史 Windows 便携分享包

## 交付文件与启动

- 桌面分享文件：`C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes_最新版_Windows64_20261025_112559.zip`。
- 工程内原包：`C:\UnityProjects\MoriEchoes\Releases\MoriEchoes_20261025_112559\MoriEchoes_最新版_Windows64_20261025_112559.zip`。
- 实际构建：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261025_112559_527\MoriEchoes.exe`。
- 149,697,668 字节，约149.7 MB / 142.76 MiB；解压430,697,335字节，194个文件，其中188个运行文件。包含完整数据、Mono运行时、UnityPlayer、图形库、中文字体、启动器和中文说明。
- SHA256：`02c81d87dd4cac0f1eb700fb7081ae97fd9f4366c9bd9a2982cea36c1d08a05d`。原包同目录有 `.sha256.txt` 和 `.package.json`；包内有 `version.json` 和 `manifest.sha256`。

将整个ZIP发给对方；对方右键“全部提取”，打开解压后的 `MoriEchoes` 文件夹，双击 `START_GAME.cmd`。也可直接运行 `MoriEchoes.exe`。默认1280×720窗口、D3D11；F11/Alt+Enter切全屏。无需安装Unity/Hub/Python，无需账号或API密钥。完整操作在包内 `请先阅读_启动与操作.txt`。保留DLL及相邻目录，不能只发EXE。

目标为Windows x64，建议Windows 10/11及支持D3D11的显卡；没有在其他电脑实测，不保证所有硬件配置。当前是Development试玩构建，保留开发版本角标，正式发布模式与跨机性能验收仍属后续工作。游戏内现有标题为《三角洲契约》，工程名MoriEchoes。

## 本轮实际改动

保留四角色独立技能、六种武器、五关三阶段Boss、20种分关小兵、稳定的Boss站姿与主题特效。仅补齐分发与字体，不更改战斗数值、已有场景或动画素材。

| 文件 | 用途 |
| --- | --- |
| `Assets/Fonts/NotoSansCJKsc-Regular.otf`、`.meta`、`Assets/Fonts.meta` | 原版静态中文字体，动态字图且嵌入字体数据 |
| `Assets/Fonts/OFL.txt`、`SOURCE.md`及meta | 上游许可、来源；分发副本位于包内ThirdPartyNotices |
| `Assets/Editor/PortableFontSetup.cs`、`.meta` | 同步导入、嵌入配置、代表字符检查、保护自定义字体引用、构建入口 |
| `Assets/Settings/MoriEchoesConfig.asset` | Ui Font从空引用绑定到随包字体 |
| `Assets/Editor/MoriEchoesSetup.cs` | 构建报告正确记录已绑定字体 |
| `Tools/Package-WindowsGame.py` | 完整运行文件打包、x64/构建报告检查、CRC、解压后逐文件SHA256验证 |
| `Tools/Distribution/START_GAME.cmd` | 相对路径启动，支持中文及空格路径、不同工作目录 |
| `Tools/Distribution/请先阅读_启动与操作.txt` | 启动、操作、技能、故障日志说明 |
| `PLAY_MoriEchoes.cmd`、`HOW_TO_PLAY.md`、`README.md` | 指向本次最新实际构建和分享包 |
| `docs/PROGRESS.md`、本文件 | 记录实际构建、包体与测试结果 |
| `docs/BuildReports/Windows_20261025_112559_527.md` | Unity实际构建结果 |

只排除118个调试符号及Unity标为 `BackUpThisFolder_ButDontShipItWithYourGame` 的备份文件，不删改构建原目录。源工程、Library、测试输出、个人存档与开发凭据没有装入ZIP。包内运行文件与来源逐文件校验一致。

字体来自[Noto官方仓库](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf)，完整[OFL许可](https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/LICENSE)随包保留；未修改字体本体。字体SHA256：`2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b`。

本轮修改前备份：`Validation/PortablePackageBackup_20261025_112119_298`，含原Config、构建工具、根启动器和文档；旧时间戳构建全部保留。

## Unity Inspector 与重建

1. 用Unity `6000.6.7f1` 打开 `C:\UnityProjects\MoriEchoes`。
2. 选择 `Assets/Fonts/NotoSansCJKsc-Regular.otf`：Character为Dynamic，Include Font Data启用，Font Names为Noto Sans CJK SC，Fallback Font References为空。
3. 选择 `Assets/Settings/MoriEchoesConfig.asset`，Ui Font应引用该字体。原场景中的GameBootstrap继续引用此Config，不需重建场景。
4. 菜单 `Tools > Mori Echoes > Build Portable Windows With Embedded Font`；或批处理执行 `MoriEchoes.EditorTools.PortableFontSetup.BuildWindows`。若已有不同自定义字体引用，工具保留并报错。
5. 调用 `Tools/Package-WindowsGame.py --build <新构建目录> --output <全新ZIP路径> --extract-check <全新解压目录>`。需要Python 3.9+，使用内置标准库；工具不覆盖已有输出。未来新包需重跑验收，不能沿用本包通过记录。

## 实际测试与验收

构建由Unity6000.6.7f1实际完成，Windows x64/Mono/Development，0错误、0警告。报告 `docs/BuildReports/Windows_20261025_112559_527.md`；日志 `Logs/Portable_Font_Build_20261025.log`，批处理最终退出码0。字体Include Font Data=True及代表字符检查通过。

从ZIP解压到工程外的中文和空格目录，再运行解压副本：

`C:\Users\陈乐天\AppData\Local\Temp\MoriEchoes_Portable_20261025_1130\中文 空格路径测试\MoriEchoes\MoriEchoes.exe`

| 实测项 | 结果 | 证据 |
| --- | --- | --- |
| ZIP CRC与逐文件SHA256 | 194个归档文件CRC通过，193个有效载荷SHA256通过，manifest本身也通过CRC；188个运行文件无遗漏 | 原包同目录`.package.json`及包内manifest |
| 四角色/技能、六武器、视角及窗口 | 312项通过，14张非空截图，0运行错误/退出码0 | `TestResults/Arsenal_1280x720_20261025_113206_614/stage-smoke.json` |
| 五关Boss、20种小兵、技能区与清理 | 164项通过，35张非空截图，0运行错误/退出码0；全部截图1280×720 | `TestResults/Encounter_1280x720_20261025_113455_632/stage-smoke.json` |
| 五关战役、升级、三阶段Boss、胜负与重试 | 124项通过，29张截图均1280×720，0运行错误/退出码0 | `TestResults/Side_1280x720_20261025_113826_594/stage-smoke.json` |
| START_GAME.cmd相对路径启动 | 从C:\Windows工作目录启动工程外副本；出现响应正常的DeltaPact窗口，正常关闭，退出码0，日志无匹配的运行异常 | `TestResults/PortableLauncher_20261025_1145/launcher-check.json`及Player.log |
| 中文菜单 | 实际查看解压副本截图，菜单/技能/武器中文正常 | Arsenal报告目录`arsenal_final_menu.png` |

600项游戏自动检查通过，测试使用合成输入及无敌/传送/伤害辅助，且测试进度仅在内存中。角色专项主动覆盖1024×768、1600×720、1600×900及1280×720，不将其称为固定分辨率性能测试。

首轮启动脚本已打开正常窗口并退出，但测试器未提前持有进程句柄，退出码读取为null，故严格判定未通过。原记录保留在 `TestResults/PortableLauncher_20261025_1142`；修正测试进程句柄获取后，同一ZIP及启动脚本复测通过，没有改动游戏或归档。

验收标准：完整解压校验一致；不依赖工程绝对路径启动；菜单可选择角色并进入战斗；移动/射击/技能/换枪/升级/五关Boss和结算回归通过；中文字体随包；包内有操作说明与完整运行依赖。上述项目本机已验证。操作系统为Windows11家庭版中文版64位（10.0.26200）；未进行真实跨电脑测试、实体手柄长测或真人难度验收，不能标记这些项目通过。

重现：先生成新的解压目录；对其EXE执行 `Tools/Test-AnimeArt.ps1 -Executable <解压EXE> -Width 1280 -Mode Arsenal`，随后Mode Encounter，再执行 `Tools/Test-SideScroller.ps1 -Executable <解压EXE> -Widths @(1280)`。不要并行运行多个可见测试窗口。启动脚本检查需从其他工作目录调用，并只关闭自己创建的测试进程。
