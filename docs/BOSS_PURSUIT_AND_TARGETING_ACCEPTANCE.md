# Boss 越障追击与动态落点验收

2026-10-29；工程 `C:\UnityProjects\MoriEchoes`，Unity 6000.6.7f1。此文档更新上一轮“预警开始即固定”的行为说明，历史报告仍描述当时版本。

## 实际行为

十关主 Boss 现在都会在收招、等待下次出招时主动调整位置，常规接近速度随形态为 6.5 / 7.2 / 7.9 米每秒，速度随剩余距离平滑减小。横版、竖井、海面和深水采用各自的高度/战场边界；竖井起手归零追击速度，避免惯性漂出攻击位置。变相、处决及攻击过程中不抢占原有动作。

Boss 的实际胶囊碰撞体对关内墙、平台、可破坏掩体及其他非玩家碰撞体逐对忽略物理阻挡，每 0.5 秒补登记新出现的障碍。保持胶囊启用和非 Trigger，因此玩家子弹的显式碰撞查询仍可击中 Boss。玩家、其他敌人和子弹的原有墙体规则没有修改，也没有全局关闭碰撞层。

当 Boss 的目标站位与自己相距超过 7.5 米、并处于可追击状态时，触发高速接近：先 0.45 秒显形预兆与落点提示，然后约 0.34 秒带本关素材尾迹飞掠。目的地在起手时固定，位于玩家侧前方约 3.8 米（竖井约 3.25 米）；不会直接叠在玩家身上。飞掠期间暂时忽略双方身体碰撞，结束或取消时恢复；不靠穿身碰撞追加必中伤害。完成后冷却随形态约 7.85 / 7.2 / 6.55 秒，只有再次拉大距离才会触发。

前五关、竖井、水关的原有目标打击在蓄力期间跟随玩家位置，分别提前约 0.8 / 0.85 / 0.85 秒固定落点。前五关目标不再被最终战场的水平范围强行截断，玩家站在场边或旧平台上也会被重新瞄准。

30 招新增特色技能现在整体追踪蓄力目标：危险体位置、移动路径的每个点、红色预警路线同步平移；首个攻击前约 0.8 秒整套技能同时锁定，随后分段攻击按已承诺路线落下。不会在激活后突然挪动判定。保留每关三招的原有特色组合及生成美术，每两次原有攻击后穿插一次。

每招增加一个使用该招专属命中插画的定点落击，直径 2.5 米、基础伤害 17 / 20 / 23、持续 0.42 秒；它和主体组合共享追踪与最终锁定。因此不会出现玩家站在同一个安全空隙就能避过整个 Boss 技能组的情况。伤害仍受既有玩家承伤上限、护盾/减伤和受击保护约束；不是直接强制命中或满血秒杀。锁定后移动、跳跃和闪避仍可逃开，不新增冻结或强吸附。

## 具体文件

| 用途 | 文件 |
|---|---|
| 接近、越障、飞掠、边界、身体碰撞恢复 | `Assets/Scripts/Enemies/BossPursuitController.cs` |
| 十关 Boss 调度 | `Assets/Scripts/Enemies/BossController.cs` |
| 预警移动时同步移动路径/红色路线 | `Assets/Scripts/Enemies/BossField.cs` |
| 30 招追踪、统一承诺时点、直接落击 | `Assets/Scripts/Enemies/BossTechniqueController.cs` |
| 原有三类 Boss 的目标窗口、竖井起手停步 | `Assets/Scripts/Enemies/SideBossController.cs`、`VerticalBossController.cs`、`SwimBossController.cs` |
| 十关越障、暂停、实伤、合成键盘闪避 | `Assets/Scripts/Diagnostics/BossPursuitSmokeTest.cs`、`Tools/Test-BossPursuit.ps1` |
| 全 30 招实时预警、路径、实伤与回收检查 | `Assets/Scripts/Diagnostics/BossTechniqueSmokeTest.cs` |
| 水关逃离新版固定落点窗口 | `Assets/Scripts/Diagnostics/SwimCampaignSmokeTest.cs` |

本轮复用已绑定的 image 生成攻击素材，无新生图请求。8 名角色、9 把武器、10 关配置和美术贴图未重置。备份位于 `Validation/BossPursuit_20261029_092628_002`，含修改前文件及哈希。

## Unity Inspector 设置

