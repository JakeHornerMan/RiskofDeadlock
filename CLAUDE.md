# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Deadlock of Rain (RiskofDeadlock)

A Melty mashup that adds Deadlock heroes (Haze, Abrams, Seven, Infernus) to Risk of Rain 2 as survivors. It reads
Deadlock's assets from the player's own install; nothing from either game ships.

## Origin and goals (from Melty's original setup brief, as summarised by the creator)
- A real mashup: you play inside RoR2, and Deadlock supplies its real heroes, read from the player's own install. Nothing is copied out of either game.
- The creator picked "Deadlock heroes drop into RoR2": pick Haze, Seven or Abrams (Infernus was added later) from the
  survivor menu, each with their real model, gun sounds and Deadlock abilities. Solo first; **co-op with friends planned for later**.
- Options not chosen, kept as possible future ideas: a Deadlock Soul shop between teleporters (real Deadlock items stacking
  with RoR2's), both combined, or RoR2 music and sounds inside Deadlock (look-and-sound only, because of Deadlock's limits).
- Working agreement: the agent does all technical work (tools, loaders, build, package, test) and talks to the creator
  about the game only, in plain words. Ask only for a sign-in, a payment, before starting a game while the creator is at the PC,
  and before publishing. Never close a game the creator has open; ask before taking over mouse and keyboard.
- Melty listing needs a title, tagline, description, license/remix choice and a real in-game screenshot; test through
  Melty's `test_release` before asking to publish.

## Read these first
1. `docs/03-PROCESS-OVERVIEW.md`: what the project is, repo layout, workflow, testing, releasing on Melty, git rules, add-a-hero checklist.
2. `docs/01-DEADLOCK-ASSETS.md`: Deadlock side: VPK paths, the extractor, the DLM format, manifest, sounds, exploration tools.
3. `docs/02-RISK-OF-RAIN-2-INTEGRATION.md`: RoR2 side: content pack, bodies, models, scale, animation and gun rig, skills, ult equipment, buffs, hooks, test mode. Its §12 "Gotchas" is worth reading before debugging.
4. `docs/04-ABILITY-REDESIGN-PROPOSALS.md`: the ability options and the creator's v0.3.0 picks (§6).
5. `docs/05-DAMAGE-COMPARISON.md`: DPS benchmark against Commando, balance suggestions, and the "retest needed" flags (§0).
6. `docs/06-CURRENT-ABILITIES.md`: every survivor's current abilities, numbers and DPS (keep it in sync).
`MODLOG.md` is the short journal of versions, lab notes and gotchas.

## Standing rules
- **Update the docs with every change.** Any change to code, design sheets, tools, recipe, versions or decisions must
  update the affected document(s) in `docs/` **in the same commit**: Deadlock side → 01, RoR2 side → 02, process,
  release and decisions → 03. Treat a change without its doc update as unfinished.
- **The design sheets in `design/` are the source of truth.** Edit sheets, then `python tools/gen.py` (runs
  `tools/preflight.py`; it must say `PREFLIGHT CLEAN`). Never edit `plugin/Generated/Sheets.g.cs` or `extractor/manifest.json` by hand.
  Code reads generated rows and never hard-codes a number, clip name, sound event, effect key or icon. Unused cells are `0` or `"none"`, never empty.
- **Never ship or commit game files** (Deadlock or RoR2), and never modify the Deadlock install. Never work around anti-cheat.
- **The Melty token is secret:** only in Melty's Authorization header, never in files, commits, logs or replies.
- **Commit after each logical step; do not push unless asked.** Ask the creator before `publish` on Melty.
- **Don't launch the game for the creator's playtests.** Leave the build installed and tell them it is ready.
  Launching for automated test runs is fine; run `tools/lab_restore.sh` afterwards.
- **Keep the ability docs current.** `docs/06-CURRENT-ABILITIES.md` lists every survivor's abilities with their numbers
  and measured DPS. Any change to an ability (sheet numbers, kind behaviour, passives, the ult equipment) updates doc 06
  **and** adds a row to the "Retest needed" table in `docs/05-DAMAGE-COMPARISON.md` §0, in the same commit. After
  `bash tools/run_bench.sh`, refresh the DPS in docs 05 and 06 and clear the flags that run covered. Visual-only changes need no flag.
  Then update `tools/kit_pages.py`, run it, and republish the five pages to their existing links (listed at the top of doc 06).
