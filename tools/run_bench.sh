#!/bin/bash
# DPS benchmark (this PC only, dev only): deploy the freshly built plugin, start Risk of Rain 2 with the bench file, wait
# until every survivor's passes are done, print the results, then close the game and restore the lab (tools/lab_restore.sh).
#
# usage: tools/run_bench.sh [seconds-per-pass] [hero-id]
#   Needs Steam signed in. Writes %LOCALAPPDATA%\DeadlockOfRain\bench.json (removed again at the end) and the results to
#   build/bench_results.txt. Gives up after 40 minutes.
#   Each hero is preceded by its RoR2 match (design/stats.json analog: Loader, Artificer, Engineer), measured the same way.
#   5 + 3 survivors x 6 passes: about 30 minutes with 30 s passes.
#   With a hero id (haze, abrams, seven, infernus) only Commando, that hero and its match run: about 10 minutes.
#   PORTRAITS=1 also writes studio renders to build/portraits first (tools/portraits.py cuts them out for the kit pages);
#   PORTRAITS=1 tools/run_bench.sh 0 renders only (about 2 minutes).
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
L="$R/BepInEx/LogOutput.log"
SECS="${1:-30}"
ONLY="${2:-}"
PDIR=""; [ -n "$PORTRAITS" ] && PDIR="$(cygpath -aw "$(dirname "$0")/../build")\portraits"
OUT="$(cygpath -aw "$(dirname "$0")/../build")\\bench_results.txt"
B="$LOCALAPPDATA/DeadlockOfRain/bench.json"
G=$(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id"); [ -n "$G" ] && $UM win kill $G >/dev/null
for i in $(seq 1 20); do tasklist | grep -qi "Risk of Rain" || break; sleep 1; done
rm -f "$L" "$(cygpath -u "$OUT")"
mkdir -p "$LOCALAPPDATA/DeadlockOfRain"
printf '{ "out": "%s", "seconds": %s, "only": "%s", "portraits": "%s" }\n' "${OUT//\\/\\\\}" "$SECS" "$ONLY" "${PDIR//\\/\\\\}" >"$B"
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
# keep a copy of the game log (lab_restore removes it), then close the game straight away and restore the lab
cp "$L" "$(dirname "$0")/../build/bench_log.txt" 2>/dev/null
rm -f "$B"
bash "$(dirname "$0")/lab_restore.sh" >/dev/null 2>&1
echo "game processes after restore: $(tasklist | grep -ci 'risk of rain')"
