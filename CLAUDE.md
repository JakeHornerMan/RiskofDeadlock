# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Deadlock of Rain (RiskofDeadlock)

A Melty mashup that adds Deadlock heroes (Haze, Abrams, Seven, Infernus, Mo & Krill, Viscous, Rat King, Billy, Wraith) to Risk of Rain 2 as survivors. The first four
were finished in v0.6.3 (2026-10-18); Mo & Krill were added in v0.7.0 (2026-10-19), Viscous in v0.8.0 (2026-10-20), Rat King in v0.9.0 (2026-10-21), Billy and Wraith (2026-10-23) in the unreleased 0.11.0 work; new heroes follow `docs/08-NEW-HERO-PLAYBOOK.md`. It reads
Deadlock's assets from the player's own install; nothing from either game ships.

## Origin and goals (from Melty's original setup brief, as summarised by the creator)
- A real mashup: you play inside RoR2, and Deadlock supplies its real heroes, read from the player's own install. Nothing is copied out of either game.
- The creator picked "Deadlock heroes drop into RoR2": pick Haze, Seven or Abrams (Infernus was added later) from the
  survivor menu, each with their real model, gun sounds and Deadlock abilities. Solo first; **co-op (up to 4) since v0.6.0** (doc 02 §7.7).
- Options not chosen, kept as possible future ideas: a Deadlock Soul shop between teleporters (real Deadlock items stacking
  with RoR2's), both combined, or RoR2 music and sounds inside Deadlock (look-and-sound only, because of Deadlock's limits).
- Working agreement: the agent does all technical work (tools, loaders, build, package, test) and talks to the creator
  about the game only, in plain words. Ask only for a sign-in, a payment, before starting a game while the creator is at the PC,
  and before publishing. Never close a game the creator has open; ask before taking over mouse and keyboard.
- Melty listing needs a title, tagline, description, license/remix choice and a real in-game screenshot; test through
  Melty's `test_release` before asking to publish.

## Read these first
0. `docs/00-HANDOFF.md`: the current state, a paste-ready prompt for a new agent, how the creator likes to work, lessons;
   then the newest entries of `docs/07-SESSION-LOG.md` (what was asked, asked back and done, session by session).
1. `docs/03-PROCESS-OVERVIEW.md`: what the project is, repo layout, workflow, testing, releasing on Melty, git rules, add-a-hero checklist.
2. `docs/01-DEADLOCK-ASSETS.md`: Deadlock side: VPK paths, the extractor, the DLM format, manifest, sounds, exploration tools.
3. `docs/02-RISK-OF-RAIN-2-INTEGRATION.md`: RoR2 side: content pack, bodies, models, scale, animation and gun rig, skills, ult equipment, buffs, hooks, test mode. Its §12 "Gotchas" is worth reading before debugging.
4. `docs/04-ABILITY-REDESIGN-PROPOSALS.md`: the ability options and the creator's v0.3.0 picks (§6).
5. `docs/05-DAMAGE-COMPARISON.md`: DPS benchmark against Commando, balance suggestions, and the "retest needed" flags (§0).
6. `docs/06-CURRENT-ABILITIES.md`: every survivor's current abilities, numbers and DPS (keep it in sync).
7. `docs/08-NEW-HERO-PLAYBOOK.md`: **adding a new hero in one go** (the four heroes are done; this is the next job):
   intake questions, sound and voice, Unity-built effects, tests, bench, pages, definition of done.
8. `docs/09-PUBLIC-EXE-BUILD.md`: why the extractor is an exe, how GitHub builds it in public (releases `extractor-X.Y.Z`), and the DLL fallback plan.
10. `docs/10-DAILY-WATCH.md`: the daily watch (`tools/watch/`): game-update and Melty-break checks, the unattended fix run, its permissions.
`MODLOG.md` is the short journal of versions, lab notes and gotchas.

## Standing rules
- **Record every session.** For every creator request that changes anything, append an entry to
  `docs/07-SESSION-LOG.md` (template at its top: the request verbatim, questions asked and answers, what was done,
  results, what is open, lessons) and refresh `docs/00-HANDOFF.md` §1 (current state) and §9 (work in progress), in the
  same commit as the work. New general lessons also go in doc 00 §7.
- **Ask before big changes.** The creator wants research and clarifying questions first (AskUserQuestion, recommended
  option first); read free-text answers closely, they often change the plan.
- **Every artifact from this project is public** (creator, 2026-10-19: "alway make the artifacts public"). Sharing is set in the
  page's Share menu, which only the creator can use: after publishing a new page, ask them to set it to "Anyone with the link",
  and add its link to `README.md` ("Hero pages") and the top of doc 06.
