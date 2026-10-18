# 08: New-hero playbook (bring one more Deadlock hero into Risk of Rain 2 in one go)

> **Status (2026-10-18, v0.6.3):** development of the first four heroes (Haze, Abrams, Seven, Infernus) is **done**
> (creator). The next job is adding a new hero from the creator's outline, "one shot": everything from the model,
> sounds and voice to the Unity-built effects, tests, balance and pages, in a single session.
> This page is the order of work. The details live in doc 01 §10 (Deadlock half), doc 02 §11 (RoR2 half) and the
> sheets' own `about` text; this page says what to do, in what order, and what "done" means.

"Into Deadlock" in the creator's words means *from* Deadlock: a Deadlock hero becomes a RoR2 survivor. Putting anything
into Deadlock itself is out of scope (online-only game, anti-cheat; doc 01 §1).

---

## 0. Before anything: the intake (one round of questions)

The creator outlines the hero ("I want X, with these moves, this feel"). Read it closely; free-text answers change the
plan. Then **one** AskUserQuestion round (recommended option first) for whatever the outline leaves open:

| Ask | Why | Default if they don't care |
|---|---|---|
| Which Deadlock ability goes on M2, Shift, R; which is the Q ult item | 4 skills + the ult equipment (doc 02 §7.1) | Deadlock's ability 1 → M2, 2 → Shift, a fitting Deadlock item or melee → R, ult → Q |
| The RoR2 match (whose stats and DPS he is balanced against) | `heroes.json roR2Analog`, the ±500 %/s rule (doc 05) | closest play style: gunner → Commando, bruiser → Loader, caster → Artificer, area/DoT → Engineer, others: any stored survivor in `docs/dps-library.json` |
| Any move changed from Deadlock (hold vs tap, range, aimed vs instant) | the creator has strong views (doc 07: "for 5 second he run", Napalm aim) | Deadlock's behaviour, **one tap, no holding** (no RoR2 dash needs holding) |
| The look of each move (colours, "rocketing forward", rings, trails) | every move gets a custom Unity effect | the hero's Deadlock colours, a clear wind-up and a readable hit |

Say up front how many game launches the build needs (§6) and why. The creator dislikes unexplained launches.

---

## 1. Deadlock lookup (no game launch; doc 01 §8 and §10)
1. Decompile `scripts/heroes.vdata` and `scripts/abilities.vdata`: the hero's codename, `m_strModelName`, ability ids,
   numbers (damage, cooldowns, durations, radii) and `m_strAbilityImage`.
