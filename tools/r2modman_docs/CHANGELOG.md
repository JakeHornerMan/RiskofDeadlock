# Changelog

One version line, one number up per build. Newest first.

## 1.6.2
- Built with Risk of Rain 2 build 25921972 (2026-11-10).
- Same thirteen heroes as 1.6.1.
- **Heroes left out while in development** are now shown blacked out with an amber `</>` dev badge, and hovering says they're
  in development (none are left out in this build).

## 1.6.1
- Built with Risk of Rain 2 build 25921972 (2026-11-10).
- **Victor and Paige are playable again:** both back in character select with all their files. Thirteen heroes.
  - **Victor rebalanced:** Jumpstart is a green lightning strike (every 8 s, +20 % speed); Pain Battery costs 1 Pain, 800 % with
    falloff to 30 m and knockback; Aura of Suffering is a +33 % damage zone (no damage or healing of its own, stays on, 12 s
    cooldown, +1 Pain every 5 s); Shocking Reanimation plays his zipline clip, other carriers stand in their idle.
  - **Paige's Captivating Read:** the sword is turned round so its edge leads, rises slowly out of the circle for a second and
    slams down, with a shockwave across the circle.
- Hero files sit beside the DLL in the `heroes` folder, as in 1.5.13.

## 1.5.13
- Built with Risk of Rain 2 build 25921972 (2026-11-10).
- **Victor and Paige are coming soon again:** greyed out in character select while they're finished. Their files aren't in this
  build. The other eleven heroes are the same as in 1.5.12.
- **The hero files sit in the mod's own folder now:** a `heroes` folder beside a small DLL (about 1 MB instead of 600 MB), the way
  other Risk of Rain 2 character mods ship their asset bundles. Nothing is unpacked to AppData any more; the old
  `%LOCALAPPDATA%\DeadlockOfRain\embedded` folder is removed on the first start, and removing the mod removes everything.

## 1.5.12
- Built with Risk of Rain 2 build 25921972 (2026-11-10).

