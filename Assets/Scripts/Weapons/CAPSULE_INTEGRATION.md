# 独立即时强化胶囊接线

本轮仅编辑 `Assets/Scripts/Weapons`，不连接经验条，不改各角色 Q。`WeaponController.Initialize` 自动创建、初始化同角色的 `CombatPowerup`。胶囊导演和击杀回调需由主工程接线。

## 必需接口

请在共享 `CharacterDefinition` 增加字段：

```csharp
public SideSpriteAnimation poweredAnimation;
```

`CombatPowerup.Animation` 读取当前 `game.Character.poweredAnimation`；字段可以为 null。动画接线根据 `IsActive` 选用该动画，且应检查 `Animation != null && Animation.IsReady`；缺少美术时保留原人物，现有轮廓、光环与能量甲仍生效。实现不按角色编号分支、不限制角色数量。该字段接入前，直接编译会报告缺少字段。

`EnemyAI`、每关敌兵列表和敌兵美术由其它工作接入。所有小兵共用位置/类型回调，无需为各角色、各关增加掉落数组。

## 最小接线

玩家 `Initialize` 完成、`game.Level` 已赋值后创建导演，必须挂在 `game.Level.gameObject` 上（当前工程也就是 Run 根），以匹配敌兵 agent 的 `GetComponent` 查询。保留引用供清理调用；不要在早于玩家创建的 `LevelManager.Initialize` 内立即初始化导演。

```csharp
WeaponCapsuleDirector capsules = game.Level.GetComponent<WeaponCapsuleDirector>();
if (capsules == null) capsules = game.Level.gameObject.AddComponent<WeaponCapsuleDirector>();
capsules.Initialize(game);
```

`WeaponController.Initialize(game, player)` 已自动执行 `CombatPowerup` 的创建和初始化，主 agent 不要再手动添加或重复初始化它。动画和 HUD 直接读取 `game.Player.Weapon.Powerup`。顶视模式没有 `Level` 时，可将导演挂到玩家的 Run 父节点，击杀处需持有该导演引用。

导演默认在可操作战斗开始 2 秒后投放首个 S，之后每 18 秒循环 S/R/L/B。胶囊从目标上方 5.5 单位可见下落，显示字母、现有枪械图和降落伞；侧视会停靠非角色地形，跟随升降台。拾取仅检查人物碰撞体附近 0.4 单位，不远程吸取。

敌兵接线的正式接口是 `public bool OnEnemyKilled(EnemyAI enemy)`，返回是否实际生成胶囊。在真实战斗死亡回调中，每个敌兵仅调用一次，放在敌人禁用/销毁之前，且此时 `enemy.Health.IsDead` 应为 true；关卡清理、卸载和检查点回收不能当作击杀：

```csharp
game.Level?.GetComponent<WeaponCapsuleDirector>()?.OnEnemyKilled(this);
```

也可解耦敌兵组件：

```csharp
capsules.OnEnemyKilled(deathPosition, enemyKind);
```

导演排除 `EnemyKind.Boss`，其余类型均可参加概率/计数掉落，兼容新增敌兵类型。默认 18% 概率、累计第 6 个小兵保底、掉落间隔 1.5 秒。池满或间隔未过时保留计数，下次有效击杀继续尝试；保底只在实际生成后重置，不承诺同一帧的多杀都生成。

## 形态与恢复规则

| 胶囊 | 时长 | 临时枪械（不足时回退） | 伤害/射速倍率 | 弹道 |
| --- | --- | --- | --- | --- |
| S 散弹爆发 | 12 秒 | 1 号霰弹（回退到可用枪） | ×2.4 / ×2.8 | 9 发均匀扇面，40° |
| R 双路速射 | 10 秒 | 3 号冲锋枪（回退 0） | ×1.3 / ×1.6 | 双路，6° |
| L 贯穿光束 | 12 秒 | 4 号轨道枪（回退 2） | ×1.5 / ×1.8 | 单发直射，额外贯穿 4 个目标 |
| B 三联爆破 | 10 秒 | 5 号榴弹（回退 0） | ×1.5 / ×1.6 | 三联，14°，爆炸半径 3.2 |

枪械编号从 0 开始。倍率乘在原枪与现有永久属性、暴击、专精、Q 射速之上，不修改共享武器定义或 Q 状态。S/R/L 禁用原枪爆炸半径，B 使用表中的半径。弹数最多等于项目配置的子弹池容量；当前空闲子弹不足整组时本次不发射、不消耗射击冷却。

