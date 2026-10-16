"""Builds the hero kit pages and the balance page (claude.ai Artifacts, links in docs/06) into build/pages/.

The numbers are copied from docs 05 and 06: update HEROES/COMMANDO here whenever those docs change, rerun, and republish
each page to its existing link (Artifact tool, same URL)."""
import html, os
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'build', 'pages')
os.makedirs(OUT, exist_ok=True)
E = html.escape

COMMANDO = {'all': 1255, 'M1': ('Double Tap', 603), 'M2': ('Phase Round', 752), 'Shift': ('Tactical Dive', 0), 'R': ('Suppressive Fire', 80), 'Q': ('Preon Accumulator', 1947)}
BENCH = 'Benchmark 2026-10-16 (v0.4.1): Monsoon, level 1, no items, 12 still enemies at 10-15 m, perfect aim, 30 s per pass, no equipment in the balance passes.'
VERSION = 'v0.4.1'
BAND = 0.20  # balanced = Commando's main moves (no equipment) ±20 %
LO, HI = round(COMMANDO['all'] * (1 - BAND)), round(COMMANDO['all'] * (1 + BAND))
MAIN = ['M1', 'M2', 'Shift', 'R']

def verdict(v):
    gap = v - COMMANDO['all']
    side = 'inside' if LO <= v <= HI else 'above' if gap > 0 else 'below'
    return side, gap

