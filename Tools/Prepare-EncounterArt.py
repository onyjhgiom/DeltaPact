import argparse
import importlib.util
import io
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


spec = importlib.util.spec_from_file_location("anime_art", Path(__file__).with_name("Prepare-AnimeArt.py"))
art = importlib.util.module_from_spec(spec)
spec.loader.exec_module(art)


def enemy_cells(path):
    image = Image.open(path).convert("RGBA")
    alpha = art.np.asarray(image.getchannel("A"))
    if float((alpha < 8).mean()) < 0.2:
        raise ValueError("Native transparency required")
    coverage = (alpha > 32).sum(axis=1)
    split = min(range(460, 562), key=lambda row: (coverage[row], abs(row - 512)))
    if coverage[split] != 0:
        raise ValueError("No horizontal transparent gutter")
    result = []
    for top, bottom in ((0, split), (split, image.height)):
        costs = art.np.convolve((alpha[top:bottom] > 32).sum(axis=0), art.np.ones(5), mode="same")
        edges = [0]
        columns = 2 if path.name == "enemies-neon-v2.png" and top > 0 else 3
        for column in range(1, columns + 1):
            nominal = image.width * column // 4
            choices = [position for position in range(nominal - 170, nominal + 171) if costs[position] == 0]
            if not choices:
                raise ValueError(f"No intact gutter: {path.name}, row {top}, column {column}")
            edges.append(min(choices, key=lambda position: abs(position - nominal)))
        if columns == 3:
            edges.append(image.width)
        for left, right in zip(edges, edges[1:]):
            cell = image.crop((left, top, right, bottom))
            bounds = cell.getchannel("A").point(lambda value: 255 if value > 32 else 0).getbbox()
            if bounds is None or min(bounds[0], bounds[1], cell.width - bounds[2], cell.height - bounds[3]) < 2:
                raise ValueError("Clipped enemy frame")
            result.append((cell, bounds))
    return result


def process(source, output):
    images, records = {}, []
    originals = sorted(source.glob("*.png"))
    preview = Image.new("RGB", (1500, ((len(originals) + 4) // 5) * 330), (30, 36, 48))
    draw = ImageDraw.Draw(preview)
    for index, path in enumerate(originals):
        image = Image.open(path).convert("RGBA")
        image.thumbnail((290, 290))
        location = (index % 5 * 300, index // 5 * 330)
        preview.paste(image, location, image)
        draw.text((location[0] + 5, location[1] + 300), path.stem, fill="white")
    art.write_image(preview, source.parent.parent / ("originals-preview-" + str(len(originals)) + ".png"))

    def add(image, name, category, stage, group, frame, original, ppu):
        images[name] = image
        normal = name.replace(".png", "-normal.png") if category == "enemy" else ""
        if normal:
            images[normal] = art.normal_map(image)
        records.append(dict(path=name, category=category, stage=stage, group=group, frame=frame,
                            normal=normal, pixelsPerUnit=ppu, original=original.name, sourceSha256=art.digest(original)))

    for stage, theme in enumerate(("ruins", "factory", "ice", "neon", "fortress")):
        original = source / ("enemies-neon-v2.png" if theme == "neon" else f"enemies-{theme}.png")
        frames = enemy_cells(original)
        for group in range(4):
            pair_original = source / "neon-elite-v3.png" if theme == "neon" and group == 3 else original
            pair = art.cells(pair_original, 2, 1) if pair_original != original else frames[group * 2:group * 2 + 2]
            factor = min(430 / max(bounds[3] - bounds[1] for _, bounds in pair), 360 / max(bounds[2] - bounds[0] for _, bounds in pair))
            for frame, (image, bounds) in enumerate(pair):
                figure = image.crop(bounds)
                flips = {"ruins": {7}, "factory": set(), "ice": {2, 3}, "neon": {1, 2, 3}, "fortress": {1, 2, 3, 6, 7}}
                if group * 2 + frame in flips[theme]:
                    figure = ImageOps.mirror(figure)
                figure = figure.resize((round(figure.width * factor), round(figure.height * factor)), Image.Resampling.LANCZOS)
                canvas = Image.new("RGBA", (384, 512))
                canvas.alpha_composite(figure, ((384 - figure.width) // 2, 469 - figure.height))
                add(canvas, f"Enemies/{theme}-{group}-{frame}.png", "enemy", stage, group, frame, pair_original, 266.66667)
        original = source / f"effects-{theme}.png"
        for group, (image, bounds) in enumerate(art.cells(original, 2, 2)):
            image = image.crop(bounds)
            if theme == "factory" and group == 0:
                image = ImageOps.mirror(image)
            canvas = Image.new("RGBA", (image.width + 16, image.height + 16))
            canvas.alpha_composite(image, (8, 8))
            add(canvas, f"Effects/{theme}-{group}.png", "effect", stage, group, 0, original, max(canvas.size))
    for name, image in images.items():
        destination = output / name
        if destination.exists():
            with Image.open(destination) as previous:
                if previous.mode != image.mode or previous.size != image.size or previous.tobytes() != image.tobytes():
                    raise ValueError(f"Existing asset differs; use new version: {destination}")
    for record in records:
        for key, path in (("sha256", record["path"]), ("normalSha256", record["normal"])):
            stream = io.BytesIO()
            if path:
                images[path].save(stream, format="PNG")
            record[key] = art.hashlib.sha256(stream.getvalue()).hexdigest() if path else ""
    manifest = dict(version="encounter-v3", status="processed-awaiting-visual-review", assets=records)
    manifest_path = output / "art-manifest.json"
    if manifest_path.exists():
        previous = json.loads(manifest_path.read_text(encoding="utf-8"))
        previous["status"] = manifest["status"]
        if previous != manifest:
            raise ValueError("Manifest differs; use a new version")
    for name, image in images.items():
        art.write_image(image, output / name)
    if not manifest_path.exists():
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    contact = Image.new("RGB", (1440, 5 * 300), (28, 34, 48))
    draw = ImageDraw.Draw(contact)
    for index, record in enumerate(records):
        image = images[record["path"]].copy()
        image.thumbnail((112, 250))
        horizontal, vertical = index % 12 * 120, index // 12 * 300
        contact.paste(image, (horizontal + (120 - image.width) // 2, vertical), image)
        draw.text((horizontal + 2, vertical + 258), Path(record["path"]).stem, fill="white")
    art.write_image(contact, source.parent.parent / "contact-sheet.png")
    return dict(sprites=len(records), textures=len(images), manifest=str(manifest_path))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    arguments = parser.parse_args()
    print(json.dumps(process(arguments.source.resolve(), arguments.output.resolve())))
