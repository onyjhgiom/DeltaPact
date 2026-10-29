"""Persist prompts for image-model boss equipment and executed ultimate animations."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
folder = root / 'ArtProduction/boss-ascension-v2/prompts'
folder.mkdir(parents=True, exist_ok=True)
themes = [
    ('ruins', 'urban siege juggernaut', 'weathered ivory plating, oxidized bronze, turquoise circuitry',
     'riveted hydraulic gauntlet; cracked armored breastplate; telescoping shoulder battering ram; heavy crowned sensor helmet',
     'a monumental forward siege impact, broken concrete, molten turquoise shock front and dense airborne masonry'),
    ('factory', 'industrial furnace titan', 'gunmetal steel, brushed copper, amber molten furnace light',
     'hydraulic clamp arm; open toothed furnace breastplate; thick spiral cannon with turbine housing; curved exhaust chimney crown',
     'a gigantic erupting molten helix cannon blast, braided orange plasma, burning metal fragments and smoky incandescent exhaust'),
    ('ice', 'glacial antler monarch', 'porcelain white armor, deep cobalt crystal, silver, icy blue translucency',
     'layered crystalline forearm armor; faceted frozen chest carapace; massive segmented ice-feather wing; intricate branching frost antler crown',
     'a colossal freezing cyclone of beautifully faceted ice blades, crystalline shards, layered snow ribbons and brilliant blue-white frost'),
    ('neon', 'cybernetic mirror executor', 'midnight navy polymer, brushed silver, cyan optics and magenta accents',
     'articulated prism gauntlet; intricate armored optical chest lens; folding segmented blade-array wing; angular high-tech visor crown',
     'an overwhelming cyan-magenta mirror rupture, sweeping razor ribbons, crystalline holographic fragments and cascading optical sparks'),
    ('fortress', 'imperial rift sovereign', 'obsidian ceramic armor, engraved antique gold, crimson reactors, ivory edges',
     'ornate armored mechanical claw; ribbed obsidian reactor cuirass; immense segmented imperial wing with physical gold hinges; spiked imperial crown',
     'a colossal crimson void-cross detonation, torn space, curling black-red fire, gold sparks and fractured obsidian debris'),
    ('crypt', 'ossuary mausoleum guardian', 'aged ivory bone, tarnished bronze, teal soul-fire and dark limestone',
     'articulated bone-armored claw; engraved ossuary breastplate; heavy carved sarcophagus shoulder assembly; immense horned skull crest',
     'an erupting catacomb soul-storm, enormous bone spears, broken stone sarcophagi, spectral teal flames and churning dust'),
    ('tide', 'abyssal cathedral bell colossus', 'oxidized bronze, pearl shell, abyssal navy and luminous aquamarine',
     'barnacle-armored tidal claw; engraved bronze cathedral bell chest casing; layered nautilus fin and pressure-pipe back assembly; coral crown',
     'a tremendous abyssal tidal detonation, curling translucent waves, shattered pearl and bell fragments, turquoise foam and pressurized spray'),
    ('rift', 'stellar gravity-machine sovereign', 'black iridium, violet starlight, polished silver, sapphire energy',
     'intricate gravity pincer; faceted star-reactor chest harness; segmented orbital stabilizer wing with physical silver joints; meteor-crystal diadem',
     'a gigantic stellar singularity eruption, warped violet plasma streams, dark meteor fragments, blinding sapphire jets and starfire dust'),
]
style = ('Use case: stylized-concept\nAsset type: production Unity 2D anime boss sprite sheet\n'
         'Original premium Japanese anime science-fantasy action game artwork. Exquisite painterly cel shading, precise ink contours, '
         'subtle material texture, physical depth, crisp silhouette, polished coherent industrial fantasy design. '
         'Real alpha transparency, NOT a painted checkerboard. No background, text, labels, borders, grid lines, watermarks or baked shadows. '
         'Fully contained silhouettes with 12 percent transparent safety margin in each cell. No object touches another cell. ')
jobs = []
for stage, (key, boss, palette, parts, ultimate) in enumerate(themes):
    prompt = style + f'For the {boss}, using {palette}. A strict FOUR COLUMNS by THREE ROWS sheet of TWELVE isolated physical equipment pieces. '
    prompt += f'Column order from left to right: {parts}. Top row: intact first-phase equipment; middle row: visibly opened and reinforced second-phase equipment; bottom row: larger aggressive final-phase equipment with sophisticated articulated upgrades. '
    prompt += 'Each column shows exactly the same part type across all three rows, distinctly evolving. These are solid tangible sculpted objects with bolts, hinges, layered armor or natural crystal structure, NOT abstract geometric icons, flat squares, discs or diamonds. No character body: isolated ATTACHABLE equipment only. Three-quarter front / side view for a side-scrolling boss facing left. Structural gear fills its cell while leaving transparent margins. Limit decorative glow so physical surfaces remain legible.'
    jobs.append(dict(out=f'{key}-ornaments.png', prompt=prompt, model='gpt-image-2.5', size='1536x1024', quality='high', output_format='png', background='transparent'))
    prompt = style + f'For the {boss}, create {ultimate}. Palette: {palette}. '
    prompt += 'A strict TWO COLUMNS by TWO ROWS sheet of FOUR independent sequential frames of the SAME massive ultimate attack AFTER it has fired. Reading order top-left, top-right, bottom-left, bottom-right: violent explosive onset, enormous dense peak, expanding destructive wave, lingering detailed fragment-and-flame aftermath. Strong anime sakuga energy, spectacular layered light, sharp fragments, turbulence and dramatic material effects, readable silhouettes. No warning shapes, no floor targeting discs, no thin outlined circles, no UI hitboxes, no character, no scenery. Keep the attack centered and the same base orientation in each cell, transparency between frames; every frame entirely contained with margins.'
    jobs.append(dict(out=f'{key}-ultimate.png', prompt=prompt, model='gpt-image-2.5', size='1536x1024', quality='high', output_format='png', background='transparent'))
for job in jobs:
    (folder / (Path(job['out']).stem + '.txt')).write_text(job['prompt'], encoding='utf-8')
(folder / 'boss-batch.jsonl').write_text(''.join(json.dumps(j, ensure_ascii=False) + '\n' for j in jobs), encoding='utf-8')
(folder / 'pilot.jsonl').write_text(json.dumps(jobs[2], ensure_ascii=False)+'\n', encoding='utf-8')
(folder / 'themes.json').write_text(json.dumps([dict(stage=i,key=t[0],name=t[1]) for i,t in enumerate(themes)], ensure_ascii=False, indent=2),encoding='utf-8')
print(f'Prepared {len(jobs)} original image jobs: 96 physical ornaments and 32 ultimate frames.')
