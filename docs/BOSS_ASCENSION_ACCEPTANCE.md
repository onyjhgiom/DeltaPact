# Boss 实体形态、高强度大招与防御调整 · 2026-10-27

## 实际文件与素材

工程：`C:\UnityProjects\MoriEchoes`。本轮不创建 ZIP。

- 实际生成的 16 张设计原图：`output/imagegen/boss-ascension-v2`，8 张 4×3 装备板、8 张 2×2 大招效果板。
- 提示词：`ArtProduction/boss-ascension-v2/prompts/boss-batch.jsonl` 和各主题文本；通过原装 imagegen CLI 请求用户指定网关的 `gpt-image-2.5`。网关内部模型无法独立核验。没有把密钥写入工程。
- 4 张原图有原生透明通道；12 张返回画出的棋盘格。`gpt-image-1.5` 编辑请求返回 404；一次 `gpt-image-2.5` 编辑虽返回图片，仍为 RGB，未用于游戏。最终使用原始设计图进行本地透明蒙版加工，保留原图及 `matted/matting-report.json`。不把棋盘格背景当透明图使用。
- 最终资源：`Assets/Art/Production/BossAscensionV2`，96 个实体装备部件、32 个大招动画帧、96 张从美术加工生成的法线贴图。`art-manifest.json` 记录源图、透明加工图及最终资源的 SHA256。
- 逐主题预览：`ArtProduction/boss-ascension-v2/review/*-review.jpg`。按透明空隙分割画格，避免固定等分裁掉装甲尖角和混入邻格碎片。
- 实际运行对照图：`ArtProduction/boss-ascension-v2/runtime-preview.jpg`，对应 JSON 记录原始截图路径和 SHA256；设计素材总览为同目录 `boss-ascension-preview.jpg`。
- 接入：`Assets/Editor/BossAscensionArtSetup.cs`、`Assets/Scripts/Level/ThemeProfile.cs`、`Assets/Scripts/Enemies/BossSignatureVfx.cs`、`VerticalBossController.cs`、`SideBossController.cs`、`Assets/Scripts/Core/CombatEffects.cs`。
- 防御：`Assets/Scripts/Combat/Health.cs`、`Assets/Scripts/Enemies/EnemyAI.cs`、`Assets/Scripts/Core/GameConfig.cs`。子弹、技能、激光刃的跳字使用减伤后的实际伤害。

## 八关差异化设计

| 主题 | 三阶段实体装饰 | 三阶段终极表现 |
|---|---|---|
| 城市废墟 | 陶瓷重甲、动力拳套、攻城炮、裂甲头冠 | 攻城装甲冲击与青色碎石爆裂 |
| 工厂蜂巢 | 液压钳、炉膛胸甲、熔核炮、排气烟囱 | 熔核螺旋喷射与燃烧碎屑 |
| 寒峰隧道 | 冰晶爪甲、晶体胸甲、寒羽背翼、冰角冠 | 极寒风切和冰晶暴风 |
| 未来都市 | 精密机械爪、光学胸甲、折叠镜翼、扫描头盔 | 紫青镜像裂爆 |
| 最终要塞 | 黑金刃甲、赤晶核心、金属翼、棘冠 | 猩红裂隙交叉爆发 |
| 废墟墓窟 | 骸骨爪、肋骨甲、石棺背架、牛骨冠 | 骨脉喷发与冥火魂潮 |
| 沉没圣堂 | 蟹钳、沉钟装甲、海螺压力器、珊瑚冠 | 深海横压与大范围回潮 |
| 星坠核心井 | 引力镰爪、奇点装甲、碎星翼、虚空冠 | 事件视界坍缩与星骸喷发 |

每个 Boss 每阶段绑定四个实体部件，生命跨阈值时换装；不再让整个 Boss 上下漂浮。前五关仅有小幅机械铰接动作，竖井实体装备固定挂接。大招在实际发招时播放，宽度 18～22 世界单位、4 帧/1.45 秒；其美术为表现层，伤害仍由该 Boss 的独立攻击机制结算，图像尺寸本身不是额外伤害盒。

## 数值与边界

| 参数 | 当前值 | 作用 |
|---|---:|---|
| Boss Damage Reduction | 0.62 | 所有常规玩家命中减伤 62% |
| Boss Maximum Hit Fraction | 0.008 | 单次命中最多扣最大生命的 0.8% |
| Boss Health Multiplier | 1.5 | 各关原 Boss 生命再乘 1.5 |
| Boss Skill Damage Multiplier | 2.25 | 常规 Boss 技能伤害乘区 |
| Boss Ultimate Damage Multiplier | 3.25 | 第三形态的第三招专属伤害乘区 |
| Boss Skill Area Scale | 1.65 | 横版技能伤害区长宽各乘 1.65 |
| Boss Ultimate Area Scale | 1.9 | 横版大招伤害区长宽各乘 1.9 |

