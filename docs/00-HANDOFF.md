# 00: Handoff: start here (state of the project, for the next agent)

> **Read this first, then the newest entries of `docs/07-SESSION-LOG.md`.** Both are kept current by the standing rule in
> `CLAUDE.md` ("Record every session"). This file is the *current state*; doc 07 is the *history* (what the creator asked,
> what we asked back, what they answered, what was done). Depth lives in docs 01–06; this file links to them.
>
> **Last updated:** 2026-10-16, v0.4.1 + kit-page redesign in progress (§9).

---

## 0. Prompt for a new coding agent (paste-ready)

You are taking over **Deadlock of Rain** (`C:\Users\jakeh\Documents\GitHub\RiskofDeadlock`), a Melty mashup that puts
four Deadlock heroes (Haze, Abrams, Seven, Infernus) into Risk of Rain 2 as survivors. It reads Deadlock's assets from the
player's own install; nothing from either game ships. You do all the technical work (code, tools, build, test, docs);
you talk to the creator about the game only, in plain words, and **ask a lot of questions before big changes** (the
creator explicitly wants that: "please do deep research and ask lots of questions before implementation"). Use the
AskUserQuestion tool with a recommended option first.

Before touching anything:
1. Read `CLAUDE.md` (rules), this file, the newest `docs/07-SESSION-LOG.md` entries, then docs 03, 02, 06, 05 as needed.
   **The four heroes are finished (v0.6.3). The next job is a new hero from the creator's outline: follow
   `docs/08-NEW-HERO-PLAYBOOK.md`** (one round of questions, then build it all: sounds, voice, Unity effects, tests, pages).
2. The design sheets in `design/*.json` are the source of truth. Edit them, run `python tools/gen.py` (must say
   `PREFLIGHT CLEAN`), then `dotnet build plugin -c Release`.
3. Test with `bash tools/run_test.sh <hero>` (**only the heroes you changed**; the creator was angry at full runs for one-hero changes; must end `N/N heroes`), then make sure the game is closed
   (`bash tools/lab_restore.sh`). Benchmark with `bash tools/run_bench.sh` (closes the game itself).
4. **Never launch the game for the creator's own playtests**; leave the build installed and say it is ready.
5. Every change updates its docs in the same commit, and every session ends with a doc 07 entry and a refresh of this
   file's §1 and §9. Commit after each logical step; never push unless asked; never publish on Melty without asking.

---

## 1. Current state in one screen

