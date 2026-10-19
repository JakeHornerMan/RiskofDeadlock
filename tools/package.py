"""Build a release zip. It holds only our own code, the bundled MIT/BSD/Apache tools with notices, and docs.
Nothing from Risk of Rain 2 or Deadlock is included: the extractor reads Deadlock on the player's PC.

  python tools/package.py <version>                 Melty zip
    plugins/DeadlockOfRain/   -> {game}/BepInEx/plugins/DeadlockOfRain
    extract/                  -> {localappdata}/DeadlockOfRain/extract   (the recipe runs it before the first start)

  python tools/package.py <version> --thunderstore  Thunderstore zip (r2modman installs plugins/ into
    BepInEx/plugins/<team>-DeadlockOfRain/; the plugin runs plugins/extract/DeadlockExtract.exe itself, FirstRun.cs)
    manifest.json, icon.png, README.md, CHANGELOG.md   Thunderstore's own files (thunderstore/)
    plugins/DeadlockOfRain.dll, plugins/extract/...    the extractor's asset list is deadlock-manifest.json there
"""
import hashlib, io, json, os, sys, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
args = [a for a in sys.argv[1:] if not a.startswith('--')]
version = args[0] if args else '0.1.0'
thunderstore = '--thunderstore' in sys.argv
TS = os.path.join(ROOT, 'thunderstore')
EXTRACT = {
    'DeadlockExtract.exe': 'build/extract/DeadlockExtract.exe',
    'THIRD-PARTY-NOTICES.txt': 'THIRD-PARTY-NOTICES.txt',
    'LICENSE.txt': 'LICENSE.txt',
}


def icon_png():
    """256x256 icon: the five heroes from our own in-game renders (build/pages/img, tools/portraits.py); no Deadlock art."""
    from PIL import Image
    # warm sandstorm glow behind them (brass over near-black), so dark outfits still read
    canvas = Image.new('RGBA', (256, 256))
    px = canvas.load()
    for y in range(256):
        for x in range(256):
            d = min(1.0, (((x - 128) / 150) ** 2 + ((y - 120) / 130) ** 2) ** 0.5)
            k = (1 - d) ** 1.6
            px[x, y] = (int(24 + 190 * k), int(18 + 120 * k), int(16 + 60 * k), 255)
    # (hero, centre x, height) back to front; Mo & Krill in the middle, the rest at the sides
    heroes = [('mokrill', 128, 150), ('seven', 34, 172), ('abrams', 222, 178), ('haze', 84, 186), ('infernus', 176, 184)]
    for h, cx, tall in heroes:
        im = Image.open(os.path.join(ROOT, 'build', 'pages', 'img', h + '.webp')).convert('RGBA')
        im = im.crop(im.getbbox())
        w = round(im.width * tall / im.height)
        im = im.resize((w, tall), Image.LANCZOS)
        canvas.alpha_composite(im, (cx - w // 2, 256 - tall))
    buf = io.BytesIO()
    canvas.convert('RGB').save(buf, 'PNG')
    return buf.getvalue()


def ts_manifest():
    m = json.load(open(os.path.join(TS, 'manifest.json'), encoding='utf-8'))
    m['version_number'] = version
    return json.dumps(m, indent=2).encode('utf-8')


if thunderstore:
    out = os.path.join(ROOT, 'build', f'DeadlockOfRain-{version}-thunderstore.zip')
    files = {
        'README.md': 'thunderstore/README.md',
        'CHANGELOG.md': 'thunderstore/CHANGELOG.md',
        'plugins/DeadlockOfRain.dll': 'plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll',
        'plugins/LICENSE.txt': 'LICENSE.txt',
        'plugins/extract/deadlock-manifest.json': 'extractor/manifest.json',
    }
    files.update({'plugins/extract/' + k: v for k, v in EXTRACT.items()})
    generated = {'manifest.json': ts_manifest(), 'icon.png': icon_png()}
else:
    out = os.path.join(ROOT, 'build', f'DeadlockOfRain-{version}.zip')
    files = {
        'plugins/DeadlockOfRain/DeadlockOfRain.dll': 'plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll',
        'plugins/DeadlockOfRain/README.md': 'README.md',
        'plugins/DeadlockOfRain/LICENSE.txt': 'LICENSE.txt',
        'extract/manifest.json': 'extractor/manifest.json',
    }
    files.update({'extract/' + k: v for k, v in EXTRACT.items()})
    generated = {}

if os.path.exists(out): os.remove(out)
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as z:
    for arc, data in generated.items():
        z.writestr(arc, data)
    for arc, src in files.items():
        z.write(os.path.join(ROOT, src), arc)
entries = []
with zipfile.ZipFile(out) as z:
    for i in z.infolist(): entries.append({'path': i.filename, 'size': i.file_size})
data = open(out, 'rb').read()
info = {'file': out, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest(), 'entries': entries}
json.dump(info, open(out + '.json', 'w'), indent=2)
print(json.dumps(info, indent=2))
