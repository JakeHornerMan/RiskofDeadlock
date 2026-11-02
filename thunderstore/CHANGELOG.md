# Changelog

## 1.5.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6839 (v1.5.0, 2026-11-02).
- **Pocket joins**, the eleventh hero, matched to Bandit: **The Black Sheep** (a shotgun firing a hexagon of 7 pellets), **Barrage** (lock onto the enemy under your crosshair and his briefcase throws 4 bursts at it while you keep shooting; each burst stuns and makes you stronger for a while), **Flying Cloak** (his own coat flies where you aim, hitting everything it passes; press again to teleport to it), **Enchanter's Satchel** (hide in his floating suitcase, untouchable, draining and healing from every enemy around, then burst out) and the **Frog Form** ult (the suitcase pops open and you leap out as a frog for 8 s: every landing, on the ground or on an enemy, hits and stuns everything around; Jump for a high leap). One jump plus an air dash.
- Every hero is now playable: no more locked "coming soon" tile.
- Mina's **Nox Nostra** rebuilt: a cone of bats pours down onto the spot under your crosshair, with a ring on the ground showing the area to everyone.
- Mina's umbrella now opens only while you hold Jump.
- Players on 1.5.0 play together; earlier versions can't join them (new hero content).

## 1.2.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6821 (v1.2.0, 2026-10-30).
- **Mina joins**, the tenth hero, matched to Huntress (150 health): **New Money** (a pistol, 90 % a bullet, 12 rounds; every hit leaves a Love Bite), **Rake** (an umbrella sweep through a 180° arc, 1000 %, heals for every enemy hit), **Sanguine Retreat** (burst into bats and fly 15 m where you look, untouchable; press again for a second hop), **Nox Nostra** (hang in the air for 5 s and pour a blood-red stream of bats onto the ground under your crosshair) and the **Mist Form** ult (8 s as a flock of furious bats, untouchable, flying freely). Hold Jump to float under her umbrella; **Love Bites** stack on enemies and bring her moves back sooner.
- The "coming soon" tile in character select now hides the next hero.
- The game's start is now written step by step to the log (and to start-trail.txt), with how Steam started it, so a game that closes right after starting can be traced. A copy of Risk of Rain 2 outside a Steam library gets a warning in the log: Steam may close or restart it.
- Players on 1.2.0 play together; earlier versions can't join them (new hero content).

## 1.1.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6821 (v1.1.0, 2026-10-27).
- **Wraith joins**, the ninth hero, matched to Bandit: The Rake tommy gun (72 % a bullet, 52 rounds), **Card Trick** (a card that follows your crosshair; each draws Bleed, a Crit, a Stun or Burn, and the trick splashes onto every enemy within 7 m; holds 4 cards, every 5 Rake hits bring the next one 1 s sooner), **Project Mind** (mark a spot up to 60 m away, on the ground or in the air, and fly there as purple smoke and cards; no fall damage from the drop after it), **Full Auto** (a double magazine, 104 rounds, fired 50 % faster) and the **Telekinesis** ult (lift every enemy within 30 m and throw them where you aim).
- One more locked "coming soon" hero tile in character select.
- Fixed another freeze at start: the Deadlock sounds (about 1,400 files) were read before the game could go on, which on a slow drive or a busy PC could make it stop responding. They now load in the background in a couple of seconds and take half the memory.
- Works with Deadlock's update of 2026-10-27 (build 6821): every hero reads.
- Players on 1.1.0 play together; earlier versions can't join them (new hero content).

## 1.0.2
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v1.0.2, 2026-10-26).
- Fixed a freeze on the loading screen: the mod no longer holds the game still while it waits for Risk of Rain 2's own files, so a slower drive can't make the game stop responding before the heroes are built.
- The loading log now says when the hero loading starts and how long it waited, so a stuck start can be traced.
- One more locked "coming soon" hero tile in character select.

## 1.0.1
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v1.0.1, 2026-10-25).
- Setting up is about 15 times faster: all eight heroes are read from your Deadlock in about 2 minutes (it took up to half an hour). Abrams alone went from 9 minutes to 14 seconds (Deadlock reader 1.8.0).
- The title screen's two buttons show their names, "Join the Risk of Deadlock Discord" and "Deadlock Hero Info" (with all eight heroes beside it); they were blank.
- Melty and Thunderstore players can play together whatever their version, as long as they have the same heroes.

