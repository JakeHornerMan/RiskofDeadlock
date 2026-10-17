#!/bin/bash
# Put the dev PC back the way Melty set it up after local test runs (this PC only):
# close Risk of Rain 2 (every copy), remove the hand-written test-mode file and the devshots folder, and remove our winhttp.dll copy
# (Melty keeps BepInEx switched off as winhttp.dll.melty-off between its own launches). The installed plugin is left
# alone; Melty reinstalls the release it manages.
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
# every copy (the co-op test runs two)
for P in $(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id"); do $UM win kill $P >/dev/null; done; sleep 3
rm -f "$LOCALAPPDATA/Melty/test/deadlock-of-rain.json"
rm -rf "$LOCALAPPDATA/DeadlockOfRain/devshots" "$LOCALAPPDATA/DeadlockOfRain/coop"
# no leftover join= from the co-op test: the next start hosts, as a solo player's does
[ -f "$LOCALAPPDATA/DeadlockOfRain/melty.properties" ] && printf 'join=\n' >"$LOCALAPPDATA/DeadlockOfRain/melty.properties"
rm -f "$(dirname "$0")/../build/checks.txt"
[ -f "$R/winhttp.dll.melty-off" ] && rm -f "$R/winhttp.dll"
ls "$R" | grep -i winhttp; ls "$R/BepInEx/plugins"