HEROES = [
 dict(id='haze', name='Haze', tag='The smoke-shrouded assassin', analog='Commando / Bandit', accent='#e46aa8', accent2='#a68cff',
      all=1315,
      kit=[
        ('Passive', 'Fixation', None, 'Each SMG hit stacks Fixation on the target; each stack is +2.5 % damage from Haze. Crits add 3 stacks. Every 10 stacks the target bursts for 300 %, splashing every enemy within 6 m, and is slowed 12 % for 2 s.',
         [('Stacks', 'max 40, 6 s'), ('Per stack', '+2.5 % from Haze'), ('Burst', '300 % every 10, 6 m splash'), ('Slow', '12 % for 2 s')], 'in M1', False),
        ('M1', 'Twin SMGs', 1448, 'Two SMGs, alternating, firing from the end of each gun. Firing breaks Smoke Bomb.',
         [('Damage', '55 % per bullet'), ('Rate', '9.5 shots/s'), ('Magazine', '25, reload 1.4 s'), ('Proc', '0.5')], 'Fixation bursts included', False),
        ('M2', 'Sleep Dagger', 100, 'A thrown dagger that pierces every enemy in its line; each takes the damage and is put to sleep (stunned).',
         [('Damage', '500 % each'), ('Stun', '4 s'), ('Cooldown', '6 s'), ('Pierces', 'every enemy in line')], None, False),
        ('Shift', 'Smoke Bomb', 0, 'Invisible (vanilla Cloak) and faster. Enemies lose her as a target and can’t pick her again until she fires.',
         [('Duration', '5 s'), ('Speed', '+50 %'), ('Cooldown', '12 s')], None, False),
        ('R', 'Phantom Strike', 282, 'From Deadlock’s Phantom Strike item. Blinks behind the enemy under the crosshair and cuts it three times, each cut adding 5 Fixation. No target, no blink: R stays ready.',
         [('Damage', '3 × 250 %'), ('Range', '30 m'), ('Fixation', '+5 per cut'), ('Cooldown', '10 s')], None, False),
        ('Q', 'Bullet Dance', 1567, 'Ult equipment (held from the start). Spins and fires at every enemy within 25 m at once, 12 times a second, with a 30 % chance to evade attacks.',
         [('Damage', '113 % per shot'), ('Rate', '~12 shots/s for 5 s'), ('Per enemy', '≈ 6800 %'), ('Cooldown', '120 s')], '47 008 % (8 enemies in range)', False),
      ],
      fixes=['Twin SMGs 45 → 55 % per bullet',
             'Fixation burst every 10 stacks (was 20) for 300 % (was 200), splashing every enemy within 6 m',
             'Sleep Dagger 6 s (was 10), 500 % (was 350), pierces every enemy in its line',
             'Bullet Dance (Q) fires at every enemy in range at once: 6 440 → 47 008 % per use'],
      est='Inside the band. Her gun alone (1448) is now the strongest single ability in the test, mostly from Fixation bursts splashing the bunched pack; watch it against single targets.'),
 dict(id='abrams', name='Abrams', tag='The unstoppable detective', analog='Loader / Acrid', accent='#e0603a', accent2='#d9a25b',
      all=729,
      kit=[
        ('Passive', 'Infernal Resilience', None, 'Heals back part of every hit he takes; debuffs on him wear off sooner.',
         [('Heal-back', '10 % over 10 s'), ('Debuffs', '−20 % duration')], 'no damage', False),
        ('M1', 'Shotgun', 513, 'Nine pellets with spread, fired from the end of the gun.',
         [('Damage', '9 × 45 %'), ('Rate', '1.6 shots/s'), ('Shells', '9, reload 1.5 s'), ('Proc', '0.4')], None, False),
        ('M2', 'Siphon Life', 400, 'Risk of Rain 2’s Mired Urn tethers: drains the nearest enemies and heals him for all of it.',
         [('Targets', 'up to 3 within 25 m'), ('Drain', '250 %/s each'), ('Duration', '8 s'), ('Cooldown', '15 s')], None, False),
        ('Shift', 'Shoulder Charge', 200, 'Charges along his aim, running like his sprint but faster. Every enemy he runs into is hit, stunned and knocked aside.',
         [('Damage', '500 % per enemy'), ('Stun', '1 s'), ('Knockback', '22 m/s'), ('Cooldown', '8 s')], None, False),
        ('R', 'Haymaker', 630, 'Deadlock’s heavy melee. A short wind-up with a glowing fist, a 6 m lunge, then a punch whose shockwave hits every enemy in a cone in front and knocks them flying.',
         [('Damage', '900 % each'), ('Shockwave', '6 m cone, 30° each side'), ('Knockback', '30 m/s'), ('Cooldown', '5 s')], None, False),
        ('Q', 'Seismic Impact', 2493, 'Ult equipment (held from the start). Rises, hovers up to 3 s while a ground reticle follows the aim, then Left Click slams down there. He can be hit while hovering.',
         [('Damage', '6800 % in 18 m'), ('Stun', '1.6 s'), ('Aim range', '40 m'), ('Cooldown', '120 s')], '74 800 % (11 enemies)', False),
      ],
      fixes=['Seismic Impact (Q) radius 12 → 18 m: 7 → 11 enemies per slam',
             'Haymaker: a shockwave cone (6 m, 30° each side) hits everything in front for 900 %; 330 → 630 %/s alone',
             'Shoulder Charge 350 → 500 % per enemy'],
      est='Still below the band (−526). His abilities add up to far more alone (513 + 400 + 200 + 630), but together each Shoulder Charge carries him up to 3 s and 70+ m away from the pack, and the Haymaker wind-up stops the shotgun. Suggestions only: a shorter charge (3 → 1.5 s) or one that ends after the first enemy hit; Shotgun 45 → 52 % per pellet; Siphon Life 15 → 12 s.'),
 dict(id='seven', name='Seven', tag='The living storm', analog='Artificer', accent='#5cc8ff', accent2='#b9e8ff',
      all=1272,
      kit=[
        ('Passive', 'Capacitor', None, 'Gun hits, Lightning Ball shocks and Power Surge arcs build Static. At 10 stacks the target discharges, arcing to 2 nearby enemies, and everything hit is stunned.',
         [('Discharge', '300 % to each'), ('Arcs', '2 enemies, 12 m'), ('Stun', '1 s'), ('Static', 'max 10, 5 s')], 'in M1, M2 and R', False),
        ('M1', 'Shock Rifle', 552, '3-round bursts from the end of the gun; every hit builds Static.',
         [('Damage', '3 × 48 %'), ('Burst', 'every 0.26 s'), ('Magazine', '29, reload 1.4 s'), ('Proc', '0.7')], 'fixed: it froze after one magazine', False),
        ('M2', 'Lightning Ball', 114, 'A slow ball that flies through enemies and stops only on walls, shocking everything near it. Every shock builds Static.',
         [('Shock', '60 % every 0.25 s'), ('Radius', '6 m'), ('Lifetime', '5 s'), ('Cooldown', '16 s')], None, False),
        ('Shift', 'Static Charge', 315, 'Plants a charge on the aimed enemy or spot; after a countdown a thunderbolt strikes from the sky.',
         [('Damage', '450 % in 8 m'), ('Stun', '0.9 s'), ('Fuse', '1.5 s'), ('Cooldown', '16 s')], None, False),
        ('R', 'Power Surge', 0, 'His real Deadlock ability, built like RoR2’s Unstable Tesla Coil. For 8 s every Shock Rifle burst arcs Tesla lightning from the target to up to 4 more enemies, each arc building Static. +40 % move speed.',
         [('Arc', '60 % each'), ('Jumps', '4 enemies, 10 m'), ('Duration', '8 s'), ('Cooldown', '30 s')], 'it powers the gun, so it counts only in “all”', False),
        ('Q', 'Thunderhead', 907, 'Ult equipment (held from the start). Dark storm clouds over the spot he stands on darken the area and strike enemies with lightning; allies inside get Warbanner.',
         [('Strikes', '4 enemies / 0.4 s'), ('Damage', '272 % each'), ('Area', '20 m for 10 s'), ('Cooldown', '120 s')], '27 200 %', False),
      ],
      fixes=['Cooldowns only, no damage cuts (creator’s choice), tuned with the bench until inside the band:',
             'Lightning Ball 6 → 16 s',
             'Static Charge 7 → 16 s',
             'Power Surge 14 → 30 s',
             'Shock Rifle bug fixed: the gun froze after a magazine (29 rounds in bursts of 3); 62 → 552 %/s alone'],
      est='Inside the band (+17 vs Commando).'),
 dict(id='infernus', name='Infernus', tag='The burning man', analog='Acrid / Engineer', accent='#ff8a2a', accent2='#ffc66b',
      all=920,
      kit=[
        ('Passive', 'Afterburn', None, 'Gun and Torment Pulse hits build Afterburn; at 12 stacks the target burns. Embers show the build-up; RoR2’s Helfire flare and big flames show the burn.',
         [('Build-up', 'max 12, 4 s'), ('Burn', '100 %/s for 3 s')], 'in M1 and R', False),
        ('M1', 'Flame Gun', 317, 'Automatic, from the end of the gun; every hit builds Afterburn.',
         [('Damage', '50 % per bullet'), ('Rate', '9.5 shots/s'), ('Magazine', '27, reload 1.4 s'), ('Proc', '0.5')], None, False),
        ('M2', 'Napalm', 644, 'A lobbed flask that bursts in a ring of fire and leaves a burning puddle: damage, a slow, and the target takes more damage from him.',
         [('Damage', '300 % in 5 m'), ('Slow', '28 % for 4 s'), ('Amp', '+16 % for 8 s'), ('Charges', '2, 5 s each')], None, False),
        ('Shift', 'Flame Dash', 257, 'Dash at ×2.2 speed, hold to keep going. Leaves burning ground; enemies he runs into are knocked aside and scorched.',
         [('Trail', '150 %/s for 5 s'), ('Shove', '75 % + 22 m/s'), ('Length', '0.8 s tap, 3 s held'), ('Cooldown', '8 s')], None, False),
        ('R', 'Torment Pulse', 823, 'From Deadlock’s Torment Pulse item. For 6 s, rings of fire race out of him every 0.5 s, burning everything within 10 m and building Afterburn.',
         [('Damage', '50 % per pulse'), ('Radius', '10 m'), ('Afterburn', '+2 per pulse'), ('Cooldown', '12 s')], 'includes the burns it ignites', False),
        ('Q', 'Concussive Combustion', 2493, 'Ult equipment (held from the start). Becomes a bomb, fire gathering on him; the blast stuns, leaves burning ground, and each kill heals him.',
         [('Damage', '6800 % in 18 m'), ('Fuse', '3 s'), ('Stun', '2 s'), ('Cooldown', '120 s')], '74 800 % (11 enemies)', False),
      ],
      fixes=['Torment Pulse 120 → 50 % per pulse: 1667 → 823 %/s alone',
             'Flame Gun 40 → 50 % per bullet: 246 → 317 %/s alone'],
      est='Just below the band (−335; the band starts at 1004, 84 away), inside the test’s ±10 % noise. Suggestion only, if wanted: Napalm splash 5 → 6 m, or Torment Pulse 50 → 60 %.'),
]

