"""Builds the hero kit pages and the balance overview (claude.ai Artifacts, links in docs/06) into build/pages/.

Inputs (nothing here is typed in twice):
  design/*.json               every ability number, description, stat and colour (the source of truth)
  build/bench_results.txt     the last `bash tools/run_bench.sh` (DPS per pass, and a `stats` line per survivor)
  build/pages/img/*.webp      studio renders cut out by tools/portraits.py (our own in-game renders; no Deadlock art)
Hand-written here: the RoR2 matches' vanilla skill names, the per-version change notes (CHANGES) and extra analysis
lines per hero (ANALYSIS); update them after a bench run, as doc 05 §3/§5 change.

Run: python tools/kit_pages.py, then republish each page to its existing link (doc 06 top)."""
import base64, html, json, os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'build', 'pages')
IMG = os.path.join(OUT, 'img')
os.makedirs(OUT, exist_ok=True)
E = html.escape
VERSION = 'v0.5.1'
BAND = 500  # balanced = within ±500 %/s of the RoR2 match (creator, 2026-10-16)

def sheet(name):
    return json.load(open(os.path.join(ROOT, 'design', name + '.json'), encoding='utf-8'))['rows']

HEROES = sheet('heroes'); STATS = {r['hero']: r for r in sheet('stats')}
SKILLS = sheet('skills'); BUFFS = {r['id']: r for r in sheet('buffs')}
EQUIP = {r['hero']: r for r in sheet('equipment')}
LINKS = {'haze': 'https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p', 'abrams': 'https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG',
         'seven': 'https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m', 'infernus': 'https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF',
         'mokrill': 'https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa', 'overview': 'https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3'}
SLOTS = ['primary', 'secondary', 'utility', 'special']
KEY = {'primary': 'M1', 'secondary': 'M2', 'utility': 'Shift', 'special': 'R', 'equipment': 'Q'}
KIND_LABEL = {'primary': 'Weapon', 'secondary': 'Ability', 'utility': 'Ability', 'special': 'Ability · new in 0.4'}

# ------------------------------------------------------------------ the RoR2 matches (vanilla, default loadout)
VANILLA = {
 'CommandoBody': dict(name='Commando', who='Commando',
   kit={'primary': ('Double Tap', 'Rapidly shoot an enemy for 100% damage.'),
        'secondary': ('Phase Round', 'A blast that pierces every enemy for 300%.'),
        'utility': ('Tactical Dive', 'Roll a short distance; no damage.'),
        'special': ('Suppressive Fire', 'Fire 6 shots for 100% each, stunning.')}),
 'LoaderBody': dict(name='Loader', who='Loader',
   kit={'primary': ('Knuckleboom', 'Swing at nearby enemies for 320%.'),
        'secondary': ('Grapple Fist', 'Fire a hook that pulls you in.'),
        'utility': ('Charged Gauntlet', 'Charge a punch for 320–2700%, launching forward.'),
        'special': ('M551 Pylon', 'Throw a pylon that zaps nearby enemies with lightning.')}),
 'MageBody': dict(name='Artificer', who='Mage',
   kit={'primary': ('Flame Bolt', 'Fire a bolt for 220% that ignites; 4 charges.'),
        'secondary': ('Charged Nano-Bomb', 'Charge a bomb for 400–1200%, stunning.'),
        'utility': ('Snapfreeze', 'Raise a wall of ice that freezes enemies.'),
        'special': ('Flamethrower', 'Burn everything in front of you for 2000% over 2 s.')}),
 'EngiBody': dict(name='Engineer', who='Engi',
   kit={'primary': ('Bouncing Grenades', 'Charge up to 8 grenades, 100% each.'),
        'secondary': ('Pressure Mines', 'Place mines that explode when enemies come near.'),
        'utility': ('Bubble Shield', 'A shield that blocks enemies and their shots; no damage.'),
        'special': ('TR12 Gauss Auto-Turret', 'Place a turret that inherits your items.')}),
 'ToolbotBody': dict(name='MUL-T', who='Toolbot',
   kit={'primary': ('Auto-Nailgun', 'Fire nails for 70% each, faster the longer you hold.'),
        'secondary': ('Blast Canister', 'Launch a canister for 220% that bursts into bomblets, stunning.'),
        'utility': ('Transport Mode', 'Charge forward, knocking enemies back for 250%.'),
        'special': ('Power Mode', 'Switch tools; no damage of its own.')}),
}

