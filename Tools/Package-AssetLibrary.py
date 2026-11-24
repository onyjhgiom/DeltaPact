"""Create a complete, restorable ZIP backup of all MoriEchoes art assets.

Bundles three source areas into one archive with a CSV index and SHA-256 manifest:
  01_游戏内素材_Assets/   -> Assets/Art, Assets/Fonts (KEEP .meta files: GUIDs are referenced by scenes/prefabs)
  02_AI生成原图_output/   -> output/imagegen (original generated images)
  03_制作稿与提示词_ArtProduction/ -> ArtProduction (prompts, previews, intermediate art)

The archive root is a single folder so it can be extracted and copied back 1:1.
Nothing in the source tree is moved, renamed or deleted.
"""
import csv
import hashlib
import io
import json
import zipfile
from datetime import datetime
from pathlib import Path

PROJECT = Path(__file__).resolve().parent.parent
STAMP = datetime.now().strftime("%Y%m%d_%H%M%S")
RELEASE_DIR = PROJECT / "Releases" / f"MoriEchoes_素材库_{STAMP}"
ZIP_PATH = RELEASE_DIR / f"MoriEchoes_素材库备份_{STAMP}.zip"

# (absolute source dir, archive subpath prefix)
AREAS = [
    (PROJECT / "Assets" / "Art", "01_游戏内素材_Assets/Art"),
    (PROJECT / "Assets" / "Fonts", "01_游戏内素材_Assets/Fonts"),
    (PROJECT / "output" / "imagegen", "02_AI生成原图_output/imagegen"),
    (PROJECT / "ArtProduction", "03_制作稿与提示词_ArtProduction"),
]

README = """末日回响 / MoriEchoes —— 素材库备份
========================================
生成时间：{stamp}
来源工程：{project}

本压缩包把项目里的美术素材完整汇总为一份可离线保存、可原样还原的备份，
不改动原工程任何文件。

【目录结构】
  01_游戏内素材_Assets/
      Art/     —— 游戏实际引用的 Sprite / 法线 / 清单（含每个 .png 对应的 .meta）
      Fonts/   —— 中文字体及 OFL / SOURCE 许可（含 .meta）
      注意：.meta 文件必须保留。Unity 靠其中的 GUID 让场景、预制体引用素材，
            丢失 .meta 会导致游戏里的图片引用断开。还原时要连同 .meta 一起拷回。
  02_AI生成原图_output/imagegen/
      —— 各系列的 image 生成原图（PNG），按系列分目录
  03_制作稿与提示词_ArtProduction/
      —— 提示词、预览图、加工中间稿与 processed-manifest 记录
  素材索引.csv
      —— 每个文件的区域、包内路径、字节数、SHA-256
  manifest.sha256
      —— 全包逐文件校验清单
  package.json
      —— 本次打包的统计与校验结果

【如何还原】
  解压后，把 01_游戏内素材_Assets/ 里的 Art、Fonts 连同 .meta 一起覆盖回
  工程的 Assets/ 对应位置；02、03 两个目录按需放回 output/ 与项目根目录即可。

【说明】
  本包只含素材，不含 Unity 工程配置、脚本或可执行游戏。
  .pdb/.mdb 等调试符号与本备份无关，未纳入；素材文件本身全部保留。
"""


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def main():
    for src, _ in AREAS:
        if not src.is_dir():
            raise FileNotFoundError(src)
    RELEASE_DIR.mkdir(parents=True, exist_ok=True)
    if ZIP_PATH.exists():
        raise FileExistsError(ZIP_PATH)

    rows = []            # (area, archive_path, size, sha256)
    generated = {}       # archive_path -> bytes

    # Build file list first so we can hash and index deterministically.
    for src, prefix in AREAS:
        for path in sorted(src.rglob("*")):
            if path.is_file() and not path.is_symlink():
                rel = path.relative_to(src).as_posix()
                arc = f"{prefix}/{rel}"
                rows.append((prefix.split("/")[0], arc, path.stat().st_size, sha256_file(path), path))

    generated["README_素材库说明.txt"] = README.format(
        stamp=datetime.now().astimezone().isoformat(), project=str(PROJECT)
    ).encode("utf-8")

    buf = io.StringIO()
    writer = csv.writer(buf)
    writer.writerow(["区域", "包内路径", "字节数", "SHA-256"])
    for area, arc, size, digest, _ in rows:
        writer.writerow([area, arc, size, digest])
    writer.writerow(["根目录", "README_素材库说明.txt", len(generated["README_素材库说明.txt"]), ""])
    generated["素材索引.csv"] = ("\ufeff" + buf.getvalue()).encode("utf-8")  # BOM for Excel

    manifest = "".join(f"{digest}  {arc}\n" for _, arc, _, digest, _ in rows)
    generated["manifest.sha256"] = manifest.encode("utf-8")

    total_bytes = sum(r[2] for r in rows)
    with zipfile.ZipFile(ZIP_PATH, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
        for _, arc, _, _, path in rows:
            zf.write(path, "MoriEchoes_素材库/" + arc)
        for arc, data in generated.items():
            zf.writestr("MoriEchoes_素材库/" + arc, data)

    # Integrity: CRC check + archive-path safety + hash spot check through re-read.
    with zipfile.ZipFile(ZIP_PATH) as zf:
        bad = zf.testzip()
        if bad:
            raise ValueError("CRC failed: " + bad)
        for info in zf.infolist():
            target = (ZIP_PATH.parent / info.filename).resolve()
            if not str(target).startswith(str(ZIP_PATH.parent.resolve())):
                raise ValueError("Unsafe archive member: " + info.filename)
        # Verify every indexed asset hash against the bytes stored inside the zip.
        with zf.open("MoriEchoes_素材库/manifest.sha256") as mf:
            manifest_lines = mf.read().decode("utf-8").splitlines()
        checked = 0
        for line in manifest_lines:
            digest, arc = line.split("  ", 1)
            with zf.open("MoriEchoes_素材库/" + arc) as f:
                h = hashlib.sha256()
                for block in iter(lambda: f.read(1024 * 1024), b""):
                    h.update(block)
            if h.hexdigest() != digest:
                raise ValueError("In-archive hash mismatch: " + arc)
            checked += 1

    result = {
        "zip": str(ZIP_PATH),
        "zipBytes": ZIP_PATH.stat().st_size,
        "zipSha256": sha256_file(ZIP_PATH),
        "assetFiles": len(rows),
        "assetBytes": total_bytes,
        "hashesVerifiedInArchive": checked,
        "crcPassed": True,
        "areas": [
            {"source": str(src), "archivePrefix": prefix}
            for src, prefix in AREAS
        ],
        "generatedAt": datetime.now().astimezone().isoformat(),
    }
    (ZIP_PATH.with_suffix(".package.json")).write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    ZIP_PATH.with_suffix(".sha256.txt").write_text(
        result["zipSha256"] + "  " + ZIP_PATH.name + "\n", encoding="utf-8"
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
