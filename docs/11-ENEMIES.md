# 11. Enemies (Deadlock NPCs as Risk of Rain 2 monsters)

Started 2026-11-11 with the creator's "Troopers — Enemy Design Spec": Deadlock's three lane Troopers join every stage's monsters.
**The creator's own DLL build only** (r2modman package `JakeHornerMan-Risk_of_Deadlock`; never on Melty, like Victor and Paige).
Everything here is compiled only with `OWN_FILES` (`-p:OwnFiles=true`); the Melty DLL has none of it and no R2API reference.

## 1. The system in one picture

```
charactersFinal/npc/<group>/deadlock/   (the creator's NPC files, read only)       design/enemies.json   design/enemySkills.json
            │  DeadlockExtract --loose ... --manifest extractor/manifest-enemies.json        │ tools/gen.py
            ▼                                                                               ▼
charactersFinal/npc/<group>/mod-cache/heroes/<model>/<model>.dlm  ──build_local_hero.sh all──►  heroes/ beside the DLL (ready.json "enemies")
                                                                                            │
plugin/Enemies/Enemies.cs: clone vanilla body + master ─► our model (EnemyModel), states (EnemyStates), AI drivers, spawn card
plugin/Enemies/TrooperDirector.cs: R2API.Director MonsterActions ─► every stage's pool + a family event
```