# ------------------------------------------------------------------ hand-written notes (update with doc 05)
CHANGES = {
 'haze': [('v0.6.1', ['Falls as a ragdoll on death, like the RoR2 survivors']),
            ('v0.6.0', ['Co-op: up to 4 players; every move works the same when a friend joins your game and plays Haze']),
            ('v0.5.1', ['Phantom Strike: every cut hits every enemy within 5 m of the target', 'Sleep Dagger always shows: thrown from the aim with a violet glow and trail']),
          ('v0.4.1', ['Twin SMGs 45 → 55 % per bullet', 'Fixation bursts every 10 stacks (was 20) for 300 % (was 200), splashing every enemy within 6 m',
                      'Sleep Dagger: 6 s (was 10), 500 % (was 350), pierces every enemy in its line', 'Bullet Dance fires at every enemy in range at once']),
          ('v0.4.0', ['Bullet Dance moves to Q as an equipment; new R: Phantom Strike'])],
 'abrams': [('v0.6.3', ['Shoulder Charge: he rockets forward with a yellow glow on his front and yellow trails streaming behind']), ('v0.6.1', ['Falls as a ragdoll on death, like the RoR2 survivors']),
            ('v0.6.0', ['Co-op: up to 4 players; every move works the same when a friend joins your game and plays Abrams']),
            ('v0.5.1', ['Haymaker locks onto the enemy you aim at (20 m, even in the air) and rushes straight at it; no target, no punch', 'Heavier hit: hit-stop, Loader’s fist impact, a strong shake and a deeper thud']),
            ('v0.4.2', ['Damage only, to match Loader: Shotgun 45 → 52 % per pellet, Siphon Life 250 → 450 %/s, Shoulder Charge 500 → 1600 %, Haymaker 900 → 2400 %']),
            ('v0.4.1', ['Haymaker: a shockwave cone (6 m, 30° each side) hits everything in front', 'Shoulder Charge 350 → 500 % per enemy',
                        'Seismic Impact radius 12 → 18 m']),
            ('v0.4.0', ['Seismic Impact moves to Q as an equipment and slams on Left Click; new R: Haymaker'])],
 'seven': [('v0.6.1', ['Falls as a ragdoll on death, like the RoR2 survivors']),
            ('v0.6.0', ['Co-op: up to 4 players; every move works the same when a friend joins your game and plays Seven']),
            ('v0.5.1', ['Lightning Ball: slower (14 → 10.5 m/s), shocks within 10 m (was 6), bounces off walls and floors, a clear strike to each enemy', 'Thunderhead clouds 15 → 35 m up']),
           ('v0.4.2', ['To match Artificer: Shock Rifle 48 → 60 % per bullet; Lightning Ball 16 → 8 s, Static Charge 16 → 6 s, Power Surge 30 → 10 s']),
           ('v0.4.1', ['Cooldowns only, no damage cuts: Lightning Ball 6 → 16 s, Static Charge 7 → 16 s, Power Surge 14 → 30 s',
                       'Shock Rifle fix: the gun froze after a magazine (29 rounds in bursts of 3)']),
           ('v0.4.0', ['Thunderhead moves to Q as an equipment; new R: Power Surge with RoR2 Tesla arcs'])],
 'mokrill': [('v0.7.1c', ['After the creator’s balance play: runs at 6.3 m/s (90 % of Abrams, was 5); 200 health (was 250)',
                         'Every kill he makes, with any move, gives +2 max health (+20 for a boss) for the stage, up to +200 (only Tag Team kills before)',
                         'Burrow: three times as fast underground (was twice); cooldown 14 s (was 8)',
                         'Sand Blast: 400 % (was 450), throws monsters back harder (22 m/s, was 8); runs up and down slopes and steps; a new sand look: the cone filled in sand on the ground, a tall wall of sand, and every monster it hits coated in sand while Sanded',
                         'Tag Team: 400 % per landing and 150 % splash (was 1,500 and 500), so a basic monster takes two landings; 8 s (was 10), cooldown 20 s (was 30); Mo’s punches still 600 % but knock monsters back half as far (10 m/s)',
                         'Dust Devil: 450 % four times a second (was 200) for 5 s (was 8); whoever holds it can jump while spinning']),
             ('v0.7.0b', ['After the creator’s play-check: Tag Team moves to R on a 30 s cooldown (same damage and timing); no target, no throw and no cooldown',
                         'Frenzy while Krill is away: double speed, punches 600 % (was 300) that throw monsters, a red aura around Mo',
                         'Dust Devil becomes the Q ult: 8 s, 200 % four times a second (6,400 %, the Preon level); any survivor holding it spins',
                         'Burrow: up to 8 s underground (was 4); the pop-out hits 3 × 500 % (was 3 × 100); Mo sinks into the ground and bursts back up',
                         'Sand Blast: a 0.4 s wind-up with the cone drawn on the ground, a heavier throw and wave, and it shoves monsters back',
                         'Gets up to speed twice as fast (acceleration 80, top speed still 5 m/s)']),
             ('v0.7.0', ['New hero, matched to MUL-T: Yellow Canary (a fixed 4-pellet triangle), Sand Blast (a cone of sand that stops monsters attacking), Burrow (dig under the ground, burst out in a spin), Dust Devil (a sand tornado that pulls enemies in), Tag Team (Q: Krill hops from enemy to enemy for 10 s, and Mo punches while he is away)', 'Heavyweight: monsters can’t knock him back'])],
 'infernus': [('v0.6.3', ['Flame Gun 50 → 75 % per bullet', 'Flame Dash: one tap runs the full 5 s (no holding), laying pockets of fire (twice the old size) that burn for 5 s and build Afterburn on enemies inside; overlapping pockets stack']), ('v0.6.2', ['Concussive Combustion: a timer ticks once a second through the fuse, faster on the last second, on anyone holding the item']), ('v0.6.1', ['Napalm: Right Click shows where the flask will land, Left Click throws it; same arc, three times as far (45 → 78 m/s)', 'Flame Dash: hold for up to 5 s (was 3); cooldown 13 s (was 8)', 'Concussive Combustion: a yellow ring on the ground shows the blast range during the fuse, on anyone holding the item', 'Falls as a ragdoll on death']),
            ('v0.6.0', ['Co-op: up to 4 players; every move works the same when a friend joins your game and plays Infernus']),
            ('v0.5.1', ['Flame Gun: louder, with a heavier shot layered under his own, and muzzle smoke and a flash', 'Napalm: a visible bottle flies from him to the target']),
              ('v0.4.1', ['Torment Pulse 120 → 50 % per pulse', 'Flame Gun 40 → 50 % per bullet']),
              ('v0.4.0', ['Concussive Combustion moves to Q as an equipment; new R: Torment Pulse; all his fire rebuilt from RoR2 fire materials'])],
}
# "all" (main moves together, %/s) from every bench run of the current build, per survivor; the balance verdict uses the
# mean, because one run is too noisy (Loader measured 1769–5241, Artificer 1526–4507). Add each new run's numbers here
# (doc 05 §3.0 lists them); reset a hero's list when its kit changes.
RUNS = {
 'Commando': [1298, 1304, 1255, 1064, 1144, 1244, 1261],
 'DeadlockHaze': [],  # v0.5.1 kit change: the 0.4.2 runs no longer count
 'Loader': [2747, 2135, 1769, 5241],
 'DeadlockAbrams': [],
 'Mage': [2790, 4507, 1526, 4028, 4294, 4482],
 'DeadlockSeven': [],
 'Engi': [1268, 1555, 1641],
 'DeadlockInfernus': [920, 1226, 1161],
}

ANALYSIS = {
 'mokrill': ['After the 0.7.1c balance pass he does about 2,425 %/s with every move (one run), against MUL-T’s 1,450: above the ±500 band, about Abrams’ level (2,460). The creator chose to keep it.',
             'Tag Team (1,407 %/s alone, with Mo’s punches) and Burrow’s pop-out spin (1,150 %/s alone, now every 14 s) are the biggest parts; Sand Blast is 333 %/s and is there for control.',
             'His gun is slower than MUL-T’s nailgun (443 against 876 alone). No healing in the kit: he starts at 200 health and grows it with every kill.'],
 'haze': ['0.5.1 made Phantom Strike cut every enemy within 5 m of the target. Every cut also gives each of them 5 Fixation, so the whole pack reaches the 10-stack burst together and the bursts splash one another: Phantom Strike alone went from 375 to 4,139 %/s, and she is far above Commando. Not retuned yet: the fix is the creator’s call (Fixation on the target only, or fewer stacks to the others).'],
 'abrams': ['0.5.1: Haymaker locks onto the enemy you aim at and rushes to it, even in the air. It does about what the old lunge did (1,760 %/s alone), and he stays just inside Loader’s band.',
            'Loader is the least steady survivor in the test (2,059 to 3,837 across the library’s runs), so the verdict uses the averages.'],
 'seven': ['0.5.1: the Lightning Ball now bounces off walls and floors. In the still-pack test it bounces off the floor and flies on past the pack instead of settling in it, so it fell from 1,652 to 390 %/s and he dropped below Artificer. Not retuned yet: bouncing only off walls, or slowing on each bounce, would keep it in the fight.',
           'Keep Lightning Ball at 8 s or more: below that the balls overlap and Capacitor discharges chain through the pack.'],
 'infernus': ['Unchanged in 0.4.2: inside the ±500 band. Engineer’s grenades carry him; his turrets land few hits in 30 s against a still pack.',
              'Engineer’s damage stat is 14 against Infernus’ 12, so in raw damage per second Engineer is about 17 % further ahead; %/s removes that difference.'],
}

# ------------------------------------------------------------------ bench results
BENCH = {}; BSTATS = {}
bench_path = os.path.join(ROOT, 'build', 'bench_results.txt')
for line in (open(bench_path, encoding='utf-8') if os.path.exists(bench_path) else []):
    m = re.match(r'(\S+) (all|primary|secondary|utility|special|equipment): .*?([\d.]+)% of damage \((\d+)%/s\), hits (\d+), enemies hit (\d+)', line)
    if m:
        BENCH.setdefault(m.group(1), {})[m.group(2)] = dict(total=float(m.group(3)), dps=int(m.group(4)), hits=int(m.group(5)), enemies=int(m.group(6)))
        continue
    m = re.match(r'stats (\S+): name (.*?), health ([\d.]+) \+([\d.]+), regen ([\d.]+) \+([\d.]+), armor ([\d.]+), damage ([\d.]+) \+([\d.]+), speed ([\d.]+), jumps (\d+), crit ([\d.]+)', line)
    if m:
        g = m.groups()
        BSTATS[g[0]] = dict(name=g[1], health=float(g[2]), lhealth=float(g[3]), regen=float(g[4]), lregen=float(g[5]), armor=float(g[6]),
                           damage=float(g[7]), ldamage=float(g[8]), speed=float(g[9]), jumps=int(g[10]), crit=float(g[11]))

