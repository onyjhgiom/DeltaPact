# 关卡平台统一、伤害与路线修复验收

本轮范围是当前 8 个已配置关卡。保留 8 人和 9 把武器。第 9、10 关的未配置工作不在本轮验收内。未制作压缩包。

## 行为与文件

| 内容 | 实际文件 | 验收要求 |
|---|---|---|
| 全关平台统一 | `Assets/Scripts/Level/StageSurfaceArt.cs`、`SegmentBuilder.cs`、`SolidWallModule.cs`、`VerticalStageDirector.cs`、`SecretBranchDirector.cs`、`Assets/Scripts/Enemies/BossField.cs` | 主地面、普通/单向/冰面/传送带平台、电梯、密道桥、密室及竖井结构按所属关卡选同一主材质。Boss 临时可站立传送带也遵循该规则。保留物理尺寸和机关功能。 |
| 电梯与冗余墙 | `Assets/Scripts/Level/SegmentBuilder.cs`、`LevelManager.cs` | 电梯行程内不再插入短平台；移除已常开且无阻挡意义的交战门、电磁门及关联开关。保留边界、有效落脚点及可破坏掩体。 |
| 适度高伤害 | `Assets/Scripts/Combat/Health.cs`、`Projectile.cs`、`ProjectilePool.cs`、`Assets/Scripts/Core/GameConfig.cs`、`Assets/Settings/MoriEchoesConfig.asset`，敌人/机关各伤害调用 | 敌兵/机关/Boss 常规技/大招单次伤害上限分别为基础生命的 24%/28%/36%/45%，且不超过所选角色最大生命的 55%；减伤在上限之后计算，受击保护 0.6 秒。满血不可被普通伤害一击毙命，残血可死亡。跌出地图属于失败条件。 |
| 受击血条 | `Assets/Scripts/UI/HUDController.cs`、`Assets/Scripts/Player/PlayerController.cs`、`Assets/Scripts/Core/CombatEffects.cs` | 青色血条立即反映真实生命；红色损失段保留 0.38 秒后平滑缩减，受击闪光、伤害数字和屏幕边缘反馈可见；致死后先显示掉血过程再显示结算。 |
| Boss 预警 | `Assets/Scripts/Enemies/BossField.cs`、`SideBossController.cs`、`VerticalBossController.cs` | 蓄力显示半透明红色区域，预警本身不伤人；实际攻击沿锁定区域或路径释放。圆环保留内圈安全区。小兵旧预判线继续隐藏。 |
| 单次中段密室 | `Assets/Scripts/Level/SecretBranchDirector.cs`、`SegmentBuilder.cs`、`LevelManager.cs` | 每关仅一扇入口。横版在第 4 段上层，约全程 43.5%；竖井在 y=50.65 的左侧支桥，约全程 52%。接触实体门进入，无须按 E。 |
| 守门增援 | `Assets/Scripts/Level/SecretBranchDirector.cs` | 原有交战阵容之外追加第 1–8 关依次 16、16、17、17、18、18、19、19 名增援，每 0.8 秒最多 2 名，受全局同时在场上限约束；不会一次刷新全部。 |
| 密室独立流程 | `Assets/Scripts/Level/SecretBranchDirector.cs`、`Assets/Scripts/Enemies/EnemyAI.cs`、`Assets/Scripts/Core/SideCameraDirector.cs`、`Assets/Scripts/Level/EncounterHazardDirector.cs` | 进入后暂停主路敌人的物理活动和主路推进；独立守卫、主题房间、故事提示；接触左侧门返回。每关守卫不同于主 Boss，击败后芯片、胶囊和风格奖励只发一次；重试不复刷已清密室。 |
| 第 7、8 关入口 | `Assets/Scripts/Core/GameConfig.cs`、`GameManager.cs`、`Assets/Settings/MoriEchoesConfig.asset`、`Assets/Scripts/UI/HUDController.cs` | `All Stages Available` 启用，8 关可直接选择；仍记录实际通关进度与评级。 |
| 竖井可达性 | `Assets/Scripts/Level/VerticalStageDirector.cs`、`VerticalSkullGate.cs`、`Assets/Scripts/Combat/Projectile.cs` | 中央交错平台间距 2，正常跳跃能衔接；上射穿过单向平台后可击破闸门，检查点重试保留已破闸门；最终 Boss 场有实体落脚平台。 |
| 清理旧几何标记 | `SegmentBuilder.cs`、`StageHazard.cs`、`SolidWallModule.cs`、`VerticalStageDirector.cs`、`VerticalSkullGate.cs` | 移除旧平台亮线、彩色边条、悬浮全息块、旧密室圆环等；实体墙、机关和平台使用已有生成插画；机关伤害尺寸独立于贴图尺寸。Boss 新增红色预警是本次明确要求保留的区域。 |

## Unity Inspector 检查

