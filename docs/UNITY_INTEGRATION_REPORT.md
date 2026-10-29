# Unity 工程集成与 Windows 验收记录

日期：2026-10-23 至 2026-10-24（Asia/Shanghai）。这是提供真实 Editor 路径后的继续开发结果；早期“尚无 Unity / 未构建”记录是历史状态。

## 当前可启动的交付

- 工程：`C:\UnityProjects\MoriEchoes`。
- 实际 Editor：`C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`，版本 `6000.6.7f1 (b22767f04f04)`。保留用户已安装版本，未切换到此前建议的 6.3。
- 已生成并测试的游戏：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261023_235738_697\MoriEchoes.exe`。
- 完整包目录：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261023_235738_697`。分发时保留整个目录，不能只复制 EXE。
- 构建：Windows x86_64 / Mono / Development；BuildPipeline Succeeded，0 错误、0 警告，耗时 13.074 秒，报告包体 173,998,333 字节。
- 原始构建报告：`docs/BuildReports/Windows_20261023_235738_697.md`。
- 启动：双击上述 EXE 或工程根目录 `PLAY_MoriEchoes.cmd`。正常启动不传 `-moriSmoke`，使用十波、每波 55 秒的默认配置。

## 环境准备与修复

接续检查时目标目录已经存在，包含有效的 Unity 版本/包清单，但 Assets 没有代码或场景，包清单没有 Input System、URP、UGUI。因此保留空工程，安全导入 52 份源码/文档，并补齐必要包与配置，没有再次创建或升级工程。

| 项目 | 实际状态 |
| --- | --- |
| Input System | 1.20.0，本地官方包归档 |
| Universal RP | 17.6.0，由此 Editor 随附模板版本确定 |
| Unity UI | 2.6.0 |
| 2D Sprite | 1.0.0 |
| Active Input Handling | Input System Package (New)，序列化值 1 |
| 渲染 | 原生 URP 2D Renderer 与 Pipeline，通过 Unity API 创建并分配 |
| Windows 模块 | 当前 Editor 可实际构建 Windows x64，非推测安装成功 |

第一次 Input System 下载因 `cdn.packages.unity.com` 解析失败。改用 Unity 官方国内源给出的 `download-packages.unity.cn` 同版归档，校验注册表 SHA1 后以本地 tarball 引用。没有修改系统 DNS、代理或全局 Unity Registry。

- 归档：`Packages/com.unity.inputsystem-1.20.0.tgz`，请随工程保留。
- SHA1：`7a4e1a2a81941121ffc0721a14009b46299dada1`。
- 来源与校验：`Logs/InputSystemPackageSource.json`。

实际 Unity 编译发现 6000.6 已禁用 `Object.GetInstanceID()`，弹丸去重改为保存目标 Health 引用；移除 `FindFirstObjectByType` 弃用用法。首次场景搭建遇到未命名场景限制，现先将其保存到唯一的 SetupRecovery 路径再增量创建入口场景。诊断使用运行时 `Debug.isDebugBuild` 门禁，消除 DEVELOPMENT_BUILD 的新版警告。

原配置备份位于源码工作区：`C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes_Codex_Starter\Validation\IntegrationBackup_20261023_232418`。已有场景/代码未被覆盖；目标原来没有这些文件。

## 实际运行结果

在独立 EXE 的可见窗口、Direct3D 11、Intel Arc Graphics 上顺序运行：

| 分辨率 | 检查 | 运行错误 | 进程退出 | 结果 |
| --- | --- | --- | --- | --- |
| 1280 × 720 | 51 | 0 | 0 | 通过 |
| 1600 × 900 | 51 | 0 | 0 | 通过 |
| 1920 × 1080 | 51 | 0 | 0 | 通过 |

具体覆盖：Boot/中文菜单、鼠标点击 Start 经 UGUI 输入模块进入战斗、WASD 与斜向归一化、Space 无敌窗口及结束、1/2/3 换枪、鼠标瞄准射击与碰撞伤害、三枪装弹、击杀与经验拾取、三选一暂停/应用/连续升级、Esc 暂停冻结与恢复、Q 脉冲、追击敌人与远程敌弹、弹丸回收、失败、重开重置、短波次生成 Boss、Boss 活动、胜利、返回菜单、默认配置未被测试修改。