LIB_PATH = os.path.join(ROOT, 'docs', 'dps-library.json')
LIBRARY = json.load(open(LIB_PATH, encoding='utf-8')) if os.path.exists(LIB_PATH) else None
# survivors not in this bench file (the vanilla matches, by default) take their base stats from the library
for _who, _e in (LIBRARY or {}).get('survivors', {}).items():
    _s = _e.get('stats')
    if _who not in BSTATS and _s:
        BSTATS[_who] = dict(name=_e.get('name', _who), health=_s['health'], lhealth=_s['healthPerLevel'], regen=_s['regen'], lregen=_s['regenPerLevel'],
                            armor=_s['armor'], damage=_s['damage'], ldamage=_s['damagePerLevel'], speed=_s['moveSpeed'], jumps=_s['jumps'], crit=_s['crit'])

def dps(who, mode):
    """This bench file's number, else the stored library's mean (the vanilla survivors are never re-benched)."""
    d = BENCH.get(who, {}).get(mode, {}).get('dps')
    if d is None: d = (LIBRARY or {}).get('survivors', {}).get(who, {}).get('dps', {}).get(mode, {}).get('mean')
    return d

def all_runs(who):
    """Every "all" run of the current build: the hand-kept RUNS plus the stored library's runs."""
    lib = (LIBRARY or {}).get('survivors', {}).get(who, {}).get('dps', {}).get('all', {}).get('runs', [])
    return list(RUNS.get(who, [])) + list(lib)

def avg_all(who):
    """Main moves together, %/s: the mean over every run of this build, else this bench file's run."""
    r = all_runs(who)
    return round(sum(r) / len(r)) if r else dps(who, 'all')

def runs_of(who):
    return len(all_runs(who)) or 1

def per_use(who):
    return BENCH.get(who, {}).get('equipment')

def img(name):
    p = os.path.join(IMG, name + '.webp')
    return 'data:image/webp;base64,' + base64.b64encode(open(p, 'rb').read()).decode() if os.path.exists(p) else None

def img_tag(name, alt='', height=None):
    src = img(name)
    if not src: return ''
    style = f' style="height:{height}px;width:auto"' if height else ''
    return f'<img src="{src}" alt="{E(alt)}"{style}>'

def strip_tags(s):
    return re.sub(r'<[^>]+>', '', s)

def n(v):
    return f'{v:g}'

def fmt(v, suffix=''):
    return '–' if v is None else f'{v:,}{suffix}'

def signed(v):
    return ('+' if v >= 0 else '−') + f'{abs(v):,}'

# ------------------------------------------------------------------ the maths beside each ability (sheet numbers)
def gun_dps(r):
    shots = r['clipSize']
    if r['kind'] == 'gun_burst':
        fire = shots / r['burst'] * (r['interval'] + (r['burst'] - 1) * r['duration'])
    else:
        fire = shots * r['interval']
    return shots * r['pellets'] * r['damagePct'] / (fire + r['reloadTime'])

def maths(r):
    """(lines of working, %/s per target by the sheet numbers) for one skill row."""
    k, d, cd = r['kind'], r['damagePct'], r['cooldown']
    if k.startswith('gun_'):
        shots = r['clipSize']; per = f"{n(r['pellets'])} × {n(d)} %" if r['pellets'] > 1 else f"{n(d)} %"
        if k == 'gun_burst':
            cyc = shots / r['burst'] * (r['interval'] + (r['burst'] - 1) * r['duration'])
            return [f"{n(shots)} rounds × {per} = {n(shots * d * r['pellets'])} %",
                    f"fired in {cyc:.2f} s ({n(r['burst'])}-round bursts) + {n(r['reloadTime'])} s reload"], gun_dps(r)
        fire = shots * r['interval']
        unit = 'shells' if r['pellets'] > 1 else 'rounds'
        return [f"{n(shots)} {unit} × {per} = {n(shots * d * r['pellets'])} %", f"fired in {fire:.2f} s + {n(r['reloadTime'])} s reload"], gun_dps(r)
    if k == 'projectile':
        extra = ' to every enemy in its line' if r['extra'] == 'pierce' else ''
        return [f"{n(d)} %{extra}, every {n(cd)} s"], d / cd
    if k == 'mired_urn':
        tot = d * r['duration']
        return [f"{n(d)} %/s × {n(r['duration'])} s = {n(tot)} % to each of {n(r['chainCount'])} targets", f"every {n(cd)} s"], tot / cd
    if k == 'dash_charge':
        return [f"{n(d)} % to each enemy run into", f"every {n(cd)} s"], d / cd
    if k == 'heavy_melee':
        cone = f" to everything in a {n(r['range'])} m cone" if r['extra'] == 'shockwave' else ''
        return [f"{n(d)} %{cone}", f"every {n(cd)} s"], d / cd
    if k == 'projectile_orb':
        tot = d / r['interval'] * r['duration']
        return [f"{n(d)} % every {n(r['interval'])} s × {n(r['duration'])} s = {n(tot)} % to each enemy within {n(r['radius'])} m", f"every {n(cd)} s"], tot / cd
    if k == 'delayed_blast':
        return [f"{n(d)} % to everything within {n(r['radius'])} m", f"every {n(cd)} s"], d / cd
    if k == 'lob':
        return [f"{n(d)} % in {n(r['radius'])} m", f"{n(r['stocks'])} charges, {n(cd)} s each"], d / cd
    if k == 'dash_trail':
        per_s = d / r['interval']; tot = per_s * r['chainCount']
        return [f"trail {n(per_s)} %/s × {n(r['chainCount'])} s = {n(tot)} % to an enemy standing in it", f"every {n(cd)} s"], tot / cd
    if k == 'phantom_strike':
        return [f"{n(r['burst'])} × {n(d)} % = {n(r['burst'] * d)} % to one enemy", f"every {n(cd)} s"], r['burst'] * d / cd
    if k == 'aura_pulse':
        tot = d / r['interval'] * r['duration']
        return [f"{n(d)} % every {n(r['interval'])} s × {n(r['duration'])} s = {n(tot)} % to each enemy within {n(r['radius'])} m", f"every {n(cd)} s"], tot / cd
    if k == 'power_surge':
        return [f"{n(d)} % per arc, up to {n(r['chainCount'])} more enemies, once per gun burst, for {n(r['duration'])} s", f"every {n(cd)} s"], None
    if k == 'cone_wave':
        return [f"{n(d)} % to every enemy in a {n(r['range'])} m cone ({n(r['radius'])} m wide at the start), Sanded", f"every {n(cd)} s"], d / cd
    if k == 'burrow':
        tot = r['burst'] * d
        return [f"{n(r['burst'])} × {n(d)} % = {n(tot)} % to everything within {n(r['radius'])} m as he pops out", f"every {n(cd)} s"], tot / cd
    if k == 'krill_hop':
        hops = r['duration'] / r['interval']; tot = hops * d
        return [f"{n(hops)} landings × {n(d)} % = {n(tot)} % over the enemies he lands on, plus {n(r['splashPct'])} % to everything within {n(r['radius'])} m of each", f"every {n(cd)} s"], tot / cd
    if k == 'ult_devil':
        ticks = r['duration'] / r['interval']; tot = ticks * d
        return [f"{n(ticks)} × {n(d)} % = {n(tot)} % to everything within {n(r['radius'])} m, pulling from {n(r['range'])} m", f"every {n(cd)} s"], tot / cd
    if k == 'self_buff':
        return ['no damage: invisibility and speed'], 0
    return [], None

def tiles(r):
    t = []
    gun = r['kind'].startswith('gun')
    if r['damagePct']: t.append(('Damage', f"{n(r['damagePct'])} %" + (f" × {n(r['pellets'])}" if r['pellets'] > 1 and gun else '')))
    if r['cooldown']: t.append(('Cooldown', f"{n(r['cooldown'])} s"))
    if r['clipSize']: t.append(('Magazine', n(r['clipSize'])))
    if gun and r['reloadTime']: t.append(('Reload', f"{n(r['reloadTime'])} s"))
    if r['duration'] and not gun: t.append(('Duration', f"{n(r['duration'])} s"))
    if r['radius'] and not gun and r['kind'] not in ('dash_charge', 'heavy_melee', 'projectile'): t.append(('Radius', f"{n(r['radius'])} m"))
    if r['stunDuration']: t.append(('Stun', f"{n(r['stunDuration'])} s"))
    if r['stocks'] > 1: t.append(('Charges', n(r['stocks'])))
    return t[:4]

