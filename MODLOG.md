# MODLOG: Deadlock of Rain

## Versions
- Risk of Rain 2 1.5.0#1210 (Unity 2021.3.45, Mono), BepInEx 5.4.23.5 (Melty installs it)
- Deadlock ClientVersion 6817 (build 26143760, 2026-10-22 evening; no clip, mesh or sound change for our heroes); 6812 (build 26118388, update of 2026-10-22; checked by the daily watch: no sheet cell, clip or mesh changed); 6795 (2026-10-19), 6774 before. Source 2
- ValveResourceFormat 20.1.7467 (.NET 10; 20.0.6980 until v0.7.1), ilspycmd 11.1 (RoR2.dll decompiled to ~/ror2-decomp, never shipped)

## Route
- RoR2 is the primary game: a BepInEx plugin built on RoR2's own ContentManager API (no R2API).
- Deadlock is the companion game. DeadlockExtract (a VRF-based exe Melty runs as recipe.setup) converts the 4 heroes'
  models, animation clips, icons and sounds from the player's VPKs into %LOCALAPPDATA%\DeadlockOfRain\cache.
- Design sheets in design/ are the source of truth. tools/preflight.py checks every cell; tools/gen.py generates the code.

## Lab
- RoR2's BepInEx is switched off by the Melty app (winhttp.dll -> winhttp.dll.melty-off); tools/dev_run.sh re-enables
  it for each dev launch.
- Left4Rain (another Melty mashup) was moved to lab-backup/ during early testing and has since been put back (it loads
  fine next to ours). If it is ever moved again, RESTORE: move lab-backup/Left4Rain back to
  BepInEx/plugins and delete our winhttp.dll copy (leave winhttp.dll.melty-off as Melty had it).
- Test mode: %LOCALAPPDATA%\Melty\test\deadlock-of-rain.json (a local copy written by hand for dev; Melty writes its own
  during test_release). Dev screenshots: create %LOCALAPPDATA%\DeadlockOfRain\devshots.

