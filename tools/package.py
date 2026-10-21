"""Build a release zip. It holds only our own code, the bundled MIT/BSD/Apache tools with notices, and docs.
Nothing from Risk of Rain 2 or Deadlock is included: the extractor reads Deadlock on the player's PC.

  python tools/package.py <version>                 Melty zip
    plugins/DeadlockOfRain/   -> {game}/BepInEx/plugins/DeadlockOfRain
    extract/                  -> {localappdata}/DeadlockOfRain/extract   (the recipe runs it before the first start)
  python tools/package.py <version> --release       the same Melty zip cut down to thunderstore/release.json's heroes
    (plugins/DeadlockOfRain/heroes.txt + a cut extractor manifest), e.g. the four originals without Mo & Krill

  python tools/package.py <version> --melty         the Melty zip cut to thunderstore/release.json's melty heroes, with its
    comingSoon heroes as greyed locked tiles (coming-soon.txt; their portrait stays in the manifest, nothing else of them)

  python tools/package.py <version> --thunderstore  Thunderstore zip (needs the GitHub-built exe: tools/fetch_extractor.py;
    r2modman installs plugins/ into
    BepInEx/plugins/<team>-DeadlockOfRain/; the plugin runs plugins/extract/DeadlockExtract.exe itself, FirstRun.cs)
    manifest.json, icon.png, README.md, CHANGELOG.md   Thunderstore's own files (thunderstore/)
    plugins/DeadlockOfRain.dll, plugins/extract/...    the extractor's asset list is deadlock-manifest.json there
    plugins/heroes.txt                                 the release's heroes (thunderstore/release.json); the plugin
                                                       builds only these, and the manifest is cut down to them

Every zip also holds built-with.txt beside the DLL: the creator's own Risk of Rain 2 Steam build and Deadlock ClientVersion,
read from this PC's installs at packaging time (thunderstore/built-with.json keeps the last one). The plugin names both
pairs (built with / this PC) in every problem message, and this script writes the same line into README.md,
thunderstore/README.md and the version's thunderstore/CHANGELOG.md section. It refuses to package when a version can't be
read, when plugin/Plugin.cs has another Version, or when CHANGELOG.md has no section for the version.
"""
import datetime, hashlib, io, json, os, re, sys, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
args = [a for a in sys.argv[1:] if not a.startswith('--')]
version = args[0] if args else '0.1.0'
thunderstore = '--thunderstore' in sys.argv
cut = '--release' in sys.argv
melty = '--melty' in sys.argv
TS = os.path.join(ROOT, 'thunderstore')
EXTRACT = {
    'DeadlockExtract.exe': 'build/extract/DeadlockExtract.exe',
    'THIRD-PARTY-NOTICES.txt': 'THIRD-PARTY-NOTICES.txt',
    'LICENSE.txt': 'LICENSE.txt',
}


RELEASE = json.load(open(os.path.join(TS, 'release.json'), encoding='utf-8'))
HERO_ROWS = {r['id']: r for r in json.load(open(os.path.join(ROOT, 'design', 'heroes.json'), encoding='utf-8'))['rows']}
SHEET_HEROES = {i: r['displayName'] for i, r in HERO_ROWS.items()}
# --melty ships release.json's melty heroes (and shows comingSoon greyed); --release and Thunderstore its heroes
CUT_HEROES = RELEASE['melty'] if melty else RELEASE['heroes']
SOON = RELEASE.get('comingSoon', []) if melty else []


STEAM = r'C:\Program Files (x86)\Steam'
ROR2_DIR = os.environ.get('RoR2Dir') or os.path.join(STEAM, 'steamapps', 'common', 'Risk of Rain 2')
DEADLOCK_DIR = os.environ.get('DEADLOCKOFRAIN_DEADLOCK') or os.path.join(STEAM, 'steamapps', 'common', 'Deadlock')
BUILT_RE = r'^- Built with Risk of Rain 2 build .*$'


def built_with():
    """The creator's Risk of Rain 2 Steam build (appmanifest_632360.acf) and Deadlock ClientVersion (steam.inf)."""
    acf = os.path.join(os.path.dirname(os.path.dirname(ROR2_DIR)), 'appmanifest_632360.acf')
    inf = os.path.join(DEADLOCK_DIR, 'game', 'citadel', 'steam.inf')
    m = re.search(r'"buildid"\s+"(\d+)"', open(acf, encoding='utf-8').read()) if os.path.exists(acf) else None
    d = re.search(r'^ClientVersion=(\d+)', open(inf, encoding='utf-8').read(), re.M) if os.path.exists(inf) else None
    if not m or not d:
        raise SystemExit(f'cannot read the build versions (Risk of Rain 2: {acf}, Deadlock: {inf}); set RoR2Dir / DEADLOCKOFRAIN_DEADLOCK')
    bw = {'ror2': m.group(1), 'deadlock': d.group(1), 'date': datetime.date.today().isoformat(), 'version': version}
    json.dump(bw, open(os.path.join(TS, 'built-with.json'), 'w', encoding='utf-8'), indent=2)
    return bw