def tiles_html(t):
    return ''.join(f'<div class="tile"><b>{E(b)}</b><span>{E(a)}</span></div>' for a, b in t)

# ------------------------------------------------------------------ styles: a Deadlock field manual
FONTS = ('<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>'
         '<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Federo&family=Barlow:wght@400;500;600;700&family=Barlow+Condensed:wght@500;600;700&family=JetBrains+Mono:wght@400;500&display=swap">')

def css(accent):
    return f'''
/* layout: a Deadlock field manual. Dossier header (portrait + stat bands), ability ledger with the maths in a side
   column, the ult as an exhibit, then updates and balance. One dark, brass-and-cream look on purpose (single theme). */
:root {{
  --ink: #0d1112; --panel: #151b1c; --panel2: #1b2324; --rule: #2c3637; --cream: #ece3cf; --cream2: #d6cfbd; --muted: #9aa49f;
  --brass: #c9a35b; --weapon: #e58c3a; --vitality: #79b84f; --spirit: #b98be6; --good: #8fd18a; --bad: #ef7b6b;
  --accent: {accent};
  --display: "Federo", "Copperplate", Georgia, serif;
  --body: "Barlow", "Segoe UI", system-ui, sans-serif;
  --cond: "Barlow Condensed", "Arial Narrow", sans-serif;
  --mono: "JetBrains Mono", ui-monospace, Consolas, monospace;
  color-scheme: dark;
}}
body {{ background: var(--ink); color: var(--cream); font: 16px/1.55 var(--body); }}
.wrap {{ max-width: 1120px; margin: 0 auto; padding-inline: 20px; padding-block: 24px 64px; display: grid; gap: 44px; }}
a {{ color: var(--brass); }}
a:focus-visible {{ outline: 2px solid var(--brass); outline-offset: 2px; }}
.eyebrow {{ font: 600 12px/1.25 var(--cond); letter-spacing: .2em; text-transform: uppercase; color: var(--brass); }}
h1, h2, h3 {{ font-family: var(--display); font-weight: 400; margin: 0; text-wrap: balance; }}
h2 {{ font-size: clamp(26px, 4vw, 34px); letter-spacing: .04em; display: flex; align-items: center; gap: 14px; }}
h2::after {{ content: ""; flex: 1; height: 1px; background: linear-gradient(90deg, var(--brass), transparent); opacity: .6; }}
h3 {{ font-size: 24px; letter-spacing: .03em; }}
p {{ margin: 0; max-width: 68ch; }}
.muted {{ color: var(--muted); }}
.num {{ font-family: var(--mono); font-variant-numeric: tabular-nums; font-size: 14px; }}
.section {{ display: grid; gap: 14px; }}
.nav {{ display: flex; flex-wrap: wrap; gap: 8px 20px; font: 600 13px/1 var(--cond); letter-spacing: .14em; text-transform: uppercase; }}
.nav a {{ color: var(--muted); text-decoration: none; padding-bottom: 5px; border-bottom: 1px solid transparent; }}
.nav a:hover, .nav a[aria-current] {{ color: var(--cream); border-color: var(--brass); }}

/* dossier */
.dossier {{ display: grid; grid-template-columns: minmax(0, 340px) minmax(0, 1fr); gap: 32px; }}
.portrait {{ margin: 0; position: relative; border: 1px solid var(--rule); min-height: 440px; overflow: hidden; display: grid; place-items: end center;
    background: radial-gradient(60% 50% at 50% 40%, color-mix(in srgb, var(--accent) 40%, transparent), transparent 72%), linear-gradient(180deg, var(--panel2), var(--ink)); }}
.portrait::before, .portrait::after {{ content: ""; position: absolute; inset: 10px; border: 1px solid color-mix(in srgb, var(--brass) 45%, transparent); pointer-events: none; }}
.portrait::after {{ inset: 16px; border-color: color-mix(in srgb, var(--brass) 18%, transparent); }}
.portrait img {{ position: relative; max-height: 410px; width: auto; margin-bottom: 26px; filter: drop-shadow(0 18px 22px rgba(0,0,0,.65)); }}
.portrait figcaption {{ position: absolute; left: 50%; bottom: 0; transform: translateX(-50%); background: var(--ink); border: 1px solid var(--brass); border-bottom: 0;
    padding: 5px 16px; font: 600 12px/1.4 var(--cond); letter-spacing: .2em; text-transform: uppercase; color: var(--brass); white-space: nowrap; }}
.id {{ display: grid; gap: 16px; align-content: start; min-width: 0; }}
.id h1 {{ font-size: clamp(54px, 9vw, 100px); line-height: .9; letter-spacing: .02em; }}
.sub {{ font: 600 20px/1.3 var(--cond); letter-spacing: .08em; color: var(--accent); text-transform: uppercase; }}
.chips {{ display: flex; flex-wrap: wrap; gap: 8px; }}
.chip {{ font: 600 12px/1 var(--cond); letter-spacing: .12em; text-transform: uppercase; padding: 8px 10px; border: 1px solid var(--rule); background: var(--panel); }}
.chip b {{ color: var(--brass); font-weight: 700; margin-left: 4px; }}
.bands {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }}
.band {{ background: var(--panel); border-top: 3px solid var(--c); min-width: 0; }}
.band h4 {{ margin: 0; padding: 8px 12px; font: 700 13px/1 var(--cond); letter-spacing: .2em; text-transform: uppercase; color: var(--c);
    background: color-mix(in srgb, var(--c) 10%, transparent); display: flex; justify-content: space-between; gap: 8px; }}
.band h4 span {{ color: var(--muted); font-weight: 600; letter-spacing: .1em; }}
.stat {{ display: grid; grid-template-columns: 10px minmax(0, 1fr) auto minmax(56px, auto); gap: 10px; align-items: baseline; padding: 7px 12px; border-top: 1px solid var(--rule); }}
.stat i {{ width: 7px; height: 7px; background: var(--c); transform: rotate(45deg); align-self: center; }}
.stat .l {{ color: var(--muted); font-size: 14px; }}
.stat .v {{ font: 600 15px/1 var(--mono); font-variant-numeric: tabular-nums; white-space: nowrap; }}
.stat .v small {{ color: var(--muted); font-weight: 400; font-size: 11px; }}
.stat .m {{ font: 400 13px/1 var(--mono); color: var(--muted); text-align: right; white-space: nowrap; }}
.lore {{ color: var(--muted); font-style: italic; border-left: 2px solid var(--brass); padding-left: 12px; }}

/* ability ledger */
.ab {{ display: grid; grid-template-columns: minmax(0, 1.25fr) minmax(0, 1fr); background: var(--panel); border: 1px solid var(--rule); }}
.ab .main {{ padding: 18px; display: grid; gap: 12px; align-content: start; min-width: 0; }}
.head {{ display: flex; align-items: center; gap: 14px; }}
.key {{ flex: none; width: 52px; height: 58px; display: grid; place-items: center; font: 700 17px/1 var(--cond); letter-spacing: .06em;
    color: var(--ink); background: var(--accent); clip-path: polygon(50% 0, 100% 25%, 100% 75%, 50% 100%, 0 75%, 0 25%); }}
.key.passive {{ background: var(--brass); font-size: 11px; letter-spacing: .1em; }}
.desc {{ color: var(--cream2); }}
.tiles {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(104px, 1fr)); gap: 8px; }}
.tile {{ background: var(--panel2); border: 1px solid var(--rule); padding: 8px 10px; display: grid; gap: 3px; }}
.tile b {{ font: 600 18px/1.1 var(--mono); font-variant-numeric: tabular-nums; }}
.tile span {{ font: 600 11px/1.2 var(--cond); letter-spacing: .14em; text-transform: uppercase; color: var(--muted); }}
.math {{ border-left: 1px solid var(--rule); padding: 18px; display: grid; gap: 10px; align-content: start; min-width: 0;
    background: linear-gradient(180deg, color-mix(in srgb, var(--accent) 7%, var(--panel)), var(--panel)); }}
.work {{ font: 400 13px/1.6 var(--mono); color: var(--cream2); display: grid; gap: 2px; overflow-wrap: anywhere; }}
.work .eq {{ color: var(--cream); font-weight: 500; }}
.meter {{ display: grid; gap: 6px; }}
.mrow {{ display: grid; grid-template-columns: minmax(0, 120px) minmax(0, 1fr) 54px; gap: 8px; align-items: center; }}
.mrow .who {{ font: 600 12px/1.15 var(--cond); letter-spacing: .08em; text-transform: uppercase; color: var(--muted); }}
.track {{ height: 10px; background: var(--ink); border: 1px solid var(--rule); position: relative; }}
.fill {{ position: absolute; inset: 1px auto 1px 1px; background: var(--accent); }}
.fill.match {{ background: var(--muted); opacity: .75; }}
.mrow .v {{ font: 500 13px/1 var(--mono); text-align: right; font-variant-numeric: tabular-nums; }}
.note {{ font-size: 13px; color: var(--muted); }}

/* the ult exhibit */
.ult {{ display: grid; grid-template-columns: minmax(0, 260px) minmax(0, 1fr); border: 1px solid var(--brass); background: var(--panel); }}
.ult .fig {{ display: grid; place-items: center; padding: 18px; min-height: 250px; border-right: 1px solid var(--rule);
    background: radial-gradient(55% 50% at 50% 55%, color-mix(in srgb, var(--accent) 35%, transparent), transparent 72%), var(--ink); }}
.ult .fig img {{ max-height: 230px; width: auto; filter: drop-shadow(0 10px 14px rgba(0,0,0,.6)); }}
.ult .body {{ padding: 20px; display: grid; gap: 12px; align-content: start; min-width: 0; }}
.stamp {{ display: inline-block; font: 700 11px/1 var(--cond); letter-spacing: .2em; text-transform: uppercase; color: var(--brass); border: 1px solid var(--brass); padding: 5px 8px; margin-right: 8px; }}

/* updates and balance */
.cols {{ display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.2fr); gap: 18px; }}
.box {{ background: var(--panel); border: 1px solid var(--rule); padding: 18px; display: grid; gap: 12px; align-content: start; min-width: 0; }}
.box ul {{ margin: 6px 0 0; padding-left: 18px; display: grid; gap: 6px; }}
.ver {{ font: 600 13px/1 var(--mono); color: var(--brass); }}
.verdict {{ display: inline-flex; align-items: center; gap: 8px; font: 700 13px/1 var(--cond); letter-spacing: .14em; text-transform: uppercase; padding: 6px 10px; border: 1px solid currentColor; white-space: nowrap; }}
.verdict.inside {{ color: var(--good); }} .verdict.above, .verdict.below {{ color: var(--bad); }} .verdict.unknown {{ color: var(--muted); }}
.scale {{ position: relative; height: 62px; margin: 4px 8px 0; }}
.scale .zone {{ position: absolute; top: 12px; height: 20px; background: color-mix(in srgb, var(--good) 16%, transparent); border-inline: 1px dashed var(--good); }}
.scale .axis {{ position: absolute; left: 0; right: 0; top: 22px; height: 1px; background: var(--rule); }}
.scale .pin {{ position: absolute; top: 6px; width: 3px; height: 32px; transform: translateX(-1px); }}
.scale .pin span {{ position: absolute; top: 36px; left: 50%; transform: translateX(-50%); font: 500 12px/1 var(--mono); white-space: nowrap; }}
.foot {{ font: 12px/1.6 var(--mono); color: var(--muted); border-top: 1px solid var(--rule); padding-top: 14px; max-width: none; }}
.tablewrap {{ overflow-x: auto; }}
table {{ border-collapse: collapse; width: 100%; min-width: 600px; font-size: 15px; }}
th, td {{ padding: 10px; border-bottom: 1px solid var(--rule); text-align: left; vertical-align: middle; }}
th {{ font: 600 12px/1.3 var(--cond); letter-spacing: .14em; text-transform: uppercase; color: var(--muted); }}
td.n, th.n {{ font-family: var(--mono); font-variant-numeric: tabular-nums; text-align: right; }}
.who-cell {{ display: flex; align-items: center; gap: 10px; }}
.badge {{ display: inline-grid; place-items: center; width: 44px; height: 50px; font: 700 15px/1 var(--cond); letter-spacing: .08em; color: var(--ink); background: var(--muted); clip-path: polygon(50% 0, 100% 25%, 100% 75%, 50% 100%, 0 75%, 0 25%); }}
.lchart {{ display: grid; gap: 6px; background: var(--panel); border: 1px solid var(--rule); padding: 16px; }}
.lrow {{ display: grid; grid-template-columns: minmax(0, 240px) minmax(0, 1fr) 60px; gap: 10px; align-items: center; }}
.lname {{ font: 600 14px/1.2 var(--cond); letter-spacing: .06em; text-transform: uppercase; }}
.libtag {{ font: 600 10px/1 var(--cond); letter-spacing: .12em; color: var(--ink); background: var(--brass); padding: 2px 5px; margin-left: 6px; vertical-align: 2px; white-space: nowrap; }}
.libtag.match {{ background: var(--muted); }}
.ltrack {{ position: relative; height: 16px; background: var(--ink); border: 1px solid var(--rule); }}
.lspread {{ position: absolute; top: 0; bottom: 0; background: color-mix(in srgb, var(--cream) 14%, transparent); }}
.lbar {{ position: absolute; left: 0; top: 4px; bottom: 4px; }}
.lval {{ font: 500 13px/1 var(--mono); text-align: right; font-variant-numeric: tabular-nums; }}
.lgrid {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(min(100%, 300px), 1fr)); gap: 14px; }}
.lcard {{ background: var(--panel); border: 1px solid var(--rule); border-top: 3px solid var(--brass); padding: 14px; display: grid; gap: 10px; align-content: start; min-width: 0; }}
.lhead {{ display: flex; align-items: center; gap: 12px; }}
.lhead h3 {{ font-size: 22px; }}
.lall {{ margin-left: auto; text-align: right; display: grid; }}
.lall b {{ font: 600 22px/1 var(--mono); font-variant-numeric: tabular-nums; }}
.lall span {{ font: 500 11px/1.3 var(--mono); color: var(--muted); }}
.moves {{ display: grid; gap: 5px; }}
.mv {{ display: grid; grid-template-columns: 40px minmax(0, 1fr) 70px 48px; gap: 8px; align-items: center; font-size: 13px; }}
.mk {{ font: 700 11px/1 var(--cond); letter-spacing: .1em; color: var(--brass); }}
.mn {{ color: var(--cream2); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }}
.mb {{ height: 8px; background: var(--ink); border: 1px solid var(--rule); position: relative; }}
.mb i {{ position: absolute; inset: 0 auto 0 0; }}
.mvv {{ font: 500 12px/1 var(--mono); text-align: right; font-variant-numeric: tabular-nums; }}
@media (max-width: 760px) {{ .lrow {{ grid-template-columns: minmax(0, 120px) minmax(0, 1fr) 50px; }} }}
.grid2 {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
@media (max-width: 760px) {{
  .dossier, .ab, .ult, .cols, .grid2 {{ grid-template-columns: minmax(0, 1fr); }}
  .math {{ border-left: 0; border-top: 1px solid var(--rule); }}
  .ult .fig {{ border-right: 0; border-bottom: 1px solid var(--rule); }}
  .portrait {{ min-height: 360px; }} .portrait img {{ max-height: 330px; }}
  .bands {{ grid-template-columns: minmax(0, 1fr); }}
}}
'''

