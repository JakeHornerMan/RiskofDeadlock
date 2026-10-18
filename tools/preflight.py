"""Preflight: lay every design sheet over the others and list what would fail.

Checks (each row x column crossing is a checkbox):
  - every row has every declared column, and no cell is null / empty
  - every cross-sheet reference resolves (hero ids, buffs, effects, skills, kinds)
  - every skill kind has an implementing state in plugin/States (a class carrying [Kind("...")])
  - slot layout: each hero has exactly primary/secondary/utility/special (RoR2's 4 skills) plus one equipment (the Deadlock ult on Q)
  - balance: gun DPS computed from the row matches targetDps (+-10%) and sits in the RoR2 band
  - Deadlock assets named by the sheets exist in the player's install listing (dev files, if present):
    vmdl/vtex paths, sound events, animation clip names, mesh names
  - RoR2 Addressables keys named by effects exist in the game's catalog (dev file, if present)
Exit code 0 only when nothing is listed.
"""
import json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
D = os.path.join(ROOT, 'design')
DEV = os.path.join(ROOT, 'tools', 'dev')

problems = []
def bad(msg): problems.append(msg)

def load(name):
    with open(os.path.join(D, name + '.json'), encoding='utf-8') as f:
        return json.load(f)

sheets = {n: load(n) for n in ['heroes', 'stats', 'skills', 'equipment', 'buffs', 'animations', 'sounds', 'effects', 'hooks', 'voice']}

# 1. every cell filled
for name, s in sheets.items():
    cols = s['columns']
    for i, row in enumerate(s['rows']):
        rid = row.get(cols[0], f'#{i}')
        for c in cols:
            if c not in row:
                bad(f'{name}[{rid}].{c}: missing')
            elif row[c] is None or (isinstance(row[c], str) and row[c].strip() == '') or (isinstance(row[c], list) and not row[c]):
                bad(f'{name}[{rid}].{c}: empty')
        for c in row:
            if c not in cols:
                bad(f'{name}[{rid}].{c}: not a declared column')

heroes = {r['id']: r for r in sheets['heroes']['rows']}
skills = {r['id']: r for r in sheets['skills']['rows']}
buffs = {r['id']: r for r in sheets['buffs']['rows']}
effects = {r['id']: r for r in sheets['effects']['rows']}

def ref(sheet, rid, col, val, valid, allow_none=True):
    if allow_none and val == 'none':
        return
    if val not in valid:
        bad(f'{sheet}[{rid}].{col}: "{val}" does not resolve')

# 2. references
EXTRA_FLAGS = {'pierce': 'projectile', 'shockwave': 'heavy_melee'}
for r in sheets['stats']['rows']: ref('stats', r['hero'], 'hero', r['hero'], heroes, False)
for r in sheets['animations']['rows']: ref('animations', r['hero'], 'hero', r['hero'], heroes, False)
for r in sheets['sounds']['rows']: ref('sounds', r['skill'], 'skill', r['skill'], skills, False)
for r in sheets['skills']['rows']:
    ref('skills', r['id'], 'hero', r['hero'], heroes, False)
    ref('skills', r['id'], 'buff', r['buff'], buffs)
    ref('skills', r['id'], 'effect', r['effect'], effects, False)
    ref('skills', r['id'], 'vfx', r['vfx'], effects)
    # extra: an effects row the kind spawns, or a behaviour flag for one kind
    ref('skills', r['id'], 'extra', r['extra'], set(effects) | set(EXTRA_FLAGS))
    if r['extra'] in EXTRA_FLAGS and r['kind'] != EXTRA_FLAGS[r['extra']]:
        bad(f'skills[{r["id"]}].extra: flag "{r["extra"]}" only works on kind {EXTRA_FLAGS[r["extra"]]}')
    if r['vfx'] != 'none' and r['vfx'] in effects and effects[r['vfx']]['kind'] != 'custom':
        bad(f'skills[{r["id"]}].vfx: "{r["vfx"]}" must be an effects row of kind custom')
    if r['minDuration'] > r['duration']: bad(f'skills[{r["id"]}].minDuration: longer than duration')
    ref('skills', r['id'], 'dot', r['dot'], {'burn'})
    # every skill must measurably do something to an enemy or to the player (checked in test mode)
    if not r['expect']: bad(f'skills[{r["id"]}].expect: empty; every skill must affect an enemy or the player')
    for e in r['expect']:
        kind, _, arg = e.partition(':')
        if kind in ('enemy_debuff', 'self_buff'): ref('skills', r['id'], 'expect', arg, buffs, False)
        elif kind not in ('enemy_damage', 'enemy_stun', 'enemy_burn', 'self_heal', 'self_move', 'self_cloak') or arg:
            bad(f'skills[{r["id"]}].expect: "{e}" is not a known effect')