## Balance
- 0.8.0 (2026-10-20, not released): Viscous, matched to REX. 0.8.0b after the creator's first play: Puddle Punch 800 % and a cel-goo fist, Goo Ball 15 s with 70 % grip, Splatter through enemies onto the floor, The Cube frozen in a goo block, goo head in a glass helmet, landing shake 0.2. 0.8.0c: base damage 15 +3, Cube burst 600 %. Bench 2236 vs REX 1738 (inside, just; +25 % real damage from the base damage).
- 0.8.0d (2026-10-20): puddles 10 s + 20 s shrink and +20 rounds per new puddle (puddleAmmo, HeroRuntime.GiveAmmo), The Cube leaves a 10 m puddle (extra = viscous_splatter), BigSplash for Splatter and the Cube, base damage back to 12, Goo Ball 75 % and grip -0.5, voiceGain 1.2. Bench 2422 vs REX 1738 (above, +684).
- 0.8.0e (2026-10-20): puddles 3 s + 5 s shrink; a puddle is a 20-round supply that tops the magazine up while he stands in it (HeroRuntime.TopUp, never above the magazine); Splatter 150 %; jump power 20. Bench 2253 vs REX 1738 (15 above the band).
- 0.8.0f (2026-10-21): Puddle Punch leaves a 6.4 m puddle; puddles 20 % smaller (Splatter 6.4 m, Cube 8 m). Bench 1958 vs REX 1738 (inside).
- Daily watch 2026-10-22 (not released): Deadlock 6812 needs no change; Fx.Stun skips enemies whose SetStateOnHurt hasn't run Start (players' 0.9.0 logs: NullReferenceException from Shoulder Charge); tools/dev_listings.py.
- 0.9.1b (2026-10-22, not released): Storm Cloud the 3 nearest per shock; Thunderhead icon surge_of_power (preflight: one icon per skill); gestures in model space (ModelSpaceGesture, all heroes); steadyClips de-spins Seven's storm_cloud_intro/scalar hover; Thunderhead casts ability_power_surge; equipment test accepts 60-120 s. Bench 1759 vs Artificer 4013 (far below).
- 0.9.1a (2026-10-21, not released): Seven redesign part 1. Lightning Ball half speed over enemies (moveSpeedMult 0.5), yellow with a clear 10 m sphere; Static Charge 225 %; R Storm Cloud (storm_flight) replaces Power Surge; effects tint column (yellow electric tracer, ball core, impact); upper-body cast clips (cache rebuilt). Bench 2827 vs Artificer 4013 (below).
- 0.9.0f / Melty 0.9.0 (2026-10-21): Goo Ball 100 s (preflight allows equipment 60-120 s); Melty release without Rat King (package.py --melty, coming-soon.txt, greyed portrait tile). Published untested at the creator's request.
- 0.9.0e (2026-10-21): puddle rounds come before the magazine (HeroRuntime.Extra/CanFire/Rounds/Spend, DlrPuddle.Supply/Draw), gone on stepping out; counter 20+5/20. Not tested or benched (creator: don't launch).
- 0.9.0d (2026-10-21): every Viscous puddle on the floor (DlrPuddle.Floor, 1000 m down; it looked 8 m, so Puddle Punch / The Cube high in the air left it floating), no puddle over a pit; an ammo counter (rounds/clip) above the M1 icon for every hero (SkillIcon.LateUpdate postfix, Runtime/AmmoCounter.cs). No balance change.
- 0.7.0 (2026-10-19, not released): Mo & Krill, matched to MUL-T: 250 +75 health, 25 armor, 5 m/s. Yellow Canary 4 x 40 % fixed pattern; Sand Blast 450 % cone, Sanded = DisableAllSkills 2 s; Burrow 4 s x2 speed +100 armor, pop-out 3 x 100 %; Dust Devil 16 x 50 %, pull 16 m; Tag Team 20 x 1500 % hops. Bench 1620 vs MUL-T 1450 (inside).
- 0.7.0b (2026-10-19, not released, after the creator's play-check): Tag Team to R (30 s, numbers unchanged, no target = no cooldown); Dust Devil to the Q ult (8 s, 32 x 200 % = 6400 %, any carrier spins); Burrow 8 s, pop-out 3 x 500 %, sinks and rises; Frenzy while Krill is away (x2 speed, punch 600 %, knockback 20 m/s, red aura); Sand Blast 0.4 s wind-up with a ground cone, shove 8 m/s; acceleration 80. Bench 3252 vs MUL-T 1450 (far above, the creator's numbers).
- 0.4.2: each hero within ±500 %/s of its RoR2 match (means over bench runs; doc 05 §3.0).
- 0.5.1: playtest feel pass (Haymaker lock-on rush, Phantom Strike 5 m area, bouncing 10 m Lightning Ball); benched once and reported, not retuned.
- 0.6.3: Abrams Shoulder Charge vfx abrams_charge (visual). run_test.sh takes heroes (test only the changed ones); run_bench.sh clears a leftover test-mode file. Bench Infernus 1149 vs Engineer 1611 (inside, just).
- 0.6.3: Flame Gun 50 -> 75 % per bullet (targetDps 319 -> 478). Flame Dash: one tap runs the full 5 s (minDuration 0); RoR2's FireTrail DamageTrail replaced by DlrFirePocket pockets (radius 4.5 -> 3.2, every 3.2 m, 5 s, +3 Afterburn per 0.5 s, no direct damage, effect none, buffStacks 3).
- 0.6.2: Concussive Combustion ticks once a second through its 3 s fuse (Rat King's scrap-grenade timer; the last tick is its final timer, cut at the blast). Sound only; no numbers changed.
- 0.6.1: Napalm thrown with Left Click after aiming (speed 45 -> 78, range 80 -> 240: same arc, 3x as far); Flame Dash held up to 5 s (was 3), cooldown 13 s (was 8). Ragdoll deaths and the Combustion range ring are visual. Infernus benched once: 929 %/s vs Engineer 1611 (below; the gun waits while he holds the flask); not retuned.
- 0.6.0: co-op; no numbers changed. Power Surge's arcs are now limited to once per gun interval on the server (was once per burst in the owner's state).

## Thunderstore (2026-10-19)
- RoR2 has no Steam Workshop; Thunderstore is the hub. `package.py <ver> --thunderstore`; the plugin runs
  `plugins/extract/DeadlockExtract.exe` itself (`FirstRun.cs`), the loading screen waits; no Deadlock files ship.
- First release "Risk of Deadlock" = Haze, Abrams, Seven, Infernus (`thunderstore/release.json` → `heroes.txt`, cut manifest).
  Mod-list entry `melty.deadlockofrain;<version>;<built hero ids>`: other heroes, or none, can't join.

- 0.7.0 rejected; moderator Harb: why an exe, use a library. VRF is .NET 10-only (Mono can't load it), so it stays an exe,
  built in public by GitHub: `.github/workflows/extractor.yml` on tag `extractor-X.Y.Z` -> Release + sha256;
  `fetch_extractor.py`; `package.py --thunderstore` ships only that exe.

## Deadlock update 6795 (2026-10-19, v0.7.1)
- VRF 20.0.6980 threw "Unknown KVType for variable 'datatype': 26" on Haze's model; extractor 1.2.0 aborted the whole run,
  so 3 Melty players on 0.6.3 got no heroes. VRF 20.1.7467 reads all five; extractor 1.3.0 reads each hero on its own
  (`HERO FAILED`, ready.json `failed`), the plugin skips and names the missing ones.
- VRF 20.1 also exports normal maps (unused: ModelBuilder clears them) and Haze's head-smoke cutout alpha (was an
  opaque black card before).

## Fallback system (2026-10-19, v0.7.2)
- A player on Deadlock 5977 had no `heroes_wip/inferno/inferno.vmdl`: 0.7.1 skipped Infernus, the others loaded.
- Extractor 1.4.0 takes each model from `scripts/heroes.vdata` (`m_strModelName`, same file name first), then the sheet
  path, then heroes_wip <-> heroes_staging; progress lines on stdout. Plugin: a locked red "!" tile per skipped hero,
  versions (built with / this PC) in every problem message, `built-with.txt` from package.py. `BREAK=<hero> run_test.sh`.

## 0.7.3 (2026-10-19)
- 0.7.2 confirmed on a player's PC: extractor 1.4.0 read 5/5 heroes on Deadlock 6795. The same player's game crashed in
  ntdll (c0000005) while quitting; `DeadlockAudio.Shutdown` now joins the mixer thread so winmm is closed before exit.
  That did NOT stop the crash: it repeats on this PC when quitting after a test run, inside Mono's shutdown with no mod
  thread alive (dump scan). Unmodded and title-screen quits don't crash. Still open.

## 0.7.4 (2026-10-20): the quit crash, found and fixed
- `tools/quit_test.sh` reproduces it (run, then close the window): 2/3 crashed at baseline, 2/2 with the mod switched off
  (`OFF=mod VANILLA=1`), so it was never our code. Guarding our OnDestroy work, pre-compiling our methods, switching off
  Harmony and the mixer: no change.
- Dumps + Microsoft/Unity symbols: a main-thread stack overflow during Unity's CleanupAllObjects; Mono's recovery thunk
  runs misaligned and ntdll faults under the Steam overlay's LoadLibraryExW hook. RoR2BepInExPack's nop patch (ported)
  turned it into a logged StackOverflowException whose trace showed the loop: RoR2's console redirector `Disengage`
  makes BepInEx's LoggedTextWriter write to itself. `Runtime/QuitSafety.cs` skips `Disengage` and keeps the nop patch.
  Details: doc 02 §12 gotcha 22.

## 0.9.0 (2026-10-21): Rat King
- Seventh hero from the creator's spec, matched to Captain. Extractor 1.6.0: props with a skeleton and clips (the rat, the banner's
  sewer), hero meshes kept as their own parts (the 19-mesh ammo belt). Spring bones for his cape and tail, a baked-mesh rat crowd
  (ring, slam burst, 50-rat swarm), a three-blast hopping grenade, a banner leap with line-of-sight blasts and a 3 s stun.