# ------------------------------------------------------------------ pieces
def match_of(hid):
    body = STATS[hid]['analog']
    return body, VANILLA.get(body, dict(name=body, who=body.replace('Body', ''), kit={}))

def hero_who(hid):
    # the body name: Deadlock + the display name in letters only ("Mo & Krill" -> DeadlockMoKrill)
    return 'Deadlock' + re.sub(r'[^A-Za-z0-9]', '', next(h['displayName'] for h in HEROES if h['id'] == hid))

def verdict(val, base):
    if val is None or not base: return 'unknown', 0
    lo, hi = base - BAND, base + BAND
    return ('inside' if lo <= val <= hi else 'above' if val > hi else 'below'), val - base

def stat_bands(hid):
    s = STATS[hid]; body, v = match_of(hid); m = BSTATS.get(v['who'], {})
    def mv(key, unit=''):
        return f'{m[key]:g}{unit}' if key in m else '–'
    weapon = [('Damage', f"{n(s['baseDamage'])} <small>+{n(s['levelDamage'])}/lvl</small>", mv('damage')),
              ('Crit chance', f"{n(s['baseCrit'])} %", mv('crit', ' %')),
              ('Attack speed', f"{n(s['baseAttackSpeed'])}×", '1×')]
    vital = [('Health', f"{n(s['baseMaxHealth'])} <small>+{n(s['levelMaxHealth'])}/lvl</small>", mv('health')),
             ('Regen', f"{n(s['baseRegen'])}/s <small>+{n(s['levelRegen'])}</small>", mv('regen', '/s')),
             ('Armor', n(s['baseArmor']), mv('armor')),
             ('Move speed', f"{n(s['baseMoveSpeed'])} m/s", mv('speed', ' m/s')),
             ('Jumps', n(s['baseJumpCount']), mv('jumps'))]
    def band(title, color, rows):
        r = ''.join(f'<div class="stat"><i></i><span class="l">{E(l)}</span><span class="v">{val}</span><span class="m">{E(mm)}</span></div>' for l, val, mm in rows)
        return f'<section class="band" style="--c:var(--{color})"><h4>{title}<span>vs {E(v["name"])}</span></h4>{r}</section>'
    return band('Weapon', 'weapon', weapon) + band('Vitality', 'vitality', vital)

