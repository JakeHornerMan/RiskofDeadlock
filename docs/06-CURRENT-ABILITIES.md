# 06: Current abilities (every survivor, with DPS)

> **Living document.** It must match `design/*.json` and the code at all times. Any change to an ability (numbers,
> behaviour, passive, ult meter) updates this page **and** adds a "retest needed" row to doc 05 §0, in the same commit
> (CLAUDE.md standing rule). After `bash tools/run_bench.sh`, the DPS columns here and doc 05 are refreshed together.

**Version:** 0.3.0 (commit `081139c`, 2026-10-16). **DPS source:** the benchmark of 2026-10-16 (commit `714977f`;
doc 05 has every variable): Monsoon, level 1, no items, 12 still enemies 10–15 m away, perfect aim, **each ability
used alone, whenever ready, for 30 s**.

How to read the DPS:
- **%/s** = percent of the survivor's damage stat per second (level-independent; 100 %/s = one 100 % hit per second).
- **dmg/s** = the same at level 1 (damage stat 12, the same for every survivor).
- **⚠ retest** = the ability changed after the last benchmark, so its number is out of date (see doc 05 §0).
- The ult column is **one cast** spread over 30 s (the R pass starts with a full meter), plus its total.
- Passives have no number of their own: they are inside their gun's (and Lightning Ball's) number.

---

## Baseline: Commando (vanilla Risk of Rain 2)

| Key | Ability | DPS alone (%/s · dmg/s) |
|---|---|---|
| M1 | Double Tap | **603** · 72 |
| M2 | Phase Round (pierces) | **592** · 71 |
| Shift | Tactical Dive | 0 |
| R | Suppressive Fire (cooldown; 4 casts in 30 s) | 80 · 10 |
| | **Everything at once** | **1058 · 127** |

---

## Haze: the smoke-shrouded assassin (everything at once: **395 %/s · 47 dmg/s**)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Fixation** | Each SMG hit stacks Fixation on the target; each stack = +2.5 % damage from Haze. Crits add 3 stacks. Every 20 stacks: a 200 % burst and a 12 % slow for 2 s. | max 40 stacks, 6 s | – | in M1 |
| M1 | **Twin SMGs** | Two SMGs, alternating, firing from the end of each gun. Firing breaks Smoke Bomb. | 45 % per bullet, 9.5 shots/s, 25-round magazine, 1.4 s reload, proc 0.5 | – | **298** · 36 |
| M2 | **Sleep Dagger** | A thrown dagger; the first enemy it hits takes the damage and is put to sleep (stunned). | 350 %, stun 4 s, 120 m/s, proc 1.0 | 10 s | **35** · 4 |
| Shift | **Smoke Bomb** | Invisible (vanilla Cloak) and faster; enemies lose her as a target and can't pick her again. Firing the guns reveals her. | +50 % move speed, 5 s | 12 s | 0 |
| R | **Bullet Dance** | Charge-meter ultimate. Spins and auto-fires at random enemies within 25 m, 30 % chance to evade attacks, slowed to 60 %. Blade-ring and spark effects. | 49 % per shot, ~12 shots/s for 5 s (≈2950 % total), meter 5000 | meter | **98** · 12 (one cast) |

## Abrams: the unstoppable detective (everything at once: **2336 %/s · 280 dmg/s**)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Infernal Resilience** | Heals back part of every hit he takes; debuffs on him are shorter. | 10 % of damage taken over 10 s; debuffs −20 % | – | (no damage) |
| M1 | **Shotgun** | Nine pellets with spread, from the end of the gun. | 9 × 45 %, 1.6 shots/s, 9 shells, 1.5 s reload, proc 0.4 | – | **511** · 61 |
| M2 | **Siphon Life** | RoR2's Mired Urn tethers: drains the nearest enemies and heals him for all of it. | up to 3 enemies within 25 m, 250 %/s each, 8 s, heals 100 % of damage dealt | 15 s | **400** · 48 |
| Shift | **Shoulder Charge** | Charges along his aim (steerable), running like his sprint, faster. Every enemy he runs into is hit, stunned and knocked aside. A wall stops him. | 350 % per enemy, stun 1 s, knockback 22 m/s, 28 m/s for up to 3 s | 8 s | **257** · 31 |
| R | **Seismic Impact** | Charge-meter ultimate. Rises, hovers up to 3 s while a ground reticle follows the aim (≤ 40 m), R or Left Click slams there. He can be hit while hovering. | 2200 % in 12 m, stun 1.6 s, meter 4500 | meter | **880** · 106 (one cast: 26400 % on 12 enemies) |