## 1.5.11
- Built with Risk of Rain 2 build 25921972 (2026-11-09).
- **Victor and Paige are back:** both playable again in character select, with all their files in the build. Thirteen heroes.
  - **Victor:** as in 1.5.8 (Memento Mori on Left Click, Jumpstart, Pain Battery, Aura of Suffering; Q Shocking Reanimation).
  - **Paige:** as in 1.5.9 (Chapter's End, Bookwyrm, Captivating Read, Rallying Charge; Q The Green Knight; Plot Armor), all ghost green.
- Still no extractor: every hero's files are built into the DLL.

## 1.5.10
- Built with Risk of Rain 2 build 25921972 (2026-11-09).
- **Victor and Paige are coming soon:** greyed out in character select (hover them for "coming in a later update"). Their files
  aren't in this build, so the download is smaller. The other eleven heroes are the same as in 1.5.9.
- **No extractor at all:** this build never had to read Deadlock, and now it carries none of that code. No DeadlockExtract, no
  Deadlock search, no hero picker, no "Deadlock folder" setting. The loading screen says the heroes are built into the mod,
  with no Deadlock install needed.

## 1.5.9
- Built with Risk of Rain 2 build 25921972 (2026-11-06).
- **Paige joins: the thirteenth hero**, from my own Paige files. A bibliomancer matched to Engineer (130 health, 14 damage); every spirit she summons is see-through ghost green.
  - **Plot Armor:** +25 % damage while you're below 40 % health.
  - **Left Click, Chapter's End:** read glowing spirit pages off the book, 140 % each, piercing up to 3 enemies; 14 pages, 2.5 s reload.
  - **Right Click, Bookwyrm:** her ghostly wyvern flies 20 m through where you aim, breathing green fire on the floor: 180 % as it passes, and the floor burns for 3 s.
  - **Shift, Captivating Read:** a 7.5 m magic circle where you aim slows everything 36 %; 1.25 s later her giant spirit sword slams into its stone for 350 % and immobilizes them for 1 s.
  - **R, Rallying Charge:** a row of 5 spectral knights rides out from behind you and charges 40 m: 300 % and a 1 s stun to everything they trample; allies they pass heal 10 %.
  - **Q, The Green Knight:** one giant green knight charges 100 m, 6000 % rising to 12000 % the further it rides, with a 2 s stun; allies it passes heal 25 %. Anyone can carry it.

## 1.5.8
- Built with Risk of Rain 2 build 25921972 (2026-11-05).
- **Victor redesigned:**
  - **Left Click, Memento Mori:** the punches are gone; the spinning gun is back on Left Click, now 100 % a round.
  - **Right Click, Jumpstart:** shock yourself for 10 health (never below 1): +1 Pain and +30 % move speed for 5 s, every 4 s. It pauses the gun for a moment and keeps it spun up; your run speeds up with it.
  - **R, Aura of Suffering:** starts bigger, 9 m plus 0.5 m per Pain (14 m at 10).
  - **Pain:** one stack fades every 10 s while you have any (9 → 8 → 7 ...); the attack speed always matches the stacks you have left.
  - **Gun locked in his hands:** Memento Mori is held in both hands and always points where the camera aims.
  - **Green lightning:** his electricity is green now; Aura of Suffering pulses wavy green strikes out of him along the floor on every heartbeat, and a flying enemy inside it gets a lightning bolt every time the aura hurts it.

## 1.5.7
- Built with Risk of Rain 2 build 25921972 (2026-11-05).
- **Victor joins: the twelfth hero**, from my own Victor files. A pain-powered bruiser matched to REX (160 health, 20 armor).
  - **Pain:** every enemy hit you take adds a stack, up to 10, each for 10 s: +15 % attack speed each, added like Soldier's Syringe.
  - **Left Click, Heavy Melee:** heavy punches, alternating arms, 350 % in a 90° arc 4 m in front.
  - **Right Click, Memento Mori:** hold to fire; it spins up from 3 to 6 rounds a second, 70 % a round, 24-round magazine.
  - **Shift, Pain Battery:** spend 3 Pain on a bolt where you aim, 1000 % to it and everything within 3 m, slowed 40 %.
  - **R, Aura of Suffering:** a toggle. It hurts you (8 % on, 2 % a second) and gives Pain; enemies near you take 30 rising to 120 % a second and you heal 40 % of it. It grows with Pain and switches off at 10 % health.
  - **Q, Shocking Reanimation:** rise into a lightning strike that stuns everything within 30 m for 3 s, then 20 s with the power of 10 Ukuleles and 10 Tesla Coils. Victor also fills to 10 Pain. Anyone can carry it.

## 1.5.6
- Built with Risk of Rain 2 build 25921972 (2026-11-05).

## 1.5.5
- Built with Risk of Rain 2 build 25921972 (2026-11-04).
- New package icon: every hero as a class photo on curved steps.

## 1.5.4
- Built with Risk of Rain 2 build 25921972 (2026-11-02).
- Effects fixed alongside RoR2BepInExPack: it makes Unity's `Sprites/Default` shader lookup fail, so Viscous's goo, Mo & Krill's
  aim cone, Mina's bites and Nox Nostra marks, Wraith's card symbols and Pocket's box broke or showed bright pink. This is why
  effects looked worse here than on Melty. Same code as Melty 1.5.0.

## 1.5.3
- Built with Risk of Rain 2 build 25921972 (2026-11-02).
- Laid out like a Thunderstore release: `manifest.json`, `icon.png`, `README.md` and `CHANGELOG.md` at the top; the DLL with
  `LICENSE.txt`, `THIRD-PARTY-NOTICES.txt` and `built-with.txt` in `plugins/` (r2modman puts them in the mod's folder).
- README.md in the Thunderstore page's style, for all eleven heroes; INSTALL.txt and README.txt stay in the package.

## 1.5.2
- Built with Risk of Rain 2 build 25921972 (2026-11-02).
- INSTALL.txt (by hand and with r2modman, updating, removing everything, problems), README.txt and this changelog in the package.
- The package names **RoR2BepInExPack** (RiskofThunder 1.49.0) as a dependency: without its Newtonsoft.Json the mod does not
  load. Found by following INSTALL.txt on a cleared PC; by hand it must go in `plugins\RiskofThunder-RoR2BepInExPack`, because
  BepInExPack 5.4.2122 deletes a `plugins\RoR2BepInExPack` folder on every start.
- The r2modman install keeps its backup of r2modman's mod list outside the profile (r2modman copied it into the game folder);
  `r2modman_install.py --uninstall` removes every Risk of Deadlock from the profile.

## 1.5.1
- Built with Risk of Rain 2 build 25921972 (2026-11-02).
- First 1.5.x: the package is `JakeHornerMan-Risk_of_Deadlock`, versions 1.5.1, 1.5.2 ... (before: time-stamp versions by "Unknown").
- All eleven heroes: Haze, Abrams, Seven, Infernus, Mo & Krill, Viscous, Rat King, Billy, Wraith, Mina, Pocket.
  Checked file by file against the Melty build: the same.
- Mina's **Nox Nostra**: a cone of bats pours out of her and dives into the floor inside a crisp red ring (everyone sees it), a
  faint red haze fades as the time runs out; no beam, mist, sparks or flashes (far lighter on the PC).
- Mina's umbrella opens only while Jump is held, also after Nox Nostra, Sanguine Retreat and Mist Form.
- Pocket's **Frog Form**: the case lands, jolts three times and pops open, and you leap out as the frog (no giant frog); landing
  on top of an enemy slams too (damage and the stun ring), and the frog hops off it.
- Pocket's suitcase never sinks into the floor; his hair is near-black with a sheen, closer to Deadlock's.
- Seven's Static debuff icon (it was missing from the own-files build).
