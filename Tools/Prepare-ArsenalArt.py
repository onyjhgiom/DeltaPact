import argparse
import importlib.util
import io
import json
from pathlib import Path

from PIL import Image, ImageDraw


helper_spec = importlib.util.spec_from_file_location("anime_art", Path(__file__).with_name("Prepare-AnimeArt.py"))
art = importlib.util.module_from_spec(helper_spec)
helper_spec.loader.exec_module(art)


def cropped(frame, padding=8):
    image, bounds = frame
    image = image.crop(bounds)
    canvas = Image.new("RGBA", (image.width + padding * 2, image.height + padding * 2))
    canvas.alpha_composite(image, (padding, padding))
    return canvas


def process(source, output):
    source, output = source.resolve(), output.resolve()
    originals = sorted(source.glob("*.png"))
    overview = Image.new("RGB", (1200, ((len(originals) + 3) // 4) * 320), (24, 31, 48))
    draw = ImageDraw.Draw(overview)
    for index, original in enumerate(originals):
        image = Image.open(original).convert("RGBA")
        image.thumbnail((290, 285))
        horizontal, vertical = index % 4 * 300, index // 4 * 320
        overview.paste(image, (horizontal + (300 - image.width) // 2, vertical), image)
        draw.text((horizontal + 5, vertical + 295), original.stem, fill=(220, 234, 255))
    preview_path = source.parent.parent / "originals-preview.png"
    if not preview_path.exists():
        art.write_image(overview, preview_path)
    images, records = {}, []

    def add(image, path, original, category, group, ppu, frame=0, normal=True):
        images[path] = image
        normal_path = path.replace(".png", "-normal.png") if normal else ""
        if normal:
            images[normal_path] = art.normal_map(image)
        records.append({"path": path, "category": category, "group": group, "frame": frame,
                        "pixelsPerUnit": ppu, "normal": normal_path, "original": original.name, "sourceSha256": art.digest(original)})

    for group, name in enumerate(("delta", "vanguard", "wraith", "havoc")):
        original = source / ("portrait-havoc-v2.png" if name == "havoc" else f"portrait-{name}.png")
        image = Image.open(original).convert("RGBA")
        alpha = art.np.asarray(image.getchannel("A"))
        bounds = image.getchannel("A").point(lambda value: 255 if value > 32 else 0).getbbox()
        if bounds is None or float((alpha < 8).mean()) < 0.2 or min(bounds[0], bounds[1], image.width - bounds[2]) < 2:
            raise ValueError(f"Portrait needs intact head, sides and native alpha: {original.name}")
        portrait = cropped((image, bounds))
        portrait.thumbnail((768, 1024), Image.Resampling.LANCZOS)
        add(portrait, f"Portraits/{name}.png", original, "portrait", group, 512, normal=False)
    for group, name in enumerate(("ruins", "factory", "ice", "neon", "fortress")):
        original = source / f"boss-{name}-sheet.png"
        for frame, image in enumerate(art.normalized_animation(original, 2, 2, (512, 512), 360)):
            add(image, f"Bosses/{name}-{frame}.png", original, "boss", group, 288.58, frame)
    for category, filename, columns, rows, sizes in (
        ("weapon", "weapons-sheet.png", 2, 3, (0.95, 1.0, 1.3, 0.75, 1.3, 1.0)),
        ("view", "viewmodels-sheet.png", 3, 2, (1, 1, 1, 1, 1, 1)),
        ("scenery", "props-sheet.png", 4, 2, (1.1, 1.5, 0.55, 1.2, 4, 3, 1.8, 2.8)),
        ("platform", "platforms-sheet.png", 2, 3, (5.6, 5.6, 5.6, 5.6, 5.6, 3)),
    ):
        original = source / filename
        if category in {"scenery", "weapon", "platform"}:
            image = Image.open(original).convert("RGBA")
            if image.size != (1536, 1024):
                raise ValueError("Reviewed sheet layout expects 1536x1024")
            row_edges, column_edges = {
                "scenery": ((0, 484, 1024), ((0, 384, 776, 1060, 1536), (0, 384, 720, 1152, 1536))),
                "weapon": ((0, 350, 673, 1024), ((0, 787, 1536), (0, 910, 1536), (0, 875, 1536))),
                "platform": ((0, 352, 642, 1024), ((0, 768, 1536), (0, 768, 1536), (0, 768, 1536))),
            }[category]
            split = []
            for row in range(rows):
                for column in range(columns):
                    cell = image.crop((column_edges[row][column], row_edges[row], column_edges[row][column + 1], row_edges[row + 1]))
                    bounds = cell.getchannel("A").point(lambda value: 255 if value > 32 else 0).getbbox()
                    if bounds is None or min(bounds[0], bounds[1], cell.width - bounds[2], cell.height - bounds[3]) < 2:
                        raise ValueError("Prop crosses reviewed cell boundary")
                    split.append((cell, bounds))
        else:
            image = Image.open(original).convert("RGBA")
            padded = Image.new("RGBA", (image.width + (-image.width % columns), image.height + (-image.height % rows)))
            padded.alpha_composite(image)
            prepared = source.parent.parent / "layout-inputs" / original.name
            art.write_image(padded, prepared)
            split = art.cells(prepared, columns, rows)
        for group, cell in enumerate(split):
            image = cropped(cell, 0 if category == "platform" else 8)
            extent = image.height if category == "scenery" else image.width
            add(image, f"{category.title()}/{category}-{group}.png", original, category, group, extent / sizes[group], normal=category != "view")
    for path, image in images.items():
        destination = output / path
        if destination.exists():
            with Image.open(destination) as previous:
                if previous.mode != image.mode or previous.size != image.size or previous.tobytes() != image.tobytes():
                    raise ValueError(f"Refusing to replace existing image: {destination}")
    for record in records:
        for key, path in (("sha256", record["path"]), ("normalSha256", record["normal"])):
            if not path:
                record[key] = ""
                continue
            stream = io.BytesIO()
            images[path].save(stream, format="PNG")
            record[key] = art.hashlib.sha256(stream.getvalue()).hexdigest()
    manifest = {"version": "arsenal-v2", "status": "processed-awaiting-visual-review", "assets": records}
    manifest_path = output / "art-manifest.json"
    if manifest_path.exists():
        existing = json.loads(manifest_path.read_text(encoding="utf-8"))
        existing["status"] = manifest["status"]
        if existing != manifest:
            raise ValueError("Manifest differs; create a new version")
    for path, image in images.items():
        art.write_image(image, output / path)
    if not manifest_path.exists():
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    preview = Image.new("RGB", (1200, ((len(records) + 5) // 6) * 220), (24, 31, 48))
    draw = ImageDraw.Draw(preview)
    for index, record in enumerate(records):
        image = images[record["path"]].copy()
        image.thumbnail((190, 190))
        horizontal, vertical = index % 6 * 200, index // 6 * 220
        preview.paste(image, (horizontal + (200 - image.width) // 2, vertical), image)
        draw.text((horizontal + 4, vertical + 198), Path(record["path"]).stem, fill=(220, 234, 255))
    art.write_image(preview, source.parent.parent / "contact-sheet.png")
    return {"sprites": len(records), "textures": len(images), "manifest": str(manifest_path)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Process locally generated arsenal sprites; no API calls.")
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    options = parser.parse_args()
    print(json.dumps(process(options.source, options.output)))