## Seven: the living storm (everything at once: **6850 %/s · 822 dmg/s**)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Capacitor** | Gun and Lightning Ball hits build Static. At 10 stacks the target discharges, arcing to 2 nearby enemies, and everything hit is stunned. | 300 % to each, 12 m, stun 1 s; Static max 10, 5 s | – | in M1 and M2 |
| M1 | **Shock Rifle** | 3-round bursts from the end of the gun; hits build Static. | 3 × 48 %, burst every 0.26 s, 29 rounds, 1.4 s reload, proc 0.7 | – | **632** · 76 |
| M2 | **Lightning Ball** | A slow ball that flies through enemies (stops only on walls), shocking everything near it; every shock builds Static. | 60 % every 0.25 s to all within 6 m (240 %/s), 14 m/s, 5 s, proc 0.3 | 6 s | **1852** · 222 |
| Shift | **Static Charge** | Plants a charge on the aimed enemy or spot; after a countdown, a thunderbolt from the sky. | 450 % in 8 m, stun 0.9 s, 1.5 s fuse | 7 s | **585** · 70 |
| R | **Thunderhead** | Charge-meter ultimate. Dark storm clouds over the spot he stands on, darkening it; lightning strikes enemies from the clouds; allies inside get RoR2's Warbanner. | up to 4 enemies every 0.4 s, 110 % each, 20 m, 10 s (2750 % per enemy); Warbanner +30 % attack and move speed; meter 5500 | meter | **367** · 44 (one cast: 11000 %) |

## Infernus: the burning man (everything at once: **3209 %/s · 385 dmg/s**)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Afterburn** | Gun hits build Afterburn; at 12 stacks the target burns. Embers show the build-up, flames the burn. | burn 100 %/s for 3 s; build-up max 12, 4 s | – | in M1 |
| M1 | **Flame Gun** | Automatic, from the end of the gun; hits build Afterburn. | 40 % per bullet, 9.5 shots/s, 27 rounds, 1.4 s reload, proc 0.5 | – | **267** · 32 |
| M2 | **Napalm** | A lobbed flask that bursts in a splash: damage, a slow, and the target takes more damage from him. | 300 % in 5 m, slow 28 % for 4 s, +16 % damage taken from Infernus for 8 s, 2 charges | 5 s | **523** · 63 |
| Shift | **Flame Dash** | Dash at ×2.2 speed; hold to keep going. Leaves a fire trail; enemies he runs into are knocked aside and scorched. 50 % slow resistance while dashing. | trail 150 %/s for 5 s; shove 75 % + knockback 22 m/s; 0.8 s on a tap, up to 3 s held | 8 s | **214** · 26 **⚠ retest** (shove damage added after the bench) |
| R | **Concussive Combustion** | Charge-meter ultimate. Becomes a bomb; can move while it builds; the blast stuns. | 2500 % in 18 m after a 3 s fuse, stun 2 s, each kill heals 10 % of max health; meter 5000 | meter | **917** · 110 (one cast: 27500 % on 11 enemies) |

---

## Ult meter (all four heroes)
R starts **locked** every life and fills from damage dealt (damage ÷ damage stat × 100 per hit, every enemy counted) and
150 per kill; the ult's own damage never charges it. Against a pack, area kits refill it very fast (doc 05 §3.3).
