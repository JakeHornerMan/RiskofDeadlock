# 05: Damage comparison (DPS benchmark, v0.4.0)

> **Analysis only.** The suggestions in §5 are proposals for the creator to pick from; nothing was changed for them.
> Measured 2026-10-16 with `bash tools/run_bench.sh 30` on the v0.4.0 build (raw output: `build/bench_results.txt`, not in git).
> The v0.3.0 numbers (charge-meter ults on R) are in git history (commit `9cecc51`).

## 0. Retest needed (flags)
Any change to an ability after the last benchmark adds a row here **in the same commit** (CLAUDE.md standing rule).
After `bash tools/run_bench.sh`, update §3, doc 06's DPS columns and the version line, then clear the rows that run covered.
Visual-only changes (effects, animations, the pickup figure) don't need a row.

**Last benchmark:** 2026-10-16, v0.4.0 (ults on Q, new R moves). It covered every ability, including v0.3.0's Flame Dash flag.

| Survivor | Ability | Why a retest is needed | Commit | Status |
|---|---|---|---|---|
| Seven | Shock Rifle (M1 alone) | The M1-alone pass measured **62 %/s** from 34 hits: the gun stopped after about one magazine in that pass. The in-game test fires it normally (560 damage in 6 s), and in the "all" pass it fired 11 times. Treat 62 as a bench fault; v0.3.0 measured 632. | v0.4.0 | **retest** |

## 1. The question
The creator's hypothesis: **Commando, using every ability the moment it is ready while shooting constantly for 30 s
against a big pack of enemies on Monsoon, is the Risk of Rain 2 standard for DPS.** Every Deadlock hero is measured the
same way and compared with him. **A hero more than ~250 %/s above or below Commando needs changes** (§5).

Since v0.4.0 every survivor also has a **Q equipment**:
- each hero: their own Deadlock ult (120 s)
- Commando: the **Preon Accumulator** (140 s), the item the ults are scaled to

Each gets **one** use per pass, because it recharges far slower than 30 s. That use happens in "all" and in its own "Q" pass.

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
| Inputs | player input switched off; the bench **holds Left Click** and presses **Right Click, Shift, R and Q every other physics frame** (~25 times a second), so each fires the moment it is ready | "use all abilities when ready and constantly shoot" |
| Aim | at the **nearest live enemy's** core, every physics frame | a perfect aimer; no misses from tracking |
| Movement | none; after a dash, blink, lunge or leap ends, the survivor is put back on the spot | movement skills still go wherever they go, then come back |
| R | a normal cooldown skill for everyone (Commando's Suppressive Fire, the heroes' new R moves), ready at the start | |
| Q | the survivor's equipment **charged** at the start of "all" and "Q" (heroes: their own ult; Commando: Preon Accumulator), empty in the other passes | one use per pass |
| What counts | every hit the survivor lands (direct hits, projectiles, zones, orbs, DoTs), as `DamageInfo.damage`: after crits and damage-taken mods (Fixation, Napalm), **before** armor and the target's health | Lemurians and Beetles have 0 armor, and nothing dies, so this is the damage really dealt |
| Unit | **% of damage per second** (damage ÷ damage stat × 100 ÷ 30) | level-independent; 100 %/s = one 100 % hit per second; at level 1, 100 %/s = 12 damage/s |
| Passes | **all** (everything), then **M1 / M2 / Shift / R / Q** each alone | "all" is the hypothesis; the single passes show what each ability adds |
| Runs | one run of each pass | expect roughly ±10 % noise (random stage, random crits, random targets) |

## 3. Results

### 3.1 The headline: everything, 30 s (one Q use included)

| Survivor | %/s | damage/s at lvl 1 | vs Commando | Uses in 30 s (M2 / Shift / R / Q) | Enemies hit |
|---|---|---|---|---|---|
| **Commando** (baseline, with Preon) | **3008** | 361 | – | 10 / 8 / 4 / 1 | 12 |
| Haze | **611** | 73 | **−2397** (0.20×) | 3 / 3 / 3 casts / 1 | 12 |
| Abrams | **1684** | 202 | **−1324** (0.56×) | 2 / 4 / 5 / 1 | 12 |
| Seven | **5113** | 614 | **+2105** (1.7×) | 5 / 5 / 3 / 1 | 12 |
| Infernus | **3005** | 361 | **−3** (1.0×) | 7 / 4 / 3 / 1 | 12 |

