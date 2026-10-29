"""Crop the actual generated RGBA sheets, preserving native alpha and provenance."""
import hashlib
import importlib.util
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
source = root / 'output/imagegen/boss-ascension-v2'
output = root / 'Assets/Art/Production/BossAscensionV2'
review = root / 'ArtProduction/boss-ascension-v2/review'
output.mkdir(parents=True, exist_ok=True)
review.mkdir(parents=True, exist_ok=True)
spec = importlib.util.spec_from_file_location('anime', root / 'Tools/Prepare-AnimeArt.py')
anime = importlib.util.module_from_spec(spec)
spec.loader.exec_module(anime)
themes = json.loads((root/'ArtProduction/boss-ascension-v2/prompts/themes.json').read_text(encoding='utf-8'))
assets = []
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
for theme in themes:
    key, group = theme['key'], theme['stage']
    for kind, suffix, columns, rows in [('ornament','ornaments',4,3),('ultimate','ultimate',2,2)]:
        original = source / f'{key}-{suffix}.png'
        processed = source / 'matted' / original.name
        image = Image.open(processed if processed.exists() else original).convert('RGBA')
        if (np.asarray(image.getchannel('A')) < 8).mean() < 0.15:
            raise ValueError('Native transparency absent: '+original.name)
        alpha = np.asarray(image.getchannel('A')) > 32
        def seams(length, count, coverage):
            costs = np.convolve(coverage, np.ones(5), mode='same')
            result = [0]
            for index in range(1, count):
                nominal = round(length * index / count)
                reach = round(length / count * .27)
                result.append(min(range(nominal-reach,nominal+reach),key=lambda p:(costs[p],abs(p-nominal))))
            return result + [length]
        ys = seams(image.height,rows,alpha.sum(axis=1))
        for row in range(rows):
            xs = seams(image.width,columns,alpha[ys[row]:ys[row+1]].sum(axis=0))
            for column in range(columns):
                cell = image.crop((xs[column],ys[row],xs[column+1],ys[row+1]))
                bounds = cell.getchannel('A').point(lambda a:255 if a>20 else 0).getbbox()
                if not bounds: raise ValueError('Empty cell: '+original.name)
                figure = cell.crop(bounds)
                size = (512,512) if kind=='ornament' else (1024,768)
                margin = 32 if kind=='ornament' else 45
                scale = min((size[0]-2*margin)/figure.width,(size[1]-2*margin)/figure.height)
                figure=figure.resize((max(1,round(figure.width*scale)),max(1,round(figure.height*scale))),Image.Resampling.LANCZOS)
                canvas=Image.new('RGBA',size)
                canvas.alpha_composite(figure,((size[0]-figure.width)//2,(size[1]-figure.height)//2))
                slot,frame=(column,row) if kind=='ornament' else (0,row*2+column)
                relative=f'{suffix}/{key}-{frame}-{slot}.png'
                target=output/relative; target.parent.mkdir(parents=True,exist_ok=True)
                if target.exists(): raise FileExistsError(target)
                anime.write_image(canvas,target)
                normal=relative.replace('.png','-normal.png') if kind=='ornament' else ''
                if normal: anime.write_image(anime.normal_map(canvas),output/normal)
                assets.append(dict(path=relative,kind=kind,group=group,slot=slot,frame=frame,pixelsPerUnit=256,
                    sha256=sha(target),normal=normal,normalSha256=sha(output/normal) if normal else '',
                    original=original.name,sourceSha256=sha(original),
                    matte=('matted/'+original.name) if processed.exists() else '',
                    matteSha256=sha(processed) if processed.exists() else ''))
    overview=Image.new('RGB',(1440,900),(17,23,34)); draw=ImageDraw.Draw(overview)
    draw.text((20,12),f'{key.upper()} / tangible phase equipment + executed ultimate',fill='white')
    for index,entry in enumerate(a for a in assets if a['group']==group):
        figure=Image.open(output/entry['path']); figure.thumbnail((330,195),Image.Resampling.LANCZOS)
        x=(index%4)*360+(360-figure.width)//2; y=40+(index//4)*211+(195-figure.height)//2
        overview.paste(figure,(x,y),figure)
    overview.save(review/f'{key}-review.jpg',quality=92)
manifest=dict(version='boss-ascension-v2',status='processed-awaiting-unity-import',
    generator='Bundled imagegen CLI via user-specified gateway; requested gpt-image-2.5; gateway backend not independently verified. RGB checkerboard backgrounds locally matted; originals retained.',
    sourceImages=16,ornaments=96,ultimateFrames=32,assets=assets)
(output/'art-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({"sprites":len(assets),"ornaments":96,"ultimateFrames":32,"normals":96},ensure_ascii=False))
