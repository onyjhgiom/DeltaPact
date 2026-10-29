# Codex 分阶段任务清单

## 阶段 0：检查环境与创建骨架
目标：确认 Unity 项目、包和文件夹。
- 检查当前目录是否为 Unity 项目根目录（是否有 Assets、Packages、ProjectSettings）。
- 不存在时，不要假装已创建 Unity 项目；先说明用户需要在 Unity Hub 创建 Universal 2D 项目。
- 创建本项目约定的 Assets 子目录与 Docs。
- 检查 Input System 是否安装，若未安装则给出 Package Manager 操作说明。
- 输出检查报告，不要继续实现所有系统。

## 阶段 1：玩家移动与瞄准
目标：可运行的灰盒场景。
- 创建 PlayerController.cs。
- WASD 移动，鼠标瞄准。
- 俯视角正交相机跟随玩家。
- 使用 Rigidbody2D 时采用正确的 FixedUpdate/MovePosition 或速度控制，不要同时混用多个移动方式。
- 添加 Inspector 组件清单与测试步骤。
- 暂时不需要角色动画和敌人。

## 阶段 2：射击与目标受伤
目标：能向鼠标方向发射子弹并造成伤害。
- WeaponController.cs、Projectile.cs、Health.cs（或明确的 Damageable 组件）。
- 子弹预制体、碰撞层、触发器设置。
- 开火间隔和伤害先写可调参数。
- 先使用圆形占位 Sprite。
- 验收：每次点击只发射一颗子弹，子弹方向正确，目标生命值递减，目标死亡后正确销毁或禁用。

## 阶段 3：敌人 AI 与玩家生命
目标：基本战斗闭环。
- EnemyAI.cs：追击、攻击距离判断、攻击冷却。
- PlayerHealth.cs：受伤、短暂无敌（可选）、死亡事件。
- 敌人攻击必须有冷却，不能每帧造成伤害。
- 验收：敌人能追击并攻击玩家；玩家可死亡并触发失败 UI。

## 阶段 4：经验与随机升级
目标：肉鸽核心。
- ExperiencePickup.cs、ExperienceManager.cs、UpgradeData.cs、UpgradeManager.cs。
- 使用 ScriptableObject 存储升级名称、描述、稀有度、升级类型和值。
- 升级时暂停敌人和玩家行为；选择后恢复时间。
- 验收：经验达到阈值时显示 3 个不重复升级，选择后实际改变属性。

## 阶段 5：波次、结算与重开
目标：一局游戏可完整结束。
- WaveManager.cs、GameManager.cs、ResultPanel UI。
- 固定波次与固定 Boss 触发，首版不做随机地图。
- 验收：完成最后波次后胜利；玩家死亡后失败；重开会重置玩家、敌人、经验和升级状态。

## 阶段 6：美术资产接入
目标：用正式素材替换占位图。
- 读取 Docs/ASSET_PROMPTS.md。
- 先接入主角静态 Sprite、1 把枪、1 种敌人和地面 Tilemap。
- 设置 Sprite Mode、Pixels Per Unit、Filter Mode、Compression。
- 任何 Sprite Sheet 都必须核实切片尺寸与帧顺序。
- 验收：素材方向一致、透明边缘正常、角色与子弹仍清晰可辨。

## 阶段 7：扩展武器与技能组合
目标：从 AR-07 扩展到 SG-12、SR-09。
- WeaponData ScriptableObject。
- 武器逻辑与武器数据分离。
- 增加火焰、电磁、冰霜的简单组合效果。
- 验收：不同武器有明显差异，升级能影响相应武器，切枪无异常。

## 阶段 8：优化与构建
目标：Windows 可交付测试包。
- 对高频生成的子弹和特效使用对象池（如果 profiling 证明有必要）。
- 修复控制台错误与关键警告。
- 检查不同分辨率、窗口模式、暂停、重开。
- 构建 Windows x86_64 测试包。
- 不宣称已在实际机器验证，除非确实完成运行测试。
