# 《三角洲契约》阶段 0：环境检查与项目骨架

环境上下文日期：2026-10-23（用户提供，Asia/Shanghai）。

## 1. 本次范围与文档依据

本次只检查本地开发环境、保全现有文件、整理文档和创建缺少的目录，不开发游戏系统，不安装软件，不生成假 Unity 配置。

已完整阅读启动包中的以下 8 份文档：

- `README.md`
- `docs/GAME_DESIGN.md`
- `docs/CODEX_TASKS.md`
- `docs/ACCEPTANCE_TESTS.md`
- `docs/ASSET_PROMPTS.md`
- `prompts/CODEX_MASTER_PROMPT.md`
- `prompts/CODEX_PHASE_1_PROMPT.md`
- `prompts/CODEX_ASSET_INTEGRATION_PROMPT.md`

附件文档中的后续任务、提示词和示例仅作为设计依据，不等于本次获准执行。开发顺序以用户本轮要求为准：移动与瞄准 → 武器射击 → 敌人 AI → 经验与随机升级 → 波次与 Boss → UI → 美术素材接入 → Windows 测试构建。图片是视觉参考，不是可直接使用的分帧素材或需要照搬执行的代码。

## 2. 环境检查结果

| 检查项 | 实际结果 | 结论 |
| --- | --- | --- |
| 当前工作目录 | `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹` | 原有文件不是 Unity 工程结构。 |
| 启动 ZIP | `C:\Users\陈乐天\Downloads\MoriEchoes_Codex_Starter.zip`，仅 8 份 Markdown 文档 | 是文档启动包，不是完整 Unity 项目。 |
| 文档暂存根目录 | `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes_Codex_Starter` | 本次创建目录骨架后仍不是有效 Unity 工程。 |
| Unity 工程标志 | 未发现 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json` | 无法确认工程 Unity 版本或包依赖；不能宣称 Unity 项目已创建。 |
| Unity / Unity Hub | PATH、标准安装位置、Hub 配置目录、标准卸载注册表项未检测到；未发现运行进程 | 未确认本机安装。未穷举所有磁盘或自定义安装位置。 |
| Input System | 没有包清单和包锁文件可检查 | 包版本、是否安装及 Active Input Handling 均尚无法验证。 |
| Visual Studio | 检测到 Visual Studio Community 2022，版本 17.14.3 | 安装目录为 `C:\Program Files\Microsoft Visual Studio\2022\Community`；未确认 Unity 开发工作负载。 |
| .NET CLI | PATH 中未检测到 `dotnet` | 不能据此断言所有 .NET SDK 均不存在；未进行编译。 |
| 既有 Unity 内容 | 当前工作目录未发现 C# 脚本、场景、预制体或 `.meta` 文件 | 无既有 Unity 游戏内容需要迁移；仍保全全部原有非 Unity 文件。 |
| Git | 当前工作目录不是 Git 仓库 | 未初始化仓库、创建分支或提交。 |

## 3. 已有文件保护

以下原有文件均留在原位置，不覆盖、不移动、不重命名：

| 文件（相对当前工作目录） | 检查基线 SHA-256 |
| --- | --- |
| `__init__.py` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `index.html` | `680FACA1A5740803D4462CD043CDD08A426CC72DDB93CAFCD06E2BE32967CC4E` |
| `script.js` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `style.css` | `C8608EC7C765560A95820D6219DE1B36B1F9467CA12880147E9F29FE62BF6B48` |

同名嵌套空目录 `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\新建文件夹` 保持不动。原始 ZIP 只读使用，未修改。

文档解包使用逐文件新建方式，拒绝覆盖已存在文件；同时检查归档目标不得越出暂存根目录。启动包原始 8 份文档保持字节内容不变，本报告是另行新增文件。

## 4. 本次实际创建的骨架

以下结构位于文档暂存根目录，不代表已完成 Unity Hub 工程创建：

```text
MoriEchoes_Codex_Starter/
  README.md
  Assets/
    Art/
      Characters/
      Weapons/
      Enemies/
      Environment/
      VFX/
      UI/
    Audio/
      BGM/
      SFX/
    Prefabs/
      Player/
      Weapons/
      Projectiles/
      Enemies/
      Pickups/
      UI/
    Scenes/
      Boot/
      MainMenu/
      Battle_01/
    Scripts/
      Core/
      Player/
      Combat/
      Weapons/
      Enemies/
      Roguelite/
      UI/
      Save/
    Data/
      Weapons/
      Enemies/
      Upgrades/
      Skills/
    Settings/
  docs/
    ENVIRONMENT_CHECK.md
    （启动包原有文档）
  prompts/
    （启动包原有提示词）
