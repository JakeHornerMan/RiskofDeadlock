# 01: Deadlock assets: how we find, extract, convert and use them

> **Audience:** an engineer or agent who has to touch anything that comes **from Deadlock**: models, skeletons, animation
> clips, textures, icons, portraits, sounds, ability numbers.
> **Keep this file current.** Every change to the extractor, the DLM format, the manifest, the asset columns of the
> design sheets or the plugin's reading of the cache must be reflected here in the same commit (see `CLAUDE.md`).

Related documents:
- `docs/02-RISK-OF-RAIN-2-INTEGRATION.md`: what happens to the assets inside Risk of Rain 2.
- `docs/03-PROCESS-OVERVIEW.md`: the whole pipeline end to end, Melty publishing, testing, and the "add a new hero" checklist.

---

## 1. The ground rules (legal and technical)

1. **We never ship Deadlock files.** The release zip contains only our code: the plugin DLL, the extractor exe,
   `manifest.json`, docs and licences. Every model, texture, clip and sound is read **on the player's own PC from the
   player's own Deadlock install** by `DeadlockExtract.exe` and written to a local cache.
2. **Deadlock is never modified.** We only *read* `pak01_dir.vpk`. Nothing is written into the Deadlock folder, and
   Deadlock never needs to run. That is why Melty's `game_info` treats this mashup as no account risk. Never work
   around Deadlock's anti-cheat; we have no reason to touch the running game at all.
3. **The project repo holds no Deadlock content either.** Exports made while developing go to a scratch folder or to
   the gitignored `tools/dev/` (listings only, never assets) and to `%LOCALAPPDATA%\DeadlockOfRain\cache` (gitignored, outside the repo).

---
- **Visual effects are never taken from Deadlock.** Deadlock's particle systems (`.vpcf`) are not read or exported.
  The abilities' looks are rebuilt by our own code in Unity with Risk of Rain 2's materials (doc 02 §8.1).

## 2. Versions this was built and verified against

