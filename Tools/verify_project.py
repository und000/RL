"""Validate Unity references; optionally compile with Unity's cached compiler inputs.

Run from any directory: python Tools/verify_project.py [--compile]
Compilation writes only to Temp/ProjectVerification and requires an imported project.
This does not run Unity or save scenes/assets.
"""
from pathlib import Path
import argparse
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
GAME = ROOT / "Assets/Survivor_Game"
SERIALIZED = {".asset", ".prefab", ".unity", ".controller", ".anim", ".mat", ".overrideController"}


def read(path):
    return path.read_text(encoding="utf-8-sig", errors="replace")


def validate_assets():
    errors = []
    assets = {}
    for folder in (ROOT / "Assets", ROOT / "Packages", ROOT / "Library/PackageCache"):
        for meta in folder.rglob("*.meta"):
            match = re.search(r"^guid: ([0-9a-f]{32})", read(meta), re.M)
            if match:
                target = Path(str(meta)[:-5])
                if folder.name == "Assets" and match[1] in assets:
                    errors.append(f"Duplicate GUID: {meta}")
                assets[match[1]] = target

    files = {p: read(p) for p in GAME.rglob("*") if p.suffix in SERIALIZED}
    for path, text in files.items():
        label = path.relative_to(ROOT)
        blocks = re.split(r"(?=^--- !u!)", text, flags=re.M)
        ids = set(re.findall(r"^--- !u!\d+ &(-?\d+)", text, re.M))
        for guid in re.findall(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})", text):
            if guid not in assets or not assets[guid].exists():
                errors.append(f"{label}: missing script {guid}")
        for block in blocks:
            if block.startswith("--- !u!1 "):
                for component in re.findall(r"- component: \{fileID: (-?\d+)\}", block):
                    if component not in ids:
                        errors.append(f"{label}: missing component {component}")
        # Check direct overrides only: inherited/nested prefab IDs need Unity's resolver.
        for file_id, guid in re.findall(r"- target: \{fileID: (-?\d+), guid: ([0-9a-f]{32})", text):
            target = assets.get(guid)
            if target not in files or "PrefabInstance:" in files[target]:
                continue
            source_ids = set(re.findall(r"^--- !u!\d+ &(-?\d+)", files[target], re.M))
            if file_id not in source_ids:
                errors.append(f"{label}: missing override target {file_id} in {target.name}")

        if "Assembly-CSharp::ChipDefinition" in text or "Assembly-CSharp::EquipmentDefinition" in text:
            for stat in re.findall(r"^  - stat: (\d+)$", text, re.M):
                if int(stat) not in range(11):
                    errors.append(f"{label}: invalid stat {stat}")
        if "Assembly-CSharp::RoomRewardDefinition" in text:
            kind = re.search(r"^  kind: (\d+)$", text, re.M)
            if not kind or int(kind[1]) not in set(range(9)) | {10, 11, 12, 13}:
                errors.append(f"{label}: invalid reward kind")
        if "Assembly-CSharp::CoreBoardLayout" in text:
            cells = re.search(r"^  cells: ([0-9a-f]*)$", text, re.M)
            if cells:
                values = [int.from_bytes(bytes.fromhex(cells[1][i:i+8]), "little")
                          for i in range(0, len(cells[1]), 8)]
                width = int(re.search(r"^  width: (\d+)$", text, re.M)[1])
                height = int(re.search(r"^  height: (\d+)$", text, re.M)[1])
                if len(values) != width * height or not set(values) <= {0, 1, 2, 4}:
                    errors.append(f"{label}: invalid board cells")

    # Deleted tracked assets must not leave GUID references behind, including animations.
    git = ["git", "-c", f"safe.directory={ROOT.as_posix()}", "-c", "core.safecrlf=false"]
    deleted = subprocess.run(git + ["diff", "HEAD", "--diff-filter=D", "--name-only"],
                             cwd=ROOT, text=True, capture_output=True, check=True).stdout.splitlines()
    for name in deleted:
        if not name.endswith(".meta"):
            continue
        previous = subprocess.run(git + ["show", f"HEAD:{name}"], cwd=ROOT,
                                  text=True, capture_output=True, check=True).stdout
        guid = re.search(r"^guid: (\w+)", previous, re.M)
        if guid and not (guid[1] in assets and assets[guid[1]].exists()):
            for path, text in files.items():
                if guid[1] in text:
                    errors.append(f"{path.relative_to(ROOT)}: references deleted asset {name[:-5]}")
    if errors:
        raise RuntimeError("\n".join(sorted(set(errors))))
    print(f"PASS: {len(files)} serialized assets; scripts, components, direct prefab overrides, enums and deleted-asset references.")


def compile_scripts():
    candidates = list((ROOT / "Library/Bee/artifacts").glob("*/Assembly-CSharp.rsp"))
    if not candidates:
        raise RuntimeError("Open RL in Unity once to generate Library/Bee compiler inputs.")
    runtime = max(candidates, key=lambda path: path.stat().st_mtime)
    data = re.search(r'[-/]r:"([^"\n]+/Data)/Managed/', read(runtime))
    if not data:
        raise RuntimeError("Cannot locate the Unity compiler from the response file.")
    editor_data = Path(data[1])
    output = ROOT / "Temp/ProjectVerification"
    output.mkdir(parents=True, exist_ok=True)
    for assembly in ("Assembly-CSharp", "Assembly-CSharp-Editor"):
        source = runtime.parent / f"{assembly}.rsp"
        result = []
        for line in read(source).splitlines():
            if re.match(r'^"Assets/Survivor_Game/.*\.cs"$', line):
                continue
            if re.match(r"[-/](out|refout):", line):
                suffix = ".ref.dll" if "refout" in line else ".dll"
                line = line.split(":", 1)[0] + f':"{(output / (assembly + suffix)).as_posix()}"'
            elif assembly.endswith("-Editor") and re.match(r'[-/]r:.*Assembly-CSharp\.ref\.dll"$', line):
                line = f'-r:"{(output / "Assembly-CSharp.ref.dll").as_posix()}"'
            result.append(line)
        for script in sorted(GAME.rglob("*.cs")):
            is_editor = "Editor" in script.relative_to(GAME).parts
            if is_editor == assembly.endswith("-Editor"):
                result.append(f'"{script.relative_to(ROOT).as_posix()}"')
        response = output / f"{assembly}.rsp"
        response.write_text("\n".join(result) + "\n", encoding="utf-8")
        subprocess.run([str(editor_data / "NetCoreRuntime/dotnet.exe"),
                        str(editor_data / "DotNetSdkRoslyn/csc.dll"), f"@{response}"], cwd=ROOT, check=True)
        print(f"PASS: {assembly} compiled with the installed Unity compiler.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--compile", action="store_true")
    args = parser.parse_args()
    try:
        validate_assets()
        if args.compile:
            compile_scripts()
    except (RuntimeError, subprocess.CalledProcessError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)
