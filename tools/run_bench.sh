#!/bin/bash
# DPS benchmark (this PC only, dev only): deploy the freshly built plugin, start Risk of Rain 2 with the bench file, wait
# until every survivor's passes are done, print the results, then close the game and restore the lab (tools/lab_restore.sh).
#
# usage: tools/run_bench.sh [seconds-per-pass] [hero-ids]
#   Needs Steam signed in. Writes %LOCALAPPDATA%\DeadlockOfRainench.json (removed again at the end) and the results to
#   build/bench_results.txt. Gives up after 40 minutes.
#   By default ONLY OUR HEROES run (no Commando, no RoR2 matches): the vanilla survivors are stored in docs/dps-library.json
#   and never re-measured. Compare with the library, then merge: python tools/dps_library.py --add build/bench_results.txt.
#   hero-ids: comma-separated (e.g. haze,abrams); bench only the heroes whose kit changed. 4 heroes x 6 passes: ~15 minutes.
#   BASE=1 also runs Commando and each hero's RoR2 match (old behaviour; only if the bench method itself changed).
#   ALL=1 benches every playable survivor (rebuilds the DPS library; ~1 hour per run); REPEAT=n repeats the whole set n
#   times in one session. tools/dps_library.py turns the results into docs/dps-library.json.
#   PORTRAITS=1 also writes studio renders to build/portraits first (tools/portraits.py cuts them out for the kit pages);
#   PORTRAITS=1 tools/run_bench.sh 0 renders only (about 2 minutes).
#   ULTS=1 tools/run_bench.sh 30: the ult bench instead (each hero's Q once against 30 enemies + a Beetle Queen, ~10 minutes).
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
L="$R/BepInEx/LogOutput.log"
SECS="${1:-30}"
ONLY="${2:-}"
PDIR=""; [ -n "$PORTRAITS" ] && PDIR="$(cygpath -aw "$(dirname "$0")/../build")\portraits"
OUT="$(cygpath -aw "$(dirname "$0")/../build")\\bench_results.txt"
# ULTS=1: the ult bench (every hero's Q and Commando's Preon once, against 30 enemies + a Beetle Queen) into build/bench_ults.txt;
# copy it to docs/ult-bench.txt for the pages' "just for fun" table (tools/kit_pages.py). Never merge it into the DPS library.
[ -n "$ULTS" ] && OUT="$(cygpath -aw "$(dirname "$0")/../build")\\bench_ults.txt"
B="$LOCALAPPDATA/DeadlockOfRain/bench.json"
. "$(dirname "$0")/game_guard.sh"
guard_open_game
rm -f "$L" "$(cygpath -u "$OUT")"
# a test-mode file left by run_test.sh would start the in-game test alongside the bench and wreck both
rm -f "$LOCALAPPDATA/Melty/test/deadlock-of-rain.json" "$LOCALAPPDATA/Melty/test/risk-of-deadlock-2.json"
mkdir -p "$LOCALAPPDATA/DeadlockOfRain"
MODE=heroes; [ -n "$BASE" ] && MODE=""; [ -n "$ALL" ] && MODE=all
printf '{ "out": "%s", "seconds": %s, "only": "%s", "portraits": "%s", "survivors": "%s", "repeat": %s, "ults": %s }\n' "${OUT//\\/\\\\}" "$SECS" "$ONLY" "${PDIR//\\/\\\\}" "$MODE" "${REPEAT:-1}" "${ULTS:-0}" >"$B"
bash "$(dirname "$0")/dev_run.sh" /nonexistent 3 || { rm -f "$B"; exit 1; }
for i in $(seq 1 40); do tasklist | grep -qi "Risk of Rain" && break; sleep 3; done
tasklist | grep -qi "Risk of Rain" || { echo "NOT_STARTED (is Steam signed in?)"; rm -f "$B"; bash "$(dirname "$0")/lab_restore.sh" >/dev/null 2>&1; exit 1; }
# give up after 40 minutes, or 100 minutes per repeat for the whole library
WAIT=800; [ -n "$ALL" ] && WAIT=$((2000 * ${REPEAT:-1}))
for i in $(seq 1 $WAIT); do
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
