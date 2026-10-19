# 05: Damage comparison (DPS benchmark, v0.4.2, each hero vs its RoR2 match)

> Measured 2026-10-16 with `bash tools/run_bench.sh 30` on the v0.4.1 build (raw output: `build/bench_results.txt`, not in git).
> The v0.4.0 numbers (one Q use inside every "all" pass, ±250 band) are in git history (commit `a333644`).
> §5 lists suggestions for the two heroes outside the band; nothing in §5 has been applied.

## 0. Retest needed (flags)
Any change to an ability after the last benchmark adds a row here **in the same commit** (CLAUDE.md standing rule).
After `bash tools/run_bench.sh`, update §3, doc 06's DPS columns and the version line, then clear the rows that run covered.
Visual-only changes (effects, animations, the pickup figure) don't need a row.

**Last benchmark:** 2026-10-16, v0.4.2 (full run after the Abrams/Seven tuning, §3.0). It covered every ability, including v0.4.0's Seven gun flag (it was a real
bug, fixed: doc 02 §7, MODLOG gotcha 11).

| Survivor | Ability | Why a retest is needed | Commit | Status |
|---|---|---|---|---|
| – | – | nothing open (v0.6.3 Flame Gun 75 % and Flame Dash pockets covered by the Infernus-only run of 2026-10-18, §3.3; Mo & Krill's first run 2026-10-19, §3.4) | – | – |

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

`tools/run_bench.sh [seconds] [heroes]` runs **our heroes only** by default (comma-separated ids, e.g. `haze,abrams`): Commando and every vanilla survivor are stored in `docs/dps-library.json` and are never re-measured (the creator, 2026-10-17: "we already collected this data, justrun tests on new characters and compare to our collected data"). `BASE=1` adds Commando and the matches, `ALL=1` every survivor; only when the bench method itself changes.

## 3. Results

### 3.0 Each hero vs its own RoR2 match (the current balance rule, v0.4.2)
Creator's decisions (2026-10-16):
- Each hero is compared with the Risk of Rain 2 survivor its stats are built on (`design/stats.json` → `analog`),
  measured the same way.
- Balanced = within **±500 %/s** of the match. It was ±20 % before; the creator: "ok let increase the threshold to 500 dps i
  think that is good enough".
- Because single runs are noisy (Loader 1,769–5,241, Artificer 1,526–4,507), the verdict compares the **means over every
  run of the current build**. Seven's target, "Artificer's average", was the creator's pick.
- `tools/kit_pages.py` `RUNS` holds the lists; add each new run, and reset a hero's list when its kit changes.

| Hero | Mean %/s (runs) | RoR2 match | Mean %/s (runs) | Gap | Verdict (±500) |
|---|---|---|---|---|---|
| Haze | **1271** (1315, 1478, 1056, 1236) | Commando | 1224 (7 runs: 1298–1064) | +47 | **inside** |
| Abrams | **2508** (1990, 3026) | Loader | 2973 (2747, 2135, 1769, 5241) | −465 | **inside** |
| Seven | **3999** (3661, 4337) | Artificer | 3605 (2790, 4507, 1526, 4028, 4294, 4482) | +394 | **inside** |
| Infernus | **1102** (920, 1226, 1161) | Engineer | 1488 (1268, 1555, 1641) | −386 | **inside** |

**v0.4.2 tuning** (creator's picks):
- **Abrams: damage only.**
  - Start values: Shotgun 52 % (the most preflight's primary band allows), Siphon 450 %/s, Shoulder Charge 1200 %,
    Haymaker 1800 %. Run 1: 1730 vs Loader 2135 (inside but low).
  - Final: Shoulder Charge **1600 %**, Haymaker **2400 %**. Runs: 1990 and 3026.
- **Seven: cooldowns back down plus a gun buff.** Shock Rifle 48 → 55 → **60 %**.

  | Lightning Ball / Static Charge / Power Surge | Seven "all" |
  |---|---|
  | 8 / 8 / 15 s | 1893 |
  | 6 / 7 / 14 s | **5652** |
  | 7 / 7.5 / 15 s | **5305** |
  | **8 / 6 / 10 s** (final) | 3661, 4337 |

  Below ~8 s Lightning Ball the balls (5 s each) overlap, every enemy stays charged and Capacitor discharges chain through
  the pack. **Keep Lightning Ball at 8 s or more.**
- **Haze and Infernus:** no changes (inside ±500).

### 3.1 v0.5.1 playtest feel pass (heroes-only run, 2026-10-17; reported, not retuned, by the creator's choice)
Matches are the stored library means (3 runs each). One run per hero; the 0.4.2 hero runs no longer count.

| Hero | %/s | M1 / M2 / Shift / R alone | RoR2 match (library) | Gap | Verdict (±500) |
|---|---|---|---|---|---|
| Haze | **6550** | 554 / 100 / 0 / **4139** (was 375) | Commando 1222 | +5328 | **far above** |
| Abrams | **2460** | 593 / 720 / 1120 / 1760 | Loader 2951 | −491 | inside (just) |
| Seven | **3164** | 554 / **390** (was 1652) / 705 / 0 | Artificer 4013 | −849 | **below** |
| Infernus | not re-run (no damage change) | – | Engineer 1611 | library 1512: −99 | inside |

- **Haze:** Phantom Strike's 5 m area gives every enemy around the target 5 Fixation per cut (15 per cast), so the whole
  pack reaches the 10-stack burst together and the 300 % bursts splash one another. Options: Fixation on the target only,
  or fewer stacks to the others.
- **Seven:** the bouncing ball no longer settles in the pack: it bounces off the floor and flies on past it (it used to
  stop at the first surface inside the pack and shock it for 5 s). Options: bounce only off walls (roll along the floor),
  or lose speed on each bounce.
- **Abrams:** the lock-on Haymaker does about what the lunge did (1760 vs 1680 %/s alone).

### 3.2 v0.6.1 Infernus (Infernus-only run, 2026-10-18; reported, not retuned)
Napalm is now aimed (Right Click readies it, Left Click throws; the bench throws by itself 0.8 s after readying, and its
mashed Right Clicks don't put it away), 45 → 78 m/s; Flame Dash 5 s held / 13 s cooldown (the bench taps it, so 0.8 s).

| Hero | %/s | M1 / M2 / Shift / R alone | RoR2 match (library) | Gap | Verdict (±500) |
|---|---|---|---|---|---|
| Infernus | **929** (was 1512) | 307 / **685** (was 588) / 145 (was 441) / 876 | Engineer 1611 | −682 | **below** |

- **Why "all" fell:** while he holds the flask the Weapon machine is busy, so the gun can't fire. The bench readies
  Napalm whenever a charge is back, so the gun fired 10 times in the pass (119 before). A player who throws at once loses
  less. Napalm alone rose (faster flight, 8 throws).
- **Flame Dash alone** fell with the longer cooldown (one run; this pass is noisy: 244–485 before).
- Options for the creator: a shorter auto-ready (the gun fires while aiming), or Flame Gun 50 → 60 % per bullet.

### 3.3 v0.6.3 Infernus (Infernus-only run, 2026-10-18)
Flame Gun 50 → 75 % per bullet; Flame Dash always runs 5 s and lays Afterburn fire pockets (no direct damage, the creator's pick).

| Hero | %/s | M1 / M2 / Shift / R alone | RoR2 match (library) | Gap | Verdict (±500) |
|---|---|---|---|---|---|
| Infernus | **1149** (was 929) | **473** (was 307) / 644 / **423** (was 145) / 929 | Engineer 1611 | −462 | **inside** (just) |

- Flame Dash's damage is now the Afterburn ignitions its pockets cause (12 enemies, 385 hits in 3 uses).
- One run each; this bench is noisy for single passes.

### 3.4 v0.7.0 Mo & Krill (Mo & Krill-only run, 2026-10-19)
New hero matched to MUL-T (creator's spec: "aiming for 1,450 %/s", balanced band 950–1,950). Library `VERSION` v0.7.0.

| Hero | %/s | M1 / M2 / Shift / R alone | RoR2 match (library) | Gap | Verdict (±500) |
|---|---|---|---|---|---|
| Mo & Krill | **1620** | 443 / 510 / 370 / 382 | MUL-T 1450 | +170 | **inside** |

- The spec estimated ~1,600 together: Dust Devil gathers the still pack around him, so the pop-out spin and each later tornado hit all 12.
- The gun is half MUL-T's nailgun alone (443 against 876); Sand Blast and the pull carry him. Levers if a later run misses the band
  (spec): above 1,950 Dust Devil 50 → 40 %, then Sand Blast 450 → 350 %; below 950 Yellow Canary 40 → 50 %.
- Q (Tag Team) one use: 81,000 % on the 12 (just for fun).

Each move alone in the last full run (%/s, hero vs match):

| Slot | Haze / Commando | Abrams / Loader | Seven / Artificer | Infernus / Engineer |
|---|---|---|---|---|
| M1 | 888 / 603 | 581 / 3221 | 704 / 3045 | 317 / 1983 |
| M2 | 117 / 654 | 720 / 11 | 904 / 1387 | 566 / 180 |
| Shift | 0 / 0 | 1120 / 3075 | 765 / 90 | 413 / 0 |
| R | 372 / 80 | 1520 / 2333 | 0 (powers the gun) / 933 | 865 / 16 |

Bench caveats seen in this work:
- Loader's and Artificer's passes **kill pack members** despite ×101 health; the bench refills them, so up to 14
  "enemies hit" and higher numbers.
- Engineer's turrets land few hits in 30 s against a still pack.
- Engineer's damage stat is 14 (Infernus 12): %/s removes it.
- Q per use, last run: Abrams 88400 % (13 hit), Infernus 74800 %, Haze 52771 %, Preon 48400 %, Seven 27200 %.

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

## 6. The stored DPS library (`docs/dps-library.json`)
Creator's request (2026-10-17): "do a dps analysis on all characters so we never need to retest this again. keep the data
for memory and use in the future... obviously we will need to test dps again on the new characters but we can just
compare to our stored data". Answers: every survivor, 3 runs each, the pack test only.

- **What it holds:** for every playable survivor (base game and SotV DLC on this PC; the Seekers of the Storm and later
  survivors were skipped because their expansion is not enabled in the bench's run) and our heroes:
  - each run's %/s for "all" and each move alone, with the mean, min and max;
  - base stats and default skill names;
  - a `note` where the test can't show a survivor fairly.
- **Built by** `python tools/dps_library.py build/library/run1.txt run2.txt run3.txt`. One more run can be merged with
  `--add`.
- **Measured by** `ALL=1 bash tools/run_bench.sh 30` (every survivor, about 1 hour per run). The runs' files are not in
  git; the JSON is.
- **New hero:** run `bash tools/run_bench.sh 30 <hero>` (Commando, the hero and its match). Compare its "all" with the
  stored means. Add its entry with `--add` once its kit is final. Vanilla survivors never need re-measuring.
- **Changed hero:** re-measure it and replace its runs; its entry is tagged `measuredOn`.
- **Not comparable** (see their notes):
  - **Acrid:** Poison and Blight deal a share of max health, and the ×101-health pack inflates him enormously.
  - **Railgunner:** the bench doesn't scope.
  - **Void Fiend:** never corrupts in 30 s.
- **Bench fixes made for the library:**
  - **Clear view:** the arena now needs a flat spot with a clear view of all 12 pack spots. One stage left Commando
    with 5 hits.
  - **Captain:** M1 is released every 0.6 s, because his Vulcan Shotgun fires on release.
  - **Seekers of the Storm survivors:** a DLC survivor whose expansion is off is skipped.
- **Library means (3 runs each, 2026-10-17):**
  - Mercenary 5112, Seven 4621, Artificer 4013, Captain 3490, Loader 2951, Abrams 2402;
  - Huntress 1921, Bandit 1919, REX 1738, Engineer 1611, Haze 1556, Infernus 1512, MUL-T 1450, Commando 1222;
  - not comparable: Acrid 11206, Railgunner 592, Void Fiend 380.
- **Pooled verdicts** (every run of this build, §3.0 lists plus the library):
  - inside: Haze 1393 vs Commando 1224 (+169); Infernus 1307 vs Engineer 1549 (−242);
  - **outside:** Abrams 2444 vs Loader 2964 (**−520, just below**); Seven 4372 vs Artificer 3741 (**+631, above**).
  - Not retuned; the creator decides. Options: Abrams Haymaker 2400 → 2700 %; Seven's Power Surge 10 → 12 s or Static
    Charge 6 → 7 s (Lightning Ball must stay ≥ 8 s).
- The overview page's "Survivor DPS library" section shows it: a ranked chart with each survivor's spread, then a card
  per survivor with its four moves.
- **Balance verdicts** (§3.0) now pool every run of the current build: the earlier ones plus the library's.

## 7. How to rerun
`bash tools/run_bench.sh [seconds] [hero]` builds nothing, so build first (`dotnet build plugin -c Release`). The script:
1. deploys the plugin
2. starts the game with `%LOCALAPPDATA%\DeadlockOfRain\bench.json`
3. waits for `DPS bench finished`
4. prints `build/bench_results.txt`
5. deletes the bench file and **closes the game** (`lab_restore.sh`)

The code is `plugin/Runtime/DpsBench.cs` (doc 02 §10.1). A full run takes about 30 minutes (8 survivors × 6 passes of
30 s); with a hero id, about 10; `ALL=1` (every survivor) about an hour.
