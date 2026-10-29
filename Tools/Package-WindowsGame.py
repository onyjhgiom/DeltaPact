import argparse
import hashlib
import json
import struct
import zipfile
from datetime import datetime
from pathlib import Path


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def excluded(relative):
    return any("BackUpThisFolder_ButDontShipItWithYourGame" in part for part in relative.parts) or relative.suffix.lower() in {".pdb", ".mdb"}


def main():
    parser = argparse.ArgumentParser(description="Create and verify a complete portable Windows game ZIP.")
    parser.add_argument("--build", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--extract-check", required=True, type=Path)
    args = parser.parse_args()
    project = Path(__file__).resolve().parent.parent
    build = args.build.resolve(strict=True)
    output = args.output.resolve()
    extract = args.extract_check.resolve()
    if output.exists() or extract.exists():
        raise FileExistsError("Use new output and extraction paths; existing files are preserved.")
    required = ("MoriEchoes.exe", "UnityPlayer.dll", "MoriEchoes_Data/globalgamemanagers", "MoriEchoes_Data/Managed/Assembly-CSharp.dll", "MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll")
    for relative in required:
        if not (build / relative).is_file():
            raise FileNotFoundError(build / relative)
    with (build / "MoriEchoes.exe").open("rb") as stream:
        stream.seek(0x3C)
        header = struct.unpack("<I", stream.read(4))[0]
        stream.seek(header)
        if stream.read(6) != b"PE\x00\x00\x64\x86":
            raise ValueError("Game executable is not Windows x64.")
    report_path = project / "docs" / "BuildReports" / ("Windows_" + build.name.removeprefix("MoriEchoes_") + ".md")
    report = report_path.read_text(encoding="utf-8-sig")
    if "Succeeded" not in report or "0 / 0" not in report or "Assets/Fonts/NotoSansCJKsc-Regular.otf" not in report:
        raise ValueError("A successful, zero-warning embedded-font build report is required.")
    distribution = project / "Tools" / "Distribution"
    sources = {path.relative_to(build).as_posix(): path for path in sorted(build.rglob("*")) if path.is_file() and not excluded(path.relative_to(build))}
    sources["ThirdPartyNotices/NotoSansCJKsc-OFL.txt"] = project / "Assets" / "Fonts" / "OFL.txt"
    sources["ThirdPartyNotices/NotoSansCJKsc-SOURCE.md"] = project / "Assets" / "Fonts" / "SOURCE.md"
    for path in sorted(distribution.iterdir()):
        if path.is_file():
            sources[path.name] = path
    if any(path.is_symlink() for path in sources.values()):
        raise ValueError("Symlinked inputs are not supported.")
    generated = {}
    launcher = sources.pop("START_GAME.cmd").read_text(encoding="utf-8-sig")
    generated["START_GAME.cmd"] = launcher.replace("\r\n", "\n").replace("\n", "\r\n").encode("ascii")
    version = {
        "project": "MoriEchoes",
        "gameTitle": "末日回响",
        "edition": "Windows x64 offline portable playtest",
        "buildId": build.name,
        "packagedAt": datetime.now().astimezone().isoformat(),
        "unity": "6000.6.7f1",
        "buildConfiguration": "Development",
        "buildErrors": 0,
        "buildWarnings": 0,
        "embeddedFont": "Noto Sans CJK SC Regular / SIL OFL 1.1",
        "sourceBuildReportSha256": sha256(report_path),
        "content": {"characters": 12, "weapons": 9, "stages": 8, "bosses": 8, "stageMinionAppearances": 32,
                    "verticalStages": ["废墟墓窟", "沉没圣堂", "星坠核心井"]},
        "testLimits": "Local extracted-package testing is recorded separately; no cross-PC hardware, human balance or physical controller certification."
    }
    generated["version.json"] = (json.dumps(version, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    hashes = {relative: sha256(path) for relative, path in sources.items()}
    hashes.update({relative: hashlib.sha256(data).hexdigest() for relative, data in generated.items()})
    generated["manifest.sha256"] = "".join(f"{digest}  {relative}\n" for relative, digest in sorted(hashes.items())).encode("utf-8")
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for relative, path in sorted(sources.items()):
            archive.write(path, "MoriEchoes/" + relative)
        for relative, data in sorted(generated.items()):
            archive.writestr("MoriEchoes/" + relative, data)
    with zipfile.ZipFile(output) as archive:
        damaged = archive.testzip()
        if damaged is not None:
            raise ValueError("Archive CRC failed: " + damaged)
        for entry in archive.infolist():
            target = (extract / entry.filename).resolve()
            if not target.is_relative_to(extract):
                raise ValueError("Unsafe archive member: " + entry.filename)
        archive.extractall(extract)
        unpacked_bytes = sum(entry.file_size for entry in archive.infolist())
    extracted_game = extract / "MoriEchoes"
    for relative, digest in hashes.items():
        if sha256(extracted_game / relative) != digest:
            raise ValueError("Extracted-file hash mismatch: " + relative)
    included = {path.relative_to(build).as_posix() for path in build.rglob("*") if path.is_file() and not excluded(path.relative_to(build))}
    if not included.issubset(hashes):
        raise ValueError("Some runtime files are absent from the archive.")
    result = {
        "zip": str(output), "zipBytes": output.stat().st_size, "zipSha256": sha256(output),
        "uncompressedBytes": unpacked_bytes, "archivedFiles": len(hashes) + 1,
        "runtimeFiles": len(included), "crcPassed": True, "extractedHashesPassed": True,
        "excludedDebugFiles": sum(1 for path in build.rglob("*") if path.is_file() and excluded(path.relative_to(build))),
        "extractedExecutable": str(extracted_game / "MoriEchoes.exe"),
        "buildId": build.name, "buildReport": str(report_path)
    }
    metadata = output.with_suffix(".package.json")
    metadata.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    output.with_suffix(".sha256.txt").write_text(result["zipSha256"] + "  " + output.name + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
