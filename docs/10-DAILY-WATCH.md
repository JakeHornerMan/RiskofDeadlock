# 10: The daily watch (game updates and Melty breaks)

> Since 2026-10-21. The short version is in doc 03 §5b; this document is the full manual.
> Code: `tools/watch/`. State and reports: `build/watch/` (gitignored, so each PC has its own).

## 1. What it is for
The mod depends on two games that update without warning. A Deadlock patch can change the files the extractor reads
(it happened on 2026-10-19: build 6795 broke every hero for Melty players). A Risk of Rain 2 patch can break the plugin.
Players' problems also show up on Melty (`install_outcomes`, `player_logs`, `player_reports`) before anyone tells us.

The watch checks for these things every day. If it finds one, it starts an unattended Claude Code session that checks the
mod, fixes it, tests it and publishes the fix to Melty.

The creator's request (2026-10-21): "is there a way we can tell is the deadlock or risk of rain game has been updated?
can we run this every day? and if deadlock update can we start a claude instancce that check files that will validate our
mod still works". Their answers: run it on the **PC only**; also check Melty for breaks "relavent to the current
version"; use their **Claude subscription**; it may **start the game when no Steam game is running**; and "**push the fix
to melty when you are done!**"

### Why it runs on the PC and not as a GitHub Action
GitHub's machines can't have Deadlock: it is a 40 GB game tied to a Steam account, and we never copy game files anywhere.
The real question ("can we still read every hero from the new Deadlock files, and do they still play?") can only be
answered on a PC that has both games installed. A GitHub Action could only spot an update, and the PC can do that by
itself by reading Steam's own files. So everything runs on the creator's PC, under their own `claude` login. No repo
secret and no Action are needed.

## 2. How a day goes

```
04:00  Task Scheduler starts tools/watch/daily.ps1 (hidden window)
        │
        ├─ another watch already running (build/watch/lock)? ─────────────► stop
        ├─ any Steam game running, or RoR2 / Deadlock open? ───────────────► stop, try tomorrow
        ├─ a game still updating in Steam (StateFlags ≠ 4)? ───────────────► stop, try tomorrow
        │
        ├─ read build numbers ──► compare with build/watch/state.json ──► "changes"
        │
        ├─ Melty triage (Claude Haiku, read-only Melty tools, ~2 cents) ──► "breaks"
        │
        ├─ no changes and no breaks ──► save lastChecked / meltySeenUntil ──► stop ("all clear")
        │
        ├─ repo not a clean `main`? ──► notification "needs checking, repo has uncommitted work" ──► stop
        │
        └─ fix run: git checkout -b watch/<date>
              claude -p fix-prompt.md (full model, allow-listed tools)
                 check → fix → test → (publish) → record → report
              back to main; branch deleted if it has no commits
              save the new build numbers
              Windows notification with the report's first line
```

## 3. The steps in detail

### 3.1 Guards
- **Lock:** `build/watch/lock` holds the running PID. A lock younger than 12 hours stops a second run, and an older one
  is treated as stale. The lock is removed in a `finally`, so it goes even when the script exits early.
- **No game running:** `HKCU\Software\Valve\Steam` → `RunningAppID` is non-zero while **any** Steam game runs (the
  creator: "if the game isn't open or anyother game on my steam"). The script also checks for `Risk of Rain 2` and
  `deadlock` processes.
- **Fully installed:** `StateFlags` in each game's `appmanifest_*.acf` must be `4`. Any other value means Steam is
  downloading or has an update queued, so the files on disk are half old and half new.

### 3.2 Build numbers
| Game | Steam app id | File read | Value |
|---|---|---|---|
| Risk of Rain 2 | 632360 | `steamapps/appmanifest_632360.acf` | `buildid` |
| Deadlock | 1422450 | `steamapps/appmanifest_1422450.acf` | `buildid` |
| Deadlock | | `steamapps/common/Deadlock/game/citadel/steam.inf` | `ClientVersion` (the number `package.py` writes as "Built with ... Deadlock build N") |

The Steam `buildid` is what changes on every patch. Deadlock's `ClientVersion` is kept beside it because that is the
number the rest of the project, and players, talk about (6795, 6807, ...).

`build/watch/state.json`:
```json
{ "ror2": "25921972", "deadlock": "26097978", "deadlockClient": "6807",
  "meltySeenUntil": "2026-10-21T14:30:00.0000000Z", "lastChecked": "2026-10-21" }
```
If the file is missing, the first run writes today's values as the baseline, and that run reports no game change. It was
seeded on 2026-10-21 with the values above.

