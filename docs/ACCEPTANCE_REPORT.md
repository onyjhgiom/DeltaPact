# 本轮真实验收报告

## 越肩优化最终结果：2026-10-24

真实 EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261024_101932_609\MoriEchoes.exe`。Unity 6000.6.7f1 构建 Succeeded，0 错误/0 警告；1280×720、1600×900、1920×1080 各 94 项检查通过、0 运行错误。覆盖新视角/开镜/后坐力/瞄准/边界及原玩法短波闭环。验收清单、实际证据与未验证项见 `CAMERA_IMPLEMENTATION_AND_TESTS.md`；不等同正常十波人工通关或最终美术验收。以下为前一版验收历史。

## 最新结果：2026-10-24

真实工程：`C:\UnityProjects\MoriEchoes`。Unity 6000.6.7f1 真实编译及 Windows x64 Development 构建成功，0 错误 / 0 警告。EXE：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261023_235738_697\MoriEchoes.exe`。

1280×720、1600×900、1920×1080 各通过 51 项可见窗口自动检查，保留 Player.log、JSON 和截图。测试使用临时短波配置及合成输入，不代表默认十波正常难度通关。未进行 Editor Play Mode、真人手柄、听感、长时性能、跨机或正式美术验收。

完整证据、修复历史、Inspector 和复现见 `UNITY_INTEGRATION_REPORT.md`，其结果优先于以下首次交付历史。原 `DEFINITION_OF_DONE.md` 保持原件，不替未执行项目打勾。

## 首次源码交付历史（当时尚无 Unity）

日期：2026-10-23。当时工作目录：

```text
C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes_Codex_Starter
```

## 首次交付结论（历史）

**交付了待导入的灰盒闭环源码、编辑器安全搭建/构建工具、测试与操作文档；没有交付已验证可玩的 Unity 工程或 Windows 游戏包。**

原始源码目录没有 Packages/ProjectSettings。首次检查未找到 Editor；之后用户提供实际安装路径，已完成顶部记载的集成工作。以下首次检查记录不代表当前环境。

## 实际执行并通过的检查

| 检查 | 实际结果 | 能说明什么 / 不能说明什么 |
| --- | --- | --- |
| 独立 C# 模型编译并运行 | 10,040 个断言通过；固定种子 5,000 次三选一抽样 | 九种升级、等级队列、基础波次规则；不涉及 Unity 物理/输入/渲染 |
| C# 9 语法检查 | 23 个 Unity 源文件 × 3 组宏，0 语法错误 | 解析成功；不是 Unity API、引用、类型或程序集编译通过 |
| PowerShell 语法 | 3 个脚本，0 解析错误 | 不等于真实 Unity 导入成功 |
| 安全导入回归 | 13 项通过，临时合成夹具 | 验证 dry-run、文档保护、Assets 冲突中止、幂等与配置不改；没有启动 Unity |
| 原始文件保护 | Starter ZIP 的 8 个原文件和 Handoff ZIP 的 9 个文件逐项 SHA256 一致 | 未重写原始需求文档、提示词或 README |
| 父目录文件保护 | HTML/CSS/JS/Python 4 个文件 SHA256 与本轮修改前一致 | 无关项目未动 |
| 实际游戏 EXE 检查 | 未发现 `MoriEchoes.exe` | 确认本轮没有生成游戏包；校验 EXE 不是游戏 |

语法宏组合为 `UNITY_EDITOR + ENABLE_INPUT_SYSTEM`、仅 `ENABLE_INPUT_SYSTEM`、两者均不定义。三组均不替代具体 Editor 对包和平台的编译。

复现：在本源码根目录的 PowerShell 执行：

```powershell
.\Tools\Validate-Source.ps1
```

入口会编译无 Unity 依赖的 `RunModel.cs + Tools/Tests/RunModelTests.cs`，运行 Roslyn 语法检查、PowerShell 解析及安全导入回归，并生成 `Validation/source-validation.json`，包含时间、编译器路径、结果和 23 个源码 SHA256。首次测试运行中 Roslyn 依赖重定向缺失的问题已修复：校验脚本复制编译器依赖与 `csc.exe.config` 后复跑通过。

