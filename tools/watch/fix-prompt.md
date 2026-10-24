You are the unattended daily watch for Risk of Deadlock (this repo), started by `tools/watch/daily.ps1` on the
creator's PC on {{DATE}}. Nobody is at the PC. You are on the git branch `{{BRANCH}}`, made from a clean `main`.

## What changed
{{WHAT}}

## Your job
Find out whether the mod still works with this, fix it if it doesn't, test it, and publish the fix on Melty.
Publish allowed: {{PUBLISH}}

Follow CLAUDE.md in full (design sheets are the source, docs updated in the same commit, the Melty token never written
anywhere). First read `docs/00-HANDOFF.md` §1 and §7, `docs/02-RISK-OF-RAIN-2-INTEGRATION.md` §12, `docs/03-PROCESS-OVERVIEW.md`
§5–§6 and the doc 00 entry "Deadlock update 6795 broke the heroes" (the last time a Deadlock update broke the mod).

Rules for this run:
- Run each command as one plain command with literal paths: no `$VARS`, no `cd x &&`. Pipes into read-only tools
  (grep, head, tail, sort) are fine. A refused command means it isn't in `tools/watch/claude-settings.json`; say so in the report.
- Never open a game while another Steam game is running (`reg query HKCU\Software\Valve\Steam /v RunningAppID` must be 0).
  If the creator starts a game while you work, stop testing, write the report and finish.
- After every game launch run `bash tools/lab_restore.sh`.
- The lab scripts never close a copy of Risk of Rain 2 they didn't start (`tools/game_guard.sh`). If one stops with `GAME_OPEN`,
  the creator is playing: never use `FORCE_CLOSE=1`; stop testing, write the report and finish.
- Don't push to `main` and never force-push. The only push allowed is a tag `extractor-<Program.Version>` when
  `extractor/*.cs` changed (GitHub builds the exe that Melty needs; then `python tools/fetch_extractor.py <ver>`).
- Don't change balance, abilities or anything a player would notice beyond the fix.
- Never close the creator's programs to free memory. The co-op test (`run_coop_test.sh`, two copies of the game) needs
  about 5 GB free; run it only if the fix touched co-op code (`Net.cs`, `Relay`, a state's server/owner split) and
  enough memory is free, otherwise say in the report that it is owed.

Steps:
1. **If Deadlock changed:** refresh the Deadlock listings in `tools/dev/` with `python tools/dev_listings.py` (doc 01 §8; it
   prints what changed), run `python tools/preflight.py`, then
   rebuild the cache:
   `build/extract/DeadlockExtract.exe --deadlock "C:/Program Files (x86)/Steam/steamapps/common/Deadlock" --out "C:/Users/jakeh/AppData/Local/DeadlockOfRain/cache" --manifest extractor/manifest.json --force`
   and look for `HERO FAILED`, `SKIPPED` and `ready.json` `failed`.
2. **If Risk of Rain 2 changed:** `dotnet build plugin -c Release`; if it fails, regenerate the decompiled source
   (doc 03 §5) and compare the APIs the plugin uses.
3. **If Melty breaks were reported:** read them, reproduce from the logs, find the cause. Only breaks on the live version count.
4. Test: `bash tools/run_test.sh` (every hero, because a game update touches all of them; for a Melty break that
   concerns one hero, that hero only), then `BREAK=infernus bash tools/run_test.sh haze` (the fallback check),
   then `bash tools/lab_restore.sh`. A healthy run ends with every check `pass` and `N/N heroes`. These dev runs load
   every sheet hero (Rat King too); the release check in step 7 uses the Melty cut.
5. **If everything passes and nothing needed fixing:** commit nothing but the report (below); don't publish.
6. **If something failed:** fix it, rebuild, re-test until every check passes. Commit after each logical step.
7. **Publish (only if allowed above and every check passed after a fix):**
   - **First make sure you would ship only the fix.** Find the commit that set `plugin/Plugin.cs` `Version` to Melty's live
     version (`mod_status` `liveVersion`; `git log -S'Version = "<live>"' -- plugin/Plugin.cs`). If `main` has commits after
     it that change `plugin/`, `design/` or `extractor/` and aren't yours from this run, that is the creator's unreleased
     work: **don't publish**; leave the fix on the branch and say so in the report (the first night, 2026-10-22, did exactly
     this because the Seven redesign was unreleased).
   - Then follow doc 03 §6 exactly (the 0.9.1 entry there is the latest reference). Bump the patch version (`plugin/Plugin.cs`
     and a `thunderstore/CHANGELOG.md` section), `python tools/package.py <ver> --melty` (same cut as the live release:
     `thunderstore/release.json` `melty` and `comingSoon`).
   - Test the zip as players get it: `RELEASE_ZIP=build/DeadlockOfRain-<ver>.zip bash tools/run_test.sh` and
     `RELEASE_ZIP=build/DeadlockOfRain-<ver>.zip BREAK=infernus bash tools/run_test.sh haze`, then `lab_restore`.
   - Recipe: the last one with the new fileName and `setup.done.contains` (`|<extractor ver>|<first 16 hex of the SHA-256 of
     the zip's extract/manifest.json>`), and `test.checks` = **the first 12 lines of `build/checks.txt`** from the release run,
     in that order (Melty reads only the first 12). `inspect_package`, `validate_recipe`, `start_upload` -> curl PUT ->
     `finish_upload`, `submit_release`.
   - `test_release` (2 runs, 12 minutes). Nobody may touch the PC; it plays itself. Poll `test_result`. After each run, look at
     its recording with `test_footage` (both parts) and give `test_review` with frames you looked at: a run only counts with a
     passing review. If the test fails, fix and test again, or stop without publishing.
   - Listing: edit `thunderstore/melty-description.md` (keep "Hero pages", set the "Built with" line to the new version and
     builds) and pass it to `update_mod`. Then `publish` and `mod_status` ("in review" is not "live").
   - Then `python tools/discord_update.py <version>` posts it to the Discord #update channel (CLAUDE.md). Never print the webhook link.
   - For a Melty break, `report_bug_fix` with its issue key after publishing.
8. For every Melty break you handled (fixed, or found not to be ours), append its `signature` (from the triage JSON above) to
   `build/watch/known-breaks.txt` as `<signature> | <date> <what happened>: <fixed on main / released in X / not ours, why>`.
   A break with a listed signature never starts a fix run again. If a signature was wrong (two causes share it), make the
   line more specific and say so in the report.
9. Record it: append an entry to `docs/07-SESSION-LOG.md` (Request: "daily watch {{DATE}}: <what changed>"), refresh
   `docs/00-HANDOFF.md` §1 and §9, update MODLOG and any doc the fix touched, in the same commit as the work.

## Report (always, even when you stop early)
Write `build/watch/{{DATE}}-report.md`. Its **first line** is a one-line summary for a Windows notification (for example
`# Deadlock 6810: all 6 heroes pass, nothing to fix` or `# Deadlock 6810 broke Haze's model: fixed in 0.9.1, published`).
Below it: what changed, what you checked, what broke and why, what you fixed (commits), the test results, the Melty
release id and publish state, and what the creator still has to do (merge `{{BRANCH}}` into `main`, anything left open).
If `gh` is installed and signed in (`gh auth status`), also open a GitHub issue with the same text
(`gh issue create --title "<first line>" --body-file build/watch/{{DATE}}-report.md`).
