"""Author specifications/prompts; actual generation uses the bundled imagegen CLI."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / 'ArtProduction/boss-techniques-v1'
PLAN.mkdir(parents=True, exist_ok=True)
STAGES = [
 ('城市废墟', '破碎者', 'weathered gunmetal, turquoise hydraulics, warm copper sparks', [
  ('轨枕穿刺', '依次顶起三道短促地刺，位置释放前固定。', '裂开的混凝土轨枕包裹钢刺，铜色碎屑沿斜面飞散。', '离开三个红色落点，可跳过中间一刺。', 'three staggered concrete railway spikes with hydraulic steel teeth'),
  ('钢索摆锤', '铆接重锤沿固定拱形轨迹横摆一次。', '粗钢索、磨损铆钉与弯月状尘带，锤头保持实物轮廓。', '从摆锤下方或轨迹外侧通过。', 'a riveted demolition hammer on a taut cable with a restrained crescent dust wake'),
  ('瓦砾折返', '两块装甲残片沿不同高度折返，留下中央通路。', '扭曲钢板拖出细碎砂砾，蓝色液压断口点亮边缘。', '观察两条路线，在中间高度换位。', 'two returning jagged armored slabs with granular rubble trails')]),
 ('工厂蜂巢', '熔核', 'blackened iron, enamel green machinery, restrained molten orange', [
  ('铸流分闸', '两道窄熔流交替开启，中央冷却带始终可通行。', '铸铁闸嘴喷出黏稠熔浆，暗色氧化壳包住橙红内核。', '进入中央间隔，避免熔流底座。', 'a pair of foundry sluice mouths pouring narrow viscous lava streams'),
  ('磁轨剪刀', '上下两根磁轨束斜向交叉，随后熄灭。', '铁磁碎片围绕细窄翡翠束芯，交点只有短促铜色火星。', '沿交叉轨道的外侧移动。', 'two crossed narrow green magnetic cutting rails carrying dark iron filings'),
  ('炉心跳火', '三枚炉渣团先后沿不同弧线跳跃。', '半凝固熔壳、短尾焰和烟灰，落点生成熔裂花纹。', '躲开固定弧线并利用两次落地之间的间隔。', 'three hopping furnace slag globes with solid black crust and small hot cracks')]),
 ('寒峰隧道', '霜牙', 'frosted cobalt crystal, pale cyan, restrained violet accents', [
  ('霜镜折射', '冰棱沿两段折线路线射入战区。', '透明冰镜保留切面与内部裂纹，折射尾迹细而清楚。', '穿过折线之间的空隙，避开拐点。', 'a faceted ice mirror splitting a crystalline lance into two angular paths'),
  ('雪檐断坠', '四块雪檐按节奏坠落，最后一块延迟。', '层积雪壳剥落成粉雪，深蓝冰芯完整可辨。', '横向走出落点，不要提前回头。', 'four separated falling cornices of blue glacial ice and finely powdered snow'),
  ('零度花葬', '冰花环从外向内绽放，内圈短暂安全。', '非对称冰瓣与细小霜晶逐层展开，不使用整屏白闪。', '进入环中心或完全离开外缘。', 'a hollow crystalline frost blossom with asymmetric translucent ice petals')]),
 ('未来都市', '零点', 'violet titanium, muted magenta, precise cyan optical edges', [
  ('像素错位', '三个窄光闸逐段亮起，按顺序封锁小片区域。', '实体投影棱片分裂重组，边缘扫描纹理克制清晰。', '在刚熄灭的光闸位置前进。', 'three dislocated optical gate shards made of layered violet glass and cyan scan edges'),
  ('相位截帧', '两枚镜门之间释放一条短暂定向束。', '金属镜框、薄膜干涉色与纤细光束，背景保持可见。', '离开两扇镜门连线。', 'two precision metal phase mirrors linked by a thin interference beam'),
  ('光棱回廊', '光刃沿三折回廊巡行一次。', '棱镜翼刃拖出半透明彩色折面，亮部集中于刀口。', '利用回廊开口穿出。', 'a prism wing blade following a three-bend corridor with delicate chromatic facets')]),
 ('最终要塞', '终末回响', 'obsidian armor, ember crimson, antique pale gold', [
  ('灰烬裁翼', '双翼刃从两侧沿弯曲路径合拢。', '黑金翼片中透出暗红余烬，刃尖细而锐利。', '从两翼之间提前向上跳出。', 'paired obsidian and antique gold scythe wings with granular red embers'),
  ('核心贯星', '核心预热后横向贯穿固定高度。', '实体炮膛收束成深红束芯，外包金属尘环。', '跳过固定高度射线。', 'a heavy fortress rail core firing one narrow crimson lance inside a metallic dust sheath'),
  ('终焉节拍', '三组错位冲击波交替从高低两侧逼近。', '暗红装甲波瓣像机械心搏逐次打开，火花不盖住轮廓。', '按节拍在高低通路之间切换。', 'three offset armored pulse fins opening like a mechanical heartbeat')]),
 ('废墟墓窟', '百骸王', 'aged ivory bone, oxidized copper, dim jade soul flame', [
  ('棺钉倒雨', '三枚巨型棺钉斜落，命中点预先标明。', '雕刻骨纹的铜钉携带细长幽绿火丝，碎骨向两侧飞散。', '从钉列间隙穿过。', 'three engraved copper coffin nails with ivory bone collars and thin jade flame wakes'),
  ('冥灯引渡', '一盏骨灯沿阶梯状路径游走。', '真实骨架灯笼、破布与烟丝形成冷暖层次。', '离开骨灯预示的折线路线。', 'a tangible ribcage funerary lantern with torn cloth and restrained spectral smoke'),
  ('骸冠合葬', '两侧骨冠咬合一次，中央保留短暂逃生口。', '多层颌骨像棺盖闭合，玉色裂隙点亮牙根。', '先退入中央，再从顶部出口离开。', 'two monumental interlocking skeletal crown jaws with jade cracks')]),
 ('沉没圣堂', '潮汐吞噬者', 'sea-worn bronze, pearl cyan water, coral orange', [
  ('钟鳍回旋', '钟形鳍沿固定半圆航迹绕过玩家旧位置。', '铜钟鳍片挤出珍珠泡沫弧，水膜保留透明层。', '向半圆开口方向移动。', 'a bronze cathedral bell-fin carving a crescent of pearl foam'),
  ('珊瑚断流', '上下两股珊瑚碎流反向横穿战区。', '细碎珊瑚与透明压力膜交织，橙色碎片作为方向标。', '保持在两股水流之间。', 'two counterflowing ribbons of coral fragments in clear cyan pressure membranes'),
  ('圣堂沉钟', '沉钟下坠，随后在固定位置激起空心水环。', '布满藤壶的铜钟击出层叠薄水膜与气泡花冠。', '先避开下坠点，再穿入水环内部。', 'a barnacled bronze cathedral bell striking a hollow layered water crown')]),
 ('星坠核心井', '引力残响', 'dark meteoric stone, violet refraction, small rose-gold cores', [
  ('双星弹弓', '两枚星核沿上下不同弧度交会。', '陨铁包裹玫瑰金星核，紫色扭曲带保持透明。', '沿两条弧线外侧移动。', 'two dark meteor cores slingshotting on opposite violet refractive arcs'),
  ('断层星轨', '三条错位星轨依次扫过预定斜面。', '断裂陨石薄片排成细密轨迹，亮度集中在切口。', '跟随先扫完的斜面穿越。', 'three offset tectonic meteor rails made of thin fractured stone plates'),
  ('视界裂瓣', '四枚裂瓣沿固定径向向旧落点收拢。', '半透明空间褶皱包裹实物黑曜瓣片，中心留空。', '在裂瓣间的对角线方向逃离。', 'four obsidian aperture petals folding inward through translucent violet space wrinkles')]),
 ('风暴海面', '狂澜利维坦', 'turquoise water, ivory naval ceramic, subdued amber', [
  ('泡沫鱼雷', '双鱼雷从水下沿不同弧线跃出。', '象牙装甲鱼雷拖出细泡尾迹，浪花仅在破水点盛开。', '跳上浮台或进入两条弧线之间。', 'two ivory naval torpedoes breaching with intricate small bubble trails and foam collars'),
  ('舰鳍破浪', '舰鳍横扫后在旧位置立起一道浪脊。', '实体折叠舰鳍切开青绿水膜，浪脊有深浅通透层。', '先跳过舰鳍，再离开浪脊位置。', 'a titanium naval hydrofoil carving a translucent turquoise wave ridge'),
  ('天潮三叠', '三道高度不同的窄浪墙依次压来。', '三重浪冠具有各异轮廓，白沫细碎而不过曝。', '在浪墙间换位或从浮台跃过低浪。', 'three distinct narrow tidal crests with delicately painted foam and dark teal interiors')]),
 ('深渊热泉', '深渊母巢', 'deep navy chitin, bioluminescent cyan, muted amber mineral heat', [
  ('管虫焰冠', '三座管虫热泉错时喷发，保留可游出的空隙。', '钙质管壁托起琥珀热羽流，青色菌膜附着于边缘。', '游入两根热柱之间。', 'three calcified tube-worm chimneys erupting narrow amber mineral plumes with cyan bacterial rims'),
  ('墨晶分裂', '墨晶沿两条分叉斜线射出。', '半透明墨囊破裂为棱晶与细小荧光液滴。', '沿分叉外缘上下游动。', 'an abyssal ink sac splitting into angular dark crystal lances and tiny luminous droplets'),
  ('深渊花房', '四瓣壳叶沿弧线包抄旧位置，顶部留有出口。', '珍珠贝壳、鳃膜和热泉烟丝构成清楚的四瓣轮廓。', '从顶部缺口游出，攻击不会继续追踪。', 'four pearlescent chitin petals enclosing delicate gill membranes and thin hydrothermal smoke')])
]
records, jobs = [], []
for stage, (title, boss, palette, skills) in enumerate(STAGES):
    for slot, (name, release, visual, dodge, motif) in enumerate(skills):
        records.append(dict(stage=stage, slot=slot, name=name, release=release, visual=visual, dodge=dodge, motif=motif))
    prompt = ('Use case: stylized-concept\nAsset type: production sprites for a premium Japanese anime 2D shooter.\n'
      f'Original boss {boss}, theme {title}. Palette: {palette}.\n'
      'A strict 3-column by 2-row effect atlas. Each COLUMN is one distinct skill. Top row is its release effect, '
      'bottom row its matching fully developed impact effect. Three columns LEFT TO RIGHT: ' + '; '.join(s[4] for s in skills) + '.\n'
      'Only isolated effects and their tangible spell mechanisms, no whole boss, no characters, no scenery. '
      'Exquisite painterly materials, clean readable silhouettes, nuanced translucent edges, purposeful negative space. '
      'Strong color identity without overexposure: bright cores occupy under 5 percent of each effect; rich midtones and dark material contours. '
      'No additive white haze, screen-filling flare, starburst spam, glowing geometric boxes, UI, text, numbers, labels, grid or watermark. '
      'True transparent alpha background. All six effects completely inside separate equal cells, generous 12 percent transparent margin and wide empty gutters. '
      'Entire full effects visible with no cropped tips. Coherent side-view game art; each skill must remain unmistakably different.')
    (PLAN / f'stage-{stage+1:02}-prompt.txt').write_text(prompt, encoding='utf-8')
    jobs.append(dict(model='gpt-image-2.5', prompt=prompt, size='1536x1024', quality='high', background='transparent', output_format='png', out=f'stage-{stage+1:02}-techniques.png'))
(PLAN / 'catalog.json').write_text(json.dumps(records, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
(PLAN / 'batch.jsonl').write_text(''.join(json.dumps(j, ensure_ascii=False)+'\n' for j in jobs), encoding='utf-8')
lines = ['# 全十关 Boss 新增技能与美术规格', '', '每关新增三招；第一、二、三形态各解锁一招，穿插于原有固定技能之间。攻击落点于释放时固定，不追着玩家移动；红色半透明范围与伤害判定一致。', '',
         '以下为设计规格，实际生图、接入及测试状态以验收报告为准。每招对应图集一列，上格为释放图、下格为命中图。', '']
for stage, (title, boss, palette, skills) in enumerate(STAGES):
    lines += [f'## 第 {stage+1} 关 · {title} / {boss}', '']
    for slot, s in enumerate(skills):
        lines += [f'### {s[0]}（形态 {slot+1} 解锁）', '', f'释放方式：{s[1]}', '', f'视觉表现：{s[2]}', '', f'闪避方法：{s[3]}', '', f'素材：`Assets/Art/Production/BossTechniquesV1/stage-{stage+1:02}/skill-{slot+1}-release.png` 与 `skill-{slot+1}-impact.png`。', '']
(ROOT / 'docs/BOSS_TECHNIQUE_DESIGN.md').write_text('\n'.join(lines), encoding='utf-8')
print(json.dumps(dict(stages=10, skills=len(records), sheets=len(jobs))))