def built_with_line(bw):
    return f'Built with Risk of Rain 2 build {bw["ror2"]} and Deadlock build {bw["deadlock"]} (v{version}, {bw["date"]}).'


def stamp_texts(bw):
    """README.md and thunderstore/README.md: one 'Built with' line under Requirements (replaced on every package);
    thunderstore/CHANGELOG.md: the line at the top of this version's section, which must exist."""
    line = '- ' + built_with_line(bw)
    for f in [os.path.join(ROOT, 'README.md'), os.path.join(TS, 'README.md')]:
        text = open(f, encoding='utf-8').read()
        if re.search(BUILT_RE, text, re.M):
            text = re.sub(BUILT_RE, line, text, flags=re.M)
        else:
            head = next((h for h in ['## Requirements\n', '## You need Deadlock installed\n'] if h in text), None)
            if head is None: raise SystemExit(f'{f} has no "## Requirements" section for the Built with line')
            text = text.replace(head, head + line + '\n', 1)
        open(f, 'w', encoding='utf-8', newline='\n').write(text)
    f = os.path.join(TS, 'CHANGELOG.md')
    lines = open(f, encoding='utf-8').read().split('\n')
    if f'## {version}' not in lines:
        raise SystemExit(f'thunderstore/CHANGELOG.md has no "## {version}" section: write what changed first')
    i = lines.index(f'## {version}') + 1
    if i < len(lines) and re.match(BUILT_RE, lines[i]): lines[i] = line
    else: lines.insert(i, line)
    open(f, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines))


def check_plugin_version():
    v = re.search(r'Version = "([^"]+)"', open(os.path.join(ROOT, 'plugin', 'Plugin.cs'), encoding='utf-8').read()).group(1)
    if v != version: raise SystemExit(f'plugin/Plugin.cs says Version = "{v}", not {version}: bump it and rebuild first')


def release_manifest():
    """extractor/manifest.json cut down to the release's heroes (owners from tools/gen.py; unowned entries stay)."""
    m = json.load(open(os.path.join(ROOT, 'extractor', 'manifest.json'), encoding='utf-8'))
    owners = json.load(open(os.path.join(ROOT, 'extractor', 'manifest_owners.json'), encoding='utf-8'))
    keep = set(CUT_HEROES)
    used = lambda e: e not in owners or bool(keep & set(owners[e]))
    m['heroes'] = [h for h in m['heroes'] if h['id'] in keep]
    # a coming-soon hero keeps only its select-screen portrait
    portraits = {HERO_ROWS[h]['imageSmall'] for h in SOON}
    for k in ['images', 'soundEvents', 'soundEventFiles', 'voiceGroups', 'voiceAllTakes']:
        m[k] = [e for e in m.get(k, []) if used(e) or (k == 'images' and e in portraits)]
    m['props'] = [p for p in m.get('props', []) if used(p['id'])]
    return json.dumps(m, indent=2).encode('utf-8')


def check_store_text():
    """The store README/CHANGELOG are written by hand for each release: refuse to package one naming a hero not in it."""
    for f in ['README.md', 'CHANGELOG.md']:
        text = open(os.path.join(TS, f), encoding='utf-8').read()
        for hid, name in SHEET_HEROES.items():
            if hid not in RELEASE['heroes'] and name in text:
                raise SystemExit(f'thunderstore/{f} mentions {name}, who is not in thunderstore/release.json')


