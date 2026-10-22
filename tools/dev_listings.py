"""Regenerate the Deadlock dev listings in tools/dev/ (vpk_files.txt, sound_events.txt, anim_names.json) from this PC's
install, with the dev tool vrfdump (build it once: dotnet build tools/vrfdump -c Release -o build/vrfdump). Doc 01 §8.
Reads the VPK only. Used by the daily watch after a Deadlock update (its permissions allow `python tools/...` only).
  python tools/dev_listings.py [--deadlock <Deadlock folder>]
Writes the old listings to build/watch/old-dev/ first and prints what changed for the sheets' heroes."""
import json, os, struct, subprocess, sys, shutil

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEV = os.path.join(ROOT, "tools", "dev")
EXE = os.path.join(ROOT, "build", "vrfdump", "vrfdump.exe")
dl = r"C:\Program Files (x86)\Steam\steamapps\common\Deadlock"
if "--deadlock" in sys.argv: dl = sys.argv[sys.argv.index("--deadlock") + 1]
VPK = os.path.join(dl, "game", "citadel", "pak01_dir.vpk")
SCRATCH = os.path.join(ROOT, "build", "watch", "listings")
OLD = os.path.join(ROOT, "build", "watch", "old-dev")

def vrf(*a, out=None):
    r = subprocess.run([EXE, VPK, *a], stdin=subprocess.DEVNULL, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0: raise SystemExit(f"vrfdump {a[0]} {a[1]} failed: {r.stderr[-800:]}")
    return r.stdout

os.makedirs(SCRATCH, exist_ok=True); os.makedirs(OLD, exist_ok=True)

# Read everything first; the listings are only replaced once every read worked (a failed run keeps the old ones)
files = sorted(l.strip() for l in vrf("list", "").splitlines() if l.strip())
print(f"vpk_files.txt: {len(files)} files")

ev = os.path.join(SCRATCH, "events.txt"); vrf("events", "x", ev)
events = sorted(set(l.strip() for l in open(ev, encoding="utf-8") if l.strip()))
print(f"sound_events.txt: {len(events)} events")

heroes = json.load(open(os.path.join(ROOT, "design", "heroes.json"), encoding="utf-8"))["rows"]
anims = {}
for h in heroes:
    glb = os.path.join(SCRATCH, h["id"] + ".glb"); vrf("glb", h["vmdl"], glb)
    with open(glb, "rb") as f:
        hdr = f.read(20); ln = struct.unpack_from("<I", hdr, 12)[0]; j = json.loads(f.read(ln))
    anims[h["id"]] = {"anims": sorted(set(a["name"].split("/")[-1] for a in j.get("animations", []))),
                      "meshes": [m["name"].split(".")[-1] for m in j.get("meshes", [])]}
    print(f"{h['id']}: {len(anims[h['id']]['anims'])} clips, {len(anims[h['id']]['meshes'])} meshes")

for f in ("vpk_files.txt", "sound_events.txt", "anim_names.json"):
    if os.path.exists(os.path.join(DEV, f)): shutil.copy2(os.path.join(DEV, f), os.path.join(OLD, f))
open(os.path.join(DEV, "vpk_files.txt"), "w", newline="\n").write("\n".join(files) + "\n")
open(os.path.join(DEV, "sound_events.txt"), "w", newline="\n").write("\n".join(events) + "\n")
json.dump(anims, open(os.path.join(DEV, "anim_names.json"), "w"))

# What changed against the old listings (sheets' heroes only for clips/meshes)
def lines(p): return set(l.strip() for l in open(p, encoding="utf-8") if l.strip()) if os.path.exists(p) else set()
for f, new in (("vpk_files.txt", set(files)), ("sound_events.txt", set(events))):
    old = lines(os.path.join(OLD, f))
    if old: print(f"{f}: +{len(new - old)} -{len(old - new)}")
    for x in sorted(old - new)[:200]: print(f"  - {x}")
oldp = os.path.join(OLD, "anim_names.json")
if os.path.exists(oldp):
    old = json.load(open(oldp))
    for h, v in anims.items():
        o = old.get(h, {"anims": [], "meshes": []})
        for k in ("anims", "meshes"):
            gone, new = set(o[k]) - set(v[k]), set(v[k]) - set(o[k])
            if gone or new: print(f"{h} {k}: +{sorted(new)[:30]} -{sorted(gone)[:30]}")