- Found on the way: a clip without a root_motion track inherited the last clip's root turn (Rat King's idle laid him on his back);
  every clip now folds the root's rotation into its children (doc 02 §12 gotcha 23). His muzzle bone sits at the grip
  (`animations.muzzleForward`). All seven heroes pass `run_test.sh`; bench 2999 vs Captain 3490 (inside).

## 0.9.0b (2026-10-21): Rat King after the creator's first play
- Footsteps a tenth (`ratking_gun/land` keeps the landing loud), flip only on the air jump, Royal Pestments 50 %, Rat Swarm with
  no time limit (rats spent when their enemy dies; 5 per enemy, 10 on bosses). Bench 3542 vs Captain 3490.
- Extractor 1.6.1 / DLM2: vertex colours and `.vmat` tint/alpha-test/glow, for props only. The banner's earth and garbage are
  white 1×1 textures coloured by vertex colours in Deadlock. Whole-material glows on heroes went badly wrong in a first try (doc 01 §7).

## 0.9.0c (2026-10-21): Royal Pestments names the rats
- Voice rows can chain takes (`chain`) and skip some (`skipTakes`); Royal Pestments calls out ~120 named rats in turn. Extractor
  1.6.2 names voice files by Deadlock's take number and reads every take only for `voiceAllTakes` groups.
- A game started during a cache rebuild drew the placeholder banner: rebuild only when the creator isn't playing.

## Gotchas found
1. RoR2's CharacterModel keeps ONE material per renderer (it re-applies defaultMaterial), so a multi-material
   SkinnedMeshRenderer only drew submesh 0. Fix: one SkinnedMeshRenderer per material, one RendererInfo each.
