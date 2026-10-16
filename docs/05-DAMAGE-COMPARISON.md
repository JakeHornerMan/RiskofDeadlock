# 05: Damage comparison (DPS benchmark, v0.3.0)

> **Analysis only.** Nothing in the heroes was changed for this. The suggestions in §5 are proposals for the creator to pick from.
> Measured 2026-10-16 with `bash tools/run_bench.sh 30` (raw output: `build/bench_results.txt`, not in git).

## 1. The question
The creator's hypothesis: **Commando, using every ability the moment it is ready while shooting constantly for 30 s
against a big pack of enemies on Monsoon, is the Risk of Rain 2 standard for DPS.** Every Deadlock hero is measured the
same way and compared with him. **A hero more than ~250 %/s above or below Commando needs changes** (§5).

## 2. Every variable of the test

| Variable | Value | Why / effect |
|---|---|---|
| Difficulty | **Monsoon** (`Run.selectedDifficulty = Hard`, set after the run starts) | the creator's choice; it scales enemies, not the player's damage |
| Stage | the first stage of a fresh run (random), one fixed flat spot 14 m from the drop pod | every survivor uses the same spot in the same run |
| Survivor level / items | **level 1, no items**, damage stat **12** for everyone (heroes' `baseDamage` = Commando's 12) | nothing died during passes, so no XP; checked at the end of every pass |
| Crit chance | base 1 % (no items) | crits barely matter; Fixation's crit stacks almost never happen |
| The pack | **12 enemies: 8 Lemurians + 4 Beetles**, a 3 × 4 grid 2.5 m apart, **10–15 m in front** | "lots of enemies in one place"; respawned fresh for every pass |
| Pack behaviour | AI **off** (they stand still, never fight back), **×101 health** (1000 BoostHp) | nothing dies, so every pass has the same 12 targets and nobody levels up; dead ones would be refilled every second |
| Survivor health | god mode | only damage dealt is measured; healing and defence (Infernal Resilience, Siphon heal, Smoke Bomb) have no value here |
| Duration | **30 s per pass**, 2.5 s gap between passes (projectiles, storms and DoTs from the last pass are not counted) | the creator's 30 s |
| Inputs | player input switched off; the bench **holds Left Click** and presses **Right Click, Shift and R every other physics frame** (~25 times a second), so each fires the moment it is ready | "use all abilities when ready and constantly shoot" |
| Aim | at the **nearest live enemy's** core, every physics frame | a perfect aimer; no misses from tracking |
| Movement | none; after a dash or leap ends, the survivor is put back on the spot | dashes still go wherever they go, then come back |
| R at the start | Commando's R (Suppressive Fire, cooldown) starts **ready**. The heroes' R is the **charge-meter ultimate**: **empty** at the start of the "all" pass (as in a real run), **full** at the start of the "R" pass | the meter is part of the design, so in "all" the ult only fires if the kit charges it |
| What counts | every hit the survivor lands (direct hits, projectiles, zones, DoTs), as `DamageInfo.damage`: after crits and damage-taken mods (Fixation, Napalm), **before** armor and the target's health | Lemurians and Beetles have 0 armor, and nothing dies, so this is the damage really dealt |
| Unit | **% of damage per second** (damage ÷ damage stat × 100 ÷ 30) | level-independent; 100 %/s = one 100 % hit per second; at level 1, 100 %/s = 12 damage/s |
| Passes | **all** (everything), then **M1 / M2 / Shift / R** each alone | "all" is the hypothesis; the single passes show what each ability adds |
| Runs | one run of each pass | expect roughly ±10 % noise (random stage, random crits, random ult targets) |

## 3. Results

### 3.1 The headline: everything, 30 s

| Survivor | %/s | damage/s at lvl 1 | vs Commando | Uses in 30 s (M1 / M2 / Shift / R) | Enemies hit |
|---|---|---|---|---|---|
| **Commando** (baseline) | **1058** | 127 | – | 150 / 10 / 8 / 4 | 11 |
| Haze | **395** | 47 | **−663** (0.37×) | held / 3 / 3 / **1** | 10 |
| Abrams | **2336** | 280 | **+1278** (2.2×) | held / 2 / 4 / **4** | 12 |
| Seven | **6850** | 822 | **+5792** (6.5×) | held / 5 / 5 / **14** | 12 |
| Infernus | **3209** | 385 | **+2151** (3.0×) | held / 7 / 4 / **3** | 12 |

**All four heroes are outside the ±250 band.** Haze is far below; the other three are far above.

### 3.2 Each ability alone (30 s, %/s)

