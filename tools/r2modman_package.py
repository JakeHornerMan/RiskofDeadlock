"""The creator's own r2modman package, laid out like a Thunderstore release (creator, 2026-11-02: "in the style of
thunderstore ... like a proper release every time ... make all the documents needed and similar for this dll version").

    python tools/r2modman_package.py <dll> <outdir> <hero id or all>      (run by tools/build_local_hero.sh --r2modman)

    manifest.json            name Risk_of_Deadlock, author JakeHornerMan, version 1.5.x, description, dependencies
    icon.png                 our own in-game renders of the heroes (build/pages/img), as package.py's icon
    README.md                tools/r2modman_docs/README.md (the Thunderstore page's style), {BUILT_WITH} filled in
    CHANGELOG.md             tools/r2modman_docs/CHANGELOG.md, {BUILT_WITH} filled in; must have a "## <version>" section
    INSTALL.txt, README.txt  the install guide and what the package is (tools/r2modman_docs)
    plugins/DeadlockOfRain.dll, LICENSE.txt, THIRD-PARTY-NOTICES.txt, built-with.txt
r2modman puts the top files and plugins/' files together in BepInEx/plugins/JakeHornerMan-Risk_of_Deadlock/
(tools/r2modman_install.py does the same). The version is one up from tools/r2modman_version.txt, used only once the
changelog has its section. The guides are also copied beside the zip. Never upload the zip: the DLL holds Deadlock's files.
"""
import io, json, os, shutil, sys, time, zipfile

dll, outdir, hid = sys.argv[1:4]
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, 'tools', 'r2modman_docs')
AUTHOR, NAME = 'JakeHornerMan', 'Risk_of_Deadlock'
rows = json.load(open(os.path.join(ROOT, 'design', 'heroes.json'), encoding='utf-8'))['rows']
ids = json.load(open(os.path.join(os.path.dirname(dll), 'cache', 'ready.json')))['heroes'] if hid == 'all' else [hid]

# one version line, one patch up a build (creator, 2026-11-02: "this is 1.5 version we keep updating it with that number
# incrementally now like 1.5.1 1.5.2"); the last one used is in tools/r2modman_version.txt (committed)
vfile = os.path.join(ROOT, 'tools', 'r2modman_version.txt')
last = open(vfile).read().strip() if os.path.exists(vfile) else '1.5.0'
major, minor, patch = (int(x) for x in last.split('.'))
version = f'{major}.{minor}.{patch + 1}'
changelog = open(os.path.join(DOCS, 'CHANGELOG.md'), encoding='utf-8').read()
if f'\n## {version}\n' not in changelog:
    raise SystemExit(f'tools/r2modman_docs/CHANGELOG.md has no "## {version}" section: write what changed first (as for Thunderstore)')

bw = json.load(open(os.path.join(ROOT, 'thunderstore', 'built-with.json'), encoding='utf-8'))
date = time.strftime('%Y-%m-%d')
built_with = f'Built with Risk of Rain 2 build {bw["ror2"]} and my own hero files (v{version}, {date}).'
names = [next(r['displayName'] for r in rows if r['id'] == h) for h in ids]
manifest = {'name': NAME, 'author': AUTHOR, 'version_number': version,
            'website_url': 'https://github.com/JakeHornerMan/RiskofDeadlock',
            'description': f'All {len(ids)} Deadlock heroes as Risk of Rain 2 survivors, from my own files built into the DLL: '
                           'no Deadlock install needed. JakeHornerMan\'s personal build, never shared.'[:250],
            'dependencies': ['bbepis-BepInExPack-5.4.2122', 'RiskofThunder-RoR2BepInExPack-1.49.0']}
built_txt = ''.join(f'{k}={v}\n' for k, v in {'ror2': bw['ror2'], 'heroes': 'own files (charactersFinal)', 'date': date,
                                              'version': version, 'author': AUTHOR}.items())

from PIL import Image
icon = Image.new('RGBA', (256, 256))
px = icon.load()
for y in range(256):
    for x in range(256):
        d = min(1.0, (((x - 128) / 150) ** 2 + ((y - 120) / 130) ** 2) ** 0.5)
        k = (1 - d) ** 1.6
        px[x, y] = (int(24 + 190 * k), int(18 + 120 * k), int(16 + 60 * k), 255)
# our own in-game renders (tools/portraits.py) side by side, the outer ones smaller and drawn first (as package.py's icon)
renders = [h for h in ids if os.path.exists(os.path.join(ROOT, 'build', 'pages', 'img', h + '.webp'))]
n = len(renders)
placed = []
for j, h in enumerate(renders):
    mid = abs(j - (n - 1) / 2) / max(1, (n - 1) / 2)
    placed.append((mid, h, round(256 * (j + 0.5) / n), round((200 if n == 1 else 170) - 40 * mid)))
for _, h, cx, tall in sorted(placed, reverse=True):
    im = Image.open(os.path.join(ROOT, 'build', 'pages', 'img', h + '.webp')).convert('RGBA')
    im = im.crop(im.getbbox())
    w = round(im.width * tall / im.height)
    if w > 240: w, tall = 240, round(im.height * 240 / im.width)
    im = im.resize((w, tall), Image.LANCZOS)
    icon.alpha_composite(im, (cx - w // 2, 256 - tall))
buf = io.BytesIO(); icon.convert('RGB').save(buf, 'PNG')

def doc(f):
    return open(os.path.join(DOCS, f), encoding='utf-8').read().replace('{BUILT_WITH}', built_with)

top = {'manifest.json': json.dumps(manifest, indent=2) + '\n', 'README.md': doc('README.md'), 'CHANGELOG.md': doc('CHANGELOG.md'),
       'INSTALL.txt': doc('INSTALL.txt'), 'README.txt': doc('README.txt')}
os.makedirs(outdir, exist_ok=True)
for f in os.listdir(outdir):
    if f.endswith('.zip'): os.remove(os.path.join(outdir, f))
zpath = os.path.join(outdir, f'{AUTHOR}-{NAME}-{version}.zip')
with zipfile.ZipFile(zpath, 'w', zipfile.ZIP_DEFLATED) as z:
    for f, text in top.items():
        z.writestr(f, text)
    z.writestr('icon.png', buf.getvalue())
    z.write(dll, 'plugins/DeadlockOfRain.dll')
    z.write(os.path.join(ROOT, 'LICENSE.txt'), 'plugins/LICENSE.txt')
    z.write(os.path.join(ROOT, 'THIRD-PARTY-NOTICES.txt'), 'plugins/THIRD-PARTY-NOTICES.txt')
    z.writestr('plugins/built-with.txt', built_txt)
# the guides beside the zip too (the folder keeps only the newest zip; nothing else in it is touched)
for f in ('README.md', 'CHANGELOG.md', 'INSTALL.txt', 'README.txt'):
    open(os.path.join(outdir, f), 'w', encoding='utf-8').write(top[f])
open(vfile, 'w').write(version + '\n')
print(f'== r2modman zip ({len(ids)} heroes: {", ".join(names)}): {zpath}')
