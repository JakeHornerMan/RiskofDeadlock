#!/bin/bash
# The creator's own hero build (creator, 2026-11-01: "a mod that will run mo and krill only ... without the need of deadlock
# installed"; "Embedded in the DLL"): their edited files built into DeadlockOfRain.dll, for their PC only. Never packaged,
# never uploaded. The guide for the creator is charactersFinal/build&install.md; doc 03 §5c has the short version.
#
#   bash tools/build_local_hero.sh <id> <folder> [--install] [--r2modman]   e.g. mokrill charactersFinal/mokrillFiles --install
#   bash tools/build_local_hero.sh --uninstall                  back to main's dev build with every hero
#
# <folder>/mod-cache is the hero's cache (heroes/<id>/, sounds/, images/, props/) and gets built into the DLL as it is.
# <folder>/edit/<id>.glb is the model to edit in Blender: made once from Deadlock (the only time Deadlock is read), and when it
# is newer than the hero's .dlm it is converted back into mod-cache (DeadlockExtract --import-glb).
# --install copies the DLL into the Steam copy and writes $LOCALAPPDATA/DeadlockOfRain/local-hero.txt: while that file exists the
# dev guard leaves the Steam copy alone and lab_restore.sh puts this DLL back instead of main's.
# --r2modman (2026-11-01) also writes DllBuilds/<Name>-<version>.zip (at the repo root since 2026-11-04) for r2modman's "Import local mod" (the creator's
# own profile; the version is the build time, so every import is newer). Same plugin id as Risk of Deadlock: disable that one
# in the profile. Never upload the zip: the DLL holds Deadlock's files.
#   bash tools/build_local_hero.sh all charactersFinal --r2modman   (2026-11-01: "a zip ... with all my edited characters")
#   bash tools/build_local_hero.sh all charactersFinal --r2modman-install   (2026-11-02): the zip, then tools/r2modman_install.py
#   puts it in the r2modman profile (R2PROFILE, default Deadlock) in place of every older Risk of Deadlock package; r2modman and
#   the game must be closed.
# "all" runs steps 1-3 for every <id>Files folder in the given folder (--cache-only), merges their mod-caches into
# build/local-all/cache (sounds.json joined, files shared by heroes kept once, sheet icons no hero owns taken from the normal
# cache), builds one DLL with every hero in it and compares it with the Melty build's cache (tools/compare_builds.py).
# The zip is named Risk_of_Deadlock (creator, 2026-11-02: Thunderstore given up; this is their own copy).
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

ID="$1"; F="$2"; INSTALL=""; R2=""; R2I=""; CACHE_ONLY=""
for a in "${@:3}"; do case "$a" in --install) INSTALL=1;; --r2modman) R2=1;; --r2modman-install) R2=1; R2I=1;; --cache-only) CACHE_ONLY=1;; *) echo "unknown option $a"; exit 1;; esac; done
[ -n "$ID" ] && [ -n "$F" ] || { sed -n 5,7p "$0"; exit 1; }
F="$(cd "$F" && pwd)"
[ -f "$EXE" ] || { echo "No $EXE: publish the extractor first (docs/01 §6.7)"; exit 1; }

