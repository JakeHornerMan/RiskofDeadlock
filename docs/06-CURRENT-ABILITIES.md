# 06: Current abilities (every survivor, with DPS)

> **Living document.** It must match `design/*.json` and the code at all times. Any change to an ability (numbers,
> behaviour, passive, ult equipment) updates this page **and** adds a "retest needed" row to doc 05 §0, in the same
> commit (CLAUDE.md standing rule). After `bash tools/run_bench.sh`, the DPS columns here and in doc 05 are refreshed together.

**Pages (claude.ai Artifacts, private to the creator):** [Haze](https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p) ·
[Abrams](https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG) · [Seven](https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m) ·
[Infernus](https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF) · [Balance vs Commando](https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3).
Built by `python tools/kit_pages.py` into `build/pages/`. Republish each page to its link whenever this doc or doc 05 changes.

**Version:** 0.4.1 (2026-10-16).

**DPS source:** the v0.4.1 benchmark of 2026-10-16; doc 05 lists every variable. Setup:
- Monsoon, level 1, no items
- 12 still enemies 10–15 m away, perfect aim
- **each ability used alone, whenever ready, for 30 s**

How to read the DPS:
- **%/s** = percent of the survivor's damage stat per second (level-independent; 100 %/s = one 100 % hit per second).
- **dmg/s** = the same at level 1 (damage stat 12, the same for every survivor).
- **⚠ retest** = the number is out of date or suspect (see doc 05 §0).
- **Balance** compares the main moves (M1, M2, Shift, R) together, without equipment, with the hero's **own RoR2 match** (the
  survivor its stats are built on): balanced = within **±20 %** of the match (doc 05 §3.0). Haze → Commando, Abrams → Loader,
  Seven → Artificer, Infernus → Engineer.
- DPS below is from the second v0.4.1 run (with the matches); ±10 % noise between runs.
- **Q (equipment)** is **not balanced** (any survivor can carry it): its column is one use, spread over 30 s, plus its total, for fun.
- Passives have no number of their own: they are inside their gun's (and other skills') numbers.

**Every hero starts each run holding their own ult on Q.** Swap it for another equipment and it stays on the ground.
Anyone, Commando included, can find the four ults wherever the Preon Accumulator drops. The pickup is a small figure
of the hero.

---

## Baseline: Commando (vanilla Risk of Rain 2)

| Key | Ability | DPS alone (%/s · dmg/s) |
|---|---|---|
| M1 | Double Tap | **603** · 72 |
| M2 | Phase Round (pierces) | **752** · 90 |
| Shift | Tactical Dive | 0 |
| R | Suppressive Fire (4 casts in 30 s) | 80 · 10 |
| | **Main moves at once** (no equipment) | **1255 · 151** (the balance baseline) |
| Q | Preon Accumulator (not balanced; one use: 58400 % on the pack) | 1947 · 234 |

---

## Haze: the smoke-shrouded assassin (main moves at once: **1478 %/s · 177 dmg/s**; match Commando 1064: above the band)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Fixation** | Each SMG hit stacks Fixation on the target; each stack = +2.5 % damage from Haze. Crits add 3 stacks. Every 10 stacks: a 300 % burst that splashes every enemy within 6 m, and a 12 % slow for 2 s. | max 40 stacks, 6 s | – | in M1 |
| M1 | **Twin SMGs** | Two SMGs, alternating, firing from the end of each gun. Firing breaks Smoke Bomb. | 55 % per bullet, 9.5 shots/s, 25-round magazine, 1.4 s reload, proc 0.5 | – | **1020** · 122 (Fixation bursts included) |
| M2 | **Sleep Dagger** | A thrown dagger that pierces every enemy in its line; each takes the damage and is put to sleep (stunned). | 500 % each, stun 4 s, 120 m/s, proc 1.0 | 6 s | **100** · 12 |
| Shift | **Smoke Bomb** | Invisible (vanilla Cloak) and faster; enemies lose her as a target and can't pick her again. Firing the guns reveals her. | +50 % move speed, 5 s | 12 s | 0 |
| R | **Phantom Strike** (Deadlock item) | Blinks behind the enemy under the crosshair (≤ 30 m) and cuts it 3 times; each cut adds 5 Fixation. No target: no blink, and R stays ready. Violet streak, smoke and slash effects. | 3 × 250 %, proc 1.0 | 10 s | **352** · 42 |
| Q | **Bullet Dance** (equipment) | Spins and fires at every enemy within 25 m at once, 30 % chance to evade attacks, slowed to 60 %. Blade-ring and spark effects. | 113 % per shot to each enemy, ~12 shots/s for 5 s (≈6800 % per enemy) | 120 s | 1567 · 188 (not balanced; one use: 47008 %, 8 enemies) |