| Thing | Version | Where it is recorded |
|---|---|---|
| Deadlock | `ClientVersion=6795` (Source 2; 6774 until the update of 2026-10-19) | `game/citadel/steam.inf`; also in `cache/ready.json` → `deadlockBuild` |
| ValveResourceFormat (VRF) NuGet | **20.1.7467** (20.0.6980 until v0.7.1: it can't read build 6795, "Unknown KVType 26") | `extractor/DeadlockExtract.csproj` |
| VRF CLI (Source2Viewer-CLI) for exploring | 20.0 (`cli-windows-x64.zip` from the VRF GitHub release `20.0`) | `C:\Users\jakeh\tools\vrf-cli\Source2Viewer-CLI.exe` (dev machine only) |
| NLayer (MP3 decoder) | 1.16.0 | `extractor/DeadlockExtract.csproj` |
| .NET SDK for the extractor | **net10.0**. VRF 20 needs it; net8 does not compile against it. | csproj `TargetFramework` |
| SharpGLTF, SkiaSharp, ValvePak, ValveKeyValue | transitive deps of VRF | `THIRD-PARTY-NOTICES.txt` |
| Extractor version | `1.3.0` (`Program.Version`; 1.1.0 added voice lines, 1.2.0 reads sound volumes as decibels, 1.3.0 reads each hero on its own and VRF 20.1) | `extractor/Program.cs`; also part of the cache stamp |

A Deadlock update can change asset paths, clip names, mesh names or sound event names. **Section 9** explains how to
re-validate everything after an update.

---

## 3. Where Deadlock keeps things (paths verified on build 6774)

Install folder (Steam default): `C:\Program Files (x86)\Steam\steamapps\common\Deadlock`.

The extractor looks for the VPK directory file in this order (`Program.FindVpk`):
1. `<Deadlock>/game/citadel/pak01_dir.vpk`, which is the real location
2. `<Deadlock>/citadel/pak01_dir.vpk`
3. `<Deadlock>/pak01_dir.vpk`

The build number is read from `steam.inf` next to the VPK (line `ClientVersion=...`).

Deadlock's internal name is **citadel**. Heroes use old codenames internally:

| Our id | Display name | Internal hero key (`scripts/heroes.vdata`) | Model (`vmdl`, inside the VPK) |
|---|---|---|---|
| `haze` | Haze | `hero_haze` | `models/heroes_staging/haze/haze.vmdl` |
| `abrams` | Abrams | `hero_atlas` | `models/heroes_wip/abrams/abrams.vmdl` |
| `seven` | Seven | `hero_gigawatt` | `models/heroes_staging/gigawatt_prisoner/gigawatt_prisoner.vmdl` |
| `infernus` | Infernus | `hero_inferno` | `models/heroes_wip/inferno/inferno.vmdl` |

Other codenames you will meet in asset paths and sound events:
- Abrams = **bull**/**atlas** (`bull_sm_psd`, `citadel_ability_bull_charge`).
- Seven = **gigawatt** (`gigawatt_sm_psd`, `Gigawatt.StormCloud.Cast`).
- Infernus = **inferno** (`Inferno.FireBomb.Explode`).

Some events use the display name instead (`Haze.Wpn.Fire.Main`, `Abrams.A1.SiphonLife.Cast`, `Seven.Wpn.Fire`,
`Infernus.Wpn.Fire`). **Always look the exact name up; do not guess** (section 8).

Asset kinds and their compiled extensions inside the VPK (VRF loads them by the path *without* `_c`):

| Kind | Compiled file | Example | What we take |
|---|---|---|---|
| Model | `.vmdl_c` | `models/heroes_staging/haze/haze.vmdl_c` | meshes, skeleton, materials, embedded animation clips |
| Texture / UI image | `.vtex_c` | `panorama/images/heroes/haze_sm_psd.vtex_c` | portraits, cards, ability icons |
| Material | `.vmat_c` | (referenced by the model) | albedo (color) only, through the glTF export |
| Animation clips | inside the model / `.vnmclip_c` | `weapon_stand_idle`, `ability_sleep_dagger` | sampled per bone |
| Sound event files | `.vsndevts_c` | `soundevents/hero/haze.vsndevts_c` | event name → `vsnd_files` list, `volume`, `base` inheritance |
| Sound | `.vsnd_c` | (named by events) | MP3 or WAV payload |
| Game data | `.vdata_c` | `scripts/heroes.vdata_c`, `scripts/abilities.vdata_c` | read **by hand during design** for ability numbers; not read at runtime |

---

## 4. The pipeline in one picture

```
design/*.json  --tools/gen.py-->  extractor/manifest.json   (what to read: models+mesh filter+clip filter, images, sound events)
                                         |
Player's Deadlock install                v
  game/citadel/pak01_dir.vpk  --> DeadlockExtract.exe (Melty recipe.setup, before first game start)
                                         |
                                         v
%LOCALAPPDATA%\DeadlockOfRain\cache\
   heroes/<id>/<id>.dlm          skeleton + skinned mesh + materials + sampled clips (Unity space)
   heroes/<id>/m<i>_albedo.png   one color texture per material (max 2048 px)
   images/<vtex path with / -> _>.png   portraits / cards / icons (max 512 px)
   sounds/<Event_Name>_<n>.wav + sounds.json   decoded sound event variants + volume
   ready.json                    stamp = "<build>|<extractor version>|<sha256(manifest)[:16]>"
   extract.log                   full log of the last run
                                         |
                                         v
Risk of Rain 2 plugin (DeadlockOfRain.dll): DeadlockCache reads it; ModelBuilder/DeadlockAnimator/DeadlockAudio use it
```

---

## 5. The manifest: what gets extracted (generated, never hand-edited)

`extractor/manifest.json` is **generated by `tools/gen.py`** from the design sheets. Edit the sheets and regenerate. The shape:

```json
{
  "heroes": [ { "id": "haze", "vmdl": "models/heroes_staging/haze/haze.vmdl",
                "meshes": ["body","haze_gun_l","haze_gun_r"], "clips": ["ability_bullet_flurry", "..."] } ],
  "images": [ "panorama/images/heroes/haze_sm_psd.vtex", "..." ],
  "soundEvents": [ "Haze.Wpn.Fire.Main", "..." ],
  "soundEventFiles": [ "soundevents/hero/haze.vsndevts", "..." ],
  "voiceGroups": [ "atlas/atlas_select", "atlas/emote/atlas_effort_melee_big", "..." ]
}
```

Where every entry comes from:

| Manifest field | Source sheet / column | Notes |
|---|---|---|
| `heroes[].id`, `vmdl` | `heroes.json` → `id`, `vmdl` | |
| `heroes[].meshes` | `heroes.json` → `meshInclude` | VRF **MeshFilter**: only these mesh names are exported. This drops LODs, alternate costumes, ability props and effect meshes. Mesh names are glTF mesh names (the part after the last `.`). |
| `heroes[].clips` | `animations.json`: every column **except** `ANIM_RIG_COLUMNS` = {`hero`, `upperBodyBone`, `gunArms`, `gunBone`, `gunHand`, `muzzle`, `muzzleLeft`, `clothParents`}, de-duplicated, `none` dropped | VRF **AnimationFilter**. `aimPose` *is* a clip column (it names a clip). |
| `images` | `heroes.imageSmall`, `heroes.imageCard`, `heroes.passiveIcon` (unless `none`), `skills.icon` (the equipment ults use their skill's icon), `buffs.icon` (unless `none`) | sorted, unique |
| `soundEvents` | `sounds.json` columns `cast`, `loop`, `hit`, `end`, `reload`, `castLayer`, `hitLayer`, `tick`, `tickFinal` (unless `none`; the layers, v0.5.1, and the ticks, v0.6.2, may be any hero's event, e.g. `Warden.Wpn.Fire` under Infernus' gun) | sorted, unique |
| `soundEventFiles` | `heroes.soundevents` | **informational only.** The extractor indexes *every* `soundevents/*.vsndevts_c` in the VPK, so cross-file `base` inheritance resolves. |

Counts at v0.4.0: 4 heroes, 32 images, 55 sound events. (0.3.0 **added** two images, the passive icons
`bull_beef_psd` for Abrams' Infernal Resilience and `giga_chain_psd` for Seven's Capacitor and Static. 0.4.0 **adds** the
new R moves' content: item icons `phantom_strike_psd`, `melee_charge_psd`, `torment_pulse_psd` and `tesla_bullets_psd` (Seven's
Capacitor icon now; `giga_chain_psd` is Power Surge's), sound events from the shared item and melee soundevents
(`Mods.Armor.Phantom.Strike.*`, `Mods.Tech.TormentPulse.Proc`, `Abrams.Melee.Swing.Charged`, `Ability.Melee.Impact.Heavy.NPC`,
`Gigawatt.PowerSurge.*`) and the clips in the new `special_start`/`special` columns (Haze `melee_dash`, `melee_quick_1`;
Abrams `melee_start`, `melee_hit`; Seven `ability_power_surge`). So a 0.3.0 cache needs a rebuild too: see below.)

Changing the manifest changes the cache stamp (section 6.6), so the next first start re-extracts. On a PC that
already has `ready.json`, though, Melty's `setup.done` check is satisfied and **setup is skipped**. So an *older*
cache stays in use until something deletes `ready.json` or the extractor runs again. This is fine as long as new
releases only **remove** clips, images or sounds. A release that **adds** a hero, clip, image or sound needs the
cache rebuilt: see section 10 and doc 03 for how to force it. **Since 0.6.3 the recipe forces it:** `setup.done` has
`contains: "|<extractor version>|<manifest hash>"` (doc 03 §6), so a cache stamped from another manifest is not "done" and
Melty runs the extractor again, which rebuilds because the stamp differs.

---

**Release subsets (2026-10-19).** `gen.py` also writes `extractor/manifest_owners.json`: each image, sound event, sound
event file and voice group → the hero(es) whose sheet rows use it (sound rows like `mokrill_gun/step` count for their
base skill's hero; shared buff icons have no owner and are always kept). `package.py --thunderstore` uses it to cut the
manifest down to `thunderstore/release.json`'s heroes. A cut manifest hashes differently, so its stamp differs from the
full manifest's and switching between the two re-extracts.

## 6. DeadlockExtract.exe: step by step (`extractor/Program.cs`)

Command line (exactly as Melty's recipe runs it):
```
DeadlockExtract.exe --deadlock "{game:deadlock}" --out "{localappdata}/DeadlockOfRain/cache"
    [--manifest <path>]   default: manifest.json next to the exe
    [--force]             ignore ready.json and rebuild
```
- If `--deadlock` is missing, the environment variable `DEADLOCKOFRAIN_DEADLOCK` is used.
- If `--out` is missing, it defaults to `%LOCALAPPDATA%\DeadlockOfRain\cache`.
- Exit code: 0 on success (or "cache is current"), 1 on failure. The failure reason is in `cache/extract.log`.

### 6.1 Opening the VPK
`SteamDatabase.ValvePak.Package` reads `pak01_dir.vpk`, with `OptimizeEntriesForBinarySearch(OrdinalIgnoreCase)`. A
VRF `GameFileLoader(package, vpkPath)` resolves compiled files and their dependencies (materials → textures).
`loader.LoadFileCompiled("models/.../x.vmdl")` returns a `Resource` whose `DataBlock` is the typed payload.

### 6.2 Heroes → glTF → DLM (`ExportHero`)
1. `GltfModelExporter(loader)` with:
   - `ExportAnimations = true`, `ExportMaterials = true`, `AdaptTextures = true`, `SatelliteImages = false` (textures embedded in the .glb)
   - **`ComposeAdditiveAnimations = true`**. Deadlock's shoot and landing clips are *additive layers*. Without
     composition they are deltas, and playing them alone "crushes" the pose into a ball. With it, every clip becomes a full pose.
   - `ProgressReporter` must be set (VRF 20 throws without one).
   - `AnimationFilter` and `MeshFilter` are read-only `HashSet`s: **call `.Add`, do not assign**.
2. Export to `heroes/<id>/_gltf/<id>.glb`, load it with `SharpGLTF.Schema2.ModelRoot.Load`, convert with
   `DlmWriter.Write`, then delete `_gltf`.
3. Timing: about 8 minutes for all four heroes on the dev PC. Abrams alone takes about 5 minutes inside VRF, because his model has 499 bones and many clips.

### 6.3 `DlmWriter`: glTF → DLM, in Unity space
**Skeleton:**
- The skeleton is the union of the joints of every skin used by an included mesh. A joint's parent is added too, but only when that parent is itself a joint of some skin and has a parent of its own (DlmWriter.cs:71-76). Any other bone whose parent is not in the list becomes a **root** (parent −1). See §8: `pelvis` is a separate root for three heroes.
- Bind poses come from each skin's inverse bind matrices (first occurrence wins).
- A root bone stores its full world transform.

**Coordinate conversion.** glTF is right-handed and Unity left-handed. Everything is mirrored on X:
- positions and normals: `x → -x`
- quaternions: `(x, y, z, w) → (x, -y, -z, w)`
- matrices: `S·M·S` with `S = diag(-1,1,1,1)`, written row-major in Unity's `m00 m01 …` order
- triangle winding is reversed `(a, c, b)`
- UV V is flipped: `v → 1 - v`

Result: models face **+Z** in Unity, the same as RoR2 bodies.

**Mesh:**
- Every TRIANGLES primitive of every included mesh node is merged.
- **Shared vertex buffers:** VRF primitives of one mesh share a POSITION accessor. The writer adds each accessor
  **once** (keyed by `LogicalIndex`). Without this, the vertex count tripled (149k for one hero).
- Skin joint indices are remapped into the DLM bone list. Weights are renormalised to sum to 1.
- An **unskinned** primitive is bound fully to bone 0 (not to its node's bone). A submesh without a material is written as material 0.
- Submeshes are grouped by material.

**Materials:**
- Name, `m<i>_albedo.png` (BaseColor channel, max 2048 px), alpha mode (0 opaque, 1 mask, 2 blend), alpha cutoff, and base color factor are kept.
- `m<i>_normal.png` (max 1024 px) is attempted, but **VRF 20 does not export normal maps for Deadlock's hero shader**. The column stays empty and the plugin does not use normals (it clears `_NormalTex`).

**Animations:**
- Only clips named in the manifest are kept, matched by short name (the last path segment, case-insensitive). Duplicates keep the first.
- Each clip is sampled at **30 fps** with SharpGLTF curve samplers (`anim.FindTranslationChannel(node).GetTranslationSampler().CreateCurveSampler(true)`, and the same for rotation), in each bone's **local** space.
- A bone with no channel, or one that never moves, gets no track.
- A track stores 1 position (constant) or every frame, and 1 rotation (constant) or every frame.
- Missing clips are logged as `clips not found: ...`.

The writer returns a stats line that goes into `extract.log`. Real values at build 6774:

| Hero | Bones | Verts | Submeshes / materials | Clips (tracks) | "height" in the log |
|---|---|---|---|---|---|
| haze | 120 | 49 632 | 5 / 5 | 17/17 (1041) | 2.420 m |
| abrams | 499 | 61 075 | 6 / 6 | 18/18 (5867) | 3.098 m |
| seven | 245 | 95 250 | 4 / 4 | 17/17 (1623) | 2.866 m |
| infernus | 158 | 63 071 | 4 / 4 | 15/15 (944) | 2.657 m |

(The clip counts above are from the 0.1.0 manifest. The 0.2.0 manifest has fewer clips: `fire`/`fire_alt` (the `shoot_idle*` clips) are `none` for everyone, `land` is `none` except for Infernus, and the `special` column, which held Seven's `ability_power_surge`, was removed with ability 3. 0.4.0 brings `special_start`/`special` back for the new R moves.)

**Warning: the log's "height" is not `heroes.sourceHeight`.** The log measures max−min Y over **every** included
vertex in the bind pose. That includes guns, which sit *below* the feet in the bind pose (Seven's gun goes down to
−0.21 m), backpacks and headgear. `sourceHeight` (Haze 2.413, Abrams 2.927, Seven 2.43, Infernus 2.505) is the
hero's **standing height, sole to top of head, from the body and head meshes only**. It was measured by hand on
2026-10-12 from the VRF glTF exports. To re-measure, run `python tools/dlm_sub.py <cache>/heroes/<id>/<id>.dlm`.
It prints each submesh's Y range; take the body and head submeshes (not the gun, hair spikes or props). Example
for Seven: body −0.18 to 2.65 includes the backpack antenna, head top 2.46, and the sheet says 2.43.

### 6.4 Images (`ExportImage`)
`LoadFileCompiled(vtex)` → `Texture.GenerateBitmap()` → down-scaled to max 512 px (Mitchell cubic) → PNG. The file name
is the vtex path with `.vtex` removed and `/` replaced by `_`, plus `.png`. For example,
`panorama/images/heroes/haze_sm_psd.vtex` becomes `images/panorama_images_heroes_haze_sm_psd.png`.
`Program.ImageFileName` and the plugin's `DeadlockCache.ImageFile` **must stay identical**.

### 6.5 Sounds (`ExportSounds`)
1. Index **every** `soundevents/*.vsndevts_c` in the VPK (`package.Entries["vsndevts_c"]`). Each is KV3 (`BinaryKV3`) or
   `KeyValuesOrNTRO`, and its top-level children are events (`name → KVObject`). Build 6774 has **81 263** events (`extract.log`: "81263 sound events indexed").
2. For each wanted event:
   - Resolve `vsnd_files` (string or array), following `base` inheritance up to 8 levels.
   - Resolve `volume` the same way. It is in **decibels** (0 = as recorded; Infernus' gun is −1, some quiet layers −16), so it is written as the linear gain `10^(dB/20)` (default 0 dB = 1, clamped 0–4). Before extractor 1.2.0 (v0.5.1) the dB number itself was used as the gain, so every event at 0 dB or below played at the plugin's 20 % floor and +2…+4 dB events at 2×.
3. Decode up to **4 variants** per event:
   - `.vsnd_c` → `Sound.GetSoundStream()`
   - MP3 → WAV with NLayer (16-bit PCM, original rate and channels)
   - WAV is copied as-is
4. Write `sounds/<EventName with non-alphanumerics→_>_<n>.wav` and `sounds/sounds.json`:
   `{ "Haze.Wpn.Fire.Main": { "volume": 1.0, "files": ["Haze_Wpn_Fire_Main_0.wav", ...] } }`.

5. **Voice lines** (`ExportVoice`, since the voice update). They are loose files, not sound events.
   - For every manifest `voiceGroups` entry `<group>`, every VPK file `sounds/vo/<group>_NN.vsnd_c` (any number of digits,
     sorted, at most 24) is decoded the same way.
   - The takes are written as `sounds/vo_<group>_<n>.wav` and indexed as one event `"vo:<group>"` in the same `sounds.json`.
   - `ready.json` gets `voiceGroups` (how many were found).
   - **Where voice lives:**
     - Each hero's lines are under `sounds/vo/<code>/`; the codes are haze, **atlas** (Abrams), **gigawatt** (Seven) and
       **inferno** (Infernus).
     - Ability lines are `<code>_use_power1..4_NN`: Deadlock's ability slots, 4 = the ultimate, 3 = Seven's Power Surge.
     - Item lines are `<code>_use_<item>_NN` (e.g. `haze_use_phantom_strike`).
     - Also `<code>_select_NN` / `_unselect_NN`, `_killstreak_start|mid|high_NN`, and `_hunt` / `_happy`.
     - Grunts are under `emote/` (`<code>_effort_melee_big_NN`, `<code>_effort_general_NN`).
     - The wiki's `<Code>_<line>.mp3` names are the same files.
   - Preflight checks every group's `_01` take against `tools/dev/vpk_files.txt`.

### 6.6 `ready.json` and the stamp
Example from this PC. It was written by the **0.1.0 manifest**; the 0.2.0 manifest hashes to `9B04D13EFD588D39`, so
this cache is "stale" but still a superset of what 0.2.0 needs:
```json
{ "stamp": "6774|1.0.1|4BA3F5F5CDC8ED4A", "deadlockBuild": "6774", "extractor": "1.0.1",
  "heroes": ["haze","abrams","seven","infernus"], "images": 28, "soundEvents": 51, "time": "..." }
```
- `stamp = <ClientVersion>|<Program.Version>|<first 16 hex of SHA-256(manifest text)>`.
- On start without `--force`, if `ready.json` exists **and** its stamp equals the new one, the run is a no-op ("Cache
  is current"). Otherwise `ready.json` is deleted first and a full extraction runs. **With `--force` the old
  `ready.json` is not deleted**, so a forced run that fails partway leaves the old "complete" marker in place. Delete it
  by hand if a forced run fails. `ready.json` is written **last**.
- **What is fatal and what is not:**
  - **Since 1.3.0 (v0.7.1) each hero is read on its own.** A missing `vmdl` or a VRF exception while exporting a hero logs
    `HERO FAILED <id>: <message>` plus the stack trace, removes that hero's folder (it is written to `heroes/<id>.tmp` and
    swapped in only when finished, so an old copy is never left behind) and carries on. `ready.json` lists the heroes read
    in `heroes` and the others in `failed: [{id, error}]`; the plugin skips those and names them on the title screen (doc 02 §3).
    The exit code is 1 only when **no** hero could be read (or the VPK can't be opened). Before 1.3.0 one bad hero aborted
    the whole run, which is how Deadlock build 6795 (2026-10-19, a new KV3 type 26 that VRF 20.0 didn't know) left
    every player with no heroes at all.
  - A failed hero is retried by itself after the next Deadlock update or extractor version, because the stamp changes.
  - A missing clip, image or sound event is only **logged**, and the run still exits 0 (a sound event that throws is caught per event; a crash of the whole sound step leaves the heroes in place).
  - Always read `extract.log`.
- **What gets wiped:** `heroes/<id>/` and `sounds/` are wiped and rewritten on each run. `images/` is **not**, so old PNGs linger (harmless).
- Melty's recipe `setup.done.file` is this `ready.json`. That's why the game waits for extraction on first start.
- **Thunderstore build (2026-10-19):** the plugin itself runs the exe on every start (`plugin/Runtime/FirstRun.cs`, doc 02 §3); the
  stamp check above makes that a ~1 s no-op once the cache is current. While working, the exe writes `progress.txt`
  (`<done> <total> <step>`, one step per hero, then images, then sounds) for the loading screen. The output itself was
  unchanged by that, so `Version` stayed 1.2.0 (a release's cut-down manifest gives it its own stamp, §5); 1.3.0 is the per-hero change above.
- The plugin reads `deadlockBuild` for its log line only. It does **not** re-check the stamp.

### 6.7 Building and publishing the extractor exe
```
cd extractor
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o ../build/extract
```
This gives one self-contained `build/extract/DeadlockExtract.exe` (about 45.8 MB), so players need no .NET install.

**After a Deadlock update** players report "Unknown KVType" / "UnexpectedMagicException" (`player_logs` on Melty): Valve changed
the resource format. Update the VRF `PackageReference` to the newest on NuGet
(`https://api.nuget.org/v3-flatcontainer/valveresourceformat/index.json`), publish the exe, rerun it with `--force` on the
updated Deadlock and compare the cache with the old one before releasing.
`tools/package.py` copies it into the zip as `extract/DeadlockExtract.exe` with `manifest.json`,
`THIRD-PARTY-NOTICES.txt` and `LICENSE.txt`. The Thunderstore zip (`--thunderstore`) puts it in `plugins/extract/` with
the manifest renamed `deadlock-manifest.json` (Thunderstore's own `manifest.json` sits at the zip root); the plugin
passes `--manifest` explicitly. **Rebuild the exe only when `extractor/*.cs` changes.** A manifest change needs only `gen.py` + `package.py`.

The published exe has its own copy of `manifest.json` next to it in `build/extract/` (from publish time), and that
copy is what it reads by default. **When running it by hand after `gen.py`, pass `--manifest extractor/manifest.json`.**
`build/` is gitignored, so after a fresh clone the exe must be published again.

To test the extractor from scratch on the dev PC (without touching the real cache):
```
build/extract/DeadlockExtract.exe --deadlock "C:\Program Files (x86)\Steam\steamapps\common\Deadlock" \
   --out <scratch>/cache_verify --manifest extractor/manifest.json --force
```
Expect one `heroes/<id>` folder per hero and the image and sound counts from the manifest. Extraction time grows with each hero (about 1–5 min each; Abrams' 499 bones are the slowest).

---

## 7. The DLM file format (version tag `DLM1`)

Little-endian. Strings use .NET `BinaryWriter` format (7-bit-encoded length prefix, then UTF-8 bytes). Written by
`extractor/DlmWriter.cs` and read by `plugin/Runtime/DeadlockCache.cs` (`HeroAsset.Read`). Both sides **must change
together**. Python readers for debugging: `tools/dlm_check.py`, `tools/dlm_pose.py`, `tools/dlm_sub.py`, `tools/dlm_tris.py`.

```
char[4]  "DLM1"
int32    boneCount
  per bone: string name; int32 parent (-1 = root); float3 localPos; float4 localRot (x,y,z,w); float3 localScale;
            float[16] bindpose (Unity row-major m00 m01 m02 m03 m10 ...)
int32    vertexCount
  float3 pos[vertexCount]; float3 normal[vertexCount]; float2 uv[vertexCount];
  per vertex: int32 bone0..bone3, float weight0..weight3      (32 bytes)
int32    submeshCount
  per submesh: int32 materialIndex; int32 indexCount; int32 index[indexCount]
int32    materialCount
  per material: string name; string albedoFile; string normalFile; int32 alphaMode; float cutoff; float4 color
int32    clipCount
  per clip: string name; float length; float fps; int32 frames; int32 trackCount;
    per track: int32 bone; int32 posFrames (0|1|frames); float3 pos[posFrames]; int32 rotFrames (1|frames); float4 rot[rotFrames]
```
(The writer never emits `posFrames = 0` today, but the reader accepts it.)

How the plugin turns this into Unity objects (details in doc 02 §4):
- **`HeroAsset.Parts`** builds **one `Mesh` per submesh**, holding only the vertices that submesh uses, with shared bind
  poses and all-white vertex colors. This is because RoR2's `CharacterModel` drives one material per renderer.
- **`HeroAsset.Texture(file)`** loads PNGs, compressing them when the size is a multiple of 4.
- **`DeadlockAnimator.BuildClips`** turns tracks into legacy `AnimationClip` curves with linear tangents, cached per
  hero. It **skips position curves on every parentless bone and on `root_motion`** (`Parent < 0 || Name ==
  "root_motion"`, DeadlockAnimator.cs ~line 152). Deadlock runs carry 4–6 m of root motion per loop, and RoR2 moves the
  body itself.
  - **Side effect:** for Haze, Seven and Infernus, `pelvis` is itself a root (see §8), so the pelvis' up/down bob from
    clips is also dropped. Rotations still play.
  - For Abrams, his ~170 parentless `$cloth_*` coat bones: since v0.3.0 the model builder attaches each to the nearest of the animations sheet's `clothParents` (pelvis, spine_0–3) and their clip tracks are skipped, so the coat follows his body instead of stretching to a fixed point (doc 02 §4.3).
  - This is a known open item (doc 03 §10).

---

## 8. Looking things up in Deadlock (exploration toolbox)

All of these read the VPK. None write to Deadlock. `D` = `.../Deadlock/game/citadel/pak01_dir.vpk`, CLI =
`C:\Users\jakeh\tools\vrf-cli\Source2Viewer-CLI.exe` (download: VRF GitHub release `20.0`, asset `cli-windows-x64.zip`).

| Goal | Command |
|---|---|
| List every file in the VPK | `Source2Viewer-CLI.exe -i "$D" -l` (we save it as `tools/dev/vpk_files.txt` with ` CRC:...` stripped) |
| List hero models | `-i "$D" -l -f "models/heroes" -e vmdl_c` |
| List a hero's folder (clips, materials) | `-i "$D" -l -f "models/heroes_staging/haze/"` |
| Portraits/icons | `-i "$D" -l -f "panorama/images/heroes/"`, `-f "panorama/images/hud/abilities/"` |
| Decompile hero/ability data | `-i "$D" -f "scripts/heroes.vdata_c,scripts/abilities.vdata_c" -d -o <scratch>` |
| Decompile one hero's sound events | `-i "$D" -f "soundevents/hero/haze.vsndevts_c" -d -o <scratch>` |
| Export a model to look at (glb) | `-i "$D" -f "<model>.vmdl_c" -d --gltf_export_format glb --gltf_export_materials --gltf_export_animations -o <scratch>` (open in Blender to check bones, meshes and clip names) |

**Dev listings in `tools/dev/` (gitignored, made on this PC from its install, used by `preflight.py`):**
- `vpk_files.txt`: every VPK path, used to check that `vmdl` and `vtex` paths in the sheets exist.
- `sound_events.txt`: every sound event name. Made by decompiling all `soundevents/*.vsndevts_c` and grepping top-level keys (`^\s<name> = $`).
- `anim_names.json`: `{hero: {anims:[clip short names], meshes:[mesh short names]}}`, read from each exported .glb's JSON chunk. Used to check clip names in `animations.json` and `meshInclude`.
- `addressable_keys.txt`: RoR2's Addressables keys (doc 02 §9). Not Deadlock, but stored alongside.

**Without these listings, preflight counts each missing listing as a problem**, so `gen.py` refuses to run. On a fresh
clone, regenerate them first (or use `python tools/gen.py --no-preflight` knowingly). The commands, run from Git Bash
with `CLI=/c/Users/jakeh/tools/vrf-cli/Source2Viewer-CLI.exe`, `D=<pak01_dir.vpk>` and `S=<scratch folder>`:
```bash
# vpk_files.txt
"$CLI" -i "$D" -l 2>/dev/null | sed -E 's/ CRC:.*//' > tools/dev/vpk_files.txt
# sound_events.txt  (decompile every sound event file, keep the top-level event names)
"$CLI" -i "$D" -f "soundevents/" -e vsndevts_c -d -o "$S/dl" >/dev/null 2>&1
grep -rhoE "^\s[A-Za-z0-9_.]+ = $" "$S/dl/soundevents" | sed -E 's/^\s+//; s/ = $//' | sort -u > tools/dev/sound_events.txt
# anim_names.json  (export each hero's model once, then read the glb JSON chunk)
"$CLI" -i "$D" -f "<hero vmdl>_c" -d --gltf_export_format glb --gltf_export_animations -o "$S/<hero>.glb"
python - "$S/haze.glb" "$S/abrams.glb" "$S/seven.glb" "$S/infernus.glb" <<'EOF' > tools/dev/anim_names.json
import json,struct,sys
out={}
for p,h in zip(sys.argv[1:],['haze','abrams','seven','infernus']):   # extend for new heroes
    f=open(p,'rb'); hdr=f.read(20); ln=struct.unpack_from('<I',hdr,12)[0]; j=json.loads(f.read(ln))
    out[h]={'anims':sorted(set(a['name'].split('/')[-1] for a in j['animations'])),'meshes':[m['name'].split('.')[-1] for m in j['meshes']]}
print(json.dumps(out))
EOF
# addressable_keys.txt  (RoR2, see doc 02 §1)
```
`anim_names.json` must contain the new hero's **meshes as well as its clips**, because preflight checks `meshInclude` against it.

**Ability numbers** (`deadlockCooldown`, `deadlockDamage` in `skills.json`) were read by hand from the decompiled
`scripts/abilities.vdata` at build 6774 (e.g. `citadel_ability_bull_charge`, `ability_sleep_dagger`). They document
the source values. The RoR2-scale numbers next to them are our own balance (doc 02 §7).

**Bone names that matter** (Deadlock's skeleton convention, shared by heroes):
- **Roots:**
  - `root_motion` is bone 0.
  - In the DLM for **Haze, Seven and Infernus**, `pelvis` is a *separate* root (parent −1), not a child of `root_motion`.
  - For **Abrams**, `pelvis` sits under `root_motion`, and about 150 cloth bones (`$cloth_m0p*`) are roots of their own.
- Spine chain: `pelvis` → `spine_0` → `spine_1` → `spine_2` → `spine_3` → `neck_0` → `head`.
- Arms: `clavicle_L/R` → `arm_upper_*` → `arm_lower_*` → `hand_*`.
- Gun bones:
  - **Haze:** `weapon_hand_R` and `weapon_hand_L` sit under the hands.
  - **Seven:** only `weapon_hand_R`, then `pump`, then `muzzle_fx`.
  - `muzzle_fx` (and `muzzle_fx_L` for Haze's left gun) mark the barrel tip. For Infernus, `muzzle_fx` sits on the index fingertip.
  - **Abrams is different:** his shotgun is skinned to `root_motion/weaponPivot/weapon`, a child of the root rather than the hand. In Deadlock it is held by IK, so we weld it to `hand_R` ourselves (doc 02 §6).
- `tools/dlm_pose.py <hero> [frame] [clip-substring]` prints, for every clip, where the gun points (yaw/pitch relative to +Z) and the `weapon`-to-`hand_R` distance. This is how we found that the `shoot_idle*` clips (frame 0) point the guns 40–68° down: Haze −59°, Abrams −46°, Seven
  −40°, Infernus −68°. They also move Abrams' `weapon` 1.75 m away from his hand.
  `weapon_stand_idle` frame 0 holds every gun **near level** (+6° to +16° pitch: Haze +7°, Abrams +16°, Seven +7°, Infernus +13° with 24° yaw), so it is the `aimPose`. The aim code measures the gun's actual
  direction, so these small offsets are corrected at runtime.
- Composed `jump_landing` is broken:
  - For Haze and Abrams it collapses (dlm_pose prints NaN, and Abrams' hand lands at the origin).
  - Infernus still uses it as `land`; its frame 0 equals `shoot_idle`. His gun arm is held by the rig during `land`, so it doesn't show.
- **`dlm_pose.py` limits:**
  - It reads only `%LOCALAPPDATA%\DeadlockOfRain\cache` (no path argument).
  - It knows only the bone names `muzzle_fx`, `muzzle_fx_L` and `weapon`. A new hero with other names needs the script extended.
- The other dlm tools take a `.dlm` path and **must be run from the repo root** (they load `tools/dlm_check.py` by relative path).

**Mo & Krill (added 2026-10-19, v0.7.0):**
- **Codename** `krill` (`hero_krill`, hero id 18); model `models/heroes_staging/digger/digger.vmdl` ("digger" is the old name of the model),
  images `panorama/images/heroes/digger_sm_psd` / `digger_card_psd`, gun icon `heroes/guns/digger_gun_psd`, ability icons in
  `hud/abilities/grappler/` (`grappler_throw_sand`, `grappler_spin` = Burrow, `grappler_regen` = Scorn, `grappler_combo` = Combo).
- **Meshes** `body` (Mo), `rider` (Krill **and his gun**: materials `digger_rider_gun`, `digger_rider_shape`), `hat` (merged into
  `digger_shape`); LODs dropped. Glb in metres, body 2.319 m tall, 2.04 m wide (`sourceHeight` 2.319).
- **Skeleton:** Mo's standard chain (`pelvis`, `spine_0..3`, `head`, arms, legs: the 12 ragdoll bones work) plus Krill's `rider_*`
  chain whose root `rider_pelvis` is a child of Mo's `spine_0`; gun under `rider_hand_R` → `rider_weapon_hand_R` → `muzzle_fx`.
- **Clips** (glb names): `weapon_stand_idle`, `weapon_run_n/s/e/w`, `jump_start`, `jump_in_air_loop`, `primary_stand_reload`,
  `ability_throw_sand`, `ability_burrow_intro_in_ground`, `burrowing_loop`, `ability_burrow_spinning_start`, `ability_tornado_cast/idle`
  (an old tornado spin, arms out: Dust Devil), `item_throw`, `melee_quick_1/2`. No death or sprint clip (ragdoll; run).
- **Gun** (`citadel_weapon_digger_set`): 4 pellets at fixed `m_vecScatterOffsets` [0,0] [0,-0.66] [-0.5,0.5] [0.5,0.5], 2.82 each,
  cycle 0.189 s, clip 20, reload 2.82 s, falloff 20 → 57.5 m.
- **Abilities:** Scorn `ability_intimidate`; Burrow `ability_burrow` (40 s, 5 s channel, +5 m/s, 60 % bullet resist, spin 1.5 s 75 DPS 5 m,
  `EnemyDamageSpeedPenalty` 0.5); Sand Blast `ability_throw_sand` (40 s, 25 m, 5 m wide + 0.5 m/m, disarm 2.5 s, speed 3500 u/s);
  Combo `ability_ult_combo` (2.4 s channel stun, +40 max health per kill).
- **Sounds** (`soundevents/hero/krill.vsndevts`): `MoKrill.Wpn.Fire`, `.Wpn.Reload.Start/Mid/End`, `.Sandblast.Cast/.Impact`,
  `.Burrow.Cast/.Cast.Delay/.Modifier.Start/.Travel.Lp/.Spin/.Spin.Lp/.Spin.End`, `.Combo.Channel.Start/.Hit/.Cast/.Duration.Lp`,
  `.Melee.Swing`, `.Footstep`, `.JumpLand`. **Voice:** Krill only, `sounds/vo/krill/krill_<group>`.
- **Particles** (looked at for colours only, nothing exported): `particles/abilities/digger/digger_throw_sand*`, `digger_burrow_*`,
  `digger_ult_tgt_*`, `weapon_fx/digger/digger_tracer*` (tan sand 236,218,186; orange 226,132,32; dark cracks 51,46,41; hot cracks 237,107,20).

**Next-hero cheat-sheet: Wraith** (found in the build 6774 listings; verify before use):
- **Model:** `models/heroes_wip/wraith/wraith.vmdl`. Other variants exist (`wraith_gen_man`, `wraith_magician`).
- **Images:** portraits `panorama/images/heroes/wraith_sm_psd.vtex`, `wraith_card_psd.vtex`; gun icon `panorama/images/heroes/guns/wraith_gun_psd.vtex`.
- **Ability icons:** `panorama/images/hud/abilities/wraith_card_trick_psd`, `wraith_teleport_psd`, `wraith_lift_psd`, `wraith_aura_psd`, `wraith_daggers_psd`.
- **Sound events:**
  - Gun: `Wraith.Wpn.Fire.Main`, `Wraith.Wpn.Reload.Start`
  - Card Trick: `Wraith.CardTrick.Cast`, `.Hit`, `.Proj.Impact`
  - Project Mind: `Wraith.ProjectMind.Cast`, `.Arrive`
  - Full Auto: `Wraith.RapidFire.Cast`
  - Telekinesis: `Wraith.Telekenisis.Cast.Delay`, `Wraith.Telekenisis.Proj.Lp`, `Wraith.Telekenisis.Slam`, `Wraith.Telekinesis.Hit`
- **Clip hints** (confirm in the glb): `weapon_stand_idle`, `weapon_run_n/s/e/w`, `jump_ground`, `jump_air`, `reload_idle`, `death_idle_1`, `ability_card_trick`, `ability_project_mind`, `ability_full_auto`, `ability_telekinesis`, `lift_loop`.
- **Kinds:**
  - Card Trick: `projectile`
  - Full Auto: `self_buff` with a new attack-speed buff
  - Project Mind: needs a new **teleport** kind
  - Telekinesis: the ult, needs a new **lift-and-stun** `UltState` kind

---

## 9. After a Deadlock update: re-validation checklist

1. Read the new `ClientVersion` from `game/citadel/steam.inf`.
2. Regenerate the dev listings (section 8) and run `python tools/preflight.py`. It reports every sheet cell that no
   longer resolves: a missing vmdl or vtex, a renamed clip, a mesh or sound event.
3. Fix the sheets (`heroes.json` meshInclude/vmdl/images, `animations.json`, `sounds.json`, `skills.json` icons), then
   run `python tools/gen.py`.
4. Run the extractor with `--force` into a scratch folder and read `extract.log`:
   - `clips not found`, `missing image`, `sound event ...: no vsnd files`
   - each hero's height, against `sourceHeight`
5. If a hero's body or head changed size (check with `tools/dlm_sub.py`, see §6.3), update `heroes.sourceHeight` and `heightScale` (= height / Haze's height). Haze is the
   anchor (`heroes.json` → `anchorHero`), so if *Haze* changed, every `heightScale` changes.
6. Run `python tools/dlm_pose.py <hero>` to confirm the `aimPose` clip still holds the gun level (it knows only the bone names `muzzle_fx`, `muzzle_fx_L` and `weapon`; see §8).
7. Run the full in-game test (doc 03).

---

## 10. Adding another Deadlock hero: the Deadlock half

(The RoR2 half is in doc 02 §11, and the full checklist is in doc 03 §8.)
1. **Find the hero:**
   - **Model path:** take `vmdl` from the hero's `m_strModelName` in the decompiled `scripts/heroes.vdata`, **not** from a folder search. `heroes_staging/` and `heroes_wip/` can both hold a `<name>.vmdl`. For example, `heroes_staging/wraith/wraith.vmdl` is only her gun, and the hero is `heroes_wip/wraith/wraith.vmdl`, the folder with `clips/` and hundreds of files.
   - **Portraits:** `panorama/images/heroes/<codename>_sm_psd.vtex` (small) and `_card_psd.vtex` (card). Some files have hashed duplicates (`..._psd_8dd14209`); pick the unhashed one.
   - **Gun icon:** `panorama/images/heroes/guns/<codename>_gun_psd.vtex`.
   - **Ability icons:**
     - Location: `panorama/images/hud/abilities/`, sometimes in a hero subfolder (`haze/`) and sometimes flat (`bull_*`, `giga_*`, `inferno_*`, `wraith_*`).
     - Map icons to abilities with `abilities.vdata`'s `m_strAbilityImage`.
   - **Sound events:** `soundevents/hero/<codename>.vsndevts`, plus voice lines in `soundevents/vo/generated_vo_hero_<codename>.vsndevts`. Valve's spelling is not consistent (Wraith has both `Telekenisis` and `Telekinesis`), so copy event names exactly from the decompiled file.
   - `deadlockKey` is documentation only; preflight doesn't check it.
2. **Export the model once** to a scratch .glb (section 8) and open it in Blender or read its JSON. **Clip names must come from the glb.** The names of `clips/*.vnmclip_c` files in `vpk_files.txt` are only a hint and can differ (Haze's `primary_sprint_n` and `jump_in_air_loop` don't appear there). When a hero has no clip for a column (e.g. no sprint), use the nearest clip (`weapon_run_n`) or `none` where allowed.
   - The mesh names to keep (body, head, gun; drop LODs and props) go in `meshInclude`.
   - The clip names for idle, 4-way run, sprint, jump, land, reload, each ability and the ult go in `animations.json`.
   - The bone names: confirm `spine_0..3`, `clavicle_*`, `hand_*`, `weapon_hand_*`, `muzzle_fx`, and whether the gun hangs off the hand or off a separate pivot (Abrams).
3. **Add the hero to the dev listings.** Re-run the `anim_names.json` script (§8) with **every** hero's glb, old and new, and extend its name list. It rewrites the whole file. Preflight reports a hero missing from `anim_names.json` as a problem.
4. **Add the rows:**
   - `heroes.json`: `id, displayName, deadlockKey, vmdl, meshInclude, imageSmall, imageCard, soundevents, sourceHeight` (measured as in §6.3), `heightScale, widthScale, primaryColor, sortPosition, roR2Analog, subtitle, passiveName, passiveDescription, passiveIcon, lore`
   - `animations.json` (`aimPose` = a clip whose frame 0 holds the gun level; check with `dlm_pose.py`)
   - `sounds.json` (one row per skill; look the events up in the decompiled vsndevts)
   - `skills.json`, `stats.json`, `buffs.json` if needed
5. Run `python tools/gen.py` (runs preflight first), then the extractor with `--force` **and `--manifest extractor/manifest.json`**.
   Without `--manifest` the exe reads `build/extract/manifest.json`, the copy next to it, which is only refreshed when you `dotnet publish` again.
6. Preflight also requires `heightScale = sourceHeight / anchor sourceHeight` (within 0.002), `gunArms ∈ {R, LR}` and `aimPose ≠ none`.
   `ANIM_RIG_COLUMNS` is duplicated in `gen.py` and `preflight.py`, so a new non-clip column in `animations.json` must be added to both.
