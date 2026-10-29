# 《三角洲契约 / DELTA PACT》2D 美术素材获取指南
### 给开发者/美术 sourcing 的合规来源推荐（配套 `CONTRA_SIDE_SCROLLER_REDESIGN.md` §4/§7）

> 当前 `ProceduralArt` 为程序化占位（203 行级）。接入正式素材时填 `GameConfig` 已预留的
> `playerSprite / playerBackSprite / enemySprites[5] / weaponSprites[3]` 槽位，`ProceduralArt` 保留为兜底。

---

## 0. 先定三件事（再花钱）
1. **风格统一**：整款游戏只选一种美术语言——推荐「高分辨率手绘扁平」或「精致像素(32/48px)」。
   混搭会直接破坏"精致感"，比素材精度更影响观感。
2. **商用授权**：确认允许**商业使用**、是否需署名(CC-BY)、是否禁止转售素材包本身。
3. **动画完整性**：优先选**带逐帧动画**（idle/run/jump/attack/hit/die）或**可切片精灵表**的包；
   纯单图需自行补帧，成本更高。

---

## 1. 免费 / CC0（先拿来跑通，零成本验证）
| 来源 | 特点 | 适用 |
|---|---|---|
| **Kenney.nl** | 100% CC0，包全（角色/敌人/地块/UI/特效），风格统一方块卡通 | 占位替换、UI、通用地块 |
| **OpenGameArt.org** | 社区 CC0/CC-BY，量大需筛授权 | 背景、音效补充 |
| **CraftPix.net（免费区）** | 免费 2D 角色/敌人/地块包，含部分动画 | 角色与敌人起步 |
| **PixelFrog（免费样例）** | 高质量像素，免费包可试水 | 角色参考 |
| **Untied Games** | CC0 像素角色/怪物，含动画 | 敌人图鉴 |

## 2. 付费高质（正式上线推荐，单价常 < ¥200）
| 来源 | 强项 | 备注 |
|---|---|---|
| **Unity Asset Store** | 搜索 `2D character / pixel side-scroller / environment tileset`；很多含动画+切片 | 与工程同生态，导入即用 |
| **itch.io / game-assets** | 独立美术师包，性价比高，主题多样 | 认准授权页（常 Royalty-Free 商用） |
| **PixelFrog（Adventure/Heartbeat 包）** | 顶级像素角色+动画，风格统一 | 付费但省大量补帧时间 |
| **LuizMelo** | 侧视角色/敌人/Boss 动画极全，多职业 | 横版动作首选 |
| **Ansimuz** | **主题环境包强**：城市/雪原/科幻/工业 | 正好对口本作 5 主题 |
| **Super Game Asset / GameDevMarket** | 打包地形+角色+特效全集 | 一次性补齐省力 |

## 3. 中文友好来源（注意授权清晰度）
- **爱给网 (aigei.com)**：音效/部分 2D 素材，免费+会员；商用前看清单条授权
- **包图网 / 千图网**：量大但**授权常模糊、不可商用或需单独购买**，不建议直接用于上线作品
- **B站 美术素材号 / 淘宝素材店**：沟通方便，但务必让卖家写明「可商用、不含第三方版权争议」

## 4. AI 生成（概念图/背景补位，慎用于动画精灵）
- **ImageGen（本环境可用）**：生成**角色概念图、关卡背景、UI 图标**作为精致占位，速度快
- 注意：AI 直接出**逐帧动画精灵表**不可靠（帧间不一致）；若要动画精灵，走
  Stable Diffusion + ControlNet（进阶）或仍采购美术师包更稳
- 建议用法：先用 AI 出**风格定调概念图**给美术/Codex 对齐方向，正式资源再采购

---

## 5. 针对本作 5 主题推荐组合（示例）
| 关卡主题 | 推荐来源 | 说明 |
|---|---|---|
| 城市废墟（教学） | Ansimuz 城市包 + Kenney 地块 | 灰蓝废墟、可破坏油桶 |
| 工厂蜂巢 | Ansimuz 工业 + LuizMelo 机械敌 | 传送带/液压柱 |
| 寒峰隧道 | Ansimuz 雪原 + 自制冰台 | 滑冰/暴风雪 |
| 未来都市 | Ansimuz 科幻霓虹 + PixelFrog | 垂直电梯/激光 |
| 最终要塞 | Ansimuz 要塞 + LuizMelo Boss 包 | 三形态 Boss |