## 1.0.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v1.0.0, 2026-10-25).
- Thunderstore gets all eight heroes: Haze, Abrams, Seven and Infernus, now joined by Mo & Krill (MUL-T's match), Viscous (REX's), Rat King (Captain's) and Billy (Mercenary's). Every hero's abilities are described on its hero page.
- Billy, the newest: Biquette the machine gun, a 3-hit Bat combo that dodges onto enemies in the air and juggles them, Bashdown, Rising Ram (chains haul everyone around into the air) and the Chain Gang ult (up to 20 chains of 1500 %, one per enemy in sight, leftovers to bosses). The G.O.A.T. passive turns his air jump into a goring dodge, and he never takes fall damage.
- **Pick your heroes:** on the first start a window lists every hero with the time it takes to read; tick the ones you want and press Go. Installed heroes are marked, skipped ones come back next start, and the window stays away once every hero is installed. Playing online needs the same heroes on every PC: install all of them to join any game.
- **The first start is about 15 times faster:** all eight heroes in about 2 minutes (it took 16 to 34 minutes). Abrams alone went from 9 minutes to 14 seconds (Deadlock reader 1.8.0).
- The title screen's two buttons show their names again, "Join the Risk of Deadlock Discord" and "Deadlock Hero Info" (with all eight heroes beside it); they were blank.
- First start fixed for more PCs: the "Deadlock folder" setting accepts a path ending in \ and the game or game\citadel folder too (a trailing \ used to break the reader); when an antivirus has removed DeadlockExtract.exe the game says so instead of asking you to install Deadlock; and if the reader ever stops responding for 10 minutes the loading screen stops waiting and tells you what to do.
- The Deadlock reader is version 1.8.0, built in public by GitHub: effect models, capes and Deadlock's colours read from your install like the heroes.
- A big "Join the Discord" button at the bottom right of the title screen opens the mod's Discord community (FAQ, hero requests, news), and a "Hero Sheets & Damage" button above it opens every hero's ability and damage pages.

## 0.11.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v0.11.0, 2026-10-23).
- The loading screen now says clearly what is happening: a big "Risk of Deadlock" banner names each hero while it is built from your own Deadlock install ("Building your Deadlock heroes: Abrams (2 of 7)"), with a timer and "The game is not stuck", and a list shows each hero as it gets ready.
- Melty's setup screen says it too (Deadlock reader 1.7.0): it explains that it is reading the heroes' models, animations, icons and sounds from your Deadlock install, that this happens once and again after each Deadlock update, and names each hero as it goes.
- Because the reader is new, the heroes are read from Deadlock once more on the first start of this version (a few minutes).
- Checked against Deadlock's update of 23 October (build 6819): every hero reads and plays.

## 0.10.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6817 (v0.10.0, 2026-10-22).
- Rat King joins as the seventh playable hero: Sovereign's Decree, the hopping Scrap Grenade, Royal Pestments, the banner leap Rule, Ratannia! and the Rat Swarm ult.
- Billy takes Rat King's place as the locked "coming soon" tile.
- Co-op: a friend's Scrap Grenade now always hits on the host too (it could fly over the enemy it hit on their screen).
- Checked against Deadlock's update of 22 October (build 6817): every hero reads and plays.

## 0.9.1
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6813 (v0.9.1, 2026-10-22).
- Seven is redesigned: a yellow Lightning Ball that slows to half speed over enemies, a retuned Static Charge, and Storm Cloud on R in place of Power Surge (fly for 10 s inside a storm that shocks the 3 nearest enemies). Thunderhead has its own icon.
- Abrams' Shoulder Charge no longer throws an error when it hits an enemy the moment it spawns.
- Co-op: a friend's Concussive Combustion (Infernus' Q) always explodes on the host; on a laggy connection it could fizzle.
- Playing together through Melty: your game is open to friends from the title screen, so Melty's Join always has an address; a Singleplayer run stays solo. A friend who joins before you start waits in your lobby instead of being told it failed, and Melty never hands out a lobby you've left.
- Rat King's "coming soon" tile is a black silhouette.

## 0.9.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6807 (v0.9.0, 2026-10-21).
- A seventh hero, Rat King.
- Heroes and effects can use Deadlock models with their own animations, and swinging capes (extractor 1.6.0).
- Clips that left a hero's root turned no longer carry that turn into the next clip.
- Effect models keep Deadlock's vertex colours and material tints (extractor 1.6.1).
- Every hero shows its magazine (rounds/magazine) just above the M1 icon of the skill bar.

## 0.8.0
- A sixth hero, Viscous.
- Moves of a future hero can use Deadlock's own effect models, read from your install like the heroes (extractor 1.5.0).

## 0.7.4
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6795 (v0.7.4, 2026-10-20).
- The game no longer crashes when you quit after playing a run. It was Risk of Rain 2 and BepInEx together (it happened without this mod too); the mod now stops it.

## 0.7.3
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6795 (v0.7.3, 2026-10-19).
- Closing the game shuts the mod's Deadlock sound down properly first.

## 0.7.2
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6795 (v0.7.2, 2026-10-19).
- A hero that can't be read from your Deadlock no longer disappears: its spot in character select shows a red "!" (it can't be picked), and hovering it says why. Every other hero still works.
- Heroes whose model Deadlock moved to another folder are found anyway (the mod now asks Deadlock's own hero list where each model is).
- Every problem message names the Risk of Rain 2 and Deadlock versions the mod was built with, and the ones on your PC.
- The setup shows each hero as it is read (ready or skipped) and a summary at the end.

## 0.7.1
- Works with Deadlock's update of 19 October 2026 (the heroes couldn't be read after it).
- If Deadlock ever changes one hero so it can't be read, only that hero is missing (a message on the title screen says which); the others still work.

## 0.7.0
- First release: Haze, Abrams, Seven and Infernus from Deadlock as Risk of Rain 2 survivors, each with their Deadlock ultimate as a Q equipment.
- The mod finds your Deadlock install and reads the heroes on the first start (the loading screen waits a few minutes, once per Deadlock update).
- Co-op with up to 4 players.
