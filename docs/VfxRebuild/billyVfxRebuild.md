# Billy: VFX rebuild

Code: `plugin/Runtime/VfxBilly.cs`. Recipes read: `charactersFinal/billyFiles/deadlock/particles/abilities/punkgoat/*` and
his `weapon_fx` (Deadlock's code name for Billy is "punkgoat"). Method and tools: `README.md` in this folder.

## The headline: the stone and pink stay, Deadlock's gold and violet come in
Billy's look (grey-brown stone and punk pink, the bigger pink swipe) was the creator's choice and stays. Deadlock's own Billy
adds two colours ours never used: **pale gold** on the bat's spikes, the horns and the slam's sigil, and **violet** round the
sigil. They are now layered in, and his rings, glows and dust are drawn with Deadlock's textures.

| Use | Deadlock colour |
|---|---|
| Bat spikes | `#FBEB79` > `#EB9D4B` |
| Horn glow and trails | `#EFCF6F`, `#FFFF81`, `#FEFC7F`, fading to magenta `#C338D6` |
| Slam sigil | gold `#F4C03F`, `#D9B44F`; violet `#A24BFF`, `#400086` |
| Launch shaft | `#FFFF81` > `#F26727`; flare `#FFF6B5` |
| Chains | rose `#DF7494` > `#3D0690` |
| Dash dust | `#D1D6DD`, `#89919D` |
| Muzzle | `#FFC072`, `#FF9823` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Gun** (M1) | `punkgoat_muzzle_flash`, `_tracer_impact` | pink sparks, RoR2 smoke | Added a ragged warm flash and a glow at the muzzle (two quads, throttled); the smoke is a cartoon puff. |
| **Bat** (M2) | `punkgoat_bashdown_bat_spikes`, `_bat_illum` | the pink sweep and slashes; a pink glow, grit, smoke on a hit | The sweep stays. Each hit adds a spiked pale-gold burst; the glow is Deadlock's glow sprite with a star core, the smoke a cartoon puff. |
| **Bashdown** | `punkgoat_sigil_slam_*`, `_sigil_explode_*` (ring, light spikes), `_sigil_ground_*` (dust, debris, flash) | pink line rings, 30 to 48 smoke puffs, 60 to 96 sparks, spikes, rocks, cracks | The rocks, spikes and cracks stay. The rings are Deadlock's shockwave rings; added a gold spiked ring, a violet spiral on the big slam, and 5 to 8 spikes of light standing up; 18 to 29 cartoon dust clouds; 40 to 64 sparks. |
| **Rising Ram** (Shift) | `punkgoat_dash_horn_glow`, `_horn_trail`, `_dash_dust`, `punkgoat_knock_up_target*` | a pink glow on the horns, pink trails, RoR2 smoke and 70 grit sparks a second | Horns light up gold with a pink star; pale gold ribbons off the horns going magenta; cartoon dust (11 a second) and 36 grit sparks a second. Impact: a shaft of yellow light going up with the launch, a pale flare, a gold spiked ring, cartoon dust, 46 sparks (was 69). |
| **G.O.A.T., float, Chain Gang** | `punkgoat_tether_*` | pink trails, grit, smoke | Unchanged in shape; their rings, glows and smoke bursts now draw with Deadlock's ring, glow and cartoon cloud through the shared building blocks. |

## Sheet and code changes
- No sheet changes. New `LightSpikes` in `VfxBilly.cs`.
- Shared (`Vfx.cs`, used by every hero from Billy on): `Ring` draws Deadlock's shockwave ring on one quad instead of a
  64-point line; `Burst` draws a still glow as one glow quad with a star core, and smoke as cartoon clouds (60 % of the count).

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#billy

Seen in game on the current build: the pink sweep, rocks, spikes and cracks are as before; the slam has cartoon dust, a gold ring and spikes of light; Rising Ram's launch has its shaft of light.

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 17.2 / 16.4 ms | 40 / 45 ms |
| Biquette | 19.8 / 17.7 ms | 81 / 96 ms |
| Bat | 17.4 / 16.5 ms | 31 / 36 ms |
| Bashdown | 17.7 / 21.0 ms | 31 / 31 ms |
| Rising Ram | 22.8 / 22.0 ms | 41 / 45 ms |
| Chain Gang | 21.4 / 20.5 ms | 41 / 31 ms |

Level with the old build except Bashdown, 3.3 ms a frame dearer in this run: its dust is big see-through clouds. The shared smoke burst now draws 45 % of the old count (was 60 %) at half opacity; not measured again. The gun's first shot is the same hitch every hero's gun has, in both builds.

Not done: the normal in-game test (`bash tools/run_test.sh billy`) on this branch; the creator's sign-off on the look.
