# CLAUDE.md: Deadlock of Rain (RiskofDeadlock)

A Melty mashup that adds Deadlock heroes (Haze, Abrams, Seven, Infernus) to Risk of Rain 2 as survivors. It reads
Deadlock's assets from the player's own install; nothing from either game ships.

## Read these first
1. `docs/03-PROCESS-OVERVIEW.md`: what the project is, repo layout, workflow, testing, releasing on Melty, git rules, add-a-hero checklist.
2. `docs/01-DEADLOCK-ASSETS.md`: Deadlock side: VPK paths, the extractor, the DLM format, manifest, sounds, exploration tools.
3. `docs/02-RISK-OF-RAIN-2-INTEGRATION.md`: RoR2 side: content pack, bodies, models, scale, animation and gun rig, skills, ult, buffs, hooks, test mode.

## Standing rules
- **Update the docs with every change.** Any change to code, design sheets, tools, recipe, versions or decisions must
  update the affected document(s) in `docs/` **in the same commit**: Deadlock side → 01, RoR2 side → 02, process,
  release and decisions → 03. Treat a change without its doc update as unfinished.
- **The design sheets in `design/` are the source of truth.** Edit sheets, then `python tools/gen.py` (runs
  `tools/preflight.py`; it must say `PREFLIGHT CLEAN`). Never edit `plugin/Generated/Sheets.g.cs` or `extractor/manifest.json` by hand.
- **Never ship or commit game files** (Deadlock or RoR2), and never modify the Deadlock install. Never work around anti-cheat.
- **The Melty token is secret:** only in Melty's Authorization header, never in files, commits, logs or replies.
- **Commit after each logical step; do not push unless asked.** Ask the creator before `publish` on Melty.
- **Don't launch the game for the creator's playtests.** Leave the build installed and tell them it is ready.
  Launching for automated test runs is fine; run `tools/lab_restore.sh` afterwards.
- Every skill row needs an `expect` list. A skill must measurably affect an enemy or the player, and `tools/run_test.sh` checks it in game.

## Quick commands
```
python tools/gen.py                      # preflight + generate code/manifest
dotnet build plugin -c Release           # plugin DLL
bash tools/run_test.sh                   # full in-game test on this PC (Steam must be signed in)
bash tools/lab_restore.sh                # restore the PC the way Melty set it up
python tools/package.py <version>        # release zip in build/
python tools/dlm_pose.py <hero>          # where each clip points the gun
```
