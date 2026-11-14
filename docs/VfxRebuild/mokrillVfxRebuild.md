# Mo & Krill: VFX rebuild

Code: `plugin/Runtime/VfxMokrill.cs` (new; their effects used to sit in `Vfx.cs`). Recipes read:
`charactersFinal/mokrillFiles/deadlock/particles/abilities/digger/*` and `weapon_fx/digger/*` (Deadlock's code name for
Mo & Krill is "digger"). Method and tools: `README.md` in this folder.

## The headline: the colours were right, the drawing was heavy and plain
Mo & Krill's colours were already taken from Deadlock's particles (tan sand, orange heat, dark cracks), so the palette stays.
But every effect was a pile of RoR2 smoke, dust and sparks: Sand Blast's wave alone emitted about 1000 particles a second,
and a Sanded enemy 125 a second each. Deadlock draws the same moves with cartoon dust clouds, debris flecks, rings and marks
projected on the ground. They are now drawn that way, at a fraction of the particle count.

| Use | Deadlock colour |
|---|---|
| Sand | `#ECDABA`, `#C7B18D`, `#AC9371` |
| Sand Blast: throw ring, light, cone lines | `#E28420` > `#451B0C`, `#FF911C`, `#DCAA72` / `#FFE1C5` |
| Burrow: ring, crack trail | `#FC7208`, `#332E29` |
| Burrow burst: cracks, hot ground, dark ground, debris | `#FE843E` / `#9F4217`, `#ED6B14`, `#19060C`, `#8D583C` / `#6A6A6A` |
| Krill: arcs, ring | `#FED776` / `#FF9B22`, `#F97D3A` |
| Gun: muzzle, pellets | `#FF8B24` / `#FECF78`, `#FFD455` > `#F56A00` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Yellow Canary** (M1) | `default_muzzle_flash`, `digger_tracer_impact*` | an orange glow, grey smoke, sparks, a dust blob | A ragged orange flash and a hot core (two quads), smoke on some shots; a star, six yellow-orange bits and chips of dirt on a hit. Throttled. |
| **Sand Blast** (M2) | `digger_throw_sand_cast` (ring, dust, light), `_linear` (cartoon clouds, edges, ground), `_debuff` (clouds, sand, flash) | grains and dust drawn in; six systems of smoke, dust and sparks for the wave (about 1000 particles a second); big smoke bursts on each hit | Wind-up: grains drawn in, an orange spiral winding up, a glow. Throw: a spiked ring, a flash, a ring on the ground, 12 clouds and 36 grains thrown along the cone. Wave: a rolling wall of cartoon sand clouds, grit ahead of it, chips of dirt, dust left hanging, a bright ribbon at each end (about 180 particles a second). Hit: a burst ring, a glow, five clouds, 14 grains. Sanded enemies: clouds and grit whirling round the head, sand pouring off (39 a second each, was 125). |
| **Burrow** (Shift) | `digger_burrow_start`, `_channel` (dust, fleks, ring, crack trail), `_explode` (cracks, debris, hot and dark ground, smoke) | smoke, sparks and dust; line rings | Digging in: the ground darkens, an orange burst ring, 24 chunks of dirt and stone, cartoon dust. Under: a dark ring drawing shut. Travelling: dust boiling up, chips bobbing, a turning orange ring, a dark crack left behind. Bursting out: two orange rings, dark ground with a hot spiral cooling for 2 s, a star, 30 chunks, sparks, dust rolling away and a column of it thrown up. |
| **Tag Team** (R) | ours; styled on `digger_ult_tgt_hit` (Krill's electric arcs) | a plain trail, line bolts | Krill flies with an orange ribbon and sparks; where he lands: an orange ring at the splash's reach, a star, Deadlock's lightning arcs, sparks. Mo's Frenzy: red embers (flecks) rising, a turning red ring and a glow at his feet; each punch a red star, a spiked ring, sparks, dust. |
| **Dust Devil** (Q) | ours; styled on `digger_burrow_spin` | four arms of smoke with trails, smoke at the foot, 120 streaks a second | The old build's four bright trails whirling round Mo, two spirals turning on the ground and at his chest, a little cartoon dust carried up the column and rolling at its foot, 50 streaks a second pulled in from the edge. |

## Sheet and code changes
- No sheet changes: the same `mokrill_*` effect rows and moments. Shared texture rows only.
- `Vfx.cs`: the six `Mokrill*` functions and `Spray` moved out and rewritten in `VfxMokrill.cs`; `SandedFx` draws with the kit.

## Results (2026-11-13)
Before/after page (pick Mo & Krill in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#mokrill

Three looks in game, and what each changed:
1. First look: Dust Devil's stacked fan sprites drew as flat cream discs and wide white bands; Burrow threw a wall of bright
   cloud at the camera. Fans removed, clouds fewer, smaller and thinner.
2. Second look: Burrow right (dark cracked ground, modest dust). Dust Devil's kit ribbons too faint beside the old bright swirl;
   Sand Blast 3 ms a frame dearer than the old build (17.6 against 20.6 ms) from its wall of big see-through clouds. Ribbons
   widened and brightened, the wall thinned (15 clouds a second, smaller).
3. Third look: Sand Blast level with the old build (17.0 ms). The ribbons still too faint, so the swirl's arms use the old
   build's own trail again (not seen in game after this last change; it is the old code). The two ground spirals, the lighter
   dust and the fewer pull streaks stay.

Single player, 6-second runs (first use inside the measurement), old build against the new after look 2:

| Move | Average, old / new | First use, old / new | Peak particles, old / new |
|---|---|---|---|
| Standing still | 15.3 / 15.5 ms | 35 / 34 ms | 340 / 412 |
| Yellow Canary | 16.9 / 17.1 | 81 / 90 | 380 / 448 |
| Sand Blast | 17.6 / 17.0 | 29 / 36 | 1148 / 584 |
| Burrow | 20.3 / 20.9 | 30 / 38 | 486 / 496 |
| Tag Team | 20.2 / 20.1 | 31 / 30 | 427 / 452 |
| Dust Devil | 21.8 / 22.6 | 30 / 30 | 502 / 482 |

Level with the old build throughout; the gun's first shot is the same hitch every hero's gun has, in both builds.

Not done: the normal in-game test (`bash tools/run_test.sh mokrill`) on this branch; the creator's sign-off on the look.