1. 使用 Unity **6000.6.7f1** 打开 `C:\UnityProjects\MoriEchoes`，打开 `Assets/Scenes/MainMenu/MainMenu.unity`。
2. 在 Project 选中 `Assets/Settings/MoriEchoesConfig.asset`，确认 **All Stages Available** 勾选、Characters 为 8、Weapons 为 9、Themes 为 8。现有素材引用保留。
3. Hierarchy 选中 GameBootstrap，确认 Config 指向该资产。关卡、密室及 HUD 由运行时搭建，不需要手工拖引用，也不要将运行时实例保存回场景。
4. Play 后进入任意关，在 Hierarchy 的 Level 下检查唯一的 `IllustratedSecretEntrance`。BoxCollider2D 为 Trigger，尺寸 **1.45 × 2.25**；其子物体 `IllustratedDoorBody` 使用主题素材。
5. 竖井中检查 `Vertical_Climb_0` 等平台：BoxCollider2D 使用 PlatformEffector2D，Use One Way 开启，Use Collider Mask 关闭。`Vertical_BossArenaFloor` 位于 y=101.0，最终台阶高度差 1.5，确保胶囊完整越过平台上沿后落地。
6. 在 Boss 蓄力时暂停，检查 `DamageFootprint` 的 SpriteRenderer：红色、Alpha 小于 0.5；真实攻击开始后由主题特效显示。旧 `Edge_*`、倒计时及小兵预判条不应显示。
7. 受击时检查 HUD 的 `RecentDamageTrail` 和 `PlayerHitVignette`。Inspector 仅用于检查，不要求添加场景引用。
8. Config 的 `Platform Sprites` 前五项依次绑定城市废墟、工厂、寒峰、未来都市、最终要塞现有主地面素材。后三关使用各自 Theme 的 Architecture 第 0 项。无需替换已有原图，也无需重新导入其他关的平台。
9. 运行时选中普通平台、电梯或密室桥，其 `StageSurfacePanel` 的 Sprite 应与该关主地面一致；前三个竖井同样检查 `Vertical_Climb_*`、Boss 落脚台和侧墙。机关、油桶和可破坏掩体保持自身用途造型，不被误替换为地砖。
10. Config 检查 `Incoming Damage Multiplier=1.35`、`Stage Damage Step=0.04`、`Player Maximum Hit Fraction=0.55`、`Player Hit Protection Seconds=0.6` 和四档 Hit Limit=0.24/0.28/0.36/0.45。不要将这些值改回早期高伤害方案。

## 手动重测步骤

1. 从主菜单依次选择第 7、8 关并部署，应直接进入，能够跳过第一波区域继续攀升。
2. 受到普通子弹与 Boss 攻击，观察真实 HP、红色损失段缓慢消失、人物命中效果与屏幕反馈；致死仍能看到归零过程。
3. 横版沿第 4 战斗段上层寻找实体门；竖井沿中段左侧支桥寻找门。门附近比普通段多出持续增援，站在旁边不会自动进入，角色身体接触门才进入。
4. 密室内击败独立守卫，拾取奖励；接触左侧门返回主路。回来后入口不再生成第二间密室，不重复发奖励。未击败时可先返回，再从同一门继续挑战。
5. 观察各关 Boss 的红色落点/攻击路径，离开后应能躲避相应伤害；预警显示时不应提前扣血。
6. 第 6–8 关逐层射开闸门向上跳跃，死亡重试应站在正确检查点，已破闸门保持打开。
7. 八关分别观察主地面、上层、密室和移动平台，不能出现其他关卡的通用工业板。第 4、5 关坐电梯上下运行，行程中不得碰到旧短平台；正常站立不应被强制改为蹲姿。
8. 用满血角色分别承受小兵、机关、Boss 普通技与大招，观察中上强度掉血且保留反应时间；护盾/减伤仍应有效。测试报告中的单次上限不是每次攻击的固定伤害。

## 平台用图规则

| 关卡 | 主材质 |
|---|---|
| 城市废墟 | 废墟混凝土地面 |
| 工厂蜂巢 | 绿色工业管线金属 |
| 寒峰隧道 | 冰雪岩层 |
| 未来都市 | 蓝色科技合金 |
| 最终要塞 | 暗红黑装甲 |
| 废墟墓窟 | 雕刻墓窟石材 |
| 沉没圣堂 | 蓝金圣堂石材 |
| 星坠核心井 | 紫色裂隙结构 |

现有 image 生成素材直接复用，新增的是统一选图与铺设入口，没有将新生成图片冒充成本轮产物。截图预览：`Validation/FeedbackTraversal_20261027_Delivery/stage-surfaces-review.jpg`。

## 自动测试与边界

