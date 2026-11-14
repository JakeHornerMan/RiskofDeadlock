# Abrams: VFX rebuild

Code: `plugin/Runtime/VfxAbrams.cs`. Recipes read: `charactersFinal/abramsFiles/deadlock/particles/abilities/abrams/*` and
`weapon_fx/bull/*` (234 files). Method and tools: `README.md` in this folder.

## The headline: Abrams was the wrong colour
Our Abrams was red and yellow. **Deadlock's Abrams is electric blue**: a blue shield of air and arcs on Shoulder Charge, blue
rings and a column of light on Seismic Impact with grey debris, smoke and the pages of his book thrown up, blue book magic
with a flick of crimson on Siphon Life. Only his shotgun is orange. Colours taken from the recipes:

| Use | Deadlock colour |
|---|---|
| Shoulder Charge shield, ribbons, rings | `#2982EE`, `#30ADF6`, `#5CD1FF` over `#0D3871`; streaks `#C3DFFF` |
| Dust (charge, landing) | `#AEA798`, `#8E8376` |
| Seismic Impact rings, column, trail | `#3E65FE`, `#5787FF`, `#1C49A4`; flash `#FF3FCC`; debris `#697082`; pages `#EBDBC2` |
| Siphon Life | `#105CD0`, `#1F5DE2`, with crimson `#CD3259` |
| Shotgun muzzle | `#F39F2A`, `#FF8B24`; smoke `#DEDEDE` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Shotgun** (M1) | `weapon_fx/bull`: a ragged orange flash, hot gas, embers, pale smoke | RoR2's tracer and muzzle flash, nothing on hit | New `abrams_gun`: a ragged orange flash and a ball of hot gas (two quads), five embers and three wisps of pale smoke; a star and seven sparks on a hit; grey chips off walls. At most one flash per 0.07 s. |
| **Siphon Life** (M2) | `abrams_siphon_life_buff_start`: a blue wave over the ground, a glyph, blue motes, pages whirling, crimson | RoR2's urn tether only | New `abrams_siphon` on the cast: two blue rings over the ground, a blue flare, one of Deadlock's glyphs rising over him, 28 blue motes, crimson plasma wisps, ten pages of his book whirling up. |
| **Shoulder Charge** (Shift) | `abrams_charge`: a blue shield of air with arcs, pale streaks, shoulder ribbons, cartoon dust; `_start`: rings; `_wall_impact`: dust ring, flash | red and yellow streaks, RoR2 dust | A turning blue ring and glow on his leading shoulder with lightning arcs, pale sparks streaming back, blue ribbons off both shoulders and one along the ground, beige cartoon dust. Two blue rings and a kick of dust where he sets off. New `hit` moment: a burst ring, a pale flash, sparks and dust on each enemy he rams. |
| **Haymaker** (R) | ours (no Deadlock move): styled with the charge's blue | a yellow wind-up, a red-orange cone | The fist gathers blue light, a spiral and sparks; the punch is a spiked burst, a ring, a star and sparks thrown along the punch; the wave is dust and blue sparks racing out in the cone with three blue rings laid along it. |
| **Seismic Impact** (Q) | `abrams_leap_hover` (reticle), `_start`, `_crash_trail`, `_impact`: blue rings, a column of light, hot ground, debris, smoke, paper | a red reticle, burning streaks, an orange blast | Reticle: a blue dome edge, a turning spiral and ring, motes rising round the edge, a thin column. Take-off: rings, a streak of light, dust. Dive: a wide blue ribbon, a glow, sparks. Landing: three blue rings racing out, a spiral of hot ground cooling for 2.6 s, a column of light, a pink-white star, 28 chunks of grey debris, 36 sparks, 18 puffs of smoke going grey to blue, 16 pages thrown up. |

The landing flash was a white-out on the first in-game look (the glow quad scaled with the 18 m radius and filled the screen).
Its size is now capped at 9 m, it is 70 % opaque and the flash light is half as bright.

