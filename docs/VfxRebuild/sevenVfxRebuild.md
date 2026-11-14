# Seven: VFX rebuild

Code: `plugin/Runtime/VfxSeven.cs`. Recipes read: `charactersFinal/sevenFiles/deadlock/particles/abilities/gigawatt/*` and
`weapon_fx/gigawatt/*` (258 files; Deadlock's code name for Seven is "gigawatt"). Method and tools: `README.md` in this folder.

## The headline: amber-gold, not yellow
Our Seven was lemon yellow. **Deadlock's Seven is amber-gold electricity** with the odd blue arc in his gun, Lightning Ball,
Static Charge and Power Surge. Only Storm Cloud (our Thunderhead) is blue-white under a grey cloud, so "Thunderhead stays blue"
turned out to be right. Colours taken from the recipes:

| Use | Deadlock colour |
|---|---|
| Lightning Ball | `#FFA53F`, `#FF8D41`, `#FA703E`; zap `#F3B137`; hot core `#FFCE89` |
| Static Charge | `#FFD181`, `#F6A259`, `#FF7B00`, with blue arcs `#69B0FF` |
| Storm Cloud | `#53B8F8`, `#6AB2FF`, `#CBDCFF`; ground bolt `#83B2FF`, `#2478FF`; cloud `#AFB9C5` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Shock Rifle** (M1) | `gigawatt_muzzle_flash`: a lick of orange plasma, a small arc, a hot streak | RoR2's tracer and muzzle flash in yellow | New `seven_gun`: a wide amber flare and one of Deadlock's stylised arcs at the muzzle (two quads, no particles); a star, an arc and five sparks on a hit. Tracer tinted amber (`#FFA53F`). |
| **Lightning Ball** (M2) | `gigawatt_lightning_ball`: a white-gold core, an amber ring, a plasma halo, arcs; `_zap`; `_explode`: flare, chips, arcs (some blue) | a yellow glow and RoR2 lightning in a yellow sphere | A gold core in a turning amber ring and a plasma halo, Deadlock's arcs snapping round it, sparks, inside the clear sphere of its reach (now amber, with glints on its edge). Each zap is two golden bolts with a flare and sparks. The burst: a wide flare, a glow, a ring, 16 hot chips, 26 sparks, arcs, one of them blue. |
| **Static Charge** (Shift) | `gigawatt_static_charge_debuff`: a ring of arcs closing in; `_explosion`: the bolt, arcs, streaks, rings | a yellow ring and a yellow bolt | A ring of amber dots and arcs spinning and closing on the target while it counts down, a glow and sparks on it. The blast: two bolts from the sky, two rings on the ground, a glow, 30 hot streaks, arcs, one of them blue. |
| **Storm Cloud** (R) | `gigawatt_power_surge_cast` | a faint haze, yellow crackles, a yellow sphere | Cast: an amber ring, a column of light, arcs, sparks. Flight: plasma and arcs on him, sparks, an amber light, the sphere of his reach (drawn from outside only, with a ring where it meets the ground, because his camera is inside it). |
| **Thunderhead** (Q) | `gigawatt_storm_cloud*`: grey cloud, blue-white glow, ground bolts | a dark cloud deck, blue bolts | The cloud deck stays but is drawn with far fewer, larger puffs (it was the dearest thing he drew), with a blue-white glow in its base. Each strike is a white core in a blue bolt with a flare, a ring on the ground, blue sparks and a puff of pale dust. The extra bolts skip their flash light when many effects are alive. |

## Sheet and code changes
- `design/effects.json`: custom row `seven_gun`; tints `tracer_seven`, `lightning_orb`, `storm_strike_seven` `#FFA53F`.
- `design/skills.json`: M1 `vfx` `seven_gun`; Static Charge `effect` `none` (RoR2's blue lightning blast played over the amber one).
- Shared lightning arcs are cut-outs of Deadlock's `fx_arc` atlas (`arcA`, `arcB` in `VfxSeven.cs`).

## Results (2026-11-13)
Before/after page (pick Seven in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#seven

Seen in game on the current build: gun, Lightning Ball, Static Charge and Storm Cloud draw in amber-gold; Thunderhead stays
blue-white. RoR2's blue blast no longer plays over Static Charge.

Single player, two 6-second runs per build (first use inside the measurement). Average frame, then the worst frame of a
move's first two seconds:

| Move | Average, old (2 runs) | Average, new (2 runs) | First use, old / new |
|---|---|---|---|
| Standing still | 15.7, 16.4 ms | 15.9, 16.2 ms | 39 / 42 ms |
| Shock Rifle | 17.8, 18.2 | 17.6, 18.3 | 78 / 96 |
| Lightning Ball | 18.0, 18.5 | 17.9, 18.6 | 34 / 36 |
| Static Charge | 17.1, 17.8 | 20.9, 20.9 | 33 / 30 |
| Storm Cloud | 18.0, 23.6 | 23.1, 23.0 | 30 / 32 |
| Thunderhead | 17.5, 21.2 | 21.7, 20.2 | 37 / 34 |

- Spikes are level with the old build. The rifle's first shot is the same hitch every hero's gun has, in both builds.
- **Static Charge is about 3 ms a frame dearer** while it is on a target, in both runs. Its ring of dots was halved afterwards
  (60 to 30 a second); not measured again.
- Storm Cloud and Thunderhead swing between 18 and 23 ms **in the old build too**; the new build is inside that range. After
  the first new-build run the screen-covering glows were cut down (the plasma and glow on him in flight, the glow in the
  cloud base, the pale dust on each strike), which made no measurable difference.
- Standing still after his moves is about 5 ms slower than before them, in both builds (the Thunderhead outlasts the pass).

Not done: the normal in-game test (`bash tools/run_test.sh seven`) on this branch; the creator's sign-off on the look.