- **Pages show no Deadlock art files.** The kit pages (claude.ai Artifacts) use our own in-game renders
  (`PORTRAITS=1 bash tools/run_bench.sh 0`), never hero cards or icons copied from the VPK.
- **Update the docs with every change.** Any change to code, design sheets, tools, recipe, versions or decisions must
  update the affected document(s) in `docs/` **in the same commit**: Deadlock side → 01, RoR2 side → 02, process,
  release and decisions → 03. Treat a change without its doc update as unfinished.
- **The design sheets in `design/` are the source of truth.** Edit sheets, then `python tools/gen.py` (runs
  `tools/preflight.py`; it must say `PREFLIGHT CLEAN`). Never edit `plugin/Generated/Sheets.g.cs` or `extractor/manifest.json` by hand.
  Code reads generated rows and never hard-codes a number, clip name, sound event, effect key or icon. Unused cells are `0` or `"none"`, never empty.
- **Never ship or commit game files** (Deadlock or RoR2), and never modify the Deadlock install. Never work around anti-cheat.
- **The Melty token is secret:** only in Melty's Authorization header, never in files, commits, logs or replies.
- **Commit after each logical step; do not push unless asked.** Ask the creator before `publish` on Melty. The one
  exception is the **daily watch** (`tools/watch/`, doc 03 §5b): its unattended fix run may publish a fix on its own, but
  only after every check and Melty's `test_release` pass, and only when `main` holds no unreleased work besides the fix (doc 10 §3.4).
- **Don't launch the game for the creator's playtests.** Leave the build installed and tell them it is ready.
  Launching for automated test runs is fine; run `tools/lab_restore.sh` afterwards.
- **Keep the ability docs current.** `docs/06-CURRENT-ABILITIES.md` lists every survivor's abilities with their numbers
  and measured DPS. Any change to an ability (sheet numbers, kind behaviour, passives, the ult equipment) updates doc 06
  **and** adds a row to the "Retest needed" table in `docs/05-DAMAGE-COMPARISON.md` §0, in the same commit. After
  `bash tools/run_bench.sh`, refresh the DPS in docs 05 and 06 and clear the flags that run covered. Visual-only changes need no flag.
  Then update `tools/kit_pages.py`, run it, and republish the five pages to their existing links (listed at the top of doc 06).
- **Never re-bench the base characters.** Commando and every vanilla RoR2 survivor (the heroes' matches included) are
  stored in `docs/dps-library.json` and never change. Bench only the heroes whose kit changed or that are new
  (`bash tools/run_bench.sh 30 haze,abrams`; heroes-only is the default), compare with the stored numbers, then merge with
  `python tools/dps_library.py --add build/bench_results.txt` (bump its `VERSION` so the changed heroes' old runs drop).
  `BASE=1` / `ALL=1` re-measure vanilla survivors only if the bench method itself changes, and only after asking the creator.
- Every skill row needs an `expect` list. A skill must measurably affect an enemy or the player, and `tools/run_test.sh` checks it in game.
- **Test only the heroes you changed** (`bash tools/run_test.sh <hero>`); never run every hero for a one-hero change.

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
- **Voice lines:** `design/voice.json` maps each hero's Deadlock dialogue groups (`sounds/vo/<group>_NN`) to triggers
  (select, unselect, skill, multikill, idle); the extractor exports them as `vo:<group>` events and `Runtime/Voice.cs` plays
  them for the local hero (an ult item speaks with its own hero's voice). Never list the lines on the pages.
- **Test mode** (`Runtime/MeltyTest.cs`) activates only when `%LOCALAPPDATA%\Melty\test\risk-of-deadlock-2.json` (Melty, slug since the rename) or `deadlock-of-rain.json` (our tools) exists;
  it runs each hero's skills on a fixed test spot and checks them against their `expect` lists.
- **Co-op** (`Runtime/Net.cs`, doc 02 §7.7): skill states run on every machine, so play sounds and visuals in the state; damage,
  buffs and stuns only on the server; movement and bullets only on the owner (`isAuthority`). Code that runs on the server
  alone plays its sounds and visuals through `Relay.*` (never from a state, or players get them twice). Anything the owner
  picks at the start (a lock-on target, a point) goes in the state's `OnSerialize`. Check co-op with `run_coop_test.sh`.

## Fallback system: one hero never blocks the others (creator, 2026-10-19)
"If 1 character cannot be completed we don't fail the whole system." Keep every layer of this intact:
- **Extractor** (`extractor/Program.cs`, 1.4.0+): each hero is read on its own (`<id>.tmp`, swapped in only when done); a failed
  one is logged `HERO FAILED` / `SKIPPED`, left out and listed in `ready.json` `failed`. Icons and sounds can't fail the run either.
  Exit code 1 only when Deadlock isn't found or **no** hero could be read. Progress lines (`[n/total] Reading <hero>...`,
  `<hero> ready`, final `Ready: ... | Skipped: ...`) go to stdout, which Melty's setup shows.
- **Model lookup**: the model comes from Deadlock's own `scripts/heroes.vdata` (`m_strModelName` for the sheet's `deadlockKey`)
  when it is the same file, then the sheet's `vmdl`, then the same path under the other of `heroes_staging/`/`heroes_wip/`,
  then the vdata path even if it differs. `ready.json` `modelPaths` says which one was used. `DeadlockExtract.exe --list-models` prints the vdata map.
- **Plugin**: a failed hero gets a **locked red "!" tile** in character select (`Content/MissingHeroes.cs`: never unlocked,
  hover gives the reason and both version pairs), a title-screen box, and on Melty a loading-screen summary (`FirstRun.ShowSummary`).
  Every other hero, its ult and its sounds still load. Placeholders live outside `Survivors.Bodies/Defs`.
- **Versions**: `tools/package.py` reads the creator's own RoR2 Steam build and Deadlock ClientVersion, ships them as
  `built-with.txt`, keeps `thunderstore/built-with.json`, and writes the "Built with ..." line into `README.md`,
  `thunderstore/README.md` and the version's CHANGELOG section. Every problem message shows *built with* vs *this PC* (`Runtime/BuiltWith.cs`).
  Put the same line in the Melty listing description on every publish. **Keep the listing's "Hero pages" section** (links to
  every kit page and the balance page, listed at the top of doc 06) whenever the description is rewritten (0.7.2 dropped it once). `package.py` refuses a zip when `Plugin.Version` differs or the CHANGELOG has no section.
