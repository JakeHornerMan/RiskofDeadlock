# 03: Process overview: how Deadlock of Rain is made, tested and shipped

> **Audience:** anyone (human or agent) picking this project up cold. Start with `docs/00-HANDOFF.md` (current state)
> and the newest entries of `docs/07-SESSION-LOG.md` (history), then this file, then docs 01 and 02 for depth.
> **Keep this file current.** Any change to the workflow, tools, release process, test checks, versions or project
> decisions must be reflected here in the same commit (see `CLAUDE.md`).

---

## 1. What this project is

**Deadlock of Rain** (repo folder `RiskofDeadlock`) is a **Melty mashup** that adds four **Deadlock** heroes (Haze,
Abrams, Seven, Infernus) to **Risk of Rain 2** as playable survivors. Each hero has:
- the real Deadlock model, animations, portrait, ability icons and sounds, read from the player's own Deadlock install
- RoR2's **four skills**: Deadlock gun (Left Click), two Deadlock abilities (Right Click, Shift) and a new **R** move (from a Deadlock item, melee or ability)
- their Deadlock **ultimate as RoR2 equipment (Q)**: held from the start of every run, 120 s cooldown, Preon Accumulator power, droppable and findable by anyone (v0.4.0)
- stats balanced against a vanilla analog, growing per level like vanilla survivors
- a size scaled so Haze = Commando, with Deadlock's relative heights kept
- the gun locked in the hand and pointing where the camera aims

**The creator's requirements** (original request, then playtest feedback):
1. Deadlock characters selectable in RoR2.
2. **Scaled properly** relative to RoR2 characters.
3. **Four abilities** from their Deadlock kit. Later clarified: RoR2 has 4 active skills, so gun + 2 abilities + ult on R.
4. Their **Deadlock ultimate**, **locked when selected**, charging up from kills and damage. (v0.4.0, creator: the ults became Q equipment instead, each hero starts holding theirs, 120 s, Preon-level, droppable; R got a new move.)
5. **Balanced** like RoR2's base survivors.
6. **Base stats increase on level-up** like RoR2 survivors.
7. (Playtest) The gun paired to the hand, the hand facing the camera direction, and no animation on the gun hand.
8. (Playtest) Every ability must really do something to an enemy or the player.

**Interview choices:**
- Roster: Haze, Abrams, Seven, Infernus.
- Ult unlock: charges from kills and damage.
- **Solo only.**
- Licence: **All rights reserved**, remixes on Melty **allowed**.

---

## 2. The big picture: two games, one mod, Melty in the middle

```
                       ┌──────────────── Melty (melty.gg app + MCP tools) ───────────────┐
 Player clicks Play →  │ installs BepInEx 5 into RoR2 · copies our zip:                   │
                       │   plugins/  → {RoR2}/BepInEx/plugins  (zip has DeadlockOfRain/)   │
                       │   extract/  → %LOCALAPPDATA%/DeadlockOfRain/extract/             │
                       │ runs setup once: DeadlockExtract.exe --deadlock {Deadlock}       │
                       │   --out %LOCALAPPDATA%/DeadlockOfRain/cache  (waits ready.json)  │
                       │ launches Risk of Rain 2                                          │
                       └─────────────────────────────────────────────────────────────────┘
 Deadlock (companion, read-only)                    Risk of Rain 2 (primary, modded)
 game/citadel/pak01_dir.vpk ──DeadlockExtract──► cache ──DeadlockOfRain.dll (BepInEx)──► 4 new survivors
   models · clips · icons · sounds         (DLM, PNG, WAV)    ContentPack · bodies · skills · HUD · audio
```

- **Risk of Rain 2** is the **primary** game (the one we mod and launch).
- **Deadlock** is a **companion**: read only, never modified, never launched.
- **Nothing from either game ships in our zip.**

---

## 3. Repository layout (`C:\Users\jakeh\Documents\GitHub\RiskofDeadlock`)

