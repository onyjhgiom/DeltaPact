import importlib.util
import json
from pathlib import Path

from PIL import Image, ImageDraw


project = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("anime_art", Path(__file__).with_name("Prepare-AnimeArt.py"))
art = importlib.util.module_from_spec(spec)
spec.loader.exec_module(art)
source = project / "ArtProduction/skills-v1/output/imagegen/character-skills.png"
output = project / "Assets/Art/Production/SkillsV1"
images = {}
for index, (image, bounds) in enumerate(art.cells(source, 2, 2)):
    figure = image.crop(bounds)
    canvas = Image.new("RGBA", (figure.width + 16, figure.height + 16))
    canvas.alpha_composite(figure, (8, 8))
    images[index] = canvas
for index, image in images.items():
    path = output / f"skill-{index}.png"
    if path.exists():
        previous = Image.open(path)
        if previous.mode != image.mode or previous.size != image.size or previous.tobytes() != image.tobytes():
            raise ValueError("Existing sprite differs; use a new version")
records = []
preview = Image.new("RGB", (1200, 340), (28, 34, 48))
draw = ImageDraw.Draw(preview)
for index, image in images.items():
    path = output / f"skill-{index}.png"
    art.write_image(image, path)
    records.append(dict(path=path.name, sha256=art.digest(path)))
    image = image.copy(); image.thumbnail((290, 290))
    preview.paste(image, (index * 300 + (300 - image.width) // 2, 0), image)
    draw.text((index * 300 + 10, 300), ("DELTA / FOCUS", "VANGUARD / HEAL", "WRAITH / TELEPORT", "HAVOC / BLAST")[index], fill="white")
manifest = dict(version="skills-v1", status="processed-awaiting-visual-review", originalSha256=art.digest(source), assets=records)
manifest_path = output / "art-manifest.json"
if manifest_path.exists():
    previous = json.loads(manifest_path.read_text(encoding="utf-8")); previous["status"] = manifest["status"]
    if previous != manifest:
        raise ValueError("Manifest differs; use a new version")
else:
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
art.write_image(preview, project / "ArtProduction/skills-v1/contact-sheet.png")
print("Prepared four character-specific skill sprites; native alpha preserved.")