**角色 4 名**：统一用 LuizMelo 或 PixelFrog 一套角色家族（换色+体型系数区分 VANGUARD/WRAITH/HAVOC），保证辨识度一致。

## 6. 接入要点（给 Codex）
- 采购包通常为精灵表(PNG) → 用 Unity Sprite Editor 切片为 `Sprite[]`，填 `GameConfig` 槽
- `ProceduralArt` 保留：当槽位为空时仍兜底生成，避免缺资源报错
- 动画优先用包内帧序列驱动 `CharacterVisual`（替代当前程序化骨骼摆动）
- 风格 LUT / 调色在 §7 后期 Volume 内按主题挂接

## 6.5 Kenney 具体包清单（免费 CC0，直接对应本项目）
> 以下均为 Kenney 站真实存在的 **2D** 包（带 "Kit" 的多为 3D，横版别下错）。全部免注册、CC0、可商用不署名。

**A. 核心横版美术（必下）**
- `Platformer Pack`（platformer-pack-redux，360 资产：角色/地块/敌人/道具，最完整横版基底）
- `1-Bit Platformer Pack`（400+ 单色精灵，风格绝对统一，最适合占位）
- `Pixel Platformer`（220，复古像素，备选风格）
- `Platformer Characters`（独立角色包）
- `Animated Characters`（含 3 套带动画皮肤 → 4 角色动画直接可用）
- `Platformer Assets: Extra Animations & Enemies`（敌人 + 动画，敌兵图鉴）

**B. 五主题扩展（按关卡）**
- 城市废墟：`Pico-8 City`（350+ 城市精灵）、`Platformer Assets: Buildings`
- 工厂：`Pixel Platformer: Industrial Expansion`、`Platformer Pack Industrial`、`Tiny Factory`
- 寒峰：`Platformer Assets: Ice`、`Tiny Ski`（100+ 雪地瓦片）
- 未来都市：`UI Pack - Sci-fi`、`Pico-8 City`、`Sci-Fi Sounds`
- 要塞/Boss：`Platformer Pack Medieval`、`Tiny Battle`（骑士/龙等战斗单位）

**C. 系统（UI / 准星 / 手柄 / 音效 / 特效）**
- UI：`UI Pack`（322 完整 UI）、`UI Pack - Sci-fi`（科幻风）、`UI Pack - Pixel Adventure`
- 准星/手柄：`Crosshair Pack`（瞄准镜/中心准星）、`Input Prompts`（1500+ 手柄/键盘图标，手柄 HUD）、`Cursor Pack`（180 光标）
- 音效：`Impact Sounds`（50 打击/命中）、`Sci-Fi Sounds`（激光/科幻）、`Interface Sounds`（UI）、`Digital Audio`（626 数字音效）、`Music Jingles`（35 循环曲）、`Voiceover Pack`（播报"WAVE 01"）
- 特效：`Particle Pack`（284 粒子 VFX）、`Explosion Pack`、`Smoke Particles`、`Splat Pack`
- 字体：`Kenney Fonts`（11 像素字体）

**D. 嫌一个个下麻烦**：itch.io 上 `Kenney Game Assets All-in-1`（6 万+ 资产，约 ¥80）一次性打包，等于付费支持作者换省事。

## 7. 授权红线（务必遵守）
- CC-BY 类**必须署名**（在游戏结尾 Credits 列出）
- 禁止把素材包**整体转售/再分发**（即使改了也常违规）
- 标 "Editorial / Non-commercial" 的**不可商用**，直接排除
- 中文站"会员可商用"多为网站授权，**不代表素材本身可商用**，以素材原作者的授权为准

---

