#!/bin/bash
# DPS benchmark (this PC only, dev only): deploy the freshly built plugin, start Risk of Rain 2 with the bench file, wait
# until every survivor's passes are done, print the results, then close the game and restore the lab (tools/lab_restore.sh).
#
# usage: tools/run_bench.sh [seconds-per-pass] [hero-id]
#   Needs Steam signed in. Writes %LOCALAPPDATA%\DeadlockOfRain\bench.json (removed again at the end) and the results to
#   build/bench_results.txt. 5 survivors x 6 passes; with 30 s passes it takes about 18 minutes. Gives up after 40.
#   With a hero id (haze, abrams, seven, infernus) only Commando and that hero run: about 7 minutes, for quick tuning.
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
L="$R/BepInEx/LogOutput.log"
SECS="${1:-30}"
ONLY="${2:-}"
OUT="$(cygpath -aw "$(dirname "$0")/../build")\\bench_results.txt"
B="$LOCALAPPDATA/DeadlockOfRain/bench.json"
G=$(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id"); [ -n "$G" ] && $UM win kill $G >/dev/null
for i in $(seq 1 20); do tasklist | grep -qi "Risk of Rain" || break; sleep 1; done
rm -f "$L" "$(cygpath -u "$OUT")"
mkdir -p "$LOCALAPPDATA/DeadlockOfRain"
printf '{ "out": "%s", "seconds": %s, "only": "%s" }\n' "${OUT//\\/\\\\}" "$SECS" "$ONLY" >"$B"
bash "$(dirname "$0")/dev_run.sh" /nonexistent 3 || { rm -f "$B"; exit 1; }
for i in $(seq 1 40); do tasklist | grep -qi "Risk of Rain" && break; sleep 3; done
tasklist | grep -qi "Risk of Rain" || { echo "NOT_STARTED (is Steam signed in?)"; rm -f "$B"; bash "$(dirname "$0")/lab_restore.sh" >/dev/null 2>&1; exit 1; }
for i in $(seq 1 800); do
  grep -qE "DPS bench finished|DPS bench: FAILED" "$L" 2>/dev/null && break
  tasklist | grep -qi "Risk of Rain" || { echo GAME_EXITED; break; }
  sleep 3
done
cat "$(cygpath -u "$OUT")" 2>/dev/null
grep -E "Exception" "$L" | grep -i deadlock | head -5
# close the game straight away and put the PC back the way Melty set it up
rm -f "$B"
bash "$(dirname "$0")/lab_restore.sh" >/dev/null 2>&1
echo "game processes after restore: $(tasklist | grep -ci 'risk of rain')"
