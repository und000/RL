"""Run C# logic with explicit Unity API stubs; this is not a Unity play test."""
from pathlib import Path
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def run(source: str, entry: str, name: str):
    cache = max((ROOT / 'Library/Bee/artifacts').glob('*/Assembly-CSharp.rsp'), key=lambda p: p.stat().st_mtime)
    data = Path(re.search(r'[-/]r:"([^"\n]+/Data)/Managed/', cache.read_text(encoding='utf-8-sig'))[1])
    framework = next((data / 'NetCoreRuntime/shared/Microsoft.NETCore.App').iterdir())
    output = ROOT / 'Temp/ProjectVerification'
    output.mkdir(parents=True, exist_ok=True)
    cs = output / (name + '.cs')
    cs.write_text(source + '\npublic static class TestEntry {public static void Main(){' + entry + ';}}\n', encoding='utf-8')
    dll = output / (name + '.dll')
    references = ['System.Private.CoreLib', 'System.Runtime', 'System.Console', 'System.Collections', 'System.Linq']
    lines = ['-nologo', '-target:exe', '-langversion:latest', '-nowarn:0649,0414', f'-out:"{dll.as_posix()}"']
    lines += [f'-r:"{(framework / (ref + ".dll")).as_posix()}"' for ref in references]
    lines += [f'"{cs.as_posix()}"']
    rsp = output / (name + '.rsp')
    rsp.write_text('\n'.join(lines), encoding='utf-8')
    dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net6.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}), encoding='utf-8')
    dotnet = str(data / 'NetCoreRuntime/dotnet.exe')
    subprocess.run([dotnet, str(data / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)], cwd=ROOT, check=True)
    subprocess.run([dotnet, str(dll)], cwd=ROOT, check=True)
