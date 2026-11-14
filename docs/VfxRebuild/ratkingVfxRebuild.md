# Rat King: VFX rebuild

Code: `plugin/Runtime/VfxRatKing.cs`. Recipes read: `charactersFinal/ratkingFiles/deadlock/particles/abilities/ratking/*`
and his `weapon_fx`. Method and tools: `README.md` in this folder.

## The headline: from yellow gold and brass to Deadlock's amber and cream
Our Rat King was drawn in a yellow gold and a pale brass over RoR2 smoke. **Deadlock's Rat King is warm amber**
(`#F4A142`, `#D5852A`) with sparks that cool from cream to red, cream rays of light off his banner, grey scrap metal and
brown sewer dust. His effects were rebuilt in those colours with Deadlock's rings, cartoon dust and flecks.

| Use | Deadlock colour |
|---|---|
| Amber (rings, glows, banner) | `#F4A142`, `#D5852A`, `#FFAB52` |
| Sparks | `#F9BF89` > `#F89760` > `#C7412A` |
| Banner rays and ring | `#FDD8A0`, `#FFDAB7` > `#765028` |
| Muzzle | `#FFB340`, `#F48014`; shock `#BC6B2D`; embers `#F6E2B8` |
| Scrap Grenade | ground light `#FFF0DC` / `#FF902F` / `#A75B3A`; scorch `#121212`; shards `#D5D5D5` / `#7D99FF`; trail `#FF993F` > `#3D2013` |
| Sewer dust | `#8D7F6E`, `#51493D` |
| Bite | `#F84909` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Sovereign's Decree** (M1) | `ratking_muzzle_flash`: flash, a rusty shock ring, embers, a casing | a brass glow, RoR2 smoke, sparks | A ragged amber flash and a rusty shock ring (two quads), four cream embers, grey smoke on some shots; a cream star and five sparks on a hit. Throttled. |
| **Scrap Grenade** (M2) | `ratking_scrap_grenade_clock_projectile` (orange ribbon, sparks), `_bounce`, `_explode` (ground light, dark ground, burst, shards, smoke) | sparks and a glow in flight; fire puffs, 45 to 72 sparks, 20 to 32 smoke puffs, a line ring, a primitive scorch disc, 8 to 14 scrap pieces each with its own material | In flight: an orange ribbon cooling to brown, sparks, a glow. Sitting: a spiral winding on the ground under it. Each blast: a flash, two rings of light over the ground, a scorch (one quad), 26 to 42 sparks cooling to red, 10 to 16 metal shards, cartoon sewer dust, 6 to 10 scrap pieces sharing three materials. Aim ring: one ring quad and 18 sparks a second (was 50). |
| **Royal Pestments** (Shift) | `ratking_ratswarm_aoe`, `_debuff_dust`, `ratking_nibble_debuff` | 30 smoke puffs a second round the edge; red sparks on a nip | An amber ring turning on the ground, 10 puffs of cartoon dust a second; each nip a small orange-red star and drops (throttled). |
| **Rule, Ratannia!** (R) | `ratking_anim_flag_plant` (cream rays, an ethereal ring), `ratking_ratshield_*` (rays, crown magic, ring) | gold line rings, 36 smoke puffs, 50 sparks, a gold flash | Raising it: a cream star, an amber burst ring, sparks. Leap: an amber ribbon. Slam: cream and amber rings racing to the stun's reach, six rays of light standing round the banner, cartoon dust, 30 sparks, the cracks, a flash. Planted banner: a soft amber glow at its top and slow rays rising off it. |
| **Rat Swarm** (Q) | `ratking_nibble_cast` (ground detail, arc), `ratking_ratswarm_debuff` | dark smoke and green sparks | An amber burst ring and a dark patch on the ground, cartoon dust, sparks; each bite a small star and drops (throttled). |

Not copied: Deadlock's rat and knight models in the swarm (`ratking_fx_rat`, `ratking_knights`); ours are the crowd's rats.

## Sheet and code changes
- No sheet changes. `Scorch` is now one ground quad (also used by Billy); scrap pieces share three materials.

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#ratking

Seen in game on the current build: amber rings and sparks, cartoon sewer dust and a scorch on the grenade, an amber ring round Royal Pestments, rings, dust and rays of light on the banner slam. The rays drew as thick grey-white pillars; they were thinned and warmed to amber afterwards (not seen again).

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 18.5 / 17.8 ms | 36 / 39 ms |
| Sovereign's Decree | 20.1 / 19.4 ms | 84 / 93 ms |
| Scrap Grenade | 19.4 / 19.3 ms | 39 / 39 ms |
| Royal Pestments | 19.0 / 20.7 ms | 42 / 35 ms |
| Rule, Ratannia! | 22.4 / 22.2 ms | 44 / 39 ms |
| Rat Swarm | 22.2 / 21.6 ms | 33 / 32 ms |

Level with the old build on every move (Royal Pestments 1.7 ms a frame dearer in this run, not repeated). The gun's first shot is the same hitch every hero's gun has, in both builds.

Not done: the normal in-game test (`bash tools/run_test.sh ratking`) on this branch; the creator's sign-off on the look.
