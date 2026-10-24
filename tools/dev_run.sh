#!/bin/bash
# Dev loop (this PC only): stop RoR2, deploy the freshly built plugin into the lab install, start it, wait for a file.
# usage: tools/dev_run.sh <file-to-wait-for> [max-seconds]
UM=/c/Users/jakeh/tools/universal-modder/bin/um
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
. "$(dirname "$0")/game_guard.sh"
guard_open_game
rm -f "$LOCALAPPDATA/DeadlockOfRain/devshots/"*
[ -f "$R/winhttp.dll" ] || cp "$R/winhttp.dll.melty-off" "$R/winhttp.dll" || { echo "BepInEx is not installed (no winhttp.dll.melty-off): install the mashup with Melty first"; exit 1; }
cp "$(dirname "$0")/../plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll" "$R/BepInEx/plugins/DeadlockOfRain/" || exit 1
# a dev build has every hero of the sheets: an installed release's heroes.txt (Melty's 0.7.2 has five) would leave a new one out
# RELEASE_ZIP=build/DeadlockOfRain-<ver>.zip: that release's heroes.txt and coming-soon.txt instead (test the Melty cut as players get it)
if [ -n "$RELEASE_ZIP" ]; then
  python -I -c "import zipfile,sys,os; z=zipfile.ZipFile(sys.argv[1]); d=sys.argv[2]
for n in ('heroes.txt','coming-soon.txt'):
    f=os.path.join(d,n); e='plugins/DeadlockOfRain/'+n
    open(f,'wb').write(z.read(e)) if e in z.namelist() else (os.path.exists(f) and os.remove(f))" "$RELEASE_ZIP" "$R/BepInEx/plugins/DeadlockOfRain" || exit 1
  echo "release cut from $RELEASE_ZIP: $(tr '\n' ' ' < "$R/BepInEx/plugins/DeadlockOfRain/heroes.txt")| coming soon: $(cat "$R/BepInEx/plugins/DeadlockOfRain/coming-soon.txt" 2>/dev/null)"
else
  python -c "import json,sys; print('\n'.join(h['id'] for h in json.load(open(sys.argv[1], encoding='utf-8'))['rows']))" "$(dirname "$0")/../design/heroes.json" > "$R/BepInEx/plugins/DeadlockOfRain/heroes.txt"
  rm -f "$R/BepInEx/plugins/DeadlockOfRain/coming-soon.txt"
fi
$UM win launch --steam 632360 >/dev/null 2>&1
# the copy that starts now is ours: recorded so the lab scripts may close it later (tools/game_guard.sh)
for i in $(seq 1 40); do tasklist | grep -qi "Risk of Rain" && break; sleep 3; done
record_our_game
MAX=${2:-420}; for i in $(seq 1 $((MAX/3))); do [ -f "$1" ] && break; sleep 3; done; sleep 2
grep -iE "error|exception" "$R/BepInEx/LogOutput.log" | grep -v "Melty check" | cut -c1-300 | head -15
