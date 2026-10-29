import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


specification = importlib.util.spec_from_file_location("prepare_art", Path(__file__).resolve().parents[1] / "Prepare-AnimeArt.py")
art = importlib.util.module_from_spec(specification)
specification.loader.exec_module(art)


def sheet(path, columns, rows, crouch=-1):
    image = Image.new("RGBA", (columns * 128, rows * 160))
    drawing = ImageDraw.Draw(image)
    for index in range(columns * rows):
        horizontal, vertical = index % columns * 128, index // columns * 160
        top = 88 if index == crouch else 20
        drawing.rectangle((horizontal + 44, vertical + top, horizontal + 84, vertical + 140), fill=(80 + index * 12, 140, 190, 255))
    image.save(path)


class AnimeArtTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="mori-art-test-")
        self.root = Path(self.temporary.name)

    def tearDown(self):
        self.temporary.cleanup()

    def test_native_alpha_required(self):
        path = self.root / "opaque.png"
        Image.new("RGB", (128, 160), "white").save(path)
        with self.assertRaisesRegex(ValueError, "Native alpha"):
            art.cells(path, 1, 1)

    def test_empty_cells_rejected(self):
        path = self.root / "empty.png"
        Image.new("RGBA", (128, 160)).save(path)
        with self.assertRaisesRegex(ValueError, "Missing figure"):
            art.cells(path, 1, 1)

    def test_clipped_cells_rejected(self):
        path = self.root / "clipped.png"
        image = Image.new("RGBA", (128, 160))
        ImageDraw.Draw(image).rectangle((0, 20, 40, 140), fill="white")
        image.save(path)
        with self.assertRaisesRegex(ValueError, "boundary"):
            art.cells(path, 1, 1)

    def test_non_divisible_grid_rejected(self):
        path = self.root / "grid.png"
        Image.new("RGBA", (128, 160)).save(path)
        with self.assertRaisesRegex(ValueError, "divide evenly"):
            art.cells(path, 1, 3)

    def test_adaptive_seam_preserves_offset_figure(self):
        path = self.root / "offset.png"
        image = Image.new("RGBA", (256, 160))
        drawing = ImageDraw.Draw(image)
        drawing.rectangle((35, 20, 95, 140), fill="white")
        drawing.rectangle((126, 20, 225, 140), fill="white")
        image.save(path)
        frames = art.cells(path, 2, 1)
        self.assertEqual(len(frames), 2)
        self.assertEqual(frames[1][1][2] - frames[1][1][0], 100)

    def test_animation_baseline_and_crouch(self):
        path = self.root / "animation.png"
        sheet(path, 4, 2, crouch=7)
        frames = art.normalized_animation(path, 4, 2, (384, 512), crouch_index=7)
        self.assertEqual(len(frames), 8)
        bounds = [frame.getchannel("A").getbbox() for frame in frames]
        self.assertEqual(len({bound[3] for bound in bounds}), 1)
        self.assertLess(bounds[7][3] - bounds[7][1], (bounds[0][3] - bounds[0][1]) * 0.6)
        with self.assertRaisesRegex(ValueError, "Unexpected small figure"):
            art.normalized_animation(path, 4, 2, (384, 512))

    def test_normal_vectors_are_normalized(self):
        image = Image.new("RGBA", (64, 64))
        ImageDraw.Draw(image).ellipse((12, 12, 52, 52), fill=(180, 200, 220, 255))
        normals = np.asarray(art.normal_map(image), dtype=float) / 255 * 2 - 1
        self.assertTrue(np.all(np.abs(np.linalg.norm(normals, axis=-1) - 1) < 0.015))

    def test_existing_image_is_preserved(self):
        path = self.root / "existing.png"
        original = Image.new("RGBA", (32, 32), "red")
        art.write_image(original, path)
        before = path.read_bytes()
        art.write_image(original, path)
        with self.assertRaisesRegex(ValueError, "Existing asset differs"):
            art.write_image(Image.new("RGBA", (32, 32), "blue"), path)
        self.assertEqual(path.read_bytes(), before)

    def test_missing_originals_write_nothing(self):
        output = self.root / "assets"
        with self.assertRaisesRegex(ValueError, "Missing generated originals"):
            art.process(self.root, output)
        self.assertFalse(output.exists())

    def test_source_selection_cannot_escape_originals(self):
        (self.root / "source-selection.json").write_text(json.dumps({"delta-sheet.png": "../external.png"}), encoding="utf-8")
        output = self.root / "assets"
        with self.assertRaisesRegex(ValueError, "Invalid source selection"):
            art.process(self.root, output)
        self.assertFalse(output.exists())

    def test_complete_fixture_pipeline_and_manifest_guards(self):
        source = self.root / "production" / "output" / "imagegen"
        source.mkdir(parents=True)
        for character in ("delta", "vanguard", "wraith", "havoc"):
            sheet(source / (character + "-sheet.png"), 4, 2, crouch=7)
        sheet(source / "enemies-sheet.png", 4, 2)
        sheet(source / "weapons-sheet.png", 1, 3)
        sheet(source / "effects-sheet.png", 4, 2)
        for theme in ("ruins", "factory", "ice", "neon", "fortress"):
            sheet(source / ("boss-" + theme + "-sheet.png"), 2, 2)
            Image.new("RGB", (1536, 864), (30, 40, 60)).save(source / (theme + "-background.png"))
        output = self.root / "assets"
        report = art.process(source, output)
        self.assertEqual(report["sprites"], 80)
        self.assertEqual(report["texturesWithNormals"], 143)
        manifest_path = output / "art-manifest.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        for record in manifest["assets"]:
            self.assertEqual(art.digest(output / record["path"]), record["sha256"])
            if record["normal"]:
                self.assertEqual(art.digest(output / record["normal"]), record["normalSha256"])
        manifest["status"] = "visually-reviewed"
        reviewed = json.dumps(manifest, indent=2) + "\n"
        manifest_path.write_text(reviewed, encoding="utf-8")
        art.process(source, output)
        self.assertEqual(manifest_path.read_text(encoding="utf-8"), reviewed)
        removed = output / manifest["assets"][0]["path"]
        removed.unlink()
        manifest["version"] = "different-version"
        manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Existing manifest differs"):
            art.process(source, output)
        self.assertFalse(removed.exists())


if __name__ == "__main__":
    unittest.main(verbosity=2)
