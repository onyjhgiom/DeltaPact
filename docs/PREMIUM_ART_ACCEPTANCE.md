# 生成美术与高难度战役交付 · 2026-10-27

本轮直接修改 `C:\UnityProjects\MoriEchoes`。不制作新的压缩包，旧构建、原角色资源和历史交付资料保留。当前启动路径、实际测试结果以本文末尾的“最终运行证据”及 `ArtProduction/descent-premium-v1/delivery-verification.json` 为准。

## 美术实际交付

通过用户指定的兼容网关，使用 imagegen 技能的原装 CLI，向图像生成接口请求 `gpt-image-2.5`，得到 56 张 PNG 原图。这个名称是网关的请求模型名，不能据此独立证明网关内部供应商或后端型号。生成过程没有将凭据写入工程。

- 原图：`C:\UnityProjects\MoriEchoes\output\imagegen\descent-premium-v1`。
- 全部最终提示词：`C:\UnityProjects\MoriEchoes\ArtProduction\descent-premium-v1\prompts`，含 `production-batch.jsonl`、`frontline-batch.jsonl`、`hazard-batch.jsonl`。
- CLI：`C:\Users\陈乐天\.codex\skills\.system\imagegen\scripts\image_gen.py`，模式 `generate-batch`；透明 PNG 保留原生 Alpha。
- 游戏素材：`C:\UnityProjects\MoriEchoes\Assets\Art\Production\PremiumV1`；313 个 Sprite、234 张由图片加工生成的法线贴图。详细文件、源图及 SHA256 见该目录 `art-manifest.json`。
- 美术总览：`C:\UnityProjects\MoriEchoes\ArtProduction\descent-premium-v1\review`。原图、加工前裁切和被替换配置均保留备份。

前五关按废墟、工厂、寒冰、霓虹都市、要塞分别制作四种敌兵的两帧图，共 40 个敌兵 Sprite；30 个落石/地刺/炮塔 Sprite；30 个实体建筑、秘密入口、掩体与油桶模块；20 个独立密道守卫 Sprite；10 个 Boss 第二/第三阶段形态；40 个攻击、阶段变化、命中和死亡反馈纹理。第一阶段 Boss 和原有前四名角色美术保留。

其余素材包括四名新增角色的 32 帧动作、4 个头像、4 个强化形态、8 个角色技能图；第六至第八关的背景、建筑、敌兵、三阶段 Boss 和效果；激光刃、电弧投射器、星脉连发弩及三把武器的瞄准前景；8 个强化胶囊、武器箱、芯片、医疗等拾取物图像。原始设计图均经过实际裁切、尺寸与脚底基线统一、边缘碎片清理后接入游戏，不把整张设计板充当单个角色。

当前可选角色严格为 DELTA、VANGUARD、WRAITH、HAVOC、KALT、VEIL、SERA、ROOK。角色名称对应的内部身份固定为 0/1/2/3/8/9/10/11，避免缩减名单后技能错位；移出名单的旧素材保留用于回退。

Boss 最新追加：16 张设计原图加工为 96 个实体装饰、32 个专属大招帧和 96 张法线。八关每阶段使用四个装备部件，替代抽象几何挂件。完整素材与透明处理记录、Inspector、防御规则见 `docs/BOSS_ASCENSION_ACCEPTANCE.md`。

## 难度与强化规则

新兵难度的全局敌方伤害倍率为 `1.65 + 关卡索引 × 0.08`，前五关为 1.65/1.73/1.81/1.89/1.97。老兵再乘 1.5，契约再乘 2.2。玩家护盾、闪避无敌等减伤仍按角色规则结算。

