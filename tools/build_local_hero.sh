#!/bin/bash
# The creator's own hero build (creator, 2026-11-01: "a mod that will run mo and krill only ... without the need of deadlock
# installed"; "Embedded in the DLL"): their edited files built into DeadlockOfRain.dll, for their PC only. Never packaged,
# never uploaded. The guide for the creator is charactersFinal/build&install.md; doc 03 §5c has the short version.
#
#   bash tools/build_local_hero.sh <id> <folder> [--install]   e.g. mokrill charactersFinal/mokrillFiles --install
#   bash tools/build_local_hero.sh --uninstall                  back to main's dev build with every hero
#
# <folder>/mod-cache is the hero's cache (heroes/<id>/, sounds/, images/, props/) and gets built into the DLL as it is.
# <folder>/edit/<id>.glb is the model to edit in Blender: made once from Deadlock (the only time Deadlock is read), and when it
# is newer than the hero's .dlm it is converted back into mod-cache (DeadlockExtract --import-glb).
# --install copies the DLL into the Steam copy and writes $LOCALAPPDATA/DeadlockOfRain/local-hero.txt: while that file exists the
# dev guard leaves the Steam copy alone and lab_restore.sh puts this DLL back instead of main's.
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
R="/c/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2"
P="$R/BepInEx/plugins/DeadlockOfRain"
L="$LOCALAPPDATA/DeadlockOfRain"
MARK="$L/local-hero.txt"
EXE="$ROOT/build/extract/DeadlockExtract.exe"
running() { powershell -NoProfile -Command "Get-Process -Name 'Risk of Rain 2' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Id" | tr -d '\r'; }

if [ "$1" = "--uninstall" ]; then
  [ -n "$(running)" ] && { echo "Risk of Rain 2 is running: close it first, then run this again."; exit 1; }
  rm -f "$MARK"
  bash "$ROOT/tools/lab_restore.sh"
  echo "Back to main's dev build with every hero."
  exit 0
fi

ID="$1"; F="$2"; INSTALL="$3"
[ -n "$ID" ] && [ -n "$F" ] || { sed -n 5,7p "$0"; exit 1; }
F="$(cd "$F" && pwd)"
C="$F/mod-cache"
GLB="$F/edit/$ID.glb"
DLM="$C/heroes/$ID/$ID.dlm"
OUT="$ROOT/build/local-$ID"
[ -d "$C" ] || { echo "No mod-cache folder in $F"; exit 1; }
[ -f "$EXE" ] || { echo "No $EXE: publish the extractor first (docs/01 §6.7)"; exit 1; }

# 1. the model to edit (once; reads Deadlock)
if [ ! -f "$GLB" ]; then
  echo "== Making $GLB from Deadlock (first time only, about a minute)"
  "$EXE" --deadlock "C:/Program Files (x86)/Steam/steamapps/common/Deadlock" --out "$OUT/from-deadlock" \
    --manifest "$ROOT/extractor/manifest.json" --heroes "$ID" --force --keep-glb "$GLB" >/dev/null
  touch -r "$GLB" "$DLM"   # the dlm in mod-cache already matches this glb
fi

# 2. an edited model back into the mod's format
if [ "$GLB" -nt "$DLM" ]; then
  echo "== Converting your edited $GLB"
  "$EXE" --import-glb "$GLB" --hero "$ID" --out "$C" --manifest "$ROOT/extractor/manifest.json" | grep -E "^  $ID:|clips not found"
  rm -f "$C/extract.log"
fi

# 3. every file the hero's moves use is there; ready.json names the hero
python - "$ROOT/extractor/manifest_owners.json" "$C" "$ID" <<'EOF'
import json, os, sys
owners, c, hid = json.load(open(sys.argv[1])), sys.argv[2], sys.argv[3]
snd = json.load(open(os.path.join(c, 'sounds', 'sounds.json'))) if os.path.exists(os.path.join(c, 'sounds', 'sounds.json')) else {}
miss, props = [], []
if not os.path.exists(os.path.join(c, 'heroes', hid, hid + '.dlm')): miss.append(f'heroes/{hid}/{hid}.dlm')
for k, v in owners.items():
    if hid not in v: continue
    if k.endswith('.vtex'):
        f = 'images/' + k.replace('.vtex', '').replace('/', '_') + '.png'
        if not os.path.exists(os.path.join(c, f)): miss.append(f)
    elif k.endswith('.vsndevts'): continue
    elif os.path.isdir(os.path.join(c, 'props', k)): props.append(k)
    elif '.' in k or '/' in k:
        e = snd.get(k) or snd.get('vo:' + k)
        if e is None: miss.append('sounds.json: ' + k)
        else: miss += ['sounds/' + f for f in e['files'] if not os.path.exists(os.path.join(c, 'sounds', f))]
    else: miss.append(f'props/{k}/')
if miss:
    print('Missing from ' + c + ':\n  ' + '\n  '.join(miss)); sys.exit(1)
json.dump({'stamp': 'own files', 'deadlockBuild': 'your own files', 'heroes': [hid], 'failed': [], 'props': props, 'failedProps': []},
          open(os.path.join(c, 'ready.json'), 'w'), indent=2)
print(f'== Every file {hid} uses is in mod-cache')
EOF

# 4. the DLL with mod-cache inside (its own output and obj folders: main's dev DLL is untouched)
echo "== Building the DLL"
dotnet build "$ROOT/plugin" -c Release -v q -nologo -p:LocalHeroDir="$(cygpath -w "$C")" \
  -p:IntermediateOutputPath="$(cygpath -w "$OUT/obj")\\" -o "$OUT" | grep -E "error|Error\(s\)" || true
[ -f "$OUT/DeadlockOfRain.dll" ] || { echo "Build failed"; exit 1; }
echo "   $OUT/DeadlockOfRain.dll ($(du -h "$OUT/DeadlockOfRain.dll" | cut -f1))"

# 5. into the Steam copy
if [ "$INSTALL" = "--install" ]; then
  [ -n "$(running)" ] && { echo "Risk of Rain 2 is running: close it, then run this again with --install."; exit 1; }
  [ -d "$P" ] || { echo "No $P: is the mod installed in the Steam copy?"; exit 1; }
  cp "$OUT/DeadlockOfRain.dll" "$P/DeadlockOfRain.dll"
  cygpath -w "$OUT/DeadlockOfRain.dll" > "$MARK"
  [ -f "$R/winhttp.dll" ] || cp "$R/winhttp.dll.melty-off" "$R/winhttp.dll" 2>/dev/null || true
  echo "== Installed: start Risk of Rain 2 from Steam. Back to every hero: bash tools/build_local_hero.sh --uninstall"
fi