| Item | State |
|---|---|
| Status | **Seven v0.9.1b on `main` (2026-10-22, not released)**: Storm Cloud shocks the 3 nearest, Thunderhead has its own icon, his clips re-picked (Deadlock's Storm Cloud hover, de-spun with `steadyClips`) and every hero's upper-body gestures laid on in model space as Deadlock does; `run_test.sh` all heroes pass; bench 1759 vs Artificer 4013 (far below, the creator's call); §9. Before that, part 1 (v0.9.1a, 2026-10-21): yellow Lightning Ball that slows to half speed over enemies inside a clear 10 m sphere, Static Charge 225 %, R Storm Cloud (fly 10 s in a 15 m shocking sphere, Space up / Shift down, on-screen keys) replacing Power Surge, yellow electric tracer, upper-body cast clips (the twisting rig); `run_test.sh seven` all pass; bench 2827 vs Artificer 4013 (below, the creator's call); §9. Before: **0.9.0 published on Melty 2026-10-21 (in safety review at the time of writing), untested at the creator's request**: six heroes (Viscous new), Rat King a greyed "coming soon" tile, Goo Ball 100 s, ammo counter (§9). Before, **v0.9.0e** (not tested in game): Viscous's puddle rounds come before his magazine (`20+5/20` on the counter) and are gone when he steps out; test and bench owed. Before, **v0.9.0d**: every Viscous puddle lies on the floor (Puddle Punch used in the air left it floating), and every hero has an ammo counter above the M1 icon; `run_test.sh` all seven pass. Before: **Rat King v0.9.0c on `main` (2026-10-21, not released)**: Royal Pestments chains his named-rat lines for 6 s (voice `chain`/`skipTakes`, extractor 1.6.2). 0.9.0b: after the creator's first play: footsteps a tenth, flip only on the air jump, banner in Deadlock's colours (extractor 1.6.1, DLM2 vertex colours, props only), Royal Pestments 50 %, Rat Swarm without a time limit (5 per enemy, 10 on bosses); bench 3542 vs Captain 3490 (+52). Before, 0.9.0: the seventh hero from the creator's spec, matched to Captain; cape and tail springs, Deadlock's rats as a crowd (ring, slam, 50-rat Rat Swarm), hopping Scrap Grenade, banner leap. `run_test.sh` all seven heroes every check pass; bench mean 2999 vs Captain 3490 (inside). Every clip now folds root_motion's turn into its children (doc 02 gotcha 23). Before: **Viscous v0.8.0f on `main` (2026-10-21, not released)**: Puddle Punch leaves a puddle too, every puddle 20 % smaller; bench 1958 vs REX 1738 (inside). Before: **v0.8.0e**: puddles last 3 s + 5 s and are a 20-round supply that tops the magazine up while he stands in them; Splatter 150 %; jump power 20; bench 2253 vs REX 1738 (15 above the band). Before: **v0.8.0d**: puddles last 10 s + 20 s shrinking and give +20 rounds per new puddle (no reloads if you hop), The Cube leaves a puddle, bigger goo explosions, base damage back to 12, Goo Ball 75 % and looser, voice +20 %; bench 2422 vs REX 1738 (above, +684; Splatter 150 % is the lever). Before: **v0.8.0c**: the creator's first-play feedback is in (Cube frozen in a goo block, giant goo fist and Puddle Punch 800 %, Goo Ball 15 s with snappier steering, Splatter through enemies onto the floor, goo head, a tenth of the landing shake), then base damage 15 and Cube burst 600 %. Bench 2236 vs REX 1738 (inside, just). His page has its pictures now; all seven pages republished (§9). Before: **v0.7.4 (2026-10-20, branch `hotfix-0.7.4`): the crash on quit after a run is found and fixed** (§9, doc 02 §12 gotcha 22): RoR2's console redirector + BepInEx's console writer loop at quit (it crashed with the mod off too); `Runtime/QuitSafety.cs` skips `Disengage` and patches Mono's stack-overflow recovery (from RoR2BepInExPack). Before the fix 11 of 13 test quits crashed (whatever part of the mod was switched off); with it, all 12 were clean. Viscous (v0.8.0) stays on `main`, not in 0.7.4. Before: **v0.7.3 (2026-10-19): the mod's sound mixer closes properly when the game quits**. Before: **v0.7.2 (2026-10-19): the fallback system and all five heroes for Melty** (§9): extractor 1.4.0 looks each model up in Deadlock's `heroes.vdata` (plus folder fallbacks) and prints per-hero progress; a hero that can't be read keeps a locked red "!" tile; every problem message shows *built with* vs *this PC* versions (`built-with.txt` from `package.py`); Melty loading summary; `BREAK=infernus run_test.sh haze` all pass. Before: **Mo & Krill balance pass v0.7.1c (2026-10-19, built, not tested, the creator is playing it)**: 6.3 m/s, Burrow ×3 speed, Sand Blast 750 % / 22 m/s throw with a new sand look (filled cone, sand wall, hit enemies coated in sand), Dust Devil 450 % ×32; then "a little bit too over powered": Tag Team 1000 % + 300 % splash, base health 200; then Tag Team 400 % + 150 %, 8 s / 20 s, Sand Blast follows slopes, every kill grows his health; then Sand Blast 400 %, Dust Devil 5 s: bench 2227 %/s vs MUL-T 1450, `run_test.sh mokrill` all pass; then Burrow 14 s, Frenzy punch knockback 10: 2425 %/s, all pass, Mo & Krill page and overview republished; then trophies fixed (health above base no longer cut) and shown on the page, 180 health, test all pass. Before: **Mo & Krill added (v0.7.0, 2026-10-19, not released) and reworked after the creator's play-check (v0.7.0b, same day)**: Tag Team on R (30 s), Dust Devil the Q ult (any carrier spins), Burrow 8 s / 3 × 500 % with sink and rise, Frenzy, Sand Blast wind-up and cone. Every test check passes; bench 3252 vs MUL-T 1450 (far above, the creator's numbers, reported). Co-op checked as a joined player (2026-10-19: every move, 88 relayed effects). Waiting on the creator's next play-check and the release. The first four heroes are done |
| Version | **0.7.4 published on Melty 2026-10-20** (release `9697f980-0b61-4ccc-a069-e709c13dc341`, upload `10278b70-…`, test `1cd1f52b-d83c-4190-9f00-aad6a95ceb49` passed 2/2 with footage reviews, `graduates: true`, no crash on either close; same recipe as 0.7.3, `setup.done.contains` `|1.4.0|544C18F87BF82B69`; listing Built-with line now v0.7.4, Hero pages kept; `report_bug_fix` recorded for `crash:crashed:ntdll.dll`; it was in Melty's safety review at the time of writing). Before: **0.7.3 published on Melty 2026-10-19 (release `b847f148-…`; Melty's safety review was checking it at the time of writing).** **0.7.2 LIVE on Melty 2026-10-19 (five heroes incl. Mo & Krill)**; a player's setup on it read 5/5 heroes on Deadlock 6795: release `043d5301-084a-447a-9c30-6924a7d00855`, test `3dd01dab-…` passed 2/2 with reviews (`graduates: true`; run 2 on Distant Roost), extractor 1.4.0, built with RoR2 Steam build 25921972 / Deadlock 6795; listing description rewritten for five heroes with the red "!" and "Built with" lines (§9). Before: **0.7.1, LIVE on Melty 2026-10-19 15:06 UTC (four heroes)** ( extractor **1.3.0**, VRF 20.1.7467, reads Deadlock build 6795; one hero that can't be read no longer stops the others; §9). Live: **0.6.3** (`plugin/Plugin.cs` then; Flame Gun 75 %, Flame Dash 5 s run with Afterburn fire pockets, Abrams charge vfx; 0.6.2: Combustion fuse ticks; 0.6.1: Napalm aimed throw, Combustion ring, ragdolls, Flame Dash 5 s/13 s), extractor **1.2.0** (sound volumes read as dB). **0.6.3 is LIVE on Melty as "Risk of Deadlock"** (https://melty.gg/m/risk-of-deadlock-2, 2026-10-18), character select screenshots; Infernus' select-screen text still says "Catalyst" (fix in the next release) |
| Daily watch | **Since 2026-10-21:** `tools/watch/daily.ps1`, run daily at 04:00 by Task Scheduler (doc 03 §5b). It checks for Deadlock/RoR2 updates on this PC and for Melty breaks on the live release; if it finds one, an unattended Claude fix run on branch `watch/<date>` tests, fixes and **publishes** once every check passes (the creator's permission). Reports go to `build/watch/<date>-report.md` and a notification. The fix run has not been tried end to end yet |
| Co-op | Up to 4 players (RoR2 lobby/Steam invites + Melty Join), everyone needs Deadlock; doc 02 §7.7. `run_coop_test.sh`: 4/4 heroes, all 25 skills as a joined player (2026-10-17). **Never tried with a real friend** (Steam lobby join, lag) |
| Installed build | **v0.7.1c dev build, five heroes, the Mo & Krill balance pass** (2026-10-19; the game was started for the creator at their request). Before: **v0.7.1 dev build with all five heroes incl. Mo & Krill** (2026-10-19, no heroes.txt, cache read from Deadlock 6795; BepInEx switched back on for Steam Play after `lab_restore`). Before: **v0.7.0b dev build with Mo & Krill** (2026-10-19, after the play-check feedback, for the next play-check; Melty's live release is still 0.6.3); BepInEx on, so pressing Play on Steam loads it (Melty may switch `winhttp.dll` off when it launches the game itself) |
| Heroes | Haze, Abrams, Seven, Infernus, **Mo & Krill** (v0.7.0); 4 skills each (M1 gun, M2, Shift, R) + their Deadlock ult as **Q equipment** |
| Test | **`run_test.sh <hero>` tests only the given heroes (creator's rule, CLAUDE.md).** v0.6.3: Infernus pass (in a 4-hero run before the filter existed), Abrams 1/1 pass (2026-10-18); the co-op test was skipped for the pockets (creator impatient with game runs); `run_coop_test.sh infernus` passes (one earlier run missed the joined Combustion, unexplained) |
| Balance rule | Main moves (no equipment) vs the hero's **own RoR2 match**, within **±500 %/s**, comparing **means over every bench run** of the current build (`RUNS` in `tools/kit_pages.py`; doc 05 §3.0) |
| Balance now | v0.5.1 heroes-only run vs the library (doc 05 §3.1): **Haze 6550 vs Commando 1222 (far above: Phantom Strike's area spreads Fixation)**, Abrams 2460 vs Loader 2951 (inside), **Seven 3164 vs Artificer 4013 (below: the bouncing ball leaves the pack)**, Infernus 1149 vs Engineer 1611 (inside, just; v0.6.3, doc 05 §3.3). Not retuned; the creator decides |
| Bench rule | **Bench our heroes only** (`run_bench.sh 30 <ids>`, the default); vanilla numbers come from `docs/dps-library.json` (creator, 2026-10-17; CLAUDE.md). Every base-game and Survivors of the Void survivor is stored (3 runs each, also listed in doc 06); the Seekers of the Storm three are not (expansion off) |
| Sounds | Every move's Deadlock sounds and the voice-line triggers are listed in doc 06 "Sounds" (mirrors `design/sounds.json` / `voice.json`) |
| Thunderstore | **0.7.0 rejected; the moderator (Harb) asked why the extractor is an exe. Answer: GitHub now builds it in public (release `extractor-1.4.0`, sha256 on the release page; §9); the creator replies to Harb and re-uploads with the GitHub-built exe (`fetch_extractor.py`).** Earlier: "Risk of Deadlock" 0.7.0 package built (2026-10-19): one mod, first release **Haze, Abrams, Seven, Infernus** (`thunderstore/release.json`; Mo & Krill later); `package.py 0.7.0 --thunderstore`; the plugin finds Deadlock and extracts on the first start (`FirstRun.cs`; doc 03 §6b). The co-op mod-list entry names the built heroes. No Deadlock files in it. Waiting on the creator's thunderstore.io account/team and go-ahead to upload |
| Pages | Eight claude.ai Artifacts now (seven heroes + the balance overview; Rat King's new 2026-10-21, the creator still has to share it and Viscous's). Earlier: five claude.ai Artifacts, redesigned (links at the top of doc 06); all five shared "anyone with the link" (2026-10-18) and linked from the Melty listing |
| Voice lines | `design/voice.json` + `Runtime/Voice.cs` (doc 02 §8). Not yet heard in a real run: unselect (teleporter finish), multikill and idle lines; the test only proves select/skill lines play. Release needs a forced re-extract (new voice files) and the recipe check `voice-lines` |
| Open | RoR2 survivor portraits fail to render (§9); pickup figure size and Haymaker wave vfx not seen in game; the bench is noisy for Loader/Artificer (a steadier multi-run bench would help) |

---

## 2. What the heroes do now (summary; numbers in doc 06)

| Hero | RoR2 match (stats) | M1 | M2 | Shift | R (new in 0.4.0) | Q ult equipment | Passive |
|---|---|---|---|---|---|---|---|
| Haze | **Commando** | Twin SMGs 55 % | Sleep Dagger 500 %, pierces, 4 s sleep, 6 s | Smoke Bomb (Cloak, +50 % speed) | **Phantom Strike**: blink behind aimed enemy, 3 × 250 % | **Bullet Dance**: 5 s, 113 % at *every* enemy in 25 m, 12×/s | **Fixation**: stacks; every 10 → 300 % burst splashing 6 m |
| Abrams | **Loader** | Shotgun 9 × 45 % | Siphon Life (= RoR2 Mired Urn) | Shoulder Charge 500 %, stun, knock aside | **Haymaker**: wind-up, 6 m lunge, 900 % + shockwave cone | **Seismic Impact**: hover, aim, Left Click slams 6800 % in 18 m | Infernal Resilience (heal-back, shorter debuffs) |
| Seven | **Artificer** | Shock Rifle 3 × 48 % bursts | Lightning Ball (16 s) | Static Charge (16 s) | **Power Surge** (30 s): bursts arc RoR2 Tesla lightning | **Thunderhead**: storm zone + RoR2 Warbanner | **Capacitor**: Static stacks discharge 300 % + stun |
| Infernus | **Engineer** | Flame Gun 50 % | Napalm (lobbed, 2 charges) | Flame Dash (hold, fire trail) | **Torment Pulse**: fire rings, 50 %/pulse for 6 s | **Concussive Combustion**: 3 s fuse, 6800 % in 18 m | **Afterburn**: build-up → burn |

The RoR2 match is `design/stats.json` → `analog` (the body the hero's stats are anchored to and cloned from):
`CommandoBody`, `LoaderBody`, `MageBody` (Artificer), `EngiBody` (Engineer). The creator confirmed these as each hero's
"corresponding actual Risk of Rain character" on 2026-10-16 (they chose "their stat base" over "closest playstyle").

---

## 3. The ult system (Q equipment), in depth (since v0.4.0)

**Why:** the creator: "each of our characters have ult in deadlock ... I actually want to restructure this and have the
ults as new pickups (Activate Equipment: Q) ... every new hero from deadlock has this auto equipped at the start, but they
can also drop them and find other hero ults in the game ... the item should look like the character's head in game
[later picked: a whole mini hero] and the recharge time should be 120 and these ults should be scaled to the same dps
power level as the Preon Accumulator."

**How it works** (code `plugin/Content/Equipment.cs`, rows `design/equipment.json`, doc 02 §7 / gotcha 17):
- One `EquipmentDef` per hero ult, added to the content pack. `canDrop`, `enigmaCompatible`,
  `canBeRandomlyTriggered` (Bottled Chaos), orange equipment colour: they drop wherever the Preon Accumulator can
  ("anywhere you can find the Preon Accumulator you can find these").
- **Starting equipment:** `CharacterBody.onBodyStartGlobal`, once per player per run (first stage, a `given` set), so a
  hero who swapped it away does not get it back.
- **Firing:** Harmony prefix on private `EquipmentSlot.PerformEquipmentAction` returns true for our defs; the ult's
  EntityState starts **where the body has authority**: the prefix for server-owned bodies, a postfix on the
  `RpcOnClientEquipmentActivationRecieved` ClientRpc for remote owners. `DlrState.RowOverride` carries the skill row (any
  survivor, even Commando or an Equipment Drone, can fire any ult) and is serialized with the state.
- **Pickup look:** the hero's display model baked in its idle pose into one mesh (materials copied with `_Fade = 1`),
  bounds padded ×2.2 so RoR2's `PickupDisplay` shrinks it to ~0.7 m. *The ×2.2 size has not been seen in game yet.*
- **Power:** at least Preon level, ~6800 % per enemy per use (Preon 140 s scaled to 120 s). **Since v0.4.1 the ults are
  not balanced** (creator: "technically any character can have them so let's just compare the main moves! and the
  pickup 'ultimates' can be overpowered as we like"). Preflight only enforces the ≥ 6000 % floor and 120 s cooldown.
- The old charge meter (`Ult.cs`, kills/damage charging R) was **removed** in 0.4.0.
- Seismic Impact slams on **Left Click only** (or its 3 s timeout).
- **Sounds, effects and voice belong to the item:** whoever fires it gets the ult's Deadlock sounds and effects (from the
  row in `RowOverride`), and the item's hero speaks its ult line (`Equipment.Fire` → `Voice.SkillLine`). Combustion's 3 s
  fuse: Deadlock's cast is itself a rising build-up; since v0.6.2 a timer ticks on top once a second (sound columns `tick` / `tickFinal`).

---

## 4. The DPS testing system, in depth

**Tool:** `plugin/Runtime/DpsBench.cs` (dev only; active only while `%LOCALAPPDATA%\DeadlockOfRain\bench.json` exists),
driven by `tools/run_bench.sh [seconds] [hero]`. Method and every variable: doc 05 §2. In short:
- Fresh run, **Monsoon**, one flat spot, a pack of **12 still enemies** (8 Lemurians, 4 Beetles, AI off, ×101 health)
  10–15 m ahead; survivor level 1, no items, god mode, damage stat 12 for everyone.
- Player input switched off; the bench **holds M1** and **taps M2/Shift/R every other physics frame** so each fires
  the moment it is ready; perfect aim at the nearest enemy; put back on the spot after dashes.
- Counts every hit the survivor (and, since the §9 work, its minions, e.g. Engineer turrets) lands, as
  `DamageInfo.damage` (after crits and damage mods, before armor), converted to **% of damage stat per second**.
- Passes per survivor: **all** (main moves together, *no equipment*), then M1 / M2 / Shift / R alone, then
  **equipment** (one Q use; Commando holds the Preon Accumulator) for the "just for fun" table.
- Writes `build/bench_results.txt` (not in git); also a `stats <body>: ...` line per survivor (§9).
- One run per pass: about ±10 % noise. Read differences under ~10 % as equal.
- `run_bench.sh 30 seven` runs only Commando, Seven (and its match): quick tuning.
- `PORTRAITS=1 run_bench.sh 0` renders studio portraits only (§9).

**Balance rule (creator, 2026-10-16):** compare main moves only, against the hero's own RoR2 match; balanced = **±500 %/s**, on the
means over every run of the current build. (Earlier the same day: ±20 % of Commando.) Until §9 lands the baseline is
Commando (1255 %/s → band 1004–1506). **Decided next:** each hero is compared with **its own RoR2 match** (§2), measured
the same way, ability by ability ("Their own RoR2 match" was the creator's pick).

**Stored DPS library:** `docs/dps-library.json` holds every survivor's numbers (3 runs each, doc 05 §6). For a **new hero**, run
`bash tools/run_bench.sh 30 <hero>` and compare with the stored means instead of re-measuring everyone; merge its run with
`python tools/dps_library.py --add build/bench_results.txt`. Acrid, Railgunner and Void Fiend are flagged not comparable.

**Pipeline after a bench run (standing rule):** refresh doc 05 §3 and doc 06's DPS, clear doc 05 §0 flags the run
covered, update `tools/kit_pages.py`, run it, republish the five pages to their existing links (doc 06 top).

**Tuning history:** doc 05 §5. Notably Seven was brought into the band with **cooldowns only** (creator: "suggest
cooldown increases rather than nerfing damage"): Lightning Ball 6 → 16 s, Static Charge 7 → 16 s, Power Surge 14 → 30 s.

---

## 5. How the creator likes to work (observed in this project)

- Writes quickly and informally (typos are normal); read for intent, quote them in doc 07 verbatim.
- Wants deep research and **many clarifying questions before implementing**; AskUserQuestion with a recommended
  first option works well. They often answer with free text in "Other"; read it carefully: it can change the plan
  (example: the Haze question was answered with "don't compare the ults at all").
- Asks "how much longer / is it ready": answer with status and say plainly when the build is ready to play.
- Cares about how things look ("make it look cool", Deadlock-wiki styling, effects built in Unity from RoR2 materials,
  "not a rip from the Deadlock engine files").
- Interrupting a tool call means "stop and listen", not "never do that": wait for their next message.

---

## 6. Hard rules (also in CLAUDE.md)

- Never ship or commit game files; never modify the Deadlock install; never work around anti-cheat. **Pages and
  artifacts must not embed Deadlock's own art files** (hero cards, icons from the VPK). Use our own in-game renders.
- The Melty token is secret (only in Melty's Authorization header).
- Never launch the game for the creator's playtests; close it after every automated run (`lab_restore.sh`).
- Docs in the same commit; doc 06 + doc 05 §0 flag for every ability change; record every session in doc 07.
- Ask before `publish` on Melty; don't push unless asked.

---

## 7. Lessons from past sessions (things that cost time)
- **A twisting hero is often a full-body clip on the upper-body layer** (2026-10-21, Seven): look for Deadlock's `*_upper_body` clips first; and read the test's `spun N deg` (a flight that whips round shows as 1000+). Doc 02 gotchas 24-26.
- Headless `claude -p` with the Melty MCP answers in prose (Melty's instructions); use `--json-schema` and read `structured_output`. PowerShell 5.1 needs `\"` inside native arguments (doc 03 §5b).

- **Never rebuild the cache (`--force`) while the creator may start the game** (2026-10-21): the creator launched Rat King
  mid-rebuild, the props folder was missing for that minute, and the planted banner came out as the placeholder pole (log:
  `effect model ratking_banner_prop not in the cache`); it looked like we had replaced Deadlock's model. Tell the creator before a
  rebuild and wait for "done playing"; the plugin reads the cache at start and lazily (props), so a running game is hit too.

- **After any extractor change, dump every hero's materials before testing** (2026-10-21): Deadlock settings applied to a whole
  material (glow, tint) wrecked approved heroes in the first 1.6.1 cache (Infernus all red, Abrams' head white). Keep new material
  behaviour to the models that need it (props), and remember Deadlock's glows are masked. Check: `python tools/dlm_mats.py`.

- **Watch long game runs in the foreground** (2026-10-21): a backgrounded `run_test.sh` was reaped while the session sat idle (the game
  kept running the test). Start it in the background, then loop on its log line in a foreground command (no leading `sleep`, it is refused).
- **A hero that stands in the lineup but lies down in play** (2026-10-21): check root_motion's rotation per clip and where the muzzle bone
  really is; both fooled the gun rig into bending Rat King's spine (doc 02 gotcha 23, `muzzleForward`).

- **Publish a page in the same session you rebuild it** (2026-10-20): the Viscous page sat at its first version (no pictures) while `build/pages` had the new one. Before republishing a page from another session, diff its live source (images stripped) against the rebuild; that diff caught a dropped row.

- **Bash heredocs with backslashes/quotes** in this Git-Bash setup: write the Python script to the scratchpad with the
  Write tool and run it, instead of inline heredocs, whenever the text has `\`, `$` or nested quotes.
  (2026-10-19: even a quoted `<<'PY'` heredoc turned `\n` inside Python strings into real newlines and broke C# strings; use Edit.)
- **Read player logs before "fixing" a reported crash** (2026-10-19): the pasted v0.6.3 crash was already fixed in the live
  0.7.1; the real new problem was a moved model on an older Deadlock. Check which version each log is from.
- A heredoc once turned `\b` into a **backspace byte** in doc 02 (`DeadlockOfRain<BS>ench.json`); fixed in 0.4.1.
  Grep docs for control characters (`grep -c $'\x08'`) after scripted edits.
- **Burst-gun bug** (fixed 0.4.1, MODLOG 11): the burst counter went to −1 when the magazine ran dry mid-burst; it
  hid in play because other skills interrupt the gun state. Look for "stops after one magazine" symptoms.
- The pickup figure first came out **life-size** (PickupDisplay scales by bounds volume); pad bounds to shrink it.
- Benchmarks are noisy (Phase Round 592/854/752 across runs; Loader 1769–5241, Artificer 1526–4507). Don't tune to one run; use means.
- Seven is non-linear: Lightning Ball below ~8 s makes Capacitor discharges chain (1893 → 5300+). Keep it ≥ 8 s.
- Claude Code may kill a long background job when the PC is low on memory; the game can then stay open, so check
  `tasklist` and run `lab_restore.sh`. Poll long runs in the foreground (≤ 10 min per call) so the session is not idle.
- In "all" passes, long dashes (Shoulder Charge 3 s) carry survivors away from the pack; that understates Abrams.
- Artifact republish: publish the same `build/pages/*.html` path with the page's `url`; a sharing change on the page
  can make the first republish fail, so retry.
- Python 3.11 (this PC) rejects backslashes inside f-string expressions; build such strings in a variable first.
- The browser tool cannot open `file://` pages, so a local page preview is not possible; check the published page instead.
- Melty MCP: `consult_melty` once per turn when doing Melty work; never claim an awaiting-review release is live.
- **Deadlock's sound `volume` is in decibels** (0 = as recorded, negative = quieter). Until extractor 1.2.0 it was used as
  a linear gain, so every 0 dB-or-quieter event (most ability sounds, Infernus' gun at −1 dB) played at the mixer's 20 %
  floor. "This sounds weak" is worth checking against the cache's `sounds/sounds.json` volumes first.
- **Co-op (v0.6.0):** RoR2 runs a skill state on every machine but a player's inputs (aim, buttons) exist only on their own
  PC; anything the server needs from the owner goes in `OnSerialize`. Code that runs on the server alone must play its
  sounds/visuals through `Relay`, or the other players never see them. Test with `run_coop_test.sh`; the second copy is slow,
  so steps wait for answers, and `run_coop_test.sh haze,abrams` keeps a check to ~8 min.
- **Bots mash keys:** the DPS bench presses every key whenever ready, so a "press again to cancel" control never lets
  it act; give test/bench mode its own path (Napalm throws by itself after 0.8 s and ignores the cancel there).
- `run_test.sh` sometimes lands on a **ledge** (hero ~15 m above where the summoned targets fall): nearly every check
  fails, including moves nobody touched. The stage is random, so run it again; if in doubt, run the previous commit at once.
  Distant Roost has a cliff-edge spawn that does the same to lock-on moves (targets below the edge: "no target").
- `run_test.sh` leaves the game open when it finishes: run `lab_restore.sh` straight away (it deletes the devshots, so
  copy them aside first if you still need to look); 2026-10-19 the game sat open through three fix-and-rerun rounds.

---
- **A Deadlock update can break the extractor** (2026-10-19, build 6795: VRF "Unknown KVType"): check `player_logs` on Melty,
  update the VRF NuGet, publish the exe, re-read with `--force`, compare the cache with the old one. Since extractor 1.3.0 one
  unreadable hero no longer takes the others down, so players keep most heroes while a fix is made.
- **Store moderators distrust bundled exes** (Thunderstore, 2026-10-19): ship one GitHub builds from public source, with a
  sha256 on the release page, and say in the README what it does and why it can't be a library.
- **A crash that "isn't ours" may still be fixable by us** (2026-10-20, the quit crash): run the control first
  (`OFF=mod VANILLA=1 tools/quit_test.sh`), and repeat every condition 3+ times when a crash is intermittent: one clean
  quit proved nothing. A native crash in ntdll after a Mono stack overflow hides the managed cause; RoR2BepInExPack's
  nop patch turns it into a logged StackOverflowException whose trace names the loop. Check what r2modman players get
  from RoR2BepInExPack that Melty players (plain BepInEx) don't.
- **Test only the heroes you changed** (`run_test.sh <hero>`). Every unasked game launch costs the creator's patience; say up front how many runs you need and why, and clear leftover test files before a bench (a stale test-mode file wrecked one bench run).

- **Unity's mixing transform is not Deadlock's layering** (2026-10-22): a gesture on `upperBodyBone` keeps the clip's spine
  relative to the legs' hips, Deadlock keeps it in model space; a clip whose own hips are turned twists the chest by that much.
  `ModelSpaceGesture` fixes it for every hero. When a hero "twists", measure the head angle (clip alone vs mixed) before
  swapping clips: the v0.9.1a clip swap treated the symptom.

## 8. Where everything is recorded (the record-keeping system)

| What | Where | When to update |
|---|---|---|
| Current state, prompt for the next agent | **this file** (`docs/00-HANDOFF.md`) | end of every session: §1, §9, and any section whose facts changed |
| What happened, session by session (requests, Q&A, results, commits) | **`docs/07-SESSION-LOG.md`** | append one entry per creator request (template at its top) |
| Deadlock side (assets, extractor) | doc 01 | with the change |
| RoR2 side (code, kinds, hooks, test, bench) | doc 02 | with the change |
| Process, decisions history, open items | doc 03 (§9 decisions, §10 open items) | with the change |
| Ability options and the creator's picks | doc 04 | when kits are redesigned |
| Benchmark method, results, flags, suggestions | doc 05 | every bench run / ability change |
| Every current ability with numbers and DPS | doc 06 | every ability change |
| How to add a new hero in one go | doc 08 (new-hero playbook) | when the process or tools change |
| Short version journal + gotchas | `MODLOG.md` | new gotchas, versions |
| Rules for agents | `CLAUDE.md` | when a rule changes |
| Player-facing description | `README.md` | when features change |

Raw, non-git records: `build/bench_results.txt` (last bench), `%LOCALAPPDATA%\DeadlockOfRain\devshots` (test
screenshots, if the folder exists), the Claude Code transcripts under `~/.claude/projects/` (full chat history).

---

## 9. Last work: Seven v0.9.1b, Storm Cloud 3 targets, new Thunderhead icon, clips re-picked (2026-10-22)

- Done this session: doc 07 entry, doc 05 §3.13, doc 02 gotchas 24 (corrected) and 27. Installed build = this one (cache rebuilt).
- Next: the creator plays it; a balance pick for 1759 vs 4013 (doc 05 §3.13); the rest of Seven (Thunderhead, the Shock
  Rifle's behaviour, the passive). Owed: `run_coop_test.sh seven`.

## 9-prev7. Seven redesign, part 1 (2026-10-21, v0.9.1a)

- Done this session: doc 07 entry, doc 04 §8, doc 05 §3.12. Installed build = this one (cache rebuilt for the upper-body clips).
- Next: the creator plays it; then the rest of Seven (Thunderhead, the Shock Rifle's behaviour, the passive) and a balance
  pick for his 2827 vs 4013 (doc 05 §3.12 levers). Owed: `run_coop_test.sh seven`; look at the yellow core, impact and tracer.

## 9-prev6. The daily watch (2026-10-21)

- `tools/watch/` (manual: doc 10; summary: doc 03 §5b; doc 07 entry). Task registered and the baseline seeded; the dry run and Melty triage have been tried.
- Next: one `daily.ps1 -NoPublish` run with an older Deadlock build in `build/watch/state.json`, while nobody is playing;
  `gh` for GitHub issues if the creator wants them. Merge any `watch/*` branch the watch leaves.

## 9-prev5. 0.9.0 on Melty without Rat King (2026-10-21)

- Goo Ball 100 s. `package.py 0.9.0 --melty`: six heroes, Rat King a greyed locked "coming soon" tile with his portrait.
- Release `310d6466-e084-4f27-920c-5d33fb19c6ad`, published (safety review); no test (creator). Next session: `mod_status`,
  `install_outcomes`, `player_logs`; then the owed Viscous test and bench.

## 9-prev4. Puddle rounds in front of the magazine (2026-10-21, v0.9.0e)

- In a puddle Viscous's counter reads `20+5/20`; shots spend the puddle's rounds first (even mid-reload), gone on stepping out; the puddle no longer refills the magazine (`HeroRuntime.Extra/CanFire/Rounds/Spend`).
- **Owed:** `run_test.sh viscous` and `run_bench.sh 30 viscous` (the creator said not to launch this time); doc 05 row open.

## 9-prev3. Puddles on the floor, ammo counter (2026-10-21, v0.9.0d)

- `DlrPuddle.Floor` looks 1000 m down (it was 8 m): puddles from Puddle Punch or The Cube high in the air land on the floor; no puddle over a pit.
- `Runtime/AmmoCounter.cs`: "rounds/magazine" above the M1 icon for every Deadlock hero (local player only, grey while reloading).
- Test checks `ammo-counter` and `puddle-on-floor`; all seven heroes pass. Open: the counter not seen by eye, co-op not run.

## 9-prev2. Royal Pestments names the rats (2026-10-21, v0.9.0c)

- Royal Pestments calls out Rat Swarm's named-rat lines one after another for 6 s (shuffled, no repeats); Scrap Grenade's and
  Rat Swarm's voice groups were swapped in 0.9.0 and are fixed. Extractor **1.6.2** (local): voice takes keep Deadlock's numbers.
- The creator saw a placeholder flag because they started the game mid-rebuild; never rebuild while they may play (§7).

## 9-prev1. Rat King's first play-check (2026-10-21, v0.9.0b)

- Footsteps 90 % quieter (landing kept), King's Leap flip only on the air jump, banner earth/garbage coloured (extractor **1.6.1**,
  DLM2; local build, needs the `extractor-1.6.1` tag for a release), Royal Pestments 50 %, Rat Swarm without a time limit. Doc 07 has the entry.
- `run_test.sh` all heroes: Rat King pass, Mo & Krill one flaky miss (passes alone). Bench 3542 vs Captain 3490. Pages republished.
- Open: the creator's play-check of the flip, the swarm and the banner; co-op; Melty release; share the Rat King and Viscous pages.

## 9-prev0. Rat King, the seventh hero (2026-10-21, v0.9.0)

- From the creator's pasted spec; answers: Scrap Grenade "any click" throws, no extras ("just damage and stun please!"). Doc 07 has the entry.
- New: extractor 1.6.0 (animated props, split meshes; **local build**: a release needs the `extractor-1.6.0` tag, `fetch_extractor.py`
  and a forced re-extract), spring bones, the rat crowd, five Rat King kinds, King's Leap, `muzzleForward`, and the root-turn fix for every hero.
- `run_test.sh` all seven heroes pass; bench mean 2999 vs Captain 3490 (inside). All eight pages republished.
- Open: share the Rat King (and Viscous) page; play-check the cape, the double-jump spin and the banner; co-op test; Melty release.

## 9-prev00000000000. Viscous Puddle Punch puddle, smaller puddles (2026-10-21, v0.8.0f)

- Puddle Punch leaves a 6.4 m puddle; Splatter's 6.4 m, the Cube's 8 m. Bench mean 1958 vs REX 1738 (inside; runs swing 1660–2410).
- The test's puddle-ammo step now clears leftover puddles first (the punch's own test left one); **its re-run was stopped for low memory: run `run_test.sh viscous` again**.
- Open: share the Viscous page; Viscous co-op test; the Melty release; vanilla portraits empty.

## 9-prev00000000000. Viscous puddles as an ammo supply (2026-10-20, v0.8.0e)

- Puddles 3 s whole + 5 s shrinking; each holds 20 rounds and tops the magazine up while the owner stands in it (`HeroRuntime.TopUp`, never above the magazine).
- Splatter 150 %, jump power 20. `run_test.sh viscous` all pass; bench mean 2253 vs REX 1738 (15 above the band; Puddle Punch 700 % or Splatter 140 % would fix it).
- Viscous and balance pages republished. Open: share the Viscous page; Viscous co-op test; the Melty release; vanilla portraits empty.

## 9-prev0000000000. Viscous puddle ammo and the second round of changes (2026-10-20, v0.8.0d)

- New sheet columns `puddleAmmo` / `puddleShrink` and `heroes.voiceGain`; the Cube's puddle via `extra`. Doc 02 has the mechanics.
- `run_test.sh viscous` all pass (puddle ammo 0 → 20 → 40). Bench mean 2422 vs REX 1738: **above** the band; offer the creator Splatter 200 → 150 %.
- Viscous and balance pages republished; the Puddle Punch picture was removed at the creator's word (no effect pictures on cards).
- Open: share the Viscous page; Viscous co-op test; the Melty release; vanilla portraits empty.

## 9-prev000000000. Viscous after the creator's first play, DPS, pictures on his page (2026-10-20)

- The earlier session's uncommitted Viscous feedback work was checked in game (`run_test.sh viscous` all pass) and committed.
- 0.8.0c: base damage 15 +3, The Cube burst 600 %. Bench 2172 / 2225 / 2312 (mean 2236) vs REX 1738: inside by 2 %/s, and 25 % more real damage than %/s shows.
- The live Viscous page was the first version, made before his renders. It is now republished with the portrait, the Goo Ball figure and the fist render.
  All seven pages republished; the creator still has to share the Viscous one.
- Open: vanilla portraits render empty (doc 02, portraits); Viscous co-op test; the Melty release with Viscous; drop `git stash` "viscous wip before 0.7.4 merge" (the work is committed now).

## 9-prev00000000. v0.7.4 the quit crash, found and fixed (2026-10-20)

Melty's only live issue was `crash:crashed:ntdll.dll` (3 players on 0.7.2/0.7.3, c0000005 while quitting after a run).
- Built on branch `hotfix-0.7.4` (worktree `../RiskofDeadlock-hotfix`, from 3471d78) so the unfinished Viscous work on
  `main` stays out; merge the fix back into `main` (main keeps 0.8.0).
- `tools/quit_test.sh` reproduces it: before the fix 2/3 crashed with the mod, 2/2 with the mod off (`OFF=mod VANILLA=1`).
- Cause (doc 02 §12 gotcha 22): RoR2's `UnitySystemConsoleRedirector.Disengage` + BepInEx's `ConsoleSetOutFix` make
  BepInEx's console writer write to itself at quit → stack overflow in Unity's final clean-up → Mono's misaligned
  recovery thunk → ntdll fault under the Steam overlay's hook.
- Fix `Runtime/QuitSafety.cs`: skip `Disengage`; nop Mono's `push rbp` (RoR2BepInExPack, MIT, notice beside the DLL).
- After the fix: console fix alone 3/3 clean with no overflow at all; both fixes 2/2 (+4/4 earlier with the patch and a
  DDOL clean-up that was then dropped); near-vanilla 1/1; `BREAK=infernus run_test.sh haze` all pass.
- Tried and dropped: guarding our OnDestroy work, pre-compiling our methods, the DDOL clean-up (not needed).
- Test-only tools kept: `MeltyTest.Off(...)` switches (`OFF=` in `run_test.sh`), `VANILLA=1` Commando run.
- Melty 0.7.4: test `1cd1f52b-…` passed 2/2, published 2026-10-20 (safety review pending at the time of writing).
- 0.7.4 **live** since 12:49 UTC; first day: 7 plays, 0 crashed. Merged into `main` (7baa5f1) with security hardening
  (998012c: Mono patch pinned to the verified build + VirtualQuery). Main tested: all six heroes 24/24 pass + clean
  quit; 3 more clean quits. Open: near-vanilla quit, `BREAK=infernus` and co-op on main (stopped / not run: low memory).
  Main's Viscous work is still uncommitted (a backup is in `git stash`).

## 9-prev0000000. v0.7.3 clean sound shutdown (2026-10-19)

The creator pasted a player's 0.7.2 extract.log:
- The setup read **5/5 heroes** on Deadlock 6795, so the fallback and model lookup work on players' PCs.
- Melty's one current issue is the same player's game crashing in `ntdll.dll` (c0000005) while quitting, after 228 s.
- `DeadlockAudio.Shutdown` used to only clear a flag. It now joins the mixer thread (0.5 s) so winmm is closed and the buffers are freed before exit (doc 02 §8).
- **That did not fix it.** On this PC:
  - quitting after a `run_test.sh haze` run still crashes, while "Deadlock audio stopped" is logged;
  - an unmodded game and a modded title-screen quit don't crash;
  - the dump (`%LOCALAPPDATA%\CrashDumps`, scanned with the `minidump` Python package) shows the crash in Mono's runtime shutdown, with no mod thread alive.
  - **Open:** find what in a run leaves Mono unable to shut down. Bisect by turning off parts (Harmony patches, Vfx, ragdolls), quitting after a run each time. Players lose nothing (the profile is saved first).

## 9-prev000000. GitHub builds the extractor exe for Thunderstore (2026-10-19)

The Thunderstore moderator Harb asked why the extractor is its own exe and suggested a library inside the DLL. Researched:
- VRF and its dependencies are .NET 10-only, so a DLL means a patched VRF copy for Unity Mono.
- That copy costs days of work up front and makes every Deadlock format fix harder.
- A DLL plan was written (session log); the creator chose to keep it as notes and explain the exe instead: "have GitHub build the exe".

Done:
- `.github/workflows/extractor.yml`: on a tag `extractor-X.Y.Z` it publishes, makes a Release with the sha256 and build log, and checks the tag against `Program.Version`.
- `tools/fetch_extractor.py`.
- `package.py --thunderstore` accepts only the GitHub-built exe.
- "About DeadlockExtract.exe" section in both READMEs.

Full write-up: `docs/09-PUBLIC-EXE-BUILD.md`. **Next:** the creator sends Harb the release link and re-uploads to Thunderstore. If Harb refuses any exe, the DLL plan is the fallback.

## 9-prev00000. Fallback system + five-hero Melty release v0.7.2 (2026-10-19)

A player's Melty logs: v0.6.3 had stopped on Deadlock 6795 (fixed by 0.7.1), and on 0.7.1 the player's Deadlock **build 5977**
had no `models/heroes_wip/inferno/inferno.vmdl`, so Infernus was skipped (the others loaded: 0.7.1's skip worked). The creator
asked for a visible red "!" instead of a missing hero, recorded versions, loading progress and more safeguards (CLAUDE.md
"Fallback system" lists every layer). Done: extractor 1.4.0 (vdata model lookup with folder fallbacks, `--list-models`,
progress lines, icons can't fail the run); `Content/MissingHeroes.cs` (locked red "!" tile, two Harmony hooks);
`Runtime/BuiltWith.cs` + `package.py` built-with stamping and release refusals; `FirstRun.ShowSummary`; test mode `"break"` +
check `missing-hero-tile`; buff icons of unreleased heroes no longer warn. Tests: extractor fault run (Infernus at a wrong
folder found, a fake hero skipped, exit 0); full cache 5/5 on 6795; `BREAK=infernus run_test.sh haze`: every check pass, red
"!" in the lobby shot. **Next:** Melty 0.7.2 with all five heroes (full manifest, `setup.done.contains` `|1.4.0|544C18F87BF82B69`),
test_release, publish, "Built with" line in the listing; later check `player_logs` for the 5977 player.

## 9-prev0000. Mo & Krill balance pass v0.7.1c (2026-10-19, built; not tested, at the creator's request)

After playing: Mo 5 → 6.3 m/s (90 % of Abrams), Burrow `burrowed.moveSpeedAdd` 1 → 2 (×3, ≈19 m/s), Sand Blast 450 → 750 %
and knockback 8 → 22, Dust Devil 200 → 450 % per tick (14400 % per enemy per use), proc 0.3 → 0.5. Sand Blast's look redone in
`Vfx.MokrillSand`: the wind-up cone filled with sand colour (`ConeMarker.Fill`), a 2.5 m wall of opaque sand with grains and
settling dust (`SetWave` keeps each part's height), a spray from his hands (`Spray`), bigger hits, and each Sanded enemy's
model coated with `mat_sand_overlay` (Bison's snow overlay tinted sand, a `TemporaryOverlay` added and removed by `SandedFx`).
Then "maybe a little bit too over powered now": Tag Team 1500 → 1000 % and splash 500 → 300 %, base health 250 → 200, game started again.
The creator said "dont do the test just open the game and let me play": no `run_test.sh`, no bench; the game was started
for them. **Next:** their verdict on the numbers and the sand look (the overlay's tint is unverified in game); then
`run_test.sh mokrill`, bench Mo & Krill only, clear the doc 05 flag, republish the Mo & Krill page.

## 9-prev000a. Deadlock update 6795 broke the heroes; fixed in v0.7.1 (2026-10-19, built and tested; not released)

Deadlock's update of 2026-10-19 (build 6795) made VRF 20.0 throw "Unknown KVType 26" on Haze's model, the extractor
aborted, and every Melty player on 0.6.3 got no heroes (3 players, `player_logs`). Melty started its own fix (0.6.4,
uploaded, not tested or published); the creator wanted it fixed here. Done: VRF 20.1.7467, extractor 1.3.0 reads each hero
on its own (ready.json `failed`), the plugin skips and names unreadable heroes on the title screen, version 0.7.1.
All five heroes read from 6795 (9 min 40 s); the broken-hero test kept the other hero. The creator: "straight away upload to melty with the 4original characters", then "Publish now": `package.py 0.7.1 --release`
(Haze, Abrams, Seven, Infernus; recipe `setup.done.contains` `|1.3.0|FD43C19E6B46D5A4`), release `2dcdb0d2-058c-4014-96a3-90198b7c99f6`,
published and **live 2026-10-19 15:06 UTC** without a Melty test (not required for a live mashup); the player bug is recorded as fixed by it.
Melty's own 0.6.4 stays an unpublished draft. **Next:** check `player_logs` stay clean; Thunderstore
re-upload of `build/Risk_of_Deadlock-0.7.1-thunderstore.zip` (by the creator); Mo & Krill in a later release.

## 9-prev000. "Risk of Deadlock" first Thunderstore release, four heroes (2026-10-19, built; uploaded and rejected)

One mod with every hero of the release (the creator dropped the per-hero idea "so online always works"); the first
release is Haze, Abrams, Seven and Infernus (`thunderstore/release.json` → `heroes.txt`, a cut-down extractor manifest,
four-hero icon and store text). The mod-list entry now names the built heroes, so a player with other heroes or none
(no Deadlock) is refused at the lobby. **Next:** the creator's thunderstore.io team and go-ahead to upload; Mo & Krill in a later release.

## 9-prev00. Thunderstore package (2026-10-19, built and tested; not uploaded)

The creator wanted Steam Workshop with Deadlock assets bundled; bundling was declined (Valve's files) and RoR2 has no
Workshop, so: Thunderstore, with the plugin reading the player's own Deadlock on the first start (`FirstRun.cs`, the
loading screen waits; doc 03 §6b). `package.py 0.7.0 --thunderstore` built; first start tested (6 min extract, heroes
built), Haze test pass on the rerun. **Next:** the creator's thunderstore.io account/team, then upload with their
go-ahead; push the repo when asked.

## 9-prev0. Mo & Krill play-check pass (v0.7.0b, 2026-10-19, built and tested; not released)

The creator's notes after playing him (doc 07): Tag Team to R (30 s, numbers unchanged, no target = R stays ready), Dust
Devil to the Q ult (8 s, 32 × 200 %, any carrier spins), Burrow 8 s with a 3 × 500 % pop-out and a visible sink and rise,
Sand Blast 0.4 s wind-up with the cone drawn on the ground, a heavier throw and an 8 m/s shove, acceleration 80 (top speed
still 5), and a Frenzy while Krill is away (×2 speed, 600 % punches that throw monsters, red aura). Kinds renamed
`ult_krill` → `krill_hop`, `dust_devil` → `ult_devil`. `run_test.sh mokrill`: every check pass (new: frenzy speed and
knockback, no-target R, Q underground, Dust Devil on Commando). Bench 3252 vs MUL-T 1450 (far above: reported, not
retuned). Pages republished.

Co-op rerun (`run_coop_test.sh mokrill`, 1.6 GB free at the start): every check pass, all five moves as a joined player.

**Next:** the creator plays him again; a release when
asked (recipe `mokrill-plays`, drop one check; forced re-extract; `test.minutes` +2). Repo public: still waiting on whether
to push the unpushed commits first.

## 9-prev. Mo & Krill, the fifth hero (2026-10-19, built and tested; not released)

From the creator's spec (doc 07). Sheets, four new kinds (`burrow`, `cone_wave`, `dust_devil`, `ult_krill`), the gun pattern and
punch, prefix-aware gun rig (Krill holds the gun), `modelScale` plus MUL-T's capsule and camera, Heavyweight, effects, tests.
`run_test.sh mokrill`: all checks pass. Bench 1620 vs MUL-T 1450. Kit page published (private) and every page republished.

**Next:** the creator play-checks the stages (snags; collider = MUL-T's) and shares the Mo & Krill page; rerun
`run_coop_test.sh mokrill` when the PC has memory to spare (the first run was stopped by Claude Code for low memory after the
join); then a 0.7.0 release when asked (recipe `mokrill-plays`, drop one check; forced re-extract; `test.minutes` +2). The
creator also asked to make the GitHub repo public: waiting on whether to push the unpushed commits first.

## 9-prev2. v0.6.1–0.6.3 Infernus requests, ragdolls, fuse ticks, fire pockets, Abrams charge vfx (2026-10-18, done)

The creator played 0.6.0 from Steam (BepInEx re-enabled by copying `winhttp.dll`; Melty may switch it off again). Napalm
is now aimed (Right Click readies, marker shows the landing, Left Click throws; same arc ×3 as far), Combustion shows its
18 m range as a yellow ground ring on any carrier, every hero ragdolls on death, Flame Dash 5 s / 13 s. Answers in doc 07.

Then the creator asked whether the Q items carry their sounds and voice lines (yes; now spelled out in docs 00 §3, 02 §7.5, 06),
and asked for a docs refresh (this section, §1's stale install row).

Then v0.6.2: the creator chose "Add a tick each second" for Combustion's fuse: Rat King's scrap-grenade timer at 0 s and 1 s,
its final timer at 2 s (cut at the blast). Earlier I had called the fuse silent; it is not (the cast is a 3 s build-up).

Then v0.6.3: Flame Gun 75 %; Flame Dash always runs 5 s and lays Afterburn fire pockets (2× size, overlapping stack);
Abrams' Shoulder Charge rockets forward (yellow front, trails). The creator was angry at four-hero test runs for
one-hero changes: `run_test.sh <hero>` and a CLAUDE.md rule. Infernus 1149 vs Engineer 1611, inside.

Then the creator declared the four heroes done and asked whether the docs are ready for a new agent to add a hero in
one shot: doc 08 (new-hero playbook) written, doc 03 §8/§10 and doc 02 §11 refreshed, the test's hard-coded hero count
removed.

Then the creator: "its time to upload to melty!": 0.6.3 packaged, uploaded and submitted as a draft (release
`5e542898-…`). The recipe's `setup.done.contains` now forces older caches to re-extract (doc 03 §6). README's Flame Dash line fixed.
The creator then renamed it on Melty: **"Risk of Deadlock"**, slug `risk-of-deadlock-2`; test mode reads that slug's test
file too. First test: every run passed every check the mod wrote, but the recipe named checks Melty didn't read; resubmitted
with the first 12 the test writes, listing text rewritten with links to the kit pages. Retest: a run failed when the creator
opened the pause menu; a 2-run test (`e2e6d068-…`) then passed with both footage reviews (`graduates: true`).

Then published (creator's yes): 0.6.3 is live; the creator supplied the four character select screenshots.

**Next:** watch `install_outcomes` / `player_reports`; fix Infernus' lore text ("Catalyst" → Napalm) in the next release. After that: a new hero from the creator's outline (doc 08). Still open: Haze/Seven balance
(0.5.1), a co-op check of the Flame Dash pockets.

## 9-prev. v0.6.0 co-op (2026-10-17, done; untested with a real friend)

The creator: "ok now make these character work online!" (answers in doc 07). Built on RoR2's own networking: states run on
every machine, damage on the server, server-only sounds/visuals through one relay effect, the owner's choices (lock-on
target, aim, Static Charge's point) sent with the state; Melty's join contract in `MeltyJoin`. The two-copy test
(`run_coop_test.sh`) passes everything; solo still passes.

**Next:** play with a friend (Steam invite or Melty Join) and watch for lag on Haymaker and Seismic Impact; check Napalm's
smoke; release 0.6.0 with the recipe's `multiplayer` block (doc 03 §6), `setup_multiplayer` again before `submit_release`.
Still waiting on the creator from 0.5.1: Haze far above Commando, Seven below Artificer (below).

## 9a. Before: v0.5.1 playtest feel pass (2026-10-17, done; balance open)

The creator's playtest list (verbatim in doc 07): Haymaker lock-on rush and heavier hit, Sleep Dagger always visible,
Phantom Strike 5 m area, Lightning Ball slower/bouncing/10 m with clear strikes, Thunderhead clouds 35 m, Flame Gun
heavier sound and smoke, Napalm visible bottle. All built and tested (4/4). Found on the way: Deadlock sound volumes are dB
(extractor 1.2.0 fix; players need a forced re-extract).

**Waiting on the creator:** Haze far above Commando (Phantom Strike's area spreads Fixation; fix idea: Fixation on the
target only) and Seven below Artificer (the ball bounces on past the pack; idea: bounce only off walls, or slow per bounce).
Unseen in a real run: the bottle in flight, the 35 m clouds, the new loudness.

## 9b. Earlier: kit-page redesign + per-hero RoR2 match (2026-10-16, done)

**The creator's request (verbatim):** "the structure of the artifact is not good you need to have the top section be a
profile picture of the character and then stats like health etc (ror stats of character) the list of the ability what
they do and then the maths of the dps per each ability to the side of them. make it look cool and copy some styling from
the deadlock wiki, then underneath you can have your Ult with a little picture as well corresponding to the character
they are coming from then finally underneath we can do updates and the balance stats for each character, please give
these guys corresponding actual risk of rain character and have the analysis be separate section for each character".
Answers: studio renders (ours, not Deadlock art), match = stat base, each hero vs its own match, 4 hero pages + overview,
and one 30-minute bench run for the matches.

**What exists now:**
- `tools/kit_pages.py` reads `design/*.json`, `build/bench_results.txt` and `build/pages/img/*.webp` directly.
  Hand-written in it: `VANILLA` (match skill names), `CHANGES` (per version), `ANALYSIS` (per-hero notes).
  - Each hero page: portrait in a deco frame, Weapon/Vitality stat bands beside the match's stats, the passive, then
    M1/M2/Shift/R cards with the sheet maths and the measured hero vs match bars, the ult with the figure render, then
    changes and balance against the match.
  - Overview: the pairs, an analysis card per hero, the Q fun table, the method.
- `tools/portraits.py` cuts out `build/portraits/*_black/_white.png` into transparent webp.
- `PORTRAITS=1 bash tools/run_bench.sh 0` renders (about 2 minutes, opens the game).

**Known gaps:**
- **RoR2 survivors' display prefabs render empty or wrong** in `Portraits()`:
  - Commando and Loader come out empty, Engineer as just his turret, Artificer as just his orbs.
  - Disabling `CharacterModel` and forcing `_Fade` = 1 did not help.
  - Next idea: instantiate the body's model (`ModelLocator.modelTransform`) or the `CharacterSelectSurvivorPreviewDisplayController`
    path, or wait for its `onSurvivorDisplayed` intro.
  - The pages show a lettered badge instead.
- The creator was annoyed that the game kept opening while building pages. **Don't launch the game for page work without
  saying why and asking first.**