for b in buffs.values():
    ref('buffs', b['id'], 'triggerBuff', b['triggerBuff'], buffs)
    ref('buffs', b['id'], 'withBuff', b['withBuff'], buffs)
    if b['triggerStacks'] and not b['canStack']: bad(f'buffs[{b["id"]}].triggerStacks: needs a stacking buff')
    if b['triggerStacks'] > b['maxStacks']: bad(f'buffs[{b["id"]}].triggerStacks: above maxStacks, it can never fire')
EFFECT_KINDS = {'none', 'tracer', 'impact', 'ghost', 'orb', 'trail', 'attachment', 'ward', 'material', 'custom'}
for e in effects.values():
    if e['kind'] not in EFFECT_KINDS: bad(f'effects[{e["id"]}].kind: "{e["kind"]}" unknown')
    if (e['kind'] in ('custom', 'none')) != (e['addressable'] == 'none'): bad(f'effects[{e["id"]}].addressable: custom/none rows have none, every other kind a key')
for h in heroes.values():
    passive = [h['passiveName'], h['passiveDescription'], h['passiveIcon']]
    if 'none' in passive and passive != ['none'] * 3: bad(f'heroes[{h["id"]}].passive*: all three passive columns are set or all are none')
for h in heroes:
    for sheet, key in [('stats', 'hero'), ('animations', 'hero')]:
        if sum(1 for r in sheets[sheet]['rows'] if r[key] == h) != 1:
            bad(f'{sheet}: hero {h} needs exactly one row')
for sid in skills:
    if sum(1 for r in sheets['sounds']['rows'] if r['skill'] == sid) != 1:
        bad(f'sounds: skill {sid} needs exactly one row')

# 3. slots
SLOTS = ['primary', 'secondary', 'utility', 'special', 'equipment']
for h in heroes:
    got = sorted(r['slot'] for r in skills.values() if r['hero'] == h)
    if got != sorted(SLOTS):
        bad(f'skills: hero {h} slots {got} != {SLOTS}')
for r in skills.values():
    if r['slot'] not in SLOTS: bad(f'skills[{r["id"]}].slot: "{r["slot"]}" unknown')
    if r['slot'] in ('secondary', 'utility', 'special') and r['cooldown'] <= 0:
        bad(f'skills[{r["id"]}]: ability needs a cooldown')
    if r['slot'] == 'equipment' and r['cooldown'] != 120:
        bad(f'skills[{r["id"]}]: equipment ults recharge in 120 s (cooldown {r["cooldown"]})')
equipment = sheets['equipment']['rows']
for e in equipment:
    ref('equipment', e['id'], 'hero', e['hero'], heroes, False)
    ref('equipment', e['id'], 'skill', e['skill'], skills, False)
    sk = skills.get(e['skill'])
    if sk and (sk['slot'] != 'equipment' or sk['hero'] != e['hero']):
        bad(f'equipment[{e["id"]}].skill: "{e["skill"]}" must be the equipment-slot skill of {e["hero"]}')
for r in skills.values():
    if r['slot'] == 'equipment' and sum(1 for e in equipment if e['skill'] == r['id']) != 1:
        bad(f'equipment: skill {r["id"]} needs exactly one equipment row')