- 普通小兵速度在原有兵种速度上乘 `1.5 + 关卡索引 × 0.035`，前五关为 1.50～1.64 倍；狙击、炮台等固定阵地单位按其设计站定。每关有独立兵种阵容，多方向与上方来敌、跳跃追击、编队行为保留；狙击预判线已关闭。
- Boss 最新常规/终极技能伤害乘区为 2.25/3.25，再乘全局敌伤；横版区域长宽各乘 1.65/1.9，面积约为旧基础的 2.72/3.61 倍。竖井弹体带 0.9075/1.71 爆炸半径。Boss 自身减伤 62%、生命 ×1.5、单次受到伤害上限为总血量 0.8%；处决保留。详见 `docs/BOSS_ASCENSION_ACCEPTANCE.md`。
- 落石基础伤害 24、地刺 19，再乘全局倍率；第一关新兵未减伤时分别为 39.6/31.35。保留 1.3 秒落石启动延迟、1.15 秒地刺启动延迟及至少 0.6 秒 Boss 蓄力时序，但按最新要求不显示任何提前范围标记；真正出招后才显示攻击美术和特效。
- 能量胶囊的同武器基础总弹丸伤害仅提高 8%～10%，射速提高 4%～8%；按弹丸数归一总伤害预算，修复散射额外弹丸让伤害成倍膨胀的问题。四种胶囊各有外观、扩散/穿透/爆破机制与限时强化形态。该比例相对于胶囊对应武器，实际战斗还受暴击、角色专精、连击、穿透和爆炸命中数量影响。

相关实际文件：`Assets/Scripts/Core/GameConfig.cs`、`GameManager.cs`、`Assets/Scripts/Enemies/EnemyAI.cs`、`SideEnemyMotor.cs`、`SideBossController.cs`、`BossField.cs`、`Assets/Scripts/Level/EncounterHazardDirector.cs`、`Assets/Scripts/Weapons/CombatPowerup.cs` 与 `WeaponController.cs`。使用美术的运行组件及全部修改清单见 `ArtProduction/descent-premium-v1/file-changes.json`。

## Unity Inspector 与运行步骤

1. 用 Unity `6000.6.7f1` 打开 `C:\UnityProjects\MoriEchoes`，等待编译，Console 不应有脚本红错。不升级包版本；已固定 Input System 1.20.0、URP 17.6.0、UGUI 2.6.0。
2. 打开 `Assets/Scenes/MainMenu/MainMenu.unity`，场景内唯一 `GameBootstrap` 的 Config 指向 `Assets/Settings/MoriEchoesConfig.asset`。对象由运行时生成，不需要手动摆放 313 张图片。
3. 在上述 Config Inspector 核对 Characters 为 8、Weapons 为 9、Themes 为 8；每个 Theme 的 Enemy Animations 有 4 项，Encounter Effects 有 8 项，Boss Forms 有 3 项；前五关 Hazard Animations 有 2 组、Stationary Animation 炮台图已绑定。
4. Config 的 Incoming Damage Multiplier 为 1.65、Stage Damage Step 为 0.08，All Characters Available 勾选。前四名角色不重新分配为新增角色图。
5. 随机选择 `Assets/Art/Production/PremiumV1/Enemies/ruins-0-0.png`：Texture Type 为 Sprite (2D and UI)，Single，Full Rect，中心 Pivot，Filter Bilinear，Wrap Clamp，无 Mipmap、无压缩。精确 Pixels Per Unit 以 manifest 每条记录为准（该敌兵为 256）；Secondary Textures 的 `_NormalMap` 指向同名 `-normal.png`，法线图 Texture Type 为 Normal map。
6. 以上已由 `Assets/Editor/PremiumArtSetup.cs` 实际导入并写入配置。只有需要重新绑定时，退出 Play Mode，运行菜单 `Tools > Mori Echoes > Production Art > Apply Premium Campaign Art`。它先校验全部哈希，并将旧配置复制到 `ArtProduction/descent-premium-v1/config-backups`。不要运行旧的重置战役工具覆盖当前角色配置。
7. 点击 Play，或双击工程根目录 `PLAY_MoriEchoes.cmd`。启动器会指向完成本轮运行测试的具体 EXE。详见根目录 `HOW_TO_PLAY.md`。

## 最新追加：移除攻击预判图示