**范围必须区分**：这是合成输入与程序化操作驱动的集成测试。测试临时克隆 Config，改成一波 5 秒，加速覆盖 Boss；Boss 阶段给玩家测试用无敌，并通过伤害 API 触发结算。除 Start 的真实输入模块点击外，其余 UI 通过按钮回调调用。不能据此宣称默认十波难度已通关、所有 Boss 攻击招式都逐一验收、或真实玩家枪感已经调好。

每个最终结果目录保存 `runtime-smoke.json`、Player.log 及六张真实截图（菜单、三选一、战斗、失败、Boss 阶段、胜利）：

```text
TestResults/Visible_1280x720_20261023_235852_239
TestResults/Visible_1600x900_20261023_235927_918
TestResults/Visible_1920x1080_20261024_000002_956
TestResults/latest-windows-smoke-summary.json
```

已查看实际菜单、战斗、升级、Boss HUD、胜利截图，未见这些已查看布局的重叠/截断；不能把抽查截图扩大为所有画面的完整视觉验收。

早期隐藏/批处理窗口的截图是黑屏，不作图像通过证据。可见窗口测试还曾因残留敌弹在死亡用例前触发受击保护而失败；修正测试为等待 3.3 秒游戏时间、确认弹丸全部回收后再检查结算，最终三轮均通过。早期失败日志保留，没有删除或改写为通过。

## Inspector 与复现

1. 使用上面的实际 Unity.exe 打开 `C:\UnityProjects\MoriEchoes`，等待导入结束。
2. `Assets/Settings/MoriEchoesConfig.asset`：Player Health 100、Move Speed 5、Waves Before Boss 10、Wave Seconds 55；Sprite Material 引用 MoriEchoesSpriteUnlit。不要把冒烟测试的短波参数保存到正式资产。
3. `Project Settings > Graphics` 的默认管线应引用 `Assets/Settings/MoriEchoesUniversal2D.asset`；Renderer 为 `Assets/Settings/MoriEchoesRenderer2D.asset`。
4. `Assets/Scenes/MainMenu/MainMenu.unity` 中唯一的 MoriEchoesBootstrap 引用 Config，Start In Menu 勾选；Battle_01 不勾选。运行时自动建立角色、相机、UI 和池。
5. `Tools > Mori Echoes > Verify Scene Bindings` 可重查绑定。点击 Play 手动检查，或直接运行已生成 EXE。
6. 修改源码后，先完成 Unity 编译，再用 `Tools > Mori Echoes > 4. Build Windows x64 Prototype` 生成新的时间戳包；不要把旧包测试结果算到新包。

独立可见窗口回归（会自动操作并退出游戏）：

```powershell
cd C:\UnityProjects\MoriEchoes
.\Tools\Test-WindowsBuild.ps1 -Executable 'C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261023_235738_697\MoriEchoes.exe'
```

不要加 `-batchmode` 或隐藏窗口来进行画面验收。诊断只在 Editor/Development 条件和显式 `-moriSmoke` 参数下启用；普通启动不改 Config、不自动操作、不设置测试无敌。

## 验收结论与后续

**灰盒工程已完成真实 Unity 编译、Windows 构建及三档分辨率自动闭环测试，可交付本机试玩。** 这次没有执行 Unity Editor Play Mode，运行验证来自独立 Windows Player。独立纯模型 10,040 条断言与 5,000 次抽样、安全导入 13 项也已通过；这些与真实 Unity 编译分别记录。

尚未通过：默认十波正常难度的完整人工通关、真人手柄、音效听感、长时间 Profiler/FPS、其他电脑与字体兼容、正式动漫角色/动画、精细场景与高级光照后处理。当前保持程序化占位美术，不能按参考图的最终画质验收。下一阶段应先人工调校默认难度，再按 `ART_INTEGRATION.md` 替换正式素材并重新构建/实测。