def meter(rows, scale):
    out = []
    for who, val, cls in rows:
        w = 0 if not val else max(1.5, min(100, val / scale * 100))
        out.append(f'<div class="mrow"><span class="who">{E(who)}</span><div class="track"><div class="fill{cls}" style="width:calc({w:.1f}% - 2px)"></div></div><span class="v">{fmt(val)}</span></div>')
    return '<div class="meter">' + ''.join(out) + '</div>'

def ability_card(r, hid, v, scale):
    slot = r['slot']
    work, per = maths(r)
    mname = v['kit'].get(slot, ('–', ''))[0]
    lines = ''.join(f'<span>{E(w)}</span>' for w in work)
    eq = f'<span class="eq">= {per:,.0f} %/s per target</span>' if per else ('<span class="eq">= 0 %/s</span>' if per == 0 else '')
    note = '<p class="note">Its arcs ride on the gun, so its damage shows in the all-together number, not alone.</p>' if r['kind'] == 'power_surge' else ''
    rows = [(r['name'], dps(hero_who(hid), slot), ''), (f"{v['name']}: {mname}", dps(v['who'], slot), ' match')]
    return f'''<article class="ab">
  <div class="main">
    <div class="head"><span class="key">{KEY[slot]}</span><div><h3>{E(r["name"])}</h3><div class="eyebrow">{E(KIND_LABEL[slot])}</div></div></div>
    <p class="desc">{E(strip_tags(r["description"]))}</p>
    <div class="tiles">{tiles_html(tiles(r))}</div>
  </div>
  <aside class="math" aria-label="{E(r['name'])} damage maths">
    <div class="eyebrow">The maths</div>
    <div class="work">{lines}{eq}</div>
    <div class="eyebrow">Measured alone on the 12-enemy pack, %/s</div>
    {meter(rows, scale)}
    {note}
  </aside>
</article>'''

def passive_card(h):
    b = None
    for r in SKILLS:
        if r['hero'] == h['id'] and r['slot'] == 'primary' and r['buff'] != 'none': b = BUFFS.get(r['buff'])
    t = []
    if b:
        if b['maxStacks']: t.append(('Max stacks', n(b['maxStacks'])))
        if b['duration']: t.append(('Lasts', f"{n(b['duration'])} s"))
        if b['triggerStacks']: t.append(('Bursts at', f"{n(b['triggerStacks'])} stacks"))
        if b['triggerDamagePct']: t.append(('Burst', f"{n(b['triggerDamagePct'])} %"))
    # trophies (Mo & Krill): max health per kill, for the stage
    math = '<p class="note">A passive has no number of its own: it is counted inside the moves that trigger it.</p>'
    for r in SKILLS:
        tb = BUFFS.get(r['debuff']) if r['hero'] == h['id'] and r['kind'] == 'krill_hop' else None
        if not tb or not tb['maxHealthAdd']: continue
        per = tb['maxHealthAdd'] * max(1, r['buffStacks']); boss = per * max(1, r['bossMult']); cap = tb['maxHealthAdd'] * tb['maxStacks']
        t = [('Per kill', f"+{n(per)} max hp"), ('Boss kill', f"+{n(boss)} max hp"), ('Up to', f"+{n(cap)}"), ('Lasts', 'the stage')] + t
        math = (f'<div class="work"><span>every kill, any move: +{n(per)} max health</span><span>a boss: +{n(per)} × {n(r["bossMult"])} = +{n(boss)}</span>'
                f'<span>up to {n(tb["maxStacks"])} stacks = +{n(cap)}</span><span class="eq">{n(STATS[h["id"]]["baseMaxHealth"])} base → up to {n(STATS[h["id"]]["baseMaxHealth"] + cap)} at level 1</span></div>'
                '<p class="note">It raises <b>max health only</b>: a kill doesn’t heal you, the extra room fills by regen and healing like the rest of the bar. '
                'Every new stage starts back at base max health: the trophies stay on the body, and each stage gives you a new one.</p>')
    tl = tiles_html(t[:4])
    return f'''<article class="ab">
  <div class="main">
    <div class="head"><span class="key passive">PASSIVE</span><div><h3>{E(h["passiveName"])}</h3><div class="eyebrow">Innate</div></div></div>
    <p class="desc">{E(strip_tags(h["passiveDescription"]))}</p>
    {f'<div class="tiles">{tl}</div>' if tl else ''}
  </div>
  <aside class="math"><div class="eyebrow">The maths</div>{math}</aside>
</article>'''

def analysis(h):
    hid = h['id']; body, v = match_of(hid)
    me, base = avg_all(hero_who(hid)), avg_all(v['who'])
    if me is None or base is None:
        return ['No benchmark for this pair yet.']
    side, gap = verdict(me, base)
    out = [f"All main moves together: <strong>{me:,} %/s</strong> (mean of {runs_of(hero_who(hid))} runs) against {E(v['name'])}’s <strong>{base:,} %/s</strong> (mean of {runs_of(v['who'])}) "
           f"({signed(gap)}, {me / base:.2f}×). Balanced is {max(0, base - BAND):,}–{base + BAND:,}: <strong>{side} the band</strong>."]
    parts = []
    for slot in SLOTS:
        a, b = dps(hero_who(hid), slot), dps(v['who'], slot)
        if a is not None and b is not None: parts.append(f"{KEY[slot]} {a:,} vs {b:,}")
    if parts: out.append(f"Each move alone ({E(h['displayName'])} vs {E(v['name'])}, %/s): " + ', '.join(parts) + '.')
    return out + ANALYSIS.get(hid, [])