```
CLAUDE.md                 rules for agents (read first) – points here
README.md                 player-facing readme (ships in the zip)
LICENSE.txt               All rights reserved; remix on Melty allowed (ships)
THIRD-PARTY-NOTICES.txt   MIT/BSD/Apache notices for what the extractor bundles (ships)
MODLOG.md                 short journal: versions, lab notes, gotchas
docs/01-DEADLOCK-ASSETS.md            Deadlock side in depth
docs/02-RISK-OF-RAIN-2-INTEGRATION.md RoR2 side in depth
docs/03-PROCESS-OVERVIEW.md           this file
docs/05-DAMAGE-COMPARISON.md         DPS benchmark vs Commando: variables, results, balance suggestions, retest flags (§0)
docs/06-CURRENT-ABILITIES.md         every survivor's current abilities, numbers and DPS (living; updated with every ability change)
docs/04-ABILITY-REDESIGN-PROPOSALS.md planning: current kit + 2 alternatives (abilities and passive) per hero, from the Deadlock and RoR2 wikis
design/                   THE SOURCE OF TRUTH – JSON sheets (heroes, stats, skills, buffs, animations, sounds, effects, hooks)
tools/preflight.py        checks every sheet cell and cross-reference (and Deadlock/RoR2 names against tools/dev listings)
tools/gen.py              runs preflight, then generates plugin/Generated/Sheets.g.cs + extractor/manifest.json
tools/package.py          builds build/DeadlockOfRain-<ver>.zip (+ .json with size, sha256, entries)
tools/dev_run.sh          kill RoR2, re-enable BepInEx, deploy the built DLL, launch via Steam
tools/run_test.sh         full local test run (test mode + devshots), prints every check
tools/lab_restore.sh      put the PC back the way Melty set it up
tools/kit_pages.py        builds the hero kit pages + balance page (claude.ai Artifacts, links in doc 06) into build/pages/
tools/run_bench.sh        DPS benchmark (Monsoon, 12-enemy pack, 30 s passes) -> build/bench_results.txt; closes the game itself (doc 05)
tools/dlm_*.py            DLM debugging (skinning check, triangles, submeshes, gun pose per clip)
tools/dev/                (gitignored) listings from this PC's installs: vpk_files.txt, sound_events.txt, anim_names.json, addressable_keys.txt
extractor/                DeadlockExtract (.NET 10, VRF) – Program.cs, DlmWriter.cs, manifest.json (generated)
plugin/                   BepInEx plugin (netstandard2.1) – see doc 02 §2
shots/                    screenshots (listing shots in shots/final)
build/                    (gitignored) extract/DeadlockExtract.exe (+ its own manifest.json copy), release zips + <zip>.json, checks.txt from local test runs
lab-backup/               (gitignored) where other mashups were parked during early lab work (empty/unused now)
```

---

## 4. The design-sheet workflow (always start here)

The JSON sheets in `design/` are **the source of truth**. Code reads generated rows and never hard-codes a number,
clip name, sound event, effect key or icon.

| Sheet | One row per | Key columns |
|---|---|---|
| `heroes.json` | hero | `id, displayName, deadlockKey, vmdl, meshInclude, imageSmall, imageCard, soundevents, sourceHeight, heightScale, widthScale, primaryColor, sortPosition, roR2Analog, subtitle, passiveName, passiveDescription, passiveIcon, lore`; top level `anchorHero`, `anchorRoR2Body` |
| `stats.json` | hero | `hero, analog, baseMaxHealth, levelMaxHealth, baseRegen, levelRegen, baseDamage, levelDamage, baseArmor, levelArmor, baseAttackSpeed, levelAttackSpeed, baseCrit, levelCrit, baseMoveSpeed, baseJumpPower, baseJumpCount, baseAcceleration, baseMaxShield, sprintingSpeedMultiplier` |
| `skills.json` | skill (5 per hero) | `id, hero, slot, deadlockAbility, name, description, kind, icon, damagePct, procCoefficient, cooldown, stocks, duration, minDuration, interval, radius, range, speed, clipSize, reloadTime, pellets, spreadDeg, burst, stunDuration, knockback, healFraction, moveSpeedMult, chainCount, buffStacks, buff, dot, effect, vfx, extra, deadlockCooldown, deadlockDamage, targetDps, expect` |
| `equipment.json` | equipment (1 per hero) | `id, hero, skill, pickupText, lore, canDrop, enigmaCompatible, randomTrigger` |
| `buffs.json` | buff | `id, name, icon, color, canStack, isDebuff, isHidden, maxStacks, duration, moveSpeedAdd, attackSpeedAdd, armorAdd, regenAdd, damageTakenFromOwnerPerStack, cloak, evasionChance, dotDamagePct, notes` |
| `animations.json` | hero | `hero, upperBodyBone, gunArms, gunBone, gunHand, muzzle, muzzleLeft, clothParents, aimPose, idle, run_n, run_s, run_e, run_w, sprint, jump_start, jump_air, land, fire, fire_alt, reload, secondary, utility, utility_loop, special_start, special, ult_start, ult_loop, ult_end, death` |
| `sounds.json` | skill | `skill, cast, loop, hit, end, reload` (a Deadlock sound event per moment) |
| `effects.json` | effect | `id, kind, addressable, scaleWithRadius, notes` |
| `hooks.json` | hook | `id, method, target, purpose, reads, side` |

