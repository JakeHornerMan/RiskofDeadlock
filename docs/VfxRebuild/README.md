# VFX rebuild (branch `vfx-rebuild`, started 2026-11-11)

The creator's request: "i want every vfx for each character to be rebuilt to look way better but still follow the deadlock
style ... review each character and their moveset and then compare to their deadlock character counterparts ... outline your
process for each character in their own separate explanation file". And: "performance is really important ... fps benchmarks on
before and after".

**Merged into `main` on 2026-11-14.** The short guide for making the same decisions on a new hero or enemy is
`docs/14-VFX-LOOK-GUIDE.md`. §8 below is the first night's results (2026-11-12); each character's own file has its later
looks, fixes and numbers, and the before/after page is https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue.

One file per character sits beside this one (`<character>VfxRebuild.md`). This file is the method they all share.

## 1. What changed in how effects are made
Before this branch every effect was built in code from RoR2's materials and nothing was read from Deadlock's effect files.
The creator chose (2026-11-11) to go further:

- **Deadlock's particle recipes are read for reference.** Each hero's folder in `charactersFinal/<hero>Files/deadlock/particles`
  holds the recipes (`.vpcf`) of every ability. They give the real colours, the layers an effect is made of, their sizes and
  timing. They are read on the dev PC only; nothing from them ships.
- **Deadlock's particle textures are drawn.** Glows, rings, flares, smoke puffs, lightning arcs, debris flecks. They are
  `texture` rows in `design/effects.json`, so the extractor reads them from the player's own Deadlock install like the icons
  (and the DLL build carries them in its `heroes/` folder like every other cache file). Shared ones start with `fx_`.
- The effects themselves are still built in code, in Unity, at runtime.

The biggest finding of the comparison: **several heroes were the wrong colour.** Haze was pink and violet (Deadlock: amber
dream-sand over black smoke), Abrams red and yellow (Deadlock: electric blue), Seven yellow (Deadlock: amber-gold, only
Storm Cloud is blue). Each character's file lists what Deadlock does and what we do now.

## 2. Tools
| What | Command |
|---|---|
| Dump a hero's recipes to text | `build/vrfdump/vrfdump.exe charactersFinal/<hero>Files/deadlock texts particles/ build/vfx-rebuild/recipes/<hero>` |
| One line per recipe (emit, colours, textures) | `python tools/vpcf_summary.py --brief build/vfx-rebuild/recipes/<hero> <name-part>` |
| Full summary of a recipe | `python tools/vpcf_summary.py build/vfx-rebuild/recipes/<hero> <name-part>` |
| Look at textures | `vrfdump.exe <folder> pngs materials/particle/ build/vfx-rebuild/tex/<hero>` (never copied into the repo) |
| A texture's sprite-sheet frames | `vrfdump.exe <folder> sheet materials/particle/sparks/sparks.vtex` |
| Add new texture rows to a dev cache without a full rebuild | `vrfdump.exe <Deadlock pak01_dir.vpk> cacheimages extractor/manifest.json %LOCALAPPDATA%/DeadlockOfRain/cache/images` |
| Fps bench and effect screenshots | `FPS=1 SHOTS=<folder> DLL=<dll> bash tools/run_bench.sh 12 <heroes>` |
| Compare two fps results | `python tools/fps_compare.py --before a.txt --after b.txt [--md]` |

`build/vfx-rebuild/` (gitignored) holds the dumps, the "before" build and all screenshots.