- 拾取立刻换枪，清除临时射击冷却和装填等待；能量形态全程不耗弹、不自动补弹，忽略普通换枪/装填输入。
- 首次强化保存原选枪、射击冷却、剩余装填时间；原弹药数组保持原值，期间暂停原装填进度。
- 同种重拾刷新到完整时长；异种立即替换并使用新时长；均不叠加时长/倍率，也不覆盖首次的恢复点。
- 到期或 `ClearPowerup()` 恢复首次原枪、冷却和装填进度；不会赠送子弹。`RestoreAmmo()` 先退出强化，再执行原检查点恢复，不把能量形态写进存档。
- 暂停/升级界面/零时间倍率冻结形态、下落、寿命、掉落间隔和补给计时；暂停时保留能量视觉。
- 死亡、胜负、菜单、禁用时清理；关卡实例/关卡编号变化、`Level.Deaths` 变化时清理。检查点重试会通过 `RestoreAmmo()` 立即取消形态，导演也会在下次更新感知重试。

## 动画与 HUD

```csharp
CombatPowerup powerup = game.Player.Weapon.Powerup;
bool active = powerup.IsActive;
Color tint = powerup.Tint;
float remaining = powerup.Remaining;
float fraction = powerup.Fraction;
SideSpriteAnimation animation = powerup.Animation;
```

另有 `Kind`、`Profile.Code`、`Profile.Name`、`Duration`、`UsesEnergyAmmo`、`Changed` 事件。`Changed` 在激活、刷新、替换、到期和主动清理时触发，HUD 时间每帧读取 `Remaining/Fraction`。基础 `Ammo/Capacity` 继续表示实际弹药，HUD 可在 `weapon.UsesEnergyAmmo` 时显示 `∞`。无须访问或修改 Q 的剩余时间。

持续视觉复用 `game.Art` 的 Ring/Glow/Diamond 和现有角色 sprite，带呼吸、尾段闪烁、能量甲片和放大轮廓；第一人称瞄准隐藏身体能量甲，不影响接口。不会更改角色原渲染器颜色或素材。

## 手动补给、清理与事件

```csharp
capsules.SpawnSupply(new Vector2(20, 1), CombatPowerupKind.Spread);
capsules.Drop(deathPosition, CombatPowerupKind.Pierce);
game.Player.Weapon.TryApplyPowerup(CombatPowerupKind.Rapid);
game.Player.Weapon.ClearPowerup();
capsules.ClearAll();
```

`SpawnSupply` 的位置是落点参考，上方生成；侧视最终停靠实际地形。`Drop` 在给定位置轻微向上抛出。手动生成和激活均只在活跃、可操作战斗中接受请求。`TryApplyPowerup` 可传正数 duration 覆盖默认秒数；默认 0 使用表中时长。

生命周期接线：

- 暂停/升级界面：沿用 `game.State` 与 `Time.timeScale` 即自动冻结，无须新增暂停 API；不要调用 `ClearAll()`。恢复时无需再次 `Initialize`，保留剩余时长。
- 重试/切关/回主菜单：在原清理或重试逻辑之前调用 `game.Level?.GetComponent<WeaponCapsuleDirector>()?.ClearAll()`，同时回收胶囊、取消强化并重置掉落计数/补给计划；重试后无需重新初始化同一导演。
- 只取消形态：`game.Player?.Weapon?.ClearPowerup()`；只回收胶囊、不取消形态：`capsules.ClearAll(false)`。
- 新建 Level/Run：按最小接线在新的 Level 对象上创建、初始化一次导演；旧对象禁用/销毁有幂等兜底清理。

导演本身不改 GameManager、LevelManager、EnemyAI、HUD 或 SideSpriteAnimator。

`Spawned(WeaponCapsule)`、`Collected(CombatPowerupKind)` 可用于提示；池大小默认固定 8，最多 32，初始化后不扩容，不回收正在场上的胶囊，满池返回 false。单胶囊默认 24 秒寿命。全部运行时对象归属 Run/玩家节点，无静态订阅、常驻节点、协程或新材质；组件销毁也销毁自己的视觉/池根节点。

## 离线规则检查

`CombatPowerupRules.cs` 不依赖 Unity，包含形态参数、对称弹道、替换/到期计时和击杀掉落计数。`CombatPowerupRulesChecks.cs` 仅在 `MORIECHOES_CAPSULE_RULES_CHECKS` 定义下编译，可与该规则文件一起用 C# 9 编译成普通控制台程序执行，无需启动 Unity。

已运行 76 条纯规则断言并全部通过；已有离线验证器的三组宏定义 C# 9 语法检查通过。最终刷新源码列表后，使用 334 个现有程序集引用完成当前 56 个运行时 C# 文件的离线类型编译，结果通过；未调用 Unity 编辑器编译或运行游戏。本轮不启动 Unity、不运行游戏/Play Mode、不改素材、场景、共享脚本和自动生成的工程文件。接线后仍需主工程验证拾取手感、地形落点、动画切换、伤害与视觉效果。