全部八关统一关闭红黄色预判线、半透明范围方块/圆圈、地面落点条、路径走廊、边框及倒计时。小兵瞄准线、落石/地刺落点、竖井机关预警图标、Boss 影响区域与密道守卫预判图示均受同一展示策略控制。HUD 不再播报 Boss 即将使用的招式、规避攻略或“下一招”文字；保留 Boss 名字、形态、血条、实际弱点血量与处决提示。

`Assets/Scripts/Core/CombatEffects.cs` 的只读 `AttackTelegraphsEnabled` 为 false，不需要 Inspector 设置。`BossField.cs` 在蓄力期间隐藏整个攻击图像，出招后只显示攻击纹理，始终隐藏几何伤害边框；伤害时序、碰撞和扩大后的范围不变。真实激光束、剑击、爆炸、命中反馈、敌人本体动作和场景实体不属于预判辅助线，继续显示。Boss 的发招脉冲移至实际出招时刻。

复测：任意五关 Boss 等待其蓄力和出招，蓄力时无落点/路径/范围图形及攻略文字，出招时能看到实际攻击效果；落石地刺仍正常触发并造成伤害。Encounter 自动组逐阶段采样预判图示是否可见，同时验证实际攻击特效计数增加，另保存 `hidden_preparation_phase` 截图。竖井组验证全程预警池没有产生任何图示。

## 可复测步骤与验收标准

- 运行 `Tools/Test-AnimeArt.ps1 -Executable <完整EXE路径> -Mode Arsenal`：8 名角色技能、16 招辅助技、8 种终结技、9 把武器实弹命中/穿透/爆炸、瞄准前景、装弹、窗口切换和 UI 边界均通过。测试的静止目标保留在敌人注册表中，且不把大招本次命中重新获取的能量误判为未扣能量。
- 同工具 `-Mode Encounter`：前五关独立敌兵、机关实体图、秘密守卫、三阶段 Boss 形态、15 种独立攻击标识、自然攻击释放、阶段特效、无预判图示、世界固定伤害区、Boss 垂直稳定、死亡清场与胜利结算通过。
- 同工具 `-Mode Art`：美术实际绑定与可见截图、动画状态、瞄准时角色隐藏、还原及合成纹理回归测试通过。合成探针仅临时覆盖配置并恢复，不代表美术质量认证。
- `Tools/Test-SideScroller.ps1 -Executable <完整EXE路径> -Widths @(1280)`：横版移动、跳跃、闪避、三种视角、完整五关推进、Boss 阈值、结算和检查点回归通过。
- `Tools/Test-VerticalCampaign.ps1 -Executable <完整EXE路径>`：三条竖井的地形闸门、敌潮、镜头、三形态 Boss 和结算，并检查至少 15 张可见截图（含三关大招）。当前工程已有向上推进实现，与 `docs/ASCENT_LEVELS_DESIGN.md` 一致；早期 `VERTICAL_CAMPAIGN.md` 中向下推进说明属历史记录，本轮没有改动推进方向。
- `Tools/Validate-Source.ps1`：纯 C# 模型、三套条件编译语法及导入安全检查通过；它不代替 Unity 实际编译。

每个 Player 测试必须退出码 0、对应 JSON 的 passed 为 true 且 errors 为空。截图非空只是自动底线，仍需目视检查角色边缘、材质、Boss 形态和界面。默认关卡按进度解锁，测试以内存存档解锁，不修改真人存档。

## 明确限制

当前交付为生成的 2D 位图及有限帧动画，搭配法线光照、粒子、镜头和场景反馈。仍保留不可见碰撞体与部分程序化粒子，不能等同于高预算 3D 建模或商业最终美术认证。第一人称为用户确认的 2D 武器前景瞄准表现。

自动测试使用合成输入、无敌、传送和显式 Boss 血量阈值，不能证明真人能在当前高难度下通关。人工手感/难度平衡、实体手柄长测、跨机性能、长时间内存稳定性和 Editor Play Mode 操作均不得据此标记为已通过。

## 最终运行证据