# ------------------------------------------------------------------ pages
def nav(current):
    items = [('overview', 'Balance overview')] + [(h['id'], h['displayName']) for h in HEROES]
    return '<nav class="nav" aria-label="Pages">' + ''.join(
        f'<a href="{LINKS[k]}"' + (' aria-current="page"' if k == current else '') + f'>{E(label)}</a>' for k, label in items) + '</nav>'

def balance_scale(h, v, me, base):
    if not me or not base: return ''
    lo, hi = base - BAND, base + BAND
    smax = max(me, hi) * 1.15
    def pin(val, color, label):
        return f'<div class="pin" style="left:{val / smax * 100:.1f}%;background:{color}"><span style="color:{color}">{E(label)} {val:,}</span></div>'
    return (f'<div class="scale" role="img" aria-label="{E(h["displayName"])} {me} against {E(v["name"])} {base} percent per second">'
            f'<div class="axis"></div><div class="zone" style="left:{lo / smax * 100:.1f}%;width:{(hi - lo) / smax * 100:.1f}%"></div>'
            f'{pin(base, "var(--muted)", v["name"])}{pin(me, "var(--accent)", h["displayName"])}</div>')

def hero_page(h):
    hid = h['id']; body, v = match_of(hid)
    rows = {r['slot']: r for r in SKILLS if r['hero'] == hid}
    vals = [dps(hero_who(hid), s) or 0 for s in SLOTS] + [dps(v['who'], s) or 0 for s in SLOTS]
    scale = max(vals + [100]) * 1.08
    me, base = avg_all(hero_who(hid)), avg_all(v['who'])
    side, gap = verdict(me, base)
    eq = rows['equipment']; use = per_use(hero_who(hid))
    use_html = f'<span class="num">One use on the pack: {use["total"]:,.0f} % ({use["enemies"]} enemies hit)</span>' if use else ''
    changes = ''.join(f'<div><span class="ver">{E(ver)}</span><ul>' + ''.join(f'<li>{E(c)}</li>' for c in cs) + '</ul></div>' for ver, cs in CHANGES.get(hid, []))
    paras = ''.join(f'<p>{t}</p>' for t in analysis(h))
    return f'''<title>{E(h["displayName"])} Kit Sheet</title>
{FONTS}
<style>{css(h["primaryColor"])}</style>
<main class="wrap">
  {nav(hid)}

  <header class="dossier">
    <figure class="portrait">
      {img_tag(hid, h["displayName"] + " rendered in Risk of Rain 2")}
      <figcaption>Survivor · {E(h["displayName"])}</figcaption>
    </figure>
    <div class="id">
      <div class="eyebrow">Deadlock of Rain · {VERSION}</div>
      <h1>{E(h["displayName"])}</h1>
      <div class="sub">{E(h["subtitle"])}</div>
      <div class="chips"><span class="chip">Risk of Rain match<b>{E(v["name"])}</b></span><span class="chip">Main moves<b>{fmt(me, " %/s")}</b></span><span class="chip">{E(v["name"])}<b>{fmt(base, " %/s")}</b></span></div>
      <div class="bands">{stat_bands(hid)}</div>
      <p class="lore">{E(h["lore"])}</p>
    </div>
  </header>

  <section class="section" aria-labelledby="ab">
    <h2 id="ab">Abilities</h2>
    {passive_card(h)}
    {"".join(ability_card(rows[s], hid, v, scale) for s in SLOTS)}
  </section>

  <section class="section" aria-labelledby="ul">
    <h2 id="ul">Ultimate · Q equipment</h2>
    <article class="ult">
      <div class="fig">{img_tag(hid + "_figure", "The " + eq["name"] + " pickup: a small figure of " + h["displayName"])}</div>
      <div class="body">
        <div class="head"><span class="key">Q</span><div><h3>{E(eq["name"])}</h3><div class="eyebrow">Starts in {E(h["displayName"])}’s hands · droppable · 120 s</div></div></div>
        <p class="desc">{E(strip_tags(eq["description"]))}</p>
        <div class="tiles">{tiles_html(tiles(eq))}</div>
        <p class="note">{E(EQUIP[hid]["pickupText"])} Anyone can find it wherever the Preon Accumulator drops; the pickup is the little {E(h["displayName"])} shown here.</p>
        <p><span class="stamp">Not balanced · just for fun</span>{use_html}</p>
      </div>
    </article>
  </section>

  <section class="section" aria-labelledby="ba">
    <h2 id="ba">Updates and balance</h2>
    <div class="cols">
      <div class="box"><div class="eyebrow">Changes</div>{changes}</div>
      <div class="box">
        <div class="eyebrow">Against {E(v["name"])}, all main moves together</div>
        <div><span class="verdict {side}">{side} the band · {signed(gap)} %/s</span></div>
        {balance_scale(h, v, me, base)}
        {paras}
      </div>
    </div>
  </section>

  <p class="foot">Benchmark {VERSION}: Monsoon, level 1, no items, 12 still enemies 10–15 m away, perfect aim, every move used the moment it is ready for 30 s, no equipment; the main-moves numbers are means over every run of this build (single runs swing ±10 %, Loader and Artificer far more); the per-move bars are the last run. {E(v["name"])} is measured the same way with its default loadout. %/s = % of the survivor’s damage stat per second (100 %/s is one 100 % hit per second). Balanced = within ±500 %/s of the Risk of Rain match. The maths column works from the design sheets; the bars are measured. Pictures are our own renders of the mod in Risk of Rain 2.</p>
</main>
'''

SLOT_NAMES = [('primary', 'M1'), ('secondary', 'M2'), ('utility', 'Shift'), ('special', 'R')]

