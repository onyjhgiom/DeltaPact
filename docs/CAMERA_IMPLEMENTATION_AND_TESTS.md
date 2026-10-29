# 2.5D 越肩视角：实现、Inspector 与验收

工程：`C:\UnityProjects\MoriEchoes`。对应需求文档：`docs/CAMERA_2_5D_OVERSHOULDER_PLAN.md`，原方案保持不变。

## 实现范围

- 正交相机默认倾斜 28°，玩家处于画面下方，侧肩偏移与前导分别控制。按住右键或手柄 LT 平滑开镜；松开恢复。
- 鼠标水平移动旋转视角，中心准星对应 XY 地面的真实交点；手柄右摇杆保持世界平面方向瞄准。鼠标纵向不改变高度，因为玩法仍为 XY 平面的 2D 战斗。
- W/S 沿实际瞄准方向前后移动，A/D 沿其右向量横移；对角归一化，开镜减速，闪避继续使用原 Rigidbody2D。
- 三种枪分别注入后坐力，设累积上限和衰减，叠加原受击/射击抖动。角色、枪、敌人血条、伤害数字面向相机，阴影、弹道与地面预警留在 XY 平面。
- HUD 增加四段准星、中心点和透明中心的圆形开镜暗角。菜单、暂停、升级、结算均释放鼠标并隐藏瞄准 UI，清除开镜/后坐力。失焦自动暂停。
- 可选背面角色资源；未指定正式美术时使用程序化背面占位。原自定义角色、武器、敌人精灵引用继续保留。

## 对方案示例的必要修正

1. 倾斜相机下，固定深度 `ScreenToWorldPoint(pointer, 10)` 不代表 `z=0` 地面。实现使用 `ScreenPointToRay` 与 XY 平面求交，避免准星和命中位置偏离。
2. 相机使用角度制旋转，并从地面焦点沿相机法线后退 `24 + camHeight`，不会移到 `z=0` 地面上。`camHeight` 在本实现中表示额外镜头距离，不移动 2D 碰撞平面。
3. 中心准星配合相对鼠标转向；相机朝向与玩家到准星的射线分开，避免侧肩偏移反馈导致不操作也自转。D 使用正确的顺时针右向量。
4. 默认开启 28° 以直接体现 2.5D；`camPitchDeg=0` 可退回平面正交。正交投影不会产生透视消失点，本次没有将游戏改为全 3D。
5. 边界先按倾角、朝向、宽高比计算地面视口范围，再保证玩家可见且瞄准保持向前。靠近墙角朝外瞄准时优先保留玩家取景，允许看到竞技场墙外的延伸地面；不强行把准星夹到玩家身后。外围地面不增加可通行区域或碰撞体。

## 修改文件

| 文件（相对工程根目录） | 作用 |
| --- | --- |
| `Assets/Scripts/Core/CameraRig.cs` | 越肩取景、投影求交、视角转向、ADS、后坐力、边界与排序 |
| `Assets/Scripts/Core/GameConfig.cs` | 相机参数与有限值/范围验证 |
| `Assets/Settings/MoriEchoesConfig.asset` | 显式保存新相机参数，保留旧玩法数值与资源引用 |
| `Assets/Scripts/Core/InputReader.cs` | 鼠标相对位移、右键/LT 开镜 |
| `Assets/Scripts/Player/PlayerController.cs` | 中心瞄准、相对移动、ADS 移速 |
| `Assets/Scripts/Weapons/WeaponController.cs` | 武器后坐力与枪械广告牌显示 |
| `Assets/Scripts/Core/CharacterVisual.cs` | 角色/血条广告牌、背面精灵、随视角深度排序、地面阴影 |
| `Assets/Scripts/Core/ProceduralArt.cs` | 背面占位、暗角纹理、延伸地面；运行时纹理统一释放 |
| `Assets/Scripts/Core/CombatEffects.cs` | 朝向相机的闪避残影与伤害文字 |
| `Assets/Scripts/Core/GameManager.cs` | 各状态鼠标锁定/释放、失焦暂停、瞬时相机状态重置 |
| `Assets/Scripts/UI/HUDController.cs` | 中心准星、开镜暗角与操作提示 |
| `Assets/Scripts/Diagnostics/RuntimeSmokeTest.cs` | 扩充真实 Player 自动检查；仅显式诊断启动时运行 |

