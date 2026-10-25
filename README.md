# Deadlock of Rain

Play Risk of Rain 2 as **Haze, Abrams, Seven, Infernus or Mo & Krill** from Deadlock, using their real models, animations, guns, abilities, icons and sounds, read from your own Deadlock install.

**How to install:** [INSTALL.md](INSTALL.md) (r2modman or by hand), or one click on [Melty](https://melty.gg/m/risk-of-deadlock-2).

## Community
Join the **[Risk of Deadlock Discord](https://discord.gg/sNByetmcqc)**: FAQ, hero requests and news. The same button is on the game's title screen.

## Hero pages
Every hero's abilities, numbers and measured damage against their Risk of Rain 2 match, with our own in-game pictures:
- [Haze](https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p) (matched to Commando)
- [Abrams](https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG) (matched to Loader)
- [Seven](https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m) (matched to Artificer)
- [Infernus](https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF) (matched to Engineer)
- [Mo & Krill](https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa) (matched to MUL-T)
- [Viscous](https://claude.ai/artifact/JRFqAM4uskGuYVJEKA84FC) (matched to REX)
- [Rat King](https://claude.ai/artifact/Kw4rTyRrvXTb2eEyT9oTYa) (matched to Captain)
- [Wraith](https://claude.ai/artifact/2Bdxqd6iUwp89iEEPELgBb) (matched to Bandit; new, not released yet)
- [Balance overview](https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3): every hero next to every Risk of Rain 2 survivor

## Features
- Five new survivors on the character select screen: Haze, Abrams, Seven, Infernus and Mo & Krill.
- Their models, animations, ability icons, portraits and sounds come from **your own Deadlock install**. Nothing from Deadlock is in this download.
- Each hero has Risk of Rain 2's four skills, made from their Deadlock kit, plus their ultimate as equipment:
  - **Left Click:** their Deadlock gun, with its magazine and reload.
  - **Right Click and Shift:** two Deadlock abilities.
  - **R:** a new move from a Deadlock item, melee attack or ability.
  - **Q (equipment):** their Deadlock ultimate. Every hero starts the run holding their own (120 s cooldown, about as strong as the Preon Accumulator). Swap it for another equipment and it stays on the ground; anyone, Commando included, can find the five ults in chests. The pickup is a little figure of the hero.
  - **Haze:** Twin SMGs, Sleep Dagger, Smoke Bomb, Phantom Strike (blink behind a target, three dagger slashes that cut everything around it); Q Bullet Dance. Passive: Fixation (every hit stacks it, crits stack 3, every 10 stacks bursts and splashes nearby enemies).
  - **Abrams:** Shotgun, Siphon Life (works like a Mired Urn), Shoulder Charge, Haymaker (locks onto an enemy, even one in the air, and rushes into a heavy punch); Q Seismic Impact (hover, aim the slam, crash down). Passive: Infernal Resilience.
  - **Seven:** Shock Rifle, Lightning Ball, Static Charge, Power Surge (his shots arc Tesla lightning); Q Thunderhead (a storm with a Warbanner). Passive: Capacitor (hits build Static that discharges).
  - **Infernus:** Flame Gun, Napalm, Flame Dash (a 5 s run that leaves pockets of fire behind), Torment Pulse (rings of fire); Q Concussive Combustion. Passive: hits build Afterburn and set enemies on fire.
  - **Mo & Krill:** a huge, heavy pair. Yellow Canary (Krill's 4-pellet spread), Sand Blast (wind up, then a cone of sand that stops monsters attacking), Burrow (sink into the ground, travel under it, burst out in a big spin), Tag Team (R: Krill leaps from enemy to enemy while Mo goes into a Frenzy, punching monsters away); Q Dust Devil (a sand tornado that pulls enemies in; anyone holding it spins). Passive: Heavyweight, monsters can't knock him back.
- The heroes speak: Deadlock voice lines play at the right moments (read from your Deadlock install, like the other sounds).
- New ability effects made for this mod in Unity, in the style of Deadlock's; Infernus' fire uses Risk of Rain 2's own fire materials.
- Guns stay in the hero's hands and point where you aim.
- Balanced against Risk of Rain 2's survivors: Haze ~ Commando, Abrams ~ Loader, Seven ~ Artificer, Infernus ~ Engineer, Mo & Krill ~ MUL-T.
  - Health, regen and damage grow on every level-up, just like vanilla survivors.
- Scaled to Risk of Rain 2 with one shared factor (Haze is Commando's height), so Deadlock's size differences are kept. Abrams is the biggest.
- Co-op with up to 4 players: Risk of Rain 2's own lobby and Steam invites, or Melty's Join button. Every player needs this mod (the same version) and their own Deadlock install.

## Requirements
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v1.0.0, 2026-10-25).
- Risk of Rain 2 and Deadlock, both installed (for co-op, on every player's PC).
- On first start, the bundled DeadlockExtract tool reads the five heroes from your Deadlock folder into `%LOCALAPPDATA%\DeadlockOfRain\cache`. This takes a few minutes and happens once per Deadlock update.
- Melty does all of this for you: press Play.
- **Thunderstore** (r2modman, as "Risk of Deadlock", eight heroes since 1.0.0): install the mod, and the first start waits on the loading screen while the heroes are read from Deadlock (a window first asks which heroes to install; about 2 minutes for all eight, once per Deadlock update). If Deadlock is in an unusual folder, set *Deadlock folder* in the mod's config. **Step-by-step guide (r2modman or by hand, and what to do if it fails): [INSTALL.md](INSTALL.md).**

## How it is built
- `design/*.json`: the design sheets (heroes, stats, skills, equipment, buffs, animations, sounds, effects, hooks). They are the source of truth.
  - `tools/preflight.py` checks every cell and cross-reference.
  - `tools/gen.py` generates the C# rows and the extractor manifest from the sheets.
- `extractor/`: DeadlockExtract (.NET 10, ValveResourceFormat). It converts the heroes' models, animations, icons and sounds from the player's Deadlock VPKs into a local cache.
- `plugin/`: the BepInEx 5 plugin.
  - It registers the survivors through RoR2's own content-pack API (no R2API).
  - It builds the models from the cache and plays the Deadlock animation clips.
  - It runs the 20 skills and the 5 ult equipments, and the gun rig (guns held in the hands and aimed at the crosshair).
  - It plays Deadlock sounds through a small Windows audio mixer, because RoR2 turns Unity audio off.

## About DeadlockExtract.exe
The mod comes with one small program, `DeadlockExtract.exe`. It reads the heroes from **your own Deadlock install** (opened
read-only) and saves them to `%LOCALAPPDATA%\DeadlockOfRain\cache` for the mod. It makes no network connections and changes
nothing else. It is a separate program because the Valve file reader it uses only runs on .NET 10, which Risk of Rain 2 can't
load. GitHub builds it in public from the source in [`extractor/`](https://github.com/JakeHornerMan/RiskofDeadlock/tree/main/extractor):
the [release page](https://github.com/JakeHornerMan/RiskofDeadlock/releases/tag/extractor-1.4.0) links the build log and lists
its SHA-256, the same file that is in this download.

## Credits
- Made with Melty (melty.gg).
- Risk of Rain 2 © Gearbox Software / Hopoo Games. Deadlock © Valve Corporation. This is an unofficial fan mashup; it ships no game files.
- Third-party libraries (all bundled with credit, see THIRD-PARTY-NOTICES.txt):
  - ValveResourceFormat, ValvePak and ValveKeyValue (MIT, ValveResourceFormat contributors)
  - SharpGLTF (MIT), SkiaSharp (MIT), NLayer (MIT), KeyValues2 (MIT), K4os.Compression.LZ4 (MIT), ZstdSharp (MIT), TinyBCSharp (MIT), TinyEXR.NET (MIT), Vortice.SpirvCross (MIT), System.IO.Hashing (MIT)
  - BepInEx 5 / HarmonyX (installed by Melty)

## License
Deadlock of Rain code: **All rights reserved** (see LICENSE.txt). Remixing on Melty is allowed.
