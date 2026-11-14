# Paige: VFX rebuild (DLL build only)

Code: `plugin/Runtime/VfxPaige.cs`. Recipes read: `charactersFinal/paigeFiles/deadlock/particles/abilities/bookworm/*` and
her `weapon_fx` (Deadlock's code name for Paige is "bookworm"). Method and tools: `README.md` in this folder.

## The headline: her ghost green was right; the wyvern's fire should be lime
Paige is the newest hero and already draws Deadlock's own letters, dragon, knight, sword, heptagram, ring and floor streak.
Her see-through ghost green matches the recipes (`#14F794`, `#45EF9F`, `#90F3C6`; ours `#61FF9E`, `#C7FFDB`).

One colour was off. **Bookwyrm's fire is lime in Deadlock** (`bookworm_dragonfire`: `#9ACD32`, `#C9FC00`, `#70C11F`), a
yellower green than the rest of her magic, so the breath stands apart from the book. Ours used a mint close to her ghost
green. `paigeFire` is now lime (`#ADED38`).

| Use | Deadlock colour |
|---|---|
| Pages, letters, Captivating Read, knights | `#14F794`, `#45EF9F`, `#90F3C6`, deep `#056037` |
| Bookwyrm's fire | lime `#9ACD32`, `#C9FC00`, `#70C11F` |
| Sparks of ink | orange `#F39020` |

## What changed
| Move | Before | Now |
|---|---|---|
| **Chapter's End** (M1) | the page's core as two glow emitters (60 particles a second) | Two glow quads. Letters, trail and sparks unchanged. |
| **Bookwyrm** (M2) | fire in mint green | Fire, burning ground and their lights in lime. |
| **Captivating Read, Rallying Charge, The Green Knight** | rings as lines, still glows and smoke as bursts | The same shapes through the shared ring, glow and cartoon cloud. |

## Sheet and code changes
- No sheet changes.

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#paige

Seen in game on the current build: run as the DLL build with the kit's textures added to its hero folder. Her pages, wyvern, sword circle and knights look as before. The still glows drawn as single sprites read as soft round blobs on The Green Knight; the shared glow was made smaller and fainter afterwards (not seen again). The lime fire did not show in the sampled frames.

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 15.6 / 15.3 ms | 37 / 35 ms |
| Chapter's End | 16.9 / 16.5 ms | 47 / 50 ms |
| Bookwyrm | 17.0 / 17.9 ms | 30 / 28 ms |
| Captivating Read | 16.0 / 19.5 ms | 34 / 36 ms |
| Rallying Charge | 16.9 / 19.6 ms | 39 / 29 ms |
| The Green Knight | 20.5 / 20.2 ms | 36 / 34 ms |

Level with the old build except Captivating Read and Rallying Charge, 2.7 to 3.5 ms a frame dearer in this one run (not repeated).

### The sword was upside down (2026-11-14)
The creator: "paige's sword is upside down, the hilt is smashing into the floor not the edge of the blade". Captivating Read's
giant sword came down pommel-first and stood with its tip in the air, in the old build too (it is not from the rebuild).
`VfxPaige.cs` turned the model over (`SwordRot` 180° about x) on the note that it "stands tip-up"; as it is read now it stands
tip-down (tip 0.5 m, hilt 5.18 m above its root), so the flip put the pommel in the ground. `SwordRot` is now the identity and
`SwordRest` puts the root under the floor so the tip sits 0.6 m in. Seen in game: point-first, standing with the pommel on top.
The same fix is needed on `main`.

### Bookwyrm's fire as flames (2026-11-14)
Her breath and burning ground used RoR2's round fire puffs; they now draw Deadlock's flame tongues in her lime (shared
`VfxFire.cs`, see Infernus' write-up). Seen in game once: small lime flames on the ground under the wyvern's pass. Not timed.

Not done: the normal in-game test (`bash tools/run_test.sh paige`) on this branch; the creator's sign-off on the look.
