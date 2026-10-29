# 《末日回响》垂直推进三关说明

本批新增第 6～8 关。它们保留 2D 横版操作，但把主推进轴改为向下：玩家从竖井顶部进入，击破每个房间底部的主题闸门，再落入下一层。三关使用运行时程序化场景、敌人外形、首领形态和攻击特效，不覆盖原有五关和既有资源。

## 关卡总结构

- 脚本入口：`Assets/Scripts/Level/VerticalStageDirector.cs`。
- 每关 8 个房间，每房间 14 世界单位，总深度 112；出生点为 `(0, 7.5)`，最终首领竞技场中心约为 `(0, -104.8)`。
- 每个非首领房间都有一扇宽 14.7、高 1.55 的可破坏实体闸门，共 7 扇；闸门两侧有固定墙柱，破坏后关闭碰撞体并生成碎裂、能量和检查点反馈。
- 每个房间有左右交错的上层平台，位置约为 `x=±4.9 / ±4.2`，并配置两条主题机关通道；竖井外墙位于 `x=±8.25`，阻止角色横向越界。
- 每关共 14 个机关发射器。机关拥有预警图形、激活窗口、伤害/位移反馈，且不会使用透明墙代替实体关卡结构。

## 第 6 关：废墟墓窟

- 主题语言：骨井回廊、交错四骸闸、地底骷髅军团。
- 闸门：填充式骷髅头障碍。每扇门由多个 `InlaidFillSkull_` 图腾组成，必须用火力清空；击破时播放骨片爆裂、灵火和冲击波。
- 小兵：墓土骸兵、哭墙墓射手、骨翅巡猎者、填墓巨骸。敌人从闸门下沿和地底裂缝上涌，分别提供近身、向上射击、俯冲和重装压迫。
- 首领：百骸王。三阶段攻击为“上方石棺列阵后砸落”“双侧灵柩交错封道”“骸骨脉冲由裂缝向上喷发”，每个阶段更换王冠、骨面和灵火构图。
- 难度重点：第一关先教玩家读地底预警和填充骷髅闸，后半段缩短刷怪间隔并增加重装敌人，要求跳台、横移与击破闸门同时处理。

## 第 7 关：沉没圣堂

- 主题语言：双侧潮汐井、水压喷流、沉钟墓道。
- 闸门：潮汐圣歌封印，使用环形圣堂符文和水压色带；破坏后触发逆流水幕和蓝白碎光。
- 小兵：溺亡守卫、圣堂潮咏者、深渊鳗行者、沉钟海兽。潮咏者制造上升水柱，鳗行者沿侧墙切入，海兽使用大体积横向压迫。
- 首领：潮汐吞噬者。三阶段攻击为“巨钟回潮横向扫荡”“潮眼向下虹吸后爆发气泡柱”“高压海流与低位回浪交叉”，不复用墓窟的落棺、骨脉和闸门攻击。
- 难度重点：水柱提供垂直位移，横向海流迫使玩家在上下平台间换位；后半段同时出现高低两层安全区，重点考验跳跃时机和空中瞄准。

## 第 8 关：星坠核心井

- 主题语言：重力反转井壁、虚空牵引、收缩竖井。
- 闸门：引力棱晶闸。棱晶和裂隙沿井壁旋转，破坏后改变房间的重力极性并留下短时虚空残影。
- 小兵：虚空残壳、棱井长矛兵、逆相漂移体、星墓重构者。分别对应地底追猎、长矛直线、漂移穿插和重装牵引，视觉上使用紫色棱晶与反相轮廓。
- 首领：引力残响。三阶段攻击为“井内重力反转并交换极性”“星骸棱柱沿墙面折射坠落”“事件视界由外向内逐层闭合”，不会复用潮汐横扫或墓窟落棺。
- 难度重点：机关会短暂反转玩家重力，棱柱预警区逐层收缩；末段刷怪间隔最短，要求玩家利用平台边缘和冲刺穿过收缩安全区。

## 生成节奏

实现位置为 `Assets/Scripts/Level/VerticalStageDirector.cs` 与 `Assets/Scripts/Enemies/VerticalEnemyController.cs`：

