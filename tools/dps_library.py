"""Builds the stored DPS library, docs/dps-library.json, from benchmark result files.

The library holds every survivor's measured damage (the 12-enemy pack test, doc 05 §2) so a new hero only needs its own
bench run (`bash tools/run_bench.sh 30 <hero>`) and is then compared with these stored numbers; the vanilla survivors
don't change, so they are not re-measured. The heroes' entries are tagged with the build they were measured on; re-measure
a hero (and update its entry) whenever its kit changes.

usage:
  python tools/dps_library.py build/library/run1.txt build/library/run2.txt ...   (rebuild from these result files)
  python tools/dps_library.py --add build/bench_results.txt                       (merge one more run into the library)

Each input is a build/bench_results.txt (one run, or several headed "run <k>" when REPEAT was used). Per survivor and per
pass it stores every run's %/s and the mean, min and max; plus the survivor's base stats and default skill names."""
import json, os, re, sys, datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIB = os.path.join(ROOT, 'docs', 'dps-library.json')
MODES = ['all', 'primary', 'secondary', 'utility', 'special', 'equipment']
VERSION = 'v0.9.0'  # the build heroes are measured on; bump it when a hero's kit changes

def parse(path):
    """-> list of runs; each run = {who: {mode: {'dps', 'total', 'hits', 'enemies'}}}, plus stats and skills per who."""
    runs, cur, stats, skills = [], {}, {}, {}
    for line in open(path, encoding='utf-8'):
        line = line.strip()
        if re.match(r'run \d+$', line):
            if cur: runs.append(cur)
            cur = {}; continue
        m = re.match(r'(\S+) (all|primary|secondary|utility|special|equipment): .*?([\d.]+)% of damage \((\d+)%/s\), hits (\d+), enemies hit (\d+)', line)
        if m:
            cur.setdefault(m.group(1), {})[m.group(2)] = dict(dps=int(m.group(4)), total=float(m.group(3)), hits=int(m.group(5)), enemies=int(m.group(6)))
            continue
        m = re.match(r'stats (\S+): name (.*?), health ([\d.]+) \+([\d.]+), regen ([\d.]+) \+([\d.]+), armor ([\d.]+), damage ([\d.]+) \+([\d.]+), speed ([\d.]+), jumps (\d+), crit ([\d.]+)', line)
        if m:
            g = m.groups()
            stats[g[0]] = dict(name=g[1], health=float(g[2]), healthPerLevel=float(g[3]), regen=float(g[4]), regenPerLevel=float(g[5]),
                               armor=float(g[6]), damage=float(g[7]), damagePerLevel=float(g[8]), moveSpeed=float(g[9]), jumps=int(g[10]), crit=float(g[11]))
            continue
        m = re.match(r'skills (\S+): (.*)$', line)
        if m:
            skills[m.group(1)] = dict(kv.strip().split('=', 1) for kv in m.group(2).split(';') if '=' in kv)
    if cur: runs.append(cur)
    return runs, stats, skills

# what the 12-enemy pack test cannot show for some survivors; stored with the numbers so later readers know
NOTES = {
 'Croco': "Acrid's Poison and Blight deal a share of the target's max health; the test enemies have x101 health to survive, so his number is hugely inflated. Not comparable.",
 'Railgunner': 'Her damage is scoped railgun shots (hold M2, fire M1); the bench taps M2, so she is badly under-measured. Not comparable.',
 'VoidSurvivor': "Void Fiend's corrupted form is never reached in 30 s and his beam hits few of the pack; under-measured.",
 'Captain': 'His Vulcan Shotgun fires on release; the bench lets go of M1 every 0.6 s. Orbital strikes and beacons are aimed at the pack.',
 'Engi': 'Turret damage counts as his. Damage stat 14 (most survivors 12).',
 'Loader': 'Very noisy between runs: Charged Gauntlet and Pylon sometimes kill pack members, which the bench refills.',
 'Mage': 'Noisy between runs (Flame Bolt burns).',
}

def summary(values):
    return dict(runs=values, mean=round(sum(values) / len(values)), min=min(values), max=max(values))

def build(paths, base=None):
    lib = base or dict(survivors={})
    raw = {}  # who -> mode -> [dps...]
    fresh = set()  # survivors measured in the files being added
    for who, e in lib['survivors'].items():
        for mode in MODES:
            if mode in e.get('dps', {}): raw.setdefault(who, {})[mode] = list(e['dps'][mode]['runs'])
    for path in paths:
        runs, stats, skills = parse(path)
        # a hero measured on an older build had a different kit: its old runs are dropped, the new ones replace them
        for who in {w for run in runs for w in run}:
            fresh.add(who)
            e = lib['survivors'].get(who, {})
            if who.startswith('Deadlock') and e.get('measuredOn') != VERSION: raw.pop(who, None)
        for run in runs:
            for who, modes in run.items():
                # a run where the survivor dealt nothing at all is a bench fault (Captain's shotgun before the bench released
                # it), not a measurement: skip it
                if modes.get('all', {}).get('dps', 1) == 0: continue
                for mode, r in modes.items():
                    raw.setdefault(who, {}).setdefault(mode, []).append(r['dps'])
        for who, st in stats.items():
            e = lib['survivors'].setdefault(who, {})
            e['name'] = re.sub(r'[「」『』?]', '', st['name']).strip(); e['stats'] = {k: v for k, v in st.items() if k != 'name'}
        for who, sk in skills.items():
            lib['survivors'].setdefault(who, {})['skills'] = sk
    for who, modes in raw.items():
        e = lib['survivors'].setdefault(who, {})
        e.setdefault('name', who)
        e['hero'] = who.startswith('Deadlock')
        if e['hero'] and who in fresh: e['measuredOn'] = VERSION  # the others keep the build they were measured on
        e['dps'] = {m: summary(v) for m, v in modes.items() if v}
        if who in NOTES: e['note'] = NOTES[who]
        else: e.pop('note', None)
    lib['about'] = ('Stored DPS library (Deadlock of Rain). Main moves and each move alone, % of damage stat per second, '
                    'measured with tools/run_bench.sh (doc 05 §2: Monsoon, level 1, no items, 12 still enemies 10-15 m away, '
                    '30 s per pass, perfect aim, every move used when ready, no equipment except the "equipment" pass). '
                    '"all" = main moves together (the balance number). Vanilla survivors never need re-measuring; heroes are '
                    'tagged with the build they were measured on. Built by tools/dps_library.py.')
    lib['updated'] = datetime.date.today().isoformat()
    lib['runsPerSurvivor'] = max((len(e['dps'].get('all', {}).get('runs', [])) for e in lib['survivors'].values() if 'dps' in e), default=0)
    return lib

if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    args = sys.argv[1:]
    if not args: sys.exit(__doc__)
    if args[0] == '--add':
        base = json.load(open(LIB, encoding='utf-8')) if os.path.exists(LIB) else None
        lib = build(args[1:], base)
    else:
        lib = build(args)
    with open(LIB, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(lib, f, indent=1, ensure_ascii=False); f.write('\n')
    rows = sorted(((e['dps']['all']['mean'], w, e) for w, e in lib['survivors'].items() if 'all' in e.get('dps', {})), reverse=True)
    for mean, who, e in rows:
        d = e['dps']['all']
        print(f"{e['name']:<22} all {mean:>5}  (runs {d['runs']}, {d['min']}-{d['max']})")
    print('wrote', os.path.relpath(LIB, ROOT))