# 4. kinds implemented
states_dir = os.path.join(ROOT, 'plugin', 'States')
impl = set()
if os.path.isdir(states_dir):
    for fn in os.listdir(states_dir):
        if fn.endswith('.cs'):
            impl |= set(re.findall(r'\[Kind\("([a-z_]+)"\)\]', open(os.path.join(states_dir, fn), encoding='utf-8').read()))
for r in skills.values():
    if r['kind'] not in impl:
        bad(f'skills[{r["id"]}].kind: "{r["kind"]}" has no implementing state in plugin/States')

# 5. balance
def gun_dps(r):
    shots = r['clipSize']
    if r['kind'] == 'gun_burst':
        cycles = shots / r['burst']
        fire_time = cycles * (r['interval'] + (r['burst'] - 1) * r['duration'])
    else:
        fire_time = shots * r['interval']
    dmg = shots * r['pellets'] * r['damagePct']
    return dmg / (fire_time + r['reloadTime'])
for r in skills.values():
    if r['slot'] == 'primary':
        dps = gun_dps(r)
        if abs(dps - r['targetDps']) > 0.1 * r['targetDps']:
            bad(f'skills[{r["id"]}].targetDps: row computes {dps:.0f} %/s, sheet says {r["targetDps"]}')
        if not (240 <= dps <= 600):
            bad(f'skills[{r["id"]}]: sustained DPS {dps:.0f} %/s outside RoR2 primary band 240-600')
    if r['slot'] == 'equipment':
        # at least the Preon Accumulator (140 s) scaled to our 120 s cooldown, about 6800 % per enemy per use;
        # no upper limit: the ults are pickups any survivor can carry and are left out of balance
        if r['kind'] == 'ult_dance': total = r['duration'] / r['interval'] * r['damagePct']
        elif r['kind'] == 'ult_zone': total = r['duration'] / r['interval'] * r['damagePct']  # per target: every tick strikes up to chainCount enemies
        else: total = r['damagePct']
        if total < 6000:
            bad(f'skills[{r["id"]}]: equipment ult total {total:.0f}% below the Preon level of 6000')

# 5b. voice lines (design/voice.json)
VOICE_TRIGGERS = {'select', 'unselect', 'skill', 'multikill', 'idle'}
for r in sheets['voice']['rows']:
    ref('voice', r['id'], 'hero', r['hero'], heroes, False)
    ref('voice', r['id'], 'trigger', r['trigger'], VOICE_TRIGGERS, False)
    if r['trigger'] == 'skill':
        ref('voice', r['id'], 'skill', r['skill'], skills, False)
        if r['skill'] in skills and skills[r['skill']]['hero'] != r['hero']:
            bad(f'voice[{r["id"]}].skill: {r["skill"]} belongs to {skills[r["skill"]]["hero"]}, not {r["hero"]}')
    elif r['skill'] != 'none': bad(f'voice[{r["id"]}].skill: only skill lines name a skill')
    if (r['trigger'] == 'multikill') != (r['kills'] > 0): bad(f'voice[{r["id"]}].kills: set (> 0) for multikill lines only')
    if not (0 < r['chance'] <= 1): bad(f'voice[{r["id"]}].chance: must be in (0, 1]')
    if r['gap'] < 0: bad(f'voice[{r["id"]}].gap: negative')
for h in heroes:
    for t in ('select', 'unselect', 'idle'):
        if not any(r['hero'] == h and r['trigger'] == t for r in sheets['voice']['rows']): bad(f'voice: {h} has no {t} line')

# 6. Deadlock assets (dev listings from this PC's install; not shipped)
def dev(name):
    p = os.path.join(DEV, name)
    return p if os.path.exists(p) else None
