"""Remove neighbouring sprite-cell fragments while preserving generated alpha edges."""
import hashlib
import importlib.util
import json
import shutil
from collections import deque
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/Art/Production/PremiumV1'


def clean_figure(image):
    mask = np.asarray(image.getchannel('A')) > 24
    visited = np.zeros(mask.shape, dtype=bool)
    largest = []
    for y, x in zip(*np.where(mask)):
        if visited[y, x]:
            continue
        queue, component = deque([(y, x)]), []
        visited[y, x] = True
        while queue:
            cy, cx = queue.popleft()
            component.append((cy, cx))
            for ny, nx in ((cy-1, cx), (cy+1, cx), (cy, cx-1), (cy, cx+1)):
                if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and not visited[ny, nx]:
                    visited[ny, nx] = True
                    queue.append((ny, nx))
        if len(component) > len(largest):
            largest = component
    if not largest:
        raise ValueError('Empty generated figure')
    keep = np.zeros(mask.shape, dtype=np.uint8)
    for y, x in largest:
        keep[y, x] = 255
    # Retain anti-aliasing, detached strands and glow within six source pixels.
    keep = Image.fromarray(keep).filter(ImageFilter.MaxFilter(13))
    alpha = np.asarray(image.getchannel('A')).copy()
    alpha[np.asarray(keep) == 0] = 0
    result = image.copy()
    result.putalpha(Image.fromarray(alpha))
    return result


def main():
    manifest_path = ASSETS / 'art-manifest.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    backup = ROOT / 'ArtProduction/descent-premium-v1/cutout-backups' / datetime.now().strftime('%Y%m%d_%H%M%S_%f')
    backup.mkdir(parents=True)
    spec = importlib.util.spec_from_file_location('art', ROOT / 'Tools/Prepare-AnimeArt.py')
    art = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(art)
    changes = []
    for key in ('kalt', 'veil', 'sera', 'rook'):
        for frame in range(8):
            path = ASSETS / f'Characters/{key}-{frame}.png'
            normal = path.with_name(path.stem + '-normal.png')
            for original in (path, normal):
                target = backup / original.relative_to(ASSETS)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(original, target)
            clean = clean_figure(Image.open(path).convert('RGBA'))
            clean.save(path)
            art.normal_map(clean).save(normal)
            changes.extend((path, normal))
        figure = Image.open(ASSETS / f'Characters/{key}-0.png').convert('RGBA')
        bounds = figure.getchannel('A').point(lambda value: 255 if value > 24 else 0).getbbox()
        figure = figure.crop(bounds)
        bust = figure.crop((0, 0, figure.width, round(figure.height * .47)))
        bust.thumbnail((480, 480), Image.Resampling.LANCZOS)
        # thumbnail never enlarges. Explicitly fit low-resolution source busts too.
        factor = min(480 / bust.width, 480 / bust.height)
        bust = bust.resize((round(bust.width * factor), round(bust.height * factor)), Image.Resampling.LANCZOS)
        portrait = Image.new('RGBA', (512, 512))
        portrait.alpha_composite(bust, ((512-bust.width)//2, (512-bust.height)//2))
        path = ASSETS / f'Portraits/{key}.png'
        target = backup / path.relative_to(ASSETS)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        portrait.save(path)
        changes.append(path)
    shutil.copy2(manifest_path, backup / manifest_path.name)
    for entry in manifest['assets']:
        entry['sha256'] = hashlib.sha256((ASSETS / entry['path']).read_bytes()).hexdigest()
        if entry['normal']:
            entry['normalSha256'] = hashlib.sha256((ASSETS / entry['normal']).read_bytes()).hexdigest()
    manifest['cutoutReview'] = 'Neighbouring-cell fragments removed; four bust portraits enlarged; originals preserved.'
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'refinedFiles': len(changes), 'backup': str(backup)}, ensure_ascii=False))


if __name__ == '__main__':
    main()
