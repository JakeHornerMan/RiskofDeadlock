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
  the model swapped as heroes' are (`ModelLocator._modelTransform`, `CharacterModel.body`, hurtboxes). Stats from the row;
  `rootMotionInMainState` off (the Beetle walks by its Mecanim root motion, which our clips lack: it never moved).
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
  (recoloured ghost) lobbed in an **arc** onto the AI's target: `projectileSpeed` is its speed across the ground (25 m/s, about a
  second over 25 m), gravity bends it down onto the target's middle (creator, 2026-11-11: "the projectile should travel as an arc";
  aimed along the body's aim ray it hit the floor). Where it lands it plays the row's `hitSound` (`ShotImpactSound`, every machine).
  The clip is sped so its swing lands at the end of the windup.
- **Sounds** (the creator's `troopers.vsndevts`, 12 events): Melee Trooper swing `Trooper.Super.Melee.Swing` (its own
  `melee_trooper_swing_*`), Ranged close hit `Trooper.Melee.Swing` (`base_trooper_melee_swing_*`), shots `TrooperRifle.Fire` and
  `TrooperRifle.BulletImpact.Default`, `Trooper.Deploy.Impact`, `Trooper.Death`. `Trooper.Medic.Heal` and `Trooper.Melee.Damage.Hit`
  point at `sounds/player/` and `sounds/physics/` files the rip doesn't have (RoR2's heal chime stands in for the Medic); the
  grenade, lob and self-destruct events belong to moves the Troopers here don't have.
- **Size**: `sizeMult` (1.5, creator 2026-11-11: "make all trooper like 1.5 time thir height") on top of the heroes' factor:
  model, capsule, hurtbox and melee hitbox (`EnemyModel.Scale`).
- **Unity copies**: per-look data lives on an `EnemyLookPart` component on each look's root (Unity objects, strings, numbers and
  arrays of them only). A `[Serializable]` class from our assembly came out empty on every spawned body (no look, no attack clip,
  no candle, no ragdoll; only the prefab had them) — test on a spawned copy (`enemy-look-spawned`), never on the prefab.
- **AI** (`Drivers`): the vanilla drivers are replaced by our list (first match wins):
  - Melee: Hit (≤ reach, skill ready) · Close (≤ reach, stand beside) · ChaseDirect (≤ 8 m, straight at it, off the node graph:
    on the graph it overshot a standing target) · Chase.
  - Ranged: Hit (≤ 3 m) · CloseChase (≤ 7 m) · Shoot (7–30 m, line of sight, **stands still**) · BackOff (7–15 m) · Hold (15–30 m) · Chase.
    The spec says "strafe while waiting" but its test fails a Trooper that keeps moving once the target is back in range, so it
    stands (2026-11-11).
  - Medic: Flee (< 10 m) · FollowAllies / StayBehindAllies (custom target, on only while there are allies: `MedicFollow`) · Shoot
    (10–30 m) · BackOff · Hold · Chase.
- **Medic** (`Medic.cs`): `MedicPulse` (server, every `interval`): every other living, hurt monster of its team within `radius`,
  not another Medic, heals `healPct` (`healPctBoss` on champions). Visuals through two of our own `EffectDef`s (sent to every
  player once): a green ground ring with RoR2's heal-nova chime (`ror2Sound`), a green beam to each healed monster and crosses
  rising off it; RoR2's own heal flash shows on their bodies. `MedicFollow` keeps a marker 7 m beyond the middle of the nearest ally
  group, away from its target (behind them but with the whole group inside its 15 m pulse; 20 m left the far side unhealed). Its death drops Monster Tooth's `HealPack` (8 + 2 %, 10 s) for the players.
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
- **Lobby box** (`TrooperRule`, creator 2026-11-11: "underneath the Karma ... a orange box in the same style with a check box for
  Troopers"): a category of RoR2's own rule catalogue inserted just before Items (so right under Karma), orange, one rule
  `Deadlock.Troopers` with On (ticked box, default) / Off (empty box), drawn in code. The lobby votes it and the run's rule book
  carries it to every player. Off: `OnMonsters` adds nothing and the spawn gate turns down any Trooper card (pools kept from an
  earlier run included).
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
- Content: `enemies-built`, `enemy-looks` (2 each), `enemy-look-spawned` (a spawned Melee Trooper picked a look and has its
  clips), `troopers-lobby-rule` (the box right after Karma, on), `enemy-spawn-pool` (R2API reached this stage, all three cards in its monster
  list, costs logged), `enemy-family`, `medic-spawn-gate`.
- Melee: `trooper-melee-chase` (gets within 3.5 m of a target walking back and forth), `trooper-melee-swing` (swings only in
  reach), `trooper-melee-hold` (target stops: ≥ 3 hits in 8 s, never more than 4 m away).
- Ranged: `trooper-ranged-approach` (from 50 m, first shot at 14–31 m, no closer afterwards), `trooper-ranged-no-oor`,
  `trooper-ranged-follow` (target 40 m past it: it moves), `trooper-ranged-resume` (target back at 22 m: shoots, moves ≤ 2 m).
- Medic: `trooper-medic-heal` (two Beetles, a Lemurian and a Melee Trooper, AI off, hurt every 0.5 s: every one healed, the Medic not
  by its own pulse), `trooper-medic-fx` (a pulse a second, rings and beams shown), `trooper-medic-alone` (group gone: shoots from
  9–32 m), `trooper-medic-orb`.

### Test notes (2026-11-11)
- The walking player is moved by root motion with its controller on: a stage with no enabled `PlayerCharacterMasterController`
  ends the run ("Defeat").
- Test spawns get the player as their target (`currentEnemy`, as the combat director does); monster AI sees only 90° ahead.
- Spawn points are dropped onto the ground by a ray, not snapped to the nearest node (that stacked the Medic's group on one spot).
- Melee swings are judged where they start (`swingstart`): walking out of reach during the windup is the dodge.
- `PLAY=1 GOD=1 SPAWN=medic:5 bash tools/run_test.sh haze`: an ordinary run with those Troopers spawned in front of the player.
