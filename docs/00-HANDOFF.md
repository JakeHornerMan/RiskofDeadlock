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
2. The design sheets in `design/*.json` are the source of truth. Edit them, run `python tools/gen.py` (must say
   `PREFLIGHT CLEAN`), then `dotnet build plugin -c Release`.
3. Test with `bash tools/run_test.sh` (all heroes, ~7 min, must end `4/4 heroes`), then make sure the game is closed
   (`bash tools/lab_restore.sh`). Benchmark with `bash tools/run_bench.sh` (closes the game itself).
4. **Never launch the game for the creator's own playtests**; leave the build installed and say it is ready.
5. Every change updates its docs in the same commit, and every session ends with a doc 07 entry and a refresh of this
   file's §1 and §9. Commit after each logical step; never push unless asked; never publish on Melty without asking.

---

## 1. Current state in one screen

| Item | State |
|---|---|
| Version | **0.4.1** (`plugin/Plugin.cs`). Not published on Melty since 0.2.0 |
| Installed build | v0.4.1 + the bench's match/portrait dev code (committed with the page redesign) |
| Heroes | Haze, Abrams, Seven, Infernus; 4 skills each (M1 gun, M2, Shift, R) + their Deadlock ult as **Q equipment** |
| Test | `run_test.sh`: every check passes, 4/4 heroes (2026-10-16, v0.4.1) |
| Balance rule | Main moves (no equipment) vs the hero's **own RoR2 match**, ±20 % (doc 05 §3.0) |
| Balance now | Infernus 1226 vs Engineer 1268: **inside**. Haze 1478 vs Commando 1064: **above**. Abrams 985 vs Loader 2747 and Seven 1242 vs Artificer 2790: **far below**. The creator has **not chosen** fixes (suggestions in doc 05 §3.0) |
| Pages | Five claude.ai Artifacts, redesigned (links at the top of doc 06); the overview is shared "anyone with the link" |
| Open | Retuning vs the matches (ask first); RoR2 survivor portraits fail to render (§9); pickup figure size and Haymaker wave vfx not seen in game |

---|---|
| Version | **0.4.1** (`plugin/Plugin.cs`), last commit `d91047f` (balance pass). Not published on Melty since 0.2.0 |
| Installed build | v0.4.1 + the bench/portrait dev code (uncommitted while §9 is in progress) |
| Heroes | Haze, Abrams, Seven, Infernus; 4 skills each (M1 gun, M2, Shift, R) + their Deadlock ult as **Q equipment** |
| Test | `run_test.sh`: every check passes, 4/4 heroes (2026-10-16, v0.4.1) |
| Balance | Main moves vs Commando ±20 %: Haze 1315 and Seven 1272 inside; Infernus 920 and Abrams 729 below (doc 05 §3). The creator has **not yet chosen** fixes for Abrams/Infernus (doc 05 §5) |
| In progress | Kit-page redesign + benchmarking each hero against **its own RoR2 match** (§9) |
| Pages | Five claude.ai Artifacts, links at the top of doc 06; the Balance page is shared "anyone with the link" |

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

**Balance rule (creator, 2026-10-16):** compare main moves only; balanced = **±20 %**. Until §9 lands the baseline is
Commando (1255 %/s → band 1004–1506). **Decided next:** each hero is compared with **its own RoR2 match** (§2), measured
the same way, ability by ability ("Their own RoR2 match" was the creator's pick).

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
- Benchmarks are noisy (Phase Round 592/854/752 across runs). Don't tune to single-digit differences.
- In "all" passes, long dashes (Shoulder Charge 3 s) carry survivors away from the pack; that understates Abrams.
- Artifact republish: publish the same `build/pages/*.html` path with the page's `url`; a sharing change on the page
  can make the first republish fail, so retry.
- Python 3.11 (this PC) rejects backslashes inside f-string expressions; build such strings in a variable first.
- The browser tool cannot open `file://` pages, so a local page preview is not possible; check the published page instead.
- Melty MCP: `consult_melty` once per turn when doing Melty work; never claim an awaiting-review release is live.

---

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
| Short version journal + gotchas | `MODLOG.md` | new gotchas, versions |
| Rules for agents | `CLAUDE.md` | when a rule changes |
| Player-facing description | `README.md` | when features change |

Raw, non-git records: `build/bench_results.txt` (last bench), `%LOCALAPPDATA%\DeadlockOfRain\devshots` (test
screenshots, if the folder exists), the Claude Code transcripts under `~/.claude/projects/` (full chat history).

---

## 9. Last work: kit-page redesign + per-hero RoR2 match (2026-10-16, done)

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