Each sheet has an `about` that explains its columns. **Unused cells are `0` or `"none"` on purpose**, never empty.

**Loop:**
1. Edit the sheets.
2. `python tools/preflight.py`. It must print `PREFLIGHT CLEAN`. It checks:
   - **Cells:** every cell is present and non-empty, and there are no undeclared columns.
   - **References resolve:** hero, buff, effect, skill, sound; `dot` must be `burn` or `none`.
   - **Row counts:** exactly one stats row and one animations row per hero, and one sounds row per skill.
   - **Kinds:** every `kind` has a `[Kind]` state in `plugin/States`.
   - **Slots:** exactly `primary`, `secondary`, `utility`, `special`, `equipment` per hero; secondary, utility and special need a cooldown; equipment has 120 s and exactly one `equipment.json` row naming it.
   - **Balance:** gun DPS math within ±10 % of `targetDps` and inside 240–600 %/s; equipment ults at least 6000 % per enemy (no upper limit); `extra` flags only on their kind (`pierce` on projectile, `shockwave` on heavy_melee).
   - **`expect`:** every entry is a known effect, and buff names resolve.
   - **Passives:** the columns are all set or all `none`.
   - **Gun rig:** `gunArms` is R or LR, and `aimPose` is not none.
   - **Scale:** `heightScale = sourceHeight / anchor sourceHeight` (±0.002).
   - **Names against the `tools/dev` listings:** vmdl/vtex exist in the VPK, sound events exist, clips and meshes exist in the hero model, Addressables keys exist.
   - **A missing listing counts as a problem**, so on a fresh clone generate `tools/dev/` first (doc 01 §8, doc 02 §1).
3. `python tools/gen.py`: runs preflight and **stops on any problem except a missing `[Kind]` state** (so states can be written after the sheet rows), then writes `plugin/Generated/Sheets.g.cs` and `extractor/manifest.json` (heroes, images, soundEvents, soundEventFiles). `--no-preflight` skips the check entirely; avoid it.
4. `dotnet build plugin -c Release`.
5. If the manifest **adds** Deadlock content (a clip, image, sound or hero), rebuild the cache (about 8 min). From Git Bash in the repo root:
   `build/extract/DeadlockExtract.exe --deadlock "C:/Program Files (x86)/Steam/steamapps/common/Deadlock" --out "$LOCALAPPDATA/DeadlockOfRain/cache" --manifest extractor/manifest.json --force`
   - **`--manifest` matters.** Without it the exe reads `build/extract/manifest.json`, a stale copy from publish time.
   - Removing content needs no rebuild.
   - If a forced run fails, delete `cache/ready.json` (doc 01 §6.6).
6. If `extractor/*.cs` changed: `dotnet publish` the extractor (doc 01 §6.7), then step 5 with `--force`.

---

## 5. The dev and test loop (on this PC)