本轮最终 Windows Development 构建：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261027_131724_160\MoriEchoes.exe`。Unity 6000.6.7f1 / StandaloneWindows64，构建成功、0 错误/0 警告；构建记录：`docs/BuildReports/Windows_20261027_131724_160.md`。下面五组均针对这一构建实际运行，Player 退出码 0、passed=true、errors 为空；详细范围以 JSON 为准。

| 实际运行组 | 通过检查 | 错误 | 原始报告 |
|---|---:|---:|---|
| arsenal | 577 | 0 | `TestResults/Arsenal_1280x720_20261027_132455_096/stage-smoke.json` |
| encounter | 345 | 0 | `TestResults/Encounter_1280x720_20261027_132017_952/stage-smoke.json` |
| art | 48 | 0 | `TestResults/ProductionArt_1280x720_20261027_132704_012/stage-smoke.json` |
| campaign | 132 | 0 | `TestResults/Side_1280x720_20261027_132806_955/stage-smoke.json` |
| vertical | 87 | 0 | `TestResults/Vertical_20261027_131844_844/vertical-campaign-smoke.json` |

源码校验：10040 项模型断言、5000 次有种子升级抽取、13 项导入安全检查。语法检查原始结果：C# 9 syntax check: 83 Unity source files x 3 define variants; 0 syntax errors.。它不替代上述 Unity 构建和 Player 测试。

这次回归修正了旧测试对角色 UI 名称、机关预警实现、强化技能充能、Boss 处决时长和新版招式的过时假设。诊断模式隔离合成键鼠的失焦状态，Boss 伤害边界探针在独立坐标等待物理同步后测量；正常游戏仍保留失焦暂停。位移技能共用胶囊碰撞探测，WRAITH / VEIL / ROOK 的实体墙阻挡和受阻保留能量/冷却已实际验证。战术镜头按玩家与 Boss 的间距平滑取景，五关 Boss 入镜检查通过。末关两个锚点独立控制交叉斩，击破后对应斩击清除；核心按现有规则可受伤。Boss 处决截图包含慢动作过程，结算在演出结束后检查。

复测工具使用 PowerShell 7。Windows PowerShell 5.1 会按 ANSI 误读现有 UTF-8 中文脚本，源码校验应在 `pwsh` 中运行。历史失败、超时和提前退出记录保留在 TestResults，不计为通过，也不替代本表的最终结果。

启动 `C:\UnityProjects\MoriEchoes\PLAY_MoriEchoes.cmd`。没有生成新 ZIP。完整变更和哈希见 `ArtProduction/descent-premium-v1/file-changes.json` / `.csv`，构建文件哈希见同目录 `windows-build-files.json`，机器可读验收见 `delivery-verification.json`。

最新追加已实际接入：八关统一关闭攻击预判线、范围方块/圆圈、路径走廊、落点标记、倒计时和下一招提示文字。实际攻击贴图、激光、爆炸和命中反馈保留。Encounter 组在每个 Boss 阶段采样确认预判图示不可见、实际特效仍发出，竖井组检查预警池零发射。

Boss 追加 16 张设计原图加工为 96 个实体装备部件、32 个大招帧及 96 张法线，已接入八关三阶段；减伤 62%、生命 1.5 倍、单次伤害上限 0.8% 已通过真实伤害调用检查，处决结算保留。具体 Inspector、材质加工、数值和测试边界见 `docs/BOSS_ASCENSION_ACCEPTANCE.md`。两批合计 72 张设计原图、441 个 Sprite、330 张加工法线；透明编辑试验图不计为额外交付设计。

目视复核另修正竖井装饰：粗略骷髅/圆环换成主题实体浮雕，场景反馈保持原始透明度和朝向。演出锁定时真正停止重力漂移，竖井 Boss 镜头同时容纳玩家和 Boss。旧版 R21 的横版自动驾驶在高台旁反复跳跃而超时，现使用真实 S+Space 下穿输入通过；该失败记录保留，最终表仅计修正后的复测。运行验证针对当前已配置的八关；工程中另有未配置的第九、十关开发代码，本轮未删除，也不将其列为已验收关卡。

人工难度平衡、实体手柄长测、跨机性能、长时间内存稳定性和 Editor Play Mode 仍未验证。
