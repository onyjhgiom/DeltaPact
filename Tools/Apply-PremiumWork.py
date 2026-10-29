"""Copy the reviewed staging changes into Unity, backing up changed files first."""
import hashlib
import json
import shutil
from datetime import datetime
from pathlib import Path

from PIL import Image

stage = Path(__file__).resolve().parents[1]
project = Path('C:/UnityProjects/MoriEchoes')
assert stage != project and (project / 'ProjectSettings/ProjectVersion.txt').is_file()
stamp = datetime.now().strftime('%Y%m%d_%H%M%S')
backup = project / 'Validation' / ('PremiumBackup_' + stamp)
manifest = json.loads((stage / 'Assets/Art/Production/PremiumV1/art-manifest.json').read_text(encoding='utf-8'))
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
for entry in manifest['assets']:
    path = stage / 'Assets/Art/Production/PremiumV1' / entry['path']
    assert sha(path) == entry['sha256'], path
    with Image.open(path) as image:
        image.load()
        if entry['kind'] != 'background':
            assert 'A' in image.getbands() or 'transparency' in image.info, path
            low, high = image.convert('RGBA').getchannel('A').getextrema()
            assert low == 0 and high > 200, path
        else:
            assert image.mode in ('RGB', 'P'), path
    if entry['normal']:
        normal = path.parent / Path(entry['normal']).name
        assert sha(normal) == entry['normalSha256']
        assert Image.open(normal).mode == 'RGB'
files = list((stage / 'Assets').rglob('*'))
files += list((stage / 'Tools').glob('*.py'))
files += list((stage / 'ArtProduction/descent-premium-v1/prompts').glob('*'))
files += list((stage / 'ArtProduction/descent-premium-v1/review').glob('*'))
changes = []
for source in files:
    if not source.is_file() or '__pycache__' in source.parts:
        continue
    relative = source.relative_to(stage)
    destination = project / relative
    previous = sha(destination) if destination.exists() else None
    digest = sha(source)
    if previous == digest:
        continue
    if destination.exists():
        preserved = backup / relative
        preserved.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(destination, preserved)
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    changes.append(dict(path=relative.as_posix(), status='modified' if previous else 'added', before=previous, after=digest))
report = dict(time=stamp, backup=str(backup), generatedSourceCount=manifest['sourceCount'],
              sprites=len(manifest['assets']), normals=sum(bool(e['normal']) for e in manifest['assets']),
              validation='PNG decoding, native alpha, RGB normals and SHA256 verified; Unity import pending', changes=changes)
backup.mkdir(parents=True, exist_ok=True)
(backup / 'file-changes.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
delivery = project / 'ArtProduction/descent-premium-v1'
(delivery / 'file-changes.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({key:value for key,value in report.items() if key != 'changes'}, ensure_ascii=True))
print('Applied ' + str(len(changes)) + ' changed files; existing versions backed up; no packaging performed.')
