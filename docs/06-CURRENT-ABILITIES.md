# 06: Current abilities (every survivor, with DPS)

> **Living document.** It must match `design/*.json` and the code at all times. Any change to an ability (numbers,
> behaviour, passive, ult equipment) updates this page **and** adds a "retest needed" row to doc 05 §0, in the same
> commit (CLAUDE.md standing rule). After `bash tools/run_bench.sh`, the DPS columns here and in doc 05 are refreshed together.

**Pages (claude.ai Artifacts, shared "anyone with the link" since 2026-10-18; linked from the Melty listing and the README):** [Haze](https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p) ·
[Abrams](https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG) · [Seven](https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m) ·
[Infernus](https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF) · [Mo & Krill](https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa) (shared by the creator) · [Viscous](https://claude.ai/artifact/JRFqAM4uskGuYVJEKA84FC) (new 2026-10-20, pictures since 2026-10-20 v3; the creator still has to share it) · [Rat King](https://claude.ai/artifact/Kw4rTyRrvXTb2eEyT9oTYa) (new 2026-10-21, released in 0.10.0; the creator still has to share it) · [Billy](https://claude.ai/artifact/7m271DJLqzAvMZDrqU6QjL) (not released) · [Wraith](https://claude.ai/artifact/2Bdxqd6iUwp89iEEPELgBb) (new 2026-10-23, not released; the creator still has to share it) · [Mina](https://claude.ai/artifact/NWZWgZoK8DRusJpTVzbYoe) (new 2026-10-26, not released; the creator still has to share it) · [Balance vs Commando](https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3).
Built by `python tools/kit_pages.py` into `build/pages/`. Republish each page to its link whenever this doc or doc 05 changes.

**Version:** 1.1.0b (2026-10-26: **Mina's Nox Nostra** hangs still for 5 s and streams a blood-red cone of bats that follows the aim, 600 % every 0.5 s, R again cancels; **Mist Form** 8 s, 60 s cooldown, much bigger effects; not released). 1.0.1c (2026-10-26: Project Mind reaches **75 m** (was 60), Full Auto loads a **double magazine** (104 rounds); not released). 1.0.1a (2026-10-25: **Wraith reworked** from the creator's play: steered cards, 4 stored, The Rake +20 %, Telekinesis 100 s and thrown at full rise, Project Mind in the air; not released). 0.11.0a (2026-10-23: **Wraith added**, the ninth hero, matched to Bandit; not released). 0.10.0 (2026-10-22: Rat King playable on Melty, Billy the coming-soon tile; no ability changes; Billy in progress for 0.11.0). 0.9.1b (2026-10-22: Storm Cloud shocks the 3 nearest enemies, Thunderhead has its own icon, Seven's clips re-picked and every hero's upper-body casts laid on as Deadlock does; 0.9.1a, 2026-10-21: Seven redesign part 1: yellow visuals, Lightning Ball slows to half speed over enemies, Static Charge 225 %, R Storm Cloud replaces Power Surge, upper-body cast clips; not released). 0.9.0 (2026-10-21: Rat King added; 0.9.0d: every hero shows an ammo counter (rounds/magazine) above the M1 icon, and every Viscous puddle lands on the floor even when made in the air; 0.9.0e: a puddle's 20 rounds come before Viscous's magazine, shown as `20+5/20`, and are gone when he steps out; 0.9.0f: Goo Ball recharges in 100 s. **Released on Melty as 0.9.0** with six heroes; Rat King held back as a greyed "coming soon" tile). 0.8.0f (2026-10-20: Viscous added and reworked after the creator's play: puddles that supply ammo, Splatter 150 %, higher jump; not released). 0.7.4 live. 0.7.0 (2026-10-19: Mo & Krill added). 0.6.3 (2026-10-18; Flame Gun 75 %, Flame Dash a 5 s run laying Afterburn fire pockets. 0.6.2: Combustion fuse ticks. 0.6.1: Napalm aimed throw, Flame Dash 5 s / 13 s, Combustion range ring, ragdoll deaths).

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

## Seven: the living storm (main moves at once: **1759 %/s**, one v0.9.1b run; match Artificer 4013 (library): **below** ±500 (−2254), doc 05 §3.13)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Capacitor** | Gun hits, Lightning Ball shocks and Storm Cloud shocks build Static. At 10 stacks the target discharges, arcing to 2 nearby enemies, and everything hit is stunned. | 300 % to each, 12 m, stun 1 s; Static max 10, 5 s | – | in M1, M2, R |
| M1 | **Shock Rifle** | 3-round bursts from the end of the gun; the electric tracer line (Huntress' snipe tracer) recoloured Deadlock yellow in 0.9.1a (effects `tint`); hits build Static. | 3 × 60 %, burst every 0.26 s, 29 rounds, 1.4 s reload, proc 0.7 | – | **570** · 68 (660 in v0.9.1a) |
| M2 | **Lightning Ball** | A yellow ball inside a clear yellow sphere that shows its 10 m reach (0.9.1a). It flies through enemies and bounces off walls and floors, and **slows to half speed while an enemy is inside the sphere** (the creator's pick: 50 %, not Deadlock's 90 %), so it lingers on a pack; it shocks everything inside with a clear yellow strike to each, and every shock builds Static. | 60 % every 0.25 s to all within 10 m (240 %/s), 10.5 m/s (5.25 m/s over enemies, `moveSpeedMult` 0.5), 5 s, proc 0.3 | 8 s (below ~8 s the balls overlap and Capacitor discharges chain: 5300+ in the bench) | **868** · 104 (it lingers on the pack now; 390 in v0.5.1) |
| Shift | **Static Charge** | Plants a yellow charge on the aimed enemy or spot; after a countdown, a yellow thunderbolt from the sky. | 225 % in 8 m (450 before 0.9.1a), stun 0.9 s, 1.5 s fuse | 6 s | **420** · 50 (765 at 450 %) |
| R | **Storm Cloud** (his Deadlock ultimate, as an R since 0.9.1a; replaces Power Surge) | He rises 10 m and flies for 10 s inside a clear yellow 15 m sphere that shocks the 3 nearest enemies in it, a yellow bolt to each. He hovers in Deadlock's own Storm Cloud pose, arms down. **Jump** (Space) flies higher, **Sprint** (Shift; Utility works too) lower (at most 40 m above where he started); he drifts sideways at 80 % of his speed. An on-screen hint shows the keys and the time left. No gun or skills while it runs; no fall damage on the landing. | 100 % every 0.5 s to the 3 nearest within 15 m (200 %/s, 2000 % per enemy, at most 3 at a time), each shock builds Static, proc 0.5; climbs and sinks at 12 m/s | 24 s, from when he lands | **190** · 23 (one flight per 30 s pass; 710 when it hit every enemy in reach) |
| Q | **Thunderhead** (equipment; its own icon since 0.9.1b, Deadlock's Surge of Power picture, so it no longer shares Storm Cloud's) | Dark storm clouds 35 m over the spot he stands on, darkening it; lightning strikes enemies from the clouds; allies inside get RoR2's Warbanner. | up to 4 enemies every 0.4 s, 272 % each, 20 m, 10 s (6800 % per enemy); Warbanner +30 % attack and move speed | 120 s | 907 · 109 (not balanced; one use: 27200 %) |

## Infernus: the burning man (main moves at once: **1149 %/s**, one v0.6.3 run; match Engineer 1611 (library): **inside** ±500, just; doc 05 §3.3)

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Afterburn** | Gun hits build Afterburn; at 12 stacks the target burns. Embers show the build-up; RoR2's Helfire flare and big flames show the burn. | burn 100 %/s for 3 s; build-up max 12, 4 s | – | in M1 and R |
| M1 | **Flame Gun** | Automatic, from the end of the gun, with a heavier shot sound (Warden's shot layered under his own, louder) and a flash and puff of smoke at the muzzle; hits build Afterburn. | **75 %** per bullet (v0.6.3, was 50), 9.5 shots/s, 27 rounds, 1.4 s reload, proc 0.5 | – | **473** · 57 |
| M2 | **Napalm** | Right Click readies the flask and shows where it will land (its flight path and a splash-sized circle); **Left Click throws it** (Right Click again puts it away, charge kept). Molotov's bottle, glowing and trailing fire, flies in a high arc (as far again ×3 since v0.6.1) and bursts in a ring of fire and leaves a burning puddle: damage, a slow, and the target takes more damage from him. | 300 % in 5 m, slow 28 % for 4 s, +16 % damage taken from Infernus for 8 s, 2 charges; thrown at 78 m/s | 5 s | **644** · 77 |
| Shift | **Flame Dash** | One tap: he runs at ×2.2 speed for **5 s** (v0.6.3; no holding), laying **pockets of fire** every 3.2 m that burn for 5 s. Enemies in a pocket build Afterburn (overlapping pockets stack; 12 build-ups ignite). Enemies he runs into are knocked aside and scorched. 50 % slow resistance while dashing. | pocket 3.2 m (×2 the old patches), +3 Afterburn every 0.5 s per pocket, no direct damage; shove 75 % + knockback 22 m/s | 13 s | **423** · 51 (one run; the Afterburn it ignites) |
| R | **Torment Pulse** (Deadlock item) | For 6 s, rings of fire race out of him every 0.5 s, burning everything within 10 m and building Afterburn. | 50 % per pulse, +2 Afterburn build-up, proc 0.5 | 12 s | **876** · 105 (burns included) |
| Q | **Concussive Combustion** (equipment) | Becomes a bomb, fire gathering on him; a yellow ring on the ground shows the blast's reach, with a ring sweeping inward each second as a countdown and a timer ticking along (on whoever holds the item); can move while it builds; the blast stuns and leaves burning ground. In co-op the blast also lands when the fuse's end reaches the host a little early (0.9.1; a friend's bomb could fizzle before). | 6800 % in 18 m after a 3 s fuse, stun 2 s, each kill heals 10 % of max health | 120 s | 2720 · 326 (not balanced; one use: 81600 % on 12 enemies) |


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
| Passive | **Gooey** | Every goo hit stacks **Gooed** on enemies: −8 % move and attack speed per stack (bosses half). **Goo puddles** (Splatter, The Cube and Puddle Punch) last 3 s whole, then shrink to nothing over 5 s (8 s in all; 10 + 20 in v0.8.0d). **Each puddle holds 20 rounds: while he stands in it they come before his magazine** (the counter shows `20+5/20`), so his shots and Splatter spend the puddle's rounds first, even while the magazine is empty or reloading; when he steps out the rest is gone and the magazine is as he left it (v0.9.0e; 0.8.0e topped the magazine up instead, 0.8.0d gave a one-off +20 up to 40). A fresh puddle supplies again. He and allies move 30 % faster on puddles (Slick). No fall damage; his landing shake is a tenth of the others'. | Gooed max 5 stacks (−40 %), 3 s; each puddle supplies 20 rounds (`puddleAmmo`), magazine never above 20 | – | – |
| M1 | **Deep Sea Interpreter** | Fires blobs of goo that drop over distance and lose damage past 20 m. | **120 %**, 5/s, 20 rounds, 2 s reload, 100 m/s, gravity 22, falloff 20–45 m to 40 % | – | **412** · 49 |
| M2 | **Splatter** | Right Click aims, Left Click lobs a ball of goo that **flies through enemies and bosses and splashes on the floor** in a **big goo explosion** (v0.8.0d: a blooming dome, a crown of globs, shockwave rings). It leaves a puddle on the ground (it follows slopes) that adds 1 Gooed a second to enemies in it, makes allies Slick and gives him rounds (passive). No cooldown: each throw costs 5 rounds. | **150 %** in 10 m (200 % until v0.8.0e), 3 Gooed; puddle **6.4 m** (8 until v0.8.0f), **3 s + 5 s shrinking**, 1 Gooed per second | 5 rounds | **1408** · 169 (the bench's spot is sometimes inside a puddle, so it throws more) |
| Shift | **The Cube** | Sealed in a thick block of goo for 3 s: **invulnerable and frozen still**, sliding on with his momentum. Tar tethers drain the 4 nearest enemies within 13 m and heal him. Then it **bursts in the big goo explosion** and **leaves an 8 m puddle** like Splatter's (10 m in v0.8.0d–e) at his feet (v0.8.0d; he stands in it, so it tops his magazine up straight away). | drain **100 %/s** each × 3 s; burst **600 %** in 16 m (300 % until v0.8.0c), Gooed to 5, knockback 8 m/s; puddle 8 m; then Cube Rush +30 % speed 3 s | 15 s (from the burst) | **493** · 59 |
| R | **Puddle Punch** | Place a **giant goo fist** anywhere within 40 m (Left Click): the ground bubbles, then the fist punches up out of it. It **leaves a 6.4 m goo puddle** like Splatter's (v0.8.0f), always on the floor (v0.9.0d: placed from the air it no longer floats). Twice as wide on a goo puddle; he and allies standing on it are bounced 20 m up. | **800 %** in 6 m (400 % until v0.8.0b), 2 Gooed, knock-up 16 m/s (not bosses) | 10 s, 2 charges | **702** · 84 |
| Q | **Goo Ball** (equipment) | Become a huge ball of goo for **15 s**: steer on the ground and in the air, faster downhill, bounces off walls; **half the normal grip** (v0.8.0d, a little looser than 0.8.0c's 70 %). Smashing into enemies damages by speed, stuns, launches and maxes Gooed; landing after a jump slams. Q again ends it. Any survivor can roll it. | **375–1500 %** by speed (500–2000 % until v0.8.0d), 0.5 s stun, launch 25 m/s (bosses ×0.1); slam **1125 %** in 8 m (1500 %); top speed 20 m/s, +100 armor, double jump | **100 s** (120 until v0.9.0f) | **1612** · 193 (mean of 3; 2062 before the 0.8.0d cut) |

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

## Billy: the punk goat (main moves at once: **6393 %/s**, one v1.0.0a run; match Mercenary 5112 (library): **above** ±500 (+1281), doc 05 §3.16)

In development (not released; Melty shows a "coming soon" tile). Deadlock's model (spiked bat in one hand, Biquette in the other).
Stats: **175 +52.5 health** (2026-10-24: +25 %, was 140 +42), 20 armor, 1 +0.2 regen, 12 +2.4 damage, 7 m/s, **1 jump**.
Passive **G.O.A.T.**: a jump in the air is an 8 m dodge along the keys (or at the crosshair), goring everything passed for 300 %;
two per trip in the air, given back on landing or by Rising Ram. **Never takes fall damage** (2026-10-24, `noFallDamage`). Look: stone and punk pink.

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| M1 | **Biquette** | Full-auto machine gun, Deadlock's octagon spray (first bullet dead centre, then 8 points round the crosshair). Full damage to 14 m, 40 % at 32 m. | **83.3 %** a bullet (10 at level 1; 2026-10-24, was 50 %), 10/s, 30 rounds, 2 s reload | – | 494 |
| M2 | **Bat** | A 3-hit combo, one full swing (0.67 s) per press, sweeping everything in a **6 m, 120° arc** in front (2026-10-24: was 4 m; a flat pink sweep now shows the whole arc). **In the air**: double damage, a dodge onto the nearest enemy **on his screen** within **13 m** (was 10; 2026-10-24: never one he isn't looking towards), each hit keeps **the enemy hit up 1 s longer** and **every other floating enemy within 10 m of it 0.5 s** (new; up to 6 s, was 4) and **puts his own float back to 1 s** (never more; 2026-10-24, two quick hits used to stack). After Rising Ram he holds 1 s at the top, then drops unless a hit tops it up. Every hit reloads 35 % of Biquette. | **180 %**, 180 %, **360 %** overhead (×2 in the air) | 0.67 s | 2940 |
| Shift | **Bashdown** | Locks onto the nearest enemy within 5 m (in the air only one on his screen, 2026-10-24). **Ground**: 1000 %, knocked back, 2 s stun, a 300 % shockwave within 4 m. **Air** (2026-10-24: his float ends and he goes **straight down**, no dive at the enemy): it takes **2000 %**, everyone within 10 m takes 1000 % and is stunned; **chains run from Billy** to every enemy in the air he drags down (2026-10-24: was from the slammed enemy), 330 % each, and they **fall with him**, drawn in beside him, the chains breaking as he lands. | 1000 % (2000 % air), 300 % / 1000 % splash, chains 330 % | 4 s | 743 |
| R | **Rising Ram** | Charge 15 m; ramming an enemy deals 1200 % and chains (396 % each) haul every grounded enemy within 10 m, and him, into a **3 s float** (2026-10-24: was 2 s; the bat keeps it going, see M2). Cooldown halved on a hit. Ground only. | 1200 %, chains 396 % | 6 s | 941 |
| Q | **Chain Gang** (equipment) | Up to **20 chains** to the enemies in sight within 20 m, **one each**, hauled 6 m up and slammed for **1500 % a chain**; leftover chains go to **bosses, up to 5 each** (7500 %), slammed where they stand; the rest stunned 3 s (2026-10-24: was 6000 % per enemy, one chain each). | **1500 %** a chain, 20 chains (30,000 % in all) | 120 s | 10,500 on the ult bench (7 of 30 + queen) |

## Wraith: the card shark (main moves at once: **2426 %/s**, one v1.1.0c run (Card Trick 1413 alone with its splash, The Rake 684); match Bandit 1919 (library): **above** ±500 by 7 (+507), doc 05 §3.18; Telekinesis ult bench 93,000 %, 14 hit. Before: 937 (v1.0.1c))

New 2026-10-23 (not released); reworked 2026-10-25 (v1.0.1a) from the creator's play. Deadlock's model (her fedora and long coat; her face in Deadlock's own vertex colours since v1.0.1b; 2.37 m to the
top of the head, `heightScale` 0.983). **Her whole long coat, its two tails and her ponytail swing as springs** (`springRoots`
`coat_0`, `coattail_0_L/R`, `ponytail_0`; the coat flares out on every card throw); the coat's cloth bones ride her hips and spine (`clothParents`).
Stats: Bandit's exactly (110 +33 health, 0 armor, 1 +0.2 regen, 12 +2.4 damage, 7 m/s, 1 jump). No passive (Deadlock gives her none).
Her look is **purple smoke magic** (the creator's first ask) with **Deadlock's own Wraith textures** since v1.0.1a: card suits (heart, diamond,
spade, club) shed off every card and burst where it hits, her eye sigil turns under Project Mind's projection, the Full Auto ring, her eye
over each enemy Telekinesis lifts.

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| M1 | **The Rake** | Her tommy gun: fast automatic fire with **a lot of kick** (the chest kicks 6°, the camera climbs: `recoil` 6, `aimKick` 1.2), violet tracers and a curl of purple smoke off the barrel. | **72 %** a bullet (60 before v1.0.1a), ~10.6 shots/s, 52 rounds, **3 s reload** (2 s before v1.0.1b: the gun fired again while her 3 s reload clip still ran), proc 0.5; **every 5 bullets that hit take 1 s off the next card** | – | **528** · 63 |
| M2 | **Card Trick** | One spinning Deadlock card (Deadlock's whole card-trick throw, its in-air one off the ground) that leaves her hand **35° off to the side and curves in**, then **follows the crosshair while it flies** (turning 300°/s): it goes for the enemy under the crosshair, the one it was thrown at while the crosshair stays on it, or the point the crosshair is on, bending onto any enemy within 8 m on the way; **a miss swings round in an arc and comes back**. Each card draws one of four tricks, its glow, suit and hit in that colour: **Bleed** (red heart: 5 stacks of RoR2's Bleed for 4 s), **Crit** (gold diamond: a certain critical hit), **Stun** (pale-blue spade: 2 s) or **Burn** (orange club: 5 stacks of RoR2's Burn for 4 s). **The trick splashes** (since v1.1.0c): every other enemy within **7 m** of where the card hits (an enemy or a wall) gets the same trick at full strength, no extra damage (Crit: a gold mark, so the next hit it takes is a certain crit), with a burst in the trick's colour (a ring and Deadlock's ring, a storm of its suit, then a spray of blood, a gold starburst, lightning forking out or a ring of fire) and its suit popping over each enemy. **Every 5 Rake bullets that hit (any enemy) bring the next card 1 s sooner** (on the M2 description and the kit page). The stored cards **float fanned above her head** (every player sees them; the M2 icon shows the count). | **400 %** (proc 1), 70 m/s, 120 m of flight | 10 s, **4 cards** (Backup Magazine +1 each); 5 Rake hits = 1 s off | **133** · 16 |
| Shift | **Project Mind** | A **see-through purple projection of her** follows the crosshair up to **60 m** away (75 in v1.0.1c, back to 60 in v1.1.0c), standing on her eye sigil on the ground or **floating in the air** (aimed at the sky, or no ground within 2.5 m), with **cards drifting to it** from her; **any key** (M1, M2, Shift, R, jump) sends her: since v1.0.1b she **bursts into purple smoke and cards and flies there** in up to **1 s** (a full 60 m trip; shorter trips quicker, at least 0.4 s), hidden and **untouchable**, on a low arc, then reappears with a smoke column, a scatter of suits and a violet flash. **She can only go where she can see** (nothing solid between her eyes and the spot: no spot behind a rock) and where she fits; elsewhere the marker turns a dim red and a key gives the charge back. After 4 s she goes by herself. **No fall damage from the drop after it** (a spot in the air) until she next lands (since 2026-10-26); every other fall hurts her as usual. | – | 12 s | 0 |
| R | **Full Auto** | Plays Deadlock's Full Auto cast (fitted to the move, v1.0.1b; the 3 s reload clip before) with a ring of purple smoke: the magazine is loaded with **104 rounds, double The Rake's 52** (since v1.0.1c; it was just refilled before) and The Rake fires **50 % faster** with **20 % more kick** **until that magazine is empty**; it then reloads at the normal speed and the buff ends. Smoke and sparks curl off the gun meanwhile. | +50 % fire rate (buff `fullauto`) | 25 s | 0 alone (it lifts M1) |
| Q | **Telekinesis** (equipment) | A purple shockwave and smoke wave roll out **30 m**; **every enemy** in it rises 6 m in purple smoke (stunned, a violet beam from her to it, her eye over it) and **the moment it is up (0.6 s) is thrown 50 m wherever she aims then** (a low arc, a violet smoke trail; 1.25 s with a hang before v1.0.1a). Where it lands it takes the hit; **a wall on the way: ×1.5 and a 3 s stun**; **thrown off a cliff into a kill zone it dies as her kill** (her gold and XP). **Bosses** rise 1.5 m, take the hit and a **3 s stun** and are not thrown. Any survivor can carry it. | **6000 %** per enemy (9000 % into a wall) | **100 s** (120 before v1.0.1a) | **900** · 108 (ult bench: 60,000 % in one use, 10 hit) |

## Mina: the vampire socialite (main moves at once: **2644 %/s**, one v1.1.0c run (Nox Nostra 2500 alone, Rake 367, New Money 249); match Huntress 1921 (library): **above** ±500 (+723), doc 05 §3.19; Mist Form ult bench 115,200 %, all 30 hit. Before: 3098 (v1.1.0b), 1475 (v1.1.0a))

New 2026-10-26 (v1.1.0a, not released) from the creator's spec. Deadlock's model (`vampirebat`: her pistol in her **left** hand, her
umbrella in her right, 2.39 m to the top of the head, `heightScale` 0.99). **Her skirt, hair and the charm on her bag swing as springs**
(`springRoots` `skirt_0`, `skirt_a_0_L/R`, `skirt_b_0_L/R`, `hair_0`, `hair_0_L/R`, `purse_charm_0`); they lift and flutter while she floats.
The umbrella stays in her hand whatever the gun rig does (`holdBones umbrella>hand_R`). Stats: Huntress's exactly (90 +27 health, 0 armor,
1 +0.2 regen, 12 +2.4 damage, 7 m/s, 1 jump). Her look: **crimson and blood red, and Deadlock's own bats** (the bat model flapping through its
wing poses, extractor 1.9.0) in Sanguine Retreat, Nox Nostra and Mist Form; Deadlock's bite mark, marker and Rake cone textures.

| Key | Ability | What it does | Numbers | Cooldown | DPS alone (%/s · dmg/s) |
|---|---|---|---|---|---|
| Passive | **Love Bites** | Every New Money bullet that hits leaves a bite on that enemy (the **Love Bites** debuff, its count on the health bar; it fades 6 s after her last hit). Counted per enemy on her own game: **every 5** on one enemy give a **jump back** (2 at most, 1 per trip in the air; on the ground one is stored), **every 10** take **3 s off Rake**, **every 20** take **10 s off Nox Nostra** (never past ready). Each milestone pops Deadlock's bite mark over the enemy and red motes fly to her. No damage of its own. | 99 stacks, 6 s | – | – |
| Passive | **Umbrella** | **Hold Jump in the air** and the umbrella opens at once, after a jump or not, and stays open while Jump is held (since v1.1.0c; before: after 0.2 s, and only once she was falling): she **drifts down at 2.5 m/s** with **80 % steering** (her air control 0.8, was 1.5× the base) and **50 % faster** sideways and **no fall damage**, hanging from it (Deadlock's glide clip), skirt fluttering, a soft crimson glow under it. Let go or land and it closes. She shoots and Rakes while floating; a reload keeps her umbrella hand on the umbrella (v1.1.0c). | 2.5 m/s | – | – |
| M1 | **New Money** | Her pistol: one hitscan bullet at a time (as Deadlock's), a crimson muzzle flash and light; a crimson spark burst on an enemy, sparks, dust and a bullet mark on the world; a faint crimson tracer. | **90 %** a bullet (75 % in the spec), ~4.5 shots/s, 12 rounds, 1.5 s reload, full damage to 20 m, 40 % at 46 m, proc 1 | – | **252** · 30 |
| M2 | **Rake** | A **180° sweep** of the umbrella (Deadlock's Rake clip, in the air too) hitting everything within **10 m** in front; **heals 10 per enemy hit**; a monster (not a boss) left under 10 % health dies outright. A crimson crescent across the arc and **a crimson slash across every enemy it hits**; the area is Deadlock's Rake cone as a **half-disc at her own height** with a bright edge (on the floor when she stands, at her waist in the air; since v1.1.0c), sparks and red motes back to her. | **1000 %** (proc 1; 400 before v1.1.0c) | 8 s (−3 s per 10 Love Bites) | **36** · 4 (the bench's pack stands at the edge of its reach) |
| Shift | **Sanguine Retreat** | She **bursts into Deadlock's bats** and flies where she looks, up to **15 m** (walls stop it short), **untouchable** (monsters lose her), and reforms **2 m up**, ready to float; Deadlock's marker shows where. **Press again within 4 s** to do it once more; the cooldown starts when that window ends. Usable in Nox Nostra's flight (the flight carries on). | 0.65 s | 8 s (after the window) | 0 |
| R | **Nox Nostra** | Bats lift her **6 m** and she **hangs still for 5 s** under her open umbrella (no flying since v1.1.0b; on screen: **[R] Cancel** and a bar for the time left). A **blood-red stream of 70 furious bats** pours out of her in a **cone** down to the ground under the crosshair (up to 50 m; **it follows your aim**, the owner sees the 10 m circle with Deadlock's marker), round a crimson beam, sparks and red mist; the ground where they land glows and smokes, and pulses with a shockwave, flash and shake on every bite. **Everything inside the cone**, from her (1 m wide) to the ground (10 m wide), takes the hit every 0.5 s. **R again** stops it early; it ends with a slam of bats where it lands. Then New Money fires again and her umbrella opens. Shift still blinks, and the stream carries on from the new spot. | **600 %** every 0.5 s (v1.1.0b, was 150 % four times a second; 60 % in the spec), proc 0.3 | **15 s** (20 in the spec; −10 s per 20 Love Bites) | **2580** alone (v1.1.0b; 1438 before the stream) |
| Q | **Mist Form** (equipment; Deadlock's Ethereal Shift) | **Any survivor** turns into a **flock of furious bats for 8 s** (v1.1.0b, was 5; blood red, glowing, beating fast, lunging at enemies, a looser outer swirl, red mist, embers and two red streaks; a big survivor makes a big cloud): untouchable, flying at 14 m/s (**Space up, Shift down**: the Jump key climbs, the Utility key or the Sprint key sinks, since 2026-10-26; Shift did nothing before when Sprint was bound to another key), no moves; it bursts out with a blinding flash, two shockwaves and a hard shake. Every enemy the flock passes through takes **300 %**, again every 0.5 s, with a flash, a blood-red burst and bats clinging to it; it **ends whatever the user was doing** (Burrow, The Cube...). When the 8 s end (or Q again) the flock spirals in and blasts out 80 bats with three shockwaves and a smoke pillar: **3000 %** within **15 m** with a push, and the user reappears (no fall damage; Mina's umbrella opens). | 300 % per pass, 3000 % burst | **60 s** (v1.1.0b, was 120) | ult bench: **100,500 %** in one use, 29 hit (v1.1.0b, 8 s; 57,000 % at 5 s) |

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
| Seven | R | Storm Cloud | `Gigawatt.StormCloud.Cast` | `Gigawatt.StormCloud.Lp` | `Gigawatt.StormCloud.Bolt.Detonate` | `Gigawatt.StormCloud.End` | – | – | 1.0 |
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
| Wraith | M1 | The Rake | `Wraith.Wpn.Fire.Main` | – | – | – | `Wraith.Wpn.Reload.Start` | – | 1.0 |
| Wraith | M2 | Card Trick | `Wraith.CardTrick.Cast` | `Wraith.CardTrick.Proj.Lp` (the card's flight) | `Wraith.CardTrick.Hit` | `Wraith.CardTrick.Proj.Impact` (on the world) | – | start + `Wraith.CardTrick.Proc` | 1.0 |
| Wraith | Shift | Project Mind | `Wraith.ProjectMind.Select` (marker up) | – | `Wraith.ProjectMind.Arrive` | `Wraith.ProjectMind.Cast` (where she leaves) | `Wraith.ProjectMind.Deselect` (nowhere to land) | – | 1.0 |
| Wraith | R | Full Auto | `Wraith.RapidFire.Cast` | – | – | – | `Wraith.Wpn.Reload.End` (the magazine in) | start + `Wraith.Wpn.Reload.Start` | 1.0 |
| Wraith | Q | Telekinesis | `Wraith.Telekenisis.Cast.Delay` | `Wraith.Telekenisis.Proj.Lp` (while they hang) | `Wraith.Telekinesis.Hit` (each landing) | `Wraith.Telekenisis.Cast` (the throw) | – | start + `Wraith.Telekenisis.Select`; hit + `Wraith.Telekenisis.Slam` | 1.0 |
| Mina | M1 | New Money | `Vampirebat.Wpn.Fire.Main` | – | `Vampirebat.Wpn.Impact` (an enemy hit, her own game) | – | `Vampirebat.Wpn.Reload.Start` | – | 1.0 |
| Mina | – | Umbrella (`mina_gun/umbrella`) | `VampireBat.Umbrella.Open` | `VampireBat.Umbrella.Flight.Lp` | – | `VampireBat.Umbrella.Close` (+ `.Flight.End`) | – | start + `VampireBat.Umbrella.Flight.Start` | 1.0 |
| Mina | – | Love Bites (`mina_gun/bites`) | – | – | `VampireBat.LoveBites.Modifier.Proc` (a milestone) | `VampireBat.LoveBites.Bats.Gained` (a jump back) | – | – | 1.0 |
| Mina | M2 | Rake | `VampireBat.Rake.Cast` | – | `VampireBat.Rake.Hit.Confirm` | `VampireBat.Rake.Kill.Confirm.Heal` (an execute) | – | start + `VampireBat.Umbrella.Swing` | 1.0 |
| Mina | Shift | Sanguine Retreat | `VampireBat.Blink.Begin` | `VampireBat.Blink.Travel.Lp` | – | `VampireBat.Blink.End` | `VampireBat.Blink.End.Final` (the recast window closes) | – | 1.0 |
| Mina | R | Nox Nostra | `VampireBat.Nox.Nostra.Cast.Delay` | `VampireBat.Nox.Nostra.Channel.Lp` (flight, and the swarm) | `VampireBat.Nox.Nostra.Impact` (bites) | `VampireBat.Nox.Nostra.Channel.End` | `VampireBat.Nox.Nostra.Whizby` (planted) | start + `VampireBat.Umbrella.Open` | 1.0 |
| Mina | Q | Mist Form | `VampireBat.Blink.Begin` | `VampireBat.Blink.Travel.Lp` | `VampireBat.Nox.Nostra.Impact` | `VampireBat.Blink.End.Final` (+ `Mods.Tech.Stasis.End`) | – | start + `Mods.Tech.Stasis.Active` (Ethereal Shift); hit + `VampireBat.LoveBites.Modifier.Proc` | 1.0 |
| Mina | – | Footsteps / landing (`mina_gun/step`) | `VampireBat.Footstep` | – | `VampireBat.JumpLand` | – | – | – | 0.4 |

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
- Rat King: select, unselect, Scrap Grenade (`use_power2`) 30 %, **Royal Pestments: he calls out his rats by name one after another for the whole 6 s** (`use_power1`, Rat Swarm's named-rat lines, shuffled, no repeats until all ~120 have played, the five without a name left out; creator 2026-10-21), Rule, Ratannia! (`use_power4`) 50 %, Rat Swarm (Q, `use_power1`, one named rat) every time (0.9.0 had power 1 and 2 swapped); idle `hunt` only (he has no `happy` lines).
- Wraith: select, unselect, Card Trick (`use_power1`) 30 % at most every 10 s, Project Mind (`use_power2`) 50 % every 8 s, Full Auto (`use_power3`) 70 % every 6 s, Telekinesis (`use_power4`) every time; streaks and `happy` (idle) as the others.
- Mina: select, unselect, Rake (`use_power1`) 30 % at most every 10 s, Sanguine Retreat (`use_power2`) 40 % every 10 s, Nox Nostra (`use_power4`) every time, Mist Form (Q, `use_power3`) every time; streaks and `happy` (idle) as the others.
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
