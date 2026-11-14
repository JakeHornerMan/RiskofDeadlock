# Viscous: VFX rebuild

Code: `plugin/Runtime/VfxViscous.cs`. Recipes read: `charactersFinal/viscousFiles/deadlock/particles/abilities/viscous/*`
(`goo_grenade_*`, `viscous_ball_*`, `viscous_bowling_*`, `viscous_chargeup_*`, `viscous_glob_portal_*`). Method and tools:
`README.md` in this folder.

## The headline: the goo stays, Deadlock's flash, ring and bits go on top
Viscous already has his own cel-shaded goo (blobs, the cube, the fist, the puddle, the ball), built with the creator over
several rounds, and its greens match Deadlock's (`#106B1F`, `#0DB929`, `#24EB38`, highlights `#D6FDDB`). That stays as it is.
What Deadlock's recipes add to every goo burst, and ours lacked, is light and energy: a green flash, a shock ring racing over
the ground, glowing bits flung up, and steam on the big ones. Those are now laid over his splashes, and a few heavy glow
emitters became single quads.

| Use | Deadlock colour |
|---|---|
| Burst flash | `#47E83C`, `#6DDA14` |
| Shock ring | `#15AA15` |
| Glowing bits | `#24EB38` > `#073514` |
| Steam | `#16B90D`, `#42B419` |
| Muzzle | `#ADDD13` fluid, `#60D51C` gas |
| Puddle Punch portal | `#3A9721` |
| Goo Ball landing ring | `#768E76` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Deep Sea Interpreter** (M1) | `viscous_chargeup_firing`: a lick of yellow-green fluid, green gas | three droplets at the muzzle; droplets and a stain on a hit | Added: a small ragged yellow-green flash and a puff of green glow at the muzzle; a green flash and four glowing bits on a hit. Both throttled. |
| **Splatter** (M2) | `goo_grenade_explode`: flash, a ring to the reach, glowing bits, blobs, splashes, steam | the goo dome, globs, 90 droplets, two line rings, a flash light | The dome and globs stay; 60 droplets instead of 90; the line rings became a shock ring and a spiked ring on the ground; added the green flash and flare, 26 glowing bits and steam. Aim ring: one ring quad and 24 motes a second (was 90 glow particles a second). Refill: a spiral closing on the ground, 16 motes winding up (was 40), a flash. |
| **The Cube** (Shift) | `goo_grenade_explode` family for the burst | the same burst as Splatter | The same new burst layers when it breaks open. The cube itself is unchanged. |
| **Puddle Punch** (R) | `viscous_glob_portal_ground`: two spirals of goo turning on the ground, a punch tube | bubbles in a dome; the fist and a splash | Telegraph: two spirals turning opposite ways on the ground. Punch: the fist is unchanged; its splash gets the flash, ring and bits. Aim ring as Splatter's. |
| **Goo Ball** (Q) | `viscous_ball_*_impact`: flash, ring, splashes; `viscous_bowling_ball_movement_dust_ring` | globs and droplets | Start and slam: flash, ring, bits. Each bounce: a dull green ring puffing out. Each enemy hit: a flash (throttled). The pop at the end: the full burst with steam. |

Not copied: Deadlock's metaball blobs (`Blobs` renderer) and its erode-shader splash meshes. Ours are the cel goo pieces.

## Sheet and code changes
- No sheet changes. `VfxViscous.cs`: new `GooBurstLayers`, called from `Splash`, `BigSplash` and the ball's pop.

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#viscous

Seen in game on the current build: his goo, cube, fist and ball are unchanged, as intended; the bursts carry the new green flash, ring and glowing bits. Side by side the two builds look close.

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 16.8 / 17.1 ms | 37 / 49 ms |
| Deep Sea Interpreter | 20.8 / 20.4 ms | 63 / 42 ms |
| Splatter | 26.3 / 26.3 ms | 61 / 62 ms |
| The Cube | 24.7 / 25.5 ms | 57 / 58 ms |
| Puddle Punch | 20.7 / 22.3 ms | 38 / 59 ms |
| Goo Ball | 19.8 / 21.6 ms | 38 / 38 ms |

Level with the old build within one run's noise (Puddle Punch and Goo Ball 1.6 to 1.8 ms a frame dearer in this run, not repeated). Fewer live particles on every move.

Not done: the normal in-game test (`bash tools/run_test.sh viscous`) on this branch; the creator's sign-off on the look.
