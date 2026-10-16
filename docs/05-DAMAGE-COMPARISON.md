# 05: Damage comparison (DPS benchmark, v0.4.1, each hero vs its RoR2 match)

> Measured 2026-10-16 with `bash tools/run_bench.sh 30` on the v0.4.1 build (raw output: `build/bench_results.txt`, not in git).
> The v0.4.0 numbers (one Q use inside every "all" pass, ±250 band) are in git history (commit `a333644`).
> §5 lists suggestions for the two heroes outside the band; nothing in §5 has been applied.

## 0. Retest needed (flags)
Any change to an ability after the last benchmark adds a row here **in the same commit** (CLAUDE.md standing rule).
After `bash tools/run_bench.sh`, update §3, doc 06's DPS columns and the version line, then clear the rows that run covered.
Visual-only changes (effects, animations, the pickup figure) don't need a row.

**Last benchmark:** 2026-10-16, v0.4.1 with the RoR2 matches (§3.0). It covered every ability, including v0.4.0's Seven gun flag (it was a real
bug, fixed: doc 02 §7, MODLOG gotcha 11).

| Survivor | Ability | Why a retest is needed | Commit | Status |
|---|---|---|---|---|
| – | – | nothing open | – | – |

## 1. The question
The creator's hypothesis: **Commando, using every ability the moment it is ready while shooting constantly for 30 s
against a big pack of enemies on Monsoon, is the Risk of Rain 2 standard for DPS.** Every Deadlock hero is measured the
same way and compared with him.