| Survivor | M1 (gun) | M2 | Shift | R (one meter's worth, or on cooldown) | Sum of the four |
|---|---|---|---|---|---|
| **Commando** | Double Tap **603** | Phase Round **592** | Tactical Dive **0** | Suppressive Fire **80** (4 casts) | 1275 |
| Haze | Twin SMGs **298** | Sleep Dagger **35** | Smoke Bomb **0** | Bullet Dance **98** (1 cast, 2940 % total) | 431 |
| Abrams | Shotgun **511** | Siphon Life **400** | Shoulder Charge **257** | Seismic Impact **880** (1 cast, 26400 % total: 2200 % × 12) | 2048 |
| Seven | Shock Rifle **632** | Lightning Ball **1852** | Static Charge **585** | Thunderhead **367** (1 cast, 11000 % total) | 3436 |
| Infernus | Flame Gun **267** | Napalm **523** | Flame Dash **214** | Concussive Combustion **917** (1 cast, 27500 % total: 2500 % × 11) | 1921 |

Commando's "all" (1058) is 83 % of his sum (1275): abilities that take time (diving, the barrage) stop the gun. Use
this **×0.83** when estimating a kit's "all" from its parts.

### 3.3 What the numbers say

1. **The ult meter is the biggest driver.** It fills from damage dealt (`ultCharge` 4500–5500 %) and counts **every
   enemy hit**. Against 12 enemies an area kit fills it many times over: **Seven used R 14 times in 30 s**, Abrams 4,
   Infernus 3. Haze (one target at a time) filled it once and ended at 76 %. Commando's R is a 9 s cooldown, so he gets 4 cheap ones.
2. **Area damage against a pack.** Every hero ability that hits an area multiplies by up to 12 here. Commando's
   only area tool is Phase Round (it pierces the rows: 58 hits from 10 casts). Seismic Impact (×12), Combustion (×11),
   Lightning Ball (passes through everything, 518 hits) and Static discharges are all "pack" damage.
3. **Haze is single-target everywhere.** Her gun hits one enemy, Sleep Dagger is one 350 % hit on a 10 s cooldown
   (35 %/s), Smoke Bomb deals nothing (fine, like Commando's Dive), and Bullet Dance is ~2900 % spread over 5 s.
   She has nothing like Phase Round.
4. **Guns are close to each other.** Commando 603, Seven 632, Abrams 511 (shotgun spread hits several), Haze 298,
   Infernus 267. Haze's and Infernus' SMG-type guns are about half of Double Tap.

## 4. Caveats (read before acting on it)
- **The scenario favours area damage.** Twelve still, bunched enemies is the best case for Abrams, Seven and Infernus
  and the worst for Haze. Against a single boss the order would change; a single-target run of the same bench would
  complete the picture.
- **Utility value is not measured:** stuns (Sleep Dagger 4 s, Shoulder Charge, Static Charge, Combustion), slows,
  heals (Siphon Life, Infernal Resilience), invisibility (Smoke Bomb), the Warbanner. Commando's Dive and Haze's Smoke
  Bomb both score 0 but are not useless.
- **One run each, perfect aim, no movement, god mode.** Real play has misses, dodging and travel time.
- **Overlap:** in "all", channelled skills (Bullet Dance, Siphon Life is not, Flame Dash, Shoulder Charge, the slam)
  stop the gun for a while; the ×0.83 factor is Commando's and is only an estimate for the others.

## 5. Suggestions (no changes made)

Target: Commando's **1058 %/s ± 250** (808–1308) in this test. Estimates use §3.2 and the ×0.83 overlap factor, so they
are rough; a bench run after any change gives the real number.

### 5.0 For every hero: the ult meter (fix this first)
The meter rewards hitting many enemies, so area kits get several ults per fight. Options, any one of them:
- **A. Count one enemy per hit:** a hit that damages several enemies charges the meter as if it hit only the most-damaged one (area skills stop multiplying charge). *Recommended.*
- **B. Cap charge per second** (e.g. at most 200 % of charge per second), so the fastest possible ult is every ~25 s.
- **C. Charge over time plus kills** instead of damage (for example 1 % per second + `killCharge`).
- **D. Raise `ultCharge`** for area heroes (Seven 5500 → ~20000). Simple, but the hero then waits forever against single targets.

With A, B or C each hero gets about **one ult per 30 s**; the estimates below assume that.

### 5.1 Haze: 395 %/s, needs about **+500 to +650**
Estimated with one ult: (298 + 35 + 0 + 98) × 0.83 ≈ 360. Her problem is the missing second damage source.

| Ability | Now | Suggestion | Estimated gain |
|---|---|---|---|
| Twin SMGs (M1) | 45 % per bullet, 298 %/s | **55–60 %** per bullet (Double Tap is ~600 %/s); keep the spread | +70 to +100 |
| Fixation (passive) | burst at 20 stacks (200 %, one enemy) | burst every **10** stacks for **300 %**, **splashing 6 m** around the target (a pack-clearing tool like Phase Round) | +150 to +250 |
| Sleep Dagger (M2) | 350 %, 10 s, one enemy (35 %/s) | **6 s cooldown, 500 %, pierces** every enemy in a line (sleeps all of them); or Alt B's fan of three daggers with 2 charges | +150 to +250 |
| Smoke Bomb (Shift) | 0 damage | keep (utility, like Commando's Dive); optional: **the first shot out of smoke crits** (doc 04 Alt A) | +0 to +20 |
| Bullet Dance (R) | 49 % × 60 shots, one random enemy per shot | each shot **ricochets to a second enemy** (doubles it against packs), or 70 % per shot | +80 to +100 |

Picking SMG 58 %, the Fixation splash and the piercing dagger lands at about **900–1000 %/s**.

### 5.2 Abrams: 2336 %/s, needs about **−1000 to −1500**
Estimated with one ult: (511 + 400 + 257 + 880) × 0.83 ≈ **1700**. The meter fix alone removes most of the excess.

| Ability | Now | Suggestion | Estimated change |
|---|---|---|---|
| Ult meter | 4 slams / 30 s | §5.0 (one slam per ~30 s) | −600 to −700 |
| Seismic Impact (R) | 2200 % in 12 m (×12 enemies = 26400 %) | **1500 %** in **9 m** (Deadlock's radius) | −300 |
| Siphon Life (M2) | 250 %/s × 3 targets for 8 s, 15 s cooldown (400 %/s) | keep, or **200 %/s** | 0 to −80 |
| Shotgun (M1) | 511 %/s | keep (close to Double Tap) | 0 |
| Shoulder Charge (Shift) | 350 % per enemy, 257 %/s | keep (it is his escape) | 0 |

With the meter fix and the smaller slam: about **1150–1300 %/s**.

### 5.3 Seven: 6850 %/s, needs about **−5500 to −6000**
Estimated with one ult: (632 + 1852 + 585 + 367) × 0.83 ≈ **2850**. Two things: the meter (14 ults) and Lightning Ball.

| Ability | Now | Suggestion | Estimated change |
|---|---|---|---|
| Ult meter | 14 Thunderheads / 30 s, storms stacking | §5.0, plus **only one storm at a time** (a new one replaces the old) | about −4000 |
| Lightning Ball (M2) | flies through the pack: 60 % every 0.25 s to everything within 6 m, and every shock builds Static (1852 %/s alone, more than Commando's whole kit) | **35 %** per shock, **4 m** radius, and only the **gun** builds Static (or one Static per enemy per second from the ball) | about −1200 |
| Capacitor (passive) | 300 % to the target + 2 enemies at 10 Static, stun 1 s | **200 %** (stun stays) | −100 to −150 |
| Static Charge (Shift) | 450 % in 8 m, 7 s (585 %/s) | **9 s** cooldown, or 6 m radius | −100 to −150 |
| Shock Rifle (M1) | 632 %/s with discharges | keep | 0 |
| Thunderhead (R) | 110 % strikes, 4 enemies every 0.4 s, 10 s | keep (it is fine once per 30 s) | 0 |

With all of these: about **1200–1400 %/s**. Lightning Ball still being strong against packs is in character (Artificer-like).

### 5.4 Infernus: 3209 %/s, needs about **−1900 to −2400**
Estimated with one ult: (267 + 523 + 214 + 917) × 0.83 ≈ **1600**.

| Ability | Now | Suggestion | Estimated change |
|---|---|---|---|
| Ult meter | 3 Combustions / 30 s | §5.0 | −1500 |
| Concussive Combustion (R) | 2500 % in 18 m (×11 enemies) | **1600 %** in **12 m** (Deadlock's 12 m) | −300 |
| Napalm (M2) | 300 % in 5 m, 2 charges, 5 s (523 %/s) | keep, or **4 m** splash | 0 to −100 |
| Flame Gun (M1) | 40 % per bullet, 267 %/s | **50 %** per bullet (it is half of Double Tap; Afterburn stays) | +60 |
| Flame Dash (Shift) | 214 %/s from the trail | keep | 0 |

With the meter fix, a smaller Combustion and a slightly stronger gun: about **1100–1300 %/s**.

## 6. How to rerun
`bash tools/run_bench.sh [seconds]` builds nothing; build first (`dotnet build plugin -c Release`). It deploys the
plugin, starts the game with `%LOCALAPPDATA%\DeadlockOfRain\bench.json`, waits for `DPS bench finished`, prints
`build/bench_results.txt`, deletes the bench file and **closes the game** (`lab_restore.sh`). Code:
`plugin/Runtime/DpsBench.cs` (doc 02 §10.1). About 16 minutes for 5 survivors × 5 passes of 30 s.
