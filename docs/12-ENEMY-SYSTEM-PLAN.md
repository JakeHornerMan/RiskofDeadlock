# 12. Making enemies as simple as heroes (research and plan, 2026-11-13)

The creator (2026-11-13): "do deep research on how we build enemy it will be very similar to how we build the player characters.
enemy will always have a model and animation and attack animations... we also want them to drop souls orbs on death, and also be
added to the risk of deadlock section on the character selection screen here we will have a boolean for each enemy type and also a
level select for this enemy so we can define where they show up ... i would love if creating enemys in the future was a lot simpler".

This is a plan, nothing here is built yet. Doc 11 describes the Troopers as they are today.

## 1. Where we stand

### 1.1 How a hero is made (the model to copy)
A hero is **data first**: a row in `heroes.json`, `animations.json`, `skills.json`, `sounds.json`, `effects.json`; `tools/gen.py`
turns them into code and the extractor manifest; preflight checks every name; one shared builder (`Survivors`, `ModelBuilder`,
`DeadlockAnimator`) makes every hero the same way; each skill row names a `kind` and only a *new mechanic* needs new C#
(`[Kind]` state). `docs/08-NEW-HERO-PLAYBOOK.md` walks it in eight steps, with an intake, tests from each row's `expect` list
and a definition of done.

### 1.2 How the Troopers were made
The same idea, partly: `enemies.json` / `enemySkills.json` / `enemyDrops.json` / `enemyTestScene.json`, gen + preflight, a shared
model builder (`EnemyModel`), animator (`EnemyAnimator`), attack states (`EnemyAttack` → melee / shot), death (`EnemyDeath` +
`EnemyFade`), soul orb (`SoulOrb`), spawning through R2API (`TrooperDirector`), the lobby box (`TrooperRule`, `TrooperStages`,
`TrooperStagesUi`) and a test plan (`EnemyTest`). About 3,400 lines in `plugin/Enemies/`.

### 1.3 What makes a second enemy type hard today (the Trooper-only shortcuts)
| Where | Trooper-only today | What it should be |
|---|---|---|
| `EnemyModel.BuildLook` | child-locator points hard-coded to trooper bones (`spine_1`, `flame_0`, `wick_end`, `banner_end`) | a `points` column per enemy: `Muzzle=laser;Chest=spine_1;Head=head;Light=flame_0` |
| `EnemyLook.Pick` | a candle light, green when the row is `trooper_medic` | `light` column (bone, colour, range) or none |
| `EnemyLook` health heads | the melting-candle mesh swap | already a column (`healthMeshes`), keep, `none` for others |
| `Enemies.IsMedic`, `MedicSpawnGate` | the Medic by id | `spawnNearAllies` (metres, 0 = off) and "don't heal my own kind" from the affix |
| `Enemies.OnDeath` | the heal pack for the Medic by id | `drops` column: `soul_orb`, `heal_pack`, … (enemyDrops rows) |
| `TrooperRule` | one "Troopers" tick | one tick **per enemy group** (§2.4) |
| `TrooperStages` | one stage list; default from `trooper_ranged`'s Lemurian | one list **per group**, default from that group's own vanilla card |
| `TrooperDirector` | adds every row to Basic Monsters; one "Troopers" family | per row `category` (basic / mini-boss / champion) and per group family |
| `EnemyStates` | `TrooperSwing`, `TrooperCloseHit`, `TrooperShot`, `MedicShot` (one class per row kind) | generic kinds (`melee`, `shot`, …) with the row's numbers; a class only for a new mechanic |
| `EnemyAnimator` | run clips paced for trooper speed (5 m/s); `runN/S/E/W` + `idle` + `jumpAir` | `runPace` column; locomotion set `four_way` / `single` (walk only) / `fly` |
| `Enemies.Drivers` | AI from `fleeRange` / `keepMin` / `keepMax` and slot names | an `ai` column naming a behaviour (`chaser`, `shooter`, `support`, `boss`) plus the same numbers |
| `EnemyTest` | Trooper names in the test (melee/ranged/medic plans) | tests built from each row (§2.7) |
| `EnemyTestScene` | "one of each Trooper" | one of each enemy in the chosen groups |

None of these are hard to change; they are just named after Troopers because the Troopers came first.

