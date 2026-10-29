import argparse
import hashlib
import io
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / "ArtProduction/combat-polish-v1"
SOURCE = ROOT / "output/imagegen/combat-polish-v1"
OUTPUT = ROOT / "Assets/Art/Production/CombatPolish"
VERSION = "combat-polish-v1"
THEMES = ("ruins", "factory", "ice", "neon", "fortress")
POWERED_KEYS = ("delta", "vanguard", "wraith", "havoc")
ROSTER_KEYS = ("aurora", "ember")
BASELINE = 469
PPU = 256


def digest(data):
    return hashlib.sha256(data).hexdigest()


def encoded_image(image):
    stream = io.BytesIO()
    image.save(stream, format="PNG", compress_level=9)
    return stream.getvalue()


def guid_for(path):
    return digest((VERSION + ":" + path).encode("utf-8"))[:32]


def load_sheet(filename, columns, rows, empty=()):
    path = SOURCE / filename
    with Image.open(path) as original:
        if original.mode != "RGBA":
            raise ValueError("Native RGBA required: " + filename)
        image = original.copy()
    if image.width % columns or image.height % rows:
        raise ValueError("Unexpected sheet dimensions: " + filename)
    pixels = np.asarray(image).copy()
    alpha = pixels[:, :, 3]
    if float((alpha < 8).mean()) < 0.2 or float((alpha > 192).mean()) < 0.03:
        raise ValueError("Missing native transparent/opaque regions: " + filename)
    pixels[alpha <= 16] = 0
    image = Image.fromarray(pixels)
    mask = pixels[:, :, 3] > 16

    def seams(length, count, coverage):
        edges = [0]
        size = length / count
        costs = np.convolve(coverage, np.ones(3), mode="same")
        for index in range(1, count):
            nominal = round(index * size)
            radius = round(size * 0.16)
            candidates = [position for position in range(max(2, nominal - radius), min(length - 2, nominal + radius + 1)) if costs[position] == 0]
            if not candidates:
                raise ValueError(f"No intact transparent gutter: {filename}, boundary {index}")
            edges.append(min(candidates, key=lambda position: abs(position - nominal)))
        return edges + [length]

    horizontal = seams(image.height, rows, mask.sum(axis=1))
    result = []
    for row in range(rows):
        top, bottom = horizontal[row:row + 2]
        vertical = seams(image.width, columns, mask[top:bottom].sum(axis=0))
        for column in range(columns):
            left, right = vertical[column:column + 2]
            cell = image.crop((left, top, right, bottom))
            bounds = cell.getchannel("A").getbbox()
            index = row * columns + column
            if index in empty:
                if bounds is not None:
                    raise ValueError("Reserved empty cell contains artwork: " + filename)
                result.append(None)
                continue
            if bounds is None or (bounds[2] - bounds[0]) * (bounds[3] - bounds[1]) < cell.width * cell.height * 0.025:
                raise ValueError(f"Missing subject: {filename}, cell {index}")
            if min(bounds[0], bounds[1], cell.width - bounds[2], cell.height - bounds[3]) < 2:
                raise ValueError(f"Subject touches cut: {filename}, cell {index}")
            foot_mask = np.asarray(cell.getchannel("A")) > 64
            band_top = max(bounds[1], bounds[3] - max(4, round((bounds[3] - bounds[1]) * 0.05)))
            horizontal_foot = np.where(foot_mask[band_top:bounds[3]])[1]
            anchor = (float(horizontal_foot.min() + horizontal_foot.max()) + 1) / 2
            record = dict(original=filename, sourceSha256=digest(path.read_bytes()), sourceSize=list(image.size),
                          sourceCell=index, sourceRect=[left, top, right, bottom], sourceAlphaBounds=list(bounds),
                          sourceSoleBoundaryTop=bounds[3], sourceRootX=anchor, alphaDiscardAtOrBelow=16)
            result.append((cell, bounds, record))
    return result


def normalize_group(cells, canvas=(512, 512), height=430):
    base = cells[0][1]
    scale = min(height / (base[3] - base[1]), 461 / max(bounds[3] - bounds[1] for _, bounds, _ in cells))
    for _, bounds, record in cells:
        left_extent = record["sourceRootX"] - bounds[0]
        right_extent = bounds[2] - record["sourceRootX"]
        scale = min(scale, (canvas[0] / 2 - 12) / max(left_extent, right_extent))
    result = []
    for cell, bounds, source_record in cells:
        figure = cell.crop(bounds)
        size = (max(1, round(figure.width * scale)), max(1, round(figure.height * scale)))
        figure = figure.resize(size, Image.Resampling.LANCZOS)
        left = round(canvas[0] / 2 - (source_record["sourceRootX"] - bounds[0]) * scale)
        top = BASELINE - size[1]
        if left < 2 or left + size[0] > canvas[0] - 2 or top < 2:
            raise ValueError("Normalization would clip a subject")
        normalized = Image.new("RGBA", canvas)
        normalized.alpha_composite(figure, (left, top))
        record = dict(source_record, sourceScale=scale, pastePosition=[left, top],
                      footBaselineTop=BASELINE, rootAnchorPixels=[canvas[0] / 2, BASELINE])
        result.append((normalized, record))
    return result


