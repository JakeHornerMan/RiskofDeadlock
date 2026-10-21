# 06: Current abilities (every survivor, with DPS)

> **Living document.** It must match `design/*.json` and the code at all times. Any change to an ability (numbers,
> behaviour, passive, ult equipment) updates this page **and** adds a "retest needed" row to doc 05 §0, in the same
> commit (CLAUDE.md standing rule). After `bash tools/run_bench.sh`, the DPS columns here and in doc 05 are refreshed together.

**Pages (claude.ai Artifacts, shared "anyone with the link" since 2026-10-18; linked from the Melty listing and the README):** [Haze](https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p) ·
[Abrams](https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG) · [Seven](https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m) ·
[Infernus](https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF) · [Mo & Krill](https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa) (shared by the creator) · [Viscous](https://claude.ai/artifact/JRFqAM4uskGuYVJEKA84FC) (new 2026-10-20, pictures since 2026-10-20 v3; the creator still has to share it) · [Rat King](https://claude.ai/artifact/Kw4rTyRrvXTb2eEyT9oTYa) (new 2026-10-21; the creator still has to share it) · [Balance vs Commando](https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3).
Built by `python tools/kit_pages.py` into `build/pages/`. Republish each page to its link whenever this doc or doc 05 changes.

**Version:** 0.9.0 in progress (2026-10-21: Rat King added). 0.8.0f (2026-10-20: Viscous added and reworked after the creator's play: puddles that supply ammo, Splatter 150 %, higher jump; not released). 0.7.4 live. 0.7.0 (2026-10-19: Mo & Krill added). 0.6.3 (2026-10-18; Flame Gun 75 %, Flame Dash a 5 s run laying Afterburn fire pockets. 0.6.2: Combustion fuse ticks. 0.6.1: Napalm aimed throw, Flame Dash 5 s / 13 s, Combustion range ring, ragdoll deaths).

**DPS source:** the stored library `docs/dps-library.json` (doc 05 §6): every vanilla survivor measured 3 times on
2026-10-17 and **never re-measured**; Haze, Abrams and Seven from the v0.5.1 heroes-only run, Infernus from his v0.6.3 run (2026-10-18).
Doc 05 lists every variable. Setup:
- Monsoon, level 1, no items
- 12 still enemies 10–15 m away, perfect aim
- **each ability used alone, whenever ready, for 30 s**

How to read the DPS:
- **%/s** = percent of the survivor's damage stat per second (level-independent; 100 %/s = one 100 % hit per second).
- **dmg/s** = the same at level 1 (damage stat 12, the same for every survivor).
- **⚠ retest** = the number is out of date or suspect (see doc 05 §0).
- **Balance** compares the main moves (M1, M2, Shift, R) together, without equipment, with the hero's **own RoR2 match** (the
  survivor its stats are built on): balanced = within **±500 %/s** of the match, comparing the **means over every bench run of
  the current build** (doc 05 §3.0; one run is too noisy). Haze → Commando, Abrams → Loader,
  Seven → Artificer, Infernus → Engineer, Mo & Krill → MUL-T, Viscous → REX.
- Per-ability DPS below is the library entry of each survivor (means of 3 runs; one run for Haze, Abrams, Seven); single runs swing ±10 %.
- **Q (equipment)** is **not balanced** (any survivor can carry it): its column is one use, spread over 30 s, plus its total, for fun.
- Passives have no number of their own: they are inside their gun's (and other skills') numbers.

**Deaths:** every hero falls as a ragdoll, like the vanilla survivors (v0.6.1).

**Every hero starts each run holding their own ult on Q.** Swap it for another equipment and it stays on the ground.
Anyone, Commando included, can find the four ults wherever the Preon Accumulator drops. The pickup is a small figure
of the hero.

---

## Baseline: Commando (vanilla Risk of Rain 2)

| Key | Ability | DPS alone (%/s · dmg/s) |
|---|---|---|
| M1 | Double Tap | **603** · 72 |
| M2 | Phase Round (pierces) | **717** · 86 |
| Shift | Tactical Dive | 0 |
| R | Suppressive Fire (4 casts in 30 s) | 80 · 10 |
| | **Main moves at once** (no equipment) | **1222 · 147** (the balance baseline; library mean of 3 runs: 1104 / 1384 / 1178) |
| Q | Preon Accumulator (not balanced; one use: 58400 % on the pack) | 1902 · 228 |

### Every other vanilla survivor (stored, never re-measured)
Measured 3 times each on 2026-10-17 (`ALL=1 bash tools/run_bench.sh 30`, doc 05 §6), main moves at once, %/s means.
The base game and Survivors of the Void are all here. Seekers of the Storm survivors are not: that expansion was off in the
bench's run.

