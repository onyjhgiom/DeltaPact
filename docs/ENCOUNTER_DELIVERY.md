# 分关敌人、Boss 技能特效与稳定姿态 / Encounter V3

本轮实现五关独立的小兵阵容、整套 Boss 技能特效。按用户追加反馈，去掉 Boss 本体的周期性上下浮动和地面高度反复校正；地面 Boss 固定站姿，冰霜 Boss 固定悬停高度，终末 Boss 第二阶段只升空一次，随后固定高度。状态变化通过独立特效表现，不通过人物上下抖动表现。

## 每关的差异

| 关卡 | 四种独立小兵 | 小兵攻击特点 | Boss 标志表现与攻击 |
| --- | --- | --- | --- |
| 城市废墟 | 锯刃拾荒者、防毒面具枪手、三爪回收蜂、破障重装兵 | 枪手窄角双弹，近战推进，重装扇射 | 破碎者：装甲节点电弧；冲锋拖尾/落地尘、返身预警、落石雨、震地波、阶段碎甲爆发 |
| 工厂蜂巢 | 液压维修机、铆钉炮台兵、涡轮巡检蜂、熔炉重工机 | 较慢移动与重型低速弹，清晰蓄力窗口 | 熔核：双肩排焰与旋转炉环；铆钉扇射、地面熔池、维修蜂召唤环、过热爆破 |
| 寒峰隧道 | 霜爪潜猎者、晶矛射手、冰翼蝠、冰川巨像 | 霜爪起跳扑击、冰翼双弹、巨像宽角五连弹 | 霜牙：独立冰晶星环；本体固定高度，冰锥弹道/冷雾、冻结区域、声呐扩环 |
| 未来都市 | 刃影武士、光轨狙击手、棱镜侦察蜂、量子武士 | 刃影锁定后突进，长蓄力高速弹，灵活飞行 | 零点：全息扫描节点与扁平扫描环；高低激光、同造型半透明分身、紫色弹幕、迟滞场 |
| 最终要塞 | 赤刃近卫、王庭枪骑士、血瞳巡游者、处刑重骑士 | 血瞳低转向追踪弹、近卫与枪骑士混编 | 终末回响：赤红翼阵与冠冕光点；四种轮换技能、追踪弹、双臂晶体护盾/破甲爆发 |

所有敌方子弹按当前关卡使用专属贴图、残迹与命中特效。每种小兵有两帧姿态，部分近战另有跳扑/突进；不是20套完整骨骼动作。前三类及精英均可在各关出现，前两关精英从第三封锁区加入。原四类通用敌人素材保留作为缺省回退。

## 关键文件与素材

- `Assets/Scripts/Enemies/EnemyRoster.cs`：20 个独立敌人名称。
- `Assets/Scripts/Enemies/SideEnemyMotor.cs`：分关弹型、蓄力、跳扑/突进；技能眩晕中断蓄力。
- `Assets/Scripts/Enemies/SideBossController.cs`：阶段逻辑、固定高度、返身冲锋预警、世界坐标技能区、召唤/分身/护甲特效。
- `Assets/Scripts/Enemies/BossSignatureVfx.cs`：五套持续状态特效，仅变换特效子物体，不变换 Boss 本体。
- `Assets/Scripts/Enemies/BossField.cs`：激光/元素区域的可见核心、真实伤害边界、暂停计时、死亡和重试清理。
- `Assets/Scripts/Core/CombatEffects.cs`：224 个固定特效槽，128 普通效果、64 战斗效果、32 预警；装饰拖尾不能占用预警槽。
- `Assets/Scripts/Combat/Projectile.cs`：敌方弹道、残迹、命中；对象池复用时清除敌方材质选择，玩家射击保留原表现。
- `Assets/Scripts/Level/ThemeProfile.cs`、`LevelManager.cs`、`Core/SideCharacterVisual.cs`、`Core/SideSpriteAnimator.cs`：按关卡绑定敌人动画/攻击姿态。
- `Assets/Art/Production/EncounterV3/`：60 精灵（40 敌人动画帧、20 技能效果）、40 近似法线图及 SHA256 清单。
- `Assets/Editor/EncounterArtSetup.cs`：整批预检、非覆盖导入、配置备份；`Assets/Settings/MoriEchoesConfig.asset` 为实际配置。
- `Tools/Prepare-EncounterArt.py`、`Tools/Tests/Test_EncounterArt.py`：透明切片、朝向修正、完整性和边界检测。
- `ArtProduction/encounter-v3/prompts.jsonl`、两个修图提示文件、`output/imagegen/`：图像 API 原始生成/修图记录；`contact-sheet.png` 为实际导入图集预览。

