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
    icon.png                 only when package/ has none: our own in-game renders of the heroes in rows (build/pages/img);
                             python tools/r2modman_package.py --icon draws it again
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
    'Join Discord.txt': lambda m, c: 'the invite link to the Risk of Deadlock Discord',
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

def make_icon(ids, path, size=256):
    """icon.png: a class photo of our own in-game renders (build/pages/img/<id>.webp, tools/portraits.py), standing on curved
    steps, each row a step higher and a little smaller than the one in front, so every face shows (creator, 2026-11-04: "each
    character render in rows and have each row slightly above the other" / "make it more like a class photo and arc with steps
    above each"). The steps curve like bleachers: the middle of each row stands higher, the ends come forward. Back row first."""
    from PIL import Image, ImageDraw, ImageFilter
    S = 4 * size  # drawn large, scaled down at the end
    icon = Image.new('RGBA', (S, S))
    px = icon.load()
    for y in range(S):
        for x in range(S):
            d = min(1.0, (((x - S / 2) / (S * 0.62)) ** 2 + ((y - S * 0.35) / (S * 0.55)) ** 2) ** 0.5)
            k = (1 - d) ** 1.6
            px[x, y] = (int(40 + 200 * k), int(30 + 130 * k), int(26 + 70 * k), 255)
    img = lambda h: os.path.join(ROOT, 'build', 'pages', 'img', h + '.webp')
    heroes = [h for h in ids if os.path.exists(img(h))]
    # narrow (tall) renders at the back, wide (short) ones like Mo & Krill and Rat King in front, so no one hides a face
    def aspect(h):
        bb = Image.open(img(h)).getbbox()
        return (bb[2] - bb[0]) / (bb[3] - bb[1])
    heroes.sort(key=aspect)
    n = len(heroes)
    nrows = 1 if n <= 3 else 2 if n <= 6 else 3
    counts = [n // nrows] * nrows          # fewer at the back: 11 -> 3, 4, 4 (back to front)
    for i in range(n - sum(counts)): counts[nrows - 1 - i] += 1
    rows, k = [], 0
    for c in counts: rows.append(heroes[k:k + c]); k += c

    ARC = S * 0.05                         # how much higher the middle of a row stands than its ends
    TREAD, RISE = S * 0.035, S * 0.20      # a step's top surface and the height of its front
    def feet(row_base, x):                 # where a row's feet are at x: an arc, the middle higher
        u = (x - S / 2) / (S / 2)
        return row_base - ARC * (1 - u * u)
    def step(row_base, shade):
        # the step this row stands on: a curved tread (lit) and its front face (darker) down past the next row's feet
        xs = [S * i / 64 for i in range(65)]
        top = [(x, feet(row_base, x) - TREAD * 0.5) for x in xs]
        lip = [(x, feet(row_base, x) + TREAD * 0.5) for x in xs]
        bottom = [(x, feet(row_base, x) + RISE) for x in xs]
        d = ImageDraw.Draw(icon)
        d.polygon(lip + bottom[::-1], fill=(int(58 * shade), int(40 * shade), int(30 * shade), 255))
        d.polygon(top + lip[::-1], fill=(int(140 * shade), int(102 * shade), int(72 * shade), 255))
        d.line(lip, fill=(int(190 * shade), int(150 * shade), int(110 * shade), 255), width=max(2, S // 256))

    for r, row in enumerate(rows):  # r = 0 is the back row
        front = r - (nrows - 1)          # 0 for the front row, -1, -2 behind it
        tall = S * (0.44 + 0.045 * front) if nrows > 1 else S * 0.70
        base = S * (0.93 + 0.235 * front) if nrows > 1 else S * 0.93
        step(base, 0.75 + 0.12 * (r / max(1, nrows - 1)))
        margin = S * 0.07
        slot = (S - 2 * margin) / len(row)
        shift = (slot / 4) * (1 if (nrows - 1 - r) % 2 else -1) if nrows > 1 and counts.count(len(row)) > 1 else 0
        for j, h in enumerate(row):
            im = Image.open(img(h)).convert('RGBA')
            im = im.crop(im.getbbox())
            t = tall
            w = im.width * t / im.height
            if w > slot * 1.35: w, t = slot * 1.35, im.height * slot * 1.35 / im.width
            im = im.resize((round(w), round(t)), Image.LANCZOS)
            cx = margin + slot * (j + 0.5) + shift
            cx = min(max(cx, w / 2 + S * 0.01), S - w / 2 - S * 0.01)  # never cut off at the sides
            x, y = round(cx - w / 2), round(feet(base, cx) - t + TREAD * 0.2)
            # a soft dark edge so the rows read apart, and a shadow on the step
            shadow = Image.new('RGBA', im.size, (0, 0, 0, 0))
            shadow.putalpha(im.getchannel('A').point(lambda v: min(255, v * 2)))
            pad = Image.new('RGBA', (im.width + 40, im.height + 40), (0, 0, 0, 0))
            pad.alpha_composite(shadow, (20, 20))
            pad = pad.filter(ImageFilter.GaussianBlur(8))
            icon.alpha_composite(pad, (x - 20, y - 20 + 6))
            icon.alpha_composite(im, (x, y))
    icon.resize((size, size), Image.LANCZOS).convert('RGB').save(path, 'PNG')
    print(f'== icon: {path} ({n} heroes on {nrows} curved steps, {", ".join(map(str, counts))} back to front)')

if sys.argv[1] == '--contents':
    write_contents(sys.argv[2]); sys.exit(0)
if sys.argv[1] == '--icon':  # python tools/r2modman_package.py --icon [out.png]: draw the icon again (all heroes)
    order = [r['id'] for r in json.load(open(os.path.join(ROOT, 'design', 'heroes.json'), encoding='utf-8'))['rows']]
    make_icon(order, sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'DllBuilds', 'package', 'icon.png')); sys.exit(0)

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
built_with = f'Built with Risk of Rain 2 build {bw["ror2"]} ({date}).'

changelog = read('CHANGELOG.md')
head = re.search(rf'^## {re.escape(version)}[ \t]*\n', changelog, re.M)
if not head:
    # no section written (the creator edits CHANGELOG only for a new character, DllBuilds/CLAUDE.md): add one above the newest
    first = re.search(r'^## ', changelog, re.M)
    at = first.start() if first else len(changelog)
    changelog = changelog[:at] + f'## {version}\n\n' + changelog[at:]
    head = re.search(rf'^## {re.escape(version)}[ \t]*\n', changelog, re.M)
end = re.search(r'^## ', changelog[head.end():], re.M)
end = head.end() + (end.start() if end else len(changelog[head.end():]))
section = changelog[head.end():end]
if '{BUILT_WITH}' in section:
    section = section.replace('{BUILT_WITH}', built_with)
elif 'Built with' not in section:
    section = f'- {built_with}\n' + (section.lstrip('\n') or '\n')
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
    make_icon(ids, os.path.join(SRC, 'icon.png'))

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
