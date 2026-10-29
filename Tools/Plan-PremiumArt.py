import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROMPTS = ROOT / 'ArtProduction/descent-premium-v1/prompts'
PROMPTS.mkdir(parents=True, exist_ok=True)
JOBS = []
STYLE = ('Premium original Japanese anime action game production art. Fine confident ink contours, sophisticated painted cel shading, '
         'brushed metal, layered cloth, beautiful material highlights, readable silhouette at small gameplay size. '
         'Unified navy tactical science fantasy aesthetic. Not pixel art, not a 3D render, not vector primitives. '
         'No text, logo, watermark, labels, borders, grid lines, UI or baked floor shadows. ')
ALPHA = ('Native transparent RGBA background, including transparent space between every object. '
         'No checkerboard painted into the image, no colored matte. Each figure fully contained with generous empty margins, no touching neighbors. ')


def job(name, prompt, size='1536x1024', transparent=True):
    full = 'Use case: stylized-concept\nAsset type: finished Unity 2D game art\n' + STYLE + ('\n' + ALPHA if transparent else '') + '\n' + prompt
    (PROMPTS / (name + '.txt')).write_text(full, encoding='utf-8')
    JOBS.append(dict(out=name + '.png', prompt=full, model='gpt-image-2.5', size=size,
                     quality='high', output_format='png', **({'background': 'transparent'} if transparent else {})))


CHARACTERS = {
    'kalt': 'Adult handsome cool stoic male sniper, silver ash short swept hair, steel blue eyes, angular beautiful face, tall lean build, navy long split-tail officer coat, gunmetal light armor, pale cyan precision optic at collar, black gloves and fitted boots. Distinctive long coat silhouette.',
    'veil': 'Adult female stealth marksman, violet-black asymmetric bob, sharp lavender eyes, asymmetrical hood folded behind head, graphite light stealth suit with layered purple scarf tails, narrow violet luminous seams, compact boots. Agile narrow silhouette.',
    'sera': 'Adult female field physician, honey blonde braided side ponytail, gentle determined emerald eyes, ivory cropped medical armored jacket over dark navy tactical suit, emerald translucent medical canisters on belt and small shoulder drone dock, practical knee boots. No real medical emblem.',
    'rook': 'Adult broad-shouldered male vanguard, short brown undercut, resolute handsome face, massive asymmetric graphite and burnt orange shoulder armor, reinforced forearm gauntlets, compact armored backpack and thick greaves. Powerful triangular silhouette without oversized head.'
}

for name, identity in CHARACTERS.items():
    job(name + '-animation', 'Character identity: ' + identity + '\nExactly 8 full-body animation drawings of this SAME character in a strict four-column, two-row sheet. '
        'All facing right in a side-on three-quarter combat view; hands ready to grip a separately attached rifle, no weapon in hands. '
        'Row 1: idle standing, run left contact, run passing, run right contact. Row 2: run recoil, run lift, airborne jump knees bent, low crouch aim. '
        'Keep identical head, costume, proportions and body scale in every frame. Each cell 384x512 on a 1536x1024 canvas, feet near 92 percent of cell height. '
        'Crouch is shorter naturally. No airborne glow or trails. Clean complete fingers, two arms, two legs. Emphasize polished faces and flowing fabric.')

job('new-powered-forms', 'Strict 2 columns by 2 rows, exactly four separate full-body figures, facing right, ready to aim a separately attached gun. '
    'Top-left KALT: ' + CHARACTERS['kalt'] + ' Now in powered form, articulated ice-blue angular cape fins and expanded precision shoulder armor. '
    'Top-right VEIL: ' + CHARACTERS['veil'] + ' Now powered, translucent violet scarf wings and sleek phase armor. '
    'Bottom-left SERA: ' + CHARACTERS['sera'] + ' Now powered, ivory emerald medical exosuit with luminous petal shoulder guards. '
    'Bottom-right ROOK: ' + CHARACTERS['rook'] + ' Now powered, deployed orange heavy armor plates and reinforced blast gauntlets. '
    'Preserve recognizable face, hair, base costume and palette. Do not add floating ground circles. 10 percent safe margins.', '1024x1024')