## 3. The effect kit (`plugin/Runtime/VfxKit.cs`)
- `Glow(texId, colour)`: a Deadlock texture drawn additively (RoR2's soft-glow material with its texture swapped).
  `Soft(texId, colour)`: the same shape alpha-blended (smoke, marks that must read on bright ground). `Raw(texId)`: the texture
  as it is (the pages of Abrams' book). A cut-out of an atlas is a `Rect`. Without the texture (an older cache) they fall back
  to RoR2's glow or smoke, so an effect gets plainer but never breaks.
- `Emit(parent, new P { ... })` / `Puff(at, new P { ... })`: one particle system from one description: colour A to B over
  life, fades, size over life, shape, drag, noise, gravity, flipbook tiles, stretch, flat.
- `Quad` / `QuadAt`: one textured quad that grows and fades (a shockwave ring, a flare, a mark laid on the terrain's slope),
  or loops while a move lasts. A quad costs almost nothing next to a particle system, so rings, flares and ground marks are quads.
- `Ribbon` (a textured trail), `Beam` (a straight fading beam), `Swoosh` (a curved cut).
- `Every(key, seconds)`: a repeated effect (a muzzle flash, a hit spark) plays at most once per interval.
- `Rich`, `N(count)`, `Glint`: optional layers, particle counts and flash lights give way when a great many effects are
  alive (four players' ults at once) or the "Effects quality" setting is lowered.

## 4. Performance: the fps bench
`FPS=1 bash tools/run_bench.sh 12 <heroes>` (`plugin/Runtime/DpsBench.cs`):
- always the same stage (`golemplains`) and the same spot (searched from a fixed map node), a pack of 12 enemies that cannot
  die and is put back on its spots before every pass, the run cannot end, no frame cap;
- passes per hero: `idle`, `M1`, `M2`, `Shift`, `R`, `Q` (each used as often as it allows for 12 s), `all` (everything, 18 s),
  `x4` (four copies of the hero using everything at once: the 4-player worst case), `idle2`;
- per pass: average, p95, p99 and worst frame time, the worst frame of the first 2 s (a first-use hitch), GC collections, and
  sampled four times a second: live particles, particle systems, lights, line and trail renderers; objects and systems left
  behind 5 s after the pass (a leak check);
- with `SHOTS=<folder>` each single move is also used once more first and 16 screenshots of it are saved.

**What is compared** is a pass's cost over idle: its average frame time minus the average of the same run's two idle passes.
The stage, the day and a warm PC cancel out of that.

**Gate** (`tools/fps_compare.py`, exits 1 on a fail): cost over idle not above before by more than 5 % or 0.3 ms or the noise
floor, whichever is larger; p99 over idle within 10 % or the noise floor; the worst frame within 5 ms; no first-use hitch
over 50 ms. Live particle and light peaks are printed beside them.

**Fair runs:** the "before" build is the commit that added the bench, with every effect untouched
(`build/vfx-rebuild/before/DeadlockOfRain.dll`). Nothing else heavy may run on the PC during a run (a build or a recipe dump
in the background moved every number by several ms on 2026-11-11, so those runs were thrown away).

## 5. Budget
A normal move: about 150 live particles and 2 lights at most; an ult: about 400 and 4. No opaque dust walls, no `Shader.Find`,
bright additive layers in darker tints (doc 02 §12). Nothing about gameplay changes: damage, timing, radii and `expect`
lists are untouched, so there are no "Retest needed" rows and no DPS re-bench.

## 6. How deep each character's rebuild went
| Character | What was done |
|---|---|
| Haze, Abrams, Seven | Rebuilt from scratch in new files; recoloured to Deadlock's palette. |
| Infernus | Deadlock's missing layers added; fire particle counts cut. Since 2026-11-14 his fire (and Paige's) is drawn as Deadlock's flame tongues, and Napalm burns on the floor. |
| Mo & Krill | Rebuilt from scratch in a new file; same palette; about a fifth of the particles. |
| Rat King | Every move rewritten; recoloured from yellow gold to Deadlock's amber and cream. |
| Pocket | Recoloured from violet (his old `synth` recipes) to teal (his current `pocket` recipes); new gun. A judgement call, see his file. |
| Viscous, Billy | Their own look kept (creator's choices); Deadlock's layers and colours added on top. |
| Wraith, Mina, Victor, Paige | Already Deadlock's colours and textures. Lighter drawing, new gun flashes, Paige's fire to lime. |
| Troopers | The soul orb matched to Deadlock's (2026-11-14). Their other effects compared only. See their file. |

Shared building blocks changed for every hero that still uses them (`Vfx.cs`): `Ring` is Deadlock's shockwave ring on one
quad (was a 64-point line); `Burst` draws a still glow as one glow quad with a star core and smoke as Deadlock's cartoon
cloud (60 % of the count); 19 still glow emitters across Wraith, Mina, Pocket, Paige and Billy became single quads.

## 7. Not done
- No particle-system pool. Effects are still built when they play; they are cheaper because rings, flares and ground marks
  are single quads and the particle counts are lower, and repeated effects are throttled.
- No "Effects quality" setting in the config yet: the kit has the switch (`Vfx.Quality`), nothing sets it.
- The relay bug in `Net.cs` (the moments `execute`, `root`, `heal`, `zap`, `mark` never reach other players) is not fixed here.
- The Troopers' own effects (see `troopersVfxRebuild.md`).

## 8. Results (2026-11-12)
### What was looked at and measured
| Character | Seen in game (screenshots) | Fps measured |
|---|---|---|
| Haze, Abrams, Seven, Infernus | yes, new build (an earlier build of it; the later fixes below are not seen yet) | once each, before and after |
| Mo & Krill | screenshots taken, not yet looked at | once, before and after |
| Viscous, Rat King, Billy, Wraith, Mina, Pocket | no | no |
| Victor, Paige | no (their DLL build needs the `fx_` textures added to its hero folders first) | no |

Screenshots: `build/vfx-rebuild/before/shots`, `build/vfx-rebuild/after/shots` (gitignored); contact sheets with `tools/fx_sheet.py`.

### Fps: one run each, `before-1` against `after-1`
Cost over idle of the 4-player pass (`x4`), and the peak of live particles and lights in it:

| Hero | x4 cost before | x4 cost after | Particles before > after | Lights before > after |
|---|---|---|---|---|
| Haze | +5.7 ms | +5.3 ms | 1682 > 728 | 28 > 17 |
| Abrams | +8.9 ms | +7.1 ms | 928 > 952 | 12 > 20 |
| Seven | +10.5 ms | +8.0 ms | 2671 > 1828 | 44 > 13 |
| Infernus | +5.3 ms | +6.6 ms (one 122 ms frame) | 6677 > 3220 | 126 > 47 |
| Mo & Krill | +8.6 ms | +5.3 ms | 3180 > 1018 | 45 > 17 |

Single moves were within about 1.5 ms either way.

### The gate cannot be decided on this PC yet
A second run of the **old** build (`before-2`, Haze and Abrams only, cut short) differed from the first run of the same
build by 3 to 12 ms per pass (Haze idle 18.5 ms against 24.1 ms; Haze `x4` cost +5.7 against +17.8). That run-to-run
spread is several times larger than any before/after difference above, so the frame-time gate in §4 says nothing either
way. Causes seen: the PC had 15 GB of memory with about 1 GB free while the game ran (other apps open), and frame time
drifted up through every run (idle 16 ms for the first hero, 24 ms from the third on, in both builds).
**What is solid:** the live particle and light counts, which fell for every hero measured except Abrams' particles (level).

To measure properly: one hero per run (about 4 minutes), other apps closed, the old and the new build turn about three
times each, `python tools/fps_compare.py --before b1 b2 b3 --after a1 a2 a3 --noise b1 b2 b3`.

### Found on the way
- Seven's lightning impacts (`storm_strike_seven`, RoR2's pooled prefab) leave about 450 idle particle systems behind for the
  rest of the run. It is the same in the old build, so it is not from the rebuild.
- Infernus' ult filled the screen white for a moment in the new build's screenshots. Not yet compared with the old build.
- Fixed after the screenshots, not yet seen in game: beams and ribbons drew as flat pale rectangles (Source's rope textures run
  the other way from Unity's trails; the strip is now made in code); RoR2's own orange and blue impact prefabs still played over
  Abrams' and Seven's new effects (`effect` set to `none` on Shoulder Charge, Haymaker, Seismic Impact, Static Charge); blue sparks
  drew yellow with RoR2's spark material (new `Spark`).
