#!/bin/bash
# Quit test (this PC only): a run_test.sh run, then the game is closed the way a player closes it (its window's close
# button, CloseMainWindow), and this reports whether it crashed on the way out (players' ntdll c0000005 on quit,
# 0.7.2/0.7.3). Same arguments and options as run_test.sh (OFF=audio switches parts of the mod off to bisect).
# usage: tools/quit_test.sh haze        prints QUIT_OK or QUIT_CRASH (+ the Windows crash event); run lab_restore.sh after
T0=$(date +%s)
bash "$(dirname "$0")/run_test.sh" "$@" | tail -3
G=$(powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id")
[ -z "$G" ] && { echo "QUIT_UNKNOWN: the game was not running after the test"; exit 1; }
echo "steam overlay loaded: $(powershell -NoProfile -Command "[bool]((Get-Process -Id $G).Modules | Where-Object { \$_.ModuleName -like 'gameoverlayrenderer*' })")"
powershell -NoProfile -Command "(Get-Process -Id $G).CloseMainWindow() | Out-Null"
for i in $(seq 1 60); do tasklist | grep -qi "Risk of Rain" || break; sleep 1; done
tasklist | grep -qi "Risk of Rain" && { echo "QUIT_HUNG: still running after 60 s"; exit 1; }
sleep 3
E=$(powershell -NoProfile -Command "Get-WinEvent -FilterHashtable @{LogName='Application'; Id=1000; StartTime=(Get-Date).AddSeconds(-($(date +%s)-$T0))} -ErrorAction SilentlyContinue | Where-Object { \$_.Message -like '*Risk of Rain 2*' } | ForEach-Object { (\$_.Message -split \"\`n\" | Select-String 'Faulting module name|Fault offset') -join ' ' }")
if [ -n "$E" ]; then echo "QUIT_CRASH: $E"; else echo "QUIT_OK"; fi
tail -2 "/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/BepInEx/LogOutput.log" | cut -c1-200
