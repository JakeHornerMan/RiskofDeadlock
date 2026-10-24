#!/bin/bash
# Put the dev PC back the way Melty set it up after local test runs (this PC only):
# close the copies of Risk of Rain 2 our scripts started (never the creator's own; FORCE_CLOSE=1 closes every copy), remove the hand-written test-mode file and the devshots folder. BepInEx stays ON
# (winhttp.dll kept, put back if missing) so pressing Play on Steam loads the mods (creator, 2026-10-19: "why ... do the
# mods not start when i press play on steam"); Melty itself may still switch it off as winhttp.dll.melty-off between
# its own launches. MELTY_OFF=1 switches it off the way Melty does.
# The creator's Steam copy always has every hero of the sheets, released or not (creator, 2026-10-23: "steam my client
# shoudl alway have all characters added even if they areb in development and not release on melty"): the dev DLL goes
# back in, heroes.txt lists every hero, coming-soon.txt goes, and a cache that misses a hero is reported (rebuild it).
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
UM=/c/Users/jakeh/tools/universal-modder/bin/um
# every copy a lab script started (the co-op test runs two); a copy the creator started stays open (tools/game_guard.sh)
. "$(dirname "$0")/game_guard.sh"
close_our_games
rm -f "$LOCALAPPDATA/Melty/test/deadlock-of-rain.json" "$LOCALAPPDATA/Melty/test/risk-of-deadlock-2.json"
rm -rf "$LOCALAPPDATA/DeadlockOfRain/devshots" "$LOCALAPPDATA/DeadlockOfRain/coop"
# no leftover join= from the co-op test: the next start hosts, as a solo player's does
[ -f "$LOCALAPPDATA/DeadlockOfRain/melty.properties" ] && printf 'join=\n' >"$LOCALAPPDATA/DeadlockOfRain/melty.properties"
rm -f "$(dirname "$0")/../build/checks.txt"
if [ -n "$MELTY_OFF" ]; then [ -f "$R/winhttp.dll.melty-off" ] && rm -f "$R/winhttp.dll"
else [ -f "$R/winhttp.dll" ] || cp "$R/winhttp.dll.melty-off" "$R/winhttp.dll" 2>/dev/null; fi
D="$R/BepInEx/plugins/DeadlockOfRain"; ROOT="$(dirname "$0")/.."
if [ -d "$D" ]; then
  [ -f "$ROOT/plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll" ] && cp "$ROOT/plugin/bin/Release/netstandard2.1/DeadlockOfRain.dll" "$D/"
  python -c "import json,sys; print('\n'.join(h['id'] for h in json.load(open(sys.argv[1], encoding='utf-8'))['rows']))" "$ROOT/design/heroes.json" > "$D/heroes.txt"
  rm -f "$D/coming-soon.txt"
  python -c "import json,sys,os; want=open(sys.argv[2]).read().split(); f=sys.argv[1]
have=json.load(open(f)).get('heroes',[]) if os.path.exists(f) else []
miss=[h for h in want if h not in have]
print('cache: every hero ready' if not miss else 'CACHE MISSES '+' '.join(miss)+': rebuild it (CLAUDE.md, Commands)')" "$LOCALAPPDATA/DeadlockOfRain/cache/ready.json" "$D/heroes.txt"
fi
ls "$R" | grep -i winhttp; ls "$R/BepInEx/plugins"
