# 正式美术接入与画面升级

## 当前状态与边界

当前真实存在的是程序化占位绘制代码，不是已经制作好的精美动漫 Sprite、3D 模型或动画资源。参考图是风格/规格图，没有被切成可用角色图集。`Art`、`Audio`、`Prefabs`、`Data` 空目录是预留空间，不代表资源已经完成。

`ProceduralArt.cs` 生成白发蓝眼轮廓、三枪共用基础枪形、五种敌人轮廓、暗色地砖、裂纹、碎石和边界。`CombatEffects.cs` 提供受限池化枪火、命中粒子、伤害数字、闪避残影、环形预警和冲锋预警。`GameAudio.cs` 合成短音效和低频氛围循环。所有这些都需要在真实 Unity 中检查视觉、听感及性能，不能标记成生产级美术完成。

本轮采用 URP 2D Unlit 确保灰盒可见，不静默修改目标工程 Renderer、Volume、HDR 或渲染设置。真正灯光、Bloom、色调、精细破败场景和完整角色动画属于后续美术集成，不靠参数命名假装完成。

## 来源与授权

1. 正式资产放在约定目录；保留原文件，不覆盖程序化代码或现有素材。
2. 每批资产建立同目录 `SOURCE.md`：作者、来源、授权范围、购买/许可凭据位置、修改日期、版本、是否允许随游戏分发。没有授权的图/音/字体不可进入发行构建。
3. 用户参考图只作风格参考；提示词不是已生成文件。未经确认不下载收费资源，不引入额外 SDK。
4. 程序化资产和合成音效由本项目代码实时产生，无外部素材下载。操作系统字体仅为临时回退，不代表字体再分发权已获得。

## 单帧资源的精确导入步骤

1. 先复制一份测试 Config，避免影响已验收配置。导入 PNG 到指定 `Assets/Art` 子目录。
2. Texture Type = `Sprite (2D and UI)`；单帧 Sprite Mode = `Single`；`Alpha Is Transparency` 开启；Filter Mode = `Bilinear`；Wrap Mode = `Clamp`；按原图大小设置 Max Size，不无意缩小。PPU 起点 100。
3. 应用后在 Sprite Editor 检查透明边距和 Pivot。当前角色绘制节点位于角色物理中心，**默认按 Center pivot 对齐**；如使用脚底 pivot，先在 CharacterVisual 增加一致的本地偏移再验收，不要假设脚底自动对齐。
4. 当前自定义角色与武器按 Transform Scale=1 绘制。例如希望角色约 1.4 世界单位高时，有效图像高度约 140 像素、PPU=100。若采用 512px 高素材，应以实际透明边距和目标身高重新计算 PPU，而不是直接塞入后产生巨人。
5. Play 前选中 Config 赋 Sprite 槽位。自定义素材保留原色；占位素材作为未填槽位的后备。

| 槽位 | 放置目录 | 顺序/约定 |
| --- | --- | --- |
| Player Sprite | `Assets/Art/Characters` | 成年白发蓝眼战术角色；单帧体态。也用于菜单、HUD 与残影 |
| Enemy Sprites[0..4] | `Assets/Art/Enemies` | Chaser、Gunner、Runner、Elite、Boss，数组长度 5 |
| Weapon Sprites[0..2] | `Assets/Art/Weapons` | AR、SG、SR；枪默认指向 +X，Pivot Center；枪节点本地 X=0.48 |
| 场景图块 | `Assets/Art/Environment` | 目前尚无 Tilemap 替换绑定，应新建美术场景层并保留运行边界碰撞 |
| VFX 图集 | `Assets/Art/VFX` | 目前基础图形由代码生成，正式特效需增加对应 Sprite/序列配置，不假装拖图即可自动接完 |
| UI | `Assets/Art/UI` | 当前程序化 UGUI；新图需显式接入 HUDController 或后续 Prefab 化 |
| BGM/SFX | `Assets/Audio/BGM`、`SFX` | 当前为合成音；正式音效需增加 AudioClip 配置并替换 GameAudio 绑定 |

不要直接用角色整张 Sprite Sheet 填入单帧槽位。多帧素材应设 Multiple 并按实际尺寸切片，不能套用参考图上的示意格。

## 动画与特效提升顺序

1. 先完成静态替换和碰撞/大小验收，再制作角色 Idle、Run、Shoot、Dodge、Hurt、Death 及敌人行为动画。
2. 当前 CharacterVisual 只有占位呼吸/移动摆动、受击闪色、翻转与血条，没有 Animator 或专用闪白 Shader。正式状态机需新增独立表现层，参数对接玩家移动、IsDodging、Health 事件、敌人状态。不要修改伤害逻辑来驱动动画。
3. 地面采用低对比灰蓝，敌弹/预警橙红，玩家弹青蓝，角色与敌人轮廓优先。在真实 1080p 图像上比较，不能只在 Inspector 放大预览。
4. 在真实 Universal 2D 工程中备份 Renderer/Volume 配置后单独试验 2D 灯光、低强度 Bloom 和色彩分级；任何性能开销都先 Profile。避免敌弹被 Bloom 淹没、预警和爆炸颜色相同、镜头持续抖动。
5. 背景、前景遮挡、法线、分层光照、制作级 UI 和精细角色不是本轮已有内容，需要素材与实际运行环境再推进。

## 验收与回退

- 对比 1920×1080、1280×720，截图记录开场、密集战斗、升级、Boss 预警和结算。
- 检查角色/枪朝向、武器锚点、透明边缘、受击闪色、角色与碰撞半径，Boss 实体要与危险范围一致。
- 验证自定义精灵在退出、重开、换枪、暂停后仍正确；中文 UI 和伤害数字无缺字。
- 逐批记录目标机 CPU/GPU、平均帧和长帧，不把对象池存在视作 60FPS 已通过。
- 回退时恢复 Config 的旧引用，程序化后备仍可用；不删除原素材或配置。

最终“高级精致”验收必须提供真实运行截图/视频、素材授权及目标机性能记录。目前均未执行，不作完成承诺。
