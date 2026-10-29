"""Slice generated native-alpha atlases; preserve sources and record provenance."""
import importlib.util
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('art_helpers', ROOT / 'Tools/Prepare-AnimeArt.py')
helpers = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helpers)
records = []
review = []

def atlas_cells(path, columns, rows):
    image=Image.open(path).convert('RGBA')
    alpha=np.asarray(image.getchannel('A'))
    if float((alpha<8).mean())<0.2:
        raise ValueError(f'Native alpha missing: {path}')
    def seams(length,count,coverage):
        edges=[0]
        costs=np.convolve(coverage,np.ones(3),mode='same')
        for index in range(1,count):
            nominal=length*index/count
            candidates=range(round(nominal-length/count*.25),round(nominal+length/count*.25))
            chosen=min(candidates,key=lambda value:(costs[value],abs(value-nominal)))
            if costs[chosen]>3:
                raise ValueError(f'Overlapping solid silhouettes: {path.name} at {chosen}')
            edges.append(chosen)
        return edges+[length]
    horizontal=seams(image.height,rows,(alpha>224).sum(axis=1))
    frames=[]
    for row in range(rows):
        vertical=seams(image.width,columns,(alpha[horizontal[row]:horizontal[row+1]]>224).sum(axis=0))
        for column in range(columns):
            frame=image.crop((vertical[column],horizontal[row],vertical[column+1],horizontal[row+1]))
            bounds=frame.getchannel('A').point(lambda value:255 if value>32 else 0).getbbox()
            if bounds is None: raise ValueError(f'Empty cell: {path.name}')
            frames.append((frame,bounds))
    return frames

def add(image, folder, name, source, stage, kind, slot=0, frame=0, normals=False):
    target = folder / name
    helpers.write_image(image, target)
    entry = dict(path=name, stage=stage, kind=kind, slot=slot, frame=frame,
                 pixelsPerUnit=256, sha256=helpers.digest(target), source=source.relative_to(ROOT).as_posix(),
                 sourceSha256=helpers.digest(source), transparency='opaque-background' if kind=='background' else 'native-alpha')
    if normals:
        normal = target.with_name(target.stem + '-normal.png')
        helpers.write_image(helpers.normal_map(image), normal)
        entry.update(normal=str(normal.relative_to(folder)).replace('\\', '/'), normalSha256=helpers.digest(normal))
    records.append(entry)
    review.append((image, f'{stage+1:02} {kind} {slot}/{frame}'))

def fit(frame, bounds, size, baseline=False, factor=None):
    figure = frame.crop(bounds)
    factor = factor or min((size[0]-48)/figure.width, (size[1]-48)/figure.height)
    figure = figure.resize((max(1, round(figure.width*factor)), max(1, round(figure.height*factor))), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', size)
    canvas.alpha_composite(figure, ((size[0]-figure.width)//2, 469-figure.height if baseline else (size[1]-figure.height)//2))
    return canvas

def manifest(folder, version):
    (folder / 'art-manifest.json').write_text(json.dumps(dict(version=version, requestedModel='gpt-image-2.5',
        provider='user-configured compatible gateway; backend model identity not independently verified',
        assets=records), ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

def contact(folder, stem):
    columns, cell = 6, 224
    rows = (len(review)+columns-1)//columns
    canvas = Image.new('RGB', (columns*cell, rows*160), '#182334')
    draw = ImageDraw.Draw(canvas)
    for index, (image, label) in enumerate(review):
        thumbnail = image.convert('RGBA'); thumbnail.thumbnail((cell-12,132), Image.Resampling.LANCZOS)
        x, y = index%columns*cell, index//columns*160
        canvas.paste(thumbnail, (x+(cell-thumbnail.width)//2, y+(132-thumbnail.height)//2), thumbnail)
        draw.text((x+6,y+137),label,fill='#cce7ee')
    canvas.save(folder / (stem+'.jpg'), quality=93)

swim = ROOT/'Assets/Art/Production/SwimAnimeV2'
swim.mkdir(parents=True,exist_ok=True)
layouts = json.loads((ROOT/'ArtProduction/swim-anime-v1/prompts/layout.json').read_text(encoding='utf-8'))
for layout in layouts:
    source = ROOT/'output/imagegen/swim-anime-v1'/layout['file']
    stage, kind = layout['stage'], layout['kind']
    if layout['opaque']:
        add(Image.open(source).convert('RGB'),swim,f'stage-{stage+1:02}/background.png',source,stage,'background')
        continue
    frames=atlas_cells(source,layout['columns'],layout['rows'])
    if kind in ('boss','enemies'):
        columns=layout['columns']
        for column in range(columns):
            pair=[frames[column],frames[columns+column]]
            size=(768,512) if kind=='boss' else (384,512)
            factor=min(430/max(b[3]-b[1] for _,b in pair), (size[0]-48)/max(b[2]-b[0] for _,b in pair))
            slot=column if kind=='boss' else [0,1,3,2][column]
            for frame,(image,bounds) in enumerate(pair):
                add(fit(image,bounds,size,True,factor),swim,f'stage-{stage+1:02}/{kind}-{slot}-{frame}.png',source,stage,kind,slot,frame,True)
    else:
        for index,(image,bounds) in enumerate(frames):
            size=(768,512) if kind=='ultimate' else (512,512)
            add(fit(image,bounds,size),swim,f'stage-{stage+1:02}/{kind}-{index}.png',source,stage,kind,index,0,kind in ('architecture','ornaments'))
manifest(swim,'swim-anime-v2'); contact(swim,'review')
print('Swim sprites:',len(records))
records=[]; review=[]
tech=ROOT/'Assets/Art/Production/BossTechniquesV1'
tech.mkdir(parents=True,exist_ok=True)
for stage in range(10):
    source=ROOT/f'output/imagegen/boss-techniques-v1/stage-{stage+1:02}-techniques.png'
    frames=atlas_cells(source,3,2)
    for slot in range(3):
        for frame,label in enumerate(('release','impact')):
            image,bounds=frames[frame*3+slot]
            add(fit(image,bounds,(512,512)),tech,f'stage-{stage+1:02}/skill-{slot+1}-{label}.png',source,stage,'technique',slot,frame)
manifest(tech,'boss-techniques-v1'); contact(tech,'review')
print('Technique sprites:',len(records))
