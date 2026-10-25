# Risk of Deadlock

Play Risk of Rain 2 as **Haze, Abrams, Seven, Infernus, Mo & Krill, Viscous, Rat King or Billy** from Deadlock, with their real models, animations, guns, abilities, icons and voices.

## You need Deadlock installed
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v1.0.0, 2026-10-25).
Nothing from Deadlock is in this download. The heroes are read from **your own Deadlock install** (free on Steam):

1. Install **Deadlock** on Steam (you never need to open it).
2. Install this mod with **r2modman** (or the Thunderstore Mod Manager) and click **Start modded**.
3. **On the first start, pick your heroes:** a window lists them with the time each takes (all eight: about 2 minutes).
   Press **Go** and the loading screen shows each one being read from Deadlock. It isn't stuck. Later starts are normal;
   heroes you skipped come back in that window, and a Deadlock update re-reads them by itself.
   **Playing online? Every player needs the same heroes installed:** install all of them to join any game.

Installing by hand, or something went wrong? The step-by-step guide is
**[INSTALL.md](https://github.com/JakeHornerMan/RiskofDeadlock/blob/main/INSTALL.md)**. In short:
- **"Deadlock was not found":** set *Deadlock folder* in the mod's config (r2modman: Config editor → `melty.deadlockofrain`)
  to your Deadlock folder, e.g. `D:\SteamLibrary\steamapps\common\Deadlock` (Steam → Deadlock → Manage → Browse local files).
- **"DeadlockExtract.exe is missing"** or reading never ends: your antivirus removed or blocked the mod's Deadlock reader
  (see *About DeadlockExtract.exe* below). Restore or allow it, then restart.
- Every step is written to `BepInEx/LogOutput.log` (r2modman: Settings → Browse profile folder). Post it in the Discord.

## Community
Join the **[Risk of Deadlock Discord](https://discord.gg/sNByetmcqc)**: help, FAQ, hero requests and news. The same button is on the game's title screen.

## Hero pages
Every hero's abilities, numbers and measured damage next to their Risk of Rain 2 match:
- [Haze](https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p) (Commando) · [Abrams](https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG) (Loader) · [Seven](https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m) (Artificer) · [Infernus](https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF) (Engineer)
- [Mo & Krill](https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa) (MUL-T) · [Viscous](https://claude.ai/artifact/JRFqAM4uskGuYVJEKA84FC) (REX) · [Rat King](https://claude.ai/artifact/Kw4rTyRrvXTb2eEyT9oTYa) (Captain) · [Billy](https://claude.ai/artifact/7m271DJLqzAvMZDrqU6QjL) (Mercenary)
- [Balance overview](https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3)

## Features
- Eight new survivors, each with Risk of Rain 2's four skills built from their Deadlock kit, plus their ultimate as equipment:
  - **Left Click:** their Deadlock gun, with its magazine and reload (the rounds show above the skill bar).
  - **Right Click and Shift:** two Deadlock abilities.
  - **R:** a fourth move from their Deadlock kit, items or melee.
  - **Q (equipment):** their Deadlock ultimate. Every hero starts the run holding their own (120 s cooldown, about as strong as the Preon Accumulator). Swap it away and it stays on the ground; anyone, Commando included, can find the ults in chests. The pickup is a little figure of the hero.
- **Haze:** Twin SMGs, Sleep Dagger, Smoke Bomb, Phantom Strike (blink behind a target, three slashes that cut everything around it); Q Bullet Dance. Passive: Fixation stacks on every hit and bursts.
- **Abrams:** Shotgun, Siphon Life, Shoulder Charge, Haymaker (locks on and rushes into a heavy punch); Q Seismic Impact (hover, aim, crash down).
- **Seven:** Shock Rifle, a yellow Lightning Ball that slows over enemies, Static Charge, and Storm Cloud (fly for 10 s inside a storm that shocks the 3 nearest enemies); Q Thunderhead.
- **Infernus:** Flame Gun, Napalm, Flame Dash (a run that leaves fire behind), Torment Pulse; Q Concussive Combustion. Hits build Afterburn.
- **Mo & Krill:** Krill's shotgun, Sand Blast, Burrow (travel underground, burst out spinning), Tag Team; Q Dust Devil, a sand tornado.
- **Viscous:** goo blobs, Splatter, The Cube, Puddle Punch; his puddles top up his ammo; Q Goo Ball.
- **Rat King:** Sovereign's Decree spreadshot, a hopping Scrap Grenade, Royal Pestments, Rule, Ratannia! (a banner leap), a cape, a tail and a crowd of Deadlock's rats; Q Rat Swarm.
- **Billy:** Biquette the machine gun, a 3-hit Bat combo that juggles enemies in the air, Bashdown, Rising Ram (chains haul everyone into the air); his air jump is a goring dodge and he never takes fall damage; Q Chain Gang, up to 20 chains.
- Deadlock voice lines play at the right moments, read from your Deadlock install like the other sounds.
- Each hero is balanced against a Risk of Rain 2 survivor (the hero pages show the numbers) and levels up like one.
- If Deadlock ever changes one hero so it can't be read, only that hero shows a red "!" tile (hover it for why); the others still work.
- Co-op with up to 4 players: Risk of Rain 2's own lobby and Steam invites. Every player needs this mod (the same version) and their own Deadlock install. Melty players on the same version can join too.

## About DeadlockExtract.exe
The mod comes with one small program, `DeadlockExtract.exe`. It reads the heroes from **your own Deadlock install** (opened
read-only) and saves them to `%LOCALAPPDATA%\DeadlockOfRain\cache` for the mod. It makes no network connections and changes
nothing else. It is a separate program because the Valve file reader it uses only runs on .NET 10, which Risk of Rain 2 can't
load. GitHub builds it in public from the source in [`extractor/`](https://github.com/JakeHornerMan/RiskofDeadlock/tree/main/extractor):
the [release page](https://github.com/JakeHornerMan/RiskofDeadlock/releases/tag/extractor-1.8.0) links the build log and lists
its SHA-256, the same file that is in this download. Some antivirus programs distrust any new unsigned program; if yours removes
it, restore it from quarantine.

## Credits
- Source and issues: https://github.com/JakeHornerMan/RiskofDeadlock. Also on Melty (melty.gg), where it was made.
- Risk of Rain 2 © Gearbox Software / Hopoo Games. Deadlock © Valve Corporation. This is an unofficial fan mod; it ships no game files.
- The bundled DeadlockExtract tool uses ValveResourceFormat, ValvePak, ValveKeyValue, SharpGLTF, SkiaSharp, NLayer and other MIT libraries (credits in `extract/THIRD-PARTY-NOTICES.txt`).