- **Test it every release**: `BREAK=infernus bash tools/run_test.sh haze` marks a hero unreadable on purpose; the run must pass
  `missing-hero-tile` and every other check. After a publish, check Melty `install_outcomes` / `player_logs` for setup failures.

## Commands
```
python tools/gen.py                      # preflight + generate code/manifest (python tools/preflight.py to check only)
dotnet build plugin -c Release           # plugin DLL (other PC: -p:RoR2Dir="<RoR2 folder>")
bash tools/run_test.sh <heroes> [checks-file]  # in-game test of the changed heroes only, e.g. `run_test.sh infernus` (Steam signed in)
bash tools/run_coop_test.sh              # co-op test: two copies on this PC, the second joins and plays every hero (~12-15 min)
bash tools/lab_restore.sh                # restore the PC the way Melty set it up (closes every copy of the game)
bash tools/run_bench.sh [seconds] [heroes]  # DPS bench of our heroes only (vanilla numbers come from docs/dps-library.json); closes the game itself
python tools/package.py <version>        # release zip in build/
python tools/dlm_pose.py <hero> [frame] [clip-substring]   # where each clip points the gun
powershell -File tools/watch/daily.ps1 -DryRun   # daily watch: game updates + Melty breaks -> Claude fix run (doc 03 §5b)
```
- **Test only the hero(es) being changed** (creator, 2026-10-18: "just test the one being changed"): `run_test.sh infernus`,
  `run_coop_test.sh infernus`. Run all four (no argument) only when code every hero shares changed (DlrState, ModelBuilder,
  DeadlockAnimator, Survivors, Net/Relay, test mode itself), and say why before starting. A healthy run ends with every check `pass`
  and `N/N heroes`.
  Screenshots land in `%LOCALAPPDATA%\DeadlockOfRain\devshots`.
- If the manifest **adds** Deadlock content, rebuild the cache (~8 min), always passing `--manifest` (the exe otherwise
  reads its stale copy in `build/extract/`):
  `build/extract/DeadlockExtract.exe --deadlock "C:/Program Files (x86)/Steam/steamapps/common/Deadlock" --out "$LOCALAPPDATA/DeadlockOfRain/cache" --manifest extractor/manifest.json --force`
- If `extractor/*.cs` changes, re-publish the exe first (exact `dotnet publish` line in doc 01 §6.7). For a release, GitHub builds it:
  push a tag `extractor-<Program.Version>`, then `python tools/fetch_extractor.py <ver>`; `package.py --thunderstore` accepts only that exe.
- Logs: `<RoR2>/BepInEx/LogOutput.log` and `%USERPROFILE%\AppData\LocalLow\Hopoo Games, LLC\Risk of Rain 2\Player.log` (exceptions).
  Decompiled RoR2 source: `C:\Users\jakeh\ror2-decomp\ror2`.