## Sheet and code changes
- `design/effects.json`: custom rows `abrams_gun`, `abrams_siphon`; texture row `abrams_paper_tex` (the book's pages).
- `design/skills.json`: M1 `vfx` `abrams_gun`; Siphon Life `vfx` `abrams_siphon`; `effect` `none` on Shoulder Charge, Haymaker and
  Seismic Impact (RoR2's orange `charge_impact`, `punch_impact` and `slam` prefabs played over the new blue effects).
- `VfxKit.cs`: `Spark` (sparks in any colour; RoR2's spark material drew his blue sparks yellow) and the beam strip made in code.
- `States/Abilities.cs`: `mired_urn` plays its row's `cast` visual; `dash_charge` relays its row's `hit` visual for each enemy rammed.

## Results (2026-11-13)
Before/after page (pick Abrams in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#abrams

Seen in game on the current build: every move draws in blue. The three faults from the first look are gone: the beams are soft
columns of light (not flat rectangles), the sparks are blue (not yellow), and RoR2's orange impact effects no longer play.
RoR2's own stun stars still show on stunned enemies; they are the game's, not ours.

Frame spikes, single player. Worst frame in each move's first two seconds, one 6-second run per build, the first use inside
the measurement. Both runs idled alike (17.6 and 16.8 ms a frame), so they compare fairly:

| Move | Old build | New build | Average frame, old / new |
|---|---|---|---|
| Standing still | 35 ms | 34 ms | 17.6 / 16.8 ms |
| Shotgun | 82 ms | 94 ms | 18.6 / 18.2 ms |
| Siphon Life | 31 ms | 42 ms | 18.4 / 17.9 ms |
| Shoulder Charge | 34 ms | 35 ms | 19.1 / 18.6 ms |
| Haymaker | 34 ms | 37 ms | 18.0 / 17.9 ms |
| Seismic Impact | 36 ms | 32 ms | 18.7 / 18.1 ms |

The new effects cost no more than the old on average (slightly less on every move). Spikes are level except two: Siphon Life's
first cast is about 10 ms worse (glyph, pages and motes built at once), and the shotgun's first shot is a hitch of 80 to 95 ms
in **both** builds. That first-shot hitch is the same one Haze's gun has (93 to 150 ms): it comes with the first bullet a hero
fires, not with the rebuilt effects, and is the one spike worth fixing next.

### After the creator's look (2026-11-13)
"where ... is Abrams' blue aura in Siphon Life and the open of the book, the pages around his feet?? The circle of Shoulder
Charge should cover body from foot to head, at the moment the bottom is at his mid section."
- **Siphon Life** had only a burst on the cast. It now has a `loop` moment for as long as the drain lasts
  (`abrams_siphon_life_buff`): a blue aura over his whole body with a turning ring, plasma and bands of light climbing him,
  motes rising, the book's ring turning on the ground under him with its red symbol, 16 pages a second whirling round his feet
  and up past his knees, and a ring on the ground at the drain's reach. `MiredUrn` plays it with the row's duration; it ends by
  itself. Seen in game: the aura and the pages read clearly.
- **Shoulder Charge**: the shield's centre was 1.2 m above the body's centre (the root follows his middle, not his feet), so it
  sat round his head. It is now centred on his middle and 4.2 m across, covering him from foot to head; the shoulder and
  ground ribbons were lowered to match. Seen in game.
- The spike numbers above were taken before the Siphon Life loop was added; its cost has not been measured.

### His book (2026-11-13)
The creator: "where ... is Abrams' book??? This character should have this!!!" Deadlock's Abrams model has a fourth mesh,
`book_model`, with its own bones and clips (`book_float_idle`, `book_stand_idle`). `heroes.json` `meshInclude` listed only
`abrams_model`, `abrams_face_model` and `gun_model`, so the extractor dropped the book. `book_model` is now included (a seventh
material in his cache folder). Seen in game with no other change: the book hangs at his hip, and his own Siphon Life clip lifts
it open in his left hand while the aura and pages play.

To get the book a cache must be re-read with this branch's manifest:
`build/extract/DeadlockExtract.exe --deadlock <Deadlock> --out <folder> --manifest extractor/manifest.json --heroes abrams --force`
(30 s; done for this PC's dev cache on 2026-11-13, the old folder kept in `build/abrams-cache-backup`). Until this branch is
merged, main's dev guard may re-read the cache with main's manifest and the book goes again. The DLL build's
`charactersFinal/abramsFiles/mod-cache` has not been re-read.

Not done: the normal in-game test (`bash tools/run_test.sh abrams`) on this branch; the creator's sign-off on the look.
