import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
prompts = root / 'ArtProduction/descent-premium-v1/prompts'
style = json.loads((prompts / 'frontline-batch.jsonl').read_text(encoding='utf-8').splitlines()[0])['prompt'].split('Strict four-column')[0]
themes = [
    ('ruins', 'sandstone, oxidized bronze, ivory concrete and turquoise circuitry', 'low horizontal twin-barrel fixed scavenger cannon'),
    ('factory', 'gunmetal steel, riveted copper and amber molten core', 'heavy fixed industrial rivet cannon with two thick horizontal barrels'),
    ('ice', 'white frost, jagged cobalt crystal and pale blue ice', 'compact crystalline frost cannon with a slender horizontal lance barrel'),
    ('neon', 'navy black polymer, magenta seams, silver and cyan optics', 'fixed prism laser turret with a long triangular cyan horizontal emitter'),
    ('fortress', 'obsidian armor, antique gold, ivory edges and crimson reactors', 'ornate armored imperial siege turret with a crimson horizontal cannon'),
]
jobs = []
for key, palette, turret in themes:
    prompt = style + ('Six orthographic side-view production sprites in a strict 3 columns by 2 rows, transparent space between all cells. '
        'Use ' + palette + '. Column one: a single heavy jagged falling rock or metal rubble block, upper intact, lower cracked but complete. '
        'Column two: a low mechanical ground spike trap with flat horizontal base, upper retracted tips, lower three fully extended sharp spikes. '
        'Column three: ' + turret + ', upper idle, lower firing with a small muzzle flash. Turret faces LEFT, grounded stable low silhouette, no legs or humanoid body. '
        'Within each column identical scale and identical base silhouette across both rows. Complete objects in separate cells with 14 percent empty margins. '
        'Exquisite painted anime material shading, crisp mechanical detail, clean readable silhouettes. No graphic rings, no ground shadow, no UI, no letters. '
        'Each row contains exactly three isolated objects, no fragments outside the object.')
    name = 'front-' + key + '-hazards'
    (prompts / (name + '.txt')).write_text(prompt, encoding='utf-8')
    jobs.append(dict(out=name + '.png', prompt=prompt, model='gpt-image-2.5', size='1536x1024', quality='high', output_format='png', background='transparent'))
(prompts / 'hazard-batch.jsonl').write_text(''.join(json.dumps(job) + '\n' for job in jobs), encoding='utf-8')
print('Prepared five dedicated hazard/turret art sheets.')