**Only Infernus is inside the ±250 band**, and only because Torment Pulse and Combustion are both very strong against
a pack (§3.3). The Preon Accumulator alone is two thirds of Commando's number here.

### 3.2 Each ability alone (30 s, %/s)

| Survivor | M1 (gun) | M2 | Shift | R | Q (one use; total per use) |
|---|---|---|---|---|---|
| **Commando** | Double Tap **603** | Phase Round **854** | Tactical Dive **0** | Suppressive Fire **80** (4 casts) | Preon Accumulator **2013** (60400 % on 12 enemies) |
| Haze | Twin SMGs **278** | Sleep Dagger **35** | Smoke Bomb **0** | Phantom Strike **84** (3 casts) | Bullet Dance **215** (6440 %, 7 enemies) |
| Abrams | Shotgun **499** | Siphon Life **400** | Shoulder Charge **175** | Haymaker **330** (5–7 punches) | Seismic Impact **1587** (47600 % = 6800 % × 7) |
| Seven | Shock Rifle **62 ⚠** (bench fault, §0; v0.3.0: 632) | Lightning Ball **1168** | Static Charge **810** | Power Surge **0** alone (it needs the gun; see "all") | Thunderhead **907** (27200 %) |
| Infernus | Flame Gun **246** | Napalm **602** | Flame Dash **307** | Torment Pulse **1667** (3 casts) | Concussive Combustion **2720** (81600 % = 6800 % × 12) |

Commando's Phase Round measured 854 this time, against 592 in v0.3.0. It is the same skill on a different stage with a
different pack layout, so expect that much noise for piercing and area skills.

### 3.3 What the numbers say

1. **The meter problem is gone.** Each ult is used once per 30 s now (Q, 120 s), so v0.3.0's 14 Thunderheads in 30 s
   cannot happen. Seven dropped from 6850 to 5113.
2. **The ults are Preon-sized per enemy, but not per pack.** Each enemy hit takes about 6800 %, as designed. Per *use*
   against this pack:

   | Q | Per use | Why |
   |---|---|---|
   | Combustion | 81600 % | all 12 enemies inside |
   | Preon Accumulator | 60400 % | |
   | Seismic Impact | 47600 % | only 7 of the 12 enemies inside its 12 m |
   | Thunderhead | 27200 % | 4 strikes per tick |
   | Bullet Dance | **6440 %** | its total is shared by all the enemies, one random enemy per shot |

   Bullet Dance is a single-target ult and is about a tenth of Preon here.
3. **Torment Pulse is far too strong against packs: 1667 %/s alone**, twice Commando's best skill. It deals 120 % to
   every enemy within 10 m, twice a second, for 6 s every 12 s, and it ignites them.
4. **Haze is still single-target everywhere:** gun 278, dagger 35, Phantom Strike 84, Bullet Dance 215. Phantom
   Strike's 3 × 250 % on a 10 s cooldown is the right size for one target but adds little against a pack.
5. **Abrams fell below** now that Seismic Impact is used once (it was 4 slams in v0.3.0). His R (330) and Shift (175)
   are modest, and his slam only reaches 7 of the 12 enemies.
6. **Seven** is high mostly from Lightning Ball (1168) and Static Charge (810), both area skills that build Static.

## 4. Caveats (read before acting on it)
- **The scenario favours area damage.** Twelve still, bunched enemies is the best case for Infernus, Seven and the Preon,
  and the worst for Haze. Against a single boss the order would change; a single-target run would complete the picture.
- **One Q per 30 s pass** inflates every "all" number compared with a long fight: over 120 s the Q adds a quarter as
  much per second. Commando's 3008 is about **1000 without the Preon**.
- **Utility value is not measured:**
  - stuns (Sleep Dagger 4 s, Shoulder Charge, Static Charge, Combustion)
  - slows and knockbacks (Haymaker)
  - heals (Siphon Life, Infernal Resilience)
  - invisibility (Smoke Bomb)
  - the Warbanner and Power Surge's move speed