现有场景、AI、经验升级、波次、Boss 与工程管线继续使用。已有界面名称保持当前文件内容，不借本次相机优化改名。

## Unity Inspector 设置

1. 用 Unity Hub 打开 `C:\UnityProjects\MoriEchoes`，选择 **6000.6.7f1**。等导入/编译结束后，打开 `Assets/Scenes/MainMenu/MainMenu.unity`。
2. 在 Project 窗口选择 `Assets/Settings/MoriEchoesConfig.asset`，展开 **Camera / orthographic shoulder view**。相机运行时由 Bootstrap 创建，不需要额外挂 Cinemachine 或复制 Main Camera。
3. 以下为已写入资源的默认值；参数均经 `GameConfig.Validate` 校验。`camAdsSize <= camBaseSize` 且 `camLead - camBack >= 1`，其余按 Inspector 范围配置，拒绝 NaN/Infinity。

| Inspector 字段 | 默认值 | 用途 |
| --- | --- | --- |
| Cam Base Size / Cam Ads Size | 7.7 / 4.2 | 正交半高、开镜半高 |
| Cam Lead / Cam Back | 3.6 / 0.9 | 前导与后撤，差值控制前方留白 |
| Cam Shoulder Offset | 0.65 | 侧向肩位；负数换肩，0 居中 |
| Cam Pitch Deg / Cam Height | 28 / 0 | 倾角 0–55°；额外法线距离 0–12 |
| Cam Follow / Cam Zoom Speed | 9 / 12 | 跟随与缩放阻尼速率 |
| Cam Recoil Max / Cam Recoil Decay | 0.25 / 1.6 | 后坐力上限与每秒恢复量 |
| Cam Recoil Pitch | 0.4 | 后坐力附加倾角系数 |
| Cam Weapon Recoil | (0.045, 0.12, 0.18) | AR / SG / SR 单发冲击 |
| Mouse Look Sensitivity | 0.15 | 每像素水平转角，ADS 自动乘 0.55 |
| Ads Move Multiplier | 0.6 | 开镜移速倍率 |
| Player Back Sprite | None | 可选背面 Sprite，建议与正面使用相同单位尺寸、中心 Pivot |

4. 正式背面图在 Texture Importer 选择 **Sprite (2D and UI)**，设置与正面一致的 Pixels Per Unit 和 Pivot，再拖到 Player Back Sprite。没有正式资源可保持 None；不修改原 Player Sprite/Enemy Sprites/Weapon Sprites。
5. Play 后点“进入战区”。Game 视图获得焦点后鼠标锁定，左右移动转向；Esc 可释放。Play Mode 内调整不会自动写回资源，退出 Play 后再保存需要保留的数值。

## 测试步骤与验收标准

| 步骤 | 验收标准 |
| --- | --- |
| 默认配置开始游戏，不动鼠标 | 玩家位于画面下部，准星居中，视角静止时不自行转动 |
| 左右转向后按 W/S/A/D 与 W+D | W 朝瞄准方向，D 正确向右横移，对角速度不增加 |
| 按住右键，松开，再用手柄 LT | 平滑缩放到约 4.2 并恢复 7.7，暗角中心透明，移动减速 |
| 依次切换 1/2/3 开火并装填 | 准星方向弹丸能命中，三枪存在有限后坐力并恢复，原弹药/装填逻辑正常 |
| 分别设置倾角 0、28、55 | 中心屏幕射线在地面有交点，回投影误差 < 1 px；角色不随地面压扁；镜头不穿地 |
| 四角向外瞄准并开镜 | 玩家仍在画面中，准星不反向，延伸地面覆盖墙外，无未渲染空洞 |
| 暂停、升级、失败、胜利、返回菜单、重开 | 鼠标能点 UI，准星/暗角隐藏，ADS/后坐力清除，原游戏流程正常 |