| Survivor | All | M1 / M2 / Shift / R alone | Matched by |
|---|---|---|---|
| Mercenary | 5112 | 2850 / 1351 / 1120 / 128 | – |
| Artificer | 4013 | 3052 / 1741 / 78 / 1058 (noisy: burns) | Seven |
| Captain | 3490 | 800 / 7 / 0 / 0 (M1 released every 0.6 s) | – |
| Loader | 2951 | 2333 / 14 / 2486 / 1933 (noisy) | Abrams |
| Huntress | 1921 | 186 / 369 / 0 / 1616 | – |
| Bandit | 1919 | 11 / 1478 / 709 / 140 | – |
| REX | 1738 | 392 / 7465 / 0 / 393 | – |
| Engineer | 1611 | 2113 / 187 / 0 / 39 (turrets count; damage stat 14) | Infernus |
| MUL-T | 1450 | 876 / 754 / 435 / 0 | – |
| Commando | 1222 | 603 / 717 / 0 / 80 | Haze |
| Acrid | 11206 | not comparable (max-health damage on ×101-health enemies) | – |
| Railgunner | 592 | not comparable (the bench doesn't scope) | – |
| Void Fiend | 380 | not comparable (never corrupts in 30 s) | – |

---

## Haze: the smoke-shrouded assassin (main moves at once: **6550 %/s**, one v0.5.1 run; match Commando 1222 (library): **far above** ±500, doc 05 §3.1)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Fixation** | Each SMG hit stacks Fixation on the target; each stack = +2.5 % damage from Haze. Crits add 3 stacks. Every 10 stacks: a 300 % burst that splashes every enemy within 6 m, and a 12 % slow for 2 s. | max 40 stacks, 6 s | – | in M1 |
| M1 | **Twin SMGs** | Two SMGs, alternating, firing from the end of each gun. Firing breaks Smoke Bomb. | 55 % per bullet, 9.5 shots/s, 25-round magazine, 1.4 s reload, proc 0.5 | – | **1020** · 122 (Fixation bursts included) |
| M2 | **Sleep Dagger** | A thrown dagger (from the aim, with a violet glow and trail so it always shows) that pierces every enemy in its line; each takes the damage and is put to sleep (stunned). | 500 % each, stun 4 s, 120 m/s, proc 1.0 | 6 s | **100** · 12 |
| Shift | **Smoke Bomb** | Invisible (vanilla Cloak) and faster; enemies lose her as a target and can't pick her again. Firing the guns reveals her. | +50 % move speed, 5 s | 12 s | 0 |
| R | **Phantom Strike** (Deadlock item) | Blinks behind the enemy under the crosshair (≤ 30 m) and cuts 3 times; each cut hits every enemy within 5 m of the target and adds 5 Fixation to each. No target: no blink, and R stays ready. Violet streak, smoke and slash effects. | 3 × 250 % to each enemy within 5 m, proc 1.0 | 10 s | **4139** · 497 (the area spreads Fixation: the pack bursts together) |
| Q | **Bullet Dance** (equipment) | Spins and fires at every enemy within 25 m at once, 30 % chance to evade attacks, slowed to 60 %. Blade-ring and spark effects. | 113 % per shot to each enemy, ~12 shots/s for 5 s (≈6800 % per enemy) | 120 s | 1567 · 188 (not balanced; one use: 47008 %, 8 enemies) |

## Abrams: the unstoppable detective (main moves at once: **2460 %/s**, one v0.5.1 run; match Loader 2951 (library): inside ±500)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Infernal Resilience** | Heals back part of every hit he takes; debuffs on him are shorter. | 10 % of damage taken over 10 s; debuffs −20 % | – | (no damage) |
| M1 | **Shotgun** | Nine pellets with spread, from the end of the gun. | 9 × 52 %, 1.6 shots/s, 9 shells, 1.5 s reload, proc 0.4 | – | **581** · 70 |
| M2 | **Siphon Life** | RoR2's Mired Urn tethers: drains the nearest enemies and heals him for all of it. | up to 3 enemies within 25 m, 450 %/s each, 8 s, heals 100 % of damage dealt | 15 s | **720** · 86 |
| Shift | **Shoulder Charge** | Charges along his aim (steerable), running like his sprint, faster, **rocketing forward** (v0.6.3): a yellow glow and sparks on his front, thin yellow trails off his shoulders and a streak behind, dust at his feet. Every enemy he runs into is hit, stunned and knocked aside. A wall stops him. | 1600 % per enemy, stun 1 s, knockback 22 m/s, 28 m/s for up to 3 s | 8 s | **1120** · 134 |
| R | **Haymaker** (Deadlock heavy melee) | Locks onto the enemy under the crosshair (≤ 20 m, even one in the air); no target, no punch, and R stays ready. A 0.2 s wind-up with a glowing fist, a rush straight at it at 45 m/s, then a heavy punch along that line with a hit-stop, Loader's impact and a strong shake; its shockwave hits every enemy in a cone behind the target and knocks them flying. | 2400 % each; cone 6 m long, 30° each side; knockback 30 m/s, proc 1.0 | 5 s | **1760** · 211 |
| Q | **Seismic Impact** (equipment) | Rises, hovers up to 3 s while a ground reticle follows the aim (≤ 40 m); **Left Click** slams there (or it slams by itself after 3 s). He can be hit while hovering. | 6800 % in 18 m, stun 1.6 s | 120 s | 2493 · 299 (not balanced; one use: 74800 % on 11 enemies) |

## Seven: the living storm (main moves at once: **3164 %/s**, one v0.5.1 run; match Artificer 4013 (library): **below** ±500, doc 05 §3.1)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Capacitor** | Gun hits, Lightning Ball shocks and Power Surge arcs build Static. At 10 stacks the target discharges, arcing to 2 nearby enemies, and everything hit is stunned. | 300 % to each, 12 m, stun 1 s; Static max 10, 5 s | – | in M1, M2, R |
| M1 | **Shock Rifle** | 3-round bursts from the end of the gun; hits build Static. | 3 × 60 %, burst every 0.26 s, 29 rounds, 1.4 s reload, proc 0.7 | – | **704** · 84 |
| M2 | **Lightning Ball** | A slow ball that flies through enemies and bounces off walls and floors, shocking everything near it with a clear lightning strike to each; every shock builds Static. | 60 % every 0.25 s to all within 10 m (240 %/s), 10.5 m/s, 5 s, proc 0.3 | 8 s (below ~8 s the balls overlap and Capacitor discharges chain: 5300+ in the bench) | **390** · 47 (it bounces on past the pack now) |
| Shift | **Static Charge** | Plants a charge on the aimed enemy or spot; after a countdown, a thunderbolt from the sky. | 450 % in 8 m, stun 0.9 s, 1.5 s fuse | 6 s | **765** · 92 |
| R | **Power Surge** (his Deadlock ability) | For 8 s he crackles with lightning: every Shock Rifle burst arcs RoR2 Tesla lightning (like the Unstable Tesla Coil) from the target to up to 4 more enemies within 10 m, each arc building Static. +40 % move speed. | 60 % per arc, proc 0.2 | 10 s | 0 alone (it powers the gun; counted in "all") |
| Q | **Thunderhead** (equipment) | Dark storm clouds 35 m over the spot he stands on, darkening it; lightning strikes enemies from the clouds; allies inside get RoR2's Warbanner. | up to 4 enemies every 0.4 s, 272 % each, 20 m, 10 s (6800 % per enemy); Warbanner +30 % attack and move speed | 120 s | 907 · 109 (not balanced; one use: 27200 %) |

## Infernus: the burning man (main moves at once: **1149 %/s**, one v0.6.3 run; match Engineer 1611 (library): **inside** ±500, just; doc 05 §3.3)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Afterburn** | Gun hits build Afterburn; at 12 stacks the target burns. Embers show the build-up; RoR2's Helfire flare and big flames show the burn. | burn 100 %/s for 3 s; build-up max 12, 4 s | – | in M1 and R |
| M1 | **Flame Gun** | Automatic, from the end of the gun, with a heavier shot sound (Warden's shot layered under his own, louder) and a flash and puff of smoke at the muzzle; hits build Afterburn. | **75 %** per bullet (v0.6.3, was 50), 9.5 shots/s, 27 rounds, 1.4 s reload, proc 0.5 | – | **473** · 57 |
| M2 | **Napalm** | Right Click readies the flask and shows where it will land (its flight path and a splash-sized circle); **Left Click throws it** (Right Click again puts it away, charge kept). Molotov's bottle, glowing and trailing fire, flies in a high arc (as far again ×3 since v0.6.1) and bursts in a ring of fire and leaves a burning puddle: damage, a slow, and the target takes more damage from him. | 300 % in 5 m, slow 28 % for 4 s, +16 % damage taken from Infernus for 8 s, 2 charges; thrown at 78 m/s | 5 s | **644** · 77 |
| Shift | **Flame Dash** | One tap: he runs at ×2.2 speed for **5 s** (v0.6.3; no holding), laying **pockets of fire** every 3.2 m that burn for 5 s. Enemies in a pocket build Afterburn (overlapping pockets stack; 12 build-ups ignite). Enemies he runs into are knocked aside and scorched. 50 % slow resistance while dashing. | pocket 3.2 m (×2 the old patches), +3 Afterburn every 0.5 s per pocket, no direct damage; shove 75 % + knockback 22 m/s | 13 s | **423** · 51 (one run; the Afterburn it ignites) |
| R | **Torment Pulse** (Deadlock item) | For 6 s, rings of fire race out of him every 0.5 s, burning everything within 10 m and building Afterburn. | 50 % per pulse, +2 Afterburn build-up, proc 0.5 | 12 s | **876** · 105 (burns included) |
| Q | **Concussive Combustion** (equipment) | Becomes a bomb, fire gathering on him; a yellow ring on the ground shows the blast's reach, with a ring sweeping inward each second as a countdown and a timer ticking along (on whoever holds the item); can move while it builds; the blast stuns and leaves burning ground. | 6800 % in 18 m after a 3 s fuse, stun 2 s, each kill heals 10 % of max health | 120 s | 2720 · 326 (not balanced; one use: 81600 % on 12 enemies) |


## Mo & Krill: the tunnel rats (main moves at once: **2425 %/s**, one v0.7.1e run (Burrow 14 s, punch knockback 10); match MUL-T 1450 (library): **above** (+975), kept by the creator, doc 05 §3.5)

Size (creator, 2026-10-19: "Abrams' height, about 2× wide"): Deadlock's model scaled evenly by `modelScale` 1.262 to Abrams' height
(≈2.2 m in RoR2, about 1.9 m wide); he moves with **MUL-T's capsule** (`capsuleFrom`), so he fits wherever MUL-T does; his hurtbox
follows the model (a bigger target). MUL-T's camera, 15 % further back and 1.2 m to the right; 1.6× interaction reach.
Stats: **180** +75 health (250 until v0.7.1c, then 200), 25 armor, 1 +0.2 regen, 11 +2.2 damage, **6.3 m/s** (90 % of Abrams' 7, v0.7.1c; acceleration 80 like the others since the play-check: he gets up to speed fast), 1 jump.

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Heavyweight** | Monsters can't knock Mo back, push or pull him (vanilla ImmuneToPhysicsForces). Krill rides on him and holds the gun. **Every kill he makes, with any move, gives +2 max health** (+20 for a boss), up to +200: max health only, a kill does not heal; back to base on every new stage (a new body). Only Tag Team kills until v0.7.1c; before the v0.7.1f fix every stat recalculation cut health back to the base max, so the extra room never held. Heavy footsteps and a thud and shake after a fall. | – | – | – |
| M1 | **Yellow Canary** | Krill fires 4 pellets in Deadlock's fixed pattern (a centre pellet and a triangle of three around it, ≈2.5° out), the same every shot. While Krill is away (Tag Team) Mo is in a **Frenzy** and **punches** instead: twice as fast, each punch throws the monster away from him (bosses a quarter as far), a red aura around him. No shots while burrowed or spinning. | 4 × 40 %, 4 shots/s, 20 shells, 2 s reload, proc 0.5, falloff from 20 m; Frenzy punch **600 %**, 2/s, 5 m, knockback **10 m/s** (20 until v0.7.1e); Frenzy ×2 speed | – | **443** · 49 |
| M2 | **Sand Blast** | Mo winds up for 0.4 s while the cone is drawn on the ground (filled sand colour with an orange outline, following the terrain, a bright line sweeping out), then a tall wall of sand races along it (5 m wide at Mo, ≈15 m at 20 m), hitting everything it passes and throwing it back. Every monster hit bursts with sand and is **coated in sand** (the whole model) while **Sanded**: their skills are locked (RoR2's DisableAllSkills) and they are slowed. Works while burrowed. | **400 %**, 20 m, 89 m/s, knockback **22 m/s** (not bosses); Sanded 2 s (1 s bosses), −30 % speed | 7 s | **333** · 37 |
| Shift | **Burrow** | Mo sinks into the ground (0.45 s), then travels under it (model hidden, a dirt mound follows him; he can still be hit): **three times as fast** (≈19 m/s) with +100 armor, each hit taken costs 1 m/s for 1.5 s. He bursts back up (the model rising out of the ground) at 8 s, on Shift or Jump, or off a ledge, in a spin: 3 hits within 8 m, knocking monsters up (not bosses) and slowing them. **Q underground** bursts straight into Dust Devil. | **8 s**, **×3 speed**, +100 armor; spin **3 × 500 %**, 8 m, slow 50 % 2 s, knock-up 12 m/s | **14 s** (from the start; 8 until v0.7.1e) | **1150** · 127 |
| R | **Tag Team** | Mo throws Krill at the enemy you aim at (40 m; no target, no throw and R stays ready). For 10 s Krill leaps to a new enemy every 0.5 s (nearest within 25 m he hasn't hit), stunning it and splashing around it, then leaps back; meanwhile Mo is in a Frenzy (M1). | **400 %** + **150 %** within 4 m (2 landings kill a level-1 Lemurian or Beetle), 1 s stun, **8 s**, 16 landings | **20 s** | **1407** · 155 (more punches land now they knock monsters only half as far) |
| Q | **Dust Devil** (equipment) | Spin as a sand tornado for **5 s**, walking at normal speed and **able to jump** (v0.7.1c); every enemy within 16 m is pulled in (bosses at ¼), everything within 8 m takes damage four times a second. Mo plays Deadlock's own `ability_tornado_idle` clip (arms out); **any other survivor holding it spins** (the whole model turns twice a second). | 20 × **450 %** = **9000 %**, 8 m, pull 14 m/s from 16 m (none inside 4 m), proc 0.5 | 120 s | **1905** · 210 (one use: 57150 % on 12 enemies) |

Not in the kit: Scorn (its heal would repeat Abrams' Siphon Life) and Deadlock's innate (two headshot heads, tunnel vents).

## Viscous: the goo (main moves at once: **1958 %/s**, mean of 3 v0.8.0f runs (1660, 1805, 2410); match REX 1738 (library): **inside** ±500 (+220), doc 05 §3.9)

Deadlock's model at its own size (≈1.84 m capsule); his **head is living goo inside a clear glass helmet** (`heroes.gooMaterials` / `glassMaterials`, v0.8.0b).
His voice lines play 20 % louder than the other heroes' (`heroes.voiceGain` 1.2, v0.8.0d).
Stats: 130 +39 health, 20 armor, 1 +0.2 regen, 12 +2.4 damage (15 +3 in v0.8.0c only, back to 12 in v0.8.0d), 7 m/s, **jump power 20** (15 until v0.8.0e: a third higher), 1 jump.

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Gooey** | Every goo hit stacks **Gooed** on enemies: −8 % move and attack speed per stack (bosses half). **Goo puddles** (Splatter, The Cube and Puddle Punch) last 3 s whole, then shrink to nothing over 5 s (8 s in all; 10 + 20 in v0.8.0d). **Each puddle holds 20 rounds: while he stands in it, it keeps his magazine topped up** (never above 20; a reload under way is called off) until its 20 are used up; nothing extra stays when he steps out (v0.8.0e; 0.8.0d gave a one-off +20 up to 40). A fresh puddle supplies again, so hopping from puddle to puddle means no reloads. He and allies move 30 % faster on puddles (Slick). No fall damage; his landing shake is a tenth of the others'. | Gooed max 5 stacks (−40 %), 3 s; each puddle supplies 20 rounds (`puddleAmmo`), magazine never above 20 | – | – |
| M1 | **Deep Sea Interpreter** | Fires blobs of goo that drop over distance and lose damage past 20 m. | **120 %**, 5/s, 20 rounds, 2 s reload, 100 m/s, gravity 22, falloff 20–45 m to 40 % | – | **412** · 49 |
| M2 | **Splatter** | Right Click aims, Left Click lobs a ball of goo that **flies through enemies and bosses and splashes on the floor** in a **big goo explosion** (v0.8.0d: a blooming dome, a crown of globs, shockwave rings). It leaves a puddle on the ground (it follows slopes) that adds 1 Gooed a second to enemies in it, makes allies Slick and gives him rounds (passive). No cooldown: each throw costs 5 rounds. | **150 %** in 10 m (200 % until v0.8.0e), 3 Gooed; puddle **6.4 m** (8 until v0.8.0f), **3 s + 5 s shrinking**, 1 Gooed per second | 5 rounds | **1408** · 169 (the bench's spot is sometimes inside a puddle, so it throws more) |
| Shift | **The Cube** | Sealed in a thick block of goo for 3 s: **invulnerable and frozen still**, sliding on with his momentum. Tar tethers drain the 4 nearest enemies within 13 m and heal him. Then it **bursts in the big goo explosion** and **leaves an 8 m puddle** like Splatter's (10 m in v0.8.0d–e) at his feet (v0.8.0d; he stands in it, so it tops his magazine up straight away). | drain **100 %/s** each × 3 s; burst **600 %** in 16 m (300 % until v0.8.0c), Gooed to 5, knockback 8 m/s; puddle 8 m; then Cube Rush +30 % speed 3 s | 15 s (from the burst) | **493** · 59 |
| R | **Puddle Punch** | Place a **giant goo fist** anywhere within 40 m (Left Click): the ground bubbles, then the fist punches up out of it. It **leaves a 6.4 m goo puddle** like Splatter's (v0.8.0f). Twice as wide on a goo puddle; he and allies standing on it are bounced 20 m up. | **800 %** in 6 m (400 % until v0.8.0b), 2 Gooed, knock-up 16 m/s (not bosses) | 10 s, 2 charges | **702** · 84 |
| Q | **Goo Ball** (equipment) | Become a huge ball of goo for **15 s**: steer on the ground and in the air, faster downhill, bounces off walls; **half the normal grip** (v0.8.0d, a little looser than 0.8.0c's 70 %). Smashing into enemies damages by speed, stuns, launches and maxes Gooed; landing after a jump slams. Q again ends it. Any survivor can roll it. | **375–1500 %** by speed (500–2000 % until v0.8.0d), 0.5 s stun, launch 25 m/s (bosses ×0.1); slam **1125 %** in 8 m (1500 %); top speed 20 m/s, +100 armor, double jump | 120 s | **1612** · 193 (mean of 3; 2062 before the 0.8.0d cut) |

## Rat King: the sewer sovereign (main moves at once: **3542 %/s**, mean of 3 v0.9.0b runs (3567, 3189, 3869); match Captain 3490 (library): **inside** ±500 (+52), doc 05 §3.11)

Deadlock's model at its own size (3.0 m in Deadlock, a 2.27 m capsule: a big hunched rat with a crown, a red cape and a long tail).
His **cape and tail swing as springs** on top of his Deadlock animations (`animations.springRoots`; doc 02): they trail when he runs,
lift and billow in jumps and falls, and drape over his ragdoll. His gun's **ammo belt shortens round by round** and fills along the
reload (Deadlock's 19 belt meshes, `heroes.splitMeshes`). The banner he carries in the leap is only shown during it (`propBones`).
Stats: Captain's (130 +39 health, 10 armor, 1 +0.2 regen, 12 +2.4 damage, 7 m/s), +20 health and +10 armor, jump power 15, **2 jumps**.

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **King's Leap** | Double jump (every Deadlock hero can air-jump; Rat King's own jump has a weaker air jump, 75 %); here the second jump climbs **30 % higher** (`heroes.airJumpMult` 1.3, from the spec) and only that jump turns one tucked forward spin, the cape whipping round (0.9.0b: not the first jump any more). | – | – | – |
| M1 | **Sovereign's Decree** | Belt-fed spreadshot: per shot one pellet goes exactly where you aim and one strays 1–3°. Full damage to 20 m, falling to 40 % at 50 m. | **2 × 80 %**, 5 shots/s, 24 rounds, 2 s reload, proc 0.7 | – | **547** · 66 |
| M2 | **Scrap Grenade** | Right Click readies it and shows the arc (from the gun to the point under the crosshair) and a ring the size of the first blast; **any click throws**. It blasts 3 times: two 8 m blasts, **hopping about 4 m toward the nearest enemy within 50 m** between them, then it sits 0.8 s (ticking, glowing, rattling) and goes off big. Scrap, sparks, a scorch mark and a shake at each blast; small monsters are nudged back. | **300 % / 300 % / 600 %** in 8 / 8 / 12 m; slow −20 % (−40 % on the last) for 4 s | 8 s | **1373** · 165 |
| Shift | **Royal Pestments** | Deadlock's rats pour out from under his cape and run in three lanes around him: a **10 m ring on the ground** that follows him (never into the air) for 6 s, biting every enemy inside (up to 4 m above the ground). Now and then a rat darts at an enemy. | **50 %** × 4 a second (proc 0.3), 24 rats (0.9.0b: was 25 %) | 12 s (from the cast) | **889** · 107 |
| R | **Rule, Ratannia!** | Raises the banner (0.25 s), **leaps up to 20 m** toward the crosshair (a little steering), and slams it down: rats burst out of the ground, the ground cracks, and Deadlock's banner and sewer rise around it for 8 s. Only enemies the banner can see are hit. | **1500 %** within 4 m; **150 %** and a **3 s stun** (1.5 s on bosses) within 14 m | 12 s | **1123** · 135 |
| Q | **Rat Swarm** (equipment) | **50 rats** pour out and stay at the carrier's feet, **with no time limit**, until they find an enemy within 40 m: each takes the enemy with the fewest rats on it, leaps the last 2 m, clings on and bites **until that enemy dies, then is gone** (**at most 5 per enemy, 10 on bosses**; fliers only up to 6 m). An enemy that runs off alive frees its rats again. Spare rats follow the carrier to the end of the stage (or the carrier's death); using it again tops them back up to 50. Any survivor can carry it. | **100 %** a bite each second per rat (proc 0.5): 500 % a second to one enemy (1000 % on a boss) until it dies | 120 s | **4618** · 554 (mean of 3; 2073 with the old 15 s limit) |

## Sounds (every hero)
Every sound is the hero's own Deadlock sound, read from the player's install (doc 01 §6.5) and played by our mixer
(doc 02, sound). The source of truth is `design/sounds.json`; this table mirrors it. **Start** plays when the move starts, **loop** while
it lasts, **hit** on each hit, **end** when it ends, **reload** on a reload; a **layer** plays under the start or hit to make it
heavier; **gain** scales the whole row. In co-op every player hears each sound once (doc 02 §7.7).

| Hero | Key | Move | Start | Loop | Hit | End | Reload | Layer | Gain |
|---|---|---|---|---|---|---|---|---|---|
| Haze | M1 | Twin SMGs | `Haze.Wpn.Fire.Main` | – | – | – | `Haze.Wpn.Reload.Start` | – | 1.0 |
| Haze | M2 | Sleep Dagger | `Haze.Finesse.Dagger.Cast` | – | `Haze.Finesse.Dagger.Projectile.Hit` | – | – | – | 1.0 |
| Haze | Shift | Smoke Bomb | `Haze.Smoke.Bomb.Cast` | – | – | `Haze.Smoke.Bomb.End` | – | – | 1.0 |
| Haze | R | Phantom Strike | `Mods.Armor.Phantom.Strike.Cast` | – | `Mods.Armor.Phantom.Strike.Hit` | – | – | – | 1.0 |
| Haze | Q | Bullet Dance | `Haze.BulletFlurry.Cast.Delay` | `Haze.BulletFlurry.Modifier.Lp` | `Haze.Wpn.Fire.Main` | `Haze.BulletFlurry.Modifier.End` | – | – | 1.0 |
| Abrams | M1 | Shotgun | `Abrams.Wpn.Fire.Main` | – | `Abrams.Wpn.Impact` | – | – | – | 1.0 |
| Abrams | M2 | Siphon Life | `Abrams.A1.SiphonLife.Cast` | `Abrams.A1.SiphonLife.Loop` | – | `Abrams.A1.SiphonLife.End` | – | – | 1.0 |
| Abrams | Shift | Shoulder Charge | `Abrams.Charge.Cast` | – | `Abrams.Charge.HitEnemy` | `Abrams.Charge.Wall.Impact` | – | – | 1.0 |
| Abrams | R | Haymaker | `Abrams.Melee.Swing.Charged` | – | `Ability.Melee.Impact.Heavy.NPC` | – | – | hit + `Abrams.Charge.Wall.Impact` | 1.3 |
| Abrams | Q | Seismic Impact | `Abrams.A4.Leap.Cast` | `Abrams.A4.Leap.Float.Lp` | `Abrams.A4.Leap.Descend` | `Abrams.A4.Leap.Impact` | – | – | 1.0 |
| Seven | M1 | Shock Rifle | `Seven.Wpn.Fire` | – | `Seven.Wpn.Impact` | – | `Seven.Wpn.Reload.Start` | – | 1.0 |
| Seven | M2 | Lightning Ball | `Gigawatt.LightningBall.Cast` | `Gigawatt.LightningBall.Proj.Lp` | `Gigawatt.LightningBall.Damage` | `Gigawatt.LightningBall.Proj.Explo` | – | – | 1.0 |
| Seven | Shift | Static Charge | `Gigawatt.StaticCharge.Cast` | `Gigawatt.StaticCharge.Lp` | `Gigawatt.StaticCharge.Proc` | – | – | – | 1.0 |
| Seven | R | Power Surge | `Gigawatt.PowerSurge.Cast` | `Gigawatt.PowerSurge.Lp` | `Gigawatt.PowerSurge.ChainMod.Proc` | `Gigawatt.PowerSurge.End` | – | – | 1.0 |
| Seven | Q | Thunderhead | `Gigawatt.StormCloud.Cast` | `Gigawatt.StormCloud.Lp` | `Gigawatt.StormCloud.Bolt.Detonate` | `Gigawatt.StormCloud.End` | – | – | 1.0 |
| Infernus | M1 | Flame Gun | `Infernus.Wpn.Fire` | – | `Infernus.Wpn.Impact` | – | `Infernus.Wpn.Reload.Start` | start + `Warden.Wpn.Fire` | 1.2 |
| Infernus | M2 | Napalm | `Inferno.Incend.Cast` | – | `Inferno.Incend.Hit` | – | – | – | 1.0 |
| Infernus | Shift | Flame Dash | `Inferno.FlameDash.Modifier.Start` | `Inferno.FlameDash.Modifier.Lp` | `Infernus.FlameDash.Hit` | `Inferno.FlameDash.Modifier.Stop` | – | – | 1.0 |
| Infernus | R | Torment Pulse | `Mods.Tech.TormentPulse.Proc` | – | `Mods.Tech.TormentPulse.Proc` | – | – | – | 1.0 |
| Infernus | Q | Concussive Combustion | `Inferno.FireBomb.Cast` | – | – | `Inferno.FireBomb.Explode` | – | – | 1.0 |
| Mo & Krill | M1 | Yellow Canary | `MoKrill.Wpn.Fire` | – | – | – | `MoKrill.Wpn.Reload.Start` | – | 1.0 |
| Mo & Krill | M1 (Krill away) | Punch | `MoKrill.Melee.Swing` | – | `MoKrill.Combo.Hit` | – | – | – | 1.0 |
| Mo & Krill | M2 | Sand Blast | `MoKrill.Sandblast.Cast` + layer `MoKrill.Sandblast.Impact` (at the throw, after the wind-up) | – | `MoKrill.Sandblast.Impact` | – | – | – | 1.0 |
| Mo & Krill | Shift | Burrow | `MoKrill.Burrow.Modifier.Start` | `MoKrill.Burrow.Travel.Lp` | `MoKrill.Burrow.Spin` (pop-out) | `MoKrill.Burrow.Spin.End` | – | – | 1.0 |
| Mo & Krill | Q | Dust Devil | `MoKrill.Burrow.Spin` | `MoKrill.Burrow.Spin.Lp` | – | `MoKrill.Burrow.Spin.End` | – | – | 1.0 |
| Mo & Krill | R | Tag Team | `MoKrill.Combo.Channel.Start` | – | `MoKrill.Combo.Hit` (each landing) | `MoKrill.Combo.Cast` (Krill back) | – | – | 1.0 |
| Mo & Krill | – | Footsteps / landing | `MoKrill.Footstep` (one per stride) | – | `MoKrill.JumpLand` (after a fall over 0.6 s) | – | – | – | 0.7 |
| Rat King | M1 | Sovereign's Decree | `Ratking.Wpn.Fire.Main` | – | `Abrams.Wpn.Impact` | – | `Ratking.Wpn.Reload.Start` | – | 1.0 |
| Rat King | M2 | Scrap Grenade (ready) | `Ratking.ScrapGrenade.Select` | `Ratking.ScrapGrenade.Select.Lp` (while held) | `Ratking.ScrapGrenade.Bounce` (each hop) | – | – | – | 1.0 |
| Rat King | M2 | Scrap Grenade | `Ratking.ScrapGrenade.Cast` (throw) | `Ratking.ScrapGrenade.Proj.Lp` | `Ratking.ScrapGrenade.Explode` (blasts 1–2) | `Ratking.ScrapGrenade.Explode.Big` (the last) | – | ticks `Ratking.ScrapGrenade.Timer` / `.Timer.Final` in the 0.8 s fuse | 1.0 |
| Rat King | Shift | Royal Pestments | `Ratking.Rats.Emote` | `Ratking.Nibble.Rat.Scurry.Lp` | `Ratking.Nibble.Damage.Tick` (bites, at most every 0.5 s) | `Ratking.Guards.Expire` | – | start + `Ratking.Guards.Cast` | 1.0 |
| Rat King | R | Rule, Ratannia! | `Ratking.Charge.Cast.Delay` (wind-up), `Ratking.Charge.Cast.Jump` at take-off | `Ratking.Charge.Travel.Lp` | `Ratking.Charge.Cast` (the plant) | `Ratking.Charge.AOE.Expire` | – | hit + `Ratking.Charge.Explosion` | 1.0 |
| Rat King | Q | Rat Swarm | `Ratking.Nibble.Cast` | `Ratking.Nibble.Rat.Scurry.Lp` | `Ratking.Nibble.Hit` | `Ratking.Nibble.DashOff` (rats go home) | – | start + `Ratking.Rats.Emote`; bites `Ratking.Nibble.Damage.Tick` (tick, at most 5 a second) | 1.0 |
| Rat King | – | Footsteps (`ratking_gun/step`; 0.9.0b: a tenth as loud) | `Ratking.Footstep` | – | – | – | – | – | 0.1 |
| Rat King | – | Landing thud (`ratking_gun/land`, its own row so it stays loud) | – | – | `Ratking.Jumpland` | – | – | – | 1.0 |

Fuse ticks (v0.6.2, columns `tick` / `tickFinal`): Concussive Combustion plays `Ratking.ScrapGrenade.Timer` at 0 s and 1 s and
`Ratking.ScrapGrenade.Timer.Final` at 2 s (cut at the blast), in time with its ring closing in. No other move has a fuse.

Notes:
- Phantom Strike and Torment Pulse are Deadlock items, so they use the item sounds (`Mods.*`); Haymaker's hit is Deadlock's
  heavy melee impact with Abrams' charge-into-wall thud under it; the Flame Gun has Warden's shot under Infernus' own (v0.5.1).
- Volumes come from Deadlock (its `volume` is decibels, read correctly since extractor 1.2.0), times the row's gain, times
  RoR2's SFX volume.
- Concussive Combustion's cast is itself a 3 s build-up that rises to the bang; the ticks sit on top of it.
- Mo & Krill's extra sound sets are rows named `<skill>/<part>` (`mokrill_gun/punch`, `mokrill_gun/step`, v0.7.0).
- Dust Devil reuses Burrow's spin sounds (Deadlock has no tornado move of its own).
- Bullet Dance's hits replay Haze's gun shot; Seismic Impact plays its `hit` (the descend) as the dive starts and `end` on landing.

### Voice lines
The heroes also speak their own Deadlock lines (`design/voice.json`, `Runtime/Voice.cs`, since v0.5.0). The lines themselves
are never written out here or on the pages. When each hero speaks:
- **Run start and after each teleport** (select) and **when the teleporter finishes charging** (unselect).
- **Moves:** M2 and Shift 40 % of the time (at most every 8 s); R 40 % for Haze and Seven, 60 % (at most every 3 s) for
  Abrams' Haymaker and Infernus' Torment Pulse, which use a grunt instead of a line; Q every time. An ult used by another survivor speaks in its own hero's voice.
- **Kill streaks:** 4, 7 and 10 kills in quick succession (gaps under 1.5 s).
- **Idle:** after 2–5 min of silence.
- Mo & Krill: Krill does the talking (Mo is silent in Deadlock): select, unselect, Sand Blast (`use_power3`) and Burrow (`use_power2`) 40 %, Tag Team (R) 50 % at most every 20 s; Dust Devil (Q) is silent (his `use_power1` lines are about Scorn).
- Rat King: select, unselect, Scrap Grenade (`use_power1`) 30 %, Royal Pestments (`use_power3`) 40 %, Rule, Ratannia! (`use_power4`) 50 %, Rat Swarm (Q, `use_power2`) every time; idle `hunt` only (he has no `happy` lines).
- Only the local player hears their own hero's voice.

---

## The ult equipment (every hero)
- **What they are:** RoR2 equipment on Q (`design/equipment.json`) with a **120 s** cooldown. Fuel Cells and Gesture of the Drowned work as on any equipment.
- **Drops:** they follow the Preon Accumulator's rules: chests, Equipment Barrels, the Artifact of Enigma and Bottled Chaos.
- **Power:** at least 6800 % per enemy per use, the Preon scaled from 140 s to 120 s. **Not balanced** (creator's decision, v0.4.1): any survivor can carry them, so they may be as strong as we like and are left out of the comparison with Commando.
- **Starting:** every hero starts holding their own; there is no charge meter any more.
- **Other users:** any survivor, or an Equipment Drone, can fire any of them. On non-heroes they use plain muzzles and no Deadlock animation.
- **Sounds and voice travel with the item:** whoever fires it gets the ult's own Deadlock sounds (the Q rows of the Sounds table above) and its effects (Combustion's yellow ring too), and the **item's hero** speaks their ult line (Commando firing Combustion hears Infernus). Voice is heard by the player who fired it only; ult sounds are heard by everyone in co-op. Checked in game by `ult-any-survivor` (Commando fires Combustion, v0.6.1).
- **Combustion's fuse** (v0.6.2): a timer ticks once a second over the build-up, the last second faster, on whoever holds the item.
