# Deadlock of Rain

Play Risk of Rain 2 as **Haze, Abrams, Seven or Infernus** from Deadlock, using their real models, animations, guns, abilities, icons and sounds, read from your own Deadlock install.

## Features
- Four new survivors on the character select screen: Haze, Abrams, Seven and Infernus.
- Their models, animations, ability icons, portraits and sounds come from **your own Deadlock install**. Nothing from Deadlock is in this download.
- Each hero has Risk of Rain 2's four skills, made from their Deadlock kit:
  - **Left Click:** their Deadlock gun, with its magazine and reload.
  - **Right Click and Shift:** two Deadlock abilities.
  - **R:** their Deadlock ultimate. It starts **locked** every run and charges as you deal damage and get kills; the R icon shows the charge %.
  - **Haze:** Twin SMGs, Sleep Dagger, Smoke Bomb, Bullet Dance. Passive: every hit stacks Fixation.
  - **Abrams:** Shotgun, Siphon Life, Shoulder Charge, Seismic Impact.
  - **Seven:** Shock Rifle, Lightning Ball, Static Charge, Storm Cloud.
  - **Infernus:** Flame Gun, Catalyst, Flame Dash, Concussive Combustion. Passive: hits build Afterburn and set enemies on fire.
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
- `design/*.json`: the design sheets (heroes, stats, skills, buffs, animations, sounds, effects, hooks). They are the source of truth.
  - `tools/preflight.py` checks every cell and cross-reference.
  - `tools/gen.py` generates the C# rows and the extractor manifest from the sheets.
- `extractor/`: DeadlockExtract (.NET 10, ValveResourceFormat). It converts the heroes' models, animations, icons and sounds from the player's Deadlock VPKs into a local cache.
- `plugin/`: the BepInEx 5 plugin.
  - It registers the survivors through RoR2's own content-pack API (no R2API).
  - It builds the models from the cache and plays the Deadlock animation clips.
  - It runs the 16 skills, the gun rig (guns held in the hands and aimed at the crosshair), the ult charge meter and its R-icon label.
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