FUN = [
  ('Seismic Impact', 'Abrams', '74 800 %', '6800 % to each of the 11 enemies inside 18 m'),
  ('Concussive Combustion', 'Infernus', '74 800 %', '6800 % to each of the 11 enemies inside 18 m'),
  ('Preon Accumulator', 'Commando (vanilla item)', '58 400 %', 'the yardstick the ults were scaled to'),
  ('Bullet Dance', 'Haze', '47 008 %', 'every shot hits every enemy within 25 m; 8 of the pack were in range'),
  ('Thunderhead', 'Seven', '27 200 %', 'strikes 4 enemies per tick, so a big pack shares it'),
]

SAYS = [
  '<strong>Baseline:</strong> Commando’s main moves come to 1,255 %/s, so balanced is 1,004–1,506.',
  '<strong>Haze (1,315) and Seven (1,272) are inside.</strong> Haze’s gun is now her engine: Fixation bursts every 10 stacks splash the whole bunched pack (1,448 alone).',
  '<strong>Seven</strong> is in through cooldowns alone: 16 / 16 / 30 s. His fixed gun now carries him (552 alone).',
  '<strong>Abrams (729) is below</strong>, and lower together than his abilities alone: Shoulder Charge runs him up to 70 m away from the pack and he stops shooting while he charges and punches.',
  '<strong>Infernus (920) is 84 under the band</strong>, within the test’s noise; Torment Pulse is now 823 alone, in line with Napalm.',
]

