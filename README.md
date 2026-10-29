# 末日回响 / MoriEchoes

Unity 6000.6.7f1 / C# / URP 2D / Input System 的离线单机横版闯关射击 Roguelite。当前实际配置为 **8 名角色、9 把武器、10 关战役**。前五关横向推进，第六至八关沿既有 Ascent 规则向上推进，第九关海面游泳，第十关水下浮沉；早期向下推进资料保留为历史设计。

双击 `PLAY_MoriEchoes.cmd`。本轮已验证的 Windows 构建：`Builds/Windows/MoriEchoes_20261028_171506_913/MoriEchoes.exe`。本轮未生成 ZIP，旧构建和旧分享包保留；它们不代表当前版本。

- 操作和启动：`HOW_TO_PLAY.md`。
- 最新水关正式素材、十关各 3 招新增技能、Inspector 与运行证据：`docs/SWIM_ART_AND_BOSS_TECHNIQUES_ACCEPTANCE.md`；30 招名称、释放方式与视觉说明：`docs/BOSS_TECHNIQUE_DESIGN.md`。
- 首页人物选择现在常驻显示 Q/F/C 三招和 X 终结技的名称、效果、冷却/能量条件。八人保留原配置，切换卡片立即更新。菜单与新水关的具体路径、Inspector、复测步骤和限制：`docs/SWIM_AND_CHARACTER_MENU_ACCEPTANCE.md`。
- 上一轮平台统一、伤害调整、受击反馈、Boss 红色预警、中段密室和竖井路线验收：`docs/FEEDBACK_AND_TRAVERSAL_ACCEPTANCE.md`。
- 新美术、Inspector、难度规则、测试与限制：`docs/PREMIUM_ART_ACCEPTANCE.md`。
- 本轮运行报告与文件变更：`Validation/SwimTechnique_20261028_Delivery/delivery-verification.json`、`file-changes.json` / `.csv`；历史美术交付保留在原目录。
- 本轮 24 张 image 原图：`output/imagegen/swim-anime-v1/`、`output/imagegen/boss-techniques-v1/`；已绑定 146 张 Sprite、60 张辅助法线：`Assets/Art/Production/SwimAnimeV2/`、`Assets/Art/Production/BossTechniquesV1/`。两目录有预览图与 SHA-256 清单，提示词位于对应 `ArtProduction` 子目录。
- 56 张 image 接口原图：`output/imagegen/descent-premium-v1`；最终 313 个 Sprite / 234 张加工法线：`Assets/Art/Production/PremiumV1`。
- Boss 追加 16 张设计原图：`output/imagegen/boss-ascension-v2`；96 个实体部件 / 32 个大招帧 / 96 张法线：`Assets/Art/Production/BossAscensionV2`。防御及验收：`docs/BOSS_ASCENSION_ACCEPTANCE.md`。
- 提示词与美术预览：`ArtProduction/descent-premium-v1/prompts`、`review`。
- 进度：`docs/PROGRESS.md`；当前竖井规格：`docs/ASCENT_LEVELS_DESIGN.md`。

可选角色固定为 DELTA、VANGUARD、WRAITH、HAVOC、KALT、VEIL、SERA、ROOK。保留原四人的形象和配置；移出名单的旧角色素材保留。每人 Q/F/C 三招与 X 充能终结技，九种武器含激光刃。前五关新增敌兵、机关、实体模块、守卫、Boss 后两阶段和攻击效果已接入生成美术；后三关与新增角色/武器也有专属生成素材。

保留敌方机动与 Boss 大范围攻击，按最新要求将伤害调为中上强度：普通敌人、机关、Boss 常规技和大招的单次伤害分别最多为基础生命的 24%、28%、36%、45%，并且最多扣当前角色最大生命的 55%；护盾和减伤在上限之后生效。普通攻击不会从满血一击毙命，受击后有 0.6 秒保护。低血量仍可能被击败，坠出场景仍按失败处理。Boss 蓄力显示半透明红色落点/路径，其他旧彩色预判线、方块和倒计时保持隐藏。能量胶囊同武器基础总弹丸伤害仍只提高 8%～10%。Tab 切换横版远景、第三人称近景、第一人称武器前景瞄准；均为 2D 正交游戏。

Boss 为高防御版本：生命 ×1.5、减伤 62%、单次最多扣总血量 0.8%。三阶段换装甲、冠饰和背部装备；常规技能/终极技能伤害乘区 1.5/1.9，范围长宽乘区 1.65/1.9，实际承伤仍受上述玩家上限约束。低血量处决保留。

前八关平台、墙体、升降梯和密道沿用本关主地面的现有生成插画：废墟混凝土、工厂管线、寒峰冰岩、都市蓝色合金、要塞红黑装甲、墓窟雕石、圣堂蓝金石材、核心井紫色结构。移除电梯通道中的中间短台及一直开启的多余门墙。前八关每关仅一处中段密室，身体接触门进入；进入后主路暂停，击败独立守卫后接触出口返回。第九、十关没有隐藏通道。十关均可从菜单直接选择，仍记录真实通关进度。

海面关支持浅潜和跃出，新增 12 组、24 块随波浮动的高低浮台，可连续跳跃及 S+Space 下穿；水下支持全向加速/阻尼、深度编队、氧气与水压伤害、热泉补氧和悬浮胶囊。两关专属背景、小兵、Boss 三形态、实体部件、环境与特效已绑定本轮 image 生成素材。光柱/气泡只在相机附近更新，特效和弹幕使用有限池；仍保留少量程序化水面/灯光辅助效果。

十关 Boss 各新增 3 招独立技能，随三个形态逐步解锁，每两次原有攻击后穿插一次。每招有独立名称、释放/命中插画、轨迹与闪避方式。红色预警表示固定落点或完整行进路径，新增技能不会追随角色位移，也不冻结/强吸附玩家。原有追踪预警提前固定落点，水关保留 1.15 秒逃离时间。高光控制集中在技能核心和边缘，实际画面仍需按个人设备和偏好复核。

工程入口：`Assets/Scenes/MainMenu/MainMenu.unity`，GameBootstrap 绑定 `Assets/Settings/MoriEchoesConfig.asset`。依赖固定 Input System 1.20.0、URP 17.6.0、UGUI 2.6.0。

本轮构建 0 错误/0 警告，同一最终 EXE 的四组 Player 回归共 1091 项通过；具体证据见本轮验收文档。测试采用合成输入、无敌/伤害/位置辅助，30 招专项设置形态/出招计数以覆盖全部技能；竖井连续攀升专项使用真实跳跃输入。人工难度、实体手柄长测、Editor Play Mode 和商业最终美术认证尚未验证；跨机测试按用户要求取消。历史交付报告仅描述当时的构建。
