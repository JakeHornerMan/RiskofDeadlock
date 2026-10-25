# Installing Risk of Deadlock (Thunderstore)

Play Risk of Rain 2 as **Haze, Abrams, Seven, Infernus, Mo & Krill, Viscous, Rat King and Billy** from Deadlock.
Stuck? Ask in the **[Risk of Deadlock Discord](https://discord.gg/sNByetmcqc)**.

## Before you start
- **Risk of Rain 2** on Steam (Windows PC).
- **Deadlock** installed on Steam (free). You never need to open it: the mod reads the heroes from its files.
- Nothing from Deadlock is in the download. On the first start a window asks **which heroes to install**, with the time each
  one takes (all eight: **about 2 minutes**). Tick them and press **Go**; the loading screen shows each one being read from
  your Deadlock: **it is not stuck.** Skipped heroes come back in that window next start.
- **Playing online? Every player needs the same heroes installed**, so install all of them to join any game.

## Option A: r2modman (easiest)
1. Install **r2modman** from <https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/> and open it.
2. Search **Risk of Rain 2** → **Select game** → **Steam** → **Select platform**.
3. **Create new** → name it (e.g. "Deadlock") → **Create**, then click your new profile and **Select**.
4. Go to **Online**, search **Risk of Deadlock**, click it → **Download** → **Download with dependencies**.
   r2modman installs BepInEx for you.
   - **Got the `.zip` instead?** r2modman does *not* add BepInEx for a zip, so first go to **Online**, search
     **BepInExPack** (by bbepis) → **Download** → **Download with dependencies**. Then **Settings** → **Profile** →
     **Import local mod** → **Select file** → pick the zip → **Import local mod**.
5. Click **Start modded** (top left). Pick your heroes in the window that opens and press **Go** (about 2 minutes for all eight, first time only).
6. The eight heroes are in character select. Have fun!

## Option B: by hand
1. Download **BepInExPack** from <https://thunderstore.io/c/riskofrain2/p/bbepis/BepInExPack/> (**Manual Download**) and unzip it.
2. Open your Risk of Rain 2 folder: Steam → right-click **Risk of Rain 2** → **Manage** → **Browse local files**.
3. From the unzipped BepInExPack, open the `BepInExPack` folder and copy **everything inside it** (`BepInEx`, `winhttp.dll`,
   `doorstop_config.ini`, ...) into the Risk of Rain 2 folder.
4. Download **Risk of Deadlock** (**Manual Download** on its Thunderstore page) and unzip it.
5. In the Risk of Rain 2 folder, open `BepInEx\plugins`, make a folder called `Risk_of_Deadlock`, and copy **everything inside the
   zip's `plugins` folder** into it (`DeadlockOfRain.dll`, the `extract` folder, `heroes.txt`, ...).
6. Start Risk of Rain 2 from Steam, pick your heroes in the window that opens and press **Go** (about 2 minutes for all eight, first time only).

To remove it again: delete `winhttp.dll` from the Risk of Rain 2 folder (that switches every BepInEx mod off).

## If it doesn't work
- **"Deadlock was not found"**: set the **Deadlock folder** in the mod's config to your Deadlock folder, e.g.
  `D:\SteamLibrary\steamapps\common\Deadlock` (find it with Steam → Deadlock → Manage → Browse local files).
  - r2modman: **Config editor** → `melty.deadlockofrain` → **Edit Config**.
  - By hand: `BepInEx\config\melty.deadlockofrain.cfg` in the Risk of Rain 2 folder (it appears after the first start).
- **"DeadlockExtract.exe is missing"** or reading never finishes: your antivirus removed or blocked the mod's Deadlock reader.
  Restore it from quarantine or allow it, then restart. It is built in public on GitHub
  ([release and checksum](https://github.com/JakeHornerMan/RiskofDeadlock/releases/tag/extractor-1.8.0)).
- **Still stuck?** Post your `LogOutput.log` in the Discord: r2modman → **Settings** → **Browse profile folder** →
  `BepInEx\LogOutput.log`; by hand, it's in the Risk of Rain 2 folder under `BepInEx`.

## Co-op
Up to 4 players. Everyone needs this mod (the same version) and their own Deadlock install.

## Links
- Discord: <https://discord.gg/sNByetmcqc>
- Hero pages (abilities, numbers, damage):
  [Haze](https://claude.ai/artifact/G1CjkUFpLFy65e2gBSRZ1p) ·
  [Abrams](https://claude.ai/artifact/JJgAwWD5kx3xTmEAHbNiiG) ·
  [Seven](https://claude.ai/artifact/WT8ZzxPro4AKvNXiwh1X8m) ·
  [Infernus](https://claude.ai/artifact/SQhdbatoDthiEEqj3EjMeF) ·
  [Mo & Krill](https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa) ·
  [Viscous](https://claude.ai/artifact/JRFqAM4uskGuYVJEKA84FC) ·
  [Rat King](https://claude.ai/artifact/Kw4rTyRrvXTb2eEyT9oTYa) ·
  [Billy](https://claude.ai/artifact/7m271DJLqzAvMZDrqU6QjL) ·
  [Balance overview](https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3)
- Also on Melty (one click, it sets everything up): <https://melty.gg/m/risk-of-deadlock-2>
