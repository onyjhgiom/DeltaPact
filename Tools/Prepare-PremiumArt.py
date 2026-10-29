import argparse
import hashlib
import importlib.util
import json
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

PROJECT = Path('C:/UnityProjects/MoriEchoes')
WORK = Path(__file__).resolve().parents[1]
SOURCE = PROJECT / 'output/imagegen/descent-premium-v1'
OUTPUT = WORK / 'Assets/Art/Production/PremiumV1'
REVIEW = WORK / 'ArtProduction/descent-premium-v1/review'
spec = importlib.util.spec_from_file_location('anime', PROJECT / 'Tools/Prepare-AnimeArt.py')
anime = importlib.util.module_from_spec(spec)
spec.loader.exec_module(anime)
records = []


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def sheet(name, columns, rows, clean=False):
    image = Image.open(SOURCE / (name + '.png')).convert('RGBA')
    alpha = np.asarray(image.getchannel('A'))
    if (alpha < 8).mean() < 0.15:
        raise ValueError('Native transparency missing: ' + name)
    result = []
    for row in range(rows):
        for column in range(columns):
            frame = image.crop((round(column * image.width / columns), round(row * image.height / rows),
                                round((column + 1) * image.width / columns), round((row + 1) * image.height / rows)))
            if clean:
                mask = np.asarray(frame.getchannel('A')) > 24
                visited = np.zeros(mask.shape, dtype=bool)
                components = []
                for y, x in zip(*np.where(mask)):
                    if visited[y, x]:
                        continue
                    visited[y, x] = True
                    queue, component = deque([(y, x)]), []
                    while queue:
                        cy, cx = queue.popleft()
                        component.append((cy, cx))
                        for ny, nx in ((cy-1,cx),(cy+1,cx),(cy,cx-1),(cy,cx+1)):
                            if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny,nx] and not visited[ny,nx]:
                                visited[ny,nx] = True
                                queue.append((ny,nx))
                    components.append(component)
                biggest = max(map(len, components))
                alpha = np.asarray(frame.getchannel('A')).copy()
                for component in components:
                    if len(component) < biggest * 0.015:
                        for cy, cx in component:
                            alpha[max(0,cy-2):cy+3, max(0,cx-2):cx+3] = 0
                frame.putalpha(Image.fromarray(alpha))
            bounds = frame.getchannel('A').point(lambda value: 255 if value > 24 else 0).getbbox()
            if not bounds:
                raise ValueError('Empty cell: ' + name)
            result.append(frame.crop(bounds))
    return result