2. Unity's Standard shader is stripped from RoR2; use the Hopoo deferred shader from matCommandoDualies.mat.
3. Instantiate doesn't copy private fields: the animator re-initialises itself on every clone.
4. Deadlock shoot/land clips are additive: export with ComposeAdditiveAnimations, or they crush the pose.
5. Deadlock clips carry root motion (runs move 4-6 m per loop): pin the root bone's position.
6. The loadout panel crashes on a body with zero skins (it indexes the empty skins row): add a Default entry.
7. Heroes lose the Commando model's ModelSkinController, so body.portraitIcon/skins come from our own code.
8. RoR2 disables Unity audio (Wwise only): Deadlock sounds go through our own waveOut mixer.
9. glTF -> Unity: mirror X, swap winding, flip V; this gives correct facing (models face +Z).
10. Equipment (v0.4.0 ults on Q): EquipmentSlot.PerformEquipmentAction runs on the server only, and a server SetState is
   ignored by a client that owns its body: start the state where the body has authority (prefix for server-owned bodies,
   the activation ClientRpc for remote owners) and serialize the row id with the state.
11. Burst guns (v0.4.1): when the magazine ran dry mid-burst, FireOne ended the burst (burstLeft = 0) and the caller
   then decremented it to -1, so the gun never fired, reloaded or let go again. Seven's 29-round magazine in bursts of 3
   hit it every magazine; other skills interrupting the state hid it in play and in the test.
12. Sound volume (v0.5.1, extractor 1.2.0): Deadlock's vsndevts `volume` is in dB, not a gain. Read as a gain, every event
   at 0 dB or below fell to the mixer's 0.2 floor (Infernus' gun, -1 dB, sounded thin). Now written as 10^(dB/20).
13. Co-op (v0.6.0): before 0.6.0 every server-only object (dagger, ball, flask, storm, pulses) and every server-only sound or
   visual (stack bursts, gun hit marks) existed on the host alone, and a joined player's gun passive never stacked (the
   bullet's hit callback runs on the owner's PC, where NetworkServer.active is false). Now: objects fly on every machine,
   damage on the server; server-only sounds/visuals go through one relay EffectDef; gun passives run in onServerDamageDealt.
14. Two copies of RoR2 on one PC (co-op test): the second starts straight from the exe with SteamAppId=632360 (no Steam
   restart); BepInEx writes it to LogOutput.log.1. RoR2 kicks a second connection of the same Steam account
   (KICK_REASON_ACCOUNT_ALREADY_ON_SERVER) and one account can't join its own Steam lobby, so the test joins over loopback
   (host 1 listens on UDP 7777) with a test-only fake id. The second copy needs 3-5 min to load and runs well behind the
   host: never time a step on one copy's clock; wait for the other copy's answer.
15. RoR2 never sends a player's aim (InputBankTest.aimDirection) to the server: on the host a joined player's body aims
   straight ahead. A state that throws on the server must carry the owner's aim (DlrState.OwnerAim, serialized).
   Symptom: a joined Infernus' Napalm never landed while the straight-ahead Sleep Dagger did.
lab restored Tue Oct 13 01:42:10 GMTST 2026

## Daily watch (2026-10-21)
- `tools/watch/daily.ps1` (Task Scheduler, 04:00): local Steam build numbers (appmanifest buildid + StateFlags, steam.inf) vs
  `build/watch/state.json`; Melty triage on the live release (Haiku, `--json-schema`); if anything changed, a `claude -p` fix run
  on branch `watch/<date>` that may publish after every check passes. Stops if any Steam game is running. Doc 03 §5b.
- 2026-10-22: first night caught Deadlock 6812 (nothing broke) and a Shoulder Charge stun exception (fixed, merged). Triage now Sonnet with
  signatures + `build/watch/known-breaks.txt`; `Bash(python tools/*)` rule fixed (a `:*` rule needs a space after the prefix).

## 0.10.0 (2026-10-22)
- Melty: seven heroes (Rat King playable), Billy the black-silhouette coming-soon tile (his work in progress ships inert).
  Built with Deadlock 6817 (updated that evening; listings: +2 files, no clip/mesh/sound change; cache 8/8). Joined-player
  Scrap Grenade sends its start point (doc 02 gotcha 30); co-op Rat Swarm window 5 s. No Melty test, at the creator's request.

## 0.9.1 (2026-10-22)
- Melty: six heroes (new Seven), Rat King a black-silhouette coming-soon tile, co-op joinable from Melty's Play (title-screen
  Steam lobby, log emptied when it closes), Fx.Stun fix, joined-player Combustion fix (blast in OnExit near the fuse end).
  Built with Deadlock 6813 (two updates that day). Test 65dcf162 2/2, published (safety review).
