"""Curated, CC0-only source downloader for the Guandu audio import.

First run --inventory to inspect package filenames; no asset is committed
until explicitly selected for an in-game role.
"""
from __future__ import annotations
import argparse
import hashlib
import pathlib
import subprocess
import urllib.request
import zipfile

ASSETS = [
    ("fire_crackle", "https://opengameart.org/sites/default/files/fire-1_0.ogg",
     "https://opengameart.org/content/fire-crackling", "AntumDeluge", "CC0"),
    ("fire_loop", "https://opengameart.org/sites/default/files/fire.wav",
     "https://opengameart.org/content/fireplace-sound-loop", "PagDev", "CC0"),
    ("crowd", "https://opengameart.org/sites/default/files/crowd_shouting_0.ogg",
     "https://opengameart.org/content/crowd-shoutingspeaking-ambience", "StarNinjas", "CC0"),
    ("sword_clash", "https://opengameart.org/sites/default/files/sword_clash_-_starninjas_0.zip",
     "https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes", "StarNinjas", "CC0"),
    ("break_fall", "https://opengameart.org/sites/default/files/sfx_breaking_and_falling.zip",
     "https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx", "rubberduck", "CC0"),
    ("metal_wood", "https://opengameart.org/sites/default/files/100-CC0-wood-metal-SFX.zip",
     "https://opengameart.org/content/100-cc0-metal-and-wood-sfx", "rubberduck", "CC0"),
    ("wood_cracks", "https://opengameart.org/sites/default/files/independent_nu_ljudbank-wood_crack_hit_destruction.7z",
     "https://opengameart.org/content/35-wooden-crackshitsdestructions", "Independent.nu", "CC0"),
]
ROOT = pathlib.Path(__file__).resolve().parents[2]
TMP = ROOT / "_downloaded_cc0_audio"

def download(name: str, url: str) -> pathlib.Path:
    dest = TMP / f"{name}{pathlib.Path(url).suffix}"
    if dest.exists():
        return dest
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (Guandu sound asset library; contact via project)"})
    with urllib.request.urlopen(req, timeout=70) as response:
        raw = response.read(35 * 1024 * 1024)
    if not raw or len(raw) > 34 * 1024 * 1024:
        raise RuntimeError(f"{name}: empty or oversized asset")
    dest.write_bytes(raw)
    print(f"FETCH {name} size={len(raw)} sha256={hashlib.sha256(raw).hexdigest()[:20]}")
    return dest

def inventory(source: pathlib.Path):
    if source.suffix == ".zip":
        with zipfile.ZipFile(source) as package:
            for member in package.infolist()[:140]:
                if not member.is_dir():
                    print("INVENTORY", source.name, member.filename[:140], member.file_size)
    elif source.suffix == ".7z":
        result = subprocess.run(["7z", "l", "-slt", str(source)], stdout=subprocess.PIPE,
                                stderr=subprocess.PIPE, text=True, check=True)
        lines = [line.strip() for line in result.stdout.splitlines() if line.startswith(("Path =", "Size ="))]
        print("INVENTORY", source.name, "\n".join(lines[-150:]))
    else:
        info = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                               "-of", "default=noprint_wrappers=1:nokey=1", str(source)],
                               stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        print("INVENTORY", source.name, info.stdout.strip() or info.stderr[:200])

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--inventory", action="store_true")
    args = parser.parse_args()
    TMP.mkdir(parents=True, exist_ok=True)
    for name,url,page,author,license in ASSETS:
        try:
            asset = download(name, url)
            inventory(asset)
        except Exception as exc:
            print("ERROR", name, repr(exc))
            raise
if __name__ == "__main__":
    main()
