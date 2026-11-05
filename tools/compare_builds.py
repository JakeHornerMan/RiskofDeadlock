"""Are the heroes in the creator's own DLL build the same as in the Melty build? (creator, 2026-11-02: "validate all heros are
the saem from deadlock melty to the ddl version")

    python tools/compare_builds.py [<melty cache> [<dll cache>]]

The Melty build reads every hero from Deadlock into %LOCALAPPDATA%/DeadlockOfRain/cache; the DLL build
(tools/build_local_hero.sh all) carries build/local-all/cache inside the DLL. Both run the same plugin code (the DLL build only adds
Runtime/LocalHero.cs, which says where the files are), so a hero looks and plays the same when its files are the same. For every
hero this checks each file it uses (its model and textures in heroes/<id>/, and the props, icons, sounds and voice lines
extractor/manifest_owners.json gives it) byte for byte, and the sound events by content. Exit code 1 when anything differs.
"""
import hashlib, json, os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
melty = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.environ['LOCALAPPDATA'], 'DeadlockOfRain', 'cache')
dll = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'build', 'local-all', 'cache')


def md5(p):
    h = hashlib.md5()
    with open(p, 'rb') as f:
        for b in iter(lambda: f.read(1 << 20), b''): h.update(b)
    return h.hexdigest()


def sounds(c):
    p = os.path.join(c, 'sounds', 'sounds.json')
    return json.load(open(p, encoding='utf-8')) if os.path.exists(p) else {}


for c in (melty, dll):
    if not os.path.exists(os.path.join(c, 'ready.json')):
        print(f'No ready.json in {c}: that build has no finished cache.'); sys.exit(1)
heroes = json.load(open(os.path.join(melty, 'ready.json')))['heroes']
dll_heroes = json.load(open(os.path.join(dll, 'ready.json')))['heroes']
owners = json.load(open(os.path.join(ROOT, 'extractor', 'manifest_owners.json'), encoding='utf-8'))
sm, sd = sounds(melty), sounds(dll)

bad = 0
print(f'Melty build: {melty}\nDLL build:   {dll}')
for h in heroes:
    files, events, why = set(), [], []
    hd = os.path.join(melty, 'heroes', h)
    for base, _, fs in os.walk(hd):
        files.update(os.path.relpath(os.path.join(base, f), melty) for f in fs)
    for k, who in owners.items():
        if h not in who: continue
        if k.endswith('.vtex'): files.add(os.path.join('images', k.replace('.vtex', '').replace('/', '_') + '.png'))
        elif os.path.isdir(os.path.join(melty, 'props', k)):
            for base, _, fs in os.walk(os.path.join(melty, 'props', k)):
                files.update(os.path.relpath(os.path.join(base, f), melty) for f in fs)
        else:
            e = sm.get(k) or sm.get('vo:' + k)
            if e is None: continue
            events.append(k if k in sm else 'vo:' + k)
            files.update(os.path.join('sounds', f) for f in e.get('files', []))
    if h not in dll_heroes: why.append('not in the DLL build at all')
    same = 0
    for rel in sorted(files):
        a, b = os.path.join(melty, rel), os.path.join(dll, rel)
        if not os.path.exists(a): continue   # Melty itself lacks it (an icon Deadlock didn't have): nothing to compare
        if not os.path.exists(b): why.append('missing: ' + rel.replace(os.sep, '/'))
        elif md5(a) != md5(b): why.append('different: ' + rel.replace(os.sep, '/'))
        else: same += 1
    for e in events:
        if sd.get(e) != sm.get(e): why.append('sound event differs: ' + e)
    if why:
        bad += 1
        print(f'  {h:9} DIFFERENT ({same} files the same)\n      ' + '\n      '.join(why[:20]) + ('\n      ...' if len(why) > 20 else ''))
    else:
        print(f'  {h:9} same: {same} files, {len(events)} sound events')

# heroes only the DLL build has (heroes.json localOnly: Victor, the creator's own files, never in the Melty build)
for h in dll_heroes:
    if h not in heroes: print(f'  {h:9} own files only (not in the Melty build)')

# everything else both builds share (icons of buffs and items, shared props)
extra = []
for base, _, fs in os.walk(melty):
    for f in fs:
        rel = os.path.relpath(os.path.join(base, f), melty)
        if rel in ('ready.json', 'extract.log', 'progress.txt') or rel == os.path.join('sounds', 'sounds.json'): continue
        b = os.path.join(dll, rel)
        if not os.path.exists(b): extra.append('missing: ' + rel.replace(os.sep, '/'))
        elif md5(os.path.join(base, f)) != md5(b): extra.append('different: ' + rel.replace(os.sep, '/'))
if sm != sd: extra.append(f'sounds.json differs ({len(sm)} events in Melty, {len(sd)} in the DLL)')
print('  shared    ' + ('same' if not extra else 'DIFFERENT\n      ' + '\n      '.join(extra[:20])))

# the code: the DLL is built from this checkout's plugin/ (only LocalHero added)
try:
    head = subprocess.run(['git', '-C', ROOT, 'log', '-1', '--format=%h %s'], capture_output=True, text=True).stdout.strip()
    dirty = subprocess.run(['git', '-C', ROOT, 'status', '--porcelain', '--', 'plugin', 'design'], capture_output=True, text=True).stdout.strip()
    print(f'Code: both builds compile plugin/ of {head[:60]}' + (' plus uncommitted changes' if dirty else ''))
except OSError:
    pass
n = len(heroes)
print(f'Result: {n - bad}/{n} heroes the same' + ('' if not extra else ', shared files differ'))
sys.exit(1 if bad or extra else 0)
