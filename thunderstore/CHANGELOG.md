# Changelog

## 0.11.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6819 (v0.11.0, 2026-10-23).
- The loading screen now says clearly what is happening: a big "Deadlock of Rain" banner names each hero while it is built from your own Deadlock install ("Building your Deadlock heroes: Abrams (2 of 7)"), with a timer and "The game is not stuck", and a list shows each hero as it gets ready.
- Melty's setup screen says it too (Deadlock reader 1.7.0): it explains that it is reading the heroes' models, animations, icons and sounds from your Deadlock install, that this happens once and again after each Deadlock update, and names each hero as it goes.
- Because the reader is new, the heroes are read from Deadlock once more on the first start of this version (a few minutes).
- Checked against Deadlock's update of 23 October (build 6819): every hero reads and plays.

## 0.10.0
- Built with Risk of Rain 2 build 25921972 and Deadlock build 6817 (v0.10.0, 2026-10-22).
- Rat King joins the Melty build as its seventh playable hero: Sovereign's Decree, the hopping Scrap Grenade, Royal Pestments, the banner leap Rule, Ratannia! and the Rat Swarm ult (not part of this store release yet; the four heroes here are unchanged).
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
- A seventh hero for the Melty build, Rat King (not part of this store release yet; the four heroes here are unchanged).
- Heroes and effects can use Deadlock models with their own animations, and swinging capes (extractor 1.6.0).
- Clips that left a hero's root turned no longer carry that turn into the next clip.
- Effect models keep Deadlock's vertex colours and material tints (extractor 1.6.1).
- Every hero shows its magazine (rounds/magazine) just above the M1 icon of the skill bar.

## 0.8.0
- A sixth hero for the Melty build (not part of this store release yet; the four heroes here are unchanged).
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
