# Infernus: VFX rebuild

Code: `plugin/Runtime/VfxInfernus.cs` (new layers), called from his existing effects in `plugin/Runtime/Vfx.cs`.
Recipes read: `charactersFinal/infernusFiles/deadlock/particles/abilities/inferno/*` and `weapon_fx/inferno/*` (275 files).
Method and tools: `README.md` in this folder.

## The headline: the colour was right, the layers were missing
Infernus was already RoR2's fire in Deadlock's oranges, which the creator chose, so **his flames stay**. What Deadlock's
recipes have and ours lacked are the layers round the fire: glowing flecks for embers instead of plain sparks, scorched and
glowing ground under every fire, a ragged muzzle flash, Napalm as black-brown tar, and a proper blast for the bomb. His fire
rings and ground fires were also his dearest effects, so their flame particle counts were cut.

| Use | Deadlock colour |
|---|---|
| Embers | `#FFA23F` > `#FF711E` > `#4F0900` |
| Fire | `#FFB986`, `#FF8E68`, `#E02C00` |
| Afterburn ring | `#DC143C`, `#8B0000` |
| Napalm | tar `#0F0906`, `#5E2700`; flash `#FF7525` |
| Concussive Combustion | shock `#FBD9B4` > `#FFBB5C` > `#C05A16`; dust `#D7B38A`; scorch `#620F0F` |
| Flame Gun muzzle | `#FF4F23`, `#FF8B24` |

## Move by move
| Move | Deadlock (recipe) | Before | Now |
|---|---|---|---|
| **Flame Gun** (M1) | `inferno_muzzle_flash`: a ragged red-orange flash, hot gas, smoke; `inferno_afterburn_explode` when Afterburn catches | RoR2's muzzle flash; a small flame on the enemy | A ragged red-orange flash and a ball of hot gas per shot (two quads), smoke on some. When Afterburn catches: a crimson burst ring, a hot flare, 20 embers. |
| **Napalm** (M2) | `inferno_napalm_explode`: black-brown tar flung out low, an orange flash | an orange splash | A tar stain on the ground that stays 3 s, an orange flare, 12 blobs of brown-black tar thrown out low, 14 dark red drops; the fire on top is unchanged. |
| **Flame Dash** (Shift) | `inferno_flame_dash*`: fire over scorched ground, embers | flames along his path | Under every burning patch: a scorch mark, a glow that breathes, and embers rising. Fewer flame particles per patch. |
| **Torment Pulse** (R) | a Deadlock item, no hero recipe: styled with his fire rings | a ring of flame | Two flat rings racing out over the ground under the flame ring, and one flash of light. Fewer flame particles in the ring. |
| **Concussive Combustion** (Q) | `inferno_fire_bomb_explode`: a pale shock ring, the flash, streaks, embers, tan dust, a scorch | RoR2's big explosion | Added to it: two shock rings over the ground, a dark red scorch that stays 4 s with a glowing spiral cooling on it, a flash and a star, 22 hot streaks, 30 embers, 16 puffs of tan dust rolling away. |

## Sheet and code changes
- No new effect rows: the layers hang off his existing `infernus_*` effects. Shared texture rows only (`fx_fleks`, `fx_muzzle`,
  `fx_ring*`, `fx_puff`, `fx_cloud`, `fx_glow*`, `fx_flare`, `fx_star`).
- `Vfx.cs`: `FireRing` and `GroundFire` emit fewer flame particles; each calls its new layer function.

## Results (2026-11-13)
Before/after page (pick Infernus in the dropdown): https://claude.ai/artifact/BvL6LjctELNqDEYv2PE7Ue#infernus

Seen in game on the current build: his fire is unchanged; the new layers show under it. Concussive Combustion in the **old**
build blacks the whole screen out for a moment (RoR2's explosion prefab with the camera inside it); the new blast (rings, dust,
scorch) does not.

Single player, 6-second runs (first use inside the measurement). Average frame, and the worst frame of a move's first two seconds:

| Move | Average, old | Average, new (first run) | Average, new (after the fix) | First use, old / new |
|---|---|---|---|---|
| Standing still | 15.3 ms | 15.9 | 15.2 | 37 / 32 ms |
| Flame Gun | 17.3 | 17.5 | 17.3 | 85 / 90 |
| Napalm | 16.2 | 20.0 | 16.6 | 38 / 41 |
| Flame Dash | 17.2 | 19.7 | 18.3 | 30 / 29 |
| Torment Pulse | 19.6 | 19.8 | 20.1 | 31 / 38 |
| Concussive Combustion | 20.0 | 19.9 | 20.5 | 30 / 34 |

The first run showed Napalm and Flame Dash 2.5 to 4 ms a frame dearer although they drew half the particles: every splash and
every burning patch laid its own tar stain, scorch and glow, and those big quads stacked on the same ground. Now one stain per
burst of splashes and a scorch and glow under about every third patch (`Every`). Napalm is level with the old build again;
Flame Dash is about 1 ms dearer. Spikes are level; the gun's first shot is the same hitch every hero's gun has, in both builds.
Peak particles in Flame Dash: 1580 before, 964 now.

### Fire as flames, Napalm on the floor (2026-11-14)
The creator: his fire "is represented through these circular type shape, can we please have something that more represents
fire", and Napalm should burn on the floor. Every call that asked for one of RoR2's fire materials now draws Deadlock's flame
tongues (`VfxFire.cs`, texture row `fx_fire`), and Napalm's fire, tar, ring and embers drop to the ground under the hit
(`Floor`). The lingering spiral after Concussive Combustion and one of the two flat rings under his fire rings were removed.

Seen in game twice. First pass: the shapes read as flames but were taller than the hero, pale and see-through (sizes meant for
round puffs). Made smaller (70 % of the asked size, 0.45 to 1.7 m), solider and less white; second pass: solid orange tongues
on Napalm's patch, along Flame Dash and around Torment Pulse, Napalm's fire on the ground. No tongue looked upside down.

One timed run, taken before the flames were made smaller (old build / flames): Napalm 16.2 / 20.9 ms, Flame Dash 17.2 / 21.4,
Torment Pulse 19.6 / 21.0, Concussive Combustion 20.0 / 21.0, first uses 30-38 / 34-40 ms. That run slowed as it went (standing
still 16.0 ms at the start, 20.9 at the end), so how much of the 4 ms is the flames is not known. Not timed again after the trim.

Not done: the normal in-game test (`bash tools/run_test.sh infernus`) on this branch; the creator's sign-off on the look.
