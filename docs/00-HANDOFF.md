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
| Status | **The four heroes are done** (creator, 2026-10-18). Next: a new hero, one shot, via doc 08 |
| Version | **0.6.3** (`plugin/Plugin.cs`; Flame Gun 75 %, Flame Dash 5 s run with Afterburn fire pockets, Abrams charge vfx; 0.6.2: Combustion fuse ticks; 0.6.1: Napalm aimed throw, Combustion ring, ragdolls, Flame Dash 5 s/13 s), extractor **1.2.0** (sound volumes read as dB). **0.6.3 submitted to Melty as a draft (2026-10-18)**; needs a passing `test_release` and the creator's yes before `publish` (doc 03 §6) |
| Co-op | Up to 4 players (RoR2 lobby/Steam invites + Melty Join), everyone needs Deadlock; doc 02 §7.7. `run_coop_test.sh`: 4/4 heroes, all 25 skills as a joined player (2026-10-17). **Never tried with a real friend** (Steam lobby join, lag) |
| Installed build | **v0.6.3** (2026-10-18); BepInEx on, so pressing Play on Steam loads it (Melty may switch `winhttp.dll` off when it launches the game itself) |
| Heroes | Haze, Abrams, Seven, Infernus; 4 skills each (M1 gun, M2, Shift, R) + their Deadlock ult as **Q equipment** |
| Test | **`run_test.sh <hero>` tests only the given heroes (creator's rule, CLAUDE.md).** v0.6.3: Infernus pass (in a 4-hero run before the filter existed), Abrams 1/1 pass (2026-10-18); the co-op test was skipped for the pockets (creator impatient with game runs); `run_coop_test.sh infernus` passes (one earlier run missed the joined Combustion, unexplained) |
| Balance rule | Main moves (no equipment) vs the hero's **own RoR2 match**, within **±500 %/s**, comparing **means over every bench run** of the current build (`RUNS` in `tools/kit_pages.py`; doc 05 §3.0) |
| Balance now | v0.5.1 heroes-only run vs the library (doc 05 §3.1): **Haze 6550 vs Commando 1222 (far above: Phantom Strike's area spreads Fixation)**, Abrams 2460 vs Loader 2951 (inside), **Seven 3164 vs Artificer 4013 (below: the bouncing ball leaves the pack)**, Infernus 1149 vs Engineer 1611 (inside, just; v0.6.3, doc 05 §3.3). Not retuned; the creator decides |
| Bench rule | **Bench our heroes only** (`run_bench.sh 30 <ids>`, the default); vanilla numbers come from `docs/dps-library.json` (creator, 2026-10-17; CLAUDE.md). Every base-game and Survivors of the Void survivor is stored (3 runs each, also listed in doc 06); the Seekers of the Storm three are not (expansion off) |
| Sounds | Every move's Deadlock sounds and the voice-line triggers are listed in doc 06 "Sounds" (mirrors `design/sounds.json` / `voice.json`) |
| Pages | Five claude.ai Artifacts, redesigned (links at the top of doc 06); all five shared "anyone with the link" (2026-10-18) and linked from the Melty listing |
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

- **Bash heredocs with backslashes/quotes** in this Git-Bash setup: write the Python script to the scratchpad with the
  Write tool and run it, instead of inline heredocs, whenever the text has `\`, `$` or nested quotes.
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

---
- **Test only the heroes you changed** (`run_test.sh <hero>`). Every unasked game launch costs the creator's patience; say up front how many runs you need and why, and clear leftover test files before a bench (a stale test-mode file wrecked one bench run).

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

## 9. Last work: v0.6.1–0.6.3 Infernus requests, ragdolls, fuse ticks, fire pockets, Abrams charge vfx (2026-10-18, done)

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
with the first 12 the test writes, listing text rewritten with links to the kit pages. Retest `40cc813d-…` started.

**Next:** `test_release` of 0.6.3 (3 × 8 min on this PC, once the creator agrees), new listing screenshots, listing text,
then ask before `publish`. After that: a new hero from the creator's outline (doc 08). Still open: Haze/Seven balance
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
