# Haze: VFX rebuild

Code: `plugin/Runtime/VfxHaze.cs`. Recipes read: `charactersFinal/hazeFiles/deadlock/particles/abilities/haze/*` (82 files).
Method and tools: `README.md` in this folder.

## The headline: Haze was the wrong colour
Our Haze was pink and violet (her RoR2 survivor colour). **Deadlock's Haze is amber.** Every ability of hers is orange
dream-sand and hot orange sparks over inky black "nightmare" smoke, with dark violet flecks in the smoke and a thin blue edge
on the dagger. Colours taken from the recipes:

| Use | Deadlock colour |
|---|---|
| Bullet Dance rings, streaks, tracers | `#F69936`, `#FFBC57`, fading to `#CC4F1A` |
| Dream sand | `#A07445` (drawn lighter here, `#D9A066`, to read on grass) |
| Smoke Bomb smoke | black, with flecks `#60308D` on `#1B0336`; ground ring `#533A21`; flash `#F1D2AF` |
| Sleep Dagger sparks and halo | `#F9AE75` > `#C36920` > `#9C3F10`; edge `#6D9AF3` / `#7895EE` |
| Fixation (max stacks) | ring `#F19028`, streaks `#FF8B24` > `#D83E06`, body flares `#FF6982` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Twin SMGs** (M1) | `weapon_fx/haze`: an anime muzzle flash, orange spark tracers | RoR2's Commando tracer, RoR2's muzzle flash, nothing on hit | New `haze_gun`: a wide hot flare and an amber glow at the muzzle (two quads, no particles), a star and five sparks on a hit, chips off walls. Tracer tinted amber (`tracer_haze` `#FFB04A`). At most one flash per 0.045 s. |
| **Sleep Dagger** (M2) | `haze_sleep_dagger_proj`: glow, the blade's shape, an 8-point halo, a spinning rope ring, a bright trail inside a "darkness" trail, a blue edge; `_impact`: sparks, dark gas turning orange, flare, chips, light | a violet glow, a pink trail, a light; RoR2's stun effect on hit | Loop: amber glow, Deadlock's dagger shape spinning, a blue swoop whirling round it, an amber ribbon inside a wide ink ribbon, sparks. New hit: flare, burst ring, 20 gravity sparks, four puffs of gas going dark to orange, chips. |
| **Smoke Bomb** (Shift) | `haze_invis_smoke_bomb`: 16 + 24 black blotches, a pale flash, a tan ground ring, dream sand, two smear swirls; `_startsmoke`: violet flecks | RoR2's Bandit smoke bomb | New `haze_smoke`: 14 ink blotches swelling, a pale flare, a tan ring on the ground, violet plasma wisps, 22 motes of dream sand drifting up, violet flecks. |
| **Phantom Strike** (R) | a Deadlock item, no hero recipe: styled with her dagger and smoke | grey dust, a violet trail, pink/violet slashes | Ink where she vanishes and lands, a hot amber beam inside a wider beam of darkness, fading afterimage glows, a ground ring; each cut is two swoops (amber and blue) at a new angle with a star and sparks; the third cut adds a burst ring and ink. |
| **Bullet Dance** (Q) | `haze_bullet_flurry_cast`: ground rings 3 per second, rising streaks, two smear swirls (one dark), a telegraph band; `haze_flurry_muzzle_flash`, `_tracer` | a ring of three blade trails, pink glows, violet sparks | A spiral turning under her, a dark fan turning the other way, two amber swoops whirling at waist and chest height, amber rings rolling out over the ground three times a second, streaks rising round her, dream sand; an anime flare per shot; a star and sparks per hit; a ring, a flare and ink at the end. |
| **Fixation** (passive) | `haze_max_stacks_start` / `_debuff`: a sight ring, 32 orange streaks, flares | RoR2 flash and bolts in violet | New `haze_fixation`: an amber sight closing on the first stack; at ten a burst ring, a flare and 22 orange streaks. Buff colour `#F69936`. |

Not copied: the afterimage poses of her own model during Bullet Dance (`haze_flurry_poses_model`, up to 140 model copies in
Deadlock). Too dear for what it adds here.

## Sheet and code changes
- `design/effects.json`: custom rows `haze_gun`, `haze_smoke`, `haze_fixation`; texture row `haze_dagger_tex`; `tracer_haze` tint.
- `design/skills.json`: M1 `vfx` `haze_gun`; Smoke Bomb `vfx` `haze_smoke`, `effect` `none`. `design/buffs.json`: Fixation `vfx`, `color`.
- `States/Abilities.cs` (`self_buff` plays its row's `cast` visual), `Runtime/Projectiles.cs` (a projectile plays its row's `hit`
  visual where it lands), `Content/Buffs.cs` (a stack burst plays the buff's own `trigger` visual).

## Results (2026-11-12)
Before/after page with a clip of each ability, old beside new: https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue

Seen in game on the new build: all five moves draw in amber over ink as described above.

Frame spikes, single player (the creator, 2026-11-12: "i just want less of a spike on when we use the moves"; the 4-player pass
was dropped from the bench the same day). Worst frame in each move's first two seconds, one 6-second run per build, no
screenshot pass first, so the first use is inside the measurement:

| Move | Old build | New build |
|---|---|---|
| Standing still | 61 ms | 37 ms |
| Twin SMGs | 150 ms | 93 ms |
| Sleep Dagger | 59 ms | 31 ms |
| Smoke Bomb | 66 ms | 34 ms |
| Phantom Strike | 63 ms | 33 ms |
| Bullet Dance | 57 ms | 33 ms |

Read with care: the old build's run was slower even standing still (29 ms a frame against 16 ms), so the PC was busier during
it and part of the gap is not the effects. What it shows: the new effects add no spike of their own; the gun's first shot is
the one real hitch, in both builds. The kit now prepares all its textures at load (`WarmKit`), so no move prepares one on
first use.

Not done: the normal in-game test (`bash tools/run_test.sh haze`) on this branch; the creator's sign-off on the look.
