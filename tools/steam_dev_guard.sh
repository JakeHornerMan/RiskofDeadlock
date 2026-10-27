#!/bin/bash
# Claude Code Stop hook (.claude/settings.json): after every Claude turn, make sure the creator's Steam copy still holds
# the dev build (creator, 2026-10-27: "EVERYTIME YOU DO A RELEASE YOU REMOVE MY MOD DEVELOPMENT ENVIRONMENT FROM MY STEAM
# CLIENT!!!"). A release test, Melty's test_release or a publish leaves the release cut behind (winhttp.dll parked as
# .melty-off, the zip's DLL, heroes.txt/coming-soon.txt, a release-manifest cache) *after* lab_restore.sh ran, and a
# "remember to put it back" note failed three times (2026-10-23, -25, -27). This checks it every turn instead.
# The build always comes from main (whichever worktree holds it), never from a release branch.
# Runs two ways: as the Stop hook, and every 5 minutes as the Windows task "DeadlockOfRain dev guard"
# (tools/steam_dev_watch.vbs, hidden), because the Melty app puts its release back the moment it starts (2026-10-27
# 19:38, no Claude session open). A cache missing a hero is rebuilt in the background with main's full manifest.
# Every action goes to $LOCALAPPDATA/DeadlockOfRain/steam-dev-guard.log.
# Leaves everything alone while Risk of Rain 2 runs, while DeadlockExtract runs (Melty's setup or ours), while a Melty test is in progress (its test file is under 3 h old),
# or while $LOCALAPPDATA/DeadlockOfRain/keep-release-cut exists (create it to test the release cut across turns; delete it after).
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
D="$R/BepInEx/plugins/DeadlockOfRain"
# the dev build is main's, wherever main is checked out (a release branch can sit in the usual folder; 1.0.2 did)
HERE="$(cd "$(dirname "$0")/.." && pwd)"
ROOT=$(git -C "$HERE" worktree list --porcelain | awk '/^worktree /{w=substr($0,10)} /^branch refs\/heads\/main$/{print w}')
[ -n "$ROOT" ] || ROOT="$HERE"
ROOT=$(cygpath -u "$ROOT" 2>/dev/null || echo "$ROOT")
DEV="$ROOT/plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll"
L="$LOCALAPPDATA/DeadlockOfRain"
[ -t 0 ] || timeout 2 cat >/dev/null  # the hook's JSON input is not needed (the task has none)

[ -d "$D" ] || exit 0
[ -f "$L/keep-release-cut" ] && exit 0
for f in "$LOCALAPPDATA/Melty/test/risk-of-deadlock-2.json" "$LOCALAPPDATA/Melty/test/deadlock-of-rain.json"; do
  [ -n "$(find "$f" -mmin -180 2>/dev/null)" ] && exit 0
done
running=$(powershell -NoProfile -Command "Get-Process -Name 'Risk of Rain 2','DeadlockExtract' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Id" | tr -d '')
[ -n "$running" ] && exit 0

want=$(python -c "import json,sys; print(' '.join(h['id'] for h in json.load(open(sys.argv[1], encoding='utf-8'))['rows']))" "$ROOT/design/heroes.json")
miss=$(python -c "import json,sys,os; f=sys.argv[1]; have=json.load(open(f)).get('heroes',[]) if os.path.exists(f) else []
print(' '.join(h for h in sys.argv[2].split() if h not in have))" "$L/cache/ready.json" "$want")
why=""
[ -f "$R/winhttp.dll" ] || why="$why BepInEx-off"
[ -f "$D/coming-soon.txt" ] && why="$why coming-soon.txt"
[ "$(tr -s '
' ' ' <"$D/heroes.txt" 2>/dev/null | xargs)" = "$want" ] || why="$why heroes.txt"
[ -f "$DEV" ] && ! cmp -s "$DEV" "$D/DeadlockOfRain.dll" && why="$why release-DLL"
[ -n "$miss" ] && why="$why cache-misses($miss)"
[ -z "$why" ] && exit 0
log() { echo "$(date '+%F %T') $*" >>"$L/steam-dev-guard.log"; }
log "release cut found:$why"

# the dev DLL from the current source, then lab_restore puts DLL, heroes.txt, coming-soon.txt and winhttp.dll back
dotnet build "$ROOT/plugin" -c Release -v q -nologo >/dev/null 2>&1
bash "$ROOT/tools/lab_restore.sh" >/dev/null 2>&1
msg="dev build with every hero put back, BepInEx on"
if [ -n "$miss" ]; then
  # 5-8 minutes: in the background, so the hook returns at once; the next check sees DeadlockExtract and waits
  (cd "$ROOT" && nohup build/extract/DeadlockExtract.exe --deadlock "C:/Program Files (x86)/Steam/steamapps/common/Deadlock"     --out "$L/cache" --manifest extractor/manifest.json --force >"$L/steam-dev-guard-extract.log" 2>&1 &)
  msg="$msg; the hero cache is being rebuilt for every hero (5-8 min)"
fi
log "$msg"
printf '{"systemMessage":"Steam copy had the release cut (%s): %s."}
' "$(echo $why)" "$msg"
