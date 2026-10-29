import importlib.util
import json
from pathlib import Path

spec = importlib.util.spec_from_file_location('premium', Path(__file__).with_name('Plan-PremiumArt.py'))
premium = importlib.util.module_from_spec(spec)
spec.loader.exec_module(premium)
premium.JOBS.clear()

themes = [
    ('ruins', 'weathered slate concrete, sand ivory, oxidized bronze, turquoise signals and orange weak points',
     'ragged armored scavenger with hooked claws; slender rooftop railgun sniper; small sharp-wing reconnaissance attack drone; wide tower-shield riot guardian',
     'winged ivory armored ruin beast with orange eye and arm cannon; form two deploys serrated bone shield wings, form three opens rib reactor and branching crown',
     'bronze bell guardian with giant hanging bell torso and two hammers; slender faceless echo weaver with ribbon arms and glass spindle staff'),
    ('factory', 'industrial gunmetal, dark jade steel, copper and amber furnace glow',
     'industrial cutter robot with saw forearms; segmented gun-turret engineer with long rivet cannon; compact flying twin-fan mine drone; massive blast shield furnace trooper',
     'round heavy brass furnace automaton, orange core and cannon forearms; form two exposes hot exhaust stacks, form three unfolds huge copper heat sinks and a glowing open furnace heart',
     'tall rivet priest with anvil head, copper robe plates and rivet staff; low quadruped slag hound with a furnace jaw and articulated iron spine'),
    ('ice', 'pearl frost, pale blue crystal, deep cobalt cloth and purple weak points',
     'ice claw stalker with white fur mantle; long slim frost railgun sniper; crystalline snow moth drone with four wings; broad ice shield glacier guardian',
     'beautiful jagged ice crystal wolf-knight with slender spear, frost blue core and crystalline mane; form two opens long crystal wing blades, form three radiates a vast angular frost halo and double lances',
     'floating mirror witch with prismatic skirt, faceless mirror head and four ice mirrors; crouched frost sonic hunter with fur mantle and a horn-shaped sonic crossbow'),
    ('neon', 'navy black polymer, silver, magenta holographic seams and cyan weak points',
     'nimble cybernetic phase assassin with two short blades; thin futuristic tactical sniper with asymmetric visor; triangular neon three-engine attack drone; massive segmented magnetic shield android',
     'slender dark violet reaper assassin with long blade fingers and elegant crescent wing blades; form two opens twin scythe wings, form three deploys six distinct sweeping purple blade wings and a brilliant chest core',
     'levitating geometric chess spirit with glossy ivory crown and orbiting physical chess blades; masked mirror thief with split glass cloak and a giant crescent mirror knife'),
    ('fortress', 'obsidian armor, antique gold, ivory blade edges and crimson core glow',
     'black red oathbreaker swordsman; ornate gold long rifle imperial sniper; crimson fork-wing assault drone; huge black gold citadel shield commander',
     'regal black ivory dragon armored tyrant, red chest core and huge arm cannon; form two opens bone wing armor and gold horns, form three unfolds a sweeping red-lined dragon mantle and exposed crimson reactor',
     'broad oathbroken paladin with a cracked golden tower shield and a short warhammer; alien star tomb keeper with asymmetrical orbital ring carapace and a long crystal key')
]

for key, palette, enemies, boss, guardians in themes:
    prefix = 'front-' + key
    premium.job(prefix + '-enemy-roster', 'Strict four-column two-row sheet: 8 full-body drawings, FOUR designs. Left to right: ' + enemies + '. '
                'Top row idle and bottom row same designs attacking. All face left. Native transparent background, identical scale between paired frames, complete silhouettes and 10 percent margins. Palette: ' + palette + '.')
    premium.job(prefix + '-boss-forms', 'Two separate full-body drawings in TWO columns, one row. Same monster identity: ' + boss + '. '
                'Left is form TWO; right is form THREE. Face LEFT. Distinct armament and silhouette changes, no hovering icons over the body. '
                'Palette: ' + palette + '. Rich material details, no ground shadow. Keep both figures fully inside own half with 12 percent empty border.', '1536x1024')
    premium.job(prefix + '-guardians', 'Strict 2 columns by 2 rows, four full-body cutouts. Two original secret route mini bosses: ' + guardians + '. '
                'Left column first guardian, right column second guardian. Top row idle, bottom row same respective guardian attacking. '
                'Face left. Strong distinctive silhouettes, no resemblance to generic humanoid armored boss. Palette: ' + palette + '. Entire figure in each cell, no overlapping particles.')
    premium.job(prefix + '-props', 'Six separate destructible and interactive architecture props in a strict 3-column by 2-row grid. Palette: ' + palette + '. '
                'Top row: wide solid platform ledge with crisp flat top; massive closed bulkhead door panel with mechanical lock; ornate upright trap emitter with warning lens. '
                'Bottom row: detailed fuel barrel with small light and ribs; chest-high breakable layered barricade with a weak plate; ornate secret passage entrance arch and closed treasure chest as ONE coherent structure. '
                'Front orthographic side view. Exquisite functional layers, bolts, bevels, worn material detail, local energy accent, transparent cutout, no words. Full objects, clean separation.')
    premium.job(prefix + '-effects', 'Eight separate premium hand-painted anime VFX sprites in strict 4 columns x 2 rows. Palette: ' + palette + '. '
                'Reading order: sharp bullet impact and shards; radial expanding energy shockwave; explosive hot core flash; tall rising energy eruption; '
                'narrow right-pointing projectile trail; boss death core disintegration burst; three sharp diagonal slicing strokes; curved weapon energy arc. '
                'Native translucent edges and rich internal filaments. No background, ground shadows, circles around icons or labels. Leave 15 percent safe transparent margins.')

premium.job('premium-pickups', 'Exactly eight collectible item sprites in strict four columns x two rows, isolated native transparent cutouts. '
            'Top row: orange spread energy capsule with diamond-pattern armor; cyan rapid-fire energy capsule with twin vent design; violet piercing energy capsule with long prism core; red explosive energy capsule with four segmented fins. '
            'Bottom row: ornate navy gold open weapon supply chest; holographic blueprint chip in a physical silver frame; emerald healing canister with lotus motif; brilliant cyan experience crystal set in a silver cage. '
            'Beautiful high-end 2D anime painted game props, readable silhouettes, exquisite glass and metal detail, no text or letters, no logos, no cast ground shadow.')
path = premium.PROMPTS / 'frontline-batch.jsonl'
path.write_text(''.join(json.dumps(item, ensure_ascii=False) + '\n' for item in premium.JOBS), encoding='utf-8')
print(f'Prepared {len(premium.JOBS)} first-five-stage redesign jobs.')