- **One run each, perfect aim, no movement, god mode.** Real play has misses, dodging and travel time.
- **Seven's M1-alone pass is broken** (§0); his "all" number used the gun normally.

## 5. Suggestions (no changes made)

Target: Commando's **3008 %/s ± 250** in this test (with one Preon), or about **1000 ± 250 without the Q** in a long fight.

### 5.1 Haze: 611 %/s, needs about **+2000** (or a single-target test)
| Ability | Now | Suggestion | Estimated gain |
|---|---|---|---|
| Bullet Dance (Q) | 113 % per shot, one random enemy per shot, 6800 % total | count the 6800 % **per enemy** like the other ults: each shot hits **every enemy within 25 m** at 113 %, or ricochets to 3 more enemies | +1000 to +1500 |
| Twin SMGs (M1) | 45 % per bullet, 278 %/s | **55–60 %** per bullet (Double Tap ~600 %/s) | +70 to +100 |
| Fixation (passive) | burst at 20 stacks (200 %, one enemy) | burst every **10** stacks for **300 %**, **splashing 6 m** | +150 to +250 |
| Sleep Dagger (M2) | 350 %, 10 s, one enemy | **6 s, 500 %, pierces** every enemy in a line | +150 to +250 |
| Phantom Strike (R) | 3 × 250 %, 10 s | the last cut **bursts 6 m** around the target for 300 % | +100 |

### 5.2 Abrams: 1684 %/s, needs about **+1000 to +1300**
| Ability | Now | Suggestion | Estimated gain |
|---|---|---|---|
| Seismic Impact (Q) | 6800 % in 12 m (7 of 12 enemies) | **18 m** radius (Preon's blast is 20 m) | +700 to +900 |
| Haymaker (R) | 900 %, 5 s, enemies in front | a **6 m shockwave cone** that hits everything in front | +150 to +250 |
| Shoulder Charge (Shift) | 350 % per enemy | **500 %** per enemy | +75 |
| Shotgun, Siphon Life | 499, 400 | keep | 0 |

### 5.3 Seven: 5113 %/s, needs about **−1800 to −2300**
| Ability | Now | Suggestion | Estimated change |
|---|---|---|---|
| Lightning Ball (M2) | 60 % every 0.25 s to everything within 6 m, every shock builds Static (1168 %/s) | **35 %** per shock, **4 m**, only the gun builds Static | about −700 |
| Static Charge (Shift) | 450 % in 8 m, 7 s (810 %/s) | **9 s** cooldown or 6 m radius | −200 to −300 |
| Capacitor (passive) | 300 % to the target + 2 at 10 Static | **200 %** | −200 to −300 |
| Power Surge (R) | 60 % arcs to 4 enemies per burst, 8 s, 14 s | **40 %**, 3 enemies | −200 to −400 |
| Thunderhead (Q) | 272 % × 4 enemies every 0.4 s | keep (Preon-sized) | 0 |
| Seven's gun alone | – | rerun the bench first (§0) | – |

### 5.4 Infernus: 3005 %/s, inside the band, but one skill is out of line
| Ability | Now | Suggestion | Estimated change |
|---|---|---|---|
| Torment Pulse (R) | 120 % to everything in 10 m, 2 pulses/s, 6 s every 12 s (1667 %/s alone) | **50 %** per pulse, or **6 m** radius, or 1 pulse per second | −800 to −1100 |
| Flame Gun (M1) | 40 % per bullet, 246 %/s | **50 %** per bullet, to give back part of that | +60 |
| Napalm, Flame Dash, Combustion | 602, 307, 2720 | keep | 0 |

If Torment Pulse is cut, Infernus lands at about **2000–2300**. To stay near Commando, pair the cut with the gun buff
or a slightly larger Napalm splash.

## 6. How to rerun
`bash tools/run_bench.sh [seconds]` builds nothing, so build first (`dotnet build plugin -c Release`). The script:
1. deploys the plugin
2. starts the game with `%LOCALAPPDATA%\DeadlockOfRain\bench.json`
3. waits for `DPS bench finished`
4. prints `build/bench_results.txt`
5. deletes the bench file and **closes the game** (`lab_restore.sh`)

The code is `plugin/Runtime/DpsBench.cs` (doc 02 §10.1). A run takes about 20 minutes: 5 survivors × 6 passes of 30 s.