def icon_png():
    """256x256 icon: the release's heroes from our own in-game renders (build/pages/img, tools/portraits.py); no Deadlock art."""
    from PIL import Image
    # warm sandstorm glow behind them (brass over near-black), so dark outfits still read
    canvas = Image.new('RGBA', (256, 256))
    px = canvas.load()
    for y in range(256):
        for x in range(256):
            d = min(1.0, (((x - 128) / 150) ** 2 + ((y - 120) / 130) ** 2) ** 0.5)
            k = (1 - d) ** 1.6
            px[x, y] = (int(24 + 190 * k), int(18 + 120 * k), int(16 + 60 * k), 255)
    # side by side, the outer ones a little smaller and drawn first, so the middle overlaps them
    ids = RELEASE['heroes']
    n = len(ids)
    placed = []
    for i, h in enumerate(ids):
        mid = abs(i - (n - 1) / 2) / max(1, (n - 1) / 2)          # 0 in the middle, 1 at the edges
        cx = round(256 * (i + 0.5) / n)
        tall = round((150 if h == 'mokrill' else 196) - 22 * mid)
        placed.append((mid, h, cx, tall))
    for _, h, cx, tall in sorted(placed, reverse=True):
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
    m['name'] = RELEASE['name']
    return json.dumps(m, indent=2).encode('utf-8')


check_plugin_version()
BW = built_with()
stamp_texts(BW)
BW_TXT = ''.join(f'{k}={v}\n' for k, v in BW.items()).encode('utf-8')
print(built_with_line(BW))

def check_github_exe():
    """The Thunderstore zip ships only the GitHub-built exe (tools/fetch_extractor.py), so its SHA-256 is the public one."""
    f = os.path.join(TS, 'extractor.json')
    if not os.path.exists(f): raise SystemExit('no thunderstore/extractor.json: run python tools/fetch_extractor.py <extractor version> first')
    want = json.load(open(f, encoding='utf-8'))
    got = hashlib.sha256(open(os.path.join(ROOT, EXTRACT['DeadlockExtract.exe']), 'rb').read()).hexdigest()
    if got != want['sha256']:
        raise SystemExit(f'build/extract/DeadlockExtract.exe is not the GitHub-built {want["version"]} ({want["url"]}): run python tools/fetch_extractor.py {want["version"]}')


if thunderstore:
    check_store_text()
    check_github_exe()
    out = os.path.join(ROOT, 'build', f'{RELEASE["name"]}-{version}-thunderstore.zip')
    files = {
        'README.md': 'thunderstore/README.md',
        'CHANGELOG.md': 'thunderstore/CHANGELOG.md',
        'plugins/DeadlockOfRain.dll': 'plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll',
        'plugins/LICENSE.txt': 'LICENSE.txt',
        # the DLL holds code adapted from RoR2BepInExPack (MIT): its notice goes beside it too
        'plugins/THIRD-PARTY-NOTICES.txt': 'THIRD-PARTY-NOTICES.txt',
    }
    files.update({'plugins/extract/' + k: v for k, v in EXTRACT.items()})
    generated = {'manifest.json': ts_manifest(), 'icon.png': icon_png(),
                 'plugins/heroes.txt': ''.join(h + '\n' for h in RELEASE['heroes']).encode('utf-8'),
                 'plugins/built-with.txt': BW_TXT,
                 'plugins/extract/deadlock-manifest.json': release_manifest()}
else:
    out = os.path.join(ROOT, 'build', f'DeadlockOfRain-{version}.zip')
    files = {
        'plugins/DeadlockOfRain/DeadlockOfRain.dll': 'plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll',
        'plugins/DeadlockOfRain/README.md': 'README.md',
        'plugins/DeadlockOfRain/LICENSE.txt': 'LICENSE.txt',
        # the DLL holds code adapted from RoR2BepInExPack (MIT): its notice goes beside it too
        'plugins/DeadlockOfRain/THIRD-PARTY-NOTICES.txt': 'THIRD-PARTY-NOTICES.txt',
        'extract/manifest.json': 'extractor/manifest.json',
    }
    files.update({'extract/' + k: v for k, v in EXTRACT.items()})
    # heroes.txt every time: a full zip lists every sheet hero, so an update overwrites an older release's cut-down list
    # (0.7.1 shipped four) even if the installer leaves old files behind
    generated = {'plugins/DeadlockOfRain/built-with.txt': BW_TXT,
                 'plugins/DeadlockOfRain/heroes.txt': ''.join(h + '\n' for h in SHEET_HEROES).encode('utf-8')}
    if cut or melty:
        del files['extract/manifest.json']
        generated.update({'plugins/DeadlockOfRain/heroes.txt': ''.join(h + '\n' for h in CUT_HEROES).encode('utf-8'),
                          'extract/manifest.json': release_manifest()})
    # every Melty zip writes the list (empty in a full zip), so an update clears an older release's coming-soon tiles
    generated['plugins/DeadlockOfRain/coming-soon.txt'] = ''.join(h + '\n' for h in SOON).encode('utf-8')

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
