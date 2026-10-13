"""Build the release zip. It holds only our own code, the bundled MIT/BSD/Apache tools with notices, and docs.
Nothing from Risk of Rain 2 or Deadlock is included: the extractor reads Deadlock on the player's PC.

  plugins/DeadlockOfRain/   -> {game}/BepInEx/plugins/DeadlockOfRain
  extract/                  -> {localappdata}/DeadlockOfRain/extract
"""
import hashlib, json, os, sys, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
version = sys.argv[1] if len(sys.argv) > 1 else '0.1.0'
out = os.path.join(ROOT, 'build', f'DeadlockOfRain-{version}.zip')
files = {
    'plugins/DeadlockOfRain/DeadlockOfRain.dll': 'plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll',
    'plugins/DeadlockOfRain/README.md': 'README.md',
    'plugins/DeadlockOfRain/LICENSE.txt': 'LICENSE.txt',
    'extract/DeadlockExtract.exe': 'build/extract/DeadlockExtract.exe',
    'extract/manifest.json': 'extractor/manifest.json',
    'extract/THIRD-PARTY-NOTICES.txt': 'THIRD-PARTY-NOTICES.txt',
    'extract/LICENSE.txt': 'LICENSE.txt',
}
if os.path.exists(out): os.remove(out)
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as z:
    for arc, src in files.items():
        z.write(os.path.join(ROOT, src), arc)
entries = []
with zipfile.ZipFile(out) as z:
    for i in z.infolist(): entries.append({'path': i.filename, 'size': i.file_size})
data = open(out, 'rb').read()
info = {'file': out, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest(), 'entries': entries}
json.dump(info, open(out + '.json', 'w'), indent=2)
print(json.dumps(info, indent=2))