THEMES = {
    'tomb': dict(title='Ruin Necropolis', palette='ivory bone, oxidized bronze, slate plum stone, pale turquoise soul fire and sparse coral red weak spots',
                 setting='ancient ossuary burial shaft, intricately carved stone arches, stone sarcophagi, ivory funerary masks, chains, roots, small turquoise lanterns',
                 boss='Hundred-Bone King: a broad ancient ossuary monarch assembled from layered ivory vertebrae, bronze funerary armor and a small red heart. Form one: hulking armored skeletal king with closed rib shield and heavy crown. Form two: open ribcage turquoise furnace, branching antler crown and separate shoulder sarcophagi. Form three: unmistakable winged burial deity, towering bone fan mantle, exposed red heart and four spectral skeletal arms. Remain recognizable as the same being; no humanoid soldier recolor.',
                 enemies='bone-clawed crouched gravebound skeleton; slender robed skull rifle-spitter with funerary staff; skeletal winged serpent with narrow coiled body; squat massive ossuary behemoth with gravestone shoulders',
                 props='wide engraved tomb platform ledge; wide packed ivory skull relief gate module with solid stone frame; bone spike trap emitter altar; tall bronze chained soul lantern',
                 fx='turquoise splinter impact burst; upward ivory bone spike eruption; vertical turquoise ghost flame column; pale soul corona ring; narrow bone shard projectile pointing right; cracked red heart death explosion; falling spectral stone sarcophagus; spreading ethereal skeletal wing arc'),
    'tide': dict(title='Drowned Cathedral', palette='petrol blue, pearl ivory, oxidized gold, turquoise water and warm amber weak spots',
                 setting='sunken cathedral shaft, exquisite carved Gothic pillars, broken stained glass, coral-encrusted statues, suspended chains, submerged bells, distant turquoise caustic illumination and drifting underwater motes',
                 boss='Tidal Devourer: huge regal deep sea leviathan with armored nautilus shell, pearl coral armor and a glowing amber eye. Form one: curled heavy shell with crustacean claws. Form two: shell unfurled into a cathedral-like crest, long fin ribbons and exposed luminous whirlpool heart. Form three: majestic abyssal beast with many large translucent fins, open crown and enormous tidal maw. Non-humanoid aquatic silhouette, sophisticated anime monster design.',
                 enemies='drowned armored diver with claw anchor; slender ghostly siren carrying pearl cannon; long luminous electric eel with fins; massive coral crab leviathan with cathedral shell',
                 props='wide ivory coral-encrusted platform ledge; wide pearl shell tidal seal gate module; ornate brass water pressure jet emitter; tall hanging ancient diving bell',
                 fx='turquoise water impact splash; upward curling tidal wave; tall translucent water geyser; spiral whirlpool ring; luminous pearl water lance pointing right; shattering pearl shell death implosion; ornate ghostly sunken bell shockwave; crescent foaming tidal slash'),
    'rift': dict(title='Starfall Core Shaft', palette='obsidian midnight blue, amethyst crystal, silver alloy, violet light and pink red energy core',
                 setting='deep alien reactor shaft, fractured obsidian buttresses, silver rib vaults, suspended amethyst monoliths, shattered orbital rings and distant starfield through spatial fissures',
                 boss='Gravity Echo: angular alien geometric entity, obsidian crystalline armor and polished silver ribs surrounding one brilliant pink singularity. Form one: solid elongated diamond carapace with three asymmetric prongs. Form two: split bipolar double core with six floating physical crystal blades tethered to body. Form three: enormous unfolded eclipse crown with a dark center and radial silver-black crystalline wings. Nonhuman, no skull, no sea creature, no generic robot.',
                 enemies='jagged obsidian rift husk with long claw legs; tall silver prism lancer with sharp railgun arm; wingless hovering segmented crystalline drifter; broad massive gravity colossus with asymmetric black monolith shoulders',
                 props='wide dark silver reactor platform ledge; wide crystalline violet gravity seal gate module; polished black gravity inversion emitter; tall floating amethyst fractured monolith',
                 fx='violet fractured glass impact; upward amethyst crystal eruption; tall narrow violet spatial rift; eclipsed singularity ring; angular violet plasma lance pointing right; crystalline red singularity death rupture; descending black silver prism; long curved magenta gravity slash')
}

