import argparse
import hashlib
import io
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_image(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        with Image.open(path) as previous:
            if previous.mode != image.mode or previous.size != image.size or previous.tobytes() != image.tobytes():
                raise ValueError(f"Existing asset differs; create a new version instead: {path}")
        return
    image.save(path)


def cells(path, columns, rows):
    image = Image.open(path).convert("RGBA")
    if image.width % columns or image.height % rows:
        raise ValueError(f"Sheet dimensions do not divide evenly: {path.name}")
    alpha = np.asarray(image.getchannel("A"))
    if float((alpha < 8).mean()) < 0.2:
        raise ValueError(f"Native alpha is missing: {path.name}")
    width, height = image.width // columns, image.height // rows

    def seams(length, count, coverage):
        result = [0]
        cell_size = length // count
        costs = np.convolve(coverage, np.ones(9), mode="same")
        for index in range(1, count):
            nominal = index * cell_size
            candidates = range(nominal - cell_size // 10, nominal + cell_size // 10 + 1)
            position = min(candidates, key=lambda candidate: (costs[candidate], abs(candidate - nominal)))
            if costs[position] != 0:
                raise ValueError(f"No transparent separation between cells: {path.name}")
            result.append(position)
        return result + [length]

    horizontal_edges = seams(image.height, rows, (alpha > 32).sum(axis=1))
    result = []
    for row in range(rows):
        row_alpha = alpha[horizontal_edges[row]:horizontal_edges[row + 1], :]
        vertical_edges = seams(image.width, columns, (row_alpha > 32).sum(axis=0))
        for column in range(columns):
            frame = image.crop((vertical_edges[column], horizontal_edges[row], vertical_edges[column + 1], horizontal_edges[row + 1]))
            width, height = frame.size
            bounds = frame.getchannel("A").point(lambda value: 255 if value > 32 else 0).getbbox()
            if bounds is None or (bounds[2] - bounds[0]) * (bounds[3] - bounds[1]) < width * height * 0.025:
                raise ValueError(f"Missing figure in {path.name}, cell {len(result)}")
            if min(bounds[0], bounds[1], width - bounds[2], height - bounds[3]) < 2:
                raise ValueError(f"Figure touches cell boundary in {path.name}, cell {len(result)}")
            result.append((frame, bounds))
    return result


def normalized_animation(source, columns, rows, target, normal_height=430, crouch_index=-1):
    frames = cells(source, columns, rows)
    base_height = frames[0][1][3] - frames[0][1][1]
    factor = min(normal_height / base_height, (target[0] - 12) / max(bounds[2] - bounds[0] for _, bounds in frames))
    result = []
    for index, (image, bounds) in enumerate(frames):
        figure = image.crop(bounds)
        width, height = max(1, round(figure.width * factor)), max(1, round(figure.height * factor))
        if width > target[0] - 12 or height > 461:
            raise ValueError(f"Inconsistent scale in {source.name}, cell {index}; needs art correction")
        if index != crouch_index and height < normal_height * 0.65:
            raise ValueError(f"Unexpected small figure in {source.name}, cell {index}")
        canvas = Image.new("RGBA", target)
        canvas.alpha_composite(figure.resize((width, height), Image.Resampling.LANCZOS), ((target[0] - width) // 2, 469 - height))
        result.append(canvas)
    return result


def normal_map(image):
    alpha = np.asarray(image.getchannel("A").filter(ImageFilter.GaussianBlur(2)), dtype=float) / 255
    luminance = np.asarray(image.convert("L").filter(ImageFilter.GaussianBlur(1)), dtype=float) / 255
    height = alpha * (0.7 + luminance * 0.3)
    vertical, horizontal = np.gradient(height)
    normal = np.stack((-horizontal * 3, vertical * 3, np.ones_like(height)), axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    encoded = np.round((normal * 0.5 + 0.5) * 255).astype(np.uint8)
    return Image.fromarray(encoded, "RGB")


def process(source, output):
    source, output = source.resolve(), output.resolve()
    characters = ["delta", "vanguard", "wraith", "havoc"]
    themes = ["ruins", "factory", "ice", "neon", "fortress"]
    expected = [name + "-sheet.png" for name in characters]
    expected += ["enemies-sheet.png", "weapons-sheet.png", "effects-sheet.png"]
    expected += ["boss-" + name + "-sheet.png" for name in themes]
    expected += [name + "-background.png" for name in themes]
    selection_path = source / "source-selection.json"
    selection = json.loads(selection_path.read_text(encoding="utf-8")) if selection_path.exists() else {}
    if not isinstance(selection, dict) or any(name not in expected or not isinstance(value, str) or Path(value).name != value or not value.endswith(".png") for name, value in selection.items()):
        raise ValueError("Invalid source selection: expected local PNG filenames")

    def original_path(name):
        return source / selection.get(name, name)

    missing = [name for name in expected if not original_path(name).is_file()]
    if missing:
        raise ValueError("Missing generated originals; no output written: " + ", ".join(missing))
    images = {}
    records = []

    def add(image, name, original, ppu, category, group=0, frame=0, normals=True):
        images[name] = image
        records.append({"path": name, "original": original.name, "sourceSha256": digest(original), "pixelsPerUnit": ppu,
                        "category": category, "group": group, "frame": frame, "normal": name.replace(".png", "-normal.png") if normals else ""})
        if normals:
            images[name.replace(".png", "-normal.png")] = normal_map(image)

    for group, character in enumerate(characters):
        original = original_path(character + "-sheet.png")
        frames = normalized_animation(original, 4, 2, (384, 512), crouch_index=7)
        for frame, image in enumerate(frames):
            add(image, f"Characters/{character}-{frame:02}.png", original, 266.66667, "character", group, frame)
        alpha_bounds = frames[0].getchannel("A").getbbox()
        top = max(0, alpha_bounds[1] - 12)
        portrait = frames[0].crop((64, top, 320, min(512, top + 256))).resize((512, 512), Image.Resampling.LANCZOS)
        add(portrait, f"Portraits/{character}.png", original, 512, "portrait", group, normals=False)

    original = original_path("enemies-sheet.png")
    enemy_cells = cells(original, 4, 2)
    for group in range(4):
        base_height = enemy_cells[group * 2][1][3] - enemy_cells[group * 2][1][1]
        for frame in range(2):
            image, bounds = enemy_cells[group * 2 + frame]
            figure = image.crop(bounds)
            factor = min(430 / base_height, 360 / figure.width)
            figure = figure.resize((round(figure.width * factor), round(figure.height * factor)), Image.Resampling.LANCZOS)
            if figure.height > 461:
                raise ValueError("Enemy pair scale mismatch")
            canvas = Image.new("RGBA", (384, 512))
            canvas.alpha_composite(figure, ((384 - figure.width) // 2, 469 - figure.height))
            add(canvas, f"Enemies/enemy-{group}-{frame}.png", original, 266.66667, "enemy", group, frame)

    for group, theme in enumerate(themes):
        original = original_path("boss-" + theme + "-sheet.png")
        for frame, image in enumerate(normalized_animation(original, 2, 2, (512, 512), normal_height=380)):
            add(image, f"Bosses/{theme}-{frame}.png", original, 288.58, "boss", group, frame)
        original = original_path(theme + "-background.png")
        background = Image.open(original).convert("RGB")
        if background.width < 1536 or abs(background.width / background.height - 16 / 9) > 0.02:
            raise ValueError(f"Background must be at least 1536px, 16:9: {original.name}")
        add(background, f"Backgrounds/{theme}.png", original, 128, "background", group, normals=False)

    original = original_path("weapons-sheet.png")
    weapon_widths = [0.9, 0.9, 1.2]
    for group, (image, bounds) in enumerate(cells(original, 1, 3)):
        figure = image.crop(bounds)
        padded = Image.new("RGBA", (figure.width + 24, figure.height + 24))
        padded.alpha_composite(figure, (12, 12))
        add(padded, f"Weapons/weapon-{group}.png", original, padded.width / weapon_widths[group], "weapon", group)

    original = original_path("effects-sheet.png")
    for group, (image, bounds) in enumerate(cells(original, 4, 2)):
        figure = image.crop(bounds)
        add(figure, f"Effects/effect-{group}.png", original, max(figure.size), "effect", group, normals=False)

    for name, image in images.items():
        destination = output / name
        if destination.exists():
            with Image.open(destination) as previous:
                if previous.mode != image.mode or previous.size != image.size or previous.tobytes() != image.tobytes():
                    raise ValueError(f"Preflight: refusing to overwrite differing asset {destination}")
    def image_digest(image):
        encoded = io.BytesIO()
        image.save(encoded, format="PNG")
        return hashlib.sha256(encoded.getvalue()).hexdigest()

    for record in records:
        path = output / record["path"]
        record["sha256"] = digest(path) if path.exists() else image_digest(images[record["path"]])
        normal = record["normal"]
        record["normalSha256"] = (digest(output / normal) if (output / normal).exists() else image_digest(images[normal])) if normal else ""
    manifest = {"version": "anime-v1", "status": "processed-awaiting-visual-review", "generatedBy": "OpenAI-compatible Image API via bundled imagegen CLI", "assets": records}
    manifest_path = output / "art-manifest.json"
    content = json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
    if manifest_path.exists():
        previous = json.loads(manifest_path.read_text(encoding="utf-8"))
        if previous.get("status") not in {"processed-awaiting-visual-review", "visually-reviewed"}:
            raise ValueError("Unknown existing manifest review status")
        previous["status"] = manifest["status"]
        if previous != manifest:
            raise ValueError("Existing manifest differs; no files written; create a new asset version")
    preview = Image.new("RGB", (1024, ((len(records) + 7) // 8) * 160), (24, 31, 48))
    draw = ImageDraw.Draw(preview)
    for index, record in enumerate(records):
        thumbnail = images[record["path"]].convert("RGBA")
        thumbnail.thumbnail((120, 132))
        horizontal, vertical = index % 8 * 128, index // 8 * 160
        preview.paste(thumbnail, (horizontal + (128 - thumbnail.width) // 2, vertical), thumbnail)
        draw.text((horizontal + 2, vertical + 135), Path(record["path"]).stem[:20], fill=(214, 229, 255))
    preview_path = source.parent.parent / "contact-sheet.png"
    if preview_path.exists():
        with Image.open(preview_path) as previous:
            if previous.mode != preview.mode or previous.size != preview.size or previous.tobytes() != preview.tobytes():
                raise ValueError("Existing contact sheet differs; no files written; create a new asset version")
    for name, image in images.items():
        write_image(image, output / name)
    if not manifest_path.exists():
        manifest_path.write_text(content, encoding="utf-8")
    write_image(preview, preview_path)
    return {"sprites": len(records), "texturesWithNormals": len(images), "output": str(output), "contactSheet": str(preview_path)}


def main():
    parser = argparse.ArgumentParser(description="Validate and process generated PNGs only; never calls an image API.")
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    arguments = parser.parse_args()
    print(json.dumps(process(arguments.source, arguments.output)))


if __name__ == "__main__":
    main()
