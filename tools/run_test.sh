#!/bin/bash
# Full local test run (this PC only): deploy the freshly built plugin, start Risk of Rain 2 in Melty test mode,
# wait until the test finishes (or the game exits / an exception from our code appears), print the results.
#
# usage: tools/run_test.sh [heroes] [checks-file]     e.g. tools/run_test.sh infernus   (default: all four)
#   Test only the hero(es) being changed; run all four only when shared code changed (creator, 2026-10-18).
#   Needs Steam signed in. Writes the test-mode file %LOCALAPPDATA%\Melty\test\deadlock-of-rain.json and creates the
#   devshots folder (screenshots in %LOCALAPPDATA%\DeadlockOfRain\devshots). Run tools/lab_restore.sh afterwards.
#   Waits at most 20 minutes; a healthy run takes about 6-7.
#   BREAK=infernus tools/run_test.sh haze   marks a hero unreadable on purpose: the others must still build and it must get
#   a locked red ! tile with both version pairs on hover (check missing-hero-tile). Run it before every release.
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
L="$R/BepInEx/LogOutput.log"; P="$USERPROFILE/AppData/LocalLow/Hopoo Games, LLC/Risk of Rain 2/Player.log"
# a first argument made of hero ids (letters and commas) picks the heroes; anything else is the checks file
HEROES=""; if [[ "$1" =~ ^[a-z,]+$ ]]; then HEROES=", \"heroes\": \"$1\""; shift; fi
[ -n "$BREAK" ] && HEROES="$HEROES, \"break\": \"$BREAK\""
# STAGE=blackbeach tools/run_test.sh haze   moves the run to that stage first (Distant Roost broke the test spot before 0.7.2)
[ -n "$STAGE" ] && HEROES="$HEROES, \"stage\": \"$STAGE\""
# OFF=audio,harmony tools/run_test.sh haze   switches parts of the mod off (MeltyTest.Off; used by quit_test.sh to bisect)
[ -n "$OFF" ] && HEROES="$HEROES, \"off\": \"$OFF\""
# VANILLA=1: a plain Commando run instead of the hero checks (with OFF=mod: the quit test's control run)
[ -n "$VANILLA" ] && HEROES="$HEROES, \"vanilla\": true"
CHECKS="${1:-$(cygpath -aw "$(dirname "$0")/../build")\\checks.txt}"
# stop any running game first, so the old logs can be removed and never mistaken for this run
G=$(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id"); [ -n "$G" ] && $UM win kill $G >/dev/null
for i in $(seq 1 20); do tasklist | grep -qi "Risk of Rain" || break; sleep 1; done
rm -f "$L" "$P" "$(cygpath -u "$CHECKS")"
mkdir -p "$LOCALAPPDATA/Melty/test" "$LOCALAPPDATA/DeadlockOfRain/devshots"
printf '{ "checks": "%s"%s }\n' "${CHECKS//\\/\\\\}" "$HEROES" >"$LOCALAPPDATA/Melty/test/deadlock-of-rain.json"
bash "$(dirname "$0")/dev_run.sh" /nonexistent 3 || exit 1
for i in $(seq 1 40); do tasklist | grep -qi "Risk of Rain" && break; sleep 3; done
tasklist | grep -qi "Risk of Rain" || { echo "NOT_STARTED (is Steam signed in?)"; exit 1; }
for i in $(seq 1 400); do
  grep -qE "Melty test finished" "$L" 2>/dev/null && break
  grep -q "MeltyTest" "$P" 2>/dev/null && { echo EXCEPTION; break; }
  tasklist | grep -qi "Risk of Rain" || { echo GAME_EXITED; break; }
  sleep 3
done
grep -E "Melty test|Melty check" "$L" | cut -c40-460
grep -nE -B2 "MeltyTest|DeadlockOfRain\." "$P" | cut -c1-250 | head