CAVEATS = [
  'The Q ults are left out of balance on purpose: any survivor can carry them. They are shown below for fun.',
  'Twelve still, bunched enemies favours area damage (Fixation splash, Napalm, Torment Pulse); a single-target test would complete the picture.',
  'One run per pass: about ±10 % noise. Commando’s Phase Round measured 752 here against 854 in the v0.4.0 run.',
  'Dashes are put back on the spot only after they end, so long dashes (Shoulder Charge) cost more here than in a real fight, where they go into the pack.',
]

RETEST = []

FONTS = '<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin><link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Saira+Condensed:wght@500;700;800&family=IBM+Plex+Sans:wght@400;500;600&family=IBM+Plex+Mono:wght@400;500&display=swap">'

def css(accent, accent2):
    return f'''
/* layout: a field manual: dossier header, a to-scale DPS strip against Commando, then one plate per ability */
:root {{
  --bg: #0f1216; --panel: #161b21; --line: #26303a; --fg: #e8edf2; --muted: #93a1ae;
  --accent: {accent}; --accent2: {accent2}; --base: #8fb7a3; --warn: #ffcf5a; --bad: #ff6b6b; --good: #7ed6a0;
  --display: "Saira Condensed", "Arial Narrow", Arial, sans-serif;
  --body: "IBM Plex Sans", "Segoe UI", system-ui, sans-serif;
  --mono: "IBM Plex Mono", ui-monospace, Consolas, monospace;
  color-scheme: dark;
}}
body {{ background: var(--bg); color: var(--fg); font: 15px/1.55 var(--body); }}
.wrap {{ max-width: 1060px; margin: 0 auto; padding-inline: 20px; padding-block: 28px 56px; display: grid; gap: 28px; }}
.eyebrow {{ font: 500 12px/1 var(--mono); letter-spacing: .14em; text-transform: uppercase; color: var(--muted); }}
h1 {{ font: 800 clamp(44px, 9vw, 88px)/.9 var(--display); letter-spacing: .01em; margin: 0; text-transform: uppercase; color: var(--fg); text-wrap: balance; }}
h1 span {{ color: var(--accent); }}
h2 {{ font: 700 26px/1.1 var(--display); text-transform: uppercase; letter-spacing: .03em; margin: 0; text-wrap: balance; }}
h3 {{ font: 700 22px/1.1 var(--display); text-transform: uppercase; letter-spacing: .02em; margin: 0; }}
p {{ margin: 0; max-width: 68ch; }}
.muted {{ color: var(--muted); }}
.head {{ display: grid; grid-template-columns: minmax(0,1.4fr) minmax(0,1fr); gap: 24px; align-items: end; border-bottom: 1px solid var(--line); padding-bottom: 22px; }}
.tag {{ font: 500 18px/1.3 var(--body); color: var(--accent2); margin-top: 10px; }}
.score {{ display: grid; gap: 6px; justify-items: end; text-align: right; }}
.score .big {{ font: 800 64px/1 var(--display); font-variant-numeric: tabular-nums; color: var(--accent); }}
.score .unit {{ font: 500 13px/1.3 var(--mono); color: var(--muted); }}
.pill {{ display: inline-flex; align-items: center; gap: 6px; padding: 4px 10px; border-radius: 999px; font: 500 12px/1.2 var(--mono); letter-spacing: .04em; border: 1px solid currentColor; }}
.pill.bad {{ color: var(--bad); }} .pill.good {{ color: var(--good); }} .pill.warn {{ color: var(--warn); }}
.strip {{ display: grid; gap: 10px; }}
.row {{ display: grid; grid-template-columns: 120px minmax(0,1fr) 64px; gap: 12px; align-items: center; }}
.row .k {{ font: 500 12px/1.2 var(--mono); color: var(--muted); text-transform: uppercase; letter-spacing: .08em; }}
.row .k b {{ display: block; color: var(--fg); font: 600 14px/1.25 var(--body); text-transform: none; letter-spacing: 0; }}
.track {{ position: relative; height: 22px; background: var(--panel); border: 1px solid var(--line); border-radius: 3px; }}
.bar {{ position: absolute; inset: 3px auto 3px 3px; border-radius: 2px; background: var(--accent); }}
.bar.base {{ background: var(--base); opacity: .8; }}
.row .v {{ font: 500 14px/1 var(--mono); font-variant-numeric: tabular-nums; text-align: right; }}
.legend {{ display: flex; flex-wrap: wrap; gap: 16px; font: 12px/1.3 var(--mono); color: var(--muted); }}
.legend i {{ display: inline-block; width: 12px; height: 12px; border-radius: 2px; margin-right: 6px; vertical-align: -2px; }}
.plates {{ display: grid; gap: 14px; }}
.plate {{ display: grid; grid-template-columns: 92px minmax(0,1fr) minmax(0,.9fr); gap: 18px; padding: 18px; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; }}
.plate.passive {{ border-style: dashed; }}
.key {{ font: 800 30px/1 var(--display); color: var(--accent); text-transform: uppercase; }}
.key small {{ display: block; font: 500 11px/1.4 var(--mono); color: var(--muted); letter-spacing: .1em; margin-top: 6px; }}
.desc {{ display: grid; gap: 8px; min-width: 0; }}
dl {{ margin: 0; display: grid; grid-template-columns: auto minmax(0,1fr); gap: 6px 14px; align-content: start; font-size: 14px; }}
dt {{ font: 500 12px/1.6 var(--mono); color: var(--muted); text-transform: uppercase; letter-spacing: .06em; }}
dd {{ margin: 0; font-variant-numeric: tabular-nums; }}
.dps {{ grid-column: 1 / -1; display: flex; flex-wrap: wrap; gap: 10px; align-items: center; border-top: 1px solid var(--line); padding-top: 10px; font: 13px/1.3 var(--mono); color: var(--muted); }}
.dps strong {{ color: var(--fg); font-size: 16px; }}
.cols {{ display: grid; grid-template-columns: minmax(0,1fr) minmax(0,1fr); gap: 18px; }}
.box {{ background: var(--panel); border: 1px solid var(--line); border-radius: 6px; padding: 18px; display: grid; gap: 10px; align-content: start; }}
ul {{ margin: 0; padding-left: 18px; display: grid; gap: 6px; }}
.foot {{ font: 12px/1.5 var(--mono); color: var(--muted); border-top: 1px solid var(--line); padding-top: 14px; }}
a {{ color: var(--accent2); }}
.tablewrap {{ overflow-x: auto; }}
table {{ border-collapse: collapse; width: 100%; min-width: 620px; font-size: 14px; }}
th, td {{ padding: 9px 10px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }}
th {{ font: 500 12px/1.3 var(--mono); color: var(--muted); text-transform: uppercase; letter-spacing: .06em; }}
td.n {{ font-family: var(--mono); font-variant-numeric: tabular-nums; text-align: right; }}
.heat {{ font-family: var(--mono); text-align: right; font-variant-numeric: tabular-nums; }}
@media (max-width: 720px) {{
  .head, .cols {{ grid-template-columns: minmax(0,1fr); }}
  .score {{ justify-items: start; text-align: left; }}
  .plate {{ grid-template-columns: minmax(0,1fr); }}
  .row {{ grid-template-columns: 92px minmax(0,1fr) 52px; }}
}}
@media (prefers-reduced-motion: no-preference) {{ .bar {{ transition: width .6s ease; }} }}
'''