前五关新兵难度 Boss 生命为 1350 / 1620 / 1890 / 2160 / 2430。10 点原始命中实际扣 3.8；100 点命中在第一关先减为 38，再受单次上限限制为 10.8。多弹丸按各自实际命中独立结算，并非每秒只许一次命中。普通减伤加血量使未触发上限时的理论耐久约为原来的 3.95 倍；真实战斗时长还受命中率、武器、成长、走位和阶段保护影响，未承诺固定通关时长。

技能伤害再乘关卡全局敌伤倍率与难度倍率。例如第一关新兵 20 基础技能伤害为 74.25，24 基础大招为 128.7（玩家减伤前）；高难度下可致命。横版覆盖面积约为旧基础尺寸的 2.72 / 3.61 倍。竖井 Boss 子弹附带实际爆炸半径：常规 0.9075、大招 1.71，伤害由中心到边缘衰减至 40%。

低血量处决保留。只有处决落幕与明确标记的自动测试阈值操作绕过 Boss 护甲，普通武器和角色大招均受减伤及单次上限约束。能量胶囊仍只增加对应武器基础总弹丸伤害 8%～10%，未用其抵消 Boss 防御。

红黄预判线、范围框、路径、倒计时和下一招攻略文字仍全部关闭；保留真实发招、命中、阶段变化与死亡效果。

## Inspector 与启动

1. Unity 6000.6.7f1 打开工程；场景 `Assets/Scenes/MainMenu/MainMenu.unity`，GameBootstrap 绑定 `Assets/Settings/MoriEchoesConfig.asset`。
2. Config 中保留 8 人/9 武器/8 Themes。每个 Theme 的 Boss Ornaments 为 12（阶段优先，每阶段爪甲/胸甲/背部武器/冠饰），Boss Ultimate Frames 为 4。
3. Config 的 Boss Ascension / Boss Armour 参数按上表核对。用户可在此统一调整，无需修改每个 Boss 脚本。
4. 新 Sprite 的 Type=Sprite、Single、Full Rect、Pivot=Center、PPU=256、Bilinear、Clamp、Mipmap 关闭、压缩关闭；装备附 `_NormalMap`，法线导入为 Normal map。
5. 需要重新绑定时退出 Play Mode，菜单 `Tools > Mori Echoes > Production Art > Apply Boss Ascension Art`。执行前自动备份 Config；不会重建角色、场景或整个战役。
6. 最终运行程序以 `PLAY_MoriEchoes.cmd` 和 `docs/PREMIUM_ART_ACCEPTANCE.md` 的最新验证路径为准。

## 测试与验收

- Encounter 组逐关用真实 Health.TakeDamage 验证 10→3.8、巨额伤害被限制为 0.8%，并核对血量放大。额外阶段跳转显式绕过护甲，不能当作实战击杀速度。
- 逐阶段验证四个实体部件、自然释放独立大招、预判图示不可见；保存各关 ultimate 截图。维持 Boss 本体垂直稳定、世界伤害区、锚点反制、处决与结算测试。
- 竖井组验证独立形态装备、伤害上限、自然大招、真实弹体伤害和半径，保存三张大招截图。
- 竖井镜头额外验证玩家脚部和 Boss 本体同时入镜；演出锁定持续 2.2 秒后高度漂移小于 0.05。墙面实体浮雕保持透明度，不再因场景反馈被刷成纯白骷髅或圆环。
- Arsenal / Art / 横版全流程另作回归，防止新防御影响武器或美术引用；实际记录汇入最终运行证据。
- 验收要求：Unity 构建成功，报告 passed=true、errors=[]、Player 退出码 0；截图无棋盘底、无裁断部件、形态可区分，处决后正常胜利。

尚未完成实测前不得标记通过。自动辅助测试不代表真人高难度通关、平衡体验、实体手柄长测或跨机性能。该素材为 AI 生成 2D 位图及有限帧动画，不等同高预算三维建模。

人工复核：从主菜单选择第一关和新兵难度，使用默认步枪攻击 Boss，观察小幅连续掉血和实际伤害跳字；分别压到两个换装阈值，确认爪甲、胸甲、背部武器、头冠确实变化。第三阶段等待终极招，观察实际攻击与大范围主题效果。最后进入处决窗口，确认演出后胜利。换至第六至八关检查竖井镜头和墙面浮雕。验收时记录真实用时、所选武器/角色/难度和死亡原因；此项尚未由真人完成。

## 实际结果

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
