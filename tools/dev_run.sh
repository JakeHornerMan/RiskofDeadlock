#!/bin/bash
# Dev loop (this PC only): stop RoR2, deploy the freshly built plugin into the lab install, start it, wait for a file.
# usage: tools/dev_run.sh <file-to-wait-for> [max-seconds]
UM=/c/Users/jakeh/tools/universal-modder/bin/um
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
P=$(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id"); [ -n "$P" ] && $UM win kill $P >/dev/null; sleep 3
rm -f "$LOCALAPPDATA/DeadlockOfRain/devshots/"* "/c/Users/jakeh/AppData/Local/Temp/claude/C--Users-jakeh/dedf8d16-4076-446a-b95f-f812f37f2873/scratchpad/checks.txt"
[ -f "$R/winhttp.dll" ] || cp "$R/winhttp.dll.melty-off" "$R/winhttp.dll"
cp "$(dirname "$0")/../plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll" "$R/BepInEx/plugins/DeadlockOfRain/" || exit 1
$UM win launch --steam 632360 >/dev/null 2>&1
MAX=${2:-420}; for i in $(seq 1 $((MAX/3))); do [ -f "$1" ] && break; sleep 3; done; sleep 2
grep -iE "error|exception" "$R/BepInEx/LogOutput.log" | grep -v "Melty check" | cut -c1-300 | head -15
