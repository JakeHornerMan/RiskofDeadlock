# Risk of Deadlock

Play Risk of Rain 2 as **Haze, Abrams, Seven or Infernus** from Deadlock, with their real models, animations, guns, abilities, icons and voices.

## You need Deadlock installed
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v0.11.0, 2026-10-23).
Nothing from Deadlock is in this download. The heroes are read from **your own Deadlock install** (free on Steam):

1. Install Deadlock on Steam (you never need to open it).
2. Install this mod with r2modman or the Thunderstore Mod Manager and start Risk of Rain 2.
3. **The first start takes a few minutes (up to about 6; Abrams alone takes about 4):** the loading screen waits while the heroes are read from Deadlock. It isn't stuck, and the bar keeps moving. Later starts are normal. It happens again only after a Deadlock update.

Every step is written to `BepInEx/LogOutput.log` (also in r2modman's log viewer): where it looked for Deadlock, where it found it, and each hero as it's read.

If Deadlock isn't found, the game starts without the heroes and tells you why. Deadlock in an unusual folder? Set *Deadlock folder* in the mod's config (the folder that holds `game/citadel/pak01_dir.vpk`).

## Hero pages
Every hero's abilities, numbers and measured damage next to their Risk of Rain 2 match:
- [Haze](https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p) (Commando) · [Abrams](https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG) (Loader) · [Seven](https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m) (Artificer) · [Infernus](https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF) (Engineer)
- [Balance overview](https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3)

## Features
- Four new survivors on the character select screen: Haze, Abrams, Seven and Infernus.
- Their models, animations, ability icons, portraits and sounds come from **your own Deadlock install**. Nothing from Deadlock is in this download.
- Each hero has Risk of Rain 2's four skills, made from their Deadlock kit, plus their ultimate as equipment:
  - **Left Click:** their Deadlock gun, with its magazine and reload.
  - **Right Click and Shift:** two Deadlock abilities.
  - **R:** a new move from a Deadlock item, melee attack or ability.
  - **Q (equipment):** their Deadlock ultimate. Every hero starts the run holding their own (120 s cooldown, about as strong as the Preon Accumulator). Swap it for another equipment and it stays on the ground; anyone, Commando included, can find the four ults in chests. The pickup is a little figure of the hero.
  - **Haze:** Twin SMGs, Sleep Dagger, Smoke Bomb, Phantom Strike (blink behind a target, three dagger slashes that cut everything around it); Q Bullet Dance. Passive: Fixation (every hit stacks it, crits stack 3, every 10 stacks bursts and splashes nearby enemies).
  - **Abrams:** Shotgun, Siphon Life (works like a Mired Urn), Shoulder Charge, Haymaker (locks onto an enemy, even one in the air, and rushes into a heavy punch); Q Seismic Impact (hover, aim the slam, crash down). Passive: Infernal Resilience.
  - **Seven:** Shock Rifle, Lightning Ball, Static Charge, Power Surge (his shots arc Tesla lightning); Q Thunderhead (a storm with a Warbanner). Passive: Capacitor (hits build Static that discharges).
  - **Infernus:** Flame Gun, Napalm, Flame Dash (a 5 s run that leaves pockets of fire behind), Torment Pulse (rings of fire); Q Concussive Combustion. Passive: hits build Afterburn and set enemies on fire.
- The heroes speak: Deadlock voice lines play at the right moments (read from your Deadlock install, like the other sounds).
- New ability effects made for this mod in Unity, in the style of Deadlock's; Infernus' fire uses Risk of Rain 2's own fire materials.
- Guns stay in the hero's hands and point where you aim.
- Balanced against Risk of Rain 2's survivors: Haze ~ Commando, Abrams ~ Loader, Seven ~ Artificer, Infernus ~ Engineer.
  - Health, regen and damage grow on every level-up, just like vanilla survivors.
- Scaled to Risk of Rain 2 with one shared factor (Haze is Commando's height), so Deadlock's size differences are kept. Abrams is the biggest.
- Co-op with up to 4 players: Risk of Rain 2's own lobby and Steam invites. Every player needs this mod (the same version) and their own Deadlock install; a player whose heroes didn't load (no Deadlock) is turned away with Risk of Rain 2's "mod mismatch" message instead of joining a broken game.

## About DeadlockExtract.exe
The mod comes with one small program, `DeadlockExtract.exe`. It reads the heroes from **your own Deadlock install** (opened
read-only) and saves them to `%LOCALAPPDATA%\DeadlockOfRain\cache` for the mod. It makes no network connections and changes
nothing else. It is a separate program because the Valve file reader it uses only runs on .NET 10, which Risk of Rain 2 can't
load. GitHub builds it in public from the source in [`extractor/`](https://github.com/JakeHornerMan/RiskofDeadlock/tree/main/extractor):
the [release page](https://github.com/JakeHornerMan/RiskofDeadlock/releases/tag/extractor-1.4.0) links the build log and lists
its SHA-256, the same file that is in this download.

## Credits
- Source and issues: https://github.com/JakeHornerMan/RiskofDeadlock. Also on Melty (melty.gg), where it was made.
- Risk of Rain 2 © Gearbox Software / Hopoo Games. Deadlock © Valve Corporation. This is an unofficial fan mod; it ships no game files.
- The bundled DeadlockExtract tool uses ValveResourceFormat, ValvePak, ValveKeyValue, SharpGLTF, SkiaSharp, NLayer and other MIT libraries (credits in `extract/THIRD-PARTY-NOTICES.txt`).
