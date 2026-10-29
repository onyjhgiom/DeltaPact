"""Finalize local delivery only after all named Windows runtime reports passed."""
import argparse
import csv
import hashlib
import json
import shutil
from datetime import datetime
from pathlib import Path

project = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--build', required=True)
for name in ('arsenal', 'encounter', 'art', 'campaign', 'vertical'):
    parser.add_argument('--' + name, required=True)
args = parser.parse_args()
build = Path(args.build).resolve()
assert build.is_relative_to(project / 'Builds/Windows') and build.is_file()
delivery = project / 'ArtProduction/descent-premium-v1'
backup = project / 'Validation/PremiumFinalization_20261027/DeliveryDocs'
stamp = datetime.now().astimezone().isoformat()

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def save(relative, content):
    path = project / relative
    preserved = backup / relative
    if path.exists() and not preserved.exists():
        preserved.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, preserved)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding='utf-8', newline='\n')

tests = []
source_report_path = project / 'Validation/source-validation.json'
source_report = json.loads(source_report_path.read_text(encoding='utf-8-sig'))
assert 'PASS: 10040 assertions' in source_report['pureModelTest']
assert '0 syntax errors' in source_report['syntaxCheck']
assert 'PASS: 13 import safety checks' in source_report['importSafety']
for source in source_report['sourceHashes']:
    assert sha(project / source['path']).lower() == source['sha256'].lower(), source['path']
for name in ('arsenal', 'encounter', 'art', 'campaign', 'vertical'):
    report_path = Path(getattr(args, name)).resolve()
    report = json.loads(report_path.read_text(encoding='utf-8-sig'))
    assert report['passed'] and not report['errors'], (name, report_path)
    process_path = report_path.parent / 'process-evidence.json'
    process = json.loads(process_path.read_text(encoding='utf-8-sig'))
    assert process['ExitCode'] == 0 and Path(process['Executable']).resolve() == build, name
    log = (report_path.parent / 'Player.log').read_text(encoding='utf-8', errors='replace')
    assert str(build).replace('\\', '/').split('/')[-2] in log, 'Report must name the delivered build: ' + name
    tests.append(dict(name=name, passed=True, checks=len(report['checks']), errors=0,
                      report=str(report_path), reportSha256=sha(report_path),
                      exitCode=process['ExitCode'], processEvidence=str(process_path),
                      scope=report.get('scope', 'Assisted vertical runtime checks; not human balance/controller acceptance.')))

manifest_path = project / 'Assets/Art/Production/PremiumV1/art-manifest.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
for entry in manifest['assets']:
    assert sha(manifest_path.parent / entry['path']) == entry['sha256']
    if entry['normal']:
        assert sha(manifest_path.parent / entry['normal']) == entry['normalSha256']
    assert sha(project / 'output/imagegen/descent-premium-v1' / entry['original']) == entry['sourceSha256']