1. 打开 `Assets/Scenes/MainMenu/MainMenu.unity`，启动组件仍绑定 `Assets/Settings/MoriEchoesConfig.asset`，不需要重建场景或重跑美术导入器。
2. 进入 Play，选择任一关并抵达 Boss。运行层级中选中当前 Boss，应有 `BossController`、`BossTechniqueController`、`BossPursuitController`，并按关卡有 Side/Vertical/Swim 三者之一。
3. 检查 Boss 的 **CapsuleCollider2D 已启用、Is Trigger 关闭**。历史 CircleCollider2D 由横版运行组件禁用，不要把它当实际 Boss 外壳。Rigidbody2D 的 Gravity Scale 为 0，仅冻结旋转，追击期间允许调整高度。
4. 追击和技能参数由运行组件计算；组件无待手工拖入的贴图/目标引用。保留 Theme 中原有 Boss Forms、Boss Ornaments、Boss Techniques 和生成的贴图绑定。
5. 运行中观察 `Pursuit.Closing / Flying / Destination / BurstCount / IgnoredTerrainCount`，以及 `Techniques.TargetStrike / TargetCommitted`；这些是只读运行状态，默认 Inspector 不一定直接显示自动属性，可通过调试器或测试报告查看，无须人为填写。

## 手动测试与验收标准

1. 先站到 Boss 与自己之间隔着实体墙/高低平台的位置，或在两侧往返。Boss 应向当前侧靠拢，不持续顶墙发呆；玩家子弹仍能减少 Boss 生命。
2. 拉开距离，观察“高速追击”、落点提示和飞掠尾迹。Boss 应先蓄力再快速来到附近，停在身体之外，不在玩家身上直接生成无预兆碰撞伤害。暂停期间计时/位置停止，恢复后继续。
3. 在蓄力红区出现时换位置，红区及行进路线应跟着调整；最后约 0.8 秒停止跟随。锁定后跑出红区、跳跃或方向+Shift，可躲开落击。
4. 保持原地不动，每个原有攻击组以及每招新增技能都应对当前位置形成真实落击，血条、受击闪光与扣血反馈出现；不能只靠固定站位免疫整套攻击。技能组合仍有各关不同的形状/节奏。
5. 三相转换、处决、主 Boss 零血量通关保持正常；追击不能提前触发胜利，Boss 死亡/重试后危险体与追击状态清除。

新测试采用实际 Windows Player。场地与 Boss/玩家位置、血量、飞掠冷却及阶段覆盖部分由测试设置；目标逃离使用合成键盘输入；30 招实伤确认首次扣血后恢复测试保护，避免测试角色硬吃全套技能导致后续样本丢失。它们不是真人正常难度通关、实体手柄长测或人工平衡认证。

## 构建与实际结果

最终程序：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261029_110837_855\MoriEchoes.exe`。实际 Unity 构建成功，Windows x64 Development，0 错误、0 警告，22.796 秒。构建记录：`docs/BuildReports/Windows_20261029_110837_855.md`；日志：`Logs/BossPursuitBuildRelease_20261029.log`。

最新运行结果及完整文件哈希见 `Validation/BossPursuit_20261029_Delivery/delivery-verification.json`。构建报告中的“运行未测试”是构建时自动生成的说明，随后 Player 测试结果以本验收及 JSON 为准。

复测命令：

```powershell
$game='C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261029_110837_855\MoriEchoes.exe'
& 'C:\UnityProjects\MoriEchoes\Tools\Test-BossPursuit.ps1' -Executable $game
& 'C:\UnityProjects\MoriEchoes\Tools\Test-BossTechniques.ps1' -Executable $game
& 'C:\UnityProjects\MoriEchoes\Tools\Test-SwimCampaign.ps1' -Executable $game
& 'C:\UnityProjects\MoriEchoes\Tools\Test-AnimeArt.ps1' -Mode Encounter -Executable $game
& 'C:\UnityProjects\MoriEchoes\Tools\Test-VerticalCampaign.ps1' -Executable $game
```

历史失败没有冒充通过：初版越障选错已禁用 CircleCollider2D，改为实际 CapsuleCollider2D 后十关越障专项通过；全招实伤回归因测试角色硬吃组合攻击而中断，确认首次扣血后恢复测试保护并避免失焦暂停影响计时后重测。交付只以最终 EXE 的完整结果为准。未新增压缩包，跨机测试仍按用户要求取消。