安全测试在系统临时目录创建带伪包版本的**合成文件操作夹具**，结束后仅删除该唯一临时目录。它不是可打开的 Unity 工程，不会将伪配置复制到生产目录。

## 源码已覆盖，运行仍待验收

| 需求组 | 已写入实现 | 实际 Unity 状态 |
| --- | --- | --- |
| 玩家 | 归一化移动、鼠标/摇杆瞄准、短暂闪避、生命/死亡 | 未编译/运行 |
| 武器 | AR/SG/SR、弹药、装填、散布、暴击、穿透、扫掠碰撞 | 未编译/运行 |
| 敌人 | 追击、远程、突袭、精英、出生预警、两种 Boss 攻击 | 未编译/运行 |
| 肉鸽 | 经验吸附、溢出等级、三选一、九种实际属性效果 | 模型通过；Unity 应用未测 |
| 流程 | 计时波次、Boss、主菜单、暂停、升级、胜负、重开 | 未走通 |
| UI | HUD、Boss 血条、弹药/装填、升级卡、结算、音量 | 未看图/交互 |
| 音画 | 程序化角色/竞技场、枪火、命中、残影、预警、合成音 | 未渲染/试听 |
| 资源管理 | 有界弹丸/拾取/特效池，敌人数上限，重开清理 | 未做 Profiler |
| 正式美术 | Sprite 槽位、规范与接入步骤 | 未导入；动画/高级灯光未完成 |
| Windows | 安全构建入口和报告写入 | 未调用 BuildPipeline；无产物 |

## 原完成定义逐项状态

保留 `DEFINITION_OF_DONE.md` 原文件，不替未执行项目打勾。其“可玩闭环”十项、“工程质量”八项和“发布前”四项中，所有依赖 Unity 或独立包的条目目前均为**未执行**。

- 子弹生命周期、暂停门控、移动归一化和一次性死亡在源码中存在，不因静态阅读就标记运行通过。
- 尚无 Missing Script/Reference、NullReference、碰撞/层配置的真实测试结果。
- 升级测试只证明纯数据规则，不能证明卡片按钮、Time.timeScale、UI 输入和实际武器属性应用没有集成问题。
- 没有 FPS、加载速度、十分钟稳定性、包体积、截图、音频听感或实际对局时长数据。
- 正式素材和字体许可尚未确认；不可作为发行版发布。

## 已知限制与待办

1. 提供真实工程目录和 Unity.exe 路径，或通过 Hub 创建新的 Universal 2D 工程；不要把本源码目录误认为已完成初始化。
2. 完成导入与首次 Unity 编译；若出现 API/包兼容错误，保留 Console 完整错误并按根因修复。Unity 的具体 6000.x 补丁、Input/URP/UGUI 版本尚未记录。
3. 运行安全搭建菜单生成资源；当前没有 `.unity`、Prefab、`.asset` 或 `.meta` 伪造文件。生成器仅在路径空闲时创建，不覆盖旧场景。
4. 按 `INSPECTOR_AND_TESTS.md` 完成 A–L，先短流程冒烟，再正式十波全流程和性能测试。
5. 交付美术是灰盒，不是精细动漫成品；正式动画、Tilemap/精细环境、分层灯光和后处理待接入。
6. UI 使用内建 UGUI Text；中文依赖显式字体或操作系统回退，目标机缺字风险待检查。高频 HUD 字符串和敌人生成/销毁需在 Profiler 中评估，不能宣称零 GC 或稳定 60FPS。
7. 本轮只有同场景状态机，没有独立存档界面、关卡编辑器、设置中心或异步加载系统；这些不影响当前源码目标，但不应当被宣传为已实现。

## 运行验证记录要求

下一次真实验证必须追加：时间、Unity 完整版本、包版本、操作系统/CPU/GPU、分辨率、测试步骤、实际结果、Console/Player.log、截图、构建绝对路径。失败必须保留原因；没有运行证据继续标“未执行”，不改写本轮历史结果。