def library_section():
    """Every survivor's stored main-move DPS (docs/dps-library.json): a ranked chart with each survivor's spread, then
    a card per survivor with its four moves."""
    if not LIBRARY: return ''
    hero_color = {hero_who(h['id']): h['primaryColor'] for h in HEROES}
    hero_id = {hero_who(h['id']): h['id'] for h in HEROES}
    match_for = {match_of(h['id'])[1]['who']: h['displayName'] for h in HEROES}
    rows = sorted(((e['dps']['all']['mean'], who, e) for who, e in LIBRARY['survivors'].items() if 'all' in e.get('dps', {})), key=lambda x: -x[0])
    if not rows: return ''
    top = max(e['dps']['all']['max'] for _, _, e in rows) * 1.05
    chart = []
    for mean, who, e in rows:
        d = e['dps']['all']; c = hero_color.get(who, 'var(--muted)')
        if who in hero_color: tag = ' <span class="libtag">Deadlock</span>'
        elif who in match_for: tag = f' <span class="libtag match">match for {E(match_for[who])}</span>'
        else: tag = ''
        label = f'{E(e["name"])}: mean {mean} percent per second, runs {d["min"]} to {d["max"]}'
        chart.append(f'<div class="lrow"><span class="lname">{E(e["name"])}{tag}</span>'
                     f'<div class="ltrack" role="img" aria-label="{label}">'
                     f'<div class="lspread" style="left:{d["min"] / top * 100:.1f}%;width:{max(0.5, (d["max"] - d["min"]) / top * 100):.1f}%"></div>'
                     f'<div class="lbar" style="width:{mean / top * 100:.1f}%;background:{c}"></div></div>'
                     f'<span class="lval">{mean:,}</span></div>')
    cards = []
    for mean, who, e in rows:
        c = hero_color.get(who, 'var(--brass)')
        sk = e.get('skills', {}); st = e.get('stats', {})
        smax = max([e['dps'].get(sl, {}).get('mean', 0) for sl, _ in SLOT_NAMES] + [1])
        moves = []
        for sl, key in SLOT_NAMES:
            v = e['dps'].get(sl, {}).get('mean')
            w = (v or 0) / smax * 100
            moves.append(f'<div class="mv"><span class="mk">{key}</span><span class="mn">{E(sk.get(sl, "–"))}</span>'
                         f'<span class="mb"><i style="width:{w:.0f}%;background:{c}"></i></span><span class="mvv">{fmt(v)}</span></div>')
        pic = img_tag(hero_id[who], '', 64) if who in hero_id else ''
        stats = f'{st["health"]:g} HP · {st["armor"]:g} armor · {st["damage"]:g} dmg · {st["moveSpeed"]:g} m/s' if st else ''
        d = e['dps']['all']
        cards.append(f'<article class="lcard" style="border-top-color:{c}">'
                     f'<div class="lhead">{pic}<div><h3>{E(e["name"])}</h3><div class="eyebrow">{E(stats)}</div></div>'
                     f'<div class="lall"><b>{mean:,}</b><span>%/s all · {len(d["runs"])} runs · {d["min"]:,}–{d["max"]:,}</span></div></div>'
                     f'<div class="moves">{"".join(moves)}</div></article>')
    n_runs = LIBRARY.get('runsPerSurvivor', 0)
    intro = (f'Every survivor measured with the same test ({n_runs} runs each, updated {E(LIBRARY.get("updated", ""))}): all main moves '
             'together, then each move alone, % of damage per second. The bar is the mean; the pale band behind it is the spread '
             'between runs. Stored in docs/dps-library.json, so a new hero only needs its own run and is compared with these numbers.')
    return ('<section class="section" aria-labelledby="lib"><h2 id="lib">Survivor DPS library</h2>'
            f'<p class="muted">{intro}</p><div class="lchart">{"".join(chart)}</div><div class="lgrid">{"".join(cards)}</div></section>')

def overview_page():
    def badge(name):
        return f'<span class="badge" aria-hidden="true">{E(name[:2].upper())}</span>'
    pairs, cards, fun = [], [], []
    for h in HEROES:
        hid = h['id']; body, v = match_of(hid)
        me, base = avg_all(hero_who(hid)), avg_all(v['who'])
        side, gap = verdict(me, base)
        pairs.append(f'<tr><td><div class="who-cell">{img_tag(hid, "", 60)}<a href="{LINKS[hid]}">{E(h["displayName"])}</a></div></td>'
                     f'<td><div class="who-cell">{img_tag(body, "", 60) or badge(v["name"])}{E(v["name"])}</div></td>'
                     f'<td class="n">{fmt(me)}</td><td class="n">{fmt(base)}</td>'
                     f'<td><span class="verdict {side}">{side} · {signed(gap)}</span></td></tr>')
        paras = ''.join(f'<p>{t}</p>' for t in analysis(h))
        cards.append(f'''<section class="box" style="--accent:{h["primaryColor"]};border-top:3px solid {h["primaryColor"]}" aria-labelledby="an-{hid}">
  <div class="head">{img_tag(hid, "", 90)}<div><h3 id="an-{hid}">{E(h["displayName"])} <span class="muted">vs {E(v["name"])}</span></h3><div class="eyebrow">{E(h["subtitle"])}</div></div></div>
  <div><span class="verdict {side}">{side} the band · {signed(gap)} %/s</span></div>
  {balance_scale(h, v, me, base)}
  {paras}
  <p><a href="{LINKS[hid]}">Open {E(h["displayName"])}’s kit sheet</a></p>
</section>''')
        u = per_use(hero_who(hid)); eq = next(r for r in SKILLS if r['hero'] == hid and r['slot'] == 'equipment')
        total = f'{u["total"]:,.0f} %' if u else '–'; hit = u['enemies'] if u else '–'
        fun.append((u['total'] if u else 0, f'<tr><td><div class="who-cell">{img_tag(hid + "_figure", "", 48)}{E(eq["name"])}</div></td><td>{E(h["displayName"])}</td><td class="n">{total}</td><td class="n">{hit}</td></tr>'))
    pre = per_use('Commando')
    if pre: fun.append((pre['total'], f'<tr><td>Preon Accumulator</td><td>Commando (vanilla item)</td><td class="n">{pre["total"]:,.0f} %</td><td class="n">{pre["enemies"]}</td></tr>'))
    fun_html = ''.join(r for _, r in sorted(fun, key=lambda x: -x[0]))
    inside = sum(1 for h in HEROES if verdict(avg_all(hero_who(h['id'])), avg_all(match_of(h['id'])[1]['who']))[0] == 'inside')
    return f'''<title>Deadlock of Rain Balance</title>
{FONTS}
<style>{css("#c9a35b")}</style>
<main class="wrap">
  {nav("overview")}
  <header class="id" style="gap:14px">
    <div class="eyebrow">Deadlock of Rain · {VERSION} · Balance</div>
    <h1>Each hero <span style="color:var(--brass)">vs</span> their Risk of Rain match</h1>
    <p class="muted">Every Deadlock hero is paired with the Risk of Rain 2 survivor their stats are built on, and both are measured the same way: all main moves (M1, M2, Shift, R) used the moment they are ready for 30 s against a pack of 12. Balanced means within ±500 %/s of the match. <strong style="color:var(--cream)">{inside} of 4</strong> are inside.</p>
  </header>

  <section class="section" aria-labelledby="pr">
    <h2 id="pr">The pairs</h2>
    <div class="tablewrap"><table>
      <thead><tr><th>Hero</th><th>Risk of Rain match</th><th class="n">Hero %/s (mean)</th><th class="n">Match %/s (mean)</th><th>Verdict (±500)</th></tr></thead>
      <tbody>{"".join(pairs)}</tbody>
    </table></div>
  </section>

  <section class="section" aria-labelledby="an">
    <h2 id="an">Analysis, hero by hero</h2>
    <div class="grid2">{"".join(cards)}</div>
  </section>

  <section class="section" aria-labelledby="fu">
    <h2 id="fu">Q ults, just for fun</h2>
    <p class="muted">The ults are pickups any survivor can carry, so they are left out of balance and may be as strong as we like. One use each against the same pack.</p>
    <div class="tablewrap"><table>
      <thead><tr><th>Equipment</th><th>Starts on</th><th class="n">One use</th><th class="n">Enemies hit</th></tr></thead>
      <tbody>{fun_html}</tbody>
    </table></div>
  </section>

  {library_section()}
  <p class="foot">Benchmark {VERSION}: Monsoon, first stage of a fresh run, level 1, no items, god mode; 12 still enemies (8 Lemurians, 4 Beetles, ×101 health) 10–15 m away; M1 held, every other move pressed the moment it is ready; perfect aim at the nearest enemy; put back on the spot after dashes; no equipment except in the Q pass; turret damage counts for Engineer. Counted: every hit landed, after crits and damage mods, before armor. Hero and match numbers are means over every run of this build; single runs swing ±10 %, Loader and Artificer far more. Commando, the yardstick before 0.4.1: {fmt(avg_all("Commando"))} %/s. Method: docs/05-DAMAGE-COMPARISON.md. Pictures are our own renders of the mod in Risk of Rain 2.</p>
</main>
'''

for h in HEROES:
    open(os.path.join(OUT, f'{h["id"]}-kit.html'), 'w', encoding='utf-8').write(hero_page(h))
open(os.path.join(OUT, 'balance-vs-commando.html'), 'w', encoding='utf-8').write(overview_page())
for f in sorted(os.listdir(OUT)):
    p = os.path.join(OUT, f)
    if os.path.isfile(p): print(f, os.path.getsize(p) // 1024, 'KB')