已沿用用户授权的 imagegen CLI/API 路径，经兼容网关以 `gpt-image-2.5` 请求标识生成；不推断网关实际底层模型。12 张输出中包括10张初稿、1张霓虹布局修订和1张双精英修订，原图全部保留。最后采用独立精英图修复长枪跨格问题，并逐帧校正朝向。法线由透明度/亮度近似生成。没有在项目保存 API 凭据。

## Inspector 与启动

1. Unity Hub 添加 `C:\UnityProjects\MoriEchoes`，使用 `6000.6.7f1` 打开；等待导入完成。
2. 打开 `Assets/Scenes/MainMenu/MainMenu.unity`。GameBootstrap 的 Config 应指向 `Assets/Settings/MoriEchoesConfig.asset`。不需要在已有场景手动添加 Boss 或特效管理器，运行时自动创建。
3. 选择该 Config，展开 Themes 的元素0–4。每项 Enemy Animations 有四项，顺序为 Chaser/Gunner/Runner/Elite；每项 Frames 为2、Faces Right 关闭、Frames Per Second 为4。Encounter Effects 顺序：弹体、法环、命中、喷发。
4. 若新素材尚未绑定，运行菜单 `Tools > Mori Echoes > Production Art > Import Reviewed Encounters V3`。导入器只接受已目检且散列匹配的图集；遇到自定义已有引用会保留并报错，不会强行覆盖。
5. Play 后选择角色和关卡；按原规则逐关解锁。新改动不改变 A/D、Space、鼠标射击、Q、Tab、F11 操作。调试时在 Hierarchy 选择 Boss，查看 SideBossController 和 BossSignatureVfx；后者的特效子物体运动不应带动刚体上下振荡。
6. 要构建新的时间戳版本，使用已有 `MoriEchoesSetup.BuildWindows`；共享游戏时必须复制整个构建文件夹。

## 四名角色的独立技能

用户追加要求已并入本轮，每人 Q / 手柄 Y 触发自己的技能，使用独立效果、描述和冷却，不共享通用脉冲作为主技能。

| 角色 | 技能 | 效果 | 持续 / 冷却 | 视觉 |
| --- | --- | --- | --- | --- |
| DELTA | 战术专注 | 射速 +60%，暴击率 +20% | 5 / 14 秒 | 蓝色战术瞄准环 |
| VANGUARD | 复苏壁垒 | 立即恢复最大生命25%（不超上限），减伤70%，35点近身冲击 | 5 / 16 秒 | 绿色恢复十字、护盾环 |
| WRAITH | 相位跃迁 | 朝瞄准方向最多5.5单位传送；随后移速+50%、减伤40%，到达点附近非Boss冻结2秒 | 4 / 12 秒 | 起终点紫色传送门、运动残像 |
| HAVOC | 蜂群轰击 | 三发范围爆破弹，不耗枪械弹药，40基础伤害/弹、2.4半径 | 即时 / 14 秒 | 金色三叉爆破光纹 |

传送使用角色胶囊扫掠和终点重叠检查，会在墙/封锁门前停止；路径不足0.25单位则提示受阻且不消耗冷却。保留跳跃计数，不能通过连续传送重置空中跳。重试重置技能，暂停冻结冷却。原脉冲升级仍作为技能附加效果。

文件：`Assets/Scripts/Player/CharacterSkill.cs`、`SidePlayerMotor.cs`、`Assets/Scripts/Level/CharacterDefinition.cs`、`Assets/Scripts/Core/CombatEffects.cs`；四张专属特效为 `Assets/Art/Production/SkillsV1/skill-0.png` 至 `skill-3.png`。实际图像生成原图/提示在 `ArtProduction/skills-v1/`。加工与导入工具为 `Tools/Prepare-SkillArt.py`、`Assets/Editor/SkillArtSetup.cs`。

Inspector：Config > Characters > 元素0–3 > Skill Sprite 按顺序绑定四图；也可运行 `Tools > Mori Echoes > Production Art > Import Reviewed Character Skills` 完成带散列校验的绑定。技能数值目前在 CharacterSkill 中定义，不伪装成 Inspector 可编辑参数。

验收：分别选四角色按Q；DELTA检查射速和暴击；VANGUARD半血时恢复25%、满血附近不超上限、到期恢复正常受伤；WRAITH检查实际位置变化、墙前停止、受阻不扣冷却、到达冻结；HAVOC检查爆炸伤害且枪械弹药不变。再检查暂停冷却、检查点重置和菜单描述。自动回归使用 `Tools/Test-AnimeArt.ps1 -Executable <实际exe> -Width 1280 -Mode Arsenal`，其中墙体/受伤为测试专用条件，不写回场景。

## 测试步骤和验收标准

