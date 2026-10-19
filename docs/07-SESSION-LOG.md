# 07: Session log (what the creator asked, what we asked, what was done)

> **Append-only history.** One entry per creator request (or per working session), newest **at the bottom**. Read
> the last 2–3 entries before starting; `docs/00-HANDOFF.md` holds the current state. CLAUDE.md requires an entry for
> every request that changes anything, written in the same commit as the work (or a docs-only commit if no code changed).
> Quote the creator verbatim (typos included) so intent can be re-read later; summarise questions and answers.

## Entry template
```
### YYYY-MM-DD · short title · vX.Y.Z · commits abc1234, def5678
**Request (verbatim):** "..."
**Questions asked → answers:** topic: question → answer (free-text answers quoted)
**Done:** what changed (sheets, code, tools, docs, pages), each in a short bullet
**Results:** test outcome (checks, heroes), bench numbers, anything measured
**Not done / open:** what is left, what is unverified, what the creator still has to choose
**Lessons:** anything that cost time and how to avoid it (also add to doc 00 §7 if general)
```

---

## Backfilled history (from git and earlier summaries; less detail)

### 2026-10-13 · 0.1.0 first release
Melty setup brief (CLAUDE.md "Origin and goals"): "Deadlock heroes drop into RoR2", roster Haze/Seven/Abrams (+Infernus),
solo first. Extractor, DLM format, survivors, gun rig started. Commits `f8e4f9a`…`1451f48`. (Commit `ca07a43` "Maths I
Linear Algebra\Matrix multiplication" is unrelated to the mod.)

### 2026-10-13/14 · 0.2.0 four skills, ult on R
Playtest: "risk of rain only has 4 active abilities"; abilities had to really work ("a lot of the abilities are not
working"), gun locked to the hand. Four skills (gun, 2 abilities, ult on R with a charge meter), `expect` checks in test
mode. Commits `dd51cca`, `bde14ed`, `c212cfe`. Docs 01–03, CLAUDE.md (`ee13466`), doc 04 proposals (`063447c`).

### 2026-10-16 · 0.3.0 ability redesign
The creator picked per slot from doc 04 (doc 04 §6, doc 03 §9). Follow-up fixes from playtest: Abrams' charge pose, Smoke
Bomb targeting, knock-aside, Thunderhead visuals. Commits `23bcd03`…`081139c`.

### 2026-10-16 · DPS benchmark and pages
Creator's hypothesis: Commando using everything when ready for 30 s on Monsoon against a pack is the RoR2 DPS standard;
heroes more than ~250 %/s off need changes. Built `DpsBench` + `run_bench.sh`, doc 05, doc 06, kit pages
(`tools/kit_pages.py`) published as claude.ai Artifacts. Commits `714977f`, `8753a40`, `9cecc51`.

---

### 2026-10-16 · Ults become Q equipment, new R moves, Infernus fire · v0.4.0 · commit a333644
**Request (verbatim):** "ok think there is a problem with the current set up each of our characters Haze Abrams Seven
Infernus have ult in deadlock. i actually want to restructure this and have the ults as new pick ups Activate Equipment: Q
as these ultimates. so every new hero form deadlock has this auto equiped at the start, but they can also drop them and
find other hero ults in the game. now the iteam should look like the characters head in game and the recharge time should
be 120 and these ult should be scaled to the same dps power level as https://riskofrain2.fandom.com/wiki/Preon_Accumulator.
now this raises andother problem now each of our characters are missin and ability good thing we alreday did some
reasearch. let look again at the wikis for each deadlock character and suggest some missing move to replace these
ultimates! please do deep researcha and ask lot of question befor eimplementations"

**Questions asked → answers:**
- Drop pool → everyone; "any wher you can find the Preon Accumulator you can find these equipable ult items".
- Power → Preon scaled to 120 s (≈6800 % per enemy).
- Charge meter → removed.
- Pickup look → "Whole mini hero".
- Haze R → "phantom strike please rebuild the vfx in unity and make it look cool!!!" (blink + 3-hit flurry).
- Abrams R → Heavy Melee haymaker (tap, instant punch).
- Seven R → Power Surge, "make shock work like risk of rain shock... Unstable Tesla Coil but make the move feel like
  deadlock" (gun-driven Tesla arcs).
- Infernus R → "please reinvest time into fire viuals in risk of rain make it clearer, and do 1. torment pulse aura";
  fire VFX scope: only Infernus' flames.
- Seismic slam key → Left Click only.

**Done:**
- `Content/Equipment.cs` (defs, starting equipment, firing on the authority side, baked mini-hero pickup);
  `Ult.cs` deleted.
- `States/Specials.cs` (Phantom Strike, Haymaker, Power Surge, Torment Pulse).
- Infernus fire rebuilt from RoR2 fire materials; preflight slots/equipment checks; test check `ult-equipment`;
  bench Q pass.
- Docs 01–06, README, MODLOG gotcha 10.

**Results:** `run_test.sh` all pass, 4/4. Bench (with one Q per pass) Commando+Preon 3008, Haze 611, Abrams 1684,
Seven 5113, Infernus 3005.

**Open:** pickup figure ×2.2 padding not seen in game; Seven M1-alone measured 62 (turned out to be a real bug, fixed
in 0.4.1).

### 2026-10-16 · "what is the artifact looking like now for the dps"
Opened the Balance page and summarised it. No changes.

### 2026-10-16 · Balance pass on main moves · v0.4.1 · commit d91047f
**Request (verbatim):** "ok let make the balencing imporvements, aks question clarify what you are doing."

**Questions asked → answers:**
- Haze buffs → all four, plus (free text): "let imporve this ult when we test next time for the dps artifact comparison we
  should not compare these ultimates! technically any character can have them so let just compare the main moves! and
  the pick up 'ultimates' can we overpowered as we like!"
- Abrams → all three (Seismic 18 m, Haymaker shockwave, Shoulder Charge 500 %).
- Seven → (free text) "can we revist this and suggest cooldown increases rather than nerfing damage!" → then: tune
  cooldowns to the band.
- Infernus → Torment Pulse 50 % + Flame Gun buff.
- Balanced band → Commando ±20 %.
- Q on the page → separate "just for fun" table.

**Done:**
- Sheets for all of the above.
- Code:
  - `extra` flags `pierce` (projectile) and `shockwave` (heavy_melee), checked by preflight;
  - Bullet Dance hits every enemy;
  - Haymaker cone + `wave` vfx;
  - burst-gun fix (Seven's gun froze after a magazine).
- Preflight: ult floor only (≥ 6000 %).
- Bench: no equipment in "all", hero filter.
- Docs 02/03/05/06, MODLOG 11, CLAUDE.md.
- Pages rebuilt and republished.

**Results:**
- Test all pass, 4/4.
- Seven tuning: 12/12/20 s → 1822; 16/16/30 s → 1272.
- Full bench (main moves, %/s): Commando 1255, Haze 1315, Seven 1272 (inside), Infernus 920, Abrams 729 (below).
- Q fun table: Seismic 74800 %, Combustion 74800 %, Preon 58400 %, Bullet Dance 47008 %, Thunderhead 27200 %.

**Open:** fixes for Abrams/Infernus (doc 05 §5) not chosen; Haymaker `wave` vfx not checked by eye (no devshots folder).

**Lessons:**
- Write Python edit scripts to the scratchpad instead of heredocs.
- A backspace byte had crept into doc 02.

### 2026-10-16 · Kit-page redesign, RoR2 matches, handoff system · in progress
**Request (verbatim):** "ok so the stuctur of the artifact is not good you need to have the top section be a pprofile
picture of the character and then state like heath etc (rog stats ore character) the list of the ability what they do and
then the moths of the dps per ecah bility to the side of them. make it look cool and copr some styling form the deadlock
wiki, the  underneath you can have your Ult with a little picture aswell correctpoding to the character they are coming
form then finall underneath we can do updates and the balance states for ecah character, please giev these guya
corresponing actual rish of rain character and have the anaysis be separate section for ecah characetr"
(It came with a pasted excerpt of the Balance page showing the old two-name analogs, e.g. "Commando / Bandit".)

**Questions asked → answers:**
- Pictures → studio render (our own in-game renders; Deadlock art may not be copied onto pages).
- RoR2 match → their stat base: Haze = Commando, Abrams = Loader, Seven = Artificer, Infernus = Engineer.
- Compare to → each hero's own RoR2 match.
- Pages → 4 hero pages + overview.

**Follow-up request (verbatim, interrupting the portrait render):** "ok i want you to write some overview prompt and
update other so basically what the characters do now the ult system and the dps testing system and how we map our
characters dps level to already existing characters in ror2 any thing else form theis chat context that need to recrorded
indepth to be handed off to the next coding agent an indpeth system that allows us to read and record everything in the
future! also yes continue with the artifact task please"

**Mid-task message (verbatim):** "you are just building an artofact why do you keep opening the game?" → explained: the
pictures and the matches' DPS can only come from the game; asked → "One 30-min bench, then pages".

**Done:**
- `docs/00-HANDOFF.md` (current state, paste-ready prompt, how the creator works, lessons, the record system) and this log.
- CLAUDE.md rules: record every session, ask before big changes, no Deadlock art on pages.
- Texts: README and the in-game Haze passive tooltip corrected (said every 20 stacks / 200 %).
- Bench:
  - each hero's RoR2 match runs before it;
  - `stats` lines per survivor;
  - minion damage counts for its owner;
  - studio portraits (`PORTRAITS=1`); `tools/portraits.py`.
- Game log copied to `build/bench_log.txt`.
- `tools/kit_pages.py` rewritten (new layout, Deadlock-wiki styling); five pages republished.
- Docs 02/03/05/06.

**Results (bench, main moves %/s, hero vs match):**
- Haze 1478 vs Commando 1064 (above).
- Abrams 985 vs Loader 2747 (below).
- Seven 1242 vs Artificer 2790 (below).
- Infernus 1226 vs Engineer 1268 (inside).

**Not done / open:**
- RoR2 survivor portraits render empty (doc 00 §9); no retuning against the matches yet (creator to choose, doc 05 §3.0).
- The pages were not previewed before publishing: the browser tool cannot open local files.

**Lessons:**
- Say why before launching the game, and ask when the creator didn't ask for game time.
- Python 3.11 rejects backslashes inside f-string expressions: build such strings first.

### 2026-10-16 · Balance each hero to its RoR2 match · v0.4.2
**Request (verbatim):** "ok come up witha  plan to balance the dps to their counter part! ask questions" (with a pasted copy
of the overview's pairs table).

**Questions asked → answers:**
- Haze → (free text) "ok let increase the threshold to 500 dps i think that is good enough, so no change here".
- Abrams → "Damage only".
- Seven → "Cooldowns + gun buff".
- Game runs → "As many as needed".
- After a run was stopped for low memory and Artificer proved noisy: Seven target → "Artificer's average"; may I keep
  running → "Yes, carry on".

**Done:**
- Band ±500 %/s, means over runs (`RUNS` in `kit_pages.py`).
- Abrams: Shotgun 52 %, Siphon 450 %/s, Shoulder Charge 1600 %, Haymaker 2400 %.
- Seven: Shock Rifle 60 %; cooldowns 8 / 6 / 10 s.
- Six bench runs: two Seven-only tries jumped to 5300+ below 8 s Lightning Ball; one run was killed by the memory reaper, and
  the game it left open was closed.
- Full in-game test; docs 00/03/05/06/07; pages republished; version 0.4.2.

**Results:** all four inside on the means. Haze 1271 vs Commando 1224, Abrams 2508 vs Loader 2973, Seven 3999 vs
Artificer 3605, Infernus 1102 vs Engineer 1488.

**Open:**
- The single-run noise of Loader and Artificer; a steadier bench (3 runs per survivor, or 60 s passes) would make the
  verdicts firmer.
- Seven's chain-reaction cliff below 8 s Lightning Ball.

**Lessons:** see doc 00 §7 (means not single runs, Seven's cliff, the memory reaper).

### 2026-10-17 · Stored DPS library for every survivor
**Request (verbatim):** "can you do a dps anaysis on all characters so we never need to retest this again. keep the data for
memory and use in the furture... obvious we will need to test dps again on the new characters but we can just compare to our
stored data... add this to the artifact at the bottom pls make it look nice with the character names and abilities"

**Questions asked → answers:**
- Who → every survivor.
- Runs → 3 each.
- Tests → pack only.
- Mid-task the creator asked "how much longer" → about 2¼ h.

**Done:**
- Bench library mode (`ALL=1`, `REPEAT`, skills lines, Q pass only for heroes and Commando).
- Arena line-of-sight check, after a bad stage gave Commando 5 hits.
- Captain releases M1 every 0.6 s.
- `tools/dps_library.py` → `docs/dps-library.json`, with notes for Acrid, Railgunner and Void Fiend (not comparable).
- Overview page section "Survivor DPS library": a ranked chart with spreads, then a card per survivor with its moves.
- Balance verdicts pool every run of the build.
- Memory note `dps-library`; docs 00/02/03/05.

**Open:** results and Seven's verdict once run 3 is in (see the next entry).

### 2026-10-17 · Deadlock voice lines (started during the library runs)
**Request (verbatim):** "ok next after you are done with the artifact there is an important task. i want youn to go to the
deadlock wiki /{character}/Voice_lines. now we can rip out the wav file form dead lock and play them in risk of rain 2. so it
imporatnt we only grab the correct ones for one under the section 'Abilities' for when abhram uses his moves make sure they
are relavent... also for the ultimate voice lines and sounds it will alway play the correct one associated with the ultimate
item not the current character so if the 'Commando' from the base game uses 'Abhrams' item ultimate 'Seismic Impact' it will
play a line from abhrams! also i want basic voice line... but i dont want them to be annoying so maybe when you do a
especially good multikill or if it has been like between 2 to 5 mins of silence form the character you are currently
playing playing. the game will play a voice line from the wiki under the 'Generic Voice lines'. now the final requirement is
when a cxharacter like abhrams spawns on a new world or stagge level, so when ou start a game but also after every teleport
when you eneter the new world please play a line fomr the 'Select' section, also when the charctere has fough the boss and
it getting their cash taken by the teleporter and is about to start spawning out please play a line fomr the 'Unselect'
section on the wiki... dont aks question i believe you can make the right decisions to amke this happen with my intervention
so please push foreward with the task as soonn as you have completed the update to the artifact... also do not log what
voice line each character has in the artifact it can be a surprise to players." Then: "no question just make the best
decicion you think suit for the next task of voice lines!!!"

**Decisions (no questions, as asked):**
- **Source:** the wiki's file names (`Atlas_select_01.mp3`) are the VPK's `sounds/vo/<code>/<code>_select_01.vsnd`, so the
  install listing is the source (the wiki's long pages were partly blocked to automated reading).
- **Ability lines:** `<code>_use_power1..4`, matched to Deadlock's ability slots.
  - Haze's R: her Phantom Strike item line.
  - Seven's R: `use_power3` (his real Power Surge).
  - Abrams' Haymaker and Infernus' Torment Pulse: an effort grunt (Deadlock has no line for them).
- **Ult:** the item's own hero speaks, 100 %.
- **Abilities:** 40 % with an 8 s gap; grunts 60 % with a 3 s gap.
- **Generic lines:** multikills of 4 / 7 / 10 within 1.5 s → killstreak start / mid / high; idle 2–5 min →
  `hunt` / `happy`.
- **Select:** once per stage, 2 s after spawning.
- **Unselect:** the teleporter finish, when gold becomes experience.
- Lines never overlap.
- The pages never list the lines.

**Done so far:** `design/voice.json` (44 rows), preflight checks against the install listing, `gen.py` manifest
`voiceGroups`, extractor `ExportVoice` + version 1.1.0, `Runtime/Voice.cs`, `DeadlockAudio.IsPlaying`, hooks sheet, test
check `voice-lines`, docs 01/02/03, README, CLAUDE.md.

**Voice results:**
- The extractor 1.1.0 re-extract found all 44 voice groups (331 takes).
- `run_test.sh`: every check passes, including the new `voice-lines`; 4/4 heroes.
- Version 0.5.0 is installed.

**Not verified in a real run:**
- unselect at the teleporter finish;
- multikill and idle lines;
- how loud the lines sit against RoR2's sounds (gain 1.15).

**Release notes:**
- Players' caches must re-extract: the extractor version is in the cache stamp, but Melty skips setup when `ready.json` exists
  (doc 03 §10).
- Add `voice-lines` to the recipe's checks.

### 2026-10-17 · playtest feel pass, sound volume fix, heroes-only bench · v0.5.1
**Request (verbatim):** "the punch on abhrams is a biut weird can it take the direction and make sure it doesnt hit into
the floor. have there abe a really quick run and snap to the enemy when a punch is selected on, even if the enemy is in the
air pls just have them fly up oto around let say 15ish meters... the main problem was them slaping into the floor and
wasting the move. also make the hit more impact ful maybe take some aniimation and add more vfx / for haze please make sure
the sleep dagger is consistant, I notices I didn't see a projectile a few times / also for phantom strike plase keep the
logic the sam I love the snap, but maybe make it a 5 meter aoe attackso it will kill multiple / ok for seven we need work on
the m2 lightning ball may make the speed 75% and let the ball bounce off surfaces but travel through enemys, no also maybe
increase it range of effective ness to like 10 meteres sphere around it and clear lighting strikes hitting the enemys close
by. for the ult make the clouds go a lot highter / Infernum I want =the m1 gun to come across more impact ful it mostly the
sound add some smokey effect or something! ffor the m2 bottle throw I want to see a projectile fly fomr the infernum
character and go toward the targeted  area, just reuse a simple object doesn't need to be accurate" + "aks questions".
Mid-work: "why are u running a teston base characters we already collected this data, justrun tests on new characters and
compare to our collected data. add thisnto claude .md"
**Questions asked → answers:** Haymaker target → "Lock only; no target, no punch"; lock range → 20 m; ball speed
"75%" → 75 % of now (slower); impact → hit-stop + shake, bigger impact VFX, heavier hit sound (no faster animation);
cloud height → 35 m; Flame Gun → louder + heavier layer; balance → bench once and report only.
**Done:**
- Haymaker (`HeavyMelee`): lock-on via shared `DlrState.AimTarget` (Phantom Strike uses it too), stock refunded without a
  target, 0.2 s wind-up, gravity-free rush at 45 m/s steering at the target, punch along the 3D line, 0.08 s hit-stop,
  Loader's fist impact (`punch_impact`), bigger shake and flashes, `hitLayer` Abrams.Charge.Wall.Impact.
- Sleep Dagger thrown from the aim origin (the hand inside a wall/floor killed it unseen), thrown on exit if cut short,
  new `haze_dagger` glow and trail. Phantom Strike cuts hit every enemy within 5 m.
- Lightning Ball 10.5 m/s, radius 10, bounces (`Vector3.Reflect`), clear strikes to each enemy. Thunderhead clouds 35 m.
- Flame Gun: `castLayer` Warden.Wpn.Fire, `gain` 1.2, muzzle flash and smoke (`muzzle` vfx moment). Napalm: Molotov's
  bottle (`flask`) rides on the lob (the old flask visual never followed the lob: `Root(at, 0, null)`).
- Sound sheet columns `castLayer`, `hitLayer`, `gain`. **Extractor 1.2.0:** Deadlock's `volume` is dB; it was used as a
  gain, so every 0 dB-or-quieter sound (Infernus' gun at −1 dB) played at the 20 % floor — the real cause of the weak gun.
- `run_bench.sh` benches our heroes only by default (`survivors: heroes`, comma-separated ids); pages and
  `dps_library.py` take vanilla numbers from `docs/dps-library.json`; CLAUDE.md rule "Never re-bench the base characters".
**Results:** `run_test.sh` every check passes, 4/4 heroes (two earlier runs landed on a ledge spot and failed everything;
v0.5.0 on the same day passed, then v0.5.1 passed). Heroes-only bench (doc 05 §3.1): Haze 6550 vs Commando 1222 (Phantom
Strike alone 4139: the area spreads Fixation and the bursts chain), Abrams 2460 vs Loader 2951 (inside), Seven 3164 vs
Artificer 4013 (ball 1652 → 390: it bounces past the pack). Not retuned, per the creator.
**Not done / open:** Haze far above and Seven below the band: the creator chooses fixes (Fixation on the target only;
ball bounces only off walls or slows). Not seen in play: the flask in flight, the clouds at 35 m, how loud the new sound
levels sit. Players need a forced re-extract (extractor 1.2.0).
**Lessons:** Deadlock sound volumes are dB; a full bench re-measured stored vanilla numbers (stopped at the creator's
word); `run_test.sh` can land on a ledge spot.

### 2026-10-17 · co-op (play online with friends) · v0.6.0
**Request (verbatim):** "ok now make these character work online!"
**Questions asked → answers:** how friends join → "Steam invite + Melty link"; a friend without Deadlock → "Everyone
needs Deadlock"; the two-copy test on this PC → "Anytime, automatically"; players → "4, like vanilla".
**Done:**
- `Runtime/Net.cs`: the mod-list entry (`melty.deadlockofrain;0.6.0`, so only the same version can join), the **relay**
  (one EffectDef; server-only sounds/visuals and other players' shots/reloads reach every player once), `MeltyJoin`
  (Melty's contract: a host's Steam lobby is written as `Melty join address: <id>.steamlobby`; `join=` joins from the
  title screen), `CoopTestPatches` (test only).
- Skill states made co-op safe (doc 02 §7.7): visuals/sounds in the states, damage on the server, owner-only movement and
  bullets; lock-on targets, Static Charge's point and the owner's aim travel with the state; `MotionDir` for steered dashes;
  Seismic Impact's dive seen as a fast drop; projectiles/zones/pulses on every machine with damage only on the server;
  gun passives and Power Surge arcs moved to the server (`GunHits`, `onServerDamageDealt`); firing breaks Smoke Bomb on
  the server; Siphon Life's loop sound for everyone (now also its end sound).
- Co-op test: `plugin/Runtime/MeltyTestCoop.cs` + `tools/run_coop_test.sh [heroes]`; `lab_restore.sh` closes every copy
  and empties `join=`. Hooks sheet: 5 new rows. Recipe gains a `multiplayer` block (doc 03 §6; `setup_multiplayer`: valid).
- Docs 02 (§7.7, §10.2, hooks), 03 (decision row, test loop, recipe, open item), README (co-op), CLAUDE.md (co-op rules,
  command), MODLOG (gotchas 13–15). Pages: a v0.6.0 "Co-op" line per hero, republished.
**Results:** `run_coop_test.sh`: every check passes (`coop-hosts`, `coop-joined`, `coop-run`, `coop-haze/abrams/seven/infernus`,
`coop-shots-seen`, `coop-effects-seen`): **4/4 heroes, all 25 skills, played by a joined player and measured on the host**;
the joiner received 334 relayed effects. `run_test.sh` (solo) still passes everything, 4/4. On the way: the joiner skipped
every "fire" (same step number as "prep"); one run landed the targets badly; the second copy loads in 3–5 min and runs well
behind the host (the host now waits for each answer); **RoR2 doesn't send a player's aim to the server**, so a joined
Infernus' Napalm never landed (fixed: `OwnerAim`); the joined copy's camera never settled, so the test switches its player
controller off and aims at the target the host names.
**Not done / open:** never played with a real friend: the Steam-lobby route and Melty's Join with a `.steamlobby` address
can't be tested with one Steam account, and lag is untested (Haymaker's hit and Seismic Impact's slam land a moment after
the owner sees them). The Melty address is written when the host opens a Multiplayer lobby, not at game start. Napalm's
smoke looked larger and darker on the joined copy's small window: check in play. Not published (needs a release with the
`multiplayer` recipe block and a forced re-extract, as before).
**Lessons:** in RoR2 the skill states run on every machine but inputs (aim) exist only on the owner; never time one copy
of the game on the other's clock; two copies of one Steam account can only meet over loopback with a test-only id.

### 2026-10-18 · documentation check: sounds, ults, the DPS library · docs only
**Request (verbatim):** "ok has everything be mad a record of in docukentation the dosund sthe the ults and the fact we have already run the dps test on all bas echaracters if not you should now"
**Questions asked → answers:** none.
**Done:** checked every doc. Already recorded: the ults (doc 06 rows and "The ult equipment", doc 02 kinds, `equipment.json`),
the DPS library (doc 05 §6, doc 03 decision row, doc 00 "Bench rule", CLAUDE.md "Never re-bench", `docs/dps-library.json`),
sound extraction (doc 01 §6.5) and the mixer (doc 02). **Missing, now added to doc 06:**
- a **Sounds** table: every move's Deadlock sound per moment (start, loop, hit, end, reload, layer, gain), mirroring `design/sounds.json`;
- a **Voice lines** summary: when each hero speaks (no lines written out);
- **every vanilla survivor's stored DPS** (library means, 3 runs each, with the not-comparable three).
**Fixed stale numbers in doc 06:** version 0.4.2 → 0.6.0; DPS source now the library; Commando baseline 1255 → 1222 (M2 752 → 717,
Q 1947 → 1902); Infernus 1102 vs Engineer 1488 → 1512 vs 1611, his moves 317/538/311/768 → 320/588/441/901.
**Results:** the pages already read the library, so they show the right numbers; no republish needed.
**Not done / open:** the Seekers of the Storm survivors (Seeker, False Son, CHEF) are not in the library: their expansion was off
in the bench's run. Measuring them needs that expansion and the creator's go-ahead (CLAUDE.md: ask before re-benching).
**Lessons:** doc 06 header lines (version, DPS source) go stale silently; refresh them whenever the numbers move.

### 2026-10-18 · Napalm aimed throw, Combustion ring, ragdolls, longer Flame Dash · v0.6.1
**Request (verbatim):** "ok for infernum i woudl like the bottle to be thorwn and bit harder... but i really like how the arn of the bottle and the visual happens so maybe throw it like 3 times as hard before the arc down happens, also i woudl like some more cisual on the equipable ultimate ability for infernum some thing to indicate he will explaode maybe some yellow ring indicating the range of the exposion on teh wind up... pls make sur ethese is acttached to the equipable item not the character, so it is accessable everywhere... also for ecah character we need on death to have a ragdoll this is for all characters!"
Mid-work: "make infernus run last fir 5 seconds and increase the cool down by 5 pls"; also asked "why are you testing every character?" and "what are you doing?" (answered: `run_test.sh` always plays all four, and the ragdoll touches all four; DPS was benched for Infernus only).
Earlier the same day: "so if i just press play on steam my new characters will be there" → Melty had switched BepInEx off; `winhttp.dll` copied back so a Steam launch loads the mods.
**Questions asked → answers:** the harder throw → "Same arc, 3× as far", with the note "can we have an indicator of how far and we trigger the trow by pressing lmb".
**Done:**
- Napalm: `ThrowLob` is now an aim state (owner sees the path and a splash circle, `DlrLob.Predict` shares the flask's flight step); Left Click → `LobThrow`; Right Click again cancels and refunds. Speed 45 → 78, range 80 → 240.
- Concussive Combustion: yellow ground ring at the full 18 m radius with an inward countdown (area domes, gotcha 15), in the ult state, so any carrier shows it.
- Ragdolls: `ModelBuilder.BuildRagdoll` + RoR2's `RagdollController` (sheet columns `ragdollBones`, `ragdollThickness`); `DeadlockAnimator.OnDisable` stops the clips, drops the gun with the hand, switches off bone hurtboxes.
- Flame Dash: up to 5 s held (was 3), cooldown 13 s (was 8).
- Co-op fix found on the way: `EmberFx` asked `DotController` (server only) every frame on clients, so friends never saw Afterburn's burn flames; now reads the synced `OnFire` buff.
- Test: checks `ragdoll` and `ult-any-survivor` (Commando fires Combustion). `dps_library.py` tagged every hero with the new build on `--add`; now only the measured ones.
- Docs 02, 03, 05, 06, MODLOG; pages v0.6.1 lines.
**Results:** `run_test.sh`: every check passes, 4/4 heroes, `ragdoll` 4/4 (bones limp, head 1.5–2.2 m → under 0.5 m), `ult-any-survivor` pass (first run: Haymaker found no target on a ledge spot, the known flaky stage; the re-run passed). `run_coop_test.sh infernus`: first run the joined player's Combustion did no damage (charge spent, no blast; no error in either log), re-run passed everything; not explained. Infernus bench: 929 %/s vs Engineer 1611 (below ±500; the gun waits while he holds the flask), Napalm alone 588 → 685, Flame Dash alone 441 → 145 (one run). Not retuned.
**Not done / open:** Infernus' balance is the creator's call (doc 05 §3.2 options). The one-off co-op Combustion miss. Haze/Seven balance from 0.5.1 still open. Not published.
**Lessons:** a "press again to cancel" control breaks the bench, which mashes every key; give bots their own path. Library tools must tag only what they measured.


### 2026-10-18 · do the Q ult items carry their sounds and voice? + docs refresh · docs only
**Request (verbatim):** "ok do these equipable ult have the sfx and voicelines attached?", then "ok let  up date all our documenttion pls"
**Questions asked → answers:** none.
**Done:** traced it in code: the ult states play sounds and effects from `row` (= the item's `RowOverride`), and
`Equipment.Fire` calls `Voice.SkillLine(row)`, so any carrier gets the ult's own sounds and the item's hero speaks. All four
items have a `sounds.json` row and a `use_power4` voice row. Answered yes. Docs: doc 06 "The ult equipment" and doc 00 §3
now say so explicitly (and note Combustion's silent fuse); doc 02 §7.5 explains why it works; doc 00 §1 "Installed build"
was stale (v0.4.1 → v0.6.1, BepInEx on) and a leftover old state table under §1 was removed; doc 00 §9 updated.
**Results:** no code change; no retest flag (nothing in a kit changed); pages unchanged.
**Not done / open:** offered a fuse sound for Combustion's 3 s wind-up (its `loop` is `none`); taken up in the next entry (v0.6.2).
Infernus balance (doc 05 §3.2) still the creator's call.
**Lessons:** doc 00 §1 rows other than Version/Test go stale when only those two are refreshed; re-read the whole table each session.

### 2026-10-18 · Combustion fuse ticks · v0.6.2
**Request (verbatim):** "yes add a fuse sound for combustion"
**Questions asked → answers:** I first corrected myself: the fuse is not silent; `Inferno.FireBomb.Cast` is a 3.4 s sound that
rises to the bang (I had read the sheet's `loop: none` as silence). Asked what to add → "Add a tick each second (Recommended)"
(over a burning sizzle, or leaving it).
**Done:**
- Deadlock's fire bomb has only Cast and Explode, so the tick is Rat King's `Ratking.ScrapGrenade.Timer` (a 1.26 s clock tick
  peaking at 0.35 s) and, for the last second, `Ratking.ScrapGrenade.Timer.Final` (the tick then a 1 s rising buzz). Checked by
  exporting both with the VRF CLI and measuring their loudness over time.
- Sound sheet columns `tick` / `tickFinal` (none for every other move); `gen.py` and preflight read them; `Fx.Sound` handles the
  two moments and returns the voice id; `UltSelfBomb` ticks at 0 s and 1 s, plays the final at 2 s, cuts it at the blast.
- Test check `sound-events`: every event the sound sheet names is in the cache.
- Cache re-extracted (`--manifest --force`, 58 sound events). Version 0.6.2. Docs 01, 02, 03, 06, MODLOG; Infernus page v0.6.2 line,
  republished (version 10). No retest flag: sound only, no numbers changed.
**Results:** `run_test.sh`: every check passes incl. `sound-events`, 4/4 heroes; Combustion 3264 damage + stun as Infernus, 3590
as Commando. The test cannot hear, so the tick timing is unheard until the creator plays. lab_restore, `winhttp.dll` back on.
**Not done / open:** the creator listens in play (tick loudness vs the build-up; `gain` is shared by the row). Infernus balance.
**Lessons:** a `none` loop is not a silent move; measure the cast's length before calling a gap.

### 2026-10-18 · Flame Gun 75 %, Flame Dash fire pockets, Abrams charge vfx, per-hero tests · v0.6.3
**Request (verbatim):** "ok infesus gun m1 needs more 50% to 75%, also i told you multiple times i want Flame Dash for 5 second he run and places pockets of fire that last for 5 second ecah and do burndamage and stack on any enemy in the aeas where they were placed."
Mid-work: "does any other risk of rain character work by needing to hold down the button for a ability???"; "can we have more like cool vfx on abhrams run ability please!!! like he rocketing forward with yellow on the fornt and trail running behind!"; "also why the fuck are you testing all character just test the one being changed add to claude.md"; "how many time do you need to run the game wtf are ya doing and how much longer!"
**Questions asked → answers:** stacking → "Afterburn only increase the size of the pockets by 2x"; stop early → "Always 5 s". Answered: no RoR2 survivor holds a button to keep a dash going (held buttons there charge or aim: Artificer's Nano-Bomb, Loader's Gauntlet and Grapple, Railgunner's scope).
**Done:**
- Flame Gun 50 → 75 % (targetDps 478). Flame Dash: one tap runs 5 s (`minDuration` 0); RoR2's `DamageTrail` replaced by `DlrFirePocket` (radius 3.2, ×2 the old patches, one every 3.2 m, 5 s, +3 Afterburn per 0.5 s, overlapping stack, no direct damage); expect `enemy_debuff:afterburn_buildup`.
- Abrams Shoulder Charge vfx `abrams_charge`: yellow front glow and sparks, thin shoulder trails and a core streak, dust, a launch burst; the first try's trails were too thick (hid the view), thinned.
- `run_test.sh <heroes>` (test-mode `"heroes"`, shared with the co-op test); CLAUDE.md rule, memory, docs 00/02/03. `run_bench.sh` now deletes a leftover test-mode file (it had hijacked the first Infernus bench).
- Docs 02, 03, 05, 06, MODLOG; pages Infernus, Abrams, Balance republished.
**Results:** four-hero test (started before the rule): 21/21 pass, Flame Dash built Afterburn and burned. Abrams-only test 1/1 pass twice (before/after thinning). Bench Infernus: 1149 vs Engineer 1611, inside (−462); M1 473, M2 644, Shift 423, R 929.
**Not done / open:** co-op test of the pockets skipped (time). Haze/Seven balance from 0.5.1.
**Lessons:** read back what the creator asked before (07) when they say "I told you"; my "hold to keep going" was not what they asked. Test only changed heroes. Tell the creator up front how many game runs are needed.

### 2026-10-18 · four heroes done; docs readied for a one-shot new hero · docs + test code
**Request (verbatim):** "ok i sthere any documentation needing upgrading as i think we are done development on these 4 characters! and the next task for na new agent would be attamepting a 1 shot birng a character into deadlock when i outline specific need. we need everything from sfx to the vfx reuild in unity sare we prepared!"
**Questions asked → answers:** none (read as: a new Deadlock hero into RoR2; putting characters into Deadlock itself is out of scope).
**Done:**
- New `docs/08-NEW-HERO-PLAYBOOK.md`: intake questions, Deadlock lookup, every sheet, sound (events, layers, ticks, how to measure a sound, voice groups), Unity-built effects (moments, building blocks, materials, rules learned), code, the game launches (test, bench, portraits, co-op), pages, docs, definition of done.
- Doc 03 §8 rewritten to follow doc 08 (it still said 4 skill rows and missed the ult item, voice, ragdoll, effects, bench, pages); §3 layout lists docs 07/08 and the library; §10 stale items fixed (cache rebuilt, re-extract note, screenshots). Doc 02 §11, CLAUDE.md (status + read-first 7), doc 00 (prompt, §1 status, §8 table, §9).
- Test code: `deadlock-content`, `survivors-listed` and the co-op summary used a hard-coded 4; now the hero count (a fifth hero would have failed them). Built; not run in game (no launch asked; trivial change).
**Results:** build clean.
**Not done / open:** the playbook is untested until the first new hero; Haze/Seven balance; co-op check of the pockets; listing screenshots; a release.
**Lessons:** checklists rot as features land (ult item, voice, ragdoll, effects, library); when a feature lands, update the add-a-hero playbook too.


### 2026-10-18 · upload 0.6.3 to Melty · v0.6.3
**Request (verbatim):** "its time to upload to melty!"
**Questions asked → answers:** (asked after the upload: test run now, screenshots, publish.)
**Done:**
- `package.py 0.6.3` (README's Flame Dash line was stale: "hold to keep going" → the 5 s run with fire pockets). inspect_package, setup_multiplayer (joined, same block as 0.6.0), validate_recipe (valid, one click), upload, `submit_release` → draft `5e542898-bfa4-4bad-9f8c-631175af83f1`; only the expected executable-code review finding.
- Recipe: `setup.done.contains: "|1.2.0|39DC86C5579DF781"` (end of the cache stamp) forces every older cache to re-extract; checks swap `four-slots` for `voice-lines`. Docs 00, 01, 03.
**Results:** draft submitted; not tested on Melty yet.
**Not done / open:** test_release, new screenshots (listing still shows 0.1.0), listing text, publish (ask first).
**Lessons:** the stamp includes the Deadlock build, so `contains` must leave it out, or setup would never count as done after a Deadlock update.

### 2026-10-18 · Melty test, rename to Risk of Deadlock, listing text · v0.6.3
**Request (verbatim):** after the upload, test now → "Start it now"; screenshots → "just screen shots of the character selector pls! also include our artifact and make them public!!"; then "can we please call it Risk of Deadlock in melty!"
**Done:**
- First test (`da6a91e3-…`): 3/3 games ran 8:00, every check the mod wrote passed, but it did not count: the recipe listed `gun-aims`, `gun-in-hand`, `voice-lines`, `level-stats`, `hero-scale`, written after the first 12 lines Melty read; no footage review.
- Title "Risk of Deadlock" → Melty made slug `risk-of-deadlock-2`. `MeltyTest` now looks for `risk-of-deadlock-2.json` or `deadlock-of-rain.json`; lab_restore/run_bench remove both. Recipe checks = the first 12 written. Resubmitted (release `4ed38587-…`), retest `40cc813d-…` started.
- Listing: new tagline and description with links to the five kit pages.
**Not done / open:** the retest and its footage reviews; character select screenshots; the kit pages must be shared publicly by the creator (I can't change who can see a page); publish (ask first).
**Lessons:** a Melty rename changes the slug, and the test file is named after the slug. Check which check names Melty actually read (`selfChecks.passed`) before trusting a recipe list.

### 2026-10-18 · Melty test, rename to Risk of Deadlock, listing text · v0.6.3
**Request (verbatim):** after the upload, test now → "Start it now"; screenshots → "just screen shots of the character selector pls! also include our artifact and make them public!!"; then "can we please call it Risk of Deadlock in melty!"
**Done:**
- First test (`da6a91e3-…`): 3/3 games ran 8:00, every check the mod wrote passed, but it did not count: the recipe listed `gun-aims`, `gun-in-hand`, `voice-lines`, `level-stats`, `hero-scale`, written after the first 12 lines Melty read; no footage review.
- Title "Risk of Deadlock" → Melty made slug `risk-of-deadlock-2`. `MeltyTest` now looks for `risk-of-deadlock-2.json` or `deadlock-of-rain.json`; lab_restore/run_bench remove both. Recipe checks = the first 12 written. Resubmitted (release `4ed38587-…`), retest `40cc813d-…` started.
- Listing: new tagline and description with links to the five kit pages.
**Not done / open:** the retest and its footage reviews; character select screenshots; the kit pages must be shared publicly by the creator (I can't change who can see a page); publish (ask first).
**Lessons:** a Melty rename changes the slug, and the test file is named after the slug. Check which check names Melty actually read (`selfChecks.passed`) before trusting a recipe list. Write doc-edit scripts with raw strings (`\t` in a Windows path became a tab).

### 2026-10-19 · Melty test passes · v0.6.3
**Request (verbatim):** "why is the game still opne?"; "all links should be accessable thorugh the link now"; "check the test"; "i think i messed with it just restrt test 2"
**Done:** explained the test opens and closes the game itself. Kit pages now public (docs 00, 06). Test `40cc813d-…`: run 1 pass (footage reviewed), run 2 failed: the creator opened the pause menu at 1:44 and browsed Settings, so the paused run never played the heroes. New 2-run test `e2e6d068-…`: 12/12 checks in both runs, both footage reviews pass, `graduates: true`.
**Not done / open:** publish (asking), character select screenshots for the listing.
**Lessons:** tell the creator up front to keep off the PC during a Melty test; Melty's own note says "play the mashup", which is wrong for a self-playing test mode. Melty can't rerun one run; a new 2-run test is the quickest redo.

### 2026-10-19 · published 0.6.3 · v0.6.3
**Request (verbatim):** publish → "Publish now"; screenshots → "Do it now", then mid-way "ill give you my screen shots", then four PNGs (Haze, Abrams, Seven, Infernus character select).
**Done:** `publish` → safety review → **live** (0.6.3, Discover). My own select-screen capture was abandoned (click coordinates off, a pause menu appeared, then the window closed); lab_restore and `winhttp.dll` back on. The creator's four shots uploaded (Haze cover), the four 0.1.0 shots removed; the new shots were still in Melty's picture check at the time of writing.
**Not done / open:** Infernus' select-screen lore says "Catalyst makes victims burn hotter" (`design/heroes.json`), stale since Napalm; fix in the next release. Watch player reports.
**Lessons:** for listing pictures, asking the creator for screenshots beats driving the game's UI blind.

### 2026-10-19 · Mo & Krill, the fifth hero · v0.7.0 (not released)
**Request (verbatim):** "lets see you 1 shot this. we need to rebuild vfx we need to take the animations we need to build these new moves we need to take the correct vfx form the wiki https://deadlock.wiki/Mo_%26_Krill lets question my descions and ask question do deep research on the file in deadlock and have a section for each actionable task for this character. please read our docs in the project there is good infomration on gotchas from other characters" (with a full design spec: Heavyweight, Yellow Canary, Sand Blast, Burrow, Dust Devil, Q Tag Team, matched to MUL-T). Also: "can you make this repo public!"
**Research:** heroes/abilities vdata, krill.vsndevts and VO, the glb (clips, meshes, the `rider_*` skeleton), particle colours, deadlock.wiki (no effect descriptions there; Krill holds the gun and does the talking), a code reuse map. Plan approved.
**Questions asked → answers:** size → "Abrams' height, ~2× wide (Recommended)"; Krill holds the gun, so M1 during Tag Team → "Mo punches instead"; Sand Blast shape → "Deadlock's cone, 20 m (Recommended)"; stage snag pass → "You play-check it"; Dust Devil look → "Try Deadlock's clip first (Recommended)"; Tag Team reward → "Reset each stage"; toughness → "Tankier: 250 / 25". Repo public: explained the history is clean, `gh` isn't installed; the creator can flip it in Settings, or I push and flip it after approval (no answer yet).
**Done:**
- Sheets: hero `mokrill` (modelScale 1.262, MUL-T capsule and camera, Heavyweight), stats, 5 skills, equipment (rider figure), 6 buffs, 6 effects, sounds (incl. `<skill>/<part>` rows), voice, hooks; new columns across heroes/skills/buffs/animations/equipment. Infernus' lore no longer says Catalyst.
- Code: kinds `burrow` (+`BurrowPop`), `cone_wave` (`DlrSandWave`), `dust_devil` (`Fx.Pull`), `ult_krill` (`DlrHopper`); fixed gun pattern, noGun, melee punch; prefix-aware gun rig; model scale, capsule/camera from a vanilla body, interaction reach; ImmuneToPhysicsForces, rider hiding, footsteps and landing thud; disarm (vanilla DisableAllSkills), maxHealthAdd, permanent stacks; Mo & Krill effects; letters-only object names; test expects `enemy_disarm`, `enemy_pulled`, `self_hidden`, punch and Heavyweight checks. Version 0.7.0.
- Cache rebuilt (74 sound events, 54 voice groups); `anim_names.json` gained mokrill. Kit page published (private, https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa); the other five pages republished with it in the navigation.
**Results:** `run_test.sh mokrill`: run 1 landed on a ledge (targets 15 m below; aim, scale, ragdoll, sounds, voice passed); run 2 every move worked but `gun-in-hand` (measured while Krill was scaled to 0) and `level-stats` (the Tag Team trophy) failed: both test fixes; run 3 **every check pass, 1/1**. Devshots: Dust Devil's first dust was an opaque brown wall, thinned; Deadlock's tornado clip reads as the arms-out spin. Bench: **1620 %/s vs MUL-T 1450, inside**; alone M1 443, M2 510, Shift 370, R 382; Tag Team 81000 % per use. Co-op test: hosted, joined and started the run, then **stopped by Claude Code for low memory** (two game copies); not rerun.
**Not done / open:** co-op check of Mo & Krill as a joined player; the creator's stage play-check; share the Mo & Krill page; release (recipe needs `mokrill-plays`, forced re-extract); repo visibility.
**Lessons:** RoR2's dust material is opaque; a display name with "&" broke the bench parser; bench and portraits fit in one launch; two game copies can run the PC out of memory.

### 2026-10-19 · Mo & Krill play-check pass · v0.7.0b (not released) · the commit that adds this entry
**Request (verbatim):** "Mo and Krill feedback
increase move speed by 20% (should be 70% of Abhrmas)
Burrow should last twice as long under ground and the damage when arising should be way more... can we get an animation for going underand coming back up?
Dust Devils spinning move Is cool but it should actually be mo and krill ultimate item, please scale the damage to same as the Preon Accumulator and make sure other chanatres can spin and do the same ultimate move!
Tag team should be reassigned to r and have a 30 second rest time don't change any damage do notchange any damage or timing numbers
Sand blash need a lot mor eumph better vfx of what is happening and clear cone indication of damage area and wind up pls!
run the dps test on just mo and krill"
On the plan, added: "i would like mo to also enter a frenzy state where move speed is double and the melle damge hits very hard with kockback! please give mo red vfx surronging him to show he is in that state!"
**Questions asked → answers:** move speed (+20 % is 6 m/s, 70 % of Abrams is 4.9: they disagree) → "Faster start, top speed 5 m/s"; Burrow pop-out → "3 × 500% = 1,500%"; Dust Devil as the ult at the Preon level → "8 s, 200% per tick (Recommended)"; Tag Team on R with no target → "Nothing, no cooldown (Recommended)"; Frenzy punch → "600%, strong knockback (Recommended)".
**Done:**
- Sheets: acceleration 40 → 80; Burrow 8 s, 500 %; Tag Team slot special, 30 s (damage and timing unchanged), kind `krill_hop`; Dust Devil slot equipment, 120 s, 8 s, 200 %, kind `ult_devil`, equipment row `eq_mokrill_dust_devil` (whole-Mo pickup figure); Sand Blast wind-up `minDuration` 0.4 / `duration` 0.7, shove `knockback` 8, `castLayer` Sandblast.Impact; new skills column `meleeKnock` (gun row 20) and punch 300 → 600 %; buff `krill_away` → "Frenzy", `moveSpeedAdd` +1; Tag Team voice 50 % at most every 20 s; hooks row `gun_hits` reads `meleeKnock`.
- Code: `UltDevil` (UltState; any carrier without the tornado clip turns its yaw 720°/s), `KrillHop` (lock-on, refund on no target), Burrow sink and BurrowPop rise (offsetting `modelBaseTransform`), Q underground in `Equipment.Fire`, `ConeWave` wind-up with a fixed direction, Sand Blast shove in `DlrSandWave`, Frenzy knockback in `GunHits`, the Frenzy aura in `HeroRuntime`; effects `telegraph`/`gather` (ConeMarker, solid orange lines following the ground), heavier Sand Blast and Burrow effects, `under`, Frenzy `hit` and `frenzy` aura; opaque dust swapped for smoke where it was big or dark.
- Test mode: the punch check moved to R and also checks Frenzy speed (×2) and knockback (> 2 m); new checks for no-target R, Q underground and Dust Devil on Commando (`ult-any-survivor`, spin ≥ 720°); a wind-up's first screenshot is taken during it.
- Pages: `kit_pages.py` maths for `krill_hop`/`ult_devil`, CHANGES v0.7.0b, analysis; six pages republished. Library `VERSION` v0.7.0b.
- Docs 00, 02 (kinds, effects, gotchas 19–21), 03 §9, 05 §3.5, 06, 08 §7b, MODLOG, README.
**Results:** `run_test.sh mokrill` ×4: run 1 every check pass but the screenshots showed black blobs (opaque dust) and a faint cone; run 2 pass, cone still faint on snow; run 3 landed on Distant Roost's cliff-edge spawn (targets below: Tag Team "no target", Burrow popped off the edge), not the code; run 4 **every check pass, 1/1**: Frenzy speed ×2.00, punched target thrown 24 m, R with no target kept its stock, Q underground → UltDevil and visible, Commando spun 5,030° and hit the pack. Bench (Mo & Krill only): **3252 %/s** vs MUL-T 1450 (was 1620): M1 448, M2 645, Shift 1617, R (Tag Team) 2100; Q 1380 (41,400 % per use). Far above the band, reported and not retuned (the creator set these numbers).
**Not done / open:** the creator's next play-check (feel of the Frenzy, Sand Blast's cone, the sink and rise); co-op rerun; release; repo visibility. Levers if he should come back into the band: Tag Team 30 → 60 s, Burrow 500 → 200 % (doc 05 §3.5).
**Lessons:** the game stayed open between my fix-and-rerun rounds: `lab_restore.sh` right after every `run_test.sh` (copy the devshots aside first); dark or big `mat_dust` makes black blobs; additive lines vanish on snow (use `Vfx.Solid`); a feedback note with two numbers that disagree is worth one question.

### 2026-10-19 · Mo & Krill co-op test · v0.7.0b · docs-only commit
**Request (verbatim):** "The co-op test hasn't been rerun yet. run them"; while it ran: "you are doing nothing on the test"
**Questions asked → answers:** only 1.6 GB of 15.4 GB free (two game copies need ~3 GB each; the first run was stopped for low memory) → "Run it anyway now".
**Done:** `bash tools/run_coop_test.sh mokrill`, then `lab_restore.sh`.
**Results:** every check pass: coop-hosts, coop-joined, coop-run, **coop-mokrill** (the joined player's Yellow Canary, Sand Blast (Sanded, disarm), Burrow (hidden, slow), Tag Team on R (2365 damage, stun, Frenzy buff), Dust Devil on Q (1672 damage, pull)), coop-shots-seen, coop-effects-seen; 88 relayed effects reached the joiner. It ran through despite the low free memory.
**Not done / open:** the creator's next play-check; release; repo visibility.
**Lessons:** the co-op run sits quietly for minutes after "host open" (no output until it ends): when asked, show progress from `%LOCALAPPDATA%/DeadlockOfRain/coop/step.txt` and the BepInEx log instead of waiting silently.

### 2026-10-19 · Thunderstore package (no Steam Workshop for RoR2) · v0.7.0 · the commit that adds this entry
**Request (verbatim):** "ok i want to upload this mod to steam work shop can we have a second verion of the app where we rip out key asset form the deadlock game so it can all be installed nativly without the two games let chat about it"; then "i want to extract the assets out of deadlock files", "can we  not ship with the deadlock assets?", "i wantthis to be on steam workshop".
**Questions asked → answers:** shipping extracted Deadlock assets was declined (Valve's copyrighted files; the project rule), offered Thunderstore / own stand-in art / both → the creator asked again for extraction and shipping, both explained; RoR2 has no Steam Workshop (checked), Thunderstore is its one-click hub → "Yes, plan Thunderstore (Recommended)"; first start → "Wait on loading screen (Recommended)".
**Done:**
- `plugin/Runtime/FirstRun.cs`: when `extract/DeadlockExtract.exe` sits beside the DLL (Thunderstore layout), find Deadlock (config "Deadlock folder", env, RoR2's Steam library, Steam via `reg.exe`, `libraryfolders.vdf`) and run the extractor; `LoadStaticContentAsync` waits for it with the loading bar, then opens the cache and starts the audio; a title-screen popup when the heroes can't be loaded. Melty build unchanged (no `extract/` beside the DLL).
- Extractor writes `progress.txt`; `Version` stays 1.2.0 (output unchanged, same stamp as Melty's). Exe republished.
- `tools/package.py <ver> --thunderstore`; `thunderstore/` manifest (BepInExPack 5.4.2122), store README, CHANGELOG; icon composed from our own renders.
- Docs 00, 01 §6.6/6.7, 02 §3, 03 §6b + §9 row, MODLOG, README; memory `thunderstore-not-workshop`.
**Results:** `build/DeadlockOfRain-0.7.0-thunderstore.zip` (40.5 MB, no Deadlock files). On this PC with the cache moved aside and the package's `extract/` in the plugin folder: first start read all five heroes on the loading screen (6 min, 74/74 sound events, 54/54 voice groups) and built them; that `run_test.sh haze` failed every damage check because Haze spawned in a void off the map (devshot; ragdoll fell 76 m), the environment, not the change. Rerun (second start: "Cache is current", ~1 s): **every check pass**. The new cache's stamp equals Melty's (`6774|1.2.0|7ED735FE7217DA40`).
**Not done / open:** the "Deadlock not found" popup was not seen in game (it needs Deadlock missing; the Deadlock install is never touched); not uploaded (the creator's thunderstore.io account and team, and a go-ahead); Thunderstore states no rule on the bundled 46 MB exe; nothing pushed. `%LOCALAPPDATA%\DeadlockOfRain\cache.thunderstore-test` (212 MB) is the test's copy, safe to delete.
**Lessons:** an `rm` with a glob after `cd` is blocked by the safety check: unzip into a fresh folder instead of emptying one.

### 2026-10-19 · "Risk of Deadlock" first Thunderstore release (four heroes), first-start log on screen · v0.7.0 · the commit that adds this entry
**Request (verbatim):** "can we have a mod page per character developed?"; then "let actually just have 1 mod with all characters!", "only all heros so online always works", "will the online work if a friend doesnt have the same characters installed?", "ok i just want HazeAbramsSevenInfernus for first release of all all pack of risk of deadlock"; during the test: "it seems stuck", "can we get some log for when ror2 is finding the deadlock files", "have it on the screen in game please".
**Questions asked → answers:** what "a mod page per character" means → "Separate Thunderstore mod per hero"; extraction scope and an all-heroes pack → replaced by "1 mod with all characters", first release Haze, Abrams, Seven, Infernus.
**Done:**
- `thunderstore/release.json` (name `Risk_of_Deadlock`, the four heroes); `package.py --thunderstore` writes `plugins/heroes.txt`, cuts the extractor manifest down with the new `extractor/manifest_owners.json` (`gen.py`), names the zip after the release, composes the icon from the listed heroes, and refuses store text naming a hero not in the release. Store README, CHANGELOG and manifest rewritten for four heroes.
- Plugin: `Survivors.Released` (heroes.txt, or every row); `Net.RegisterModList` at the end of content loading registers `melty.deadlockofrain;<version>;<built hero ids>`, so a player with other heroes or none is refused at the lobby; the test's content, voice and sound checks use the released heroes.
- First start: every place searched for Deadlock and every extractor line go to the BepInEx log; `FirstRunOverlay` shows the step, percentage (creeping within a step), time and the last lines on the loading screen (hidden on normal starts); Steam paths compared without case. csproj references `UnityEngine.IMGUIModule`. `hooks.json` `mod_list` row updated.
- Docs 00, 01 §5, 02 §3/§7.7, 03 §6b + §9 row, MODLOG, README.
**Results:** package `build/Risk_of_Deadlock-0.7.0-thunderstore.zip` (4 heroes, 32 images, 58 sound events, 44 voice groups, no Mo & Krill entries; icon of the four). In game (cache moved aside, the package's `extract/` + `heroes.txt` in the plugin folder), `run_test.sh haze` twice: run 1 read the four heroes (8 min 51 s), "Mo & Krill: not in this release", 4 survivors and 4 ults built, mod list `...;0.7.0;haze,abrams,seven,infernus`, every Haze check pass except voice-lines/sound-events (they still expected Mo & Krill: fixed); run 2 with the log and box: every check pass, read in 324 s (Abrams 197 s), the box seen on the loading screen (devshot). The Steam-path de-duplication was built after the last run (log-only change, not rerun).
**Not done / open:** upload (the creator's thunderstore.io team and go-ahead); the Thunderstore exe question; Mo & Krill in a later release; leftover test caches `%LOCALAPPDATA%\DeadlockOfRain\cache.rod-test`, `cache.rod-test2`, `cache.thunderstore-test` (~560 MB) can be deleted.
**Lessons:** Abrams alone takes 3–4 min to read, so a first start without visible progress looks hung: show progress on screen, not only in a log. C# backslash literals don't survive a bash heredoc: write such patches with the Write tool.

### 2026-10-19 · Deadlock update 6795 broke the heroes for players; fixed here, one hero can no longer stop the others · v0.7.1 · the commit that adds this entry
**Request (verbatim):** (Melty's progress pasted) "i donk know what the issue is but melty is tring to fix it", "what is the issue"; then "i would prefer we fix it here! and also can we make sure this doesnt happen again, so if 1 character fails they dont all fail. update to the latest deadlock update"; then "straight away upload to melty with the 4original characters".
**Questions asked → answers:** none (plan approved as written).
**Done:**
- `player_logs`: 3 players on 0.6.3, "Unknown KVType for variable 'datatype': 26" on Haze's model with Deadlock build 6795; extractor 1.2.0 aborted, no ready.json, no heroes. Melty had started its own fix (0.6.4 uploaded, untested, unpublished).
- VRF 20.0.6980 → 20.1.7467 (no code change needed); extractor 1.3.0 reads each hero on its own (`<id>.tmp` swapped in on success, `HERO FAILED` + stack, ready.json `failed`, exit 1 only when no hero was read; a sound event that throws is caught per event, a failed sound step leaves the heroes).
- Plugin 0.7.1: `DeadlockCache.Failed` from ready.json, `Survivors` names the reason when skipping, title-screen box "<names> couldn't be read from this Deadlock version; the other heroes are ready" (`FirstRun.ProblemHeader`), also on Melty starts.
- `package.py <ver> --release`: the Melty zip cut to `thunderstore/release.json`'s heroes (heroes.txt + cut manifest), for the four-hero Melty release.
- Docs 00, 01 (§1, §6.6, §6.7 "after a Deadlock update"), 02 §3, 03 §9, MODLOG, Thunderstore CHANGELOG.
**Results:** full re-read from 6795: 5/5 heroes, 40 images, 74 sounds, 54 voice groups, 9 min 40 s. Against the old cache: VRF 20.1 also writes normal maps (unused, ModelBuilder clears them); Haze's head-smoke texture now has its cutout alpha (it was an opaque black card); Infernus' glow textures differ only in encoding. Broken-hero test (a bogus Haze model path): Haze in `failed`, Infernus read, exit 0. `run_test.sh haze`: every check pass, 1/1 heroes. Melty zip `DeadlockOfRain-0.7.1.zip` (Haze, Abrams, Seven, Infernus; manifest hash FD43C19E6B46D5A4).
**Melty:** "straight away upload to melty with the 4original characters" → `package.py 0.7.1 --release`, uploaded, release `2dcdb0d2-…`; asked "Publish it to players now?" → "Publish now (Recommended)"; published, **live 2026-10-19 15:06 UTC**, `report_bug_fix` recorded against it. Thunderstore zip `Risk_of_Deadlock-0.7.1-thunderstore.zip` built.
**Not done / open:** confirm 0.7.1 live; Thunderstore 0.7.1 zip and re-upload (0.7.0 was rejected, "Invalid submission"); `cache.pre-0.7.1` kept for comparison (can be deleted).
**Lessons:** a Deadlock update can change Valve's file format overnight: watch `player_logs`, update VRF, and never let one hero's failure take down the rest.

### 2026-10-19 · Mo & Krill balance pass and Sand Blast look · v0.7.1c (dev) · the commit that adds this entry
**Request (verbatim):** "we were working on https://claude.ai/artifact/XwhDWMMq5RfHb7bxPgE1Aa fe thing to note. let increase thier speed even more like 90% of abhrams. lets make under the ground movement really fast in Burrow.Sand Blast need a visual overhaul i need to see what it is hitting make it very obvious its sand ffs also increase damage and knockback! Dust Devil need some good damage out put it a fucking ult"; then "when you are done with the change dont do the test just open the game and let me play to see the balance".
**Questions asked → answers:** none (plan with the numbers approved as written).
**Done:**
- Sheets: mokrill `baseMoveSpeed` 5 → 6.3; `burrowed.moveSpeedAdd` 1 → 2 (Burrow ×3, row `moveSpeedMult` 3); Sand Blast 450 → 750 %, knockback 8 → 22; Dust Devil 200 → 450 %, proc 0.3 → 0.5; descriptions; new material row `mat_sand_overlay`; `mokrill_sand` notes.
- `Vfx.cs`: filled sand cone under the outline (`ConeMarker.Fill`, fades with it); tall opaque sand wall with forward grains and settling dust trail; `Spray` from Mo's hands on the throw; bigger hit bursts with a ring and flash; `SandedFx` coats the enemy's model (TemporaryOverlay) while Sanded and removes it after.
- Docs 06, 05 §0 (retest flag), 00 §1/§9.
**Results:** `gen.py` PREFLIGHT CLEAN; build succeeded. Installed and RoR2 started through Steam for the creator. Not tested in game (their request).
**Not done / open:** their verdict; `run_test.sh mokrill` and the Mo & Krill bench; the kit page republish once the numbers settle.
**Lessons:** none new.

### 2026-10-19 · Mo & Krill toned down a little · v0.7.1c (dev) · the commit that adds this entry
**Request (verbatim):** "ok maybe a little bit too over powered now lets dial back the 
Tag Team gamage from Krill  and also let -50 form mo and krill base health make the changes and open the game again"
**Questions asked → answers:** none ("dial back" read as about a third: 1500 → 1000 %, splash 500 → 300 %).
**Done:** Tag Team `damagePct` 1000, `splashPct` 300, description; mokrill `baseMaxHealth` 250 → 200; docs 06, 05 §0, 00.
**Results:** PREFLIGHT CLEAN, build succeeded, installed, RoR2 started through Steam for the creator (the game was already closed). Not tested in game (their request).
**Not done / open:** as the entry before: their verdict, then test, bench, page.
**Lessons:** none new.

### 2026-10-19 · Tag Team nerf, Sand Blast on slopes, health from every kill, bench · v0.7.1c (dev) · the commit that adds this entry
**Request (verbatim):** "ok let really amke sure Tag Team Krill nerfed so he need like 2 bounces on a basic enemy to kill them, no nerf to mo and his punching!, also let bring the cool down to 20 seconds and the duration to 8....can we plase make sure Sand Blast travels upat and  angle r down angle or along any floor to damaage enemys please!"; "please run damage testing and suggest edits to fix!"; (pasted the Tag Team page text) "also is ot only tage team that increase health? it shoudl be all kill that increase their health pool that why it starts low!", "all kill by that character!"
**Questions asked → answers:** none yet (suggestions asked after the bench).
**Done:**
- Tag Team 400 % + 150 % splash (level 1: 44 per landing, a Lemurian/Beetle has 80), duration 8, cooldown 20; `krill_away` 8 s. Frenzy punches unchanged.
- `DlrSandWave` walks the ground in 0.5 m steps (rises up to 1.2 m per step, any drop; a higher rise is a wall) and checks each enemy's height against the ground it passed (±4 m), so it climbs and descends slopes and hits there.
- Trophies: `DlrHopper.InitTrophies` (Plugin start): every kill by a hero with a krill_hop row gives that row's debuff stacks; the hopper's own death hook is gone. Descriptions (passive, Tag Team), buff name "Trophies", hooks.json.
- Bench `run_bench.sh 30 mokrill` (built before the slope and trophy changes; neither changes the flat bench); library VERSION v0.7.1c, merged.
**Results:** all 2317 %/s (was 3252), M1 443, Sand Blast 800, Burrow 1567, Tag Team 1367, Dust Devil 3330 (99900 % per use on 12). MUL-T 1450: +867, above the ±500 band; Abrams 2460, Seven 3164.
**Not done / open:** the creator's pick from the suggestions; `run_test.sh mokrill` (slopes, trophies); page republish.
**Lessons:** a terrain-following wave must step along the ground; a flat ray at the start height reads every uphill slope as a wall.
