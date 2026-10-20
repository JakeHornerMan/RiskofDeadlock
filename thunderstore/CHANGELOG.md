# Changelog

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
