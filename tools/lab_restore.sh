#!/bin/bash
# Put the dev PC back the way Melty set it up after local test runs (this PC only):
# close Risk of Rain 2 (every copy), remove the hand-written test-mode file and the devshots folder. BepInEx stays ON
# (winhttp.dll kept, put back if missing) so pressing Play on Steam loads the mods (creator, 2026-10-19: "why ... do the
# mods not start when i press play on steam"); Melty itself may still switch it off as winhttp.dll.melty-off between
# its own launches. MELTY_OFF=1 switches it off the way Melty does. The installed plugin is left alone.
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
# every copy (the co-op test runs two)
for P in $(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id"); do $UM win kill $P >/dev/null; done; sleep 3
rm -f "$LOCALAPPDATA/Melty/test/deadlock-of-rain.json" "$LOCALAPPDATA/Melty/test/risk-of-deadlock-2.json"
rm -rf "$LOCALAPPDATA/DeadlockOfRain/devshots" "$LOCALAPPDATA/DeadlockOfRain/coop"
# no leftover join= from the co-op test: the next start hosts, as a solo player's does
[ -f "$LOCALAPPDATA/DeadlockOfRain/melty.properties" ] && printf 'join=\n' >"$LOCALAPPDATA/DeadlockOfRain/melty.properties"
rm -f "$(dirname "$0")/../build/checks.txt"
if [ -n "$MELTY_OFF" ]; then [ -f "$R/winhttp.dll.melty-off" ] && rm -f "$R/winhttp.dll"
else [ -f "$R/winhttp.dll" ] || cp "$R/winhttp.dll.melty-off" "$R/winhttp.dll" 2>/dev/null; fi
ls "$R" | grep -i winhttp; ls "$R/BepInEx/plugins"