for key, theme in THEMES.items():
    if key != 'tomb':
        job(key + '-background', 'Finished richly detailed vertical background for ' + theme['title'] + '. ' + theme['setting'] + '. Palette: ' + theme['palette'] + '. '
            'Straight-on side-view shaft section, no top-down view. Open dim middle 60 percent for combat readability, rich architecture mostly on both edges. '
            'Multiple layers of distant architecture and atmospheric depth, restrained highlights, painterly exquisite details. No characters, no enemies, no foreground combat effects, no center blockage, no platforms. '
            'Full bleed portrait composition, not a map or diagram.', '1024x1536', False)
    job(key + '-boss-forms', theme['boss'] + ' Palette: ' + theme['palette'] + '. '
        'Exactly six isolated full-body drawings in strict three columns and two rows. Column 1 form one, column 2 form two, column 3 form three. '
        'Top row calm combat-ready pose, bottom row same respective form attacking with arms/claws extended slightly. '
        'Face towards LEFT in three-quarter side view. Complete body, generous cell padding, baseline aligned. '
        'Keep forms equally readable at similar canvas size; do not surround with huge glow disks or baked cast shadows. Surface details and distinct silhouettes matter. Cell size 512x512.')
    job(key + '-enemy-roster', 'Exactly 8 isolated full-body enemy drawings in a strict 4-column, 2-row grid. '
        'These FOUR distinct enemy designs from left to right: ' + theme['enemies'] + '. '
        'Top row each enemy idle ready pose; bottom row SAME four corresponding enemies in attack pose. '
        'All face LEFT in a three-quarter side view. Shared palette: ' + theme['palette'] + '. '
        'Each design must have a dramatically distinct silhouette, ornate material detail, a restrained bright weak point. '
        'Full subjects within own cells, scale consistent between the two poses, generous padding, no ground or particles behind body.')
    job(key + '-architecture', 'Exactly four separate prop cutouts in 2 columns, 2 rows: ' + theme['props'] + '. '
        'Reading order: upper left platform; upper right gate; lower left emitter; lower right ornament. '
        'Straight-on orthographic side view, usable 2D modular architecture. Palette: ' + theme['palette'] + '. '
        'Platform and gate each visibly wider than tall with a clean flat top collision edge; do not fuse props together. '
        'Crisp bevels, rich engraved surfaces, restrained built-in light sources, 12 percent space around each object.')
    job(key + '-effects', 'Exactly eight distinct isolated VFX sprites, strict 4 columns by 2 rows, reading left to right: ' + theme['fx'] + '. '
        'Palette: ' + theme['palette'] + '. Fine anime hand-painted energy, detailed bright cores and naturally fading translucent edges, elegant filaments and particles. '
        'Each effect centered within its cell with 15 percent fully transparent safe margins. Preserve alpha, no dark rectangle. '
        'Directional attacks have strong readable shapes; emitters point UP except projectile points RIGHT. Not icons inside circles, no card or border.')

job('advanced-weapons', 'Exactly THREE highly detailed original weapon side-profile cutouts stacked in 3 equal horizontal rows, points RIGHT. '
    'Top: LS-01 laser sword, elegant angular navy-silver hilt at left, long straight cyan white energy blade extending right with a controlled fine glowing edge. '
    'Middle: AC-13 arc caster, heavy compact dark navy gun with copper induction coils, turquoise insulators and forked electric muzzle. '
    'Bottom: BN-07 pulse crossbow, sleek silver navy tactical crossbow with amethyst capacitor, gracefully curved twin bow arms and a triangular forward rail. '
    'No hands, no ammunition labels, no letters. Distinct silhouettes and beautiful functional material details.', '1024x1536')
for weapon, desc in [('laser-sword', 'navy silver angular laser sword hilt with a long brilliant cyan energy blade'),
                     ('arc-caster', 'navy copper electromagnetic arc caster with twin induction coils and forked turquoise muzzle'),
                     ('pulse-crossbow', 'navy silver pulse crossbow with amethyst capacitors and curved bilateral limbs')]:
    job(weapon + '-view', 'One isolated weapon: ' + desc + '. First-person aiming foreground view from behind and slightly above the grip, '
        'weapon lower-right directed toward upper-left vanishing point, handsome detailed metal construction, full weapon and 10 percent margins. '
        'No hands, character, ground, scope reticle or background. Same premium anime painted weapon aesthetic.', '1024x1024')
job('new-character-skills', 'Exactly 8 isolated special attack effect cutouts in strict 4 columns x 2 rows. Columns correspond to KALT icy cyan, VEIL violet, SERA emerald gold, ROOK orange. '
    'Top row: horizontal precision ice lance pointing right; crescent violet phase slash; floating emerald healing lotus halo; massive orange gauntlet shockwave pointing right. '
    'Bottom row: cyan railgun annihilation burst; violet ethereal butterfly explosion; large luminous emerald protective petal dome; orange heavy armor explosive ground impact. '
    'Delicate hand-painted anime VFX with sharp bright cores, translucent tapered edges, lively small sparks, no text or cards, each contained inside its own cell.')

(PROMPTS / 'production-batch.jsonl').write_text(''.join(json.dumps(item, ensure_ascii=False) + '\n' for item in JOBS), encoding='utf-8')
(PROMPTS.parent / 'production-plan.json').write_text(json.dumps({'version': 'descent-premium-v1', 'jobs': len(JOBS), 'characters': list(CHARACTERS), 'themes': list(THEMES), 'outputs': [item['out'] for item in JOBS]}, indent=2), encoding='utf-8')
print(f'Prepared {len(JOBS)} distinct image jobs; no API requests performed.')