def bars(rows, scale):
    out = []
    for label, name, val, kind in rows:
        w = 0 if val is None else max(0.0, min(100.0, val / scale * 100))
        v = '–' if val is None else f'{val:,}'
        out.append(f'<div class="row"><div class="k">{E(label)}<b>{E(name)}</b></div><div class="track" role="img" aria-label="{E(name)} {v} percent per second"><div class="bar{" base" if kind == "base" else ""}" style="width:calc({w:.2f}% - 6px)"></div></div><div class="v">{v}</div></div>')
    return '\n'.join(out)

def hero_page(h):
    slots = {k[0]: k for k in h['kit']}
    scale = max([k[2] for k in h['kit'] if k[2] and k[0] in MAIN] + [COMMANDO[s][1] for s in MAIN]) * 1.05
    rows = []
    for slot in MAIN:
        k = slots[slot]
        rows.append((slot, k[1], k[2], 'hero'))
        rows.append(('Commando', COMMANDO[slot][0], COMMANDO[slot][1], 'base'))
    plates = []
    for slot, name, dps, desc, nums, note, retest in h['kit']:
        dl = ''.join(f'<dt>{E(a)}</dt><dd>{E(b)}</dd>' for a, b in nums)
        if dps is None: d = f'<span>DPS</span> <strong>–</strong> <span>{E(note)}</span>'
        elif slot == 'Q':
            d = f'<span>One use</span> <strong>{E(note)}</strong> <span>· not balanced: a pickup any survivor can carry (spread over 30 s: {dps:,} %/s)</span>'
        else:
            d = f'<span>DPS alone</span> <strong>{dps:,} %/s</strong> <span>· {dps*12/100:.0f} dmg/s at level 1</span>'
            if note: d += f' <span>· {E(note)}</span>'
        if retest: d += ' <span class="pill warn">⚠ retest</span>'
        sub = 'passive' if slot == 'Passive' else {'M1': 'left click', 'M2': 'right click', 'Shift': 'utility', 'R': 'special', 'Q': 'ult equipment'}[slot]
        plates.append(f'''<article class="plate{' passive' if slot == 'Passive' else ''}">
  <div class="key">{E(slot)}<small>{sub}</small></div>
  <div class="desc"><h3>{E(name)}</h3><p>{E(desc)}</p></div>
  <dl>{dl}</dl>
  <div class="dps">{d}</div>
</article>''')
    side, gap = verdict(h['all'])
    sign = '+' if gap >= 0 else '−'
    pill = (f'<span class="pill good">{sign}{abs(gap):,} %/s vs Commando: inside ±20 %</span>' if side == 'inside' else
            f'<span class="pill bad">{sign}{abs(gap):,} %/s vs Commando: outside ±20 %</span>')
    fixes = ''.join(f'<li>{E(f)}</li>' for f in h['fixes'])
    return f'''<title>{E(h["name"])} Kit Sheet</title>
{FONTS}
<style>{css(h["accent"], h["accent2"])}</style>
<main class="wrap">
  <header class="head">
    <div>
      <div class="eyebrow">Deadlock of Rain · {VERSION} · Risk of Rain 2 analog: {E(h["analog"])}</div>
      <h1>{E(h["name"])}</h1>
      <div class="tag">{E(h["tag"])}</div>
    </div>
    <div class="score">
      <div class="eyebrow">Everything at once, 30 s</div>
      <div class="big">{h["all"]:,}</div>
      <div class="unit">% of damage per second · {h["all"]*12/100:.0f} dmg/s at level 1 · main moves only (M1, M2, Shift, R)</div>
      {pill}
    </div>
  </header>

  <section class="strip" aria-labelledby="s1">
    <h2 id="s1">Each ability alone vs Commando</h2>
    <div class="legend"><span><i style="background:var(--accent)"></i>{E(h["name"])}</span><span><i style="background:var(--base)"></i>Commando (baseline)</span><span>Scale: % of damage per second, used whenever ready for 30 s. The Q ult is left out of balance (see its plate).</span></div>
    {bars(rows, scale)}
  </section>

  <section class="plates" aria-labelledby="s2">
    <h2 id="s2">The kit</h2>
    {"".join(plates)}
  </section>

  <section class="cols">
    <div class="box">
      <h2>Why the number</h2>
      <p>Main moves at once: <strong>{h["all"]:,} %/s</strong> against Commando’s <strong>{COMMANDO["all"]:,} %/s</strong> (no equipment for anyone). Balanced is {LO:,}–{HI:,} (±20 %). The Q ults are pickups any survivor can carry, so they are left out of balance. The pack of 12 bunched enemies favours area damage.</p>
    </div>
    <div class="box">
      <h2>Changed in {VERSION}</h2>
      <ul>{fixes}</ul>
      <p class="muted">{E(h["est"])}</p>
    </div>
  </section>

  <p class="foot">{E(BENCH)} DPS = % of the survivor’s damage stat per second (100 %/s = one 100 % hit per second; damage stat 12 at level 1). Sources: docs/06-CURRENT-ABILITIES.md, docs/05-DAMAGE-COMPARISON.md, design/*.json.</p>
</main>
'''