## 8. 其他免费站（Kenney 之外，均 CC0 / 免费可商用）
> 下面每个站都给**当前真实存在的免费包名**，按本项目的「角色/主题/敌人/UI/音效」需求标注。

| 网站 | 最佳用途 | 具体免费包 / 入口 | 授权要点 |
|---|---|---|---|
| **OpenGameArt.org** | 角色/敌人/音乐/音效大库 | 搜 `Side Scrolling Character Art Collection`（含 Knight/Ninja/Zombie/Mage 等大量横版动画角色）；音乐搜 `Juhani Junkala`（CC0 曲包） | **逐条查授权**（CC0 / CC-BY / CC-BY-SA 混存），CC-BY 需署名、SA 需同协议开源 |
| **CraftPix.net/freebies** | 横版角色/地块/GUI | `Free Platformer Game Assets`、`Free Game GUI Pack`、各主题 tileset | 免费商用、**无需署名**、**禁止转售包本身** |
| **PixelFrog** | 高质量像素角色+动画 | 免费样例包（Adventure / Heartbeat 免费版） | 免费商用，**署名更佳**（非强制） |
| **Untied Games** | 像素角色/怪物（带动画） | 站内 CC0 角色与怪物包 | **CC0**，可商用不署名 |
| **Bevouliin.com/category/free_game_asset** | 敌人/怪物精灵表 | 大量免费 Flappy/Zombie/Skull/Monster 精灵表 | 免费商用，署名更佳 |
| **itch.io/game-assets/free** | 独立美术师包（筛选 Free） | 搜索 `platformer` / `pixel` / `sci-fi`，勾选 Price=Free | **逐包看授权页**（多为 Royalty-Free 商用） |
| **GameArt2D.com/freebies** | 角色/地块/GUI | `Free Game GUI Pack`、Forest/Water tileset、Knights sprite | 免费商用、无需署名 |
| **Quaternius.com** | CC0 低多边形（若未来转 3D） | 全站 CC0 角色/环境包 | **CC0** |
| **Game-icons.net** | UI / 技能图标 | 数千游戏图标 | **CC BY 3.0，必须署名** |

**音频专项（本作枪声/命中/UI/战斗曲）**：OpenGameArt（Juhani Junkala 包）、FreeSound.org（CC0/CC-BY，注意署名）、Pixabay Music / Mixkit（CC0 免署名）。Kenney 的 `Impact Sounds / Sci-Fi Sounds / Music Jingles`（见 §6.5）已覆盖基础战斗音效。

**避坑提醒**：OpenGameArt 与 itch.io 的授权**每件独立**，下载前务必看该资源页的 License 字段；CC-BY-SA / GPL 类会要求你的衍生作品同协议开源，**商用闭源游戏慎用**。

---

## 9. 高端素材方向（目标：对标《和平精英》级精致度）
> 用户目标不是免费占位，而是**高级、精致、接近《和平精英》观感**的素材。
> **现实前提**：《和平精英》是 **3D 写实军事风**（3D 建模 + PBR + 实时光照 + 影视级后期），
> 而本工程当前是 **2D 正交管线**——2D 精灵再精致，审美天花板与"3D 写实"是两条路，无法像素级还原真实枪械/人物质感。
> 因此"高级素材"分两条路线，需在立项时二选一：

### 路线 A：高级 2D（stylized 精致，非写实，但可做到"高级感"）
适合继续用现有 2D 管线，靠美术质量 + 2.5D 光照/后期拉高观感。

#### 路线 A 具体采购清单（按本作拆解；价格为近似参考，以商店当时为准）
**角色（4 名可选 + 主角，侧视动画是核心）**
- **LuizMelo（itch.io）**：`Soldier / SWAT / Mercenary` 系列侧视动画角色（idle/run/jump/shoot/hit/die 齐全），正对"三角洲契约"军事 operator 主题；另有 `Robot / Demon / Monster` 敌兵包与 `Boss` 包。单价约 $5–$15
- **Penusbmic（itch.io）**：`Sci-fi / Modern` 动画角色包，帧数与细节更高，做精致主角首选。约 $10–$25
- **Ghostpixxells（itch.io）**：精细像素英雄/敌人，层次丰富，走"高级像素"路线。约 $5–$15
- **PixelFrog 付费包**（Adventure）：顶级像素 + 完整动画（偏 RPG，可作备选风格）

