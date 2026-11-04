"""The creator's own r2modman package, laid out like a Thunderstore release (creator, 2026-11-02: "in the style of
thunderstore ... like a proper release every time ... make all the documents needed and similar for this dll version").

    python tools/r2modman_package.py <dll> <outdir> <hero id or all>      (run by tools/build_local_hero.sh --r2modman)
    python tools/r2modman_package.py --contents <zip>                    (write PACKAGE-CONTENTS.md again for a zip)

THE SOURCE IS <outdir>/package/ (DllBuilds/package/; creator, 2026-11-04: "can we just have these files here be
the default and then pull in the info every time"). Every file in it goes into the zip as laid out there, so editing a file there
(or adding one) changes the next package. The build only fills in:
    manifest.json            version_number (one up from tools/r2modman_version.txt); everything else is as written
    README.md                the "- Built with ..." line (or {BUILT_WITH}) set to this build's
    CHANGELOG.md             must have a "## <version>" section (write it first); its {BUILT_WITH}, or a "Built with" first bullet
                             when the section has none, is filled in and written back, so older sections keep their own line
    plugins/built-with.txt   always written by the build (the RoR2 build, version, date, author)
    plugins/DeadlockOfRain.dll  the DLL just built (never kept in package/)
    icon.png                 only when package/ has none: our own in-game renders of the heroes (build/pages/img)
After a successful build the values filled in are written back into package/, and the text files and manifest are copied to
tools/r2modman_docs/ so git keeps their history (charactersFinal is gitignored). If package/ is missing (a fresh clone) it is
made from tools/r2modman_docs/ and the repo's LICENSE.txt and THIRD-PARTY-NOTICES.txt.
r2modman puts the top files and plugins/' files together in BepInEx/plugins/JakeHornerMan-Risk_of_Deadlock/
(tools/r2modman_install.py does the same). PACKAGE-CONTENTS.md beside the zip lists every file in the zip, its size and what
it is, read back from the zip itself (creator, 2026-11-02: "just for my own record"). Never upload the zip: the DLL holds
Deadlock's files.
"""
import io, json, os, re, shutil, sys, time, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, 'tools', 'r2modman_docs')
MIRRORED = ('manifest.json', 'README.md', 'CHANGELOG.md', 'INSTALL.txt', 'README.txt')

WHAT = {  # what each file in the package is (PACKAGE-CONTENTS.md)
    'manifest.json': lambda m, c: f"{m['name']} by {m['author']}, {m['version_number']}, a description, and its required packs "
                                  f"({', '.join(d.rsplit('-', 1)[0] for d in m['dependencies'])})",
    'icon.png': lambda m, c: "the picture r2modman shows: the heroes' own in-game renders",
    'README.md': lambda m, c: 'the mod page r2modman shows, written like the Thunderstore page: requirements with a "Built with" line, '
                              "installation, hero pages, every hero's kit, community, credits",
    'CHANGELOG.md': lambda m, c: f"Thunderstore-style sections for {', '.join(c)}, each with its \"Built with\" line",
    'INSTALL.txt': lambda m, c: 'by hand (Part A), r2modman (Part B), updating, removing everything, problems',
    'README.txt': lambda m, c: "what the package is, what's in it, where things go on the PC",
    'plugins/DeadlockOfRain.dll': lambda m, c: "the mod (BepInEx 5 plugin), with every hero's files inside (only in the zip)",
    'plugins/LICENSE.txt': lambda m, c: "the mod's licence, as in the Thunderstore zip",
    'plugins/THIRD-PARTY-NOTICES.txt': lambda m, c: 'the libraries the project uses and their licences, as in the Thunderstore zip',
    'plugins/built-with.txt': lambda m, c: 'the Risk of Rain 2 build, the version, the date and the author (written by the build)',
}

def size(n):
    return f'{n / 1048576:.0f} MB' if n >= 1048576 else f'{n / 1024:.1f} KB' if n >= 1024 else f'{n} bytes'

def write_contents(zpath):
    """PACKAGE-CONTENTS.md beside the zip, read back from the zip."""
    outdir = os.path.dirname(os.path.abspath(zpath))
    with zipfile.ZipFile(zpath) as z:
        infos = [i for i in z.infolist() if not i.is_dir()]
        m = json.loads(z.read('manifest.json'))
        c = re.findall(r'^## (\S+)', z.read('CHANGELOG.md').decode('utf-8'), re.M)
        built = dict(l.split('=', 1) for l in z.read('plugins/built-with.txt').decode('utf-8').splitlines() if '=' in l)
    rows = [(i.filename, size(i.file_size), WHAT[i.filename](m, c) if i.filename in WHAT else '(a file you added in package/)')
            for i in infos]
    lines = [f"# {m['author']}-{m['name']} {m['version_number']}", '',
             f"Built {built.get('date', '?')} with Risk of Rain 2 build {built.get('ror2', '?')} and "
             f"{built.get('heroes', '?')}. Zip: `{os.path.basename(zpath)}` ({size(os.path.getsize(zpath))}).",
             'Written by tools/r2modman_package.py on every build, from the zip itself. For my own record: never upload the zip.', '',
             '| File | Size | What it is |', '|---|---|---|']
    lines += [f'| `{f}` | {sz} | {what} |' for f, sz, what in rows]
    lines += ['', "The files come from `package/` beside this file: edit them there and the next build packs them "
                  "(the build fills in the version and the \"Built with\" lines, and writes `plugins/built-with.txt`).",
              f"r2modman (and INSTALL.txt step A6) puts all of them, `plugins/` included, in one folder: "
              f"`BepInEx\\plugins\\{m['author']}-{m['name']}\\`.", '']
    open(os.path.join(outdir, 'PACKAGE-CONTENTS.md'), 'w', encoding='utf-8').write('\n'.join(lines))
    print(f'== package record: {os.path.join(outdir, "PACKAGE-CONTENTS.md")}')

