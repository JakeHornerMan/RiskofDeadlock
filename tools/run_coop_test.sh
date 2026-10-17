#!/bin/bash
# Co-op test on this PC: two copies of Risk of Rain 2. The first (started through Steam) hosts; the second is started
# straight from the exe with join=<host address> in melty.properties, the way Melty's Join button starts a friend's game,
# and plays every hero's skills as a joined player while the host measures them (plugin/Runtime/MeltyTestCoop.cs).
#
# usage: tools/run_coop_test.sh [heroes]     e.g. tools/run_coop_test.sh haze,abrams (default: all four)
#   Needs Steam signed in. Closes any running Risk of Rain 2 first (never run it while the creator plays).
#   Writes build/coop_checks.txt (the host's checks), build/coop_host.log and build/coop_join.log (Melty's contract lines:
#   the address the host announced, whether the second copy got in; for setup_multiplayer). Run tools/lab_restore.sh after.
#   About 12-15 minutes (the second copy takes 3-5 minutes to load beside the first).
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
B="$(cd "$(dirname "$0")/.." && pwd)/build"
L="$R/BepInEx/LogOutput.log"; LJ="$R/BepInEx/LogOutput.log.1"
P="$USERPROFILE/AppData/LocalLow/Hopoo Games, LLC/Risk of Rain 2/Player.log"
D="$LOCALAPPDATA/DeadlockOfRain"; ML="$D/melty.log"; MP="$D/melty.properties"
JP="$B/coop_player_join.log"
CHECKS="$(cygpath -aw "$B")\\coop_checks.txt"
kill_all() { for p in $(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id"); do $UM win kill $p >/dev/null; done; }
kill_all
for i in $(seq 1 20); do tasklist | grep -qi "Risk of Rain" || break; sleep 1; done
rm -f "$L" "$LJ" "$P" "$JP" "$(cygpath -u "$CHECKS")" "$B/coop_host.log" "$B/coop_join.log" "$ML" "$D/coop/"*.txt
mkdir -p "$LOCALAPPDATA/Melty/test" "$D/devshots"
printf 'join=\n' >"$MP"
HEROES="${1:+, \"heroes\": \"$1\"}"
printf '{ "checks": "%s", "coop": true%s }\n' "${CHECKS//\\/\\\\}" "$HEROES" >"$LOCALAPPDATA/Melty/test/deadlock-of-rain.json"

# 1. the host
bash "$(dirname "$0")/dev_run.sh" /nonexistent 3 || exit 1
for i in $(seq 1 40); do tasklist | grep -qi "Risk of Rain" && break; sleep 3; done
tasklist | grep -qi "Risk of Rain" || { echo "NOT_STARTED (is Steam signed in?)"; exit 1; }
ADDR=""
for i in $(seq 1 100); do
  ADDR=$(grep -a "Melty join address: " "$ML" 2>/dev/null | tail -1 | sed 's/.*Melty join address: //' | tr -d '\r')
  [ -n "$ADDR" ] && break
  grep -q "Melty test finished" "$L" 2>/dev/null && break
  sleep 3
done
[ -z "$ADDR" ] && { echo "HOST_NOT_OPEN"; grep -E "Melty check|Melty test" "$L" | cut -c40-300; exit 1; }
cp "$ML" "$B/coop_host.log"
echo "host open at $ADDR"

# 2. the second copy joins, as Melty's Join would start it (join= set before it starts)
printf 'join=%s\n' "$ADDR" >"$MP"
WR=$(cygpath -w "$R"); WJ=$(cygpath -w "$JP")
powershell -NoProfile -Command "\$env:SteamAppId='632360'; \$env:SteamGameId='632360'; Start-Process -FilePath '$WR\\Risk of Rain 2.exe' -ArgumentList '-logFile','$WJ','-screen-fullscreen','0','-screen-width','960','-screen-height','540' -WorkingDirectory '$WR'"
for i in $(seq 1 800); do
  grep -q "Melty test finished" "$L" 2>/dev/null && break
  grep -q "MeltyTest" "$P" 2>/dev/null && { echo "EXCEPTION (host)"; break; }
  grep -q "MeltyTest" "$JP" 2>/dev/null && { echo "EXCEPTION (joiner)"; break; }
  [ "$(tasklist | grep -ci "Risk of Rain")" -lt 2 ] && { echo "A_COPY_EXITED"; break; }
  sleep 3
done
sleep 3
cp "$ML" "$B/coop_join.log" 2>/dev/null
printf 'join=\n' >"$MP"
grep -E "Melty test|Melty check" "$L" | cut -c40-460
echo "--- joiner"
grep -E "Melty: |Melty join|Melty test" "$LJ" 2>/dev/null | cut -c40-300 | tail -8
grep -nE -B2 "MeltyTest|DeadlockOfRain\." "$P" "$JP" 2>/dev/null | cut -c1-250 | head -20
kill_all
