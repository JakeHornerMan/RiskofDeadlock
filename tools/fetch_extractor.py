"""Download the GitHub-built DeadlockExtract.exe (release extractor-<version>, .github/workflows/extractor.yml) into
build/extract/, check it against the release's .sha256, and record it in thunderstore/extractor.json. The Thunderstore zip
ships only this exe (tools/package.py checks the hash), so its checksum is the one on the public release page.

  python tools/fetch_extractor.py 1.4.0
"""
import hashlib, json, os, sys, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = 'JakeHornerMan/RiskofDeadlock'
version = sys.argv[1] if len(sys.argv) > 1 else sys.exit('usage: python tools/fetch_extractor.py <version>')
base = f'https://github.com/{REPO}/releases/download/extractor-{version}/'
out_dir = os.path.join(ROOT, 'build', 'extract')
os.makedirs(out_dir, exist_ok=True)

want = urllib.request.urlopen(base + 'DeadlockExtract.exe.sha256').read().decode('ascii').split()[0].lower()
data = urllib.request.urlopen(base + 'DeadlockExtract.exe').read()
got = hashlib.sha256(data).hexdigest()
if got != want: sys.exit(f'checksum mismatch: release says {want}, download is {got}')
open(os.path.join(out_dir, 'DeadlockExtract.exe'), 'wb').write(data)
info = {'version': version, 'sha256': got, 'url': f'https://github.com/{REPO}/releases/tag/extractor-{version}'}
json.dump(info, open(os.path.join(ROOT, 'thunderstore', 'extractor.json'), 'w', encoding='utf-8'), indent=2)
print(json.dumps(info, indent=2))
