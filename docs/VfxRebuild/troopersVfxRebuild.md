# Troopers: VFX rebuild (DLL build only)

Code: `plugin/Enemies/` (`Enemies.cs`, `EnemyStates.cs`, `Medic.cs`). Recipes read:
`charactersFinal/npc/troopers/deadlock/particles/*`. Method and tools: `README.md` in this folder.

## Status: soul orb done; the rest compared, not rebuilt
The Troopers' effects are being changed on `main` at the same time as this branch (soul drops and death smoke, uncommitted
there on 2026-11-11). Editing `plugin/Enemies/` here would collide with that work when the branch is merged, so on this
branch the Troopers' own code is **untouched**. They get one thing through shared code: the Medic's heal ring
(`Vfx.Ring`) is now Deadlock's shockwave ring on one quad instead of a 64-point line.

## What Deadlock does (for the pass after main's enemy work lands)
| Effect | Deadlock (recipe) | Ours now | To do |
|---|---|---|---|
| Rifle muzzle | `trooper_muzzle_flash`: orange `#FF8B24`, `#FECF78`, dark `#8C3713` | RoR2's Lemurian fire muzzle flash | a ragged orange flash quad and a hot core (the heroes' `fx_muzzle` pattern), throttled |
| Bullet | `trooper_bullet_travel`: orange `#F27210` > `#32170A` with a red core | the sheet's projectile | an orange ribbon cooling to brown |
| Melee hit | `tier1boss_melee*`: orange `#FFAA79`, `#F9861C`, dust `#AC8E71` | RoR2's slash impact | a spiked burst and cartoon dust |
| Medic heal | `medic_target`: teal `#1CD9B9` crescents and a beam | a green ring, a bolt, crosses | recolour to Deadlock's teal, a soft beam instead of a jagged bolt |
| Death | `neutral_trooper_death`: teal `#37FFEE`, `#17957B` motes over dark flecks | main's new death fade and smoke | teal motes and flecks as the body fades |
| Head glow | `trooper_ambient_head`: orange `#F38E14` (one team), blue `#18A3D6` (the other) | none | a small glow quad on the head bone |
| Jump | `npc_trooper_jump`: cartoon dust, a blue light | none | cartoon dust on take-off and landing |

## The soul orb, matched to Deadlock's (2026-11-14)
The creator: "when you get to the trooper i really want you to look at the soul orb and make sure they are pretty accurate to
how they look in deadlock". Main's finished Troopers work was merged into this branch first (2026-11-13), so `plugin/Enemies`
could be touched. Deadlock's recipe for the orb a dead trooper leaves is `particles/environment/spirit_orb_dropped_*`
(dumped read-only from the game's own archive to `build/vfx-rebuild/recipes/soulorb`):

| Layer | Deadlock | Ours before | Ours now |
|---|---|---|---|
| Orb | `generic_soul_orb` core `#2DECA9` on `#002C32`, the voronoi shell, a soft glow | the same parts, colour `#3CF0C3`, but the glow (six orb widths, full strength) and the shell's emission (2.4) burned it out to a white blob | colour `#2DECA9`; the glow a faint halo (3.6 widths, about a third as strong); shell emission 0.9, so the dark veins show |
| Rays | six at once, then seven a second, streaming slowly out (`_rays`, `_rays_cont`) | none | the same counts, as stretched glow particles on the orb |
| Trail | a short green plasma wisp, `#16C786`, half a second (`_trail`); the enemy team's orb has a grey one | a dark smoke trail (from the creator's picture) | the dark trail kept, with the green wisp added |
| Appearing | a ring and a glow (`_start`) | none | a ring snapping out and a soft glow |
| Claimed | flares `#11D9C8` and three rays (`_claim`, `_claim_ray`), motes `#30DBBE` > `#025C41` with a few pale blue (`_endcap`) | 14 glow particles | a star, six flares, three rays thrown out, the scatter of motes |
| Faded unclaimed | the motes | it shrank away | it shrinks away with half the motes |

Code: `plugin/Runtime/VfxEnemies.cs` (`SoulOrbAmbient`, `SoulOrbAppear`, `SoulOrbClaim`, `SoulOrbEnd`), called from
`plugin/Enemies/SoulOrb.cs`; `design/enemyDrops.json` `orbColor`. Seen in game three times
(`ENEMIES=melee STAGE=golemplains bash tools/run_test.sh haze` on this branch's DLL build, 14/14 checks each time): the dark
core, the pale teal shell with dark holes and the green rays read. The shell is still paler than Deadlock's teal. Worked from
Deadlock's effect files, not from screenshots of Deadlock. The claim burst was not caught in a screenshot.

The other Trooper effects in the table above (muzzle, bullet, melee hit, Medic heal, death motes, head glow, jump dust) are
still not rebuilt.

