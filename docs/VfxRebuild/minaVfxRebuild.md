# Mina: VFX rebuild

Code: `plugin/Runtime/VfxMina.cs`. Recipes read: `charactersFinal/minaFiles/deadlock/particles/abilities/vampirebat/*` and
her `weapon_fx` (Deadlock's code name for Mina is "vampirebat"). Method and tools: `README.md` in this folder.

## The headline: the colours and the bats were right; lighter drawing
Mina's crimson, rose and near-black match Deadlock's recipes (`#DC1923`, `#FF525E`, `#F37777`, `#240202`; ours `#E01233`,
`#FF526B`, `#38050D`), and she already draws Deadlock's Love Bites mark, marker symbol and Rake cone. Her bats must keep
reading as bats, and Nox Nostra was already cut back once for fps. So nothing is recoloured and no shapes change.

What changed is how the plain pieces are drawn:
- five still glows (muzzle flash, bite flashes, the glow where she reforms) are single glow quads instead of small particle
  systems;
- her rings are Deadlock's shockwave ring on one quad instead of 64-point lines;
- her smoke bursts are Deadlock's cartoon cloud, 60 % as many, swelling as they thin;
- flash lights give way when a great many effects are alive.

| Use | Deadlock colour |
|---|---|
| Rake, bats, Sanguine Retreat | `#DC1923`, `#FF525E`, `#F37777`, `#940016`, `#240202` |
| Nox Nostra | `#CF1515`, `#E5111C`, with violet `#8500FA` in the blast |
| Muzzle | `#FF2033`, `#E55058` |

Not done: the violet (`#8500FA`, `#7A24EC`) Deadlock mixes into Nox Nostra's blast and the bat tracers. It would change a
look the creator already tuned; it is noted here as an option.

## Sheet and code changes
- No sheet changes.

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#mina

Seen in game on the current build: her bats, marker, bite mark and crimson look the same as the old build, as intended.

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 16.5 / 16.3 ms | 35 / 34 ms |
| New Money | 17.9 / 17.9 ms | 85 / 91 ms |
| Rake | 17.0 / 16.5 ms | 34 / 32 ms |
| Sanguine Retreat | 16.6 / 16.4 ms | 42 / 41 ms |
| Nox Nostra | 19.8 / 17.8 ms | 34 / 35 ms |
| Mist Form | 20.2 / 19.8 ms | 31 / 31 ms |

Level with the old build (the first old-build run was thrown away: the PC was busy and it idled at 35 ms a frame).

Not done: the normal in-game test (`bash tools/run_test.sh mina`) on this branch; the creator's sign-off on the look.