Since v0.4.1 (creator's decisions):
- **Balance compares the main moves only:** M1, M2, Shift and R. Nobody, Commando included, holds equipment in those passes.
- **Balanced = Commando ±20 %.**
- **The Q ults are not balanced.** They are pickups any survivor can carry, so they "can be overpowered as we like". A
  separate "equipment" pass fires each one once (Commando: the Preon Accumulator) for a just-for-fun table (§3.3).
  Preflight only keeps a floor: at least 6000 % per enemy, Preon level.

## 2. Every variable of the test

| Variable | Value | Why / effect |
|---|---|---|
| Difficulty | **Monsoon** (`Run.selectedDifficulty = Hard`, set after the run starts) | the creator's choice; it scales enemies, not the player's damage |
| Stage | the first stage of a fresh run (random), one fixed flat spot 14 m from the drop pod | every survivor uses the same spot in the same run |
| Survivor level / items | **level 1, no items**, damage stat **12** for everyone (heroes' `baseDamage` = Commando's 12) | nothing died during passes, so no XP; checked at the end of every pass |
| Crit chance | base 1 % (no items) | crits barely matter |
| The pack | **12 enemies: 8 Lemurians + 4 Beetles**, a 3 × 4 grid 2.5 m apart, **10–15 m in front** | "lots of enemies in one place"; respawned fresh for every pass |
| Pack behaviour | AI **off** (they stand still, never fight back), **×101 health** (1000 BoostHp) | nothing dies, so every pass has the same 12 targets and nobody levels up; dead ones would be refilled every second |
| Survivor health | god mode | only damage dealt is measured; healing and defence (Infernal Resilience, Siphon heal, Smoke Bomb) have no value here |
| Duration | **30 s per pass**, 2.5 s gap between passes (projectiles, storms and DoTs from the last pass are not counted) | the creator's 30 s |
| Inputs | player input switched off; the bench **holds Left Click** and presses **Right Click, Shift and R every other physics frame** (~25 times a second), so each fires the moment it is ready | "use all abilities when ready and constantly shoot" |
| Aim | at the **nearest live enemy's** core, every physics frame | a perfect aimer; no misses from tracking |
| Movement | none; after a dash, blink, lunge or leap ends, the survivor is put back on the spot | movement skills still go wherever they go, then come back |
| Equipment | **none** in "all" and the slot passes; in the "equipment" pass the survivor's ult is charged once and Q pressed (heroes: their own ult; Commando: Preon Accumulator) | balance leaves the ults out |
| What counts | every hit the survivor lands (direct hits, projectiles, zones, orbs, DoTs), as `DamageInfo.damage`: after crits and damage-taken mods (Fixation, Napalm), **before** armor and the target's health | Lemurians and Beetles have 0 armor, and nothing dies, so this is the damage really dealt |
| Unit | **% of damage per second** (damage ÷ damage stat × 100 ÷ 30) | level-independent; 100 %/s = one 100 % hit per second; at level 1, 100 %/s = 12 damage/s |
| Passes | **all** (main moves together), then **M1 / M2 / Shift / R** each alone, then **equipment** | "all" is the balance number |
| Runs | one run of each pass | expect roughly ±10 % noise (random stage, random crits, random targets) |

`tools/run_bench.sh [seconds] [hero]` with a hero id runs only Commando and that hero (about 7 minutes), used to tune Seven.

## 3. Results

### 3.0 Each hero vs its own RoR2 match (the current balance rule)
Creator's decision (2026-10-16): each hero is compared with the Risk of Rain 2 survivor its stats are built on
(`design/stats.json` → `analog`), measured the same way; balanced = within **±20 % of the match**. Second run of the day
(the matches were added to the bench; same build as §3.1), so the heroes' numbers differ from §3.1 by the usual noise.

| Hero | %/s | RoR2 match | Match %/s | Band | Verdict |
|---|---|---|---|---|---|
| Haze | **1478** | Commando | 1064 | 851–1277 | **above** (+414) |
| Abrams | **985** | Loader | 2747 | 2198–3296 | **below** (0.36×) |
| Seven | **1242** | Artificer | 2790 | 2232–3348 | **below** (0.45×) |
| Infernus | **1226** | Engineer | 1268 | 1014–1522 | **inside** |

Each move alone (%/s, hero vs match):

| Slot | Haze / Commando | Abrams / Loader | Seven / Artificer | Infernus / Engineer |
|---|---|---|---|---|
| M1 | 1020 / 603 | 508 / 1120 | 672 / 2342 | 317 / 1720 |
| M2 | 100 / 588 | 400 / 11 | 118 / 1073 | 538 / 50 |
| Shift | 0 / 0 | 350 / 2287 | 165 / 0 | 311 / 0 |
| R | 352 / 80 | 570 / 1800 | 0 (powers the gun) / 853 | 768 / 69 |

Notes:
- **Loader** is carried by Charged Gauntlet (2287) and M551 Pylon (1800): big area hits on a bunched pack. Grapple Fist hit
  once in its pass (11).
- **Artificer** is carried by Flame Bolt (2342: 4 charges of 220 % that ignite) and Nano-Bomb (1073).
- **Engineer**'s turrets (R) were placed twice but landed few hits in 30 s (69); his grenades carry him (1720). Turret damage
  counts as his. His damage stat is **14** (Infernus 12): %/s removes that, raw damage favours him by ~17 %.
- Stats per survivor come from the bench's `stats` lines (all four matches as in vanilla; the heroes as in `stats.json`).
- Suggestions (not applied, the creator decides): Haze: Fixation splash 6 → 4 m, or bursts every 15 stacks, or SMGs 50 %.
  Abrams: about 2.8× needed (e.g. Haymaker 900 → 1800 %, Shoulder Charge 500 → 1000 %) or a different yardstick.
  Seven: bring the 0.4.1 cooldowns (16 / 16 / 30 s, tuned for Commando) back toward 6 / 7 / 14 s and retune.
  Infernus: none.
- Q, one use each in this run: Haze 44635 %, Abrams 54400 % (8 enemies), Seven 27200 %, Infernus 47600 % (7 enemies),
  Preon 39200 % (10 enemies); per-use totals depend on how many of the pack are inside.

### 3.1 The headline: main moves together, 30 s (no equipment)
Band: **1004–1506 %/s** (Commando 1255 ± 20 %).

| Survivor | %/s | damage/s at lvl 1 | vs Commando | In the band? | Uses in 30 s (M2 / Shift / R) | v0.4.0 (with one Q) |
|---|---|---|---|---|---|---|
| **Commando** (baseline) | **1255** | 151 | – | – | 10 / 8 / 4 | 3008 |
| Haze | **1315** | 158 | +60 (1.05×) | **inside** | 5 / 3 / 3 casts | 611 |
| Abrams | **729** | 87 | −526 (0.58×) | **below** | 2 / 4 / 5 | 1684 |
| Seven | **1272** | 153 | +17 (1.01×) | **inside** | 2 / 2 / 1 | 5113 |
| Infernus | **920** | 110 | −335 (0.73×) | **below** (84 under) | 7 / 4 / 3 | 3005 |

### 3.2 Each ability alone (30 s, %/s)

| Survivor | M1 (gun) | M2 | Shift | R |
|---|---|---|---|---|
| **Commando** | Double Tap **603** | Phase Round **752** | Tactical Dive **0** | Suppressive Fire **80** (4 casts) |
| Haze | Twin SMGs **1448** (Fixation bursts included) | Sleep Dagger **100** | Smoke Bomb **0** | Phantom Strike **282** (3 casts) |
| Abrams | Shotgun **513** | Siphon Life **400** | Shoulder Charge **200** | Haymaker **630** (7 punches) |
| Seven | Shock Rifle **552** (v0.4.0: 62, the bug) | Lightning Ball **114** (16 s) | Static Charge **315** (16 s) | Power Surge **0** alone (it powers the gun; counted in "all") |
| Infernus | Flame Gun **317** | Napalm **644** | Flame Dash **257** | Torment Pulse **823** (3 casts, burns included) |

### 3.3 The Q ults: just for fun (not balanced)
One use each against the same pack ("equipment" pass):

| Q | Starts on | One use | Why |
|---|---|---|---|
| Seismic Impact | Abrams | **74800 %** | 6800 % × the 11 enemies inside 18 m (v0.4.0: 12 m, 7 enemies) |
| Concussive Combustion | Infernus | **74800 %** | 6800 % × 11 inside 18 m |
| Preon Accumulator | Commando (vanilla) | **58400 %** | the yardstick the ults were scaled to |
| Bullet Dance | Haze | **47008 %** | every shot now hits every enemy within 25 m (v0.4.0: 6440 %, one random enemy per shot); 8 of the pack were in range |
| Thunderhead | Seven | **27200 %** | 4 strikes per tick, so a pack of 12 shares it |

### 3.4 What the numbers say
1. **Haze is in**, carried by her gun: with Fixation bursting every 10 stacks and splashing 6 m, the SMGs deal 1448 alone
   against the bunched pack, the strongest single ability in the test. She is lower together (1315) because Phantom Strike
   and Sleep Dagger interrupt the shooting. Against one target the splash is worth nothing, so she will be weaker there.
2. **Seven is in** through cooldowns alone (§5.3). His fixed gun is now his main damage.
3. **Abrams is below, and lower together than his parts:** 513 + 400 + 200 + 630 alone, but 729 together. Each Shoulder
   Charge runs for up to 3 s and carries him 70+ m past the pack, and he doesn't shoot while charging or winding up a
   Haymaker. The bench puts him back on the spot only when the charge ends.
4. **Infernus is 84 under the band**, within the test's noise. Torment Pulse (823) is now in line with Napalm (644).

## 4. Caveats (read before acting on it)
- **The scenario favours area damage.** Twelve still, bunched enemies is the best case for Fixation splash, Napalm and
  Torment Pulse. Against a single boss the order would change; a single-target run would complete the picture.
- **Long dashes cost more here than in play.** In a real fight Shoulder Charge goes into the pack, not 70 m past it.
- **Utility value is not measured:**
  - stuns (Sleep Dagger 4 s, Shoulder Charge, Static Charge, Combustion)
  - slows and knockbacks (Haymaker)
  - heals (Siphon Life, Infernal Resilience)
  - invisibility (Smoke Bomb)
  - the Warbanner and Power Surge's move speed
- **One run each, perfect aim, no movement, god mode.** Real play has misses, dodging and travel time.
- **Noise:** Commando's Phase Round measured 752 here and 854 in the v0.4.0 run; Seven's Lightning Ball measured 156 and
  326 in two tuning runs at 12 s and 16 s. Read differences under ~10 % as equal.

## 5. Changes made in v0.4.1, and what is left

### 5.1 Haze: 611 → 1315 (inside)
| Ability | Before | Now |
|---|---|---|
| Twin SMGs (M1) | 45 % per bullet | **55 %** |
| Fixation (passive) | burst every 20 stacks, 200 %, one enemy | every **10** stacks, **300 %**, splashing every enemy within **6 m** |
| Sleep Dagger (M2) | 350 %, 10 s, first enemy only | **500 %, 6 s, pierces** every enemy in its line |
| Bullet Dance (Q) | one random enemy per shot | **every enemy** within 25 m per shot (≈6800 % each) |

### 5.2 Abrams: 1684 (with Q) → 729 (main moves; below by 526)
| Ability | Before | Now |
|---|---|---|
| Seismic Impact (Q) | 12 m | **18 m** |
| Haymaker (R) | 900 % to what is in front of the fist | plus a **shockwave cone**, 6 m long, 30° each side, 900 % to each |
| Shoulder Charge (Shift) | 350 % per enemy | **500 %** |

**Suggestions (not applied; the creator picks):**
| Option | Expected effect |
|---|---|
| Shoulder Charge 3 → **1.5 s**, or it ends on the first enemy hit | he spends less time away from the pack: about +200 to +300 |
| Shotgun 45 → **52 %** per pellet (~590 %/s by the sheet math, just inside preflight's 240–600 band) | about +70 |
| Siphon Life cooldown 15 → **12 s** | about +80 |
| Haymaker cooldown 5 → **4 s** | about +100 |

### 5.3 Seven: 5113 (with Q) → 1272 (inside)
The creator asked for **cooldown increases instead of damage cuts**, tuned with `run_bench.sh 30 seven`:

| Ability | v0.4.0 | Try 1 | **Final** |
|---|---|---|---|
| Lightning Ball (M2) | 6 s | 12 s | **16 s** |
| Static Charge (Shift) | 7 s | 12 s | **16 s** |
| Power Surge (R) | 14 s | 20 s | **30 s** |
| Seven "all" | – | 1822 (above) | **1272** |

Also the **Shock Rifle bug**: a magazine that ran dry mid-burst drove the burst counter to −1, so the gun never fired or
reloaded again (62 %/s alone in v0.4.0). Fixed; it measures 552 now.

### 5.4 Infernus: 3005 (with Q) → 920 (84 below the band)
| Ability | Before | Now |
|---|---|---|
| Torment Pulse (R) | 120 % per pulse (1667 %/s alone) | **50 %** (823 alone) |
| Flame Gun (M1) | 40 % per bullet | **50 %** |

**Suggestions (not applied):** Napalm splash 5 → **6 m** (about +100), or Torment Pulse 50 → **60 %** (about +100). Either
brings him inside; he is already within the test's noise.

## 6. How to rerun
`bash tools/run_bench.sh [seconds] [hero]` builds nothing, so build first (`dotnet build plugin -c Release`). The script:
1. deploys the plugin
2. starts the game with `%LOCALAPPDATA%\DeadlockOfRain\bench.json`
3. waits for `DPS bench finished`
4. prints `build/bench_results.txt`
5. deletes the bench file and **closes the game** (`lab_restore.sh`)

The code is `plugin/Runtime/DpsBench.cs` (doc 02 §10.1). A full run takes about 20 minutes (5 survivors × 6 passes of
30 s); with a hero id, about 7.
