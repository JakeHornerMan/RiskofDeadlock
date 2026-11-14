# Victor: VFX rebuild (DLL build only)

Code: `plugin/Runtime/VfxVictor.cs`. Recipes read: `charactersFinal/victorFiles/deadlock/particles/abilities/frank/*` and
his `weapon_fx` (Deadlock's code name for Victor is "frank"). Method and tools: `README.md` in this folder.

## The headline: the green was right; small touches
Deadlock's Victor is green electricity (`#9DD37D`, `#9DDD79`, `#B9EE65`, `#8EF851`, with the odd blue arc `#4B7EFF`), and
ours is too (`victorBolt`, `victorArc`, `victorField`; the wavy green strikes and the floor-only aura were the creator's own
calls on 2026-11-05). His red (`victorRed`, `victorBlood`) is ours, for pain and blood; Deadlock has none, and it is kept.

| Use | Deadlock colour |
|---|---|
| Arcs on his arm and shoulder, Jumpstart | `#9DD37D`, `#B9EE65`, `#5AFF65` |
| Shock (our Pain Battery) | `#9DDD79`, `#8EF851`, `#5CF75F`, deep `#165E35` |
| Aura of Suffering | `#BCD48F`, `#99BB63`, `#689E13` on dark ground `#101210` |
| Muzzle | warm `#FBBF76`, `#FFDC7B`, with pale green `#B9E1B7` |

## What changed
| Move | Before | Now |
|---|---|---|
| **Memento Mori** (M1) | a red glow, pink smoke, red sparks and a light per shot | Deadlock's warm ragged flash with a pale green wisp in it (two quads), the red sparks, a light only while few effects are alive. |
| **Jumpstart, Pain Battery, Aura of Suffering, Shocking Reanimation** | rings as 64-point lines, still glows and smoke as particle bursts | The same shapes; rings are Deadlock's shockwave ring on one quad, still glows one glow quad with a star core, smoke bursts cartoon clouds. |

Not done: recolouring his greens to Deadlock's softer yellow-green. The creator chose the present green.

## Sheet and code changes
- No sheet changes.

## Aura of Suffering, closer to Deadlock's (2026-11-14)
The creator: "maybe for victor his aura of suffering should look more like their ability in deadlock". Deadlock's
`frank_painaura` is a pale, sickly yellow-green (`#BCD48F`, `#99BB63`, `#DCCA8D`), not a vivid green: the ground inside the
field is projected dark (`#101210`), a ring of streaks flows round its edge with lightning arcs along it, pale smoke comes off
the edge and off his body, arcs crawl over his arm and shoulder, and the ground cracks under each enemy it holds. Ours now
has those layers: a dark ground disc, two pale rings turning opposite ways on the edge, Deadlock's arcs along the edge and on
him, pale smoke on the edge and on him, a burst of pale rays when it switches on, a pale crack ring under marked enemies, and
the dome in the pale green. His wavy green strikes on every heartbeat (the creator's own, 2026-11-05) are kept.
Seen in game: the dark ground, the pale edge and smoke read; the edge arcs are subtle. This was worked from Deadlock's effect
files, not from screenshots of Deadlock.

## Results (2026-11-13)
Before/after page (pick the character in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#victor

Seen in game on the current build: run as the DLL build with the kit's textures added to its hero folder. The gun has Deadlock's warm flash; Aura of Suffering was reworked after the creator's note (below).

Single player, one 6-second run per build (first use inside the measurement; the worst frame of a move's first two seconds):

| Move | Average frame, old / new | First use, old / new |
|---|---|---|
| Standing still | 16.3 / 15.7 ms | 34 / 36 ms |
| Memento Mori | 17.7 / 17.0 ms | 87 / 95 ms |
| Jumpstart | 16.5 / 15.9 ms | 38 / 34 ms |
| Pain Battery | 30.3 / 15.7 ms | 55 / 34 ms |
| Aura of Suffering | 30.8 / 16.0 ms | 54 / 29 ms |
| Shocking Reanimation | 32.5 / 16.6 ms | 60 / 30 ms |

The old-build run was disturbed from Pain Battery on (the PC was busy: 30 ms a frame even standing still), so only the first three rows compare fairly; those are level. The new build's numbers were taken before the aura rework; the reworked aura is not measured.

Not done: the normal in-game test (`bash tools/run_test.sh victor`) on this branch; the creator's sign-off on the look.