- 玩家进入新房间后，首个敌人延迟约 0.45 秒出现。
- 每房间最多生成 7 个地底敌人；基础间隔为 `max(0.72, 1.75 - 房间序号×0.105 - 难度×0.2)` 秒，并受 `GameConfig.maximumEnemies - 5` 总量保护。
- 出生点在当前闸门上方约 0.9～1.2 单位处，敌人先播放裂地预警和上涌特效，再进入攻击状态；不会瞬移到玩家背后。
- 敌人轮换 Chaser / Gunner / Runner / Elite，并通过 `EnemyRoster.DefaultArchetype` 映射为本关专属名称、颜色和行为。第 6～8 关不共用同一套敌人命名和首领攻击表。
- `Assets/Scripts/Level/VerticalHazardEmitter.cs` 每个机关约 5.1 秒一轮，先预警，再进入约 1.3 秒激活窗口；墓窟为骨刺，圣堂为潮汐喷流，核心井为重力反相。

## 移动与镜头

- 玩家仍使用横版输入：`A/D` 移动、`Space` 跳跃、`S/Down` 下蹲或下落、`Shift` 冲刺；向下推进通过平台下落和击破闸门完成，不改变原有横版角色控制器。
- `Assets/Scripts/Core/SideCameraDirector.cs` 的 `TickVertical` 使用 `SmoothDamp` 软跟随，目标点根据玩家位置和瞄准方向偏移；相机上下边界由 112 单位竖井深度限制，左右边界由竖井宽度限制。
- `Assets/Scripts/Level/VerticalShaftParallax.cs` 让深层背景按 0.12 比例移动，近景墙刻、平台边缘和机关保持独立层级，形成明显的下潜视差。
- `Assets/Scripts/Level/VerticalStageDirector.cs` 为竖井创建 URP 2D 全局光和玩家轮廓点光，避免 2D Lit Sprite 在新分支中变成黑屏；光照随主题 accent 色变化。

## Inspector 设置

1. 打开 `Assets/Scenes/Prototype.unity`，确认 `GameBootstrap.config` 指向 `Assets/Settings/MoriEchoesConfig.asset`。
2. 在 `MoriEchoesConfig` 确认 `sideScroller = true`、`themes` 为 8 项、`characters` 为 12 项、`weapons` 为 9 项。
3. 在同一资产确认 `sideSpriteMaterial` 指向 `Assets/Settings/MoriEchoesSideSpriteLit.mat`；项目 Graphics 使用 URP 2D Renderer。
4. 不需要手动把三关脚本拖入场景。`LevelManager.Initialize()` 在选择第 6～8 关时自动创建 `VerticalStageDirector`、实体闸门、机关、场景反馈和程序化美术对象。
5. 若要在菜单直接测试新关卡，将进度解锁到 8；当前配置迁移工具已使用非破坏式追加并备份至 `Validation/CampaignBackups/`。

## 测试与验收

自动验收入口为 `Assets/Scripts/Diagnostics/VerticalCampaignSmokeTest.cs`，命令行参数为 `-moriVerticalSmoke <输出目录>`。它会逐关检查：

- 八房间、112 单位深度、7 扇实体闸门、14 个机关。
- 废墟墓窟的填充骷髅闸，以及另两关的独立符文/棱晶闸。
- 地底上涌、攻击预警、向下镜头跟随和三关主题场景光照。
- 每关专属首领、两次血量阈值相变、第三阶段处决和死亡演出。

最终实际验证：Unity `6000.6.7f1` Windows 开发构建路径为 `Builds/Windows/MoriEchoes_20261026_051615_432/MoriEchoes.exe`，BuildPipeline 0 错误/0 警告。三关运行验收报告为 `TestResults/Vertical_20261026_051700/vertical-campaign-smoke.json`，53 项检查通过、0 错误；截图为该目录下的 `stage_6_shaft.png`、`stage_7_shaft.png`、`stage_8_shaft.png`。最终 ZIP 解压副本报告为 `TestResults/PortableVertical_20261026_052200/vertical-campaign-smoke.json`，同样 53 项通过、0 错误。竖井入口提示已缩短为层数提示，避免长文遮挡画面。

本验收使用自动化血量、无敌和传送辅助，验证的是代码闭环，不替代真人难度平衡、实体手柄长测、跨机器性能或最终商业美术验收。