if [ "$ID" = "all" ]; then
  # every hero folder on its own first (its glb, its file check), then one cache with all of them
  OUT="$ROOT/build/local-all"
  C="$OUT/cache"
  n=0
  for d in "$F"/*Files/; do
    [ -d "$d/mod-cache" ] || continue
    h=$(basename "$d"); h=${h%Files}
    echo "== $h"
    bash "$0" "$h" "$d" --cache-only || { echo "Stopped at $h (see above)"; exit 1; }
    n=$((n + 1))
  done
  [ $n -gt 0 ] || { echo "No <hero>Files folders with a mod-cache in $F"; exit 1; }
  rm -rf "$C"
  python - "$F" "$C" <<'EOF'
import json, os, shutil, sys
src, dst = sys.argv[1], sys.argv[2]
heroes, props, sounds = [], [], {}
for d in sorted(os.listdir(src)):
    c = os.path.join(src, d, 'mod-cache')
    if not d.endswith('Files') or not os.path.isdir(c): continue
    ready = json.load(open(os.path.join(c, 'ready.json')))
    heroes += ready['heroes']; props += [p for p in ready['props'] if p not in props]
    sounds.update(json.load(open(os.path.join(c, 'sounds', 'sounds.json'))))
    for base, _, files in os.walk(c):
        for f in files:
            rel = os.path.relpath(os.path.join(base, f), c)
            if rel in ('ready.json', os.path.join('sounds', 'sounds.json'), 'extract.log'): continue
            out = os.path.join(dst, rel)
            os.makedirs(os.path.dirname(out), exist_ok=True)
            shutil.copy2(os.path.join(base, f), out)   # a file two heroes share is the same file in both: kept once
os.makedirs(os.path.join(dst, 'sounds'), exist_ok=True)
json.dump(sounds, open(os.path.join(dst, 'sounds', 'sounds.json'), 'w'), indent=1)
json.dump({'stamp': 'own files', 'deadlockBuild': 'your own files', 'heroes': heroes, 'failed': [], 'props': props, 'failedProps': []},
          open(os.path.join(dst, 'ready.json'), 'w'), indent=2)
print(f'== One cache with {len(heroes)} heroes: {", ".join(heroes)}')
EOF
  # icons the sheets use that no hero folder owns (2026-11-02: Seven's Static debuff icon was missing): from the normal cache
  python - "$ROOT/design" "$L/cache" "$C" <<'EOF'
import glob, os, re, shutil, sys
design, normal, dst = sys.argv[1:4]
icons = set()
for f in glob.glob(os.path.join(design, '*.json')):
    icons.update(re.findall(r'"([^"]+\.vtex)"', open(f, encoding='utf-8').read()))
added, missing = [], []
for k in sorted(icons):
    rel = os.path.join('images', k.replace('.vtex', '').replace('/', '_') + '.png')
    if os.path.exists(os.path.join(dst, rel)): continue
    if os.path.exists(os.path.join(normal, rel)):
        shutil.copy2(os.path.join(normal, rel), os.path.join(dst, rel)); added.append(os.path.basename(rel))
    else: missing.append(os.path.basename(rel))
if added: print('== Shared icons added from the normal cache: ' + ', '.join(added))
if missing: print('   Not in the normal cache either (no icon shown there): ' + ', '.join(missing))
EOF
else
C="$F/mod-cache"
GLB="$F/edit/$ID.glb"
DLM="$C/heroes/$ID/$ID.dlm"
OUT="$ROOT/build/local-$ID"
[ -d "$C" ] || { echo "No mod-cache folder in $F"; exit 1; }

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
[ -n "$CACHE_ONLY" ] && exit 0
fi

# 4. the DLL with mod-cache inside (its own output and obj folders: main's dev DLL is untouched)
echo "== Building the DLL"
dotnet build "$ROOT/plugin" -c Release -v q -nologo -p:LocalHeroDir="$(cygpath -w "$C")" \
  -p:IntermediateOutputPath="$(cygpath -w "$OUT/obj")\\" -o "$OUT" | grep -E "error|Error\(s\)" || true
[ -f "$OUT/DeadlockOfRain.dll" ] || { echo "Build failed"; exit 1; }
echo "   $OUT/DeadlockOfRain.dll ($(du -h "$OUT/DeadlockOfRain.dll" | cut -f1))"
# 4b. (all) every hero the same as in the Melty build? (creator, 2026-11-02: "validate all heros are the saem from deadlock melty to the ddl version")
if [ "$ID" = "all" ]; then python "$ROOT/tools/compare_builds.py" "$L/cache" "$C" || echo "   Differences listed above (tools/compare_builds.py)"; fi

# 5. into the Steam copy
if [ -n "$INSTALL" ]; then
  [ -n "$(running)" ] && { echo "Risk of Rain 2 is running: close it, then run this again with --install."; exit 1; }
  [ -d "$P" ] || { echo "No $P: is the mod installed in the Steam copy?"; exit 1; }
  cp "$OUT/DeadlockOfRain.dll" "$P/DeadlockOfRain.dll"
  cygpath -w "$OUT/DeadlockOfRain.dll" > "$MARK"
  [ -f "$R/winhttp.dll" ] || cp "$R/winhttp.dll.melty-off" "$R/winhttp.dll" 2>/dev/null || true
  echo "== Installed: start Risk of Rain 2 from Steam. Back to every hero: bash tools/build_local_hero.sh --uninstall"
fi

# 6. a zip for r2modman's "Import local mod"
if [ -n "$R2" ]; then
  # laid out like a Thunderstore release (tools/r2modman_package.py; creator, 2026-11-02)
  python "$ROOT/tools/r2modman_package.py" "$OUT/DeadlockOfRain.dll" "$ROOT/DllBuilds" "$ID" || exit 1
  # 7. (--r2modman-install) straight into the r2modman profile, every older Risk of Deadlock package removed first
  if [ -n "$R2I" ]; then
    Z=$(ls -t "$ROOT/DllBuilds"/*.zip | head -1)
    python "$ROOT/tools/r2modman_install.py" "$Z" ${R2PROFILE:+--profile "$R2PROFILE"} ${R2WAIT:+--wait}
  fi
fi
