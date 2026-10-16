# Deadlock of Rain

Play Risk of Rain 2 as **Haze, Abrams, Seven or Infernus** from Deadlock, using their real models, animations, guns, abilities, icons and sounds, read from your own Deadlock install.

## Features
- Four new survivors on the character select screen: Haze, Abrams, Seven and Infernus.
- Their models, animations, ability icons, portraits and sounds come from **your own Deadlock install**. Nothing from Deadlock is in this download.
- Each hero has Risk of Rain 2's four skills, made from their Deadlock kit, plus their ultimate as equipment:
  - **Left Click:** their Deadlock gun, with its magazine and reload.
  - **Right Click and Shift:** two Deadlock abilities.
  - **R:** a new move from a Deadlock item, melee attack or ability.
  - **Q (equipment):** their Deadlock ultimate. Every hero starts the run holding their own (120 s cooldown, about as strong as the Preon Accumulator). Swap it for another equipment and it stays on the ground; anyone, Commando included, can find the four ults in chests. The pickup is a little figure of the hero.
  - **Haze:** Twin SMGs, Sleep Dagger, Smoke Bomb, Phantom Strike (blink behind a target, three dagger slashes); Q Bullet Dance. Passive: Fixation (every hit stacks it, crits stack 3, every 20 stacks bursts).
  - **Abrams:** Shotgun, Siphon Life (works like a Mired Urn), Shoulder Charge, Haymaker (a lunging heavy punch); Q Seismic Impact (hover, aim the slam, crash down). Passive: Infernal Resilience.
  - **Seven:** Shock Rifle, Lightning Ball, Static Charge, Power Surge (his shots arc Tesla lightning); Q Thunderhead (a storm with a Warbanner). Passive: Capacitor (hits build Static that discharges).
  - **Infernus:** Flame Gun, Napalm, Flame Dash (hold to keep going), Torment Pulse (rings of fire); Q Concussive Combustion. Passive: hits build Afterburn and set enemies on fire.
- New ability effects made for this mod in Unity, in the style of Deadlock's; Infernus' fire uses Risk of Rain 2's own fire materials.
- Guns stay in the hero's hands and point where you aim.
- Balanced against Risk of Rain 2's survivors: Haze ~ Commando, Abrams ~ Loader, Seven ~ Artificer, Infernus ~ Engineer.
  - Health, regen and damage grow on every level-up, just like vanilla survivors.
- Scaled to Risk of Rain 2 with one shared factor (Haze is Commando's height), so Deadlock's size differences are kept. Abrams is the biggest.
- Single player.

## Requirements
- Risk of Rain 2 and Deadlock, both installed.
- On first start, the bundled DeadlockExtract tool reads the four heroes from your Deadlock folder into `%LOCALAPPDATA%\DeadlockOfRain\cache`. This takes a few minutes and happens once per Deadlock update.
- Melty does all of this for you: press Play.

## How it is built
- `design/*.json`: the design sheets (heroes, stats, skills, equipment, buffs, animations, sounds, effects, hooks). They are the source of truth.
  - `tools/preflight.py` checks every cell and cross-reference.
  - `tools/gen.py` generates the C# rows and the extractor manifest from the sheets.
- `extractor/`: DeadlockExtract (.NET 10, ValveResourceFormat). It converts the heroes' models, animations, icons and sounds from the player's Deadlock VPKs into a local cache.
- `plugin/`: the BepInEx 5 plugin.
  - It registers the survivors through RoR2's own content-pack API (no R2API).
  - It builds the models from the cache and plays the Deadlock animation clips.
  - It runs the 16 skills and the 4 ult equipments, and the gun rig (guns held in the hands and aimed at the crosshair).
  - It plays Deadlock sounds through a small Windows audio mixer, because RoR2 turns Unity audio off.

## Credits
- Made with Melty (melty.gg).
- Risk of Rain 2 © Gearbox Software / Hopoo Games. Deadlock © Valve Corporation. This is an unofficial fan mashup; it ships no game files.
- Third-party libraries (all bundled with credit, see THIRD-PARTY-NOTICES.txt):
  - ValveResourceFormat, ValvePak and ValveKeyValue (MIT, ValveResourceFormat contributors)
  - SharpGLTF (MIT), SkiaSharp (MIT), NLayer (MIT), KeyValues2 (MIT), K4os.Compression.LZ4 (MIT), ZstdSharp (MIT), TinyBCSharp (MIT), TinyEXR.NET (MIT), Vortice.SpirvCross (MIT), System.IO.Hashing (MIT)
  - BepInEx 5 / HarmonyX (installed by Melty)

## License
Deadlock of Rain code: **All rights reserved** (see LICENSE.txt). Remixing on Melty is allowed.