**Fresh clone or new PC:**
1. Install the prerequisites below.
2. Generate `tools/dev/` (doc 01 §8, doc 02 §1).
3. Publish the extractor exe (doc 01 §6.7); `build/` is not in git.
4. Recreate the decompiled RoR2 source: `ilspycmd -p -o C:/Users/jakeh/ror2-decomp/ror2 "<RoR2>/Risk of Rain 2_Data/Managed/RoR2.dll" -r "<RoR2>/Risk of Rain 2_Data/Managed"`.
5. Install the mashup once with Melty, so BepInEx is present.

On another PC, build the plugin with `dotnet build plugin -c Release -p:RoR2Dir="<RoR2 folder>"`.

**Prerequisites on the dev PC:**
- Both games installed under `C:\Program Files (x86)\Steam\steamapps\common\`.
- Steam **signed in**.
- .NET 10 SDK, Python 3.11 (`numpy`, `Pillow` for the dlm tools and montages).
- `ilspycmd` (`~/.dotnet/tools`), the VRF CLI in `C:\Users\jakeh\tools\vrf-cli`, universal-modder in `C:\Users\jakeh\tools\universal-modder` (`um win launch|kill|shot|ps`).

**The lab:**
- Melty installed BepInEx into RoR2.
- **Between its own launches, Melty switches BepInEx off** by renaming `winhttp.dll` to `winhttp.dll.melty-off`.
- **Another mashup (Left4Rain) is installed too.** It is back in `BepInEx/plugins` and loads together with ours without problems. MODLOG's note about moving it to `lab-backup/` describes the early lab work, which has been undone.

**Steps:**
1. `bash tools/run_test.sh [checks-file]`:
   - kills a running RoR2 and deletes the old BepInEx log, Player.log and checks file, so nothing stale is read
   - writes the test-mode file (`{ "checks": "<path>" }`; the plugin parses it by hand) with the checks file defaulting to `build/checks.txt`, and creates the devshots folder
   - `dev_run.sh` copies `winhttp.dll.melty-off` → `winhttp.dll` (fails loudly if BepInEx isn't installed), copies the fresh DLL into `BepInEx/plugins/DeadlockOfRain/` and launches with `um win launch --steam 632360`
   - waits for `Melty test finished` (a healthy run takes about 6–7 min; it gives up after 20) and prints every skill's measured effect and every check
   - If the run never starts (e.g. no run could be created), no "finished" line appears and the wait runs to its limit.
2. Read the output. A healthy run ends with every check `pass` and `N/N heroes` (`4/4` today). The final line always carries the text "every skill did what its sheet expects", so look at the number in front of it.
   Each skill line shows damage, stun, burn, buffs, heal, distance moved and cloak, with `-> ok` or `-> MISSING …`.
3. Look at the screenshots in `%LOCALAPPDATA%\DeadlockOfRain\devshots`:
   - `<hero>_aim+30_game.png`, `_aim0_`, `_aim-30_` (over the shoulder)
   - `<hero>_aim*.png` (side render)
   - one per skill id
   - `lobby.png`, `lineup_cam.png`
4. `bash tools/lab_restore.sh` when done. It closes the game, removes the test file, devshots and `build/checks.txt`, and removes the `winhttp.dll` copy. It then lists `BepInEx/plugins` so you can see Left4Rain and DeadlockOfRain are both there.

**User preference (important):** when a build is ready for the creator to playtest, **do not launch the game for
them**. Leave it installed and tell them it is ready.

**Debugging aids:**
- BepInEx log: `<RoR2>/BepInEx/LogOutput.log`.
- Unity log with exceptions: `%USERPROFILE%\AppData\LocalLow\Hopoo Games, LLC\Risk of Rain 2\Player.log`.
- Window screenshot: `um win shot --exe "Risk of Rain 2.exe" out.png --scale 0.5` (Windows.Graphics.Capture, works while covered).
- Window list: `um win ps`. A Steam window titled "Sign in to Steam" means launches silently fail.
- DLM: `python tools/dlm_pose.py <hero> [frame] [clip-substring]` reads the cache directly. `dlm_sub.py`, `dlm_check.py` and `dlm_tris.py` take a `.dlm` path and must be run from the repo root.
- Decompiled RoR2: `C:\Users\jakeh\ror2-decomp\ror2`, or `ilspycmd -t <Type> RoR2.dll`.

---

## 6. Releasing on Melty

Melty MCP tools (`mcp__melty__*`) do everything. **The Melty token is a secret.** It is only ever sent in Melty's
Authorization header by the tools. Never put it in files, commits, logs or replies, and never send it to upload URLs.

| Item | Value |
|---|---|
| Mod id | `c713920e-6e21-4bfa-ae4a-12c4a50868d9` |
| Slug | `deadlock-of-rain` |
| Listing | https://melty.gg/m/deadlock-of-rain |
| Studio | https://melty.gg/studio/c713920e-6e21-4bfa-ae4a-12c4a50868d9 |
| Melty progress-log workId | `3b07563c-694a-493a-9547-d27dfc179c87` (`consult_melty` once per turn, with the full reply) |
| Licence / remix | All rights reserved / remix allowed |
| Game slugs | `risk-of-rain-2` (primary), `deadlock` (companion) |

**Steps for a new version** (0.2.0 is the reference):
1. Bump `Plugin.Version` (`plugin/Plugin.cs`), build, then `python tools/package.py <ver>`. **Always pass the version**, because it defaults to 0.1.0. This writes `build/DeadlockOfRain-<ver>.zip` and `build/DeadlockOfRain-<ver>.zip.json` (size, sha256, entries, plus an absolute local path; don't upload it).
   The zip contains:
   - `plugins/DeadlockOfRain/{DeadlockOfRain.dll, README.md, LICENSE.txt}`
   - `extract/{DeadlockExtract.exe, manifest.json, THIRD-PARTY-NOTICES.txt, LICENSE.txt}`

   `extract/manifest.json` comes from `extractor/manifest.json`, so it is current. If `extractor/*.cs` changed, publish the exe first.
2. `inspect_package`: fileName, entries, gameSlugs `["risk-of-rain-2","deadlock"]`, and the README feature text as `says`.
3. Recipe. Start from the last one and change only the fileName and checks:
```json
{ "schemaVersion": 1, "mode": "installed",
  "games": [ {"slug":"risk-of-rain-2","role":"primary"}, {"slug":"deadlock","role":"companion"} ],
  "components": [ {"id":"main","fileName":"DeadlockOfRain-<ver>.zip","label":"Deadlock of Rain","kind":"main","required":true} ],
  "requirements": [ {"kind":"loader","id":"bepinex-5"} ],
  "mappings": [ {"component":"main","from":"plugins/","to":"{game}/BepInEx/plugins"},
                {"component":"main","from":"extract/","to":"{localappdata}/DeadlockOfRain/extract"} ],
  "setup": { "label":"Deadlock heroes",
             "launch": {"kind":"exe","path":"{localappdata}/DeadlockOfRain/extract/DeadlockExtract.exe",
                        "args":["--deadlock","{game:deadlock}","--out","{localappdata}/DeadlockOfRain/cache"]},
             "done": {"file":"{localappdata}/DeadlockOfRain/cache/ready.json"} },
  "launch": {"kind":"game"},
  "runtimeData": ["{localappdata}/DeadlockOfRain"],
  "test": { "checks": ["deadlock-content","survivors-listed","haze-plays","abrams-plays","seven-plays","infernus-plays",
                       "ult-equipment","gun-aims","gun-in-hand","four-slots","level-stats","hero-scale"], "minutes": 8 } }
```
   - **Max 12 checks.** Only list names the mod really reports.
   - **Do not** take `inspect_package`'s draft mappings. It suggests `{game}/BepInEx/plugins/DeadlockOfRain-<ver>`, which is version-named, and puts the extractor inside the plugins folder.
4. `validate_recipe` (with `slug` and entries) → expect `valid`, one-click `yes`.
5. `start_upload` (fileName, size, sha256, modId) → `curl -X PUT -H "content-type: application/octet-stream" --data-binary @zip "<url>"` → `finish_upload`.
6. `submit_release` (modId, version, uploadIds, recipe, changelogMd) → a draft. The only finding is the expected "executable-code: review".
7. `test_release` (3 runs). **Tell the creator straight away what its `tellCreator` says.** Melty on this PC installs the release, plays it 3 × 8 min in test mode, and reports. Poll with `test_result`. 0.2.0 passed 3/3 (`graduates: true`).
8. `update_mod` for listing text (description, tagline) when features change. `add_screenshot`/`finish_screenshot` for real in-game shots.
9. **Ask the creator before `publish`.** Only after a clear yes: `publish`, then `mod_status`. Say "live" only if the server says so; otherwise it is "awaiting review".

**Status at the time of writing:**
- 0.1.0 and 0.2.0 are both draft releases. 0.2.0's test passed.
- **Not yet published.** Waiting for the creator's playtest and go-ahead.
- The listing screenshots are from 0.1.0.

---

## 7. Git conventions

- Repo `main`, remote `https://github.com/JakeHornerMan/RiskofDeadlock`.
- **Commit after each logical step.** The creator wants edits tracked.
- **Do not push unless asked.**
- Commit messages end with the attribution line from the session instructions.
- Tags: `v0.1.0`, `v0.2.0`.
- **Push state** (as of writing): `origin/main` is at `ca07a43`. Every later commit and both tags are **local only**, not pushed. Push only when the creator asks: `git push origin main --tags`.
- **Never commit:** `build/`, `bin/`, `obj/`, `lab-backup/`, `tools/dev/`, `*.dlm` (all gitignored), any Deadlock or RoR2 file, the Melty token.
- **Note:** one commit, `ca07a43`, was made from GitHub Desktop with a message meant for another project ("Maths I Linear Algebra\Matrix multiplication"). Its content is ours and correct. It is left as-is, because rewriting would need a force-push.

---

## 8. Checklist: adding a new Deadlock hero

0. **Read the three per-doc checklists** (doc 01 §10, doc 02 §11 including its §0 test-mode limits, and this one); each covers a different part. Doc 01 §8 has a ready asset cheat-sheet for **Wraith**.
1. **Pick a hero** and a RoR2 analog (play style, durability). Read their kit in the decompiled `scripts/abilities.vdata` and `scripts/heroes.vdata` (doc 01 §8). Choose the gun, **two** abilities, an R move and the ultimate (the Q equipment).
2. **Deadlock half** (doc 01 §10): find the vmdl, mesh names, clip names, bones, images and sound events. Update the dev listings.
3. **Sheets:** `heroes.json`, `stats.json`, `skills.json` (4 rows with `expect`), `animations.json` (gun rig + clips), `sounds.json`, maybe `buffs.json` and `effects.json`.
4. **Code only if a new mechanic is needed:** a new `[Kind]` state (doc 02 §11).
5. **Run:** `gen.py` (preflight clean) → build plugin → extractor `--force` → `tools/run_test.sh` until every check passes → inspect the devshots (scale next to Commando, gun in hand, aim) → `lab_restore.sh`.
6. **Recipe:** add `<hero>-plays`. The recipe is at the 12-check cap, so drop `four-slots` (doc 02 §11), and raise `test.minutes` by about 2 per hero.
7. **Docs:** update **all three** docs, CLAUDE.md's hero list, README/listing text, and the "four heroes" wording in §1. **Commit.**
8. **Release** as in §6. Ask before publishing.

---

## 9. History of decisions (why things are the way they are)

| Decision | Why |
|---|---|
| No R2API; RoR2's own ContentManager | One less dependency, no version conflicts with other mods |
| Custom DLM format instead of glTF at runtime | The plugin needs no glTF/JSON libs; conversion to Unity space happens once in the extractor |
| Extractor runs on the player's PC (Melty `setup`) | Legal: we ship no Deadlock files. Deadlock updates are picked up by re-running. |
| Legacy `Animation` with runtime clips | No Unity editor or AssetBundles needed; clips come straight from the cache |
| One SkinnedMeshRenderer per material | RoR2 CharacterModel's one-material-per-renderer behaviour |
| Haze = Commando height, one global factor | Requirement: scale relative to RoR2 characters while keeping Deadlock's relative sizes |
| Gun rig (rigid arms, weld, chest aim, recoil) | Playtest: guns floated or pointed at the floor when firing; the user asked for gun locked to hand, aimed at camera, no arm animation |
| 4 skills, ult on R (v0.2.0) | Playtest: "risk of rain only has 4 active abilities". Ability 3 dropped; Haze and Infernus keep their gun on-hit effects as passives. |
| `expect` column + effect-measuring test | Playtest: "a lot of the abilities are not working". The 0.1.0 test only checked that states started. |
| QuietStage + arena reset in the test | Wandering monsters and slopes made the test flaky across stages |
| Solo only | Interview choice; networking (UNet) of custom bodies and the waveOut audio is untested |
| Ability redesign (v0.3.0) | The creator went through doc 04 slot by slot. Picks: Haze Fixation+ passive, Sleep Dagger stun 4 s / 10 s, Smoke Bomb 5 s +50 % / 12 s, Bullet Dance unchanged with new effects; Abrams Infernal Resilience (10 % over 10 s), Siphon Life as a Mired Urn (3 targets, 25 m, 8 s, 15 s), Shoulder Charge 3 s stunning everything hit / 8 s, Seismic Impact with a hover and a reticle you aim (he can be hit); Seven Capacitor passive, Lightning Ball builds Static, Static Charge with a thunderbolt, Thunderhead zone at his feet with a Warbanner; Infernus Afterburn with visible embers, Napalm (lobbed, 2 charges), Flame Dash held up to 3 s with a 5 s trail that knocks enemies aside, Combustion 3 s fuse / 2 s stun / heal per kill. Details in doc 04 §6 |
| Reuse RoR2 items for skills | The creator's idea: Siphon Life *is* RoR2's Mired Urn attachment and Thunderhead's attack speed *is* RoR2's Warbanner ward, so they look and work like the items players know, with no new buff code |
| Ults as Q equipment (v0.4.0) | The creator: the ults should be pickups on Q, "auto equipped at the start" but droppable, other heroes' ults findable in the game, the item looking like the hero (picked: a whole mini hero), 120 s recharge, Preon Accumulator power. Answers: drop pool = everyone, same rules as Preon (chests, Enigma, Bottled Chaos); power = Preon scaled to 120 s (≈6800 % per enemy); charge meter removed; Seismic Impact slams on Left Click. New R per hero after deadlock.wiki research (all four Deadlock abilities were already used): Haze **Phantom Strike** (item) with a 3-cut flurry, Abrams **Haymaker** (heavy melee, tap), Seven **Power Surge** (his real ability) as RoR2 Tesla arcs per burst, Infernus **Torment Pulse** (item). Plus: rebuild all Infernus fire in Unity with RoR2's fire materials. Details in doc 04 §7 |
| Balance on main moves, ults unbalanced (v0.4.1) | The creator: the Q ults can be carried by any survivor, so they are left out of balance and "can be overpowered as we like"; compare the main moves only. Balanced = Commando (no equipment) **±20 %**. Picks: Haze all four buffs (SMGs 55 %, Fixation burst every 10 stacks for 300 % splashing 6 m, Sleep Dagger 6 s / 500 % / pierces) plus Bullet Dance hitting every enemy; Abrams Seismic 18 m, Haymaker shockwave cone, Shoulder Charge 500 %; Seven **cooldowns only, no damage cuts**, tuned with the bench until inside the band (Lightning Ball 16 s, Static Charge 16 s, Power Surge 30 s); Infernus Torment Pulse 50 % per pulse and Flame Gun 50 %. Details in doc 05 |
| Each hero vs its own RoR2 match; kit pages redesigned (2026-10-16) | The creator: pages should open with a profile picture and stats, then each ability with its DPS maths beside it, Deadlock-wiki styling, the ult with a picture, then updates and balance; "give these guys corresponding actual risk of rain character and have the analysis be separate section for each character". Answers: studio renders made in game (Deadlock art may not be copied onto pages), matches = stat base (Haze Commando, Abrams Loader, Seven Artificer, Infernus Engineer), each hero compared with its own match (±20 %), 4 hero pages + an overview. `tools/kit_pages.py` now reads the sheets and `build/bench_results.txt` directly; `tools/portraits.py` cuts out the renders. The creator also asked for a full handoff and record system: `docs/00-HANDOFF.md` and `docs/07-SESSION-LOG.md` |
| Balance band ±500 %/s vs the match, means over runs (v0.4.2) | The creator: "ok let increase the threshold to 500 dps i think that is good enough, so no change here" (Haze stays); Abrams "damage only"; Seven "cooldowns + gun buff", aimed at "Artificer's average" once single runs proved too noisy; as many automated bench runs as needed. Result: all four inside on the means (doc 05 §3.0). Seven's Lightning Ball must stay ≥ 8 s (Capacitor chain reaction below that) |
| Effects built in Unity, not ripped | The creator: "make it similar to the Deadlock game, not a rip from the Deadlock engine files". `Runtime/Vfx.cs` builds them at runtime from Unity components and RoR2 materials (doc 02 §8.1) |

---

## 10. Known open items and ideas

- **Haze leans forward a lot** when aiming steeply down: the whole aim is taken by the spine. Possible fix: give part of the pitch to the clavicles or arms.
- Seven's Thunderhead cast uses his Storm Cloud `ult_start` clip (0.6 s); its loop pose (tilted) is no longer used.
- **No normal maps** (VRF does not export them for Deadlock's hero shader). Only the albedo is used.
- **Reload clips still animate the arms** (gun parts stay frozen), by design.
- **Listing screenshots** are from 0.1.0 and should be replaced with 0.4.0 shots (4 skills, the Q ults, the hero pickups, the new effects).
- **0.3.0 and 0.4.0 add Deadlock content** (icons, sounds, clips; doc 01 §5). A player's older cache has `ready.json`, so Melty skips setup and the new icons, sounds and R clips stay missing until the cache is rebuilt. The release must force a re-extract (doc 01 §5).
- **New effects are only checked by eye** (devshots); the test measures what skills do, not how they look.
- **Multiplayer** would need: network-safe states (the equipment ults already start on the owning client and send their row id), audio per client, `recipe.multiplayer`. Phantom Strike picks its target on each side separately.
- **Equipment ults on non-heroes** (Commando, Equipment Drones) use plain muzzles and no Deadlock animation; they work but look plainer than on the hero.
- **More heroes:** follow §8. Abrams-style off-hand weapon pivots need `gunBone`.
- **Seven's gun froze after a magazine (fixed in v0.4.1):** 29 rounds in bursts of 3 drove the burst counter below zero; any gun whose magazine is not a multiple of its burst would have done the same.
- **Seven's gun is stronger than the sheet says:** about 350 %/s sustained (before Capacitor), against a `targetDps` of 250. Preflight's burst formula doesn't match `GunState`'s re-arm timing (doc 02 §7.6).
- **Negative move-speed adds divide** (doc 02 §7.4). The 0.3.0 slows (Napalm Slow, Fixation Slow) are written so they really are 28 % and 12 %.
- **Pelvis bob is lost** for Haze, Seven and Infernus: their `pelvis` is a separate root in the DLM, and the animator skips position curves on every root bone (doc 01 §7). A fix would skip only `root_motion`, plus the pelvis' *horizontal* drift.
- **The local cache is from the 0.1.0 manifest**, a superset of 0.2.0's. A clean re-extract with `--manifest extractor/manifest.json --force` has not been run for 0.2.0. Melty's 0.2.0 test skipped setup because `ready.json` existed.

---

## 11. Keeping these docs true (standing rule)

Every change to this project (code, sheets, tools, recipe, versions, decisions) updates the affected document(s)
**in the same commit**:
- Deadlock-side changes go in doc 01.
- RoR2-side changes go in doc 02.
- Process, release and decision changes go here (doc 03).
- Ability changes also go in doc 06 and add a "retest needed" row to doc 05 §0; a benchmark run refreshes both.
- Every creator request that changes anything gets an entry in doc 07 (session log), and doc 00 (handoff) §1/§9 are
  refreshed.

`CLAUDE.md` repeats this rule for agents.