if sys.argv[1] == '--contents':
    write_contents(sys.argv[2]); sys.exit(0)

dll, outdir, hid = sys.argv[1:4]
SRC = os.path.join(outdir, 'package')
rows = json.load(open(os.path.join(ROOT, 'design', 'heroes.json'), encoding='utf-8'))['rows']
ids = json.load(open(os.path.join(os.path.dirname(dll), 'cache', 'ready.json')))['heroes'] if hid == 'all' else [hid]

def read(rel):
    return open(os.path.join(SRC, *rel.split('/')), encoding='utf-8').read()

def write(rel, text, base=None):
    p = os.path.join(base or SRC, *rel.split('/'))
    os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, 'w', encoding='utf-8', newline='\n').write(text)

# a fresh clone: make package/ from the committed copies
if not os.path.isdir(SRC):
    for f in MIRRORED:
        shutil.copy(os.path.join(DOCS, f), os.path.join(os.makedirs(SRC, exist_ok=True) or SRC, f))
    os.makedirs(os.path.join(SRC, 'plugins'), exist_ok=True)
    for f in ('LICENSE.txt', 'THIRD-PARTY-NOTICES.txt'):
        shutil.copy(os.path.join(ROOT, f), os.path.join(SRC, 'plugins', f))
    print(f'== made {SRC} from tools/r2modman_docs')

# one version line, one patch up a build (creator, 2026-11-02: "this is 1.5 version we keep updating it with that number
# incrementally now like 1.5.1 1.5.2"); the last one used is in tools/r2modman_version.txt (committed)
vfile = os.path.join(ROOT, 'tools', 'r2modman_version.txt')
last = open(vfile).read().strip() if os.path.exists(vfile) else '1.5.0'
major, minor, patch = (int(x) for x in last.split('.'))
version = f'{major}.{minor}.{patch + 1}'

bw = json.load(open(os.path.join(ROOT, 'thunderstore', 'built-with.json'), encoding='utf-8'))
date = time.strftime('%Y-%m-%d')
built_with = f'Built with Risk of Rain 2 build {bw["ror2"]} and my own hero files (v{version}, {date}).'

changelog = read('CHANGELOG.md')
head = re.search(rf'^## {re.escape(version)}[ \t]*\n', changelog, re.M)
if not head:
    raise SystemExit(f'{SRC}\\CHANGELOG.md has no "## {version}" section: write what changed first (as for Thunderstore)')
end = re.search(r'^## ', changelog[head.end():], re.M)
end = head.end() + (end.start() if end else len(changelog[head.end():]))
section = changelog[head.end():end]
if '{BUILT_WITH}' in section:
    section = section.replace('{BUILT_WITH}', built_with)
elif 'Built with' not in section:
    section = f'- {built_with}\n' + section.lstrip('\n')
changelog = changelog[:head.end()] + section + changelog[end:]

readme = re.sub(r'^- Built with Risk of Rain 2 build .*$', '- {BUILT_WITH}', read('README.md'), flags=re.M)
readme = readme.replace('{BUILT_WITH}', built_with)
manifest = json.loads(read('manifest.json'))
manifest['version_number'] = version
built_txt = ''.join(f'{k}={v}\n' for k, v in {'ror2': bw['ror2'], 'heroes': 'own files (charactersFinal)', 'date': date,
                                              'version': version, 'author': manifest['author']}.items())
filled = {'manifest.json': json.dumps(manifest, indent=2) + '\n', 'README.md': readme, 'CHANGELOG.md': changelog,
          'plugins/built-with.txt': built_txt}

if not os.path.exists(os.path.join(SRC, 'icon.png')):
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
    icon.convert('RGB').save(os.path.join(SRC, 'icon.png'), 'PNG')

# every file in package/ as laid out there, with the filled-in ones swapped in; the DLL always the one just built
files = []
for d, _, fs in os.walk(SRC):
    for f in fs:
        rel = os.path.relpath(os.path.join(d, f), SRC).replace(os.sep, '/')
        if not rel.lower().endswith('.dll'): files.append(rel)
order = ['manifest.json', 'icon.png', 'README.md', 'CHANGELOG.md', 'INSTALL.txt', 'README.txt']
files = sorted(set(files) | set(filled), key=lambda r: (r.startswith('plugins/'), order.index(r) if r in order else 99, r))
os.makedirs(outdir, exist_ok=True)
for f in os.listdir(outdir):
    if f.endswith('.zip'): os.remove(os.path.join(outdir, f))
zpath = os.path.join(outdir, f"{manifest['author']}-{manifest['name']}-{version}.zip")
with zipfile.ZipFile(zpath, 'w', zipfile.ZIP_DEFLATED) as z:
    for rel in files:
        if rel in filled: z.writestr(rel, filled[rel])
        else: z.write(os.path.join(SRC, *rel.split('/')), rel)
        if rel == 'plugins/built-with.txt': z.write(dll, 'plugins/DeadlockOfRain.dll')

# built: the filled-in values back into package/, the text files to tools/r2modman_docs (git history), the version used
for rel, text in filled.items():
    write(rel, text)
for f in MIRRORED:
    shutil.copy(os.path.join(SRC, f), os.path.join(DOCS, f))
write_contents(zpath)
open(vfile, 'w').write(version + '\n')
names = [next(r['displayName'] for r in rows if r['id'] == h) for h in ids]
print(f'== r2modman zip ({len(ids)} heroes: {", ".join(names)}): {zpath}')