## 2. Adding an enemy type
1. Copy its NPC files into `charactersFinal/npc/<group>/deadlock/` (with `edit/<model>.glb` for the clips the model can't give).
2. A row in `design/enemies.json` (one per type; `models`/`vmdls` one per look; `meshes` = every look's main mesh, never the
   `*_fractured_model` gib copy; `healthMeshes` = Deadlock's health head meshes, or `none`) and its attacks in `enemySkills.json`
   (`slot` primary/secondary replaces that slot; `passive` = a component). `python tools/gen.py` must say `PREFLIGHT CLEAN`.
3. `build/extract/DeadlockExtract.exe --loose charactersFinal/npc/<group>/deadlock --out charactersFinal/npc/<group>/mod-cache --manifest extractor/manifest-enemies.json`
   Every model must report `N/N clips`; a clip the model lacks is in neither the model nor its `edit` glb: pick another.
4. A new attack kind is an `EnemyAttack` subclass with `[Kind("...")]` in `plugin/Enemies/` (preflight checks it exists).
5. `ENEMIES=<ids> STAGE=golemplains bash tools/run_test.sh haze` (needs the DLL build installed and R2API in the Steam copy, §5).

## 3. How a Trooper is built (Enemies.cs)
- **Body**: the `cloneBody` prefab (Beetle / Lemurian) instantiated under the inactive prefab holder, a stable network asset id,
  the model swapped as heroes' are (`ModelLocator._modelTransform`, `CharacterModel.body`, hurtboxes). Stats from the row.
  The vanilla capsule is scaled to `sourceHeight × GlobalScale` (the heroes' shared factor: Deadlock size, creator's choice).
  Spawn state `EnemySpawn` (Lemurian's spawn effect and sound, the deploy sound, the landing clip), death `EnemyDeath` (the shown
  look's ragdoll, Deadlock's death sound), stagger `EnemyHurt` at `staggerPct` (SetStateOnHurt.hitThreshold). The vanilla death and
  bark sounds and the logbook unlock are cleared.
- **Model** (`EnemyModel`): every look (amber, sapphire) under one root, all their renderers listed in `CharacterModel` (it turns
  every listed renderer on each frame), the unshown look and health heads switched off through their GameObjects. `EnemyLook`
  picks the look from the body's network id (every player sees the same) and swaps the health heads by thirds of health
  (Deadlock's melting candle); a point light at the wick flickers and goes out on death (green on the Medic).
  `EnemyAnimator` plays the clips (legacy Animation, `DeadlockAnimator.BuildClips`): locomotion from velocity, attacks as gestures.
- **Attacks** (`EnemyStates`): run where the body has authority (the server) and on every client through the network state
  machine, so sounds and clips play in the state and the hit only on authority. Melee: `OverlapAttack` on the model's
  `TrooperMelee` hitbox after the windup, rooted meanwhile. Shot: the muzzle glows for the windup, then the Lemurian's fireball
  (recoloured ghost, `projectileSpeed`). The clip is sped so its swing lands at the end of the windup.
- **AI** (`Drivers`): the vanilla drivers are replaced by our list (first match wins):
  - Melee: Hit (≤ reach, skill ready) · Close (stand beside) · Chase.
  - Ranged: Hit (≤ 3 m) · CloseChase (≤ 7 m) · Shoot (7–30 m, line of sight, **stands still**) · BackOff (7–15 m) · Hold (15–30 m) · Chase.
    The spec says "strafe while waiting" but its test fails a Trooper that keeps moving once the target is back in range, so it
    stands (2026-11-11).
  - Medic: Flee (< 10 m) · FollowAllies / StayBehindAllies (custom target, on only while there are allies: `MedicFollow`) · Shoot
    (10–30 m) · BackOff · Hold · Chase.
- **Medic** (`Medic.cs`): `MedicPulse` (server, every `interval`): every other living, hurt monster of its team within `radius`,
  not another Medic, heals `healPct` (`healPctBoss` on champions). Visuals through two of our own `EffectDef`s (sent to every
  player once): a green ground ring with RoR2's heal-nova chime (`ror2Sound`), a green beam to each healed monster and crosses
  rising off it; RoR2's own heal flash shows on their bodies. `MedicFollow` keeps a marker 20 m beyond the nearest ally group,
  away from its target. Its death drops Monster Tooth's `HealPack` (8 + 2 %, 10 s) for the players.
- **Spawn cards**: human hull, ground graph, never on `NoCharacterSpawn` nodes, cost = the `costFrom` vanilla card's × `costMult`
  (read at load: the spec's "read the Lemurian's cost from the game data").

## 4. Spawning (TrooperDirector.cs, R2API.Director 3.1.3)
- `DirectorAPI.MonsterActions`: on every stage's `DccsPool` (Simulacrum's waves, Commencement and the Void stages use these
  pools too) each Trooper is added to Basic Monsters (`Helpers.AddNewMonster`), `minimumStageCompletions` 0, weight = the copied
  vanilla card's weight on that stage × `weightMult` (at least 1: weights are whole numbers, so the Medic is at most as common as
  the Lemurian; its spawn gate makes it rarer in practice).
- Family event: one `FamilyDirectorCardCategorySelection` per `family` (Troopers), from stage 2 like vanilla families, added to
  each pool's Family category at the average family weight, announced "You hear the march of candles...".
- Medic gate (`MedicSpawnGate`, Harmony prefix on `CombatDirector.AttemptSpawnOnTarget`): no other living monster within 30 m of
  the spawn point → the card is dropped and the director picks again.
- Config `Enemies / Troopers` (default on): off = the enemies are not built and never spawn.
- R2API is a **soft** dependency of the plugin: without it the heroes load and the log says the Troopers can't spawn.

## 5. R2API: how it is used (R2Wiki Cookbook)
- Never bundled. `DllBuilds/package/manifest.json` lists `RiskofThunder-R2API_Core-5.3.0` and `RiskofThunder-R2API_Director-3.1.3`
  (the Cookbook's `Owner-Name-Version`); r2modman needs HookGenPatcher and R2API_Addressables too, which come as their dependencies.
- Compile: `tools/fetch_r2api.py` downloads the three R2API assemblies from NuGet into the gitignored `build/ref/`;
  the csproj references them only when `OwnFiles=true`, `Private=false`.
- Install: `tools/r2modman_install.py` calls `tools/thunderstore_deps.py`, which installs any missing Thunderstore dependency
  (recursively) into the profile, laid out as r2modman does (`plugins/<Owner-Name>/`, `patchers/<Owner-Name>/`, a mods.yml entry).
  The Steam copy (test runs): `python tools/thunderstore_deps.py <manifest> --steam`.

## 6. Tests (EnemyTest.cs; `ENEMIES=melee,ranged,medic STAGE=golemplains bash tools/run_test.sh haze`)
The creator's test plan, with the player (can't be hurt) as the target, moved by the test; the Troopers run their own AI.
States note swings, hits and shots with their distance to the target (`MeltyTest.EnemyNote`).
- Content: `enemies-built`, `enemy-looks` (2 each), `enemy-spawn-pool` (R2API reached this stage, all three cards in its monster
  list, costs logged), `enemy-family`, `medic-spawn-gate`.
- Melee: `trooper-melee-chase` (gets within 3.5 m of a target walking back and forth), `trooper-melee-swing` (swings only in
  reach), `trooper-melee-hold` (target stops: ≥ 3 hits in 8 s, never more than 4 m away).
- Ranged: `trooper-ranged-approach` (from 50 m, first shot at 14–31 m, no closer afterwards), `trooper-ranged-no-oor`,
  `trooper-ranged-follow` (target 40 m past it: it moves), `trooper-ranged-resume` (target back at 22 m: shoots, moves ≤ 2 m).
- Medic: `trooper-medic-heal` (two Beetles, a Lemurian and a Melee Trooper, AI off, hurt every 0.5 s: every one healed, the Medic not
  by its own pulse), `trooper-medic-fx` (a pulse a second, rings and beams shown), `trooper-medic-alone` (group gone: shoots from
  9–32 m), `trooper-medic-orb`.
