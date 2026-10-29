import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw


project = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("encounter_art", project / "Tools/Prepare-EncounterArt.py")
art = importlib.util.module_from_spec(spec)
spec.loader.exec_module(art)


class EncounterArtTests(unittest.TestCase):
    def test_delivered_manifest_integrity_and_unique_minions(self):
        root = project / "Assets/Art/Production/EncounterV3"
        manifest = json.loads((root / "art-manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(manifest["status"], "visually-reviewed")
        self.assertEqual(len(manifest["assets"]), 60)
        identities = set()
        slots = set()
        for entry in manifest["assets"]:
            slot = (entry["category"], entry["stage"], entry["group"], entry["frame"])
            self.assertNotIn(slot, slots)
            slots.add(slot)
            self.assertEqual(art.art.digest(root / entry["path"]), entry["sha256"])
            image = Image.open(root / entry["path"])
            self.assertEqual(image.mode, "RGBA")
            alpha = art.art.np.asarray(image.getchannel("A"))
            self.assertGreater(float((alpha < 8).mean()), 0.2)
            self.assertEqual(int(alpha[0].max()), 0)
            if entry["normal"]:
                self.assertEqual(art.art.digest(root / entry["normal"]), entry["normalSha256"])
            if entry["category"] == "enemy" and entry["frame"] == 0:
                identities.add(entry["sha256"])
        self.assertEqual(len(identities), 20)

    def test_overlapping_enemy_sheet_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "overlap.png"
            image = Image.new("RGBA", (1536, 1024))
            draw = ImageDraw.Draw(image)
            draw.rectangle((20, 100, 1500, 350), fill="white")
            draw.rectangle((20, 600, 1500, 850), fill="white")
            image.save(path)
            with self.assertRaisesRegex(ValueError, "gutter"):
                art.enemy_cells(path)

    def test_opaque_enemy_sheet_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "opaque.png"
            Image.new("RGB", (1536, 1024), "black").save(path)
            with self.assertRaisesRegex(ValueError, "transparency"):
                art.enemy_cells(path)


if __name__ == "__main__":
    unittest.main()