def normalized(figure, size=(640, 512), height=410):
    factor = min(height / figure.height, (size[0] - 40) / figure.width)
    figure = figure.resize((max(1, round(figure.width * factor)), max(1, round(figure.height * factor))), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', size)
    canvas.alpha_composite(figure, ((size[0] - figure.width) // 2, 469 - figure.height))
    return canvas


def add(image, path, source, kind, group, slot=0, frame=0, ppu=256, normal=True):
    target = OUTPUT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    anime.write_image(image, target)
    normal_path = path.replace('.png', '-normal.png') if normal else ''
    if normal:
        anime.write_image(anime.normal_map(image), OUTPUT / normal_path)
    records.append(dict(path=path, kind=kind, group=group, slot=slot, frame=frame, pixelsPerUnit=ppu,
                        sha256=sha(target), normal=normal_path, normalSha256=sha(OUTPUT / normal_path) if normal else '',
                        original=source + '.png', sourceSha256=sha(SOURCE / (source + '.png'))))


def cutout(image, width=512, height=512):
    image = image.copy()
    image.thumbnail((width - 32, height - 32), Image.Resampling.LANCZOS)
    result = Image.new('RGBA', (width, height))
    result.alpha_composite(image, ((width - image.width) // 2, (height - image.height) // 2))
    return result


def process():
    for group, key in enumerate(('kalt', 'veil', 'sera', 'rook')):
        source = key + '-animation'
        frames = sheet(source, 4, 2, clean=True)
        factor = min(410 / frames[0].height, 580 / max(frame.width for frame in frames))
        for index, frame in enumerate(frames):
            size = (max(1, round(frame.width * factor)), max(1, round(frame.height * factor)))
            if size[1] > 465:
                raise ValueError('Animation exceeds foot baseline: ' + key)
            canvas = Image.new('RGBA', (640, 512))
            canvas.alpha_composite(frame.resize(size, Image.Resampling.LANCZOS), ((640 - size[0]) // 2, 469 - size[1]))
            add(canvas, f'Characters/{key}-{index}.png', source, 'character', group + 4, frame=index)
        figure = frames[0]
        portrait = figure.crop((0, 0, figure.width, min(figure.height, round(figure.height * 0.47))))
        add(cutout(portrait), f'Portraits/{key}.png', source, 'portrait', group + 4, ppu=512, normal=False)
    for index, frame in enumerate(sheet('new-powered-forms', 2, 2)):
        add(normalized(frame), f'Powered/{index}.png', 'new-powered-forms', 'powered', index + 4)
    for stage, key in enumerate(('ruins', 'factory', 'ice', 'neon', 'fortress', 'tomb', 'tide', 'rift')):
        prefix = 'front-' + key if stage < 5 else key
        source = prefix + '-enemy-roster'
        frames = sheet(source, 4, 2)
        for frame in range(2):
            for kind in range(4):
                figure = frames[frame * 4 + kind]
                add(normalized(figure, height=390), f'Enemies/{key}-{kind}-{frame}.png', source, 'enemy', stage, kind, frame)
        source = prefix + '-boss-forms'
        if stage < 5:
            for index, figure in enumerate(sheet(source, 2, 1)):
                add(normalized(figure, (768, 512), 385), f'Bosses/{key}-{index+1}-0.png', source, 'boss', stage, index + 1)
            source = prefix + '-guardians'
            for index, figure in enumerate(sheet(source, 2, 2)):
                add(normalized(figure, (640, 512), 390), f'Guardians/{key}-{index%2}-{index//2}.png', source, 'guardian', stage, index % 2, index // 2)
        else:
            for index, figure in enumerate(sheet(source, 3, 2)):
                add(normalized(figure, (768, 512), 385), f'Bosses/{key}-{index%3}-{index//3}.png', source, 'boss', stage, index % 3, index // 3)
            source = key + '-background'
            add(Image.open(SOURCE / (source + '.png')).convert('RGB'), f'Backgrounds/{key}.png', source, 'background', stage, ppu=128, normal=False)
        source = prefix + ('-props' if stage < 5 else '-architecture')
        for index, figure in enumerate(sheet(source, 3 if stage < 5 else 2, 2)):
            add(cutout(figure, 640, 512), f'Architecture/{key}-{index}.png', source, 'architecture', stage, index)
        source = prefix + '-effects'
        for index, figure in enumerate(sheet(source, 4, 2)):
            add(cutout(figure), f'Effects/{key}-{index}.png', source, 'effect', stage, index, ppu=512, normal=False)
        if stage < 5:
            source = prefix + '-hazards'
            for index, figure in enumerate(sheet(source, 3, 2)):
                image = normalized(figure, height=300) if index % 3 == 2 else cutout(figure, 640, 512)
                add(image, f'Hazards/{key}-{index%3}-{index//3}.png', source, 'hazard', stage, index % 3, index // 3)
    for index, figure in enumerate(sheet('advanced-weapons', 1, 3)):
        canvas = cutout(figure, 640, 320)
        add(canvas, f'Weapons/{index+6}.png', 'advanced-weapons', 'weapon', index + 6, ppu=480)
    for index, key in enumerate(('laser-sword', 'arc-caster', 'pulse-crossbow')):
        source = key + '-view'
        add(cutout(sheet(source, 1, 1)[0], 768, 768), f'Views/{index+6}.png', source, 'view', index + 6, ppu=256)
    for index, figure in enumerate(sheet('new-character-skills', 4, 2)):
        add(cutout(figure), f'Skills/{index}.png', 'new-character-skills', 'skill', index % 4 + 4, index // 4, ppu=512, normal=False)
    for index, figure in enumerate(sheet('premium-pickups', 4, 2)):
        add(cutout(figure), f'Pickups/{index}.png', 'premium-pickups', 'pickup', index, ppu=512)
    manifest = dict(version='descent-premium-v1', status='processed-awaiting-runtime-review', sourceCount=len({r['original'] for r in records}), assets=records)
    (OUTPUT / 'art-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Processed {len(records)} sprites; originals preserved, native alpha retained.')


def previews():
    REVIEW.mkdir(parents=True, exist_ok=True)
    originals = sorted(SOURCE.glob('*.png'))
    for start in range(0, len(originals), 8):
        page = Image.new('RGB', (1600, 1000), '#162332')
        draw = ImageDraw.Draw(page)
        for index, path in enumerate(originals[start:start + 8]):
            image = Image.open(path).convert('RGBA')
            image.thumbnail((390, 460), Image.Resampling.LANCZOS)
            horizontal, vertical = index % 4 * 400, index // 4 * 500
            page.paste(image, (horizontal + (400 - image.width)//2, vertical + (470 - image.height)//2), image)
            draw.text((horizontal + 7, vertical + 478), path.stem, fill='white')
        page.save(REVIEW / f'originals-{start//8+1}.jpg', quality=92)
    if OUTPUT.exists():
        characters = Image.new('RGB', (1200, 600), '#182737')
        for index, key in enumerate(('kalt', 'veil', 'sera', 'rook')):
            portrait = Image.open(OUTPUT / f'Characters/{key}-0.png').convert('RGBA')
            portrait.thumbnail((290, 540))
            characters.paste(portrait, (index*300 + (300-portrait.width)//2, 25), portrait)
            ImageDraw.Draw(characters).text((index*300+120, 560), key.upper(), fill='white')
        characters.save(REVIEW / 'new-roster.png')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview-only', action='store_true')
    args = parser.parse_args()
    if not args.preview_only:
        process()
    previews()
