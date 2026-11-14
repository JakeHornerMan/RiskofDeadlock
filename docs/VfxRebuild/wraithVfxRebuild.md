# Wraith: VFX rebuild

Code: `plugin/Runtime/VfxWraith.cs`. Recipes read: `charactersFinal/wraithFiles/deadlock/particles/abilities/wraith/*` and
her `weapon_fx`. Method and tools: `README.md` in this folder.

## The headline: already Deadlock's colours and textures; made lighter and crisper
Wraith was built late, with the creator, as purple smoke magic, and she already draws Deadlock's own card suits, sigil, ring
and eye textures. Her violets match the recipes (`#A47FCD`, `#AA5BFF`, `#9A60FF`, `#CE72FF`; ours `#8F4FD9`, `#CC9EFF`,
`#DB6BFF`). So nothing is recoloured. What changed: her gun flash is Deadlock's, her rings and flashes draw with Deadlock's
ring and glow sprites, and her thickest smoke and glow emitters were thinned, because many puffs sat on top of each other.

| Use | Deadlock colour |
|---|---|
| Card Trick, Full Auto, Project Mind | `#A47FCD`, `#9A60FF`, `#CE72FF`, `#671EF0` |
| Telekinesis | `#AA5BFF`, `#863AF8`, `#B93FFC` |
| Muzzle | warm `#FF8D22`, `#FEB576`; shoot glow `#FF00FF` / `#FFC9F7` |

## Move by move
| Move | Before | Now |
|---|---|---|
| **The Rake** (M1) | a lilac glow, a curl of smoke and sparks on every shot | Deadlock's warm ragged flash inside a pink-violet glow (two quads, at most one per 0.06 s), smoke on some shots. |
| **Card Trick** (M2) | 60 smoke, 40 glow, 25 core glow, 30 sparks a second behind each card | 34 smoke and 22 glow a second, the core glow is one quad; suits and sparks unchanged. |
| **Project Mind** (Shift) | travel: 90 + 40 smoke, 40 + 50 glow, 50 sparks a second | 48 + 22 smoke, one glow quad + 26 glow, 30 sparks a second; the aim ring's smoke 24 a second (was 40). |
| **Full Auto** (R) | bursts of smoke and a still glow | The smoke bursts are cartoon clouds in her violet, the glow one quad with a star core. |
| **Telekinesis** (Q) | loop: 45 smoke and 30 glow a second; wave: 70 smoke and 40 glow | 26 and 16; 38 and 22. Rings and flashes through the shared ring and glow. |

All the thinned emitters now also follow the effects quality (`N()`), so they give way when many effects are alive.

## Sheet and code changes
- No sheet changes.

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#wraith

Seen in game on the current build: the first look was worse than the old build: the shared change that draws smoke bursts as cartoon clouds turned her purple smoke into flat pink blobs. That change now applies to grey and tan dust only, so her smoke is RoR2's again. Looked at after the fix (2026-11-14): her smoke, suits and rings match the old build again, with fewer particles and lights.

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 18.2 / 17.3 ms | 42 / 40 ms |
| The Rake | 20.2 / 19.2 ms | 47 / 78 ms |
| Card Trick | 19.8 / 19.1 ms | 47 / 43 ms |
| Project Mind | 18.5 / 21.1 ms | 46 / 42 ms |
| Full Auto | 23.3 / 23.3 ms | 33 / 33 ms |
| Telekinesis | 23.9 / 24.0 ms | 42 / 47 ms |

After the fix: level with the old build except Project Mind, 2.6 ms a frame dearer (4.3 before the fix). Fewer live particles and far fewer lights on every move. Her gun's first shot is worse in the new build (47 to 78 ms): the new muzzle flash, to look at next.

Not done: the normal in-game test (`bash tools/run_test.sh wraith`) on this branch; the creator's sign-off on the look.