### 1.4 What the creator's folders hold (`charactersFinal/npc`, 2026-11-05 copy)
| Group | Models | Clips | Sounds | Notes |
|---|---|---|---|---|
| troopers | 59 | 875 | 101 | done (melee, ranged, medic); super troopers and gibs unused |
| guardian | 1 | 78 | 44 | tier 1 boss: `melee_close_range_01/02`, `melee_long_range_attack`, `primary_shoot`, idles |
| walker | 3 | 58 | 52 | tier 2 boss: `ability_electric_beam_*`, `death`, `walk_*`, shielded / unshielded idles |
| midboss | 2 | 66 | 5 | the Mid-Boss |
| patron | 17 | 82 | 27 | the Patron, cores, effigies |
| shrines | 5 | 9 | 24 | shrines and base watchers |
| baseSentry | 1 | 0 | 20 | no clips of its own |
| neutrals/* | 14 camps | 459 (+180 shared) | 274 (+260 shared) | e.g. library watcher (`run_n…`, `melee_01`, `shoot_idle`), underhand (hover attacks), lanterns, mimics, mushrooms, plants |
| haunts, unusedBosses, pigeons, dockCreature, streetTrash, parkWatcher | — | 0 | 0 | models only: no clips of their own (some take `_shared`) |

So the system must cope with: two looks or one; four-way runs or one walk; ground or hovering; one attack or several; a death clip or
none; bosses. Enemies with no clips at all can only stand, slide or fly (§2.3).

## 2. The proposal: an enemy is a sheet row, like a hero

### 2.1 One command to start an enemy (`tools/add_enemy.py <npc group>`)
Reads the group's folder (no game launch, creator's files only) and prints a ready-to-paste row:
- its models (vmdl paths; two looks when both team models are there), main meshes (gib copies left out), height;
- its clips sorted by role from their names: idle, run/walk (four-way or one), air, flinch (`hit_react`), spawn, **death**, and
  every **attack** clip (`melee*`, `attack*`, `shoot*`, `fire*`, `ability_*`), with lengths;
- its sound events (from its `vsndevts`) sorted the same way (attack, impact, death, spawn), and which of their files are there;
- bones worth pointing at (`head`, `spine_*`, hand/weapon ends) for the muzzle and chest;
- a suggested vanilla monster to borrow the hull and cost from (by height: Beetle/Lemurian small, Golem/Elder Lemurian large,
  Wisp for hovering).
Then `--extract` runs `DeadlockExtract --loose` with the right manifest. This replaces the hand-reading of clip lists, which is
where most of the Trooper time went (choosing clips, finding the gib copy, the vertex-coloured materials).

### 2.2 The sheets (columns added to what exists; preflight checks them all)
- `enemies.json`, one row per enemy type: `group` (the lobby row it belongs to, e.g. `troopers`, `guardian`), `looks` (models),
  `archetype` (`walker`, `hover`, `boss`: which vanilla body to borrow and which AI), `points` (named bones), `light`,
  `locomotion` (`four_way` / `single` / `fly`) + `runPace`, `deathClip` (or `none`: topple, as now), `spawnClip`, stats,
  `category` (`basic` / `miniboss` / `champion`), `spawnNearAllies`, `drops` (enemyDrops ids), `costFrom`, `lookTints`,
  `vertexColorMaterials`, `sizeMult`, `expect` (what its test checks).
- `enemySkills.json`, one row per attack: `kind` from a **library** (below), `clip`, `windup`, numbers, sounds, projectile look.
- `enemyGroups.json` (new), one row per lobby row: `id`, `name` ("Troopers", "Guardian", …), `defaultOn`, `defaultStagesFrom`
  (the vanilla card whose stages are the default, e.g. `cscLemurian`), `family` event text.
- `enemyDrops.json` as today (soul orb), with `gold` per row so a boss's orb can pay more.

### 2.3 A library of attack kinds (C# written once, numbers in the sheet)
| kind | does | Troopers today |
|---|---|---|
| `melee` | windup, then a hitbox `reach` m ahead | Melee swing, Ranged close hit |
| `shot` | windup, then a projectile: `arc` or straight, speed, look | Ranged shot, Medic shot |
| `burst` | several `shot`s in a row | — (Guardian `primary_shoot`?) |
| `slam` | windup, then a ring blast `radius` m around it | — (Guardian melee long range) |
| `beam` | a held line that damages along it | — (Walker electric beam) |
| `summon` | spawns other enemies rows | — |
| `affix` | carries a RoR2 elite buff (Mending today) | Medic |
| `pulse` | heals or buffs allies in `radius` every `interval` | Medic (old pulse) |
Each kind plays the row's `clip` (or stands idle with `none`), sounds and muzzle point, and is checked by preflight. A new kind is
C# only when a mechanic is truly new; everything else is a row.

### 2.4 The lobby (the Risk of Deadlock box)
One line per enemy group, each with its **tick** (on/off) and its own **Stages ▾** list beside it, built from `enemyGroups.json`;
the orange **Play Test scene** button stays under the box and spawns one of each enemy of the ticked groups. Each group's choice is
its own config entry (`Enemies / <Group>`, `Enemies / <Group>Stages`), the host's counts, a list greys out while its tick is off.
With many groups the box grows; past about six groups the lines fold into a scrolling list (the same one the stage list uses).

### 2.5 Spawning (one `EnemyDirector`, not Trooper-named)
Per row: its category in each ticked stage's pool (`basic` → Basic Monsters, `miniboss` → Minibosses, `champion` → Champions, which
RoR2 uses for the teleporter boss), weight from its `costFrom` card, cost ×`costMult`, `spawnNearAllies` instead of the Medic gate,
one family event per group. Bosses (Guardian, Walker) come as teleporter champions with their own health bar.

### 2.6 Death and drops (shared already, made general)
Every enemy: smoke in its look colour, the corpse topples or plays `deathClip`, sinks and fades; its `drops` (a soul orb by default,
the gold from the drop row).

### 2.7 Tests from the row (like heroes' `expect`)
For every enemy row the test plan runs, without new test code: built + a spawned copy has its look and clips; it reaches a walking
target (walker/hover) or holds its band (shooter); each attack fires only in range and lands; it dies, falls and sinks; its orb pays.
`ENEMIES=<group>` runs one group; the Play Test scene runs them all together.

### 2.8 The enemy playbook (`docs/13-NEW-ENEMY-PLAYBOOK.md`, to write with the first new enemy)
1. Intake (one round): which group, which enemies, attacks wanted, where they spawn, boss or not.
2. `python tools/add_enemy.py <group>` → paste the rows, adjust numbers.
3. `--extract`; `python tools/gen.py` → PREFLIGHT CLEAN.
4. Line-up screenshots against the creator's Deadlock pictures (materials, size).
4b. Effects by `docs/14-VFX-LOOK-GUIDE.md` (recipes from the group's `particles/` folder, `VfxEnemies.cs`, its checklist in §7);
   `add_enemy.py` should also print the group's recipe names and their colours (`tools/vpcf_summary.py --brief`).
5. `ENEMIES=<group>` until every check passes; Play Test scene for the creator.
6. Docs, CHANGELOG, `--r2modman-install`.

## 3. Order of work (each step keeps every Trooper test green)
1. **Generalise without changing behaviour**: the shortcuts in §1.3 become columns (points, light, spawnNearAllies, drops,
   locomotion, generic `melee`/`shot` kinds); rename `Trooper*` classes to `Enemy*`; the Troopers' rows carry their current values.
2. **Groups in the lobby**: `enemyGroups.json`, one tick + stage list per group (Troopers the only group), config per group.
3. **`tools/add_enemy.py`** (lookup, row skeleton, extract).
4. **First new enemy as the proof**: one with clips of its own and a plain attack, e.g. the Library Watcher (four-way run,
   `melee_01`, `shoot_idle`) or the Underhand (hover attacks). Write doc 13 from what it took.
5. **Bosses** (Guardian, then Walker): champion category, `slam` / `beam` kinds, the death clip.

## 4. Decisions for the creator
- Which enemy comes first after the Troopers (Library Watcher, Underhand, Guardian, …)?
- Bosses: should the Guardian / Walker be teleporter bosses (with a boss bar) or roaming minibosses?
- Lobby: one line per group is the plan; is a scrolling list fine once there are many groups?
- Soul orbs: the same 5 money for every enemy, or more for bigger ones (bosses)?
- Enemies with no clips of their own (haunts, pigeons, street trash, base sentry): leave out, or let them hover/slide?