vpk = dev('vpk_files.txt')
if vpk:
    files = set(l.strip() for l in open(vpk, encoding='utf-8'))
    def has(path, col):
        if path + '_c' not in files: bad(f'{col}: "{path}" not in Deadlock install')
    for h in heroes.values():
        for c in ['vmdl', 'imageSmall', 'imageCard', 'soundevents']: has(h[c], f'heroes[{h["id"]}].{c}')
        if h['passiveIcon'] != 'none': has(h['passiveIcon'], f'heroes[{h["id"]}].passiveIcon')
    for r in skills.values(): has(r['icon'].replace('.vtex', '.vtex'), f'skills[{r["id"]}].icon')
    for r in sheets['voice']['rows']:
        if f"sounds/vo/{r['group']}_01.vsnd_c" not in files: bad(f'voice[{r["id"]}].group: no sounds/vo/{r["group"]}_01.vsnd in Deadlock install')
    for b in buffs.values():
        if b['icon'] != 'none': has(b['icon'], f'buffs[{b["id"]}].icon')
else:
    bad('dev listing tools/dev/vpk_files.txt missing: Deadlock asset paths unverified')
ev = dev('sound_events.txt')
if ev:
    events = set(l.strip() for l in open(ev, encoding='utf-8'))
    for r in sheets['sounds']['rows']:
        for c in ['cast', 'loop', 'hit', 'end', 'reload', 'castLayer', 'hitLayer', 'tick', 'tickFinal']:
            if r[c] != 'none' and r[c] not in events: bad(f'sounds[{r["skill"]}].{c}: event "{r[c]}" not in Deadlock soundevents')
else:
    bad('dev listing tools/dev/sound_events.txt missing: sound events unverified')
ANIM_RIG_COLUMNS = {'hero', 'upperBodyBone', 'gunArms', 'gunBone', 'gunHand', 'muzzle', 'muzzleLeft', 'clothParents', 'ragdollBones', 'ragdollThickness'}  # animations columns that are not clip names
for r in sheets['animations']['rows']:
    if r['gunArms'] not in ('R', 'LR'): bad(f'animations[{r["hero"]}].gunArms: "{r["gunArms"]}" must be R or LR')
    if r['aimPose'] == 'none': bad(f'animations[{r["hero"]}].aimPose: the gun rig needs a holding pose clip')
an = dev('anim_names.json')
if an:
    a = json.load(open(an, encoding='utf-8'))
    for h in heroes:
        if h not in a: bad(f'dev listing tools/dev/anim_names.json has no "{h}": re-export every hero glb and regenerate it (docs/01 §8)')
    for r in sheets['animations']['rows']:
        if r['hero'] not in a: continue
        names = set(a[r['hero']]['anims'])
        for c in sheets['animations']['columns']:
            if c in ANIM_RIG_COLUMNS: continue
            if r[c] != 'none' and r[c] not in names: bad(f'animations[{r["hero"]}].{c}: clip "{r[c]}" not in hero model')
    for h in heroes.values():
        if h['id'] not in a: continue
        for m in h['meshInclude']:
            if m not in a[h['id']]['meshes']: bad(f'heroes[{h["id"]}].meshInclude: mesh "{m}" not in model')
else:
    bad('dev listing tools/dev/anim_names.json missing: clips unverified')
ak = dev('addressable_keys.txt')
if ak:
    keys = set(l.strip() for l in open(ak, encoding='utf-8'))
    for e in effects.values():
        if e['addressable'] != 'none' and e['addressable'] not in keys: bad(f'effects[{e["id"]}].addressable: "{e["addressable"]}" not in RoR2 catalog')
else:
    bad('dev listing tools/dev/addressable_keys.txt missing: effect keys unverified')

# 7. scale sanity
anchor = heroes[sheets['heroes']['anchorHero']]
for h in heroes.values():
    want = round(h['sourceHeight'] / anchor['sourceHeight'], 3)
    if abs(want - h['heightScale']) > 0.002: bad(f'heroes[{h["id"]}].heightScale: {h["heightScale"]} but sourceHeight ratio is {want}')

cells = sum(len(s['rows']) * len(s['columns']) for s in sheets.values())
if problems:
    print(f'PREFLIGHT: {len(problems)} problem(s) across {cells} cells')
    for p in problems: print('  -', p)
    sys.exit(1)
print(f'PREFLIGHT CLEAN: {cells} cells checked, all references resolve')