```

已创建 30 个规划叶目录及其必要父目录。保留启动包原本的小写 `docs/`；Windows 默认大小写不敏感，不再另建 `Docs/`。

没有创建 `.cs`、`.unity`、`.prefab`、`.meta`、`Packages/manifest.json` 或 `ProjectSettings/ProjectVersion.txt`。没有安装 Input System、导入美术、修改 Inspector 或生成任何游戏系统。

## 5. 进入阶段 1 前的环境准备（尚未执行）

1. 在本机定位已有 Unity Hub / Unity Editor；如确实没有，再由用户确认安装。选用 Unity 6 的 Universal 2D 模板，记录实际安装的完整编辑器版本，不猜测具体补丁版本。
2. 通过 Unity Hub 新建实际工程，建议目标为 `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes`。该路径本次没有创建；不要把文档暂存骨架当成已经有效的 Unity 项目。
3. 保留 Hub 生成的 `Packages/`、`ProjectSettings/`、模板资产和全部 `.meta`。将文档复制到实际工程中，仅合并缺少的 `Assets/` 目录，不用整包覆盖模板。遇到同名文件先比较内容，再报告冲突。
4. 在 Unity 的 Package Manager 中确认来自 Unity Registry 的 `com.unity.inputsystem`，使用编辑器可用的兼容版本。再检查 `Project Settings > Player > Other Settings > Active Input Handling` 为 `Input System Package (New)`，按提示重启编辑器。菜单名称以实际 Unity 版本为准。
5. 确认 Universal 2D 模板的 URP / 2D Renderer 配置；在编辑器外部工具设置中确认 C# IDE。检查 Visual Studio 的“使用 Unity 的游戏开发”工作负载，不能只凭 IDE 存在认定环境完整。
6. 重新读取真实工程的 `ProjectVersion.txt`、`manifest.json`、包解析结果及 Console，确认没有导入或编译错误后，才开始阶段 1。

当前工程位于 OneDrive 目录下。后续若出现同步占用或导入问题，再与用户协商非同步工程位置；本次未移动任何文件。

## 6. 下一阶段：玩家移动与瞄准（仅计划）

阶段边界：仅灰盒玩家、输入、瞄准、相机和墙体碰撞。不做枪械、敌人、经验、升级、存档、闪避或主动技能，不引入第三方相机依赖。

### 6.1 拟新增文件

以下路径均基于建议的真实工程，尚未创建：

- `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes\Assets\Scripts\Player\PlayerController.cs`
- `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes\Assets\Scripts\Core\CameraFollow.cs`
- `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes\Assets\Settings\PlayerInput.inputactions`
- `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes\Assets\Prefabs\Player\Player.prefab`
- `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes\Assets\Scenes\Battle_01\Battle_01.unity`

若真实工程内已有同类文件、动作资产或场景，应先检查并复用，不覆盖重建。

### 6.2 Unity Inspector 设置计划

1. 在灰盒 Battle_01 场景建立 Player 根对象，添加 `Rigidbody2D`（Dynamic、Gravity Scale = 0、Freeze Rotation Z）和适合占位外形的 2D Collider。运动只在 `FixedUpdate` 中采用一种物理写入方式，按实际版本选择 `MovePosition` 或速度方案，不混用。
2. 另建 Visual 子对象放置占位 Sprite；鼠标瞄准只旋转视觉层，不旋转冻结 Z 的物理根对象。WASD 输入归一化，避免斜向加速。
3. 配置输入动作资产：Move 为 Value / Vector2，绑定 WASD 2D Vector Composite；Aim 为 Value / Vector2，绑定鼠标 Position。绑定输入引用和主相机引用，具体序列化字段以届时实现为准。
4. 主相机使用 Orthographic，设置 Player 为跟随目标并保留正确 Z 偏移；跟随平滑参数可在 Inspector 编辑，不在本阶段接入 Cinemachine。
5. Ground 只负责显示，不给整片可行走地面添加阻挡 Collider；边界墙添加静态 `BoxCollider2D`，确认与玩家所在物理层允许碰撞。
6. 将 Player 保存为预制体，绑定必要引用并保存场景。速度、相机尺寸和平滑度以实际灰盒测试调整，本报告不宣称任何未测试参数已经合适。

### 6.3 测试步骤与验收标准

| 测试步骤 | 验收标准 |
| --- | --- |
| 打开场景，等待脚本导入，进入 Play Mode | 无编译错误、缺失脚本或未绑定引用异常。 |
| 测试 WASD、按键释放及八方向移动 | 能开始与停止移动；相同持续时间下斜向与轴向移动速度一致。 |
| 将鼠标移至玩家四周，并改变窗口尺寸 | 视觉朝向与鼠标世界坐标一致，物理碰撞体不随视觉旋转；调整窗口后无明显坐标偏差。 |
| 持续移动并观察相机跟随 | 正交相机平滑跟随，不出现明显抖动或错误 Z 位置。 |
| 朝各侧墙体移动并沿边界滑动 | 不穿越墙体，不出现双重物理驱动导致的异常。 |
| 停止并重新进入 Play Mode | 不遗留异常运行状态；Console 无新增错误；交付仍只含阶段 1 内容。 |

以上为计划，不是测试结果。阶段 1 尚未实现，尚未实际验证。

## 7. 后续阶段路线（全部待确认后按序开发）

本表路径均相对于建议真实工程 `C:\Users\陈乐天\OneDrive\Desktop\新建文件夹\MoriEchoes`，均为拟新增或拟复用位置，不是本次产物。每个阶段交付时仍须按实际实现给出最终文件清单、精确 Inspector 字段和真实验证结果。

| 阶段 | 计划文件位置 | Inspector / 编辑器设置重点 | 测试与验收 |
| --- | --- | --- | --- |
| 2. 武器射击 | `Assets/Scripts/Weapons/WeaponController.cs`；`Assets/Scripts/Combat/Projectile.cs`、`Health.cs`；`Assets/Prefabs/Projectiles/Projectile.prefab`；`Assets/Data/Weapons/AR07.asset` | 绑定枪口、子弹预制体、伤害接收者、碰撞层；配置射速、伤害、速度和寿命。数据资产对应脚本届时一并明确。 | 首把 AR-07 按瞄准方向开火；伤害只结算一次；不自伤；子弹按寿命或范围回收；暂停不推进射击或子弹。 |
| 3. 敌人 AI | `Assets/Scripts/Enemies/EnemyAI.cs`；`Assets/Scripts/Player/PlayerHealth.cs`；`Assets/Prefabs/Enemies/Enemy.prefab` | 绑定玩家目标与生命组件；设置 2D 物理、速度、攻击范围和攻击冷却。 | 敌人追踪玩家；范围内按冷却攻击而不是每帧扣血；多敌人无异常；玩家死亡可明确识别。 |
| 4. 经验与随机升级 | `Assets/Scripts/Roguelite/ExperiencePickup.cs`、`ExperienceManager.cs`、`UpgradeData.cs`、`UpgradeManager.cs`；`Assets/Data/Upgrades/` | 配置拾取范围、升级阈值、升级候选池，以及名称、描述、稀有度、类型、数值；绑定升级面板与实际属性目标。 | 死亡经验只发放一次；累计达到阈值暂停战斗；候选充足时三选项不重复，不足时有明确降级处理；选择真正修改属性并恢复游戏。 |
| 5. 波次与 Boss | `Assets/Scripts/Core/WaveManager.cs`、`GameManager.cs`；`Assets/Scripts/Enemies/BossAI.cs`；`Assets/Prefabs/Enemies/Boss.prefab` | 配置出生点、波次节奏、敌人及 Boss 预制体、Boss 生命与攻击预警参数。 | 完成固定场地波次；Boss 至少两种有预警的攻击；胜负只结算一次；重新开始可清理全部局内状态。 |
| 6. UI | `Assets/Scripts/UI/HUDController.cs`、`PauseMenu.cs`、`ResultPanel.cs`；`Assets/Scenes/MainMenu/MainMenu.unity`；`Assets/Prefabs/UI/` | Canvas Scaler 使用 1920×1080 参考分辨率并调整锚点；绑定生命、经验、按钮、暂停和结果数据；接入 Input System 的 UI 输入。 | 主菜单可进入游戏；Esc 暂停与恢复；胜负结果、重开流程正确；16:9 与窗口缩放无关键文字遮挡或裁切。 |
| 7. 美术素材接入 | `Assets/Art/Characters/`、`Weapons/`、`Enemies/`、`Environment/`、`VFX/`、`UI/`；对应现有预制体 | 根据实际素材检查透明通道、Sprite 2D 导入方式、Pixels Per Unit 和排序层；只有尺寸与布局明确时才切图；保留来源与授权信息。 | 优先接入主角、AR-07、一种敌人和地面；视觉尺寸与方向一致，敌我可读；仅替换视觉，不破坏已验收玩法或覆盖原始素材。 |
| 8. Windows 测试构建 | `Assets/Scenes/Boot/Boot.unity`、`MainMenu/MainMenu.unity`、`Battle_01/Battle_01.unity`；拟输出 `Builds/Windows/MoriEchoes.exe` | 在实际 Unity 版本的 Windows Build Profile 中设置架构、场景清单与启动场景；核对输入、URP 和发布配置。构建目录本次不创建。 | Console 干净并成功构建；实际启动 Windows 程序，验证菜单、战斗、升级、Boss、胜负、重开和退出；无缺材质或必要资源。 |

## 8. 验证边界与停止点

本次只进行文件系统、文档、配置存在性、安装迹象和目录结构的静态检查。交付前复核原有 4 文件哈希、8 份解包文档与归档字节一致性、30 个叶目录存在性，并检查未产生游戏实现文件或伪 Unity 配置。

没有启动 Unity Hub 或 Unity Editor，没有解析 Unity 包，没有进行 C# 编译、Play Mode、Edit Mode / Play Mode 自动化测试或 Windows 构建；也没有操作 Unity Inspector。因此 Unity 运行与编译层面均为：尚未实际验证。

阶段 0 到此停止。等待用户确认真实 Unity 工程和环境准备方式后，再进行“玩家移动与瞄准”，不自动开始下一阶段。