2. Export the model to a scratch `.glb`: mesh names (`meshInclude`), clip names (**from the glb, not the VPK listing**),
   bones (gun hand, muzzle, a separate gun pivot like Abrams').
3. Decompile `soundevents/hero/<codename>.vsndevts` (abilities, gun, footsteps) and
   `soundevents/vo/generated_vo_hero_<codename>.vsndevts` (voice groups). Copy event names exactly.
4. Add the hero to the dev listings (`anim_names.json` script with **every** hero's glb).
5. Measure `sourceHeight` (doc 01 §6.3).

## 2. The sheets (the source of truth; `python tools/gen.py` must say `PREFLIGHT CLEAN`)
| Sheet | Rows for the new hero |
|---|---|
| `heroes.json` | 1 row: model, meshes, images, `sourceHeight`, `heightScale = sourceHeight / 2.413`, `widthScale`, colour, `roR2Analog`, subtitle, passive, lore |
| `stats.json` | 1 row, from the RoR2 match's stats |
| `skills.json` | **5 rows** (`primary`, `secondary`, `utility`, `special`, `equipment`), each with an `expect` list; reuse a `kind` when one fits (doc 02 §7.2 table) |
| `equipment.json` | 1 row for the Q ult (120 s, ≥ 6000 % per enemy; the pickup figure is built from the model automatically) |
| `animations.json` | gun rig (`gunArms`, `gunBone`, `gunHand`, `muzzle`, `aimPose`), every clip column, **`ragdollBones`** (the 12 shared Deadlock bones work if the skeleton matches; check in the glb) and `ragdollThickness` |
| `sounds.json` | 1 row per skill (§3) |
| `voice.json` | select, unselect, one per skill (Q always speaks), multikill 4/7/10, idle (§3) |
| `buffs.json` | if the kit needs buffs/debuffs (stacking build-ups, slows, a trigger at N stacks) |
| `effects.json` | 1 `custom` row per visual (§4), plus RoR2 prefab rows (`impact`, `tracer`, `orb`, `ward`...) if reused |
| `hooks.json` | only if a new Harmony patch is added (keep it in sync with doc 02 §9) |

## 3. Sound (SFX and voice)
- **Find events** in the decompiled vsndevts. Ability sounds are usually `<Hero>.<Ability>.Cast/.Lp/.Hit/.End`; item
  moves use `Mods.*`. Another hero's event is fine when the hero has none (Combustion's tick is Rat King's).
- **Columns:** `cast`, `loop` (looped while the move lasts), `hit`, `end`, `reload`, `castLayer`/`hitLayer` (a heavier
  sound played with cast/hit), `tick`/`tickFinal` (a countdown once a second through a fuse), `gain` (row volume).
  Volumes come from Deadlock (`volume` is dB); `gain` 1 = as Deadlock mixes it.
- **Check a sound before choosing it** (no game needed): export its `.vsnd_c` with the VRF CLI (it writes `.mp3`),
  `ffmpeg -i x.mp3 x.wav`, and print its loudness over time (MODLOG and doc 07 v0.6.2 show the snippet). This tells you
  its length and shape: a 3 s cast may already be a build-up (Combustion's was).
- **New events need a cache rebuild:** extractor `--manifest extractor/manifest.json --force` (~8 min, no game).
- **Voice:** groups are `sounds/vo/<group>_NN` (`<codename>/<codename>_select`); the four heroes use `select`,
  `unselect`, `use_power1`/`2`/`4` (or `use_<item>` for an item move), `killstreak_start`/`_mid`/`_high` (4/7/10 kills),
  `hunt` and `happy` (idle). Never write the lines out on pages or docs. Test check `voice-lines`.

## 4. Visual effects (built in Unity, in code)
Effects are made at runtime in `plugin/Runtime/Vfx.cs` from Unity particle systems, trails, lines and lights tinted
with RoR2's own materials. Nothing is imported.
1. **Sheet:** an `effects.json` row `{id: "<hero>_<move>", kind: "custom", addressable: "none", notes: what it looks like}`
   and the skill row's `vfx` = that id.
2. **Code:** a `case "<id>": return <Hero><Move>(moment, at, radius, follow...)` in `Vfx.Play`, and a function with one
   `case` per **moment** the state plays: `cast` (start), `loop` (follows the body while the move lasts; return the root
   so the state can `Vfx.Stop` it), `hit`, `end`, `aim` (a reticle the state moves), `muzzle`, or your own.
3. **Building blocks** (all in `Vfx.cs`): `Root` (an anchor that follows a transform and dies after `life`),
   `Particles` (rate or burst; `world`, `flat`, `ring`, `upward`, `stretch`, `gravity`), `Burst`, `Trail`, `Line`,
   `Circle`, `Ring` (expanding ring), `Bolt`, `Flash`, `Light` (+ `Flicker`), `Shake`, `GroundFire`, `FireRing`,
   `Slash`, and behaviours `Spin`, `Pulse`, `Shrink`, `RingGrow`, `DomeSweep`, `GrowLight`, `Crackle`, `Mover`.
4. **Materials** (`effects.json` kind `material`, RoR2 Addressables, tinted with `Mat(id, colour)`): `mat_glow`,
   `mat_tracer`, `mat_lightning`, `mat_ring`, `mat_fire`, `mat_fire_large`, `mat_gas_fire`, `mat_fire_puff`, `mat_smoke`,
   `mat_dust`, `mat_spark`, `mat_area` (ground dome).
5. **Rules learned the hard way:**
   - Ground markers: use `mat_area` domes or particles from a circle's edge, never flat line circles (gotcha 15).
   - Readability from the camera behind the hero: thin trails (≤ 0.5 m), glows on the front; a 1.5 m white trail hid the
     whole screen (Abrams' charge, v0.6.3).
   - Facing effects: `Root` follows position only; turn the root in the state each frame
     (`fx.transform.rotation = Quaternion.LookRotation(dir)`, as `DashCharge` does).
   - **Co-op:** play visuals and sounds **in the state** (it runs on every machine). Code that runs on the server only
     (damage, pockets, zones) plays its visuals with `Relay.*`, never from the state too, or players see them twice.
6. **Check by eye:** the test saves `<skill>.png`, `_2`, `_3` in `%LOCALAPPDATA%\DeadlockOfRain\devshots`; read them.

## 5. Code (only for a new mechanic)
- A new `[Kind("...")]` state in `plugin/States/` (inherit `DlrState`; `UltState` for the Q ult, which must work on
  **any** body). Movement kinds go in the `Body` list in `Survivors.MachineFor`.
- Damage, buffs and stuns on the server only; movement and aim on the owner (`isAuthority`); anything the owner picks
  (a target, a point) goes in `OnSerialize` (doc 02 §7.7).
- New `expect` effect kinds need code in `MeltyTest.Effects.Missing` and preflight's allowed list.
- Hard-coded per-hero bits to check (doc 02 §11): Abrams' looping secondary clip, Haze's alternating muzzles, Smoke
  Bomb's sprint rule, cloak tied to buff `smoke`. The hero count is no longer hard-coded (v0.6.3).

## 6. Test, measure, publish pages (the only game launches)
Tell the creator the count first: normally **3 launches** (test, bench, portrait), plus 1 co-op check.
1. `bash tools/run_test.sh <hero>`: **only the new hero**. Every check `pass`, `1/1 heroes`. Read the devshots (scale
   next to Commando, gun in hand, aim, every effect). Fix and re-run that hero only.
2. `bash tools/run_bench.sh 30 <hero>`, then bump `VERSION` in `tools/dps_library.py` and
   `python tools/dps_library.py --add build/bench_results.txt`. Compare with the match in `docs/dps-library.json`
   (±500 %/s). **Never re-bench vanilla survivors.** Report; retune only with the creator's say.
3. `PORTRAITS=1 bash tools/run_bench.sh 0 <hero>`, then `python tools/portraits.py`: our own cut-out renders of the hero,
   its match and its ult figure for the page (never Deadlock art files).
4. `bash tools/run_coop_test.sh <hero>`: the joined player's moves work and the relay shows server-side effects.
5. After any launch: `bash tools/lab_restore.sh`, then copy `winhttp.dll.melty-off` back to `winhttp.dll` (the creator
   plays from Steam). Never launch the game for the creator's own playtest.
6. **Pages:** in `tools/kit_pages.py` add the match to `VANILLA` if new (its kit names), a `CHANGES` entry and a notes
   entry; run it; **publish the new hero page as a new artifact**, put its link in `LINKS` and doc 06, regenerate, and
   republish all pages (each page's nav lists every hero).

## 7. Docs (same commit as the work)
- Doc 06: the hero's section (moves, numbers, DPS), the sounds table rows, the voice summary.
- Doc 05: the bench result; doc 02: new kinds, vfx table rows, hooks; doc 01: the hero's paths and the hero table.
- Doc 03 §9 decision row (the creator's answers quoted); doc 07 entry; doc 00 §1/§9; MODLOG; CLAUDE.md's hero list.
- Release (doc 03 §6, only when asked): recipe `<hero>-plays` (drop `four-slots`; the recipe is at the 12-check cap),
  `test.minutes` +2, a forced re-extract for players' old caches.

## 8. Definition of done
- [ ] `PREFLIGHT CLEAN`, build clean, cache rebuilt with the new content.
- [ ] `run_test.sh <hero>`: every check passes (including `ragdoll`, `sound-events`, `voice-lines`), `1/1 heroes`.
- [ ] Every move has its Deadlock sound and a custom effect, checked in the devshots.
- [ ] Bench stored in the library; verdict against the match reported to the creator.
- [ ] Co-op check passed (or skipped with the creator's OK, recorded as open).
- [ ] Hero page published and linked; all pages regenerated.
- [ ] Docs, doc 07 entry, doc 00 refreshed; committed; game closed, `winhttp.dll` back; the creator told it's ready.