**场景 / 主题环境（直接对口 5 关）**
- **Ansimuz（itch.io）**：`Sunny Land`（自然/森林）、`Industrial`（工厂）、`Snowy/Winter`（寒峰）、`Cyberpunk/Urban`（未来都市）、`Castle/Warrior`（要塞）。每套含瓦片+道具+视差层，单价约 $5–$20
- 城市废墟：Ansimuz `Urban`/`Cyberpunk` + 自搭废墟修饰

**敌人图鉴（按主题复用换色）**
- 机械/未来敌：`LuizMelo Robot/Demon`、`Penusbmic Sci-fi enemies`
- 雪原/要塞敌：通用敌包换色 + 主题环境点缀

**UI / HUD（高级战术风）**
- **Unity Asset Store** 搜 `Sci-fi UI` / `Tactical UI` / `Game UI`（按评分排序）；或 **CraftPix 付费 GUI**、**Super Game Asset UI**

**武器 / 特效**
- 武器侧视精灵：**CraftPix** 武器包、**LuizMelo** 武器包
- 特效：先用 Kenney `Particle Pack`（免费），升级用付费 `Game VFX` / `Juice VFX` 包

**音频（高级战斗听感）**
- **Sonniss Game Audio**（电影级音效库，含免费段）、**Unity Asset Store** `Cinematic Game Sounds` / `Weapon Sounds` 包、**Ghosthack** 战斗音乐包

**预算合计（路线 A 高端 2D 整套）**：约 **$300–$800**（4 角色 + 5 主题环境 + 敌兵 + Boss + UI + 音效）

> 采购顺序建议：先买 **LuizMelo 角色 + Ansimuz 一关环境** 验证风格统一度，再补齐其余 4 关与 UI/音效。
> 接入方式不变：填 `GameConfig` 的 `playerSprite/playerBackSprite/enemySprites[5]/weaponSprites[3]` 槽位，`ProceduralArt` 保留兜底。

### 路线 B：真·3D 写实（真正接近《和平精英》观感，需整体转 3D 管线）
即 redesign 文档 §3 标注的"真三维第一人称未实现"——需把工程从 2D 正交转 3D：
- **Synty Studios（POLYGON 系列，Unity Asset Store / syntystudios.com）**：低多边形但干净、移动端友好、有"高级感"，最现实的 3D 起步（非写实但专业）
- **Quaternius（全 CC0 3D）**：风格化 3D 角色/环境，免费可商用
- **写实军事 3D**：TurboSquid / Sketchfab（付费，需确认游戏商用授权）或**委托美术**产出定制枪械/角色
- ⚠️ 转 3D 是架构级改动（模型导入、第三人称控制器、第一人称手臂/相机、碰撞从 2D 改 3D），工作量≈重做一半，且需配合本机 GPU 性能验收

### AI 生成（概念定调 / 部分生产）
- **ImageGen（本环境可用）/ Midjourney**：生成**角色概念图、关卡氛围图、UI 视觉定调**，先锁定"高级感"方向再采购/委托
- 限制：AI 直接出**逐帧动画精灵或一致角色多视角**不可靠，正式角色仍建议采购或美术师制作
- 用法建议：先用 AI 出 1 套"目标视觉参考图"给美术/Codex 对齐，再走路线 A 或 B

### 成本现实
- 路线 A 高端 2D 合集：单包常 $20–$200，整套 5 关 + 4 角色约 $300–$800
- 路线 B 3D：Synty 单包 ~$50–$150；写实委托按角色/场景计费，单价高且周期长
- 替代：招聘/外包一名 2D/3D 美术按项目报价（最可控但需预算）

> 决策建议：若"高级感"指**精致但不必写实**，走路线 A + 2.5D 光照后期，性价比最高、无需改架构；
> 若必须**写实军事风（真·和平精英）**，只能路线 B，并接受转 3D 的架构成本。
