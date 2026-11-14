# 14. The VFX look guide (how to decide what an effect looks like)

Written 2026-11-14, when the `vfx-rebuild` branch was merged into `main`. It is the short version of what was learned
rebuilding the effects of all 13 heroes and the Troopers' soul orb. Use it for **every new hero** (doc 08 §4), **every new
enemy** (doc 11 §2, doc 12 §2.8) and any change to an existing effect.

- The method, tools and bench in full: `docs/VfxRebuild/README.md`.
- One worked example per character (Deadlock's colours and layers, what we had, what was built, what went wrong on the first
  look): `docs/VfxRebuild/<character>VfxRebuild.md`. Read the one closest to the character you are making before you start.
- Number 13 is kept for the enemy playbook that doc 12 §2.8 plans.

The creator's bar, in their words: "rebuilt to look wayyy better but still follow the deadlock style"; "make sure the vfx
match the deadlock game"; "performance is really important"; "i just want less of a spike on when we use the moves".

## 1. The decisions, in order
Make them in this order. Each one below came from a real mistake or a real note from the creator.

1. **Find the right recipes.** Deadlock's effect recipes (`.vpcf`) for a hero sit in
   `charactersFinal/<id>Files/deadlock/particles/abilities/<code name>/` and `weapon_fx/`; an enemy's in
   `charactersFinal/npc/<group>/deadlock/particles/`. The folder is often a **code name**, not the hero's name: Paige is
   `bookworm`, Billy `punkgoat`, Mina `vampirebat`, Victor `frank`. If there are two sets (Pocket has an old violet `synth_*`
   and a current teal `pocket_*`), the set that goes with the model named in `heroes.json` is the current one.
2. **Take the colours from the recipes, never from RoR2 or from a guess.** Three of the first four heroes were the wrong
   colour until this was done: Haze was pink and violet (Deadlock: amber sand over black smoke), Abrams red and yellow
   (Deadlock: electric blue, only the shotgun orange), Seven lemon yellow (Deadlock: amber-gold, only Storm Cloud blue).
   The survivor colour in the menu is not the effect colour. Write the palette as a comment at the top of the character's
   `Vfx<Name>.cs`, with the recipe each colour came from.
3. **What the creator already chose stays.** Viscous' cel goo, Billy's stone and pink swipe, Victor's wavy green strikes and
   red blood, Mina's bats, Wraith's purple smoke were all tuned with the creator. Lay Deadlock's missing layers on top of
   those; do not replace them. A recolour of something the creator has seen and accepted is their call: do it so that it is
   a few constants to undo, and tell them (Pocket's teal; Mina's violet was left alone and listed as an option).
4. **List Deadlock's layers for each move, then ours, then the gap.** One row per move: recipe names, what Deadlock draws,
   what we draw, what is missing. This table goes in the character's write-up. Most of what was missing was the same few
   layers every time (§2).
5. **Check the model for parts that belong to a move.** Abrams' book is a fourth mesh (`book_model`) with its own clips; it
   was left out of `heroes.json` `meshInclude`, so Siphon Life had no book. Look at the mesh and clip lists for anything named
   after an ability.
6. **Decide what not to copy, and write it down.** Too dear or not ours: Haze's 140 afterimage copies of her own model,
   Viscous' metaball blobs, Rat King's rat and knight models. Each write-up has a "Not copied" line.
7. **Build it with the kit (§3), look at it in game (§5), then fix what the screenshots show.** No effect was right on the
   first look. Plan for two or three looks per character.

## 2. What a Deadlock effect is made of
Deadlock's look is flat and graphic: clean rings, spiky stars, cartoon dust, marks on the ground, a few bright sparks. It is
not thick realistic smoke or a pile of soft glows. The same layers come back in every hero:

| Effect | Deadlock's layers | How we draw it |
|---|---|---|
| Gun muzzle | a ragged flash, a hot core, smoke on some shots | two quads (`fx_muzzle`, `fx_flare` or `fx_glow`), no particles, no light; smoke on about one shot in three; `Every(key, 0.045-0.06 s)` |
| Bullet hit | a small star, a few sparks, chips off walls | one `fx_star` quad, five `Spark`s; throttled |
| Tracer | a thin streak in the hero's hot colour | the sheet's tracer row with a `tint` |
| Blast, slam | a flash, a shock ring racing over the ground to the real reach, a spiked ring, sparks cooling to a darker colour, debris flecks, cartoon dust, a scorch or mark left on the ground | quads for the flash, rings and mark (`fx_ring`, `fx_ring_burst`, `Ground`); `Puff` for sparks and `fx_fleks`; `fx_cloud` for grey or tan dust |
| Dash, leap | a ribbon behind the body, dust at the feet, a shield or glow on the front | `Ribbon`, a few clouds a second, one quad |
| A move that lasts (drain, aura, channel) | a mark turning on the ground, an edge ring, motes rising, something on the body | a `loop` moment for as long as it lasts, not only a burst on the cast (Abrams' Siphon Life had no aura until the creator asked where it was) |
| Ground zone | dark projected ground, a ring of streaks on the edge, light smoke off the edge | a dark `Ground` disc, two rings turning opposite ways, a few clouds (Victor's Aura of Suffering) |
| Projectile in flight | a short ribbon cooling to a darker colour, a small glow, a few sparks | `Ribbon`, one quad, a low-rate `Emit` |
| Fire | stylised flame tongues, pale at the root, darker as they rise | any call that asks for RoR2's fire materials is drawn as flames (`VfxFire.cs`, `fx_fire`); fire that belongs on the ground is put there with `Floor(at)` |
| Lightning | stylised arcs | cut-outs of `fx_arc` |
| Magic smoke (coloured) | plasma wisps | RoR2's smoke in the hero's colour, or `fx_plasma`; **not** the cartoon cloud |
| Marks on the world | scorch, slime, tar, cracks | one `Ground` quad that stays 2.5 to 5 s and fades; one per burst, never one per projectile |
| Pick-up (soul orb) | a dark core, a veined shell, thin rays streaming off, a short wisp behind, a ring where it appears, flares and motes when taken | `VfxEnemies.cs` (`SoulOrbAmbient`, `Appear`, `Claim`, `End`) |

## 3. Which tool for which layer (`plugin/Runtime/VfxKit.cs`)
- **Anything that is one shape growing and fading is a quad** (`Quad`, `QuadAt`, `Ground`): rings, flares, flashes, still
  glows, ground marks. A quad costs almost nothing next to a particle system. 19 still-glow emitters became single quads.
- **Many small things are one particle system** from one description: `Emit(parent, new P { ... })` for a stream,
  `Puff(at, new P { ... })` for a burst. Colour A to B over life is how sparks cool.
- `Glow(texture, colour)` is additive (light). `Soft(texture, colour)` is alpha-blended (smoke, dark marks, anything that
  must read on bright ground). `Raw(texture)` is the texture as it is (the pages of Abrams' book).
- `Ribbon` (a trail), `Beam` (a straight fading beam), `Swoosh` (a curved cut), `Spark` (sparks in any colour), `Strip` (the
  beam strip, made in code).
- `Every(key, seconds)` throttles anything that repeats. `N(count)`, `Rich` and `Glint` let particle counts, optional layers
  and flash lights give way when a great many effects are alive. Use them on every emitter and light you add.
- `Ring` and `Burst` in `Vfx.cs` are the shared ring and glow or smoke burst; every hero that calls them gets Deadlock's ring
  and glow.
- Without a texture (an older cache) the kit falls back to RoR2's glow or smoke, so an effect gets plainer but never breaks.

Deadlock's shared textures (`design/effects.json`, `texture` rows whose id starts with `fx_`; read from the player's own
install, never shipped): `fx_glow`, `fx_glow_soft`, `fx_star`, `fx_flare`, `fx_flare_wide`, `fx_ring`, `fx_ring_burst`,
`fx_ring_spiral`, `fx_ring_thick`, `fx_ring_soft`, `fx_swoop`, `fx_spin`, `fx_cloud` (cartoon cloud), `fx_puff` (blotch),
`fx_dust`, `fx_smoke`, `fx_plasma`, `fx_arc`, `fx_fleks` (debris), `fx_beam`, `fx_muzzle`, `fx_fire`, `fx_glyphs`. A texture
only one character uses is a row named `<id>_<thing>_tex`. Reuse a shared one before adding a row.

## 4. What went wrong on the first look, and the rule each time
| Seen in game | Cause | Rule |
|---|---|---|
| Abrams' landing flash whited the screen out | a glow quad sized by the move's 18 m radius | cap a flash quad at about 9 m, 70 % opaque, and halve its light |
| Wraith's purple smoke became flat pink blobs | smoke bursts drawn as the cartoon cloud | the cartoon cloud is for grey and tan dust only; coloured smoke stays soft |
| Billy's Bashdown, Mo & Krill's Sand Blast, Infernus' first flames ran 3 to 4 ms a frame slower with fewer particles | big see-through sprites stacked on each other | big transparent sprites are the dearest thing we draw: fewer, smaller, about half opacity |
| Infernus' flames were taller than him, pale and see-through | sizes meant for round puffs given to tall tongues | flames at 70 % of the asked size, 0.45 to 1.7 m, solid and only a little white |
| Beams drew as flat pale rectangles | Deadlock's rope textures run across, Unity's trails along | use `Beam` and `Strip`, never a rope texture on a trail |
| Abrams' blue sparks drew yellow | RoR2's spark material has its own colour | `Spark` for coloured sparks |
| RoR2's orange and blue impacts played over the new effects | the skill row still named a RoR2 `effect` | when a move has its own effect, set the row's `effect` to `none` |
| Shoulder Charge's shield sat round his head | the body's root is its middle, not its feet | centre on the root and size to the body (4.2 m covers Abrams foot to head) |
| Rat King's banner rays were thick grey-white pillars | stretched glow particles too wide and untinted | rays thin and in the hero's colour |
| Still glows read as soft round blobs (Paige's Green Knight) | one glow sprite where a small burst was | smaller and fainter than the emitter it replaces |
| Dust Devil's fans drew as flat cream discs; its kit ribbons were too faint | stacked fan sprites; thin additive ribbons on a bright stage | no stacked fans; when the old drawing reads better, keep it |
| Pocket's slime splats read as dark smudges | dark `Soft` marks on grass | a dark mark needs a brighter slick on top |
| The soul orb was a white blob | a glow six orb-widths wide at full strength, shell emission 2.4 | glow about 3.6 widths at a third the strength, emission under 1, so the dark veins show |
| Napalm burned in the air on the enemy | the effect played where the flask hit | ground effects go to `Floor(at)` |
| Napalm and Flame Dash got slower though they drew half the particles | every splash laid its own stain, scorch and glow | one ground mark per burst; a scorch under about every third patch (`Every`) |
| Haze's dream sand vanished on grass | Deadlock's own sand colour is dull | additive colours can be lifted a step to read on RoR2's bright stages; say so in the write-up |

Older rules still hold (doc 02 §12): no opaque dust walls, no `Shader.Find`, bright additive layers in darker tints, thin
trails behind the hero, ground markers as domes or edge particles, never flat line circles.

## 5. Looking and measuring
- **Look before you measure.** Screenshots of each move on the test spot, old build beside new:
  `FPS=1 SHOTS=<folder> DLL=<dll> bash tools/run_bench.sh 3 <hero>` (16 pictures per move), joined with `tools/fx_sheet.py`.
  A DLL-build-only hero or an enemy needs the DLL build with its `heroes/` folder beside it (doc 03 §5c, doc 11 §6).
- **One character per game run**, other apps closed (the creator, 2026-11-12, after a run was stopped for low memory). A
  6-second pass per move is enough: `FPS=1 DLL=<dll> FPSOUT=<file> bash tools/run_bench.sh 6 <hero>`.
- **What the numbers can and cannot say on this PC.** Two runs of the same build differ by 3 to 12 ms a frame, and a run
  slows as it goes. So a difference under about 3 ms in one run is not a finding; say "level within one run's noise" or
  repeat it. What is solid: live particle and light peaks, and a spike in a move's first two seconds.
- **Budget:** a normal move about 150 live particles and 2 lights; an ult about 400 and 4. For an enemy, think of twenty of
  them on screen: no light per enemy or per pick-up, shared materials, a handful of particles each.
- **The one known hitch is not ours to chase here:** every hero's first gun shot stalls 80 to 95 ms in the old build and the
  new. The kit loads its textures at start (`WarmKit`), so no move loads one on first use.
- Visual-only work changes no gameplay number, so it needs no "Retest needed" row and no DPS bench.

## 6. Honest write-ups
Each character gets `docs/VfxRebuild/<id>VfxRebuild.md` (enemies: `<group>VfxRebuild.md`) with: the headline (was the colour
right?), the palette table with recipe names, the move-by-move table (Deadlock, before, now), what was not copied, sheet and
code changes, and results. In the results say plainly:
- what was **seen in game** and what was changed afterwards and **not seen again**;
- how many runs each number comes from, and whether the old and new runs idled alike;
- that the look was worked from Deadlock's effect files, not from pictures of Deadlock, unless the creator gave pictures.
  When the creator cares about one effect ("i really want you to look at the soul orb"), ask for a picture of it.

## 7. Checklist for a new hero or enemy
- [ ] Recipes dumped (`vrfdump.exe ... texts particles/`), the right set chosen, `tools/vpcf_summary.py --brief` read.
- [ ] Palette from the recipes at the top of the `Vfx` file; nothing coloured from the menu colour.
- [ ] Mesh and clip lists checked for parts that belong to a move.
- [ ] Per move: muzzle, hit, cast, loop (if it lasts), end, ground mark, each with Deadlock's layers or a "not copied" note.
- [ ] RoR2's own `effect` set to `none` wherever our effect replaces it.
- [ ] Rings, flares, glows and marks are quads; every emitter uses `N()`, every repeat `Every()`, every light `Glint`.
- [ ] Seen in game twice at least; faults from §4 looked for (white-out, blobs, pillars, wrong height, effects in the air).
- [ ] One timed run beside the old build or a like hero; anything over 3 ms dearer repeated or thinned.
- [ ] Write-up in `docs/VfxRebuild/`, clips on the before/after page, the creator asked to sign off the look.

## 8. Still open (2026-11-14)
- The Troopers' muzzle, bullet, melee hit, Medic heal, death motes, head glow and jump dust are compared but not rebuilt
  (`docs/VfxRebuild/troopersVfxRebuild.md` has the table).
- Not measured again after their last change: Seven's Static Charge, Billy's Bashdown, Abrams' Siphon Life loop, Victor's
  aura, the flames (Infernus, Paige). Not seen again: Rat King's rays, Pocket's brighter slime, Mo & Krill's Dust Devil arms.
- No "Effects quality" setting yet (`Vfx.Quality` exists, nothing sets it); no particle pool; the relay of the moments
  `execute`, `root`, `heal`, `zap`, `mark` to other players (`Net.cs`) is still missing.
- The creator has not signed off any hero's new look, and decides whether Pocket stays teal.