## Abrams: the unstoppable detective (main moves at once: **985 %/s · 118 dmg/s**; match Loader 2747: below the band)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Infernal Resilience** | Heals back part of every hit he takes; debuffs on him are shorter. | 10 % of damage taken over 10 s; debuffs −20 % | – | (no damage) |
| M1 | **Shotgun** | Nine pellets with spread, from the end of the gun. | 9 × 45 %, 1.6 shots/s, 9 shells, 1.5 s reload, proc 0.4 | – | **508** · 61 |
| M2 | **Siphon Life** | RoR2's Mired Urn tethers: drains the nearest enemies and heals him for all of it. | up to 3 enemies within 25 m, 250 %/s each, 8 s, heals 100 % of damage dealt | 15 s | **400** · 48 |
| Shift | **Shoulder Charge** | Charges along his aim (steerable), running like his sprint, faster. Every enemy he runs into is hit, stunned and knocked aside. A wall stops him. | 500 % per enemy, stun 1 s, knockback 22 m/s, 28 m/s for up to 3 s | 8 s | **350** · 42 |
| R | **Haymaker** (Deadlock heavy melee) | A 0.4 s wind-up with a glowing fist, a 6 m lunge, then a heavy punch whose shockwave hits every enemy in a cone in front and knocks them flying. | 900 % each; cone 6 m long, 30° each side; knockback 30 m/s, proc 1.0 | 5 s | **570** · 68 |
| Q | **Seismic Impact** (equipment) | Rises, hovers up to 3 s while a ground reticle follows the aim (≤ 40 m); **Left Click** slams there (or it slams by itself after 3 s). He can be hit while hovering. | 6800 % in 18 m, stun 1.6 s | 120 s | 2493 · 299 (not balanced; one use: 74800 % on 11 enemies) |

## Seven: the living storm (main moves at once: **1242 %/s · 149 dmg/s**; match Artificer 2790: below the band)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Capacitor** | Gun hits, Lightning Ball shocks and Power Surge arcs build Static. At 10 stacks the target discharges, arcing to 2 nearby enemies, and everything hit is stunned. | 300 % to each, 12 m, stun 1 s; Static max 10, 5 s | – | in M1, M2, R |
| M1 | **Shock Rifle** | 3-round bursts from the end of the gun; hits build Static. | 3 × 48 %, burst every 0.26 s, 29 rounds, 1.4 s reload, proc 0.7 | – | **672** · 81 |
| M2 | **Lightning Ball** | A slow ball that flies through enemies (stops only on walls), shocking everything near it; every shock builds Static. | 60 % every 0.25 s to all within 6 m (240 %/s), 14 m/s, 5 s, proc 0.3 | 16 s | **118** · 14 |
| Shift | **Static Charge** | Plants a charge on the aimed enemy or spot; after a countdown, a thunderbolt from the sky. | 450 % in 8 m, stun 0.9 s, 1.5 s fuse | 16 s | **165** · 20 |
| R | **Power Surge** (his Deadlock ability) | For 8 s he crackles with lightning: every Shock Rifle burst arcs RoR2 Tesla lightning (like the Unstable Tesla Coil) from the target to up to 4 more enemies within 10 m, each arc building Static. +40 % move speed. | 60 % per arc, proc 0.2 | 30 s | 0 alone (it powers the gun; counted in "all") |
| Q | **Thunderhead** (equipment) | Dark storm clouds over the spot he stands on, darkening it; lightning strikes enemies from the clouds; allies inside get RoR2's Warbanner. | up to 4 enemies every 0.4 s, 272 % each, 20 m, 10 s (6800 % per enemy); Warbanner +30 % attack and move speed | 120 s | 907 · 109 (not balanced; one use: 27200 %) |

## Infernus: the burning man (main moves at once: **1226 %/s · 147 dmg/s**; match Engineer 1268: inside the band)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Afterburn** | Gun hits build Afterburn; at 12 stacks the target burns. Embers show the build-up; RoR2's Helfire flare and big flames show the burn. | burn 100 %/s for 3 s; build-up max 12, 4 s | – | in M1 and R |
| M1 | **Flame Gun** | Automatic, from the end of the gun; hits build Afterburn. | 50 % per bullet, 9.5 shots/s, 27 rounds, 1.4 s reload, proc 0.5 | – | **317** · 38 |
| M2 | **Napalm** | A lobbed flask that bursts in a ring of fire and leaves a burning puddle: damage, a slow, and the target takes more damage from him. | 300 % in 5 m, slow 28 % for 4 s, +16 % damage taken from Infernus for 8 s, 2 charges | 5 s | **538** · 65 |
| Shift | **Flame Dash** | Dash at ×2.2 speed; hold to keep going. Leaves burning ground behind him; enemies he runs into are knocked aside and scorched. 50 % slow resistance while dashing. | trail 150 %/s for 5 s; shove 75 % + knockback 22 m/s; 0.8 s on a tap, up to 3 s held | 8 s | **311** · 37 |
| R | **Torment Pulse** (Deadlock item) | For 6 s, rings of fire race out of him every 0.5 s, burning everything within 10 m and building Afterburn. | 50 % per pulse, +2 Afterburn build-up, proc 0.5 | 12 s | **768** · 92 (burns included) |
| Q | **Concussive Combustion** (equipment) | Becomes a bomb, fire gathering on him; can move while it builds; the blast stuns and leaves burning ground. | 6800 % in 18 m after a 3 s fuse, stun 2 s, each kill heals 10 % of max health | 120 s | 2493 · 299 (not balanced; one use: 74800 % on 11 enemies) |

---

## The ult equipment (all four heroes)
- **What they are:** RoR2 equipment on Q (`design/equipment.json`) with a **120 s** cooldown. Fuel Cells and Gesture of the Drowned work as on any equipment.
- **Drops:** they follow the Preon Accumulator's rules: chests, Equipment Barrels, the Artifact of Enigma and Bottled Chaos.
- **Power:** at least 6800 % per enemy per use, the Preon scaled from 140 s to 120 s. **Not balanced** (creator's decision, v0.4.1): any survivor can carry them, so they may be as strong as we like and are left out of the comparison with Commando.
- **Starting:** every hero starts holding their own; there is no charge meter any more.
- **Other users:** any survivor, or an Equipment Drone, can fire any of them. On non-heroes they use plain muzzles and no Deadlock animation.
