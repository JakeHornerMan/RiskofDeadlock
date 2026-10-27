#!/bin/bash
# Claude Code Stop hook (.claude/settings.json): after every Claude turn, make sure the creator's Steam copy still holds
# the dev build (creator, 2026-10-27: "EVERYTIME YOU DO A RELEASE YOU REMOVE MY MOD DEVELOPMENT ENVIRONMENT FROM MY STEAM
# CLIENT!!!"). A release test, Melty's test_release or a publish leaves the release cut behind (winhttp.dll parked as
# .melty-off, the zip's DLL, heroes.txt/coming-soon.txt, a release-manifest cache) *after* lab_restore.sh ran, and a
# "remember to put it back" note failed three times (2026-10-23, -25, -27). This checks it every turn instead.
# The build always comes from main (whichever worktree holds it), never from a release branch.
# Leaves everything alone while Risk of Rain 2 runs, while a Melty test is in progress (its test file is under 3 h old),
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
cat >/dev/null  # the hook's JSON input is not needed

[ -d "$D" ] || exit 0
[ -f "$L/keep-release-cut" ] && exit 0
for f in "$LOCALAPPDATA/Melty/test/risk-of-deadlock-2.json" "$LOCALAPPDATA/Melty/test/deadlock-of-rain.json"; do
  [ -n "$(find "$f" -mmin -180 2>/dev/null)" ] && exit 0
done
[ -n "$(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id" | tr -d '\r')" ] && exit 0

want=$(python -c "import json,sys; print(' '.join(h['id'] for h in json.load(open(sys.argv[1], encoding='utf-8'))['rows']))" "$ROOT/design/heroes.json")
why=""
[ -f "$R/winhttp.dll" ] || why="$why BepInEx-off"
[ -f "$D/coming-soon.txt" ] && why="$why coming-soon.txt"
[ "$(tr -s '\r\n' ' ' <"$D/heroes.txt" 2>/dev/null | xargs)" = "$want" ] || why="$why heroes.txt"
[ -f "$DEV" ] && ! cmp -s "$DEV" "$D/DeadlockOfRain.dll" && why="$why release-DLL"
[ -z "$why" ] && exit 0

# the dev DLL from the current source, then lab_restore puts DLL, heroes.txt, coming-soon.txt and winhttp.dll back
dotnet build "$ROOT/plugin" -c Release -v q -nologo >/dev/null 2>&1
out=$(bash "$ROOT/tools/lab_restore.sh" 2>&1)
if echo "$out" | grep -q "CACHE MISSES"; then
  echo "Steam copy had the release cut ($why); dev build put back, but $(echo "$out" | grep 'CACHE MISSES'). Rebuild the cache now with the full manifest (CLAUDE.md, Commands)." >&2
  exit 2
fi
printf '{"systemMessage":"Steam copy had the release cut (%s): dev build with every hero put back, BepInEx on."}\n' "$(echo $why)"