def compare_page():
    acc, acc2 = '#c9d6e3', '#8fb7a3'
    all_rows = [('Baseline', 'Commando', COMMANDO['all'], 'base')] + [(h['analog'], h['name'], h['all'], 'hero') for h in HEROES]
    scale = max([COMMANDO['all'] * 1.6] + [h['all'] * 1.1 for h in HEROES])
    def allbar(label, name, val, kind, color):
        w = val / scale * 100
        band_l, band_r = LO / scale * 100, HI / scale * 100
        return f'<div class="row"><div class="k">{E(label)}<b>{E(name)}</b></div><div class="track" role="img" aria-label="{E(name)} {val} percent per second"><div style="position:absolute;top:0;bottom:0;left:{band_l:.2f}%;width:{band_r-band_l:.2f}%;background:rgba(143,183,163,.18);border-left:1px dashed var(--base);border-right:1px dashed var(--base)"></div><div class="bar" style="width:calc({w:.2f}% - 6px);background:{color}"></div></div><div class="v">{val:,}</div></div>'
    bars_html = '\n'.join(allbar(l, n, v, k, 'var(--base)' if k == 'base' else h_color) for (l, n, v, k), h_color in zip(all_rows, ['#8fb7a3'] + [h['accent'] for h in HEROES]))
    # per-slot matrix
    def cell(v, base):
        if v is None: return '<td class="heat">–</td>'
        ratio = v / base if base else None
        col = 'var(--fg)'
        if base:
            col = 'var(--bad)' if v > base * 1.5 else 'var(--warn)' if v < base * 0.5 else 'var(--good)'
        r = f'<br><span class="muted" style="font-size:11px">{ratio:.1f}×</span>' if ratio else ''
        return f'<td class="heat" style="color:{col}">{v:,}{r}</td>'
    slot_rows = []
    for slot in MAIN:
        base = COMMANDO[slot][1]
        cells = ''.join(cell(next(k for k in h['kit'] if k[0] == slot)[2], base) for h in HEROES)
        slot_rows.append(f'<tr><th scope="row">{slot}</th><td class="heat">{base:,}<br><span class="muted" style="font-size:11px">{E(COMMANDO[slot][0])}</span></td>{cells}</tr>')
    fun_rows = ''.join(f'<tr><td>{E(name)}</td><td>{E(who)}</td><td class="n">{per_use}</td><td>{E(why)}</td></tr>' for name, who, per_use, why in FUN)
    outside = sum(1 for h in HEROES if verdict(h['all'])[0] != 'inside')
    kit_rows = []
    for h in HEROES:
        for slot, name, dps, desc, nums, note, retest in h['kit']:
            flag = ' <span class="pill warn">⚠ retest</span>' if retest else ''
            kit_rows.append(f'<tr><td>{E(h["name"])}</td><td>{E(slot)}</td><td>{E(name)}{flag}</td><td>{E(", ".join(b for a, b in nums[:3]))}</td><td class="n">{"–" if dps is None else f"{dps:,}"}</td></tr>')
    sugg = ''.join(f'<div class="box"><h3 style="color:{h["accent"]}">{E(h["name"])}</h3><p><strong>{h["all"]:,} %/s</strong> · {"+" if verdict(h["all"])[1] >= 0 else "−"}{abs(verdict(h["all"])[1]):,} vs Commando · {verdict(h["all"])[0]} the band</p><ul>{"".join(f"<li>{E(f)}</li>" for f in h["fixes"])}</ul><p class="muted">{E(h["est"])}</p></div>' for h in HEROES)
    return f'''<title>Deadlock of Rain Balance</title>
{FONTS}
<style>{css(acc, acc2)}
.grid4 {{ display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 14px; }}
@media (max-width: 720px) {{ .grid4 {{ grid-template-columns: minmax(0,1fr); }} }}
</style>
<main class="wrap">
  <header class="head">
    <div>
      <div class="eyebrow">Deadlock of Rain · {VERSION} · docs 04, 05 and 06</div>
      <h1>Balance <span>vs</span> Commando</h1>
      <div class="tag">Every survivor’s main moves, same pack, same 30 seconds, Monsoon. The Q ults are left out.</div>
    </div>
    <div class="score">
      <div class="eyebrow">Target band</div>
      <div class="big" style="color:var(--base)">{COMMANDO["all"]:,}</div>
      <div class="unit">Commando’s main moves (M1, M2, Shift, R; no equipment), % of damage per second · ±20 % allowed ({LO:,}–{HI:,})</div>
      <span class="pill {"good" if outside == 0 else "bad"}">{"all 4 heroes inside the band" if outside == 0 else f"{outside} of 4 heroes outside the band"}</span>
    </div>
  </header>

  <section class="strip" aria-labelledby="c1">
    <h2 id="c1">Main moves at once, 30 s</h2>
    <div class="legend"><span><i style="background:rgba(143,183,163,.35);border:1px dashed var(--base)"></i>Commando ±20 %</span><span>% of damage per second; level 1 damage stat 12 for everyone.</span></div>
    {bars_html}
  </section>

  <section class="box" aria-labelledby="c2">
    <h2 id="c2">Each ability alone</h2>
    <p class="muted">%/s for 30 s, used whenever ready. Colour: green within half to 1.5× of Commando’s same slot, yellow below half, red above 1.5×.</p>
    <div class="tablewrap"><table>
      <thead><tr><th>Slot</th><th>Commando</th>{"".join(f"<th>{E(h['name'])}</th>" for h in HEROES)}</tr></thead>
      <tbody>{"".join(slot_rows)}</tbody>
    </table></div>
  </section>

  <section class="cols">
    <div class="box">
      <h2>What the numbers say</h2>
      <ul>{"".join(f"<li>{t}</li>" for t in SAYS)}</ul>
    </div>
    <div class="box">
      <h2>Read before acting</h2>
      <ul>{"".join(f"<li>{t}</li>" for t in CAVEATS)}</ul>
    </div>
  </section>

  <section class="box" aria-labelledby="c7">
    <h2 id="c7">Q ults: just for fun (not balanced)</h2>
    <p class="muted">The ults are pickups any survivor can carry, so they are left out of balance and may be as strong as we like. One use each against the same pack of 12.</p>
    <div class="tablewrap"><table>
      <thead><tr><th>Equipment</th><th>Starts on</th><th>One use</th><th>Why</th></tr></thead>
      <tbody>{fun_rows}</tbody>
    </table></div>
  </section>

  <section class="plates" aria-labelledby="c3">
    <h2 id="c3">Per hero: what changed in {VERSION}</h2>
    <div class="grid4">{sugg}</div>
  </section>

  <section class="box" aria-labelledby="c4">
    <h2 id="c4">Retest needed</h2>
    <div class="tablewrap"><table>
      <thead><tr><th>Survivor</th><th>Ability</th><th>Change since the last benchmark</th><th>Commit</th><th>Status</th></tr></thead>
      <tbody>{"".join(f'<tr><td>{E(a)}</td><td>{E(b)}</td><td>{E(c)}</td><td>{E(d)}</td><td><span class="pill warn">⚠ retest</span></td></tr>' for a, b, c, d in RETEST) or '<tr><td colspan="5">Nothing: the last benchmark covers every ability.</td></tr>'}</tbody>
    </table></div>
  </section>

  <section class="box" aria-labelledby="c5">
    <h2 id="c5">Every ability today</h2>
    <div class="tablewrap"><table>
      <thead><tr><th>Hero</th><th>Key</th><th>Ability</th><th>Key numbers</th><th>DPS alone</th></tr></thead>
      <tbody>{"".join(kit_rows)}</tbody>
    </table></div>
  </section>

  <section class="box" aria-labelledby="c6">
    <h2 id="c6">Test variables</h2>
    <ul>
      <li>Monsoon, first stage of a fresh run, one fixed flat spot; level 1, no items, damage stat 12 for all.</li>
      <li>12 enemies (8 Lemurians, 4 Beetles), AI off, ×101 health, a 3 × 4 grid 10–15 m in front; nothing dies.</li>
      <li>M1 held; M2, Shift and R pressed about 25 times a second, so each fires the moment it is ready; no equipment in the balance passes; a separate pass fires the Q once (heroes: their ult; Commando: Preon Accumulator) for the fun table; aim on the nearest enemy; put back on the spot after dashes.</li>
      <li>Counted: every hit the survivor lands (direct, projectiles, zones, DoTs), after crits and damage mods, before armor. One run per pass: about ±10 % noise.</li>
      <li>Not measured: stuns, slows, heals, invisibility, Warbanner. The setup favours area damage; a single-target test would complete the picture.</li>
    </ul>
  </section>

  <p class="foot">{E(BENCH)} Sources: docs/04-ABILITY-REDESIGN-PROPOSALS.md (the creator’s picks), docs/05-DAMAGE-COMPARISON.md (method, results, suggestions, retest flags), docs/06-CURRENT-ABILITIES.md (current kit). Rerun with <code>bash tools/run_bench.sh</code>.</p>
</main>
'''

for h in HEROES:
    open(os.path.join(OUT, f'{h["id"]}-kit.html'), 'w', encoding='utf-8').write(hero_page(h))
open(os.path.join(OUT, 'balance-vs-commando.html'), 'w', encoding='utf-8').write(compare_page())
print('\n'.join(sorted(os.listdir(OUT))))