1. 每关实际召唤四类小兵，确认贴图/名称不与其他关重复；枪手与飞行单位有蓄力→发射→弹道→命中的完整过程。
2. 每关 Boss 血量降至50%及20%附近触发三阶段；检查预警后才发招、飞弹/区域/召唤/弱点有对应特效。终末 Boss 每阶段至少覆盖四种轮换技能。
3. 观察至少三个完整攻击周期：Boss 无周期性上下浮动，不能通过持续晃动本体表现蓄力。冰霜保持固定高度；终末升空后稳定。冲锋允许水平位移，阶段转换保留特效。
4. 熔池/激光释放后移动 Boss：伤害区域应留在原世界坐标；可见范围外不扣血，范围内按间隔扣血；冰冻只在成功受击时施加。
5. Esc 暂停应冻结技能区生命周期；击杀 Boss、检查点重试或返回菜单后无残留伤害区。胜利时爆发特效可播放完毕。
6. 特效不能盖住玩家整个人体，喷发起点应贴地；法环/喷焰在人物后方，飞行弹体和短促命中闪光在前方。独立预警槽避免普通拖尾挤占预警。
7. 自动专项命令：`Tools/Test-AnimeArt.ps1 -Executable <实际exe路径> -Width 1920 -Mode Encounter`；完整战役：`Tools/Test-SideScroller.ps1 -Executable <实际exe路径> -Widths @(1280)`。报告写入独立时间戳 TestResults 目录，不改用户解锁存档。

## 实际结果

最终实际 Windows Development 包：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261025_004020_691\MoriEchoes.exe`。Unity 6000.6.7f1，构建0错误、0警告，包体424,534,219字节；构建记录 `docs/BuildReports/Windows_20261025_004020_691.md`。启动器 `PLAY_MoriEchoes.cmd` 指向此版本，旧包保留。

- 角色/六武器/窗口专项：312项通过、14张非空截图、0运行错误、退出码0。报告 `TestResults/Arsenal_1280x720_20261025_004829_422/stage-smoke.json`；截图包含1024×768、1600×720、1600×900和1280×720。
- 五关Boss/小兵专项：164项通过、35张非空截图、0运行错误、退出码0。报告 `TestResults/Encounter_1280x720_20261025_005518_738/stage-smoke.json`；五关均通过本体高度稳定检查，覆盖三阶段技能、20种小兵绑定与攻击、场地伤害边界、暂停、死亡/重试清理。初始窗口1280×720，19张为此尺寸，其余16张为运行期间最大化后的2880×1479；不能称为全程固定720p测试。报告的 screenshotSizes 记录每张实际尺寸。
- 最终包完整五关战役回归：124项通过，0运行错误、退出码0；报告 `TestResults/Side_1280x720_20261025_011117_626/stage-smoke.json`。包括五关推进、Boss三阶段、升级、胜负/重试、视角及原动画集成回归。首轮在19项后的合成缓冲跳输入断言失败，未修改游戏代码的重复运行通过；该短时间按键测试存在时序敏感性，失败报告 `TestResults/Side_1280x720_20261025_010301_897` 保留，不作为稳定性已彻底解决的结论。
- 54个Unity C#源文件×3宏分支语法检查、10,040项纯模型断言、13项既有导入保护检查通过；14项美术加工测试通过，60张新战斗图散列/透明度/唯一性检查通过，四张新角色技能图散列和透明度检查通过。语法检查不替代Unity实际编译，实际编译证据见构建日志。
- 战役截图记录为29次，其中3张1280×720、26张2880×1479，运行期间窗口被最大化；截图尺寸写入JSON。后续仍需无人调整窗口的固定分辨率性能测量。
- 13个原场景及关联meta文件SHA256保持一致；无删除原场景/素材。全量变更清单在 `docs/ENCOUNTER_FILE_CHANGES.csv`。

保留的迭代记录：首轮构建通过154项，但截图发现熔池遮挡与Boss边缘取景，随后改为人物背后喷发、按地面定位并调整测试取景；用户追加要求后移除上下浮动、增加五套标志特效。角色技能首轮在264项后因传送终点的随机障碍使旧加速位移检查失败，改在Boss空地验证加速，实际最终包312项通过。加工器重叠测试最初因测试图不透明先触发透明度校验，修正合成样例后14项通过；新技能图的最大alpha为253，允许半透明光效的检查通过，未强行改写原alpha。

源码备份：`Validation/EncounterBackup_20261024_231759`；首次美术配置备份：`Validation/EncounterConfig_20261024_234853_280.asset`；角色技能配置备份：`Validation/SkillConfig_20261025_002853_440.asset`。人工难度平衡、实体手柄长测和跨机器性能未验证；自动测试使用合成输入、无敌/传送与血量阈值辅助，不等于真人通关验收。