### 3.3 Melty triage
- `claude -p tools/watch/triage-prompt.md --model haiku --max-turns 20`
- Allowed tools: `list_releases`, `mod_status`, `install_outcomes`, `player_logs`, `player_reports` (read only; it
  can't publish or change anything).
- `--output-format json --json-schema tools/watch/triage-schema.json`: the answer is read from `structured_output` as
  `{liveVersion, liveReleaseId, breaks:[{source,id,when,deadlockBuild,summary,detail}], summary}` and saved to
  `build/watch/triage.json`.
- **Only breaks that matter now:** entries for the **live (published) release version** that are newer than
  `meltySeenUntil`. Old versions, drafts and Melty test runs are ignored. A "break" is a failed or partial setup,
  `HERO FAILED` or `SKIPPED`, a DeadlockOfRain exception, a crash, missing heroes, or a player report of a fault.
- If triage fails (no answer, no network), the script logs it, carries on as if there were no breaks, and leaves
  `meltySeenUntil` alone, so the next day looks at the same window again.
- First real run (2026-10-21): live 0.7.4, no breaks. The 0.6.3 setup failures and the 0.7.2/0.7.3 crashes were correctly
  left out. Cost: 1.6 cents.

### 3.4 Fix run
It only starts when there is a game change or a Melty break, and only from a clean `main` (no uncommitted files). That
rule keeps the creator's own work in progress out of the watch's commits.

- `git checkout -b watch/<date>`
- `claude -p <fix-prompt.md filled in> --settings tools/watch/claude-settings.json --max-turns 400`
- The prompt is filled in with the date, the branch, **what changed** (old → new builds and the triage breaks as JSON),
  and whether publishing is allowed.

What the prompt tells Claude to do:
1. Read CLAUDE.md, doc 00 §1/§7, doc 02 §12, doc 03 §5–§6, and the doc 00 entry about the 6795 break (the last time this happened).
2. **Deadlock changed:** refresh the `tools/dev` listings (doc 01 §8) and run `python tools/preflight.py`. Then rebuild the
   cache with the extractor (`--manifest extractor/manifest.json --force`) and look for `HERO FAILED`, `SKIPPED` and
   `ready.json` `failed`.
3. **RoR2 changed:** `dotnet build plugin -c Release`. If the build fails, decompile RoR2 again and compare the APIs.
4. **Melty breaks:** read the logs and find the cause (live version only).
5. Test: `bash tools/run_test.sh` (every hero for a game update; only the affected hero for a one-hero Melty break), then
   `BREAK=infernus bash tools/run_test.sh haze` (the fallback system), then `bash tools/lab_restore.sh`.
6. Everything passes and nothing needs fixing: no release, just the report.
7. Something failed: fix it, rebuild and re-test until every check passes, committing after each step.
8. **Publish** (only if allowed and every check passed): doc 03 §6 step by step. Bump the patch version and add a CHANGELOG
   section, run `package.py <ver> --melty` (same cut as the live release), then `inspect_package` → recipe →
   `validate_recipe` → upload → `submit_release` → `test_release` (2 runs) → `test_result` → `publish` → `mod_status`.
   Keep "Hero pages" and the "Built with" line in the listing. Use `report_bug_fix` for a Melty break.
9. Record it: a doc 07 entry ("daily watch <date>: ..."), doc 00 §1/§9, MODLOG and any doc the fix touched, all in the same commit.
10. **Report:** always write `build/watch/<date>-report.md`, even after stopping early. Its first line is the notification
    text. If `gh` is installed and signed in, also open a GitHub issue with the same text.

When Claude finishes, the script reads the report's first line, goes back to `main` (if the branch is clean), deletes
the branch if it has no commits, saves the new build numbers and `meltySeenUntil`, and shows the notification.
`meltySeenUntil` moves on even when the fix failed, so an unfixable break doesn't start a new fix run every day. The
report says what is still open.

## 4. What the fix run may and may not do
Permissions come from `tools/watch/claude-settings.json`. In `claude -p` mode, a tool that isn't allowed is simply refused.
Nothing uses `--dangerously-skip-permissions`.

| Allowed | Denied |
|---|---|
| Read, Edit, Write, Glob, Grep | `git push origin main`, `git push --force` / `-f` |
| `bash tools/run_test.sh`, `lab_restore.sh`, `run_coop_test.sh`, `BREAK=infernus bash tools/run_test.sh` | `git reset --hard`, `git checkout main` (the script does that) |
| `python tools/*`, `python -c`, `dotnet build`/`publish`, the extractor exe, `ilspycmd`, `um win` | `rm -rf` |
| `git status/diff/log/show/add/commit`, `git tag extractor-*`, `git push origin extractor-*` | Melty `unpublish`, `rollback_release`, `delete_draft`, `set_price` |
| `curl -X PUT` (the Melty upload), `gh auth status`, `gh issue create`, `reg query` | `publish` too, when run with `-NoPublish` |
| every Melty tool (`mcp__melty`) except the denied ones | |

**Standing permissions (creator, 2026-10-21), for the watch only:**
- It may **publish** a fix on Melty without asking, but only after every in-game check **and** Melty's `test_release`
  pass. Every other session still asks first (CLAUDE.md).
- It may **start Risk of Rain 2** for its tests when no Steam game is running, and always runs `lab_restore` afterwards.
- The only push it may make is an `extractor-<ver>` tag, when the extractor changed and Melty needs the GitHub-built exe
  (doc 09). Fixes stay on the `watch/<date>` branch until the creator merges them.

## 5. Files

| File | What it is |
|---|---|
| `tools/watch/daily.ps1` | The script: guards, build numbers, triage, fix run, notification |
| `tools/watch/install.ps1` | Registers or removes the scheduled task |
| `tools/watch/triage-prompt.md` | Instructions for the Melty triage (`{{SINCE}}` filled in) |
| `tools/watch/triage-schema.json` | The JSON shape the triage must answer in |
| `tools/watch/fix-prompt.md` | Instructions for the fix run (`{{DATE}}`, `{{BRANCH}}`, `{{WHAT}}`, `{{PUBLISH}}`) |
| `tools/watch/claude-settings.json` | The fix run's allow/deny list |
| `build/watch/state.json` | Last seen builds, `meltySeenUntil`, `lastChecked` |
| `build/watch/<date>.log` | Everything the script and both Claude runs printed |
| `build/watch/triage.json` | The last triage answer |
| `build/watch/<date>-report.md` | The fix run's report (first line = notification) |
| `build/watch/lock` | Present while a run is going |

## 6. Using it

Install, change or remove the daily task (Windows PowerShell):
```
powershell -File tools/watch/install.ps1              # daily 04:00 (default)
powershell -File tools/watch/install.ps1 -At 13:30    # another time
powershell -File tools/watch/install.ps1 -Uninstall
Get-ScheduledTask "DeadlockOfRain daily watch" | Get-ScheduledTaskInfo   # last/next run, last result
Start-ScheduledTask "DeadlockOfRain daily watch"                          # run it now
```
The task: "DeadlockOfRain daily watch", runs as the logged-on user (interactive, so the game can start), starts
missed runs when the PC is next on, never runs twice at once, and is stopped after 6 hours.

Run the script by hand:
```
powershell -File tools/watch/daily.ps1 -DryRun      # guards and build numbers only, no Claude
powershell -File tools/watch/daily.ps1 -SkipMelty   # builds only, no triage
powershell -File tools/watch/daily.ps1 -NoPublish   # full run, but publish is denied
powershell -File tools/watch/daily.ps1              # what the task runs
```

**Force a practice fix run:** set `"deadlock"` in `build/watch/state.json` to an older number, make sure `main` is clean
and nothing is running, then `daily.ps1 -NoPublish`. Afterwards check `build/watch/<date>.log` and the report, and the
`watch/<date>` branch if one was left. The next real run puts the right number back.

**After a run that left a branch:** read the report, then `git merge watch/<date>` into `main` (or ask Claude to review it
first). If it published, Melty already has that version.

## 7. Troubleshooting

| Symptom | Cause and fix |
|---|---|
| Log says "a game is running (Steam app N)" every day | A game was open at 04:00 or when the PC started. Normal; it tries again tomorrow. Move the time with `install.ps1 -At`. |
| "a game is still updating in Steam" | Steam has an update queued or downloading (StateFlags ≠ 4). Let Steam finish. |
| Notification "repo has uncommitted work" | `main` had changes or another branch was checked out. Commit or switch to `main`; the next day's run picks the change up, because the state isn't updated. |
| "Melty triage gave no readable answer" | No network, Melty down, or the Claude login expired (`claude` in a terminal to sign in again). The same window is checked again next day. |
| Triage answers in prose instead of JSON | Melty's MCP instructions make Claude write a report; that is why the schema flag is there. If it comes back, check that `--json-schema` is still passed and that `structured_output` exists. |
| Fix run stops early, or the log shows refused tools | The command wasn't in the allow list (`claude-settings.json`). Add the narrowest rule that covers it. |
| Nothing happens at 04:00 | The PC was off or the user was logged out (the task runs only while logged on). It runs at the next logon. Check `Get-ScheduledTaskInfo` → `LastTaskResult`. |
| Stale `build/watch/lock` | A run crashed hard. It is ignored after 12 h, or delete it. |

Windows PowerShell 5.1 gotchas found while building it:
- Native arguments lose bare `"`, so the JSON schema is passed with `\"` escapes.
- With `$ErrorActionPreference = 'Stop'`, git's normal stderr ("Switched to branch") becomes an error under `2>&1`, so the
  script uses `Continue`.

## 8. Limits and open items
- **The fix run has not run end to end yet** (on 2026-10-21 the creator was playing). Do the practice run in §6 once.
- It runs only while the PC is on and the creator is logged on. A patch on a day the PC is off is caught on the next run.
- It only sees updates Steam has installed on this PC. If Steam auto-update is off, the watch won't know about a patch.
- `gh` isn't installed, so there are no GitHub issues yet (`winget install GitHub.cli`, `gh auth login`).
- Thunderstore isn't covered: the fix run publishes to Melty only.
- Cost: the triage is about 2 cents a day. A fix run is a full Claude session (game tests, perhaps a Melty test) and counts
  against the creator's plan like any long session.