- Every skill row needs an `expect` list. A skill must measurably affect an enemy or the player, and `tools/run_test.sh` checks it in game.

## Architecture (big picture)
Three pieces, linked by generated data:
- **`design/*.json` → `tools/gen.py`** generates `plugin/Generated/Sheets.g.cs` (one `*Row` class per sheet, static `All`)
  and `extractor/manifest.json` (what to pull from Deadlock). Preflight checks cells, cross-references, slots, balance
  math (gun DPS vs `targetDps`, equipment ults at least Preon level), scale, and Deadlock/RoR2 names against listings in the gitignored `tools/dev/`
  (a missing listing is a failure; on a fresh clone generate them per doc 01 §8 / doc 02 §1).
- **`extractor/`** (DeadlockExtract, .NET 10 + ValveResourceFormat): Melty runs it once on the player's PC; it converts
  hero models/clips (to the custom DLM format, `DlmWriter.cs`), icons and sounds from the Deadlock VPK into
  `%LOCALAPPDATA%\DeadlockOfRain\cache` and writes `ready.json` last.
- **`plugin/`** (BepInEx 5, netstandard2.1, RoR2's own ContentManager API, no R2API): reads the cache
  (`Runtime/DeadlockCache.cs`), builds Commando-clone bodies and models (`Content/Survivors.cs`, `Runtime/ModelBuilder.cs`),
  plays Deadlock clips and the gun rig (`Runtime/DeadlockAnimator.cs`), and plays sound through its own waveOut mixer
  because RoR2 disables Unity audio. Sheet access goes through `Runtime/SheetLookup.cs` (`Sheet.*`).
- **Skills:** each `skills.json` row has a `kind`; it maps to an EntityState class tagged `[Kind("...")]` in
  `plugin/States/` (guns, abilities, ultimates, all deriving from `DlrState`). Preflight requires every kind to have a state.
  Four skill slots per hero: `primary`, `secondary`, `utility`, `special` (R, a normal-cooldown move), plus `equipment`: the
  hero's Deadlock ultimate is a RoR2 equipment (Q, 120 s, `Content/Equipment.cs`, rows in `design/equipment.json`) that the
  hero starts holding and anyone can find; firing it starts the ult's state on whatever body holds it (`DlrState.RowOverride`).
  Buff effects live in Harmony patches in `Content/Buffs.cs`; every hook is also listed in `design/hooks.json` (keep in sync).
- **Test mode** (`Runtime/MeltyTest.cs`) activates only when `%LOCALAPPDATA%\Melty\test\deadlock-of-rain.json` exists;
  it runs each hero's skills on a fixed test spot and checks them against their `expect` lists.

## Commands
```
python tools/gen.py                      # preflight + generate code/manifest (python tools/preflight.py to check only)
dotnet build plugin -c Release           # plugin DLL (other PC: -p:RoR2Dir="<RoR2 folder>")
bash tools/run_test.sh [checks-file]     # full in-game test on this PC (Steam must be signed in; ~6-7 min)
bash tools/lab_restore.sh                # restore the PC the way Melty set it up (closes the game)
bash tools/run_bench.sh [seconds]        # DPS benchmark vs Commando (docs/05); closes the game itself
python tools/package.py <version>        # release zip in build/
python tools/dlm_pose.py <hero> [frame] [clip-substring]   # where each clip points the gun
```
- There is no per-skill test: `run_test.sh` runs all heroes; a healthy run ends with every check `pass` and `4/4 heroes`.
  Screenshots land in `%LOCALAPPDATA%\DeadlockOfRain\devshots`.
- If the manifest **adds** Deadlock content, rebuild the cache (~8 min), always passing `--manifest` (the exe otherwise
  reads its stale copy in `build/extract/`):
  `build/extract/DeadlockExtract.exe --deadlock "C:/Program Files (x86)/Steam/steamapps/common/Deadlock" --out "$LOCALAPPDATA/DeadlockOfRain/cache" --manifest extractor/manifest.json --force`
- If `extractor/*.cs` changes, re-publish the exe first (exact `dotnet publish` line in doc 01 §6.7).
- Logs: `<RoR2>/BepInEx/LogOutput.log` and `%USERPROFILE%\AppData\LocalLow\Hopoo Games, LLC\Risk of Rain 2\Player.log` (exceptions).
  Decompiled RoR2 source: `C:\Users\jakeh\ror2-decomp\ror2`.
