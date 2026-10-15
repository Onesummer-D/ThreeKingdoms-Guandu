"""Stage 11 hand-picked CC0 OpenGameArt effects as stable Unity assets."""
from __future__ import annotations
import hashlib
import pathlib
import subprocess
import uuid
import zipfile
from cc0_sfx_import import ASSETS, ROOT, TMP, download

CHOICES = [
    ("FireIgnition", "fire_crackle", None, "乌巢初燃与木料噼啪"),
    ("FireLoop", "fire_loop", None, "乌巢持续燃烧环境声"),
    ("BattleShout", "crowd", None, "战场群体呼喊"),
    ("SwordClashA", "sword_clash", "sword_clash.4.ogg", "兵刃交锋变体一"),
    ("SwordClashB", "sword_clash", "sword_clash.7.ogg", "兵刃交锋变体二"),
    ("RockBreak", "break_fall", "bfh1_rock_breaking_02.ogg", "石块裂解和霹雳车重击"),
    ("RockFall", "break_fall", "bfh1_rock_falling_03.ogg", "地道塌方落石"),
    ("WoodBreak", "break_fall", "bfh1_wood_breaking_03.ogg", "木制攻城楼断裂"),
    ("WoodFall", "break_fall", "bfh1_wood_falling_01.ogg", "木楼坍塌收尾"),
    ("SwordDrop", "metal_wood", "metal_falling_01.ogg", "倚天剑落地"),
    ("WoodThud", "metal_wood", "wood_slam_02.ogg", "拍案或重物闷响"),
]
META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 2
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""
def main():
    TMP.mkdir(parents=True, exist_ok=True)
    out = ROOT / "Assets" / "Audio" / "ImportedCC0"
    out.mkdir(parents=True, exist_ok=True)
    sources = {name: (url, page, author, license) for name, url, page, author, license in ASSETS}
    needed = {row[1] for row in CHOICES}
    retrieved = {name: download(name, sources[name][0]) for name in sorted(needed)}
    table = ["# 官渡之战 CC0 音效导入台账", "",
             "下列音频源自 OpenGameArt 页面明示的 CC0 资源。",
             "已导入为 Unity 可识别 OGG 及固定 GUID 的 .meta 文件，但尚未绑定剧情节点；先经实机试听确认后再挂接。",
             "",
             "| 音频文件 | 用途 | 作者和授权 | 原始素材 | 时长 | SHA256 |",
             "|---|---|---|---|---:|---|"]
    for target, source, member, purpose in CHOICES:
        url, page, author, license = sources[source]
        original = retrieved[source]
        if member is None:
            raw = original.read_bytes()
        else:
            with zipfile.ZipFile(original) as z:
                if member not in z.namelist():
                    raise ValueError("Missing " + member + " inside " + str(original))
                raw = z.read(member)
        intermediate = TMP / ("selected_" + target + (".wav" if source == "fire_loop" else ".ogg"))
        intermediate.write_bytes(raw)
        dest = out / (target + ".ogg")
        if source == "fire_loop":
            subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
                            "-i", str(intermediate), "-ac", "1", "-c:a", "libvorbis",
                            "-q:a", "5", str(dest)], check=True)
        elif source == "crowd":
            subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
                            "-ss", "1.5", "-t", "6", "-i", str(intermediate), "-ac", "1",
                            "-af", "afade=t=in:st=0:d=0.12,afade=t=out:st=5.4:d=0.6",
                            "-c:a", "libvorbis", "-q:a", "5", str(dest)], check=True)
        else:
            dest.write_bytes(raw)
        probe = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                                "-of", "default=noprint_wrappers=1:nokey=1", str(dest)],
                               check=True, capture_output=True, text=True)
        duration = float(probe.stdout.strip())
        if not 0.10 < duration < 32.0:
            raise ValueError("Invalid audio duration " + str(duration))
        guid = uuid.uuid5(uuid.NAMESPACE_URL, "guandu-cc0:" + target + ":" + url).hex
        (out / (target + ".ogg.meta")).write_text(META.format(guid=guid), encoding="utf8")
        digest = hashlib.sha256(dest.read_bytes()).hexdigest()
        table.append("| " + target + ".ogg | " + purpose + " | " + author + " / " + license +
                     " | [来源](" + page + ") / " + (member or original.name) + " | " +
                     format(duration, ".2f") + "s | " + digest + " |")
        print("STAGED", target, round(duration, 2), "sec", "sha256", digest[:16], "guid", guid)
    table += ["", "CC0 不强制署名，保留出处是为了比赛和开源发布材料可追溯。",
              "这些文件是根据用途与原始文件名筛选的候选素材，尚未经过 Unity 内实机听感验收。",
              "霹雳车独立机械发射/绞盘声仍需进一步寻找，不应用普通木头碎裂声冒充。"]
    ledger = ROOT / "Documentation" / "QA" / "CC0ImportedAudio.md"
    ledger.parent.mkdir(parents=True, exist_ok=True)
    ledger.write_text("\n".join(table) + "\n", encoding="utf8")
    print("IMPORTED", len(CHOICES), "assets with CC0 provenance ledger")
if __name__ == "__main__":
    main()