manifest['status'] = 'runtime-integrated-and-automatically-tested'
manifest['acceptanceScope'] = 'Windows runtime integration and manual image inspection; not final commercial art/human balance certification.'
save('Assets/Art/Production/PremiumV1/art-manifest.json', json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')

boss_manifest_path = project / 'Assets/Art/Production/BossAscensionV2/art-manifest.json'
boss_manifest = json.loads(boss_manifest_path.read_text(encoding='utf-8'))
for entry in boss_manifest['assets']:
    assert sha(boss_manifest_path.parent / entry['path']) == entry['sha256']
    if entry['normal']: assert sha(boss_manifest_path.parent / entry['normal']) == entry['normalSha256']
    raw = project / 'output/imagegen/boss-ascension-v2'
    assert sha(raw / entry['original']) == entry['sourceSha256']
    if entry.get('matte'): assert sha(raw / entry['matte']) == entry['matteSha256']
boss_manifest['status'] = 'runtime-integrated-and-automatically-tested'
boss_manifest['acceptanceScope'] = 'Eight configured stages; assisted runtime testing and visual review, not human balance certification.'
save('Assets/Art/Production/BossAscensionV2/art-manifest.json', json.dumps(boss_manifest, ensure_ascii=False, indent=2) + '\n')

build_relative = build.relative_to(project).as_posix()
build_name = build.parent.name.replace('MoriEchoes_', '')
build_report = project / ('docs/BuildReports/Windows_' + build_name + '.md')
assert '- BuildPipeline 结果：Succeeded' in build_report.read_text(encoding='utf-8-sig')
assert '- 错误 / 警告：0 / 0' in build_report.read_text(encoding='utf-8-sig')
test_rows = '\n'.join('| ' + t['name'] + ' | ' + str(t['checks']) + ' | 0 | `' + str(Path(t['report']).relative_to(project)).replace('\\', '/') + '` |' for t in tests)
evidence = f'''## 最终运行证据

本轮最终 Windows Development 构建：`{build}`。Unity 6000.6.7f1 / StandaloneWindows64，构建成功、0 错误/0 警告；构建记录：`{build_report.relative_to(project).as_posix()}`。下面五组均针对这一构建实际运行，Player 退出码 0、passed=true、errors 为空；详细范围以 JSON 为准。

| 实际运行组 | 通过检查 | 错误 | 原始报告 |
|---|---:|---:|---|
{test_rows}

源码校验：10040 项模型断言、5000 次有种子升级抽取、13 项导入安全检查。语法检查原始结果：{source_report['syntaxCheck'].splitlines()[0]}。它不替代上述 Unity 构建和 Player 测试。

这次回归修正了旧测试对角色 UI 名称、机关预警实现、强化技能充能、Boss 处决时长和新版招式的过时假设。诊断模式隔离合成键鼠的失焦状态，Boss 伤害边界探针在独立坐标等待物理同步后测量；正常游戏仍保留失焦暂停。位移技能共用胶囊碰撞探测，WRAITH / VEIL / ROOK 的实体墙阻挡和受阻保留能量/冷却已实际验证。战术镜头按玩家与 Boss 的间距平滑取景，五关 Boss 入镜检查通过。末关两个锚点独立控制交叉斩，击破后对应斩击清除；核心按现有规则可受伤。Boss 处决截图包含慢动作过程，结算在演出结束后检查。

复测工具使用 PowerShell 7。Windows PowerShell 5.1 会按 ANSI 误读现有 UTF-8 中文脚本，源码校验应在 `pwsh` 中运行。历史失败、超时和提前退出记录保留在 TestResults，不计为通过，也不替代本表的最终结果。

启动 `C:\\UnityProjects\\MoriEchoes\\PLAY_MoriEchoes.cmd`。没有生成新 ZIP。完整变更和哈希见 `ArtProduction/descent-premium-v1/file-changes.json` / `.csv`，构建文件哈希见同目录 `windows-build-files.json`，机器可读验收见 `delivery-verification.json`。

最新追加已实际接入：八关统一关闭攻击预判线、范围方块/圆圈、路径走廊、落点标记、倒计时和下一招提示文字。实际攻击贴图、激光、爆炸和命中反馈保留。Encounter 组在每个 Boss 阶段采样确认预判图示不可见、实际特效仍发出，竖井组检查预警池零发射。

Boss 追加 16 张设计原图加工为 96 个实体装备部件、32 个大招帧及 96 张法线，已接入八关三阶段；减伤 62%、生命 1.5 倍、单次伤害上限 0.8% 已通过真实伤害调用检查，处决结算保留。具体 Inspector、材质加工、数值和测试边界见 `docs/BOSS_ASCENSION_ACCEPTANCE.md`。两批合计 72 张设计原图、441 个 Sprite、330 张加工法线；透明编辑试验图不计为额外交付设计。

目视复核另修正竖井装饰：粗略骷髅/圆环换成主题实体浮雕，场景反馈保持原始透明度和朝向。演出锁定时真正停止重力漂移，竖井 Boss 镜头同时容纳玩家和 Boss。旧版 R21 的横版自动驾驶在高台旁反复跳跃而超时，现使用真实 S+Space 下穿输入通过；该失败记录保留，最终表仅计修正后的复测。运行验证针对当前已配置的八关；工程中另有未配置的第九、十关开发代码，本轮未删除，也不将其列为已验收关卡。

人工难度平衡、实体手柄长测、跨机性能、长时间内存稳定性和 Editor Play Mode 仍未验证。
'''
acceptance = (project / 'docs/PREMIUM_ART_ACCEPTANCE.md').read_text(encoding='utf-8')
save('docs/PREMIUM_ART_ACCEPTANCE.md', acceptance.split('## 最终运行证据')[0] + evidence)
boss_acceptance = (project / 'docs/BOSS_ASCENSION_ACCEPTANCE.md').read_text(encoding='utf-8')
save('docs/BOSS_ASCENSION_ACCEPTANCE.md', boss_acceptance.split('## 实际结果')[0] + '## 实际结果\n\n' + evidence)
launcher = f'''@echo off
setlocal
set "mori_game=%~dp0{build_relative.replace('/', chr(92))}"
if not exist "%mori_game%" (
  echo Verified game build was not found. See docs/PREMIUM_ART_ACCEPTANCE.md.
  pause
  exit /b 1
)
start "" "%mori_game%" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11
endlocal
'''
save('PLAY_MoriEchoes.cmd', launcher)
save('README.md', f'''# 末日回响 / MoriEchoes

Unity 6000.6.7f1 / C# / URP 2D / Input System 的离线单机横版闯关射击 Roguelite。当前实际配置为 **8 名角色、9 把武器、8 关战役**。前五关横向推进，后三关按当前工程既有 Ascent 规则向上推进；早期向下推进资料保留为历史设计。

双击 `PLAY_MoriEchoes.cmd`。实际 EXE：`{build_relative}`。本轮未生成 ZIP，旧构建和旧分享包保留；它们不代表当前版本。

- 操作和启动：`HOW_TO_PLAY.md`。
- 新美术、Inspector、难度规则、测试与限制：`docs/PREMIUM_ART_ACCEPTANCE.md`。
- 实际运行报告与完整文件变更：`ArtProduction/descent-premium-v1/delivery-verification.json`、`file-changes.json` / `.csv`。
- 56 张 image 接口原图：`output/imagegen/descent-premium-v1`；最终 313 个 Sprite / 234 张加工法线：`Assets/Art/Production/PremiumV1`。
- Boss 追加 16 张设计原图：`output/imagegen/boss-ascension-v2`；96 个实体部件 / 32 个大招帧 / 96 张法线：`Assets/Art/Production/BossAscensionV2`。防御及验收：`docs/BOSS_ASCENSION_ACCEPTANCE.md`。
- 提示词与美术预览：`ArtProduction/descent-premium-v1/prompts`、`review`。
- 进度：`docs/PROGRESS.md`；当前竖井规格：`docs/ASCENT_LEVELS_DESIGN.md`。

可选角色固定为 DELTA、VANGUARD、WRAITH、HAVOC、KALT、VEIL、SERA、ROOK。保留原四人的形象和配置；移出名单的旧角色素材保留。每人 Q/F/C 三招与 X 充能终结技，九种武器含激光刃。前五关新增敌兵、机关、实体模块、守卫、Boss 后两阶段和攻击效果已接入生成美术；后三关与新增角色/武器也有专属生成素材。

提高敌方伤害和机动，Boss 区域扩大；按最新要求关闭红黄预判线、范围框、落点与下一招提示，出招时显示真实特效。能量胶囊的同武器基础总弹丸伤害只提高 8%～10%；具体乘区和限制见验收文档。Tab 切换横版远景、第三人称近景、第一人称武器前景瞄准；均为 2D 正交游戏。

Boss 为高防御版本：生命 ×1.5、减伤 62%、单次最多扣总血量 0.8%。三阶段换装甲、冠饰和背部装备；常规技能/终极技能伤害乘区 2.25/3.25，范围长宽乘区 1.65/1.9。低血量处决保留。

工程入口：`Assets/Scenes/MainMenu/MainMenu.unity`，GameBootstrap 绑定 `Assets/Settings/MoriEchoesConfig.asset`。依赖固定 Input System 1.20.0、URP 17.6.0、UGUI 2.6.0。

本轮构建 0 错误/0 警告，五组独立 Player 自动回归通过。测试采用合成输入、无敌/传送/血量辅助；人工难度、实体手柄长测、跨机性能、Editor Play Mode 和商业最终美术认证尚未验证。历史交付报告仅描述当时的构建。
''')
save('HOW_TO_PLAY.md', f'''# 末日回响：当前版本操作说明

双击 `C:\\UnityProjects\\MoriEchoes\\PLAY_MoriEchoes.cmd`。实际程序：`{build}`。本轮没有新压缩包。打开工程使用 Unity 6000.6.7f1，入口 `Assets/Scenes/MainMenu/MainMenu.unity`；Inspector 和验收步骤见 `docs/PREMIUM_ART_ACCEPTANCE.md`。

主菜单选择角色、出战武器、关卡和难度，点击“部署 / 开始关卡”。共 8 人：DELTA 专注射击、VANGUARD 复苏壁垒、WRAITH 相位机动、HAVOC 爆破，新增 KALT 冷酷精准射手、VEIL 隐身射手、SERA 医师、ROOK 冲锋者。每人 Q/F/C 三招，HUD 显示名称和冷却；攻击和承伤为 X 专属终结技充能。位移遇实体墙会缩短距离，完全受阻保留冷却/能量。

| 操作 | 键鼠 |
|---|---|
| 移动、跳跃 | A/D 或左右箭头；Space 短按低跳、长按高跳 |
| 蹲下、下穿平台 | S/↓；S+Space |
| 闪避冲刺 | 左 Shift |
| 瞄准、开火、机瞄 | 鼠标指向；左键；右键 |
| 切枪、装弹 | 1–9 / 滚轮；R |
| 三招技能、能量终结技 | Q / F / C；X |
| 机关与秘密入口互动 | E |
| 视角 | Tab：战术远景 → 第三人称近景 → 第一人称武器前景瞄准 |
| 窗口、暂停 | F11 / Alt+Enter；Esc |

窗口可拖拽和最大化；三档视角保持 2D 横版规则。失焦自动暂停，回到窗口后点“继续作战”。升级时选三选一继续。手柄的基础映射为左摇杆移动、右摇杆瞄准、RT 开火、LT 机瞄、A 跳跃、B 冲刺、X 装弹、Y 主技能、LB/RB 换枪、View 切视角、十字上互动、Start 暂停；实体手柄、辅助招式与终结技的手柄体验尚未验收。

武器 1–9 依次为突击步枪、霰弹枪、精准步枪、冲锋枪、电磁轨道枪、榴弹枪、激光刃、电弧投射器、星脉连发弩。掉落胶囊即刻切换强化形态和火力机制；同武器基础总弹丸伤害仅 +8%～10%，实际伤害受暴击、专精、连击与多目标影响。

前五关依次为城市废墟、工厂蜂巢、寒峰隧道、未来都市、最终要塞，含七段战斗和独立 Boss 场。后三关废墟墓窟、沉没圣堂、星坠核心井当前实际为向上推进的竖井：击破主题闸门、对抗上方来敌和机关、抵达顶层 Boss。每关 Boss 有三个血量阶段、独特招式和处决死亡反馈。秘密路线有独立守卫与奖励。死亡后可从检查点重试，未消费经验减半；按战役进度解锁下一关。

本轮高难度显著提高敌伤、小兵速度和 Boss 覆盖；按最新要求不显示攻击预判线、范围框、落点、倒计时及下一招文字，攻击发生时才呈现相应特效。具体值、素材来源、实际测试和完整变更见 `docs/PREMIUM_ART_ACCEPTANCE.md`。自动测试不代表真人默认难度通关、实体手柄、跨机性能或商业美术最终验收。
''')
start = (project / 'START_HERE.md').read_text(encoding='utf-8-sig')
if '<!-- premium-current -->' not in start:
    save('START_HERE.md', f'''<!-- premium-current -->
# 当前版本入口 · 2026-10-27

双击 `C:\\UnityProjects\\MoriEchoes\\PLAY_MoriEchoes.cmd`。当前工程已集成生成美术，8 人、9 武器、8 关；实际程序 `{build}`。操作见 `HOW_TO_PLAY.md`，本轮所有运行证据、Inspector 与限制见 `docs/PREMIUM_ART_ACCEPTANCE.md`。没有生成新 ZIP。

以下是历史集成记录，其旧版路径、角色数量、灰盒描述与安装建议不代表当前版本。

---

''' + start)
progress = (project / 'docs/PROGRESS.md').read_text(encoding='utf-8-sig')
save('docs/PROGRESS.md', progress + f'''\n\n## {stamp} / 生成美术、高难度与最终运行回归

- 原装 imagegen CLI 经指定网关请求 gpt-image-2.5；56 张原图，313 个 Sprite，234 张加工法线，提示词、源图、裁切修订和配置备份已保留。
- Boss 新增 16 张设计原图、96 个实体装饰部件、32 个大招帧、96 张加工法线；12 张 RGB 棋盘底原图本地抠图。减伤 62%、生命 ×1.5、单次上限 0.8%，实际伤害跳字、处决和自然大招已验证。
- 前五关四类敌兵、机关、实体模块、独立守卫、Boss 后两阶段、攻击/命中/死亡效果接入；新增四人及后三关/三武器生成素材接入。名单严格 8 人，原四人配置和资源保留。
- 提高敌伤、机动与 Boss 区域覆盖；能量胶囊基础总弹丸伤害只 +8%～10%。位移技能统一实体碰撞保护，受阻保留冷却/能量。
- 按最新追加要求移除八关攻击预判图示与下一招攻略文字，实际攻击/命中效果保留；Boss 伤害区域蓄力时不可见，出招后仅展示攻击纹理。
- 实际构建 `{build_relative}`，Unity 6000.6.7f1，0 错误/0 警告。五组运行报告如下，均 passed=true、0 错误、退出码 0。

| 运行组 | 通过检查 | 错误 | 证据 |
|---|---:|---:|---|
{test_rows}

- 源码模型、语法和导入安全检查另已通过。启动器已指向本轮验证构建，未生成新 ZIP。
- Inspector、重测步骤、验收标准、完整变更和限制见 `docs/PREMIUM_ART_ACCEPTANCE.md`。人工平衡、实体手柄、跨机性能、长时间内存和 Editor Play Mode 尚未验证。
''')

build_files = [dict(path=p.relative_to(build.parent).as_posix(), bytes=p.stat().st_size, sha256=sha(p))
               for p in sorted(build.parent.rglob('*')) if p.is_file()]
save('ArtProduction/descent-premium-v1/windows-build-files.json', json.dumps(build_files, ensure_ascii=False, indent=2) + '\n')
verification = dict(completedAt=stamp, project=str(project), build=str(build), buildSha256=sha(build),
                    unity='6000.6.7f1', buildResult='Succeeded', buildErrors=0, buildWarnings=0,
                    buildReport=str(build_report), buildFiles=len(build_files), tests=tests,
                    sourceValidation=str(source_report_path), sourceValidationSha256=sha(source_report_path),
                    sourceImages=72, sprites=441, processedNormals=330, noNewZip=True,
                    artBatches={'premium': {'sourceImages':56,'sprites':313,'normals':234},
                                'bossAscension': {'sourceImages':16,'sprites':128,'normals':96}},
                    bossTuning={'damageReduction':0.62,'maxHitHealthFraction':0.008,'healthMultiplier':1.5,
                                'skillDamage':2.25,'ultimateDamage':3.25,'skillArea':1.65,'ultimateArea':1.9},
                    notVerified=['human balance', 'physical controller endurance', 'cross-machine performance',
                                 'Editor Play Mode', 'long-duration memory stability', 'commercial final art certification'])
save('ArtProduction/descent-premium-v1/delivery-verification.json', json.dumps(verification, ensure_ascii=False, indent=2) + '\n')

initial = json.loads((project / 'Validation/PremiumBackup_20261027_000154/file-changes.json').read_text(encoding='utf-8'))
changes = {item['path']: item for item in initial['changes']}
extras = [project / p for p in ('README.md', 'HOW_TO_PLAY.md', 'START_HERE.md', 'PLAY_MoriEchoes.cmd',
    'docs/PREMIUM_ART_ACCEPTANCE.md', 'docs/PROGRESS.md', 'Validation/source-validation.json', 'Tools/Finalize-PremiumDelivery.py',
    'Tools/Refine-PremiumCutouts.py', 'Tools/Test-VerticalCampaign.ps1', 'Tools/Test-AnimeArt.ps1',
    'Tools/Test-SideScroller.ps1', 'Tools/Plan-PremiumArt.py', 'Tools/Plan-FrontlineArt.py',
    'Tools/Plan-HazardArt.py', 'Tools/Prepare-PremiumArt.py', 'Tools/Apply-PremiumWork.py',
    'Assets/Settings/MoriEchoesConfig.asset', 'Assets/Settings/MoriEchoesConfig.asset.meta',
    'Assets/Scripts/Core/SideCameraDirector.cs', 'Assets/Scripts/Core/SideCameraDirector.cs.meta',
    'Assets/Scripts/Diagnostics/SideScrollerSmokeTest.cs', 'Assets/Scripts/Diagnostics/SideScrollerSmokeTest.cs.meta')]
extras += [p for root in ('Assets/Art/Production/PremiumV1', 'ArtProduction/descent-premium-v1/prompts',
    'ArtProduction/descent-premium-v1/review', 'output/imagegen/descent-premium-v1')
    for p in (project / root).rglob('*') if p.is_file()]
extras += [project / p for p in ('Assets/Art/Production/PremiumV1.meta',
    'ArtProduction/descent-premium-v1/windows-build-files.json',
    'ArtProduction/descent-premium-v1/delivery-verification.json')]
extras.append(build_report)
no_telegraphs_backup = backup.parent / 'NoTelegraphs'
extras += [project / p.relative_to(no_telegraphs_backup) for p in no_telegraphs_backup.rglob('*.cs')]
boss_backup = backup.parent / 'BossAscensionV2'
extras += [project / p.relative_to(boss_backup) for p in boss_backup.rglob('*.cs')]
visual_backup = backup.parent / 'BossVisualReviewFix'
extras += [project / p.relative_to(visual_backup) for p in visual_backup.rglob('*') if p.is_file()]
extras += [project / p for p in ('docs/BOSS_ASCENSION_ACCEPTANCE.md', 'Tools/Plan-BossAscensionArt.py',
    'Tools/Matte-BossAscensionArt.py','Tools/Prepare-BossAscensionArt.py','Assets/Editor/BossAscensionArtSetup.cs',
    'Assets/Editor/BossAscensionArtSetup.cs.meta','Assets/Art/Production/BossAscensionV2.meta',
    'ArtProduction/boss-ascension-v2/asset-quality-check.json','ArtProduction/boss-ascension-v2/build-source-hashes.json',
    'ArtProduction/boss-ascension-v2/boss-ascension-preview.jpg',
    'ArtProduction/boss-ascension-v2/runtime-preview.jpg','ArtProduction/boss-ascension-v2/runtime-preview.json')]
extras += [p for root in ('Assets/Art/Production/BossAscensionV2','ArtProduction/boss-ascension-v2/prompts',
    'ArtProduction/boss-ascension-v2/review','output/imagegen/boss-ascension-v2')
    for p in (project / root).rglob('*') if p.is_file()]
extras += [p for p in (project / 'Assets').rglob('*.cs.meta') if p.with_suffix('').relative_to(project).as_posix() in changes]
for p in extras:
    relative = p.relative_to(project).as_posix()
    if relative not in changes:
        old = backup / relative
        if not old.exists() and (no_telegraphs_backup / relative).exists():
            old = no_telegraphs_backup / relative
        if not old.exists() and (boss_backup / relative).exists():
            old = boss_backup / relative
        if not old.exists() and (visual_backup / relative).exists():
            old = visual_backup / relative
        if not old.exists():
            preserved = {
                'Assets/Scripts/Core/SideCameraDirector.cs': 'CameraFix/SideCameraDirector.cs',
                'Assets/Scripts/Diagnostics/SideScrollerSmokeTest.cs': 'Assets/Scripts/Diagnostics/SideScrollerSmokeTest.cs',
            }.get(relative)
            if preserved:
                old = backup.parent / preserved
        changes[relative] = dict(path=relative, status='modified' if old.exists() else 'added', before=sha(old) if old.exists() else None)
for item in changes.values():
    path = project / item['path']
    assert path.exists(), item['path']
    item['after'] = sha(path)
final_changes = sorted(changes.values(), key=lambda item: item['path'])
initial.update(time=stamp, validation='PNG/alpha (native or matted)/normal/hash verified; Unity import, build and five runtime groups passed.',
               changes=final_changes, finalizationBackups=str(backup.parent), generatedNewZip=False)
save('ArtProduction/descent-premium-v1/file-changes.json', json.dumps(initial, ensure_ascii=False, indent=2) + '\n')
with (delivery / 'file-changes.csv').open('w', encoding='utf-8-sig', newline='') as stream:
    writer = csv.DictWriter(stream, fieldnames=['path', 'status', 'before', 'after'])
    writer.writeheader(); writer.writerows(final_changes)
print(json.dumps(dict(build=str(build), changes=len(final_changes), tests={t['name']:t['checks'] for t in tests}, zip=False), ensure_ascii=False))