def normal_map(image):
    alpha = np.asarray(image.getchannel("A").filter(ImageFilter.GaussianBlur(2)), dtype=float) / 255
    luminance = np.asarray(image.convert("L").filter(ImageFilter.GaussianBlur(1)), dtype=float) / 255
    surface = alpha * (0.7 + 0.3 * luminance)
    vertical, horizontal = np.gradient(surface)
    vectors = np.stack((-horizontal * 3, vertical * 3, np.ones_like(surface)), axis=-1)
    vectors /= np.linalg.norm(vectors, axis=-1, keepdims=True)
    return Image.fromarray(np.round((vectors * 0.5 + 0.5) * 255).astype(np.uint8))


def texture_meta(path, ppu, normal=False, normal_path=""):
    guid = guid_for(path)
    secondary = "    secondaryTextures: []\n"
    if normal_path:
        secondary = f"    secondaryTextures:\n    - texture: {{fileID: 2800000, guid: {guid_for(normal_path)}, type: 3}}\n      name: _NormalMap\n"
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: {0 if normal else 1}
    linearTexture: 0
    alphaTestReferenceValue: 0.5
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 50
  spriteMode: {0 if normal else 1}
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: {0 if normal else 1}
  textureType: {1 if normal else 8}
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {guid_for(path + ':sprite')}
    internalID: 0
{secondary}    nameFileIdTable: {{}}
  userData: combat-polish-v1; native-alpha; reviewed-import-only
  assetBundleName:
  assetBundleVariant:
""".encode("utf-8")


def checker_preview(items, columns=4, tile=(320, 350)):
    canvas = Image.new("RGB", (columns * tile[0], math.ceil(len(items) / columns) * tile[1]), (27, 34, 47))
    draw = ImageDraw.Draw(canvas)
    for index, (label, image) in enumerate(items):
        left, top = index % columns * tile[0], index // columns * tile[1]
        for row in range(0, tile[1] - 28, 16):
            for column in range(0, tile[0], 16):
                color = (55, 65, 80) if (row // 16 + column // 16) % 2 else (75, 86, 100)
                draw.rectangle((left + column, top + row, left + column + 15, top + row + 15), fill=color)
        thumb = image.copy()
        thumb.thumbnail((tile[0] - 16, tile[1] - 40), Image.Resampling.LANCZOS)
        canvas.paste(thumb, (left + (tile[0] - thumb.width) // 2, top + (tile[1] - 28 - thumb.height) // 2), thumb)
        draw.text((left + 6, top + tile[1] - 22), label, fill=(240, 244, 255))
    return canvas


def prepare(include_roster):
    files, records, previews = {}, [], {}

    def add(image, relative, kind, key, source_record, phase=0, frame=0, ppu=PPU):
        content = encoded_image(image)
        normal_path = relative.replace(".png", "-normal.png") if kind != "portrait" else ""
        normal_bytes = encoded_image(normal_map(image)) if normal_path else b""
        files[OUTPUT / relative] = content
        files[OUTPUT / (relative + ".meta")] = texture_meta(relative, ppu, normal_path=normal_path)
        if normal_path:
            files[OUTPUT / normal_path] = normal_bytes
            files[OUTPUT / (normal_path + ".meta")] = texture_meta(normal_path, ppu, normal=True)
        alpha = np.asarray(image.getchannel("A"))
        bounds = image.getchannel("A").getbbox()
        entry = dict(source_record, path=relative, kind=kind, key=key, phase=phase, frame=frame,
                     sha256=digest(content), guid=guid_for(relative), normalPath=normal_path,
                     normalSha256=digest(normal_bytes) if normal_bytes else "", pixelsPerUnit=ppu,
                     width=image.width, height=image.height, pivot=[0.5, 0.5], mesh="FullRect",
                     boundsHeightUnits=image.height / ppu, outputAlphaBounds=list(bounds),
                     clearFraction=float((alpha == 0).mean()), solidFraction=float((alpha > 192).mean()),
                     alphaFootBoundaryTop=int(np.where(alpha > 32)[0].max()) + 1)
        records.append(entry)
        previews.setdefault(kind, []).append((relative, image))

    first = load_sheet("boss-forms-a.png", 3, 2)
    second = load_sheet("boss-forms-b.png", 2, 2)
    for theme_index, key in enumerate(THEMES):
        cells = [first[theme_index], first[theme_index + 3]] if theme_index < 3 else [second[theme_index - 3], second[theme_index - 1]]
        for phase, (image, source_record) in enumerate(normalize_group(cells, (640, 512), 360), start=2):
            add(image, f"Bosses/{key}-form-{phase}.png", "boss-form", key, source_record, phase=phase)

    powered = load_sheet("powered-players.png", 2, 2)
    for index, key in enumerate(POWERED_KEYS):
        image, source_record = normalize_group([powered[index]])[0]
        add(image, f"Characters/{key}-powered.png", "powered", key, source_record)

    walls = load_sheet("solid-walls.png", 3, 2, empty=(5,))
    for index, key in enumerate(THEMES):
        cell, bounds, source_record = walls[index]
        figure = cell.crop(bounds)
        scale = 500 / max(figure.size)
        size = (max(1, round(figure.width * scale)), max(1, round(figure.height * scale)))
        figure = figure.resize(size, Image.Resampling.LANCZOS)
        image = Image.new("RGBA", (size[0] + 4, size[1] + 4))
        image.alpha_composite(figure, (2, 2))
        add(image, f"Walls/{key}.png", "wall", key, dict(source_record, sourceScale=scale, pastePosition=[2, 2]))

    if include_roster:
        designs = load_sheet("new-character-designs.png", 2, 2)
        for index, key in enumerate(ROSTER_KEYS):
            body, body_record = normalize_group([designs[index * 2]])[0]
            add(body, f"Characters/{key}-body.png", "body", key, body_record)
            image, source_record = normalize_group([designs[index * 2 + 1]])[0]
            add(image, f"Characters/{key}-powered.png", "powered", key, source_record)
            upper = max(0, body.getchannel("A").getbbox()[1] - 8)
            portrait = body.crop((128, upper, 384, upper + 256)).resize((512, 512), Image.Resampling.LANCZOS)
            add(portrait, f"Portraits/{key}.png", "portrait", key, dict(body_record, portraitCrop=[128, upper, 384, upper + 256]), ppu=512)
            animation = load_sheet(key + "-animation.png", 4, 2)
            for frame, (image, source_record) in enumerate(normalize_group(animation)):
                add(image, f"Characters/{key}-{frame:02}.png", "animation", key, source_record, frame=frame)

    for kind, items in previews.items():
        columns = 5 if kind in ("boss-form", "wall") else 4
        if kind == "powered":
            original = [item for item in items if any(item[0] == f"Characters/{key}-powered.png" for key in POWERED_KEYS)]
            expanded = [item for item in items if item not in original]
            files[PRODUCTION / "previews" / "powered.png"] = encoded_image(checker_preview(original))
            if expanded:
                files[PRODUCTION / "previews" / "new-powered.png"] = encoded_image(checker_preview(expanded, 2))
        elif kind == "animation":
            for key in ROSTER_KEYS:
                selected = [item for item in items if item[0].startswith("Characters/" + key + "-")]
                files[PRODUCTION / "previews" / (key + "-animation.png")] = encoded_image(checker_preview(selected))
        else:
            files[PRODUCTION / "previews" / (kind + ".png")] = encoded_image(checker_preview(items, columns))

    review_path = PRODUCTION / "visual-review.json"
    review = json.loads(review_path.read_text(encoding="utf-8")) if review_path.is_file() else {}
    approvals = {entry["path"]: entry for entry in review.get("assets", [])}
    reviewed = all(approvals.get(entry["path"], {}).get("sha256") == entry["sha256"] and approvals[entry["path"]].get("decision") == "approved" for entry in records)
    sources = {entry["original"]: entry["sourceSha256"] for entry in records}
    reviewed = reviewed and review.get("sources") == sources
    manifest = dict(version=VERSION, status="visually-reviewed" if reviewed else "processed-awaiting-visual-review",
                    generator="bundled image_gen.py; requested gpt-image-2.5; gateway underlying model unverified",
                    baselineTop=BASELINE, pixelsPerUnit=PPU, mesh="FullRect", sources=sources,
                    normals="Derived alpha/luminance approximate normal maps; not AI-generated geometry",
                    bindingPolicy="Existing four callsign prefixes; roster length unrestricted. Optional original AURORA/EMBER only when included.",
                    assets=records)
    manifest_bytes = (json.dumps(manifest, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    manifest_path = OUTPUT / "art-manifest.json"
    files[manifest_path] = manifest_bytes
    files[PRODUCTION / "processed-manifest.json"] = manifest_bytes

    for path, content in files.items():
        if not (path.is_relative_to(OUTPUT) or path.is_relative_to(PRODUCTION)):
            raise ValueError("Write outside allowed new art directories")
        if path.exists() and path.read_bytes() != content:
            if path.name not in ("art-manifest.json", "processed-manifest.json"):
                raise ValueError("Existing asset differs; no files written: " + str(path))
            previous = json.loads(path.read_text(encoding="utf-8"))
            if previous.get("version") != VERSION:
                raise ValueError("Foreign manifest preserved")
            previous_assets = {entry["path"]: entry["sha256"] for entry in previous["assets"]}
            if any(entry["path"] in previous_assets and previous_assets[entry["path"]] != entry["sha256"] for entry in records):
                raise ValueError("Previously processed sprite changed")
    for path, content in files.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        if not path.exists() or path.read_bytes() != content:
            path.write_bytes(content)
    print(json.dumps(dict(status=manifest["status"], sprites=len(records), sources=len(sources), output=str(OUTPUT)), ensure_ascii=False))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Offline native-alpha art slicing; never starts Unity or mutates legacy assets.")
    parser.add_argument("--include-roster", action="store_true", help="Also require and prepare the original AURORA/EMBER designs and eight-pose sheets.")
    prepare(parser.parse_args().include_roster)
