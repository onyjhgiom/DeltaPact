# GitHub 仓库曝光设置：Topics 与双语描述

> **状态：已通过 GitHub API 写入并核对生效（2026-11-28）。** Topics 15 个、中英双语描述、homepage 均已设置；下方清单保留作为记录与后续修改依据。
>
> GitHub 站内搜索与推荐很大程度依赖 Topics 和可双语理解的描述。以后要改：仓库主页 → 右侧 About 齿轮 ⚙ → 编辑描述与 Topics；或 `Settings → General`。

## 一、Topics（15 个，已写入）

```
unity
unity2d
csharp
gamedev
game-development
indie-game
roguelike
2d-game
side-scroller
platformer
shooter
action-game
boss-fight
pixel-art
game-art
```

说明：

- `unity` / `unity2d` / `csharp` 是技术栈搜索词，`gamedev` / `game-development` / `indie-game` 是开发者聚集的高流量 topic。
- `roguelite` 才是本项目准确类型；GitHub 里 `roguelike` 流量更大，两者一起用。
- `side-scroller` / `platformer` / `shooter` / `action-game` / `boss-fight` 对应实际玩法，不夸大。
- `pixel-art` / `game-art` 对应工程内的生成式 2D 精灵美术；如果后续觉得不够贴切，可换成 `2d-platformer` 或 `game-prototype`。
- GitHub 单仓库最多 20 个 topic，不必堆满；以上 15 个已经覆盖主要搜索入口。

## 二、仓库描述（About description）

**中文（当前，可保留）：**

```
三角洲契约 DELTA PACT —— 基于 Unity 的 2D 横版动作射击游戏。10 大战役涵盖横版闯关、竖版攀爬、海面游泳与水下作战，含 Boss 招式美术与波次战斗。
```

**英文（已追加到中文描述之后，用 ` | ` 分隔）：**

```
Unity 2D side-scrolling shooter roguelite. 10 stages: side-scrolling, shaft climbing, surface swimming and underwater combat. 8 characters, 9 weapons, 3-phase bosses with 30 unique skills. Windows x64 offline demo available.
```

中英合并后的完整一行（已生效，228 字符，在 350 字符限制内）：

```
三角洲契约 DELTA PACT —— 基于 Unity 的 2D 横版动作射击游戏。10 大战役涵盖横版闯关、竖版攀爬、海面游泳与水下作战，含 Boss 招式美术与波次战斗。 | Unity 2D side-scrolling shooter roguelite with 10 stages, 8 characters, 9 weapons and 3-phase bosses. Windows x64 offline demo available.
```

> GitHub About description 上限 350 字符，上面合并行在限制内。

## 三、Website（About 里的 homepage 字段）

当前已填 `https://github.com/onyjhgiom/DeltaPact`（仓库本身）。若后续做了 GitHub Pages 展示页，可换成 Pages 地址。

## 四、做完这一步之后

Topics 生效很快（几分钟内可被搜到）。真正的 star 仍来自「有人看到并愿意玩」，继续做：

1. `README_EN.md`：英文版 README（本仓库已添加，顶部可切换语言）。
2. `docs/STAR_GROWTH/02-github-release-guide.md`：把 406MB 试玩包挂成 Release。
3. `docs/STAR_GROWTH/03-promotion-kit.md`：V2EX / 掘金 / B站 / 知乎 / 小红书文案与发布节奏。
