# Changelog

One version line, one number up per build. Newest first.

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
