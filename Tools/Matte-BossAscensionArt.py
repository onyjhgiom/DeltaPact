"""Extract painted checkerboard backgrounds from generated art; never generate art."""
from pathlib import Path
import json
import numpy as np
from scipy import ndimage
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = root / 'output/imagegen/boss-ascension-v2'
dest = source / 'matted'
dest.mkdir(exist_ok=True)
records = []
for path in sorted(source.glob('*.png')):
    im = Image.open(path).convert('RGBA')
    rgba = np.asarray(im).copy()
    native = float((rgba[:,:,3] < 8).mean())
    if native > .15:
        records.append(dict(file=path.name,method='native-alpha',transparent=native))
        continue
    rgb = rgba[:,:,:3].astype(float)
    luminance = rgb.mean(axis=2)
    chroma = rgb.max(axis=2)-rgb.min(axis=2)
    # Sample the sheet perimeter, which is explicit empty gutter in these designs.
    edge = np.zeros(luminance.shape, dtype=bool)
    edge[:8,:]=True; edge[-8:,:]=True; edge[:,:8]=True; edge[:,-8:]=True
    neutral_edge = luminance[edge & (chroma < 16)]
    floor = max(125, min(205, float(np.percentile(neutral_edge, 3))-7))
    candidate = (chroma < 19) & (luminance >= floor)
    # Connected neutral backdrop preserves enclosed white ceramic plates and flash cores.
    background = ndimage.binary_propagation(edge & candidate, mask=candidate)
    labels, count = ndimage.label(candidate & ~background)
    if count:
        sizes=np.bincount(labels.ravel())
        # Isolated large empty holes have the same fine chess pattern as the exterior.
        gradient=np.abs(np.diff(luminance,axis=1,prepend=luminance[:,:1]))
        for label in np.flatnonzero(sizes > 60):
            if not label: continue
            region=labels==label
            levels=luminance[region]
            if np.percentile(levels,85)-np.percentile(levels,15)>12 and (gradient[region]>7).mean()>.08:
                background |= region
    foreground = ~background
    components, count = ndimage.label(foreground)
    sizes=np.bincount(components.ravel())
    foreground &= sizes[components] >= (14 if 'ultimate' in path.name else 35)
    alpha=ndimage.gaussian_filter(foreground.astype(float), .45)
    alpha[~ndimage.binary_dilation(foreground)]=0
    rgba[:,:,3]=np.clip(alpha*255,0,255).astype('uint8')
    rgba[rgba[:,:,3]==0,:3]=0
    target=dest/path.name
    Image.fromarray(rgba).save(target)
    records.append(dict(file=path.name,method='connected-checkerboard-matte',floor=round(floor,2),
        transparent=float((rgba[:,:,3]<8).mean()),sourcePreserved=True))
(dest/'matting-report.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
print(json.dumps(records))