自动化重跑（PowerShell）：`Tools/Test-WindowsBuild.ps1 -Executable <本次 Development EXE 完整路径>`。脚本为可见窗口测试，不要用 `-batchmode/-nographics` 代替视觉验收。诊断进程临时禁用其自身 Input System 中的真实鼠标/键盘/手柄，仅使用合成输入，结束后恢复；不禁用 Windows 设备。

## 实际验证记录

最终产物：`C:\UnityProjects\MoriEchoes\Builds\Windows\MoriEchoes_20261024_101932_609\MoriEchoes.exe`。双击工程根目录的 `PLAY_MoriEchoes.cmd` 启动此包，分发时复制整个构建文件夹。

- Unity **6000.6.7f1**，Windows x64 / Mono / Development，BuildPipeline **Succeeded，0 错误 / 0 警告**，耗时 16.685 秒。原始报告：`docs/BuildReports/Windows_20261024_101932_609.md`；编译/构建日志：`Logs/CameraBuild_20261024_final.log`。
- 三档可见窗口均实际运行 **94 项检查通过、0 运行错误、退出码 0**，请求与实际分辨率一致。汇总：`TestResults/latest-windows-smoke-summary.json`。
- 覆盖静止无自转、转向后 W/D 方向、对角速度、闪避、倾角 0/28/55 的地面求交/回投影/瞄准/广告牌/安全距离、鼠标与手柄 ADS、开镜减速、四角向外瞄准、后坐力封顶与自然衰减、三种武器命中与装填（含 SR 开镜命中）、经验/升级、暂停/恢复、敌人 AI、Boss、失败/重开/胜利/返回菜单。
- 独立源码校验另通过：25 个 C# × 3 宏组合无语法错误；纯 C# 模型 10,040 断言和 5,000 次种子抽样；13 项安全导入检查。静态检查本身不替代上面的 Unity 编译和 Player 运行。

| 分辨率 | 检查/错误 | JSON、Player.log 与 8 张实际截图所在目录 |
| --- | --- | --- |
| 1280×720 | 94 / 0 | `TestResults/Visible_1280x720_20261024_102042_075` |
| 1600×900 | 94 / 0 | `TestResults/Visible_1600x900_20261024_102125_011` |
| 1920×1080 | 94 / 0 | `TestResults/Visible_1920x1080_20261024_102206_930` |

截图包含菜单、ADS、边界 ADS、升级、战斗、失败、Boss 阶段和胜利。已抽查开镜、边界与战斗截图的取景和 UI；截图抽查不等同人工操控手感验收。

修改前备份：`Validation/CameraBackup_20261024_010538`。场景与用户原方案哈希未变；配置资源旧字段原值保留。差异盘点与 SHA256：`docs/CAMERA_CHANGE_MANIFEST.csv`，保留核验：`Validation/camera-preservation.json`。本次只修改真实主工程，OneDrive 的首次源码交接副本不作为当前运行入口。

失败历史同样保留，未记作通过：

- `TestResults/Visible_1280x720_20261024_011428_613`：SG 合成开火未消费弹药；`...011824_108`：鼠标接管 ADS 断言失败。两次失败点不一致，诊断中真实设备事件可能改变 `Mouse.current`；将测试进程内真实输入与合成设备隔离后重跑通过。没有改动正常游戏的输入设备启用状态。
- `TestResults/Visible_1280x720_20261024_012208_628`：报告时间从 01:22:12 跨到 02:23:55，超出 100 秒诊断时限，Boss 阶段超时。未将此轮当作有效通关；相同包之后三档各 91 项完整重跑通过。最终再增加 3 项检查并重新构建，得到上面的三档 94 项结果。

尚未验证：Unity Editor Play Mode、真人鼠标/实体手柄长时间手感、真实十波正常难度通关、失焦/多显示器的人工操作、音效听感、帧率/长时性能、跨电脑、非 16:9 分辨率。测试使用临时一波 5 秒配置，Boss 阶段有测试无敌和伤害 API，默认十波资产未改；不表示人工默认难度通关。角色、场景和特效仍为程序化占位，正式动漫素材、角色动画、精细 3D 建模与制作级后处理尚未完成。