入口 `Tools/Test-FeedbackTraversal.ps1` 和 `Assets/Scripts/Diagnostics/FeedbackTraversalSmokeTest.cs`：新内存存档测试八关菜单，真实子弹/范围伤害触发血条，八关中段门接触、额外守门刷怪、独立守卫奖励和返回，以及三个竖井的实际跳跃输入贯通。`-ClimbOnly` 仅用于单独排查攀升，不能替代完整验收。

测试使用当前进程中的合成键鼠、无敌辅助和击破闸门、清理近身挡路小兵的高伤害真实投射物；Boss 在通路探测时设为无敌，防止辅助射击提前触发处决。密室入口检查会先定位到门附近；密室奖励检查会直接击杀守卫。竖井贯通段不写入位置、不传送，使用真实跳跃与左右输入。测试使用临时内存存档，不改写玩家磁盘进度。新增伤害矩阵覆盖 8 角色 × 4 伤害类型 × 3 难度 × 8 关 × 2 组伤害样本，并检查减伤与连续命中保护。

`Tools/Test-AnimeArt.ps1 -Mode Encounter` 复核前五关 Boss 各阶段；`Tools/Test-VerticalCampaign.ps1` 复核后三关的形态、大招和结算；`Tools/Test-SideScroller.ps1` 复核横版操作与完整流程。所有报告必须对应同一个交付 EXE，失败记录保留。

### 本轮实际结果（2026-10-27）

交付 EXE 为 `C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261027_202840_130\MoriEchoes.exe`。Unity 6000.6.7f1 的真实 Windows x64 Development 构建成功，**0 错误 / 0 警告**。原始构建记录：`docs/BuildReports/Windows_20261027_202840_130.md`；日志：`Validation/SurfaceFeedbackBuild_20261027_2026.log`。

下列五组均在该 EXE 中实际运行，进程退出码均为 0，报告无错误；共 **2414 项运行检查通过**。共 148 张截图已检查非空白：145 张为 1280 × 720；装备测试还实际切换窗口尺寸，另有 1024 × 768、1600 × 720、1600 × 900 各 1 张。八关主材质合图另有图像检查。

| 测试 | 实际结果 | 报告（工程根目录下） |
|---|---|---|
| 平台、伤害、血条、红色预警、八关接触密室与三竖井物理攀升 | PASS，1258 项；32 张截图 | `TestResults/FeedbackTraversal_20261027_203021_529/feedback-traversal.json` |
| 横版操作、菜单、战斗、升级与流程 | PASS，132 项 | `TestResults/Side_1280x720_20261027_203021_598/stage-smoke.json` |
| 前五关敌兵与 Boss 三阶段 | PASS，360 项 | `TestResults/Encounter_1280x720_20261027_203021_696/stage-smoke.json` |
| 八角色、九武器与装备效果 | PASS，577 项 | `TestResults/Arsenal_1280x720_20261027_203440_642/stage-smoke.json` |
| 后三关 Boss 形态、大招、处决与结算 | PASS，87 项；15 张截图 | `TestResults/Vertical_20261027_204425_115/vertical-campaign-smoke.json` |

各报告旁的 `process-evidence.json` 记录同一 EXE 路径和退出码。平台专项逐个检查可站立结构的 Sprite 与所属关卡一致，电梯行程无旧短台，场内无旧常开门墙；可破坏掩体和油桶作为独立物品保留。三竖井的攀升专项实际射开全部 7 道闸门、越过第一波并在最终平台上落地，松开跳跃后持续确认支撑。它不是用坐标传送替代跳跃的流程测试。

源码检查另通过 10040 条模型断言、5000 次固定种子抽样、86 份脚本三套定义的语法检查、9 份 PowerShell 语法检查和 13 条导入安全检查，记录于 `Validation/source-validation.json`。这些检查不等于 Unity 编译；上面的真实 BuildPipeline 和 Player 记录独立提供编译与运行证据。

失败历史没有删除：早期平台探测曾把油桶误归为平台、旧流程机器人误入密室、竖井最终台阶胶囊越沿与排队跳跃检查曾失败，均已修复并重新构建。最终 EXE 的 `TestResults/Vertical_20261027_203440_730` 曾超时无报告，不能记为通过；该次与其他可见窗口测试并行，疑因失焦暂停，未将该推断当作已证实原因。单独重跑 `Vertical_20261027_204425_115` 已完整通过。最新有效交付只采用表中五份完整报告。

文件变更、哈希与运行证据索引：`Validation/FeedbackTraversal_20261027_Delivery/file-changes.json` / `.csv` 和 `delivery-verification.json`。启动器已指向本轮 EXE；旧程序仍可能在旧窗口内运行，应关闭旧窗口后重新启动。没有生成压缩包。

**尚未验证：** 真人默认难度完整通关、实体手柄长测、跨机性能与兼容性、Editor Play Mode，以及商业最终美术质量。本轮自动测试的无敌与伤害辅助不能代替这些验收。
