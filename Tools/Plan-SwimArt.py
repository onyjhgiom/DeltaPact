"""Write art specifications only; live generation uses the unmodified bundled imagegen CLI."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / 'ArtProduction/swim-anime-v1/prompts'
PLAN.mkdir(parents=True, exist_ok=True)
STYLE = ('Premium hand-painted 2D Japanese anime game production art, precise silhouette, exquisite coherent material rendering, '
         'crisp contour with delicate internal shading. Side-view horizontal shooter, original designs. '
         'No text, UI, letters, labels, numbers, watermark, drop shadow, ground plane, borders or grid lines. ')
SHEET = ('True transparent background with alpha. Separate each cell by generous empty transparent gutters. '
         'Keep every silhouette fully inside its own cell with at least 12 percent margin, never overlap cells. ')
themes = [
    dict(key='sea', stage=8, name='Storm Sea', palette='turquoise seawater, ivory naval ceramic, dark indigo titanium and amber signal lenses',
         boss='a massive original biomechanical leviathan with a long armored shark-dragon head, layered ship-hull plates, fin hydrofoils and cyclone reactor',
         forms=['closed armored hull and serrated jaw', 'opened storm reactor with larger twin dorsal fins', 'fully deployed trident prow, exposed glowing reactor and winglike hydrofoils'],
         enemies=['a squat naval crab drone with twin rotary cannons', 'a slim armored combat diver with harpoon rifle', 'a fast blade-fin torpedo drone', 'a heavy manta-shaped naval gunship with broad armored wings'],
         props=['a long low floating pontoon deck with ivory plate top, rubber floats and metal cleats, perfectly horizontal walkable top',
                'a reinforced naval wreck wall section with layered ship hull plating', 'a moored warning buoy with an amber lantern and wave-battered supports',
                'a curved broken battleship prow with dangling cable and barnacles'],
         effects=['small dense water-cannon muzzle splash', 'elongated pale-cyan torpedo trail with bubbles', 'curling transparent breaking wave with white foam',
                  'bright water-impact starburst and droplets', 'surging spray pillar with isolated foam fragments', 'naval reactor electric arc with mist',
                  'an explosive tidal vortex with visible clear center', 'blue-white ship armor shrapnel blast'],
         ultimate='a towering breaking tsunami shaped like the leviathan jaw, from rising spray to enormous crest, collapse, then dissipating foam'),
    dict(key='abyss', stage=9, name='Abyssal Hydrothermal Vents', palette='deep navy chitin, bioluminescent cyan and violet, incandescent amber hydrothermal vents',
         boss='an original gigantic abyssal brood queen with a recognizable segmented nautilus shell, skeletal crab forelimbs, lantern eyes and vent chimneys',
         forms=['closed ridged nautilus carapace with restrained cyan eye glow', 'split carapace exposing violet gills and four vent chimneys', 'fully opened crown of luminous tentacles, molten core and extended hooked crab arms'],
         enemies=['a small luminous lantern-jelly predator with stinging tentacles', 'a thin pressure-suit diver with an elongated needle cannon',
                  'a fast translucent blade-eel with cyan spine', 'a squat armored vent-crab with glowing chimney and crushing claws'],
         props=['a stratified basalt shelf with pale tube worms, flat upper rock ledge', 'a tall jagged basalt cliff section with glowing mineral veins',
                'a recognizable hydrothermal vent chimney with amber mouth and mineral coral', 'a delicate bioluminescent coral cluster with small bubbles'],
         effects=['small violet pressure-cannon muzzle bloom', 'cyan bioluminescent bubble projectile wake', 'curved violet pressure-wave slash',
                  'bright amber thermal impact with mineral chips', 'roiling upward hydrothermal plume', 'purple abyssal suction spiral with clear center',
                  'molten vent detonation with bright cyan edges', 'luminous chitin shell shattering blast'],
         ultimate='an enormous hydrothermal eruption blooming upward around a luminous brood-queen silhouette, from vent ignition to plume crown, engulfing burst, then fading mineral embers')
]
jobs = []
layout = []
def job(theme, kind, prompt, cols=1, rows=1, opaque=False):
    key = theme['key']; name = f'{key}-{kind}.png'
    text = f'Use case: stylized-concept\nAsset type: Unity 2D game {kind}\n{STYLE}\nPalette: {theme["palette"]}.\n{prompt}\n'
    if not opaque: text += SHEET
    (PLAN / f'{key}-{kind}.txt').write_text(text, encoding='utf-8')
    entry = dict(model='gpt-image-2.5', prompt=text, size='1536x1024', quality='high', output_format='png', out=name)
    if not opaque: entry['background'] = 'transparent'
    # Preserve the already successful first background; do not pay for or overwrite it again.
    if not (ROOT / 'output/imagegen/swim-anime-v1' / name).is_file(): jobs.append(entry)
    layout.append(dict(file=name, stage=theme['stage'], kind=kind, columns=cols, rows=rows, opaque=opaque))

for theme in themes:
    key = theme['key']
    if key == 'sea':
        bg = 'Wide side-on landscape of a storm sea with shafts of sunlight, turquoise foam, distant ruined naval bridges and battleship silhouettes. Clear playable middle band. No characters or foreground props.'
    else:
        bg = 'Wide side-on abyssal underwater panorama: immense layered basalt walls receding into navy depth, bioluminescent coral, ghostly shipwreck ribs, amber hydrothermal plumes and sparse cyan overhead shafts. Clear central playable band. No characters. Rich painterly depth, not flat gradients.'
    job(theme, 'background', bg, opaque=True)
    job(theme, 'enemies', 'A strict 4-column by 2-row sprite sheet. Columns are four distinct enemies: ' + '; '.join(theme['enemies']) +
        '. Top row each enemy idle aiming LEFT; bottom row same corresponding design in attack pose aiming LEFT. Identical size, identity and baseline per column. Entire body and weapons visible.', 4, 2)
    job(theme, 'boss', f'A strict 3-column by 2-row sprite sheet of {theme["boss"]}. Columns show three increasing transformations: ' + '; '.join(theme['forms']) +
        '. Top row each form idle facing LEFT; bottom row matching form releasing a powerful attack facing LEFT. Preserve identity and overall body scale between forms. Full body, tail, wings and all limbs visible. No background aura hiding the actual physical form.', 3, 2)
    job(theme, 'architecture', 'A strict 2 by 2 modular environment sprite sheet, reading left-to-right then downward: ' + '; '.join(theme['props']) +
        '. Every prop separate and completely visible. Side-on physical props, not floating decorative rectangles. Props occupy their cells with generous empty gutters.', 2, 2)
    job(theme, 'effects', 'A strict 4-column by 2-row VFX sprite sheet, reading left-to-right then downward: ' + '; '.join(theme['effects']) +
        '. Dynamic detailed anime effects with thin translucent edges and bright readable cores, no geometric telegraph rectangles. Each effect an isolated independent cutout.', 4, 2)
    job(theme, 'ornaments', f'A strict 4-column by 3-row sheet of detached tangible equipment belonging to {theme["boss"]}. '
        'Each row belongs to one transformation, increasing complexity downward. Columns: left armored appendage, right armored appendage, crown assembly, central chest reactor. '
        'These are physical crafted shell/metal pieces with fasteners, overlapping plates and surface wear; no abstract rings, no floating crystals, no whole bosses. '
        'Row 1: ' + theme['forms'][0] + '. Row 2: ' + theme['forms'][1] + '. Row 3: ' + theme['forms'][2] + '.', 4, 3)
    job(theme, 'ultimate', 'A strict 2 by 2 sheet of four sequential ultimate attack frames in reading order. Show ' + theme['ultimate'] +
        '. Spectacular anime scale, intricate layers of foam or heated fluid and shock fragments. Each frame contains the full effect with transparent margin; shape expands across sequence without leaving its cell.', 2, 2)

(PLAN / 'batch.jsonl').write_text(''.join(json.dumps(j, ensure_ascii=False) + '\n' for j in jobs), encoding='utf-8')
(PLAN / 'layout.json').write_text(json.dumps(layout, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(jobs=len(jobs), sourceSheets=len(layout), promptDirectory=str(PLAN))))
