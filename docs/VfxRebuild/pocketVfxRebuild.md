# Pocket: VFX rebuild

Code: `plugin/Runtime/VfxPocket.cs`. Recipes read: `charactersFinal/pocketFiles/deadlock/particles/abilities/pocket/*`
(161 files) and `.../synth/*` (143 files), plus his `weapon_fx`. Method and tools: `README.md` in this folder.

## The headline: Pocket is teal now, and ours was built from his old recipes
Deadlock ships two sets of recipes for Pocket. The older set is named `synth_*` and is **violet with gold**; our Pocket was
built to match it. The newer set is named `pocket_*`, covers every move he has today (Barrage, Flying Cloak, Enchanter's
Satchel, Affliction) and is **teal** (`#35978E`, `#4CDECD`, `#8AFFEB`). The model we use is the newer one
(`models/heroes_wip/pocket`, not `heroes_staging/synth`), so the teal set is the one that goes with it.

**This is a judgement call for the creator.** His spirit magic is recoloured from violet to teal. It is four colour constants
at the top of `VfxPocket.cs` (`pocketSpirit`, `pocketPale`, `pocketDeep`, `pocketSmoke`); putting the old violet values back
restores the old look. Frog Form's colours (`pocketTeal`, treasure green, gold) were already teal and are untouched. His
survivor colour in the menu (`heroes.json` `primaryColor`) is left violet.

| Use | Deadlock colour (pocket_*) | Was (synth_*) |
|---|---|---|
| Barrage, cloak, satchel magic | `#4CDECD`, `#35978E`, pale `#8AFFEB` / `#5EF9E9`, deep `#06534B` | violet `#9C25EC`, gold `#FBC479` |
| Affliction (not in our kit) | green `#64B141`, `#83B942` | gold |
| Muzzle | `#CB1C05`, `#E46C11`, `#FECF78` | the same family |
| Pellets | `#FFB23E` | |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **The Black Sheep** (M1) | `pocket_muzzle_flash`: a red-orange flame, smoke; `pocket_tracer` gold | a lilac glow, violet smoke and sparks, a light per shot; lilac tracer | A ragged red-orange flash and a pale core (two quads), five sparks, grey smoke on some shots, no light; a pale star and gold sparks on a hit; tracer tinted gold (`tracer_pocket` `#FFB23E`). Throttled. |
| **Barrage** (M2) | `pocket_barrage*`: teal channel glow, teal missiles and blasts | violet | The same shapes in teal; its rings, glows and smoke bursts draw with Deadlock's ring, glow and cartoon cloud. |
| **Flying Cloak** (Shift) | `pocket_flying_cloak`: teal over black | a dark-violet cloak with teal glow | Cloak edges and trail in teal. |
| **Enchanter's Satchel** (R) | `pocket_satchel*`: teal ring and beam, grey dust | violet tendrils and smoke | The same in teal. |
| **Frog Form** (Q) | ours | teal, treasure green, gold | Unchanged apart from the shared ring, glow and smoke drawing. |

## Sheet and code changes
- `design/effects.json`: `tracer_pocket` tint `#FFB23E`.
- `VfxPocket.cs`: colours renamed and changed (`pocketViolet` > `pocketSpirit`, `pocketLilac` > `pocketPale`); new muzzle and hit.

## Splats on the world (2026-11-14)
The creator: "i think pocket should have more splat like effect on the world like he does in deadlock, none functional just
visual". Deadlock leaves marks on the ground for him: a scorch under each Barrage blast (`pocket_barrage_explosion_proj`) and
slime with splatter under Affliction (`pocket_affliction_aoe_ground_proj_detail`, `_splatter`). New `PocketSplat` in
`VfxPocket.cs`: a dark wet stain with a slick of slime on it, smaller blots thrown round it, a quick splash ring and droplets
flung up; it stays 2.5 to 5 seconds and fades. Looks only, no damage or slow. Teal slime on each Barrage blast, where the
Flying Cloak ends and when the Satchel bursts open; green slime under the frog when it appears, when the case lands and on
every slam. At most one splat per 0.1 s so a volley does not stack them.

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#pocket

Seen in game on the current build: his magic is teal instead of violet, the gun has Deadlock's red-orange flash, and the new splats show on the ground under the frog and the satchel (dark stain, slime, blots). They read dark in the screenshots, so the slime was brightened afterwards (not seen again).

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 17.0 / 16.8 ms | 43 / 39 ms |
| The Black Sheep | 18.5 / 18.3 ms | 104 / 92 ms |
| Barrage | 18.2 / 18.6 ms | 35 / 35 ms |
| Flying Cloak | 18.7 / 22.2 ms | 30 / 35 ms |
| Enchanter's Satchel | 20.9 / 21.3 ms | 31 / 30 ms |
| Frog Form | 19.3 / 19.6 ms | 35 / 32 ms |

Measured with the splats in. Level with the old build except Flying Cloak, 3.5 ms a frame dearer in this run (not repeated, not yet explained). The gun's first shot is the same hitch every hero's gun has, in both builds.

Not done: the normal in-game test (`bash tools/run_test.sh pocket`) on this branch; the creator's sign-off on the look.
