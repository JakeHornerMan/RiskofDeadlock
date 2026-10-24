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
**Questions asked → answers:** after the bench, fix the +867 over MUL-T (Burrow 3×300 % recommended, or Burrow 3×350 % + Sand Blast 600 %, or leave it) → "Leave it as is". Game started again for the creator with this build.
**Done:**
- Tag Team 400 % + 150 % splash (level 1: 44 per landing, a Lemurian/Beetle has 80), duration 8, cooldown 20; `krill_away` 8 s. Frenzy punches unchanged.
- `DlrSandWave` walks the ground in 0.5 m steps (rises up to 1.2 m per step, any drop; a higher rise is a wall) and checks each enemy's height against the ground it passed (±4 m), so it climbs and descends slopes and hits there.
- Trophies: `DlrHopper.InitTrophies` (Plugin start): every kill by a hero with a krill_hop row gives that row's debuff stacks; the hopper's own death hook is gone. Descriptions (passive, Tag Team), buff name "Trophies", hooks.json.
- Bench `run_bench.sh 30 mokrill` (built before the slope and trophy changes; neither changes the flat bench); library VERSION v0.7.1c, merged.
**Results:** all 2317 %/s (was 3252), M1 443, Sand Blast 800, Burrow 1567, Tag Team 1367, Dust Devil 3330 (99900 % per use on 12). MUL-T 1450: +867, above the ±500 band; Abrams 2460, Seven 3164.
**Not done / open:** the creator's pick from the suggestions; `run_test.sh mokrill` (slopes, trophies); page republish.
**Lessons:** a terrain-following wave must step along the ground; a flat ray at the start height reads every uphill slope as a wall.

### 2026-10-19 · Jump during Dust Devil · v0.7.1c (dev) · the commit that adds this entry
**Request (verbatim):** "while in the 
Dust Devil ability for alllcharactere i would like to be able to jump"
**Questions asked → answers:** none.
**Done:** `UltDevil` jumps on the jump press for any carrier (all its jumps, Feathers included) via `GenericCharacterMain.ApplyJumpVelocity`; sheet description, docs 02 and 06.
**Results:** PREFLIGHT CLEAN, build succeeded; the creator had the game open and the running game locks the DLL, so it is not installed yet (the game was not closed). Not tested in game.
**Not done / open:** as before.
**Lessons:** none new.

### 2026-10-19 · Sand Blast 400 %, Dust Devil 5 s, bench and test again · v0.7.1c (dev) · the commit that adds this entry
**Request (verbatim):** "ok snad blast need to be brought down to 400, and Dust Devil shoudl only last 5 seconds!"; "re run the test after!"
**Questions asked → answers:** none.
**Done:** Sand Blast `damagePct` 400; Dust Devil `duration` 5 (20 × 450 % = 9000 % per enemy, above the 6000 floor); descriptions; bench merged into the library; docs 06, 05 §0.
**Results:** bench all **2227 %/s** (M1 427, Sand Blast 333, Burrow 1700, Tag Team 1017, Dust Devil 1980) vs MUL-T 1450. `run_test.sh mokrill`: first run spawned below the map (ragdoll at −24 m, every move 0 damage, the environment); second run **every check pass, 1/1 heroes** (covers Sand Blast's slope walking, Tag Team 400 %, trophies code, Dust Devil jump build).
**Not done / open:** the kit page republish once the creator settles the numbers; Sand Blast hit 25 times in 5 uses (32 before the slope change): watch it in play.
**Lessons:** none new (a below-the-map spawn is the known environment fault: rerun).

### 2026-10-19 · Burrow 14 s, softer Frenzy punches, test, pages · v0.7.1e (dev) · the commit that adds this entry
**Request (verbatim):** "burrow cool down timer need to be brough up to 14 seconds and i think tage team mos melee punche need less knock back. please run the test and the update the artifact with all our changes"
**Questions asked → answers:** none ("less" read as half: 20 → 10 m/s).
**Done:** Burrow `cooldown` 14; Yellow Canary row `meleeKnock` 10 (the Frenzy punch); library VERSION v0.7.1e; `kit_pages.py` v0.7.1c change list and analysis for Mo & Krill; Mo & Krill page and balance overview republished to their links; docs 06, 05.
**Results:** `run_test.sh mokrill`: every check pass, 1/1 (punch throws 10.9 m). Bench: first try "no flat spot" on the stage, retry: all **2425 %/s** (M1 443, Sand Blast 333, Burrow 1150, Tag Team 1407, Dust Devil 1905); Tag Team rose (1017 → 1407) because monsters knocked half as far stay in punch reach. Game left closed (the test closed the creator's copy, at their request to test).
**Not done / open:** the creator's next play.
**Lessons:** weaker knockback can raise damage: what is not thrown away stays in reach.

### 2026-10-19 · Mods off on Steam Play after tests; lab_restore keeps BepInEx on · v0.7.1e (dev) · the commit that adds this entry
**Request (verbatim):** "play the game! and why the fuck do the mods not start when i press play on steam!"
**Questions asked → answers:** none.
**Done:** cause: `lab_restore.sh` removed `winhttp.dll` after every test and bench (copying Melty's between-launch state), so Steam's Play started RoR2 without BepInEx; Melty also does this between its own launches. `lab_restore.sh` now keeps BepInEx on (restores `winhttp.dll` if missing); `MELTY_OFF=1` for the old behaviour. Doc 03 §4. Game started for the creator.
**Results:** five heroes built, no errors in the log.
**Not done / open:** if Melty launches the game it may switch BepInEx off again (its own behaviour).
**Lessons:** after any test, check `winhttp.dll` is there before telling the creator a build is ready for Steam Play.

### 2026-10-19 · Trophies: shown on the page, verified, fixed; health 180 · v0.7.1f (dev) · the commit that adds this entry
**Request (verbatim):** (pasted the Heavyweight card) "why the fuck is there no ention of the health gain on kill and how you lose it after each stage also please validate the code that this is how it works. 2+ for ecah kill in the health kill no health game just to max health, when we change stages all accimalated health is reset back to base!"; "make starting health 180 for mo and krill"
**Questions asked → answers:** run the test now (closes their game) or wait → "Close it and test now (Recommended)".
**Done:**
- Code read: kills → `DlrHopper.OnKill` adds permanent `tag_team_health` stacks (+2 each, ×10 boss, max 100); a new stage spawns a new body, so they reset. **Bug found:** `StatsPatch` added the max after vanilla's heal/clamp, so every recalculation cut health back to base; fixed with a prefix/postfix pair (doc 02 §12). A gain never healed (as wanted) and still doesn't.
- `MeltyTest`: new check `trophies` (10 stacks: max up, no heal, full health kept through recalculations).
- Page: the passive card shows Per kill / Boss kill / Up to / Lasts the stage tiles and the maths ("max health only", "back to base every stage"); passive text says it too. Mo & Krill page republished.
- `baseMaxHealth` 200 → 180. Docs 02, 06.
**Results:** `run_test.sh mokrill` ×3: run 1 below-the-map spawn (0 damage everywhere; trophies **pass** 200 → 220, 100 → 100 on gain, 220 kept); run 2 a bad spot (targets 15–18 m, Mo on a ledge; Sand Blast, Burrow, Dust Devil missed; trophies pass, and it showed 4 stacks already from the test's own kills); run 3 **every check pass, 1/1**, trophies 180 → 200, no heal, kept.
**Not done / open:** Sand Blast missed targets 18 m away from a ledge in run 2: watch it in play from high ground.
**Lessons:** a stat added in a `RecalculateStats` postfix bypasses vanilla's heal/clamp and gets clamped next time: carry health across it.

### 2026-10-19 · Fallback system: one hero never blocks, red ! tile, built-with versions; five heroes to Melty · v0.7.2 · the commit that adds this entry
**Request (verbatim):** (pasted Melty's player logs: v0.6.3 "Unknown KVType ... 26"; v0.7.1 "infernus: model not found: models/heroes_wip/inferno/inferno.vmdl" on Deadlock build 5977) "I though we already designed a system that if we could not find a particular character and it file reference we skip them and do the rest no failure maybe just a nice red ! where their profile photo would have been and maybe a popup... what happened to the logs on loading aswell that told the user what we were loding and our progress. no I don't want a blocker I want as many character we can find on initial load also on release I want  to store my version of risk of rain 2 and my version of deadlock and if anything go wrong in this loading process I want you to show up thei popup . this information should also be included in the readme when we push an update and also in on the melty page on which version I am building from" / "any other way we can prevent stupidity or just have a better process please suggest. main thing is fix the error if 1 character cannot be completed we dont fail the whole system!" / mid-work: "add these fall back system to the claude.md" and "and then lets relese all 5 characters to melty!"
**Questions asked → answers:** missing hero in character select → "Red ! tile, locked (Recommended)"; look models up from Deadlock's hero list → "Yes, look it up (Recommended)".
**Done:**
- Found: the per-hero skip already worked in the live 0.7.1 (the pasted crash was 0.6.3); the new failure was a moved model on the player's older Deadlock.
- Extractor 1.4.0: model from `heroes.vdata` + folder fallbacks (`ready.json` `modelPaths`, `--list-models`), progress lines for Melty's setup, icons can't fail the run.
- Plugin: `Content/MissingHeroes.cs` (locked red "!" tile, hover = reason + versions; hooks `missing_hero_lock`, `missing_hero_tile`); `Runtime/BuiltWith.cs`; versions in every problem message; `FirstRun.ShowSummary` (Melty loading box when a hero is skipped); no "missing image" warnings for unreleased heroes' buffs; test mode `"break"` + check `missing-hero-tile`, `BREAK=` in `run_test.sh`.
- `package.py`: `built-with.txt` in every zip, `thunderstore/built-with.json`, "Built with" line in README.md / thunderstore README / CHANGELOG; refuses a wrong `Plugin.Version`, a missing CHANGELOG section or unreadable versions.
- CLAUDE.md "Fallback system" section; docs 00, 01, 02, 03, MODLOG.
**Results:** extractor fault run: Infernus pointed at the wrong folder was found, a fake hero skipped, exit 0. Full cache 5/5 on Deadlock 6795 (stamp `6795|1.4.0|544C18F87BF82B69`). `BREAK=infernus run_test.sh haze`: every check pass incl. `missing-hero-tile`, red "!" tile in the lobby shot. The first run showed "this PC: Risk of Rain 2 build ?" (acf looked one folder too high): fixed, in the packaged DLL. Package 0.7.2: built with RoR2 25921972 / Deadlock 6795.
**Not done / open:** Melty 0.7.2 upload, test and publish (next, creator said release); the 5977 player should get Infernus back once the vdata or folder lookup finds him (unverified on their build).
**Lessons:** check which version each pasted log comes from before fixing; heredocs here mangle `\n` even when quoted.

### 2026-10-19 · Thunderstore moderator asked why an exe; GitHub now builds it in public · no version bump · the commit that adds this entry
**Request (verbatim):** (pasted the Discord thread: Harb "Why does the extractor need to be it's own exe?" ... "This is c# code, you could load it as library or just directly embed it into your dll") "what are some approaches we can do to appease this moderator. lets chat!" / then "do you think the .exe is the best way for this is there a way we could explain this to them... i do believe this uy is just bieng a gate keeper. what are dthe downsides of doing the plan we just created it seem like a lot fo work if valve updates their game" / "can i have a link to the .exe in my github" / "have GitHub build the exe"
**Questions asked → answers:**
- Approach → "Rewrite into the DLL". A plan was written: a netstandard2.1 reader with a vendored, patched VRF slice, DLM written straight from VRF objects, and no Skia/glTF.
- The creator then said "let keep this is a .md for now pls!" (plan file only) and chose to explain the exe instead.
**Done:**
- `.github/workflows/extractor.yml`: Windows runner, the doc 01 §6.7 publish line, tag/Version check, sha256, a Release with what the exe does and why it's an exe.
- `tools/fetch_extractor.py`; `package.py --thunderstore` checks the exe against `thunderstore/extractor.json`.
- README and store README "About DeadlockExtract.exe" section.
- Docs 00, 01, 03, CLAUDE.md, MODLOG.
- Pushed `main` and the tag `extractor-1.4.0`.
**Results:**
- GitHub's build passed: https://github.com/JakeHornerMan/RiskofDeadlock/releases/tag/extractor-1.4.0, SHA-256 `175b8845…0475a1`.
- `fetch_extractor.py 1.4.0` hash OK.
- The GitHub exe read Haze on Deadlock 6795 in 37 s, exit 0, and its `haze.dlm` is byte-identical to the local build's.
- `package.py 0.7.2 --thunderstore` packs that exe (same hash in the zip).
**Not done / open:**
- The creator's reply to Harb and the Thunderstore re-upload.
- The DLL plan is kept only as notes (the plan file and this entry); it is the fallback if any exe is refused.
**Lessons:** a store moderator's "make it a library" can be far more work than it sounds for a .NET 10 dependency. Weigh the upkeep on every game update, and offer verifiable public builds first.

### 2026-10-19 · Melty 0.7.2 with all five heroes; the test spot fixed · v0.7.2 · the commit that adds this entry
**Request (verbatim):** "and then lets relese all 5 characters to melty!"; during the tests: "what is happening?", "what going on now? it seemed to have stopped!", "the guy is tanding still wtf?", "there something wrong with the tests", "the character is not under the floor", "ok so are we gonna test and upload or what??? can you fix the fucking test?"
**Questions asked → answers:** after the first failed test: retest, publish now, or both → "Retest, then publish (Recommended)".
**Done:**
- Melty 0.7.2 (full five-hero zip, `heroes.txt` with every hero so 0.7.1's four-hero list is overwritten), recipe with `mokrill-plays` and `trophies`, 12-minute runs.
- Two Melty tests failed in the same place on Distant Roost: the test spot 14 m ahead of the drop pod sat above a slope, so the summoned targets slid 17 m away and every hit check failed while the hero stood idle. I first said "under the map"; the creator corrected it (the hero was on the ground). Fix: `MeltyTest.FindArena` picks a spot with ground under the hero and level, visible ground under both targets (old spot, landing, then nearby ground nodes in 8 directions); `STAGE=` test option.
- Footage reviewed for every run; listing description rewritten for five heroes with the red "!" and "Built with" lines; published.
**Results:** `STAGE=blackbeach` and `STAGE=blackbeach2 run_test.sh haze`: targets at 10 m, pass. Melty test `3dd01dab-…`: run 1 (Siphoned Forest) and run 2 (Distant Roost) every check pass, 5/5 heroes, reviews pass, `graduates: true`. Published; Melty's safety review was checking it when this was written.
**Not done / open:** confirm 0.7.2 went live (`mod_status`); check `player_logs` for the Deadlock 5977 player; Melty still flags "players hit bugs" (the "fix all bugs" pass, if the creator wants it).
**Lessons:** a test that fails the same way at the same coordinates is a test bug: look at the recording before calling it bad luck, and don't guess what the picture shows (the hero was on a slope, not under the floor).

### 2026-10-19 · Write-up of the public exe build · docs only · the commit that adds this entry
**Request (verbatim):** "ok please write some documentation on what we just did!"
**Done:** `docs/09-PUBLIC-EXE-BUILD.md`: why, the options weighed, what was built, the first release, how to release and verify, and the DLL fallback plan in full as an appendix, since the plan file was overwritten. Linked from CLAUDE.md, doc 03 layout and doc 00 §9.

### 2026-10-19 · A player's 0.7.2 logs; clean sound shutdown · v0.7.3 · the commit that adds this entry
**Request (verbatim):** the creator pasted a player's 0.7.2 extract.log (plus the older 0.6.3 and 0.7.1 logs), then: "upload to melty when you are done!"
**Questions asked → answers:** none.
**Done:**
- Read Melty: 0.7.2 is live. The player's setup read 5/5 heroes on Deadlock 6795 (the fallback and model lookup work on a player's PC). The one current issue is that player's game crashing in ntdll (c0000005) while quitting, 228 s in.
- `DeadlockAudio.Shutdown` now joins the mixer thread (0.5 s), also on `Application.quitting`, frees its buffers and logs "Deadlock audio stopped".
- Docs 00, 02, MODLOG, CHANGELOG 0.7.3.
**Results:**
- `run_test.sh haze`: every check passes, 1/1 heroes (run twice).
- Quit tests on this PC: quitting after a test run crashes in ntdll even with the fix (the audio stop is logged first). An unmodded quit, a BepInEx-only quit and a modded title-screen quit did not crash. Windows' log has the same crash on this PC since 2026-10-13.
- The dump scan shows the crash in Mono's runtime shutdown, with no winmm or mod thread alive.
- Melty 1-run test of 0.7.3: everything passed except `mokrill-plays`, twice on test timing: (1) Burrow-then-Dust-Devil fired Burrow while the R no-target state still held the body (now: back to idle, then wait up to 1.5 s for him to be under); (2) the Tag Team punch target died part-way (thrown 1.9 m), because Tag Team had already hurt it (now healed to full first). `run_test.sh mokrill`: all pass, thrown 9.1 m. Published at the creator's word ("ok just publish it when it passes").
**Not done / open:** the quit crash itself (bisect plan in doc 00 §9). The 0.7.3 change doesn't fix it and isn't described as a fix.
**Lessons:** reproduce a crash before calling a change its fix. Quitting with `CloseMainWindow()` after a test run reproduces this one in about 7 minutes. Bash heredocs still mangle backslashes in scripts: write Python with the Write tool.

### 2026-10-20 · Hero pages back on the Melty listing · listing only · the commit that adds this entry
**Request (verbatim):** "put the fucking artifacts on the melty page!"
**Done:** the 0.7.2 description rewrite had dropped the links to the five kit pages and the balance page. They are back in a "Hero pages" section (`update_mod`), and the Built-with line now says v0.7.3. CLAUDE.md now says to keep that section on every rewrite.
**Lessons:** when rewriting a listing, start from the current text and keep every section unless the creator asks to remove it.

### 2026-10-20 · The crash on quit, found and fixed · v0.7.4 (branch hotfix-0.7.4) · the commit that adds this entry
**Request (verbatim):** (pasted Melty player logs: 0.7.3 extract logs, 0.6.3 "Unknown KVType ... 26" setup failures, 0.7.1 "closes right after starting" / "error loading its code") then "release as soon as you are done!"; "yes run the overlay test"; "i want you to work independantly now! please make the best decions and keep testing the game al you want. let fix this issue onece and for all for all pc set ups"; "if you need me to interviene with some test please write it in a document but honestly you shoudl just use mcp to resolve it"; "you shoudl be working harder not stagnant on a death screen"; "do deep research use lots of subagent on all corder of the code we have implemented and find solution and test out asumptions and issues!"; "if you are not testing anything dont leave the game open"; "when you have done all you can please upload to meshy [Melty], but please do everything take a full day if you need to explore the code and the program!"; "that was me messing with the game run the test again"; "test and upload to melty when you are done"
**Questions asked → answers:** quick fix vs cause → "Find the real cause first"; release → "Hotfix 0.7.4 now (Recommended)"; after the overlay couldn't be removed → "Keep digging first". The creator switched the Steam overlay off for RoR2 (Steam still loads its DLL, so that test was void).
**Done:**
- Melty: every pasted error but one was an old version's (fixed in 0.7.1/0.7.2); 0.7.3 setups read 5/5 heroes. The live issue: `crash:crashed:ntdll.dll`.
- Branch `hotfix-0.7.4` (worktree) from 3471d78 so Viscous stays out. `tools/quit_test.sh`; test-mode `off` switches and `vanilla` run.
- Dumps resolved with Microsoft/Unity symbols (scratch scripts, not in the repo); four research agents (code audit, dump unwind, web, RoR2 quit order).
- `Runtime/QuitSafety.cs`: skip RoR2's `UnitySystemConsoleRedirector.Disengage`; nop Mono's restore_stack `push rbp` (ported from RoR2BepInExPack, MIT; notice in THIRD-PARTY-NOTICES.txt, now also beside the DLL via `package.py`).
- Docs 00, 02 (§8, §12 gotcha 22), 03, MODLOG, CHANGELOG 0.7.4.
**Results:**
- Before: baseline 2/3 crashed; our OnDestroy guard 4/4; Harmony+audio off 2/2; pre-compiling 1/2; mod off (Commando run) 2/2.
- With RoR2BepInExPack's nop patch: no crash, and Player.log showed the StackOverflowException looping in BepInEx's `LoggedTextWriter.WriteLine` ↔ `SyncTextWriter.WriteLine`: the root cause.
- After the fix: console fix alone 3/3 clean with no overflow; both fixes 2/2; mod off 1/1 (all reach Unity's final "Memory Statistics"). `BREAK=infernus run_test.sh haze`: all 18 checks pass, `missing-hero-tile` pass.
- Melty: release `9697f980-…` (same recipe as 0.7.3), test `1cd1f52b-…` passed 2/2 (Siphoned Forest, Verdant Falls), both footage reviews pass, `graduates: true`, Windows logged no crash for either close. Published at the creator's word ("test and upload to melty when you are done"); in Melty's safety review at the time of writing. Listing: only the Built-with line changed (v0.7.4), Hero pages kept. `report_bug_fix` for `crash:crashed:ntdll.dll`.
**Not done / open:** merge into `main` (main's working tree has uncommitted Viscous work, so not done in this session); confirm 0.7.4 is live (`mod_status`); watch `install_outcomes` for 0.7.4 quits.
**Lessons:** run the no-mod control early; repeat intermittent crashes 3+ times per condition; a native crash after a Mono stack overflow hides a managed trace that RoR2BepInExPack's patch reveals; Melty players don't get RoR2BepInExPack's fixes.

### 2026-10-20 · 0.7.4 merged into main, security hardening, tests · main (0.8.0 in progress) · commits 7baa5f1, eb14fb4, 998012c and the one that adds this entry
**Request (verbatim):** "merge it into main and do loads of fucking testt make sure its all scure and wokring!"
**Questions asked → answers:** none.
**Done:**
- Main's uncommitted Viscous work stashed, `hotfix-0.7.4` merged (7baa5f1; the only conflict, `design/hooks.json`, keeps Viscous's five rows + `keep_console_on_quit`), stash restored (Plugin.cs stays 0.8.0, CHANGELOG 0.8.0 above 0.7.4); the Viscous work is uncommitted as before. The merge kept the hotfix's generated hook rows: `gen.py` (PREFLIGHT CLEAN) regenerated `Sheets.g.cs` (eb14fb4).
- Security review (agent): safe to ship; hardening done anyway (998012c): Mono patch only on the verified build (SizeOfImage 0xA54000 + TimeDateStamp 0x66FD8CF6 via GetModuleHandle) and only after VirtualQuery shows committed executable code; console type from RoR2's assembly; `quit_test.sh` removes the test file; `run_test.sh` validates BREAK/STAGE/OFF; notices beside the Thunderstore DLL too. Doc 02 gotcha 22.
- Cache re-read with extractor 1.5.0 (Melty's 0.7.4 test had rebuilt it with 1.4.0): 6/6 heroes, stamp `6795|1.5.0|E2F6C2203AC8C931`.
**Results:**
- `quit_test.sh` with all six heroes: **6/6 heroes, 24/24 checks pass** (incl. viscous-plays), QUIT_OK, both fixes applied.
- `quit_test.sh haze` ×3: QUIT_OK, 0 fails, 0 overflows, clean Unity shutdown each time.
- Melty: 0.7.4 live since 12:49 UTC; first day 7 plays, 6 worked, **0 crashed**, 0 failed installs (0.7.3: 2 of 4 crashed).
**Not done / open:** Claude Code stopped the near-vanilla quit and the `BREAK=infernus` check (low memory, ~1.3 GB free); not rerun without asking. Co-op test not run (two game copies need more free memory than this PC had). The hardening is on main only; the live 0.7.4 doesn't have it (the review said it isn't needed to ship). Drop `git stash` "viscous wip before 0.7.4 merge" once the Viscous work is committed.
**Lessons:** after merging a branch that regenerated `Sheets.g.cs`, run `gen.py` again: the merge can silently keep the wrong side's generated rows. A Melty test of an older release rebuilds the cache with that release's extractor.

### 2026-10-20 · Viscous after the creator's first play, DPS and pictures on his page · v0.8.0b/c (not released) · the commit that adds this entry
**Request (verbatim):** (pasted) "Viscous feedback / screen shake on landing is way to much like literally like 10% of what it is / I want the The Cube to stop all animation as they are trpped ina  cube and I actually want the character engulfed in a goo cube / Puddle Punch I want an actual fist come out of the ground and I want the damage delt to be twice what it is no / Goo Ball I want more control of the ball and I want it to last for 15 seconds / M2 Splatter Ability the surface area does not sit on the floo or align with the floor it just sit on top of things, it should always stick on the flor and go through enemys and bosses damaging them / why the fi his head no made out of gooaswell and why is there no images on the artifact so rebuild the articafct when you ahev done all the changes and dps testing!" then "do a dps test and then update therir artifact, also why are there no fucking photos or image son the fucking artifact!"; during the work: "have you ran the dps test on Viscous? can we please update his artifact please!" and "also the cube goo explosion need like double the damage when Viscous excape and let put base damage of the gun at 15 instead. re run dps test and update the artifacts and comparison and for fuck say will you put some images on the Visous artifact!"
**Questions asked → answers:** none (plan approved as written). "Base damage of the gun at 15" was read as the hero's base damage stat (the "Damage 12" on his page), not the gun's 120 %.
**Done:**
- An earlier session had already built every feedback item, but it never committed, recorded or republished them: landShake 0.2, The Cube frozen in a cel-goo block (`DeadlockAnimator.Freeze`), the cel-goo fist and Puddle Punch 800 %, Goo Ball 15 s with grip −0.3, Splatter world-only flight with `DlrLob.Floor` and the draped puddle, goo head and glass helmet (`ModelBuilder.GooPart`). Checked in game this session and committed here.
- 0.8.0c: Viscous base damage 12 → 15 (+3/lvl), The Cube burst 300 → 600 %.
- Why the page had no pictures: the live Viscous page was its first version, built before his renders existed, and was never republished. Now published (v3) with his portrait, the Goo Ball figure and a new studio render of Puddle Punch's fist on its card (`DpsBench` renders `<skill>_fx`, `kit_pages.py` shows any `<skill id>_fx.webp`; `Vfx.StudioView` so CelGoo shades for the studio camera).
- All seven pages republished (version line, nav link to Viscous, balance page with Viscous). `kit_pages.py` `LAST_USE` keeps Mo & Krill's one-use Q numbers, which the last-bench-only file had dropped.
- Docs 02, 05 (§0 row, §3.6), 06 (Viscous section), MODLOG, 00.
**Results:**
- `run_test.sh viscous` (0.8.0b): every check pass, 1/1 heroes. Screenshots: goo cube with him frozen inside, puddle lying on the floor, goo head in the helmet. The test camera never catches the fist (it lands at his feet), so it was checked in the studio render.
- Bench 0.8.0b: 1912, 1942, 1585. Bench 0.8.0c: **2172, 2225, 2312, mean 2236** vs REX 1738 (+498, inside by 2). Real damage is 25 % above that (base 15 vs 12). The Cube 303 → 560, Splatter 1451, punch 871, gun 400, Q 2062.
- `run_test.sh viscous` after 0.8.0c: every check pass, 1/1 heroes (Cube test damage 144 → 274); `lab_restore.sh` run, the game is closed, the build stays installed.
**Not done / open:**
- The creator has to share the Viscous page ("Anyone with the link").
- The vanilla portraits (Commando, Loader, MUL-T, REX, Artificer's body) still render empty (no material); one fix was tried and reverted (doc 02).
- Viscous co-op and the Melty release are not done.
**Lessons:** when a page is built from local work, publish it in the same session, or the creator sees the old one. `kit_pages.py` reads only the last bench file for one-use Q numbers. Diff the live page (images stripped) before republishing, which caught the lost Mo & Krill row.

### 2026-10-20 · Viscous: puddle ammo, 30 s puddles, Cube puddle, bigger goo explosions · v0.8.0d (not released) · the commit that adds this entry
**Request (verbatim):** "ok i want 20% turn up on the voice lines for Viscous, i walso want teh goo explosion to be more visuall powerfull and leave behind a puddle just like Splatter. now if you are atsing in one of those splashes on the floor/ puddle i want the ammo to be +20 i would also like al puuble to stay for 10 second and begin shrinking to nothing over another 20 second so theyr ecan be use for extra ammo for 20 seconds. now if you change puddle you get another 20+ ammo without reloading! so kinda making this puddle travel system to prevent reloads! also can we make the Goo Ball slightl;y less controllable and mayebe cutr the damage down to 75% of what it is now. please do a dps bench amrk on viscous and up date the artifact please includ ethe goo standing machanic in the passive section aswell. also let revent the M1 / Deep Sea Interpreter / Weapon to previous values please!"; then "the goo explosion on the Splatter and The Cube need to be more visuall impactful!"; then "remove the picture of the Puddle Punch"
**Questions asked → answers:** puddle ammo → "+20 rounds per new puddle (Recommended)" (on top of the magazine, up to 40, once per puddle); revert M1 → "Base damage back to 12 (Recommended)" (the gun's own row never changed); Goo Ball 75 % → "All its damage (Recommended)".
**Done:**
- Sheets: skills columns `puddleAmmo` and `puddleShrink` (Splatter 20 / 20, `chainCount` 6 → 10); The Cube `extra` `viscous_splatter`, `reach` 10 (its puddle); Goo Ball 1500 / 375 / slam 1125, buff grip −0.3 → −0.5; heroes `voiceGain` (Viscous 1.2); Viscous base damage 12 +2.4; Gooey passive text.
- Code: `DlrPuddle` shrinks (radius and pool, `Vfx.SetShrink`) and gives the owner rounds once per puddle (`HeroRuntime.GiveAmmo`, refill effect + reload sound); `GooCube.Burst` plants the puddle; `Vfx.BigSplash` for Splatter and the Cube (blooming dome, glob crown, rings, flash); `Voice.Play` × voiceGain; test step for puddle ammo.
- Pages: the passive card shows the puddle mechanic; the Puddle Punch picture is off (no effect pictures on cards); Viscous and balance pages republished.
- Docs 02, 05 (§0, §3.7), 06, MODLOG, 00.
**Results:** `run_test.sh viscous`: every check pass, 1/1 heroes, puddle ammo empty → 20 → 40 with no reload. Bench 2607 / 2367 / 2293, mean **2422** vs REX 1738 (+684, above the band); Splatter alone 1924 (1451 before), because puddles that reach the bench's spot refill it; Goo Ball 1419 (2062).
**Not done / open:** he is above the band: the creator chooses whether to pull Splatter 200 → 150 %. The explosion and puddle-shrink looks are not captured in the devshots (the shot timing misses them). Not checked in co-op; the refill only runs on the owner's machine (by design). The creator still has to share the Viscous page.
**Lessons:** a puddle near the bench's fixed spot changes the bench (more throws), so a passive that refills ammo shows up in the numbers even with no movement.

### 2026-10-20 · Viscous: puddles as an ammo supply, 8 s puddles, Splatter 150 %, higher jump · v0.8.0e (not released) · the commit that adds this entry
**Request (verbatim):** "OK FEW TWEAKS THE PUDDLE S YOU CAN STAND IN NOW START SHRINKNG ON 3 SECOND AND COMPLELY SHRINK BY 5 SECONDS SO A TOTAL OF 8 SECONDS. WHEN YOU STAND IN A PUDDLE ITS NOT PERMANENT+20 IT ONLY WHEN YOU ARE STANDING IN THE PUDDLE THE PUDDLE IS A RESOURCE SUPLLYING MORE AMMO, ALSO LET LESSEN THE DAMMAGE OF THE SPLATTER EXPLOSION! ALSO MAKE THIS vISCOUS GUY JUMP HIGHER ONLY BY LIKE 33% OR SOMETHING!"; then "RE DO DAMAGE DPS AND UPDATE THE ARTIFACT!"
**Questions asked → answers:** none. Read as: each puddle holds 20 rounds and tops the magazine up (never above 20) while he stands in it, until used up; "lessen" Splatter = 200 → 150 % (the lever offered last time); jump power 15 → 20 (+33 %).
**Done:** Splatter `chainCount` 3, `puddleShrink` 5, `damagePct` 150; Viscous `baseJumpPower` 20; `HeroRuntime.TopUp` replaces `GiveAmmo`; `DlrPuddle` keeps a per-puddle supply drawn while the owner stands in it; the test step checks fill, a used-up puddle, and a fresh one; passive text and page card; docs 02, 05 (§0, §3.8), 06, MODLOG, 00.
**Results:** `run_test.sh viscous` every check pass, 1/1 heroes; puddle supply empty → 20, same puddle again → 0, new puddle → 20, not reloading. Bench 2273 / 2213 / 2272, mean **2253** vs REX 1738 (+515, 15 above the band). Viscous and balance pages republished.
**Not done / open:** 15 %/s above the band (Puddle Punch 700 % or Splatter 140 % would bring him in); the creator still has to share the Viscous page; Viscous co-op and the Melty release.

### 2026-10-21 · Viscous: Puddle Punch leaves a puddle, every puddle 20 % smaller · v0.8.0f (not released) · the commit that adds this entry
**Request (verbatim):** "Puddle Punch should alse place a puddle lets make all puddles 20% smaller"
**Questions asked → answers:** none. Read as: the punch leaves a Splatter-size puddle; 20 % off every puddle's width.
**Done:** Puddle Punch `extra` `viscous_splatter`, `reach` 6.4 (`DlrPunch.Strike` plants it on every machine); Splatter `reach` 8 → 6.4; The Cube `reach` 10 → 8; descriptions and passive text; the test step clears leftover puddles first; docs 02, 05 (§0, §3.9), 06, MODLOG, 00.
**Results:** first `run_test.sh viscous` failed `viscous-plays` on the puddle-ammo step (20/20/20): Puddle Punch's own test had just left a puddle at his feet, which refilled him. A test-order problem, not a mechanic bug; the step now clears old puddles first. The re-run with that fix was stopped by Claude Code for low memory before any check reported (the test game was closed with lab_restore.sh), so the fixed test step itself is **not yet confirmed in game**. Bench 1660 / 1805 / 2410, mean **1958** vs REX 1738 (+220, inside). Viscous and balance pages republished.
**Not done / open:** the creator still has to share the Viscous page; Viscous co-op and the Melty release.
**Lessons:** when a move starts leaving something in the world (a puddle), later test steps that assume a clean spot need to clear it.

### 2026-10-21 · Rat King, the seventh hero, in one go · v0.9.0 (not released) · commits a031ff9 and the one that adds this entry
**Request (verbatim):** (pasted) "Rat King — Survivor Design Spec ... Captain is the match ... Visual priority 1 — The cape ... Visual priority 2 — The rat ... M1 — Sovereign's Decree ... M2 — Scrap Grenade ... Shift — Royal Pestments ... R — Rule, Ratannia! ... Q — Rat Swarm ... Balance budget ... Build notes for Claude Code ... Open questions" (the full spec is in the session transcript); then "go with you best descision i accept the plan please develop the character with new rebuild vfx and pull our correct sfx please read all the documenetation and 1 shot this character, run a dps analysis on the character and then try to build an artifact comparison with all other characters includeing full images and all. please follow the structure we have done so far for making characters, and also please if you have any question just choose your best guess and continue, i am going to bed i want to wake yup to a fully playable rat king character in ror2! go now make the guy!"
**Questions asked → answers:** M2 controls → "no jsut right click readys it and any click will throw it, lmb or rmb i beleiev its the same on viscous"; extras (banner aura, Shift barrier, Q armor shred) → "just damage and stun please!"; the uncommitted Viscous work → "viscous dont worry about that character that will be done by the time we start implementing!" (it was still uncommitted when the work started, so it was committed on its own first, ad7ad86, as found).
**Done:**
- Deadlock lookup with `tools/vrfdump` (its build was broken by a stray CR in a comment; fixed): `hero_ratking`, `models/heroes_wip/ratking/ratking.vmdl`, abilities, 58 sound events, voice groups, the rat (`ratking_minion`), grenade and banner models (doc 01).
- Sheets: Rat King in every sheet (Captain's stats +20 health, +10 armor, 2 jumps; five skills; Rat Swarm equipment; two slows; sounds and voice). New columns: `heroes.splitMeshes`, `heroes.airJumpMult`, `animations.springRoots/springStiffness/springDamping/springGravity/propBones/muzzleForward`, `effects.meshes/clips`, `skills.seekRange`.
- Extractor 1.6.0 (local publish): props with a skeleton and clips, split hero meshes.
- Code: `SpringBones` (cape and tail), `RatCrowd`/`PropModels` (baked-mesh rats, animated props), `RatKingParts` (ammo belt, ring, swarm, slam burst, planted banner, the hopping Scrap Grenade), states `lob_scrap`, `rat_ring`, `banner_leap`, `ult_swarm`, gun pattern `ratking` with falloff, King's Leap (higher air jump and a tucked spin), Rat King's effects (`VfxRatKing.cs`). Shared fix: clips fold root_motion's rotation into its children (gotcha 23). Vanilla matches are rendered from their body models (MUL-T now renders). Test-mode pose and grenade log lines.
- Pages: new Rat King page (private until shared), all eight pages rebuilt and republished (nav, v0.9.0, Captain in the overview, Viscous's moves from the library means).
- Docs 01, 02, 03, 05, 06, 08, MODLOG, README, CLAUDE.md, CHANGELOG 0.9.0, `Plugin.Version` 0.9.0.
**Results:**
- `run_test.sh ratking`: run 1 everything but the grenade; the grenade flew 170 m past the targets (thrown from a muzzle 3 m off to the side along the camera ray) and he lay on his back in play. Fixed (aim at the crosshair point; root fold; muzzle tip). `run_test.sh` all seven heroes: **7/7 heroes, every check pass**, Rat King upright (head 1.67 m).
- Bench (3 runs in one launch): **2959 / 2981 / 3058, mean 2999** vs Captain 3490: −491, inside the band by 9. M1 547, M2 1170, Shift 509, R 1035, Q 2073.
- Portraits: Rat King and his figure render; of the vanilla matches only MUL-T does.
**Not done / open:** the creator has to share the Rat King page ("Anyone with the link"); the cape, the double-jump spin and the slam's banner are unseen in a play session (the devshots catch the ring, the swarm and the pose, not the cape in motion); co-op not run; extractor 1.6.0 is a local build (a release needs the `extractor-1.6.0` tag and a forced re-extract); vanilla portraits other than MUL-T still empty; no Melty release.
**Lessons:** a muzzle bone at the grip makes the gun rig bend the spine; a clip without a root track inherits the last root turn; background test watchers get reaped when the session is idle (watch in the foreground; leading sleeps are refused, loop on the log line).

### 2026-10-21 · Rat King's first play-check · v0.9.0b (not released) · the commit that adds this entry
**Request (verbatim):** "ok its very good the art king few things!, the foot step noises need to reduced by liek 90% they are alot, the double jump is cool is that part of deadlock? in King's Leap Innate Rat King can double jump, and the second jump goes 30% higher: a tucked spin with his cape swept up behind him. also the flip is happening on the first jump if we can tie the flip to the second jump that would be better !Rule, Ratannia! Ability · new in 0.4 can we make sure we have all the correct texture applied! Royal Pestments Ability Rats pour out from under your cape and run in circles around you for 6s, a 10m ring on the ground that follows you. Every enemy inside (up to 4m off the ground) is bitten for 25% damage four times a second. can we double the damage of this move! [pasted: the Rat Swarm card from his page] does this need to have a time limit can we keep the rat with the player until they have attached on to enough enemys. how rats disappear is if the rat attach to a enemy and the enemy dies, eithert from other damage or form the arst chipping away at their health!"; then mid-work "un dps and update artifact when done!".
**Questions asked → answers:** spare rats that never find an enemy → "Stay until stage ends" (recommended; Q again tops back up to 50); per-enemy cap → "bosses can have 10" (5 per enemy, 10 on bosses). Double jump in Deadlock (answered, no code): every Deadlock hero can air-jump with stamina; Rat King has his own jump (`citadel_ability_jump_ratking`, air jump 75 %); the 30 % higher jump and the spin came from the spec.
**Done:**
- Footsteps: `ratking_gun/step` gain 0.1; the landing thud moved to a new row `ratking_gun/land` (gain 1) that `HeroRuntime` uses when it exists.
- King's Leap flip only on the air jump: the owner flips on `jumpCount` rising past 1 in the air; other machines on a sharp climb only after 0.25 s airborne.
- Banner textures: its earth (`ratking_ult_diorama`) and garbage (`ratking_garbage`) have a 1×1 white texture in Deadlock and take their colour from vertex colours. Extractor 1.6.1 writes DLM2 (vertex colours, each material's tint, alpha-test, glow and vertex-colour flag read from the `.vmat`); the plugin reads DLM1 and DLM2 and turns on RoR2's `_ColorsOn`/`VERTEXCOLOR` for those parts. Props only, and the glow unused (see lessons). Local exe; cache rebuilt twice.
- Royal Pestments 25 → 50 %. Rat Swarm: no time limit, a rat bites until its enemy dies and is then gone, 5 per enemy and 10 on bosses (`bossMult` 2), spares stay to the end of the stage, Q tops up to 50, the scurry loop only while rats hunt; preflight counts the swarm over 15 s.
- Pages: R's "new in 0.4" label only on the first four heroes; Rat King notes, numbers; all eight pages republished. Docs 01, 02, 05, 06, MODLOG, CHANGELOG, this entry, doc 00.
**Results:**
- `run_test.sh` all seven heroes (the model reader is shared): Rat King every check pass; Mo & Krill failed once on "Q underground: was under False" (Burrow's hide not seen in time; nothing here touches it) and passed alone right after (`run_test.sh mokrill`). `run_test.sh ratking` again for screenshots: pass; the banner's base is dark earth, no longer white. Log: `vertex colours: shader Hopoo Games/Deferred/Standard, _ColorsOn yes`.
- Bench (3 runs): **3567 / 3189 / 3869, mean 3542** vs Captain 3490: +52, inside (was 2999). M1 547, M2 1373, Shift 889 (509), R 1123, Q 4618 (2073 with the 15 s limit).
**Not done / open:** the flip and the swarm's new rules are checked by the test only as "the move works", not by eye; co-op not run; extractor 1.6.1 is a local build (a release needs the `extractor-1.6.1` tag and players' setup re-reads Deadlock); Deadlock's masked glows (slime, eyes) not shown; the creator still has to share the Rat King and Viscous pages.
**Lessons:** a Deadlock material setting applied to the whole material can wreck a hero (the first 1.6.1 cache gave Infernus' clothes a full red glow, Abrams' head white, Viscous' glass black): check every hero's materials after an extractor change (`python tools/dlm_mats.py`) before testing, and keep new material behaviour to the models that need it. The Mo & Krill underground check can miss once on a full run; re-run that hero alone before chasing it.

### 2026-10-21 · Royal Pestments calls out the rats by name · v0.9.0c (not released) · the commit that adds this entry
**Request (verbatim):** "ok now wehnwe use Royal Pestments i want to add some sfx 1 after anotehr https://deadlock.wiki/Rat_King/Voice_lines#Rat_Swarm the ones with names so just keep plying those linkes 1 after another!"; mid-work, with a screenshot of a striped block flag: "wtf did you do to the model of the flag... i want tjhe fucking model form deadlock!!!!"; then "ok im done playing, do the rebuild".
**Questions asked → answers:** none (read as: during Royal Pestments, chain the Rat Swarm "Use" lines that name a rat, one after another).
**Done:**
- The wiki's Rat Swarm "Use" lines are `ratking_use_power1_01..125`; all but 106, 111, 114, 116, 117 name a rat. Our 0.9.0 voice rows had power 1 and 2 swapped (Scrap Grenade said Rat Swarm's lines): Scrap Grenade now `use_power2`, Rat Swarm (Q) and Royal Pestments `use_power1`.
- `voice.json` columns `chain` (seconds of lines back to back) and `skipTakes`; Royal Pestments chains 6 s, skipping the five unnamed. `Voice`: a shuffled deck per row (no repeats until all have played), the `Chain` coroutine, a chain blocks unimportant lines.
- Extractor 1.6.2: voice files named by Deadlock's take number; every take for `voiceAllTakes` groups (gen.py: chained/skipping rows), the first 24 for the rest. `DeadlockAudio`: take numbers (`Takes`, `Play(..., take)`), mono files share one buffer. `package.py` trims `voiceAllTakes` per release.
- The flag: not our doing to the model. The creator started the game at 12:52 while the 1.6.2 cache rebuild ran (done 12:53); the props folder was gone for that minute (log: `effect model ratking_banner_prop not in the cache`), so `RatBanner.StandIn` drew the placeholder pole and block flag (a copy of the template material, so Commando's texture shows through in stripes). Restarting the game with the finished cache gives Deadlock's banner. Lesson in doc 00 §7.
**Results:** cache 1.6.2: 7/7 heroes, 74/74 voice groups, `ratking_use_power1` 125 takes (no other group has more than 24, so the cut changed nothing else). `run_test.sh ratking` pass; one Royal Pestments: takes 73, 14, 70 at 0.0, 2.4, 5.3 s ("Make 'em bleed, Donna!", "Nacho's comin'...", "Say hello to Denise.").
**Not done / open:** extractor 1.6.2 local only (release tag `extractor-1.6.2`); the placeholder banner could say more clearly that the cache is incomplete.
**Lessons:** never rebuild the cache while the creator may play (doc 00 §7); check the wiki's voice page for which `use_powerN` is which move.

### 2026-10-21 · Puddles on the floor, ammo counter for every hero · v0.9.0d (not released) · the commit that adds this entry
**Request (verbatim):** "lets work on viscous... all puddle must land on the floor! i found the bug with the Puddle Punch ability when it was triggered in the air! also can we please on all deadlock character please add a ammo counter right about the weapon slot, just a ??/?? just for infomration."
**Questions asked → answers:** may I launch the game for an automated test of all seven (the counter is shared code)? → "Yes, test all 7".
**Done:**
- Cause: `DlrPuddle.Plant` looked only 8 m down for the floor, so Puddle Punch (and The Cube) used high in the air left the puddle floating. New `DlrPuddle.Floor` (1000 m down, from 1 m above): every puddle lies on the floor below where it was made, none over a bottomless pit; `DlrPunch` and the Puddle Punch aim ring use it too.
- Ammo counter: `Runtime/AmmoCounter.cs`, a postfix on `RoR2.UI.SkillIcon.LateUpdate` puts "rounds/magazine" just above the M1 icon (skill bar font, grey while reloading) for any hero whose gun has a `clipSize`, only for the body this machine moves; hook row `ammo_counter` in `design/hooks.json`.
- Test checks `ammo-counter` (each hero: magazine set to clip − 3, the label must say it) and `puddle-on-floor` (a puddle made 25 m up must land within 1 m of his feet).
- Docs 02, 05 (row, no re-bench: the bench fights on the ground), 06, MODLOG, CHANGELOG, doc 00.
**Results:** `run_test.sh` all seven heroes: **7/7, every check pass**; counters 22/25, 6/9, 26/29, 24/27, 17/20, 17/20, 21/24; the 25 m puddle landed 0.02 m from his feet. Game closed, `lab_restore` run, final DLL installed.
**Not done / open:** the counter's look not seen by eye (no screenshot checked); co-op not run (the counter is local only); no Melty release; pages not republished (no number changed).
**Lessons:** a ground ray with a short reach is a floating-object bug waiting for someone to use the move in the air; look far.

### 2026-10-21 · Puddle rounds in front of the magazine · v0.9.0e (not released) · the commit that adds this entry
**Request (verbatim):** "so with viscous you need to allow a {number}+??/?? when they are standing in a puddle. {number}++ is what goes down first and as soon as viscous leave that puddle the {number}++ disappears!"
**Questions asked → answers:** walking in with 5/20, should the puddle refill the magazine (as before) or sit in front of it? → "Puddle spent first" (20+5/20, 3 shots 17+5/20, leave 5/20). Test and bench? → "Don't launch".
**Done:** `HeroRuntime.TopUp` replaced by `Extra`, `CanFire`, `Rounds`, `Spend` (puddle rounds first, then the magazine; they fire even while empty or reloading); `DlrPuddle.Supply`/`Draw`, supply set at planting, the refill effect on first stepping in; guns and Splatter spend through `Spend`; the counter puts the puddle rounds in front in goo green (`20+5/20`); the test's puddle-ammo block rewritten for the new rule (5 in the magazine, 20+5 in a puddle, 17+5 after 3, 0+2 after 20 more, a new puddle 20, stepping out 0+2). Docs 02, 05 (open row), 06, MODLOG, doc 00.
**Results:** builds. Not run in game (the creator asked not to launch); build installed.
**Not done / open:** `run_test.sh viscous` and `run_bench.sh 30 viscous` (the puddle no longer refills the magazine, so his DPS may drop a little); the counter's look and the green part not seen by eye; pages not republished.
**Lessons:** a counter request can be a rules change: "goes down first" and "disappears when he leaves" meant the puddle no longer fills the magazine; ask with a worked example (5/20 walking in) before building.

### 2026-10-21 · Goo Ball 100 s and the 0.9.0 Melty release without Rat King · v0.9.0f · the commit that adds this entry
**Request (verbatim):** "let reduce viscous item recharge to 100 second and no test just let upload to melty this new build but with no rat kind please... keep this picture in but grey him out!"
**Questions asked → answers:** none. Read "this picture" as Rat King's portrait tile in character select: kept, greyed out and locked.
**Done:**
- Goo Ball cooldown 120 → 100 s; preflight now allows equipment ults 60-120 s (was exactly 120).
- Coming-soon tiles: `Survivors.ComingSoon` (`coming-soon.txt` beside the DLL), `MissingHeroes.Add(..., why: null)`: locked, his own portrait greyed, "coming soon" hover. `package.py --melty` cuts to `release.json` `melty` (six heroes), keeps only the coming-soon heroes' portraits in the manifest, writes `coming-soon.txt`.
- Melty: zip 0.9.0 (40.6 MB, sha256 5c72e642…), upload `25e1a80c-c979-4106-9e59-70d8b120d52d`, release `310d6466-e084-4f27-920c-5d33fb19c6ad` (one click yes, the usual executable-code review), listing description and tagline for six heroes with Hero pages and the Built-with line, **published: in Melty's safety review**.
- Docs 02, 03, 05 (open row), 06, MODLOG, this entry, doc 00.
**Results:** builds, preflight clean, the zip holds six heroes and only Rat King's portrait. **Not tested in game** (the creator: no test; no BREAK run either).
**Not done / open:** the grey tile, the counter and the puddle rounds never seen in game; Goo Ball's Q DPS not re-benched; check `install_outcomes` / `player_logs` next session; the creator still has to share the Viscous and Rat King pages; extractor 1.6.2 is a local build (Thunderstore needs the GitHub tag).
**Lessons:** a release can carry a hero as "coming soon" with only its portrait (the cut manifest keeps that one image).

### 2026-10-21 · Daily watch: game updates and Melty breaks · no version · the commit that adds this entry
**Request (verbatim):** "i want bto work on git actions is there a way we can tell is the deadlock or risk of rain game has been updated? can we run this every day? and if deadlock update can we start a claude instancce that check files that will validate our mod still works on melty.ai"
**Questions asked → answers:** where should it run (GitHub's machines can't have Deadlock)? → "Pc only now and can we please also pull the melty ai to see if there is any breaks aswell so we can start and instance to fix that also. the breaks need to be relavent to the current version". Claude key → "My Claude subscription" (the local login; no secret needed for PC only). After the check → "Report and draft a fix". May it start RoR2 for the in-game test? → "Yes, if the game isn't open or anyother game on my steam, also push the fix to melty when you are done!"
**Done:** `tools/watch/` with `daily.ps1` (lock, any-Steam-game guard, local build numbers vs `build/watch/state.json`, Melty triage, fix run on branch `watch/<date>`, report, notification), `triage-prompt.md` and `triage-schema.json`, `fix-prompt.md`, `claude-settings.json` (allow list), and `install.ps1`. Task "DeadlockOfRain daily watch" is registered daily at 04:00; the baseline is seeded (RoR2 25921972, Deadlock 26097978 / 6807). Docs: 03 §5b, CLAUDE.md (publish exception, command), doc 00, MODLOG.
**Results:** both scripts parse cleanly. `-DryRun` stopped correctly: "a game is running (Steam app 1422450)" (the creator was playing Deadlock). Build reading was checked on its own. The triage run against Melty with the JSON schema: live 0.7.4, no breaks (the old 0.6.3 setup failures and the 0.7.2/0.7.3 crashes were left out as not current), 1.6 cents.
**Not done / open:** the fix run was never run end to end (it starts the game, and the creator was playing). Try `daily.ps1 -NoPublish` after setting an older Deadlock build in `build/watch/state.json`, while nobody plays. `gh` isn't installed, so reports go only to `build/watch/` and a notification (`winget install GitHub.cli` + `gh auth login` adds issues).
**Lessons:** in `claude -p`, the Melty MCP's own instructions turn the final answer into a prose report, so ask for structured output with `--output-format json --json-schema` and read `structured_output`. Windows PowerShell 5.1 drops bare `"` from native arguments, so escape them as `\"`. Under `2>&1`, `$ErrorActionPreference='Stop'` turns git's stderr into errors.

### 2026-10-21 · Daily watch documentation · no version · the commit that adds this entry
**Request (verbatim):** "please write documentation on this system!" (after "SO HOW DOES THIS WORK?", which was answered in plain words in chat)
**Questions asked → answers:** none.
**Done:** new `docs/10-DAILY-WATCH.md`, the full manual: why it runs on the PC, a day's flow diagram, guards, build numbers and `state.json`, triage, the fix run's steps, permissions table and standing permissions, files, install/run commands, a practice run, troubleshooting, limits. Linked from CLAUDE.md "Read these first", doc 03 §5b and doc 00 §9.
**Results:** docs only.
**Not done / open:** the practice fix run (doc 10 §8).
**Lessons:** none.

### 2026-10-21 · Seven redesign, part 1: yellow, Lightning Ball slow, Static Charge 225 %, Storm Cloud on R, rig fixes · v0.9.1a · the commit that adds this entry
**Request (verbatim):** "ok i want a complete redesign of sevem [Lightning Ball's Deadlock text] so this now need to be recouled to suit deadlock deisgn the yellow, there need to be a ball and a clear sphere of influentce and when it get close to enemy it will slow donw my -90% and doe damage this ball flie though tha air and will pass through enemies but ... M2 Lightning Ball let start with"; then "Static Charge ... let recolor this and cut the damage in half!"; then "[Power Surge] let reseign this to the ultimate, so now seven flies into the air and there is a shere of influenece and it shocks eveerything in side, space to ho higher abd shirt to go lower put teh control on screen when we are in this stae now add this to the plan"; while building: "shift does not mke him go lower! als i prefer the old visuals of the m1 bullet the arcs of electricty try again", "maybe 50% slower speed when incontact with an enemy for m2", "please make all the s projectile yellow just like in deadlock!", "seven rig and character model is getting pretty broken acn you please amek sure he stays stable and matches his animation in deadlock!"
**Questions asked → answers:** ball on the world → "Keeps bouncing"; what the -90 % slows → the ball; radius → "Keep 10 m"; yellow scope → "Just the ball for now ... and the bullet form Shock Rifle"; where the flying storm goes → R (Power Surge removed, Q Thunderhead stays); shooting while flying → "No, he only channels and he cant do anything else while in this state"; numbers → "10 s, 15 m, 100%/0.5 s ... cooldown 24 seconds"; run the test and bench now → yes; where the rig looks broken → "check all his rig seem to be twisting in weird ways!"
**Done:**
- Lightning Ball: slows to `moveSpeedMult` (0.5, the creator's second pick after 0.1) of its speed while an enemy is within its radius (`DlrProjectile`, looked up every 0.1 s, eased); yellow core (Artificer's bomb, `tint`), crackles, bolts and a clear yellow `mat_shell` sphere of its 10 m reach; yellow `storm_strike_seven` impacts.
- Static Charge 450 → 225 %, yellow ring and thunderbolt.
- R **Storm Cloud** (`storm_flight`, `StormFlight` on the Body machine) replaces Power Surge (state, `power_surge` buff, `tesla_arc`/`seven_surge` rows, the gun's arc hook and `HeroRuntime.NextSurge` removed): rises 10 m, flies 10 s, Jump up / Sprint (Shift) or Utility down at 12 m/s, 40 m ceiling, 80 % sideways drift, skills locked, 100 % every 0.5 s within 15 m + Static, cooldownOnEnd 24 s, soft landing; `FlightHint` shows the player's own Jump/Sprint keys and a time bar; `seven_flight` visuals (sphere drawn from outside plus RoR2's area dome as a ground ring, since the camera sits inside it). Storm Cloud sounds and voice line.
- Shock Rifle keeps Huntress' electric tracer (the creator turned down Captain's golden one), recoloured yellow through the new effects `tint` column (`Fx.Tinted` copies, `Fx.TintedDefs` in the content pack, `Vfx.Recolor`/`RecolorMat`, which keeps each RoR2 ramp's brightness and alpha in the new hue).
- Rig: M2 and Shift gestures now use Deadlock's `lightning_ball_upper_body` / `static_charge_upper_body` (the full-body clip on the upper-body layer twisted his chest; cache rebuilt); the flight turns through `characterDirection.moveVector` and only from a clearly sideways aim (it had whipped round 1430° in one flight).
- Test mode waits the whole flight and sinks for its last 3 s (`storm flight: sank`). Docs 02 (kinds, gotchas 24-26), 04 §8, 05 (§0 row, §3.12), 06, this entry, doc 00, MODLOG; `kit_pages.py`; Seven's page and the balance page republished.
**Results:** `run_test.sh seven` every check pass (five runs while fixing; the last with everything in). He sank 9.8 m of 9.8 m. Spin in the flight 1430° → 453° (the rest is aiming). Bench one run: all **2827** vs Artificer 4013 (−1186, below; was 3164), M1 660, M2 812 (was 390), Shift 383 (was 765), R 710 (new), Q 907. Library `VERSION` v0.9.1a.
**Not done / open:** Seven is below the band; levers in doc 05 §3.12 (the creator's call). The ball's yellow core and the yellow impact passed the test but weren't seen on screen (the bench cleared the screenshots); the tracer's new yellow strength not seen either. Not checked in co-op (`run_coop_test.sh seven`). Thunderhead, the Shock Rifle's own behaviour and the passive are still to redesign. Not released.
**Lessons:** a "twisting" hero is often a full-body clip on the upper-body layer: Deadlock ships `*_upper_body` clips. The test's `spun` number catches a flight that whips round. Watch the creator's keys: on their PC Shift is Sprint and Utility is Mouse 4, so "Shift" meant Sprint.

### 2026-10-22 · Seven: Storm Cloud hits 3, Thunderhead's own icon, every clip re-picked, model-space gestures · v0.9.1b · the commit that adds this entry
**Request (verbatim):** "[Static Charge's text] or [Thunderhead's page text] you need a new image for the icon the both cant ahve the same! also let rebuild all seven animations. tegher somthing gone wrong with the animations  would rather you clear all animation and find them again!"; while working: "does R Storm Cloud Ability · new in 0.9.1 hit more than 1 person at a time? it should hit up to 3 people every time"
**Questions asked → answers:** start Risk of Rain 2 for tests while Deadlock is open (all seven heroes, the animation fix is shared, plus Seven's bench) → "Yes, run them now (Recommended)".
**Done:**
- Storm Cloud: the `chainCount` (3) nearest enemies within 15 m per shock (`Fx.Enemies` max), the description says so; it hit every enemy in reach before.
- Thunderhead's icon is Deadlock's Surge of Power item picture (`items/spirit/surge_of_power_psd`), picked by eye from eight candidates (`tools/vrfdump` gained a `png` mode); preflight now refuses two skills of one hero with the same icon. Storm Cloud keeps `giga_storm`.
- Seven's clips, all re-picked from scratch: every one of his 366 Deadlock clips was extracted to a scratch folder (not the live cache) and measured (hips/shoulders/gun yaw, twist, frame jumps, head angle from the idle's). `weapon_*`/`primary_*` locomotion are the same data: kept. Two real faults found:
  1. **Our gesture layer mixed in local space.** Deadlock lays upper-body clips on in model space. `static_charge_upper_body` has its own hips turned ~100°, so his head ended up 90-120° sideways (Deadlock: within 25°); Lightning Ball's head went to 59° (Deadlock 21°). New `ModelSpaceGesture` (all heroes): the head angle equals the clip's own on every hero's gestures, and never gets worse (Haze melee 86° → 4°, jump 48° → 4°).
  2. **Deadlock's Storm Cloud clips spin him a full turn** in the pelvis. New `steadyClips` column removes that turn when the clips are built (`Steady`); his limbs still move.
  - The flight now plays Deadlock's current `storm_cloud_intro` → `storm_cloud_scalar` hover (arms down) on `special_start`/`special_loop`, and blends out when he lands. Thunderhead casts with `ability_power_surge` (it shared `ability_storm_cloud_start` with the R).
- Fixed on the way: the Rat King crowd builds clips with no animation row (my first lookup crashed it). The equipment test now accepts any 60-120 s ult (Viscous' 100 s Goo Ball had failed it since 0.9.0f, untested then). `gen.py` stops when preflight crashes (it had silently generated). `tools/vrfdump/Program.cs` had a raw carriage return in a comment that broke its build.
- Cache rebuilt (7/7). Docs 01, 02 (§6, gotcha 24 corrected, 27, storm_flight), 05 (§0, §3.13), 06, this entry, 00, MODLOG; `kit_pages.py`; Seven's page (v16) and the balance page (v22) republished.
**Results:** `run_test.sh` all seven heroes: 6/7 on the first run (the Rat King crash and the old Viscous cooldown check), then `ratking,viscous` 2/2 and `seven` 1/1, every check pass. Screenshots: he hovers upright, facing forward, arms down; the Static Charge throw faces forward; Q shows the new icon. Bench (one run, v0.9.1b): all **1759** vs Artificer 4013 (−2254, far below; 2827 before), R 710 → 190, M1 570, M2 868, Shift 420.
**Not done / open:** Seven's balance is the creator's call (levers in doc 05 §3.13). Not checked in co-op (`run_coop_test.sh seven`). Not released.
**Lessons:** a hero "twisting" may not be the clip at all: compare the head angle of the clip alone with the mixed pose (scratch scripts `clipscan.py`, `chest.py`, `stick.py` this session; the method is in doc 02 gotcha 24). Extract every clip to a scratch `--out` to measure clips the sheet doesn't use yet; it leaves the live cache alone.

### 2026-10-22 · Daily watch: Deadlock 6812 checked, stun fix · no version bump, not released · the commit that adds this entry (branch `watch/2026-10-22`)
**Request (verbatim):** "daily watch 2026-10-22: Deadlock updated: build 26097978 -> 26118388, ClientVersion 6807 -> 6812" (the first real unattended fix run, started by `tools/watch/daily.ps1`; nobody at the PC)
**Questions asked → answers:** none (unattended).
**Done:**
- Deadlock listings refreshed with the new `tools/dev_listings.py` (vrfdump; 158,306 files, 87,188 sound events, no clip or mesh of our seven heroes changed); `preflight.py`: **PREFLIGHT CLEAN** (5,878 cells).
- Cache rebuilt from 6812 with extractor 1.6.2: **7/7 heroes**, every clip (16/16 Haze ... 14/14 Rat King), 6/6 props, 56/56 images, 119/119 sound events, 74/74 voice groups; heights unchanged (Haze 2.420 m, the anchor). A Melty player on 6812 with live 0.9.0 had read 6/6 the same way (`player_logs`).
- Melty: live 0.9.0 has 11 plays, 11 worked, 0 crashes. `player_logs` `load:errors` held one real code error on 0.9.0: `NullReferenceException` in `SetStateOnHurt.GetModifiedDuration` from `Fx.Stun` ← `DashCharge` (Abrams' Shoulder Charge). Cause: RoR2 fills `buffDurationControllers` in `Start`, so an enemy spawned that frame can't be stunned yet. `Fx.Stun` now skips such an enemy (doc 02 gotcha 28). The other `load:errors` line (Infernus on a player's Deadlock 5977) and the `not-loaded` report (a player with R2API and another Deadlock mod, log cut short) are not ours to fix; the lobby join timeout is Steam.
- Watch tooling: `fix-prompt.md` and doc 10 use `dev_listings.py`; doc 10 §7/§8 note the permission matching seen in this run and that the triage missed the stun error. Docs 01 §8, 02 §12, 10, MODLOG, doc 00 §1/§9.
**Results:** `run_test.sh` (all seven heroes): **7/7 heroes, 27/27 checks pass**, 706 Deadlock sounds, no exceptions; Shoulder Charge still stuns. `BREAK=infernus run_test.sh haze`: **19/19 pass** incl. `missing-hero-tile`. `lab_restore` after each.
**Not done / open:** **Not published.** Deadlock 6812 needs no fix: players on 0.9.0 keep working. The stun error is minor (one-frame exception, no crash). Shipping it alone would mean a hotfix cut from the 0.9.0 commit, because `main` carries the unreleased Seven redesign (0.9.1a/b), and choosing what goes out is the creator's call. It ships with the next release once `watch/2026-10-22` is merged. The stun race itself can't be forced in the test (it needs an enemy spawning under the charge).
**Lessons:** in this `claude -p` run, `python tools/x.py` was refused although `claude-settings.json` allows it, while `python -c` worked; pipes, `&&`, `cd` and `$VARS` were refused too. Read `player_logs` in the fix run even when the triage says "no breaks".

### 2026-10-22 · The watch's first night, merged; its two problems fixed · no version · the commit that adds this entry
**Request (verbatim):** "DID IT RUN LAST NIGHT?" then "YES FIX AND RESOLVE THOSE 2 PROBLEMS"
**Questions asked → answers:** none (the two problems were named in the report: the `python tools/*` rule and the triage missing the stun exception).
**Done:**
- Merged `watch/2026-10-22` (Deadlock 6812 checked, Fx.Stun fix, `tools/dev_listings.py`) into `main`.
- Permissions: `Bash(python tools/:*)` → `Bash(python tools/*)` (a `:*` rule needs a space after it), same for the extractor tag and push rules; added `cd`, `echo`, `sort`, `uniq`, `sed -n`; fix-prompt asks for plain commands with literal paths (no `$LOCALAPPDATA`).
- Triage: Sonnet instead of Haiku, prompt opens every live-version log and counts any exception in our code; each break gets a `signature` and `ours`; `daily.ps1` starts a fix run only for ours + a signature not in `build/watch/known-breaks.txt` (seeded with Fx.Stun and Infernus-on-5977); fix-prompt adds a line per handled break. Docs 10 §3.3/§5/§7/§8, 03 §5b.
**Results:** a headless run with the watch's settings ran `python tools/preflight.py` with no refusal (PREFLIGHT CLEAN). Triage on the last two days: Sonnet found `exception:NullReferenceException:Fx.Stun` (ours), `coop:steam-lobby-join-timeout` and `not-loaded:…` (not ours), about 30 cents. Haiku found nothing (1.7 cents). The script's matching skipped the known Fx.Stun and would start a run for a new exception in our code. An earlier try that let the model judge "known" was unreliable both ways.
**Not done / open:** the Fx.Stun fix ships with the next release; `gh` still not installed.
**Lessons:** let the model describe (signature, ours) and let the script decide; a `:*` permission rule is a word prefix.

### 2026-10-22 · Melty issues researched and fixed; 0.9.1 with six heroes and a blacked-out Rat King · v0.9.1 · the commit that adds this entry
**Request (verbatim):** "OK CAN YOU SEE THE ISSUES ON MELTY WITH THE CURRENT BUILD? CAN WE FIX DO DEEP RESEARCH AND THEN PUBLISH ON MELTY WITHTHE 6 CARCYERS WITH RAT KING BLACKED OUT"; later "PLS YOU CLOSE EVERYTHING THAT IS NOT NESSISARY FOR THIS AND RERUN"
**Questions asked → answers:** ship the new Seven? → "Yes, ship new Seven". Rat King's tile → "Black silhouette". Co-op fix → "Always joinable". Melty test before publishing → "Yes, run Melty's test". After the co-op test was killed for low memory: → close what isn't needed and rerun (closed Discord, Chrome (normal close first), Razer, NVIDIA App, Snipping Tool, GitHub Desktop; left Steam, Melty, VPN, VS Code, OBS, other Claude windows): 1.9 → 5.0 GB free.
**Melty issues on 0.9.0 (15/15 plays worked, 0 crashes):** (1) Abrams' Fx.Stun exception (fixed on main by the watch); (2) co-op: `setup_multiplayer` `hostChecks.no-marker` 3 (players pressed Play and never opened a lobby: RoR2 has one only in the Multiplayer menu or a multiplayer run) and the one joiner got a lobby id its host had already left (we never cleared the address); (3) one "code didn't load" with Mina from Deadlock + R2API + RoR2BepInExPack, its log cut off before our lines: cause not found (our stack-overflow patch already steps aside for RoR2BepInExPack).
**Done:**
- `MeltyJoin` (Net.cs): started by Melty the title screen opens a lobby (`steam_lobby_create_if_none`), a Singleplayer run leaves it, the log is emptied when the lobby closes, a joiner waiting in the host's lobby is not reported as failed. Hook row `melty_join`. Doc 02 §7.7.
- Coming-soon tile: black silhouette (`MissingHeroes.Silhouette`), test check `coming-soon-tile`. Doc 02 §1.
- Found in the co-op test: a joined player's Concussive Combustion could fizzle on the host (the owner's end arrives before the server's late copy reaches 3 s); `UltSelfBomb` blasts in `OnExit` when ended within 1.5 s of the fuse. Doc 02 gotcha 29, docs 05/06.
- Co-op test: the Q check used a fixed 120 s, so Goo Ball (100 s) failed; now 80 % of the row's cooldown.
- `dev_run.sh` `RELEASE_ZIP=` tests the release's hero list. Version 0.9.1, CHANGELOG, `thunderstore/melty-description.md`.
**Results:** Deadlock updated again to 6813: full cache 7/7, the release manifest 6/6 (`|1.6.2|4373A158E9E99476`). `run_test.sh` (release cut) **6/6 heroes, 27/27 pass**; `BREAK=infernus run_test.sh haze` **20/20**; co-op first run killed (low memory), second: everything but `coop-infernus` (Combustion) and `coop-viscous` (the 120 s check); after both fixes `run_test.sh infernus` 20/20 and `run_coop_test.sh infernus,viscous` all pass (Combustion 1632 damage, stunned). Normal start: "Melty: opening a Steam lobby", `Melty join address: …steamlobby`; Melty's dry run reads it, reach public. Melty release `09294a1d-ae04-436d-9cac-a193bdf140a1`, test `65dcf162-f7f8-41fe-95ed-60d6b5f872cf` **passed 2/2** (Melty's setup read 6/6 from 6813; each run 27/27 own checks; both footage reviews pass; `graduates: true`). Listing text and tagline rewritten (six heroes, new Seven, co-op, Hero pages, Built-with v0.9.1). **Published: in Melty's safety review** at the time of writing. `report_bug_fix` recorded for `load:errors` and the join-waiting failure (they clear when 0.9.1 is live).
**Not done / open:** the Mina conflict (needs a full log or a test with that mod installed); a Singleplayer run closing the lobby was not driven by hand in the test; a real friend joining over Steam is still untried.
**Lessons:** a `hostChecks.no-marker` count means players' games weren't joinable, not that the log was missing; two game copies need ~5 GB free (Discord alone held 3 GB); a timed state whose effect only the server deals can lose it when the owner ends first.

### 2026-10-22 · The overnight check's docs brought up to date after 0.9.1 · no version · the commit that adds this entry
**Request (verbatim):** "OK UPDATE DOCS TO ANY CHANGE WE MADE TO TE OVERNIGHT FIX CHECK!"
**Questions asked → answers:** none.
**Done:** `tools/watch/fix-prompt.md` step 7 rewritten from the 0.9.1 release: publish only when `main` holds nothing unreleased besides the fix; test the zip with `RELEASE_ZIP=`; recipe `test.checks` = the first 12 lines of `build/checks.txt`; `test_footage` + `test_review` for each Melty test run; listing from `thunderstore/melty-description.md`; `report_bug_fix`. New rule: never close the creator's programs; the co-op test only when the fix touched co-op code and ~5 GB is free. `build/watch/known-breaks.txt`: the Fx.Stun line now says fixed in 0.9.1. Doc 10 §3.4 (steps 5 and 8), §5 (files), §7 (two troubleshooting rows), §8 (0.9.1 note); doc 03 §5b and CLAUDE.md publish rule (only the fix ships).
**Results:** docs and prompt only; nothing run.
**Not done / open:** the fix run's publish path is still untried end to end.
**Lessons:** none new.

### 2026-10-22 · 0.10.0: Rat King playable, Billy coming soon, checked on Deadlock 6817 · v0.10.0 · commits f2a5a11 and the commit that adds this entry
**Request (verbatim):** "can we do a new release with the rat  king but not billy, billy will be out new greyed out character! just disable his button so you can never select him"; then "deadlock just had and update can you also make sure we are not pushing any bigs justa  quick check on the new file that everything is in working order for our release"; "imm updaating deadlock no make sure everythings ok"; "ok so please when this dont do the melty test and uplad staright away no more question... if something is brokene fix it and then upload"
**Questions asked → answers:** version → "0.10.0"; publish after Melty's test passes → "Publish when it passes" (later: no Melty test at all); start the game for tests → "Yes, start now"; after the co-op run was stopped for low memory → "Re-run co-op test".
**Done:**
- Billy's uncommitted work committed as work in progress (`f2a5a11`); his notes relabelled v0.11.0; his shared-code touches read and found inert for other heroes.
- `thunderstore/release.json`: `melty` + ratking, `comingSoon: ["billy"]` (the existing coming-soon tile: locked, black silhouette). Version 0.10.0, CHANGELOG, `melty-description.md`.
- Deadlock 6817: waited for Steam to finish, `dev_listings.py` (+2 files, nothing for our heroes), PREFLIGHT CLEAN, cache rebuilt 8/8.
- Co-op fixes: `ScrapThrow` sends `ownerFrom` (doc 02 gotcha 30); co-op test window 5 s for `ult_swarm`; a `rat swarm:` log line and rat bites in the co-op result.
- Docs 02 (coming soon, gotcha 30), 03, 06, MODLOG, doc 00 §1/§9.
**Results:** run_test (release zip) 7/7 heroes 28/28 incl. `coming-soon-tile` for Billy; BREAK=infernus haze 20/20; run_test ratking 20/20 after the fix. Co-op ratking: run 1 swarm 0 damage (test window); run 2 desynced (the joiner never moved on the host; no conclusion); run 3 stopped for low memory (two copies ~5.5 GB, 1.5 GB free); run 4 swarm 444 / 37 bites, scrap 0 (start-point bug); run 5 after the fix scrap 288 + both slows, swarm 924 / 77 bites, banner hit and stunned but showed 1.3 m of movement (8-17 m in runs 1, 2-desync aside, 4). Melty release `ab990a7a-…` published without a Melty test, in safety review.
**Not done / open:** the co-op banner leap's movement on the host (one run in four); Rat King's kit page still to be shared by the creator; Billy for 0.11.0.
**Lessons:** a throw needs its start point sent as well as its direction; a move with no duration needs a longer co-op window; check free memory before a co-op run (a run gets killed below ~2 GB).

### 2026-10-23 · Wraith, the ninth hero · v0.11.0a (not released) · the commit that adds this entry (and 25178bf, leftover Billy test-order edits)
**Request (verbatim):** "ok lete work on https://deadlock.wiki/Wraith. m1 is the The Rake, m2 is Card Trick choose 4 randome status effect from ror2 bleed (eemy get bleed), crit (more damage on the card), stun, burn, shift is Project Mind a purple place it shown and any button clicked will teleport you there i want it to go pretty far! R is Full auto, reset magazine to full with double the ammo for your gun and fire rate is increased until you run out of a clip. now the purple magic vfx are really important for this character. the equipabe item is telekenisis now this will pick up all enemy withing a 30 metre radius raise them into the air and chuck them like 50 meter in the direction the player is  land the get massive damage and if they hit into a wall even more damage, and i guess you can chuck enemy off clliff etc etc for bosses this willdo massive damage and also stun the if that possible on bossed. the purple smoke magic is really imprtant for this character and so it the cards that she throw. now take the animation rebuild the vfx (purple smokey!) take the correct sfx for the character and abilities. i want you to make a suggest a related character and ask questions. when you are developing please run a dps test the build the artifact and test fo the character works on coop online!"
**Questions asked → answers:**
- RoR2 match → "Bandit (Recommended)".
- Card Trick → "1 card per press random effect (if it stacks make it full stack eg burn) cool down every 5 sceonds". Full stack → "5 stacks at once (Recommended)".
- Project Mind → "60 m, instant on confirm (Recommended)".
- Telekinesis → "Crosshair; bosses lift a little (Recommended)".
- Full Auto → "+50% fire rate, 25 cooldown, make this gun have alot of kick back its a tommy gun! but when we enable full auto make the kick worse by like 20%. also it stays full auto until the clip is gone. let not double the ammo in the clip, also play the relaot animation when the ability is tiggered also a purple smoke aura on start!"
- After a test run was stopped for low memory → "Close Discord/Chrome first". The creator closed Chrome; Discord stayed running in the tray.
- Balance (517 vs Bandit 1919) → "Keep her as she is".

**Done:**
- Deadlock lookup with `build/vrfdump`:
  - heroes and abilities vdata, `wraith.vsndevts`
  - the glb: 374 clips, 422 bones, coat cloth bones
  - doc 01 Wraith section
- Sheets:
  - heroes, stats (Bandit), 5 skills, equipment, animations (coat springs, cloth parents), sounds, voice
  - buff `fullauto`
  - effects: `tracer_wraith`, the card prop, 5 custom rows
  - new skills columns `recoil`, `aimKick`, `dotTime`; every old gun keeps its old kick
- Code:
  - `States/Wraith.cs`: `card_trick`, `mind_port`, `full_auto`, `ult_telekinesis`
  - `Runtime/WraithParts.cs`: `DlrCard`, `WraithThrown`, `WraithAura`
  - `Runtime/VfxWraith.cs`: the purple smoke kit
  - `Fx.Hit(forceCrit)`, `Buffs.Dot`
  - `GunState`: kick from the sheet; Full Auto's faster fire, auto-reload and normal-speed reload
  - the reload relay ends Full Auto on the server
  - test mode: `TestWraith` (all four tricks, Full Auto) and a 5 s co-op window for Telekinesis
  - the card's start point travels with the state (gotcha 30)
- Cache rebuilt: 9/9 heroes, Wraith 15/15 clips, the card prop.
- Pages: Wraith's kit page published (new), and all nine other pages republished with Wraith in the nav. Links added to README and doc 06.
- Docs: 01, 02 (kinds, vfx, kick columns, gotchas 31–32, co-op window), 03 §9, 05 §0/§3.14, 06, 08 §7d, MODLOG, CLAUDE.md, doc 00.

**Results:**
- `run_test.sh wraith`: **1/1, 18/18 pass** on run 3.
  - Run 1 failed only Telekinesis' stun (fixed). Run 2 was stopped by Claude Code for low memory and passed when rerun.
  - Every trick was seen: bleed, crit (96 = 2 × 48), stun, burn.
  - Full Auto: magazine 52/52, emptied in 3.4 s against 4.9 s plain, ended on the reload.
  - Telekinesis: 1440 damage, enemies thrown 50.3 m.
- Bench (one run): all **517** vs Bandit 1919 (below). M1 440, M2 187, Q 1400.
- `run_coop_test.sh wraith`: **7/7 pass** on the second run. On the first, the joined player's Telekinesis landed after the 3.7 s window.

**Not done / open:**
- Wraith's balance is the creator's call; the levers are in doc 05 §3.14.
- A second bench run.
- The creator has to share her page.
- Not released; she ships in 0.11.0 with Billy.
- The creator hasn't seen the smoke in play yet.
- A real friend over the internet is still untried; the co-op test is two copies on this PC through a Steam lobby.

**Lessons:**
- A game copy takes about 4 GB. Run single-hero tests in the foreground and the co-op test detached (`Start-Process`), and ask the creator to close big programs.
- `lab_restore.sh` empties the devshots; copy them first.
- Coloured smoke needs a light tint and a glow inside (gotcha 32).
- Re-stun a lifted enemy once it is up (gotcha 31).
- Republishing a page made in another conversation takes three steps: publish (refused), read part of the saved copy, publish again.

### 2026-10-23 · Overnight check, then a clear loading screen · v0.11.0 (not released) · this commit
**Request (verbatim):** "did our overnight checker push any change slast nignh there seem to be issue with v0.10.0", then (with
Melty's "Closes right after starting" logs) "can we add an update that amkes it really clear taht we are extracting file and
building their game?", and mid-work "when you upload do not ship billy or Wraith".
**Questions asked → answers:** what shows → "Box + bigger banner"; Melty setup wording too → "Both" (extractor 1.7.0);
release → "With 0.11.0".
**Done:**
- Checked: the 04:00 watch pushed nothing. It saw Deadlock 6812 → 6819 but skipped the fix run (21 uncommitted files).
  Melty's flag is one player who closed 0.10.0 after 41 s **before the title screen** (no "Melty: opening a Steam lobby" line),
  with a clean log on Deadlock 6819; 0.10.0: 6 started, 5 worked, 0 crashed, no reports. Not a bug.
- Plugin: the loading screen on every start shows a banner ("DEADLOCK OF RAIN", "Building your Deadlock heroes: <Name>
  (n of N)", clock, "The game is not stuck") and the box with each hero as it gets ready (doc 02 §3). `ShowSummary` folded in.
  Test check `loading-screen`, devshots `loading-building.png` / `loading-done.png`.
- Extractor 1.7.0: plain-words lines for Melty's setup screen; manifest `heroes[].name` from `gen.py` (doc 01 §6).
- Docs 01, 02, 10, 00, MODLOG.
**Results:** extractor 1.7.0: 9/9 heroes, 160/160 sounds, 95/95 voice groups, new lines as intended. `run_test.sh haze`: every
  gameplay check passed but `loading-screen` failed (Haze never drawn: the loader steps several times per frame) → wait for a new
  frame; box too short at 1440p → grows with the screen. After the fix `BREAK=infernus run_test.sh haze`: 20/20 incl.
  `missing-hero-tile` and `loading-screen`; screenshots checked (banner, "8 of 9 Deadlock heroes ready", Infernus NOT AVAILABLE).
  Building 9 heroes takes 15.1 s on this PC.
**Not done / open:**
- Released 0.11.0 to Melty as a draft **without Billy and Wraith** (seven heroes, Billy's coming-soon tile kept: creator's
  answer "Keep the tile"): `RELEASE_ZIP` run_test 7/7 29/29 (shared code changed since 0.10.0, so all seven), BREAK 21/21,
  release `ee1fac3c`, Melty test `fa69bd4a` started (creator: "Melty test, then publish"),
  then **published without waiting** when the creator asked "is this upload or wtf is going on i want this release on melty"
  (live 2026-10-23 13:26 UTC after ~20 min of safety review; listing text updated). Listing text updated in
  `thunderstore/melty-description.md` (built with 6819, loading screen line); goes to `update_mod` at publish.
- Thunderstore: needs the GitHub-built 1.7.0 (`git push origin extractor-1.7.0`, `fetch_extractor.py 1.7.0`); not done.
- The co-op test was not run for 0.11.0 (loading-screen change is client-only; Wraith's relay change is inert without Wraith).
**Lessons:** a Melty test re-runs setup when the release manifest differs from the dev cache (several minutes of "starting" with
  nothing visible); tell the creator that up front, or they think nothing is happening. A quick-close before the title screen is visible in the log by the missing Melty lobby line.

### 2026-10-23 · The Steam copy has every hero again · no version · this commit
**Request (verbatim):** "why is my new hero not attached to steam!"
**Questions asked → answers:** put the full build on for Steam? → "steam my client shoudl alway have all characters added even if
they areb in development and not release on melty" (a standing rule; Billy playable too). Then: "ok dont open when you are done!"
**Done:**
- Cause: after the 0.11.0 Melty test the PC held Melty's seven-hero cut (`heroes.txt` without Wraith, `coming-soon.txt` billy),
  the cache had been re-read for seven heroes only, and Melty had parked `winhttp.dll` as `.melty-off`, so Steam's Play loaded no mods.
- Installed the dev DLL from `main` (c298b3a), `heroes.txt` with all nine, removed `coming-soon.txt`, put `winhttp.dll` back.
- Rebuilt the cache with the full manifest (extractor 1.7.0, Deadlock 6819).
- `tools/lab_restore.sh` now puts the dev cut back after every run and prints `CACHE MISSES <heroes>` when the cache lacks one.
- Docs 00 (§1 installed build, §7 lesson, §9), 03 §4; memory `steam-client-has-all-heroes`.
**Results:** cache `Ready: haze, abrams, seven, infernus, mokrill, viscous, ratking, billy, wraith`, nothing failed. The game was not started.
**Not done / open:** no in-game check this time (the creator plays it). The rebuild took far longer than the ~8 min quoted
  (about 9 min on Abrams alone); `lab_restore.sh`'s new lines are untested by a real run.
**Lessons:** a Melty test leaves the release cut installed; restore the dev cut right after. Don't quote "8 minutes" for a full rebuild.

### 2026-10-23 · Billy looks turned left; test mode never left on · no version · this commit
**Request (verbatim):** "so billy does seem to be walking staright like is model is cocked 45 degrees to the left? to he walk that
direction ratbher that the way the camera is facing!", "does not", "he shoudl walk straight like all other characters!", "run a test
yourself aswelll", "and then leave the game open so i can test", "works now", then "alway turn off fucking test mode unless you open it ffs".
**Questions asked → answers:** "works now" = Billy walks straight now? → not answered yet (the test-mode complaint came next).
**Done:**
- Measured: Billy's gun points straight, but his `weapon_stand_idle` (the aimPose the gun rig freezes the arms in) has his
  shoulders ~59° left of the gun; the rig turns the chest until the gun aims, so while running his chest stays ~59° left of
  his legs (Haze +63, Wraith -20, Abrams -29). Deadlock has `shoot_idle`, `primary_idle`, `aim_weapon_idle` etc.; Billy
  re-extracted with them into the scratchpad to compare (no sheet change yet).
- `run_test.sh billy`: 1/1, every check pass (screenshots: from behind he looks straight standing; side view chest bladed).
- `run_test.sh` / `run_coop_test.sh` delete the test-mode file on exit (`trap`), doc 03 §4; memory `test-mode-off-after-runs`.
**Results:** run_test billy all pass. The test file had been left behind after that run (no lab_restore, game left open); removed.
**Not done / open:** Billy's stance: waiting for the creator to say whether he still looks turned; the fix would be a squarer aimPose.
**Lessons:** leaving the game open for the creator skipped lab_restore and left test mode on; the scripts now clean it themselves.

### 2026-10-24 · Billy: bigger bat, chained air Bashdown, longer Ram float, +25 % health · v0.11.0 (in development) · this commit
**Request (verbatim):** "ok work on bbilly i want the aoe arc on m2 to be alot bigger you can express this through the vfx swipe, also
lets work on Shift Bashdown Ability whiel in the air the enemy need to be chained form billy now and fall alongside with him. all enemys
launced into the air form Rising Ram Ability need to start at 3 second in air if an enemy is hit with a 10m radius with m2 Bat they get an
adition 0.5 seconds in air, if they are hit they get an extra second, let make the snapping to enemy with the bata ability a bit larager
and let increase billy health by 25", then "when you run a test after the change leave it open for me to test!"
**Questions asked → answers:** bat arc → "6m, 120° wide" (then told the arc was already 120°, I had said 60° by mistake: "6m, keep 120°");
Ram float extras → "Hit +1s, nearby +0.5s" (cap 6 s); health → "+25% (140 → 175)"; bat snap → "13m"; stance fix → "No, it's fine now".
**Done:**
- Sheets: bat `radius` 6, `lockRange`/`reach` 13, `floatTime` 6, new columns `nearHang` 0.5 / `nearRadius` 10; Ram `floatTime` 3;
  Billy 175 +52.5 health; descriptions rewritten.
- Code: `BatCombo.Strike` extends floats near each hit; `Vfx.Sweep` (a flat arc over the whole hit area) for the bat's cast;
  air Bashdown chains from Billy and drags enemies down with him (`DlrLift` phase Drag, `BillyAir.DragDown`/`LetGo`).
- Test: `TestBillyAir` snaps from `lockRange − 1` m, checks the near enemy's float and the air Bashdown drag (2/2 chained, beside him, both down).
- Docs 02 (Billy's kinds, `billy_bat`), 05 §0 flag, 06 (new Billy section), hooks.json.
**Results:** `PRACTICE=1 run_test.sh billy`: 20/20 pass on the third run. Run 1 pass. Run 2: one enemy landed 7.4 m away (pull capped at
  20 m/s, raised to 60), the ragdoll check also failed there and the run went back to the title (not seen again). The game was left open
  on the practice spot for the creator.
**Not done / open:** bench (it closes the game; after the creator's play), kit page refresh and republish (after the bench), co-op check of the drag.
**Lessons:** never put a second command after a test in the same shell line: a stray `run_test.sh ... &` killed the practice game the
  creator was waiting for and had to be killed and rerun.

### 2026-10-24 · Billy's own float: never more than 1 s left · v0.11.0 (in development) · this commit
**Request (verbatim):** "OK I THINK THE FLOAT TIMING IS RIGHT ON THE ENEMYS BUT FOR BILLY HOW LONG DOES HE EGT IN ARI ON THE Rising Ram ABILITY",
then "EACH BAT HIT SHOULD EXTENND HIS TIME BY 1 SECOND BUT HE SHULD NEVER HAVE OVER 1 SECOND FLOATING", then "DO NOT PLAY DO NOT TEST JUST MAKE THE CHANGE!"
**Questions asked → answers:** how long should Billy stay up after Rising Ram → the answer above (tops up to 1 s, never more).
**Done:** `HeroRuntime.HangFor` tops his hold back up to `hangTime` (1 s) from now instead of adding to it (two quick hits used to give 2 s);
  `HangLeft` for the test; `TestBillyAir` checks at most 1 s left after two quick hits; bat description and skills `about`; docs 02, 06.
  The DLL is installed for the creator's next start (the open game was left alone).
**Results:** builds, preflight clean. **Not tested in game** (the creator: "DO NOT PLAY DO NOT TEST").
**Not done / open:** run `run_test.sh billy` when the creator allows it (the new check is in); bench, kit pages, co-op check still open.

### 2026-10-24 · Billy benched, co-op test, 1.0.0 on Melty with Wraith blacked out · v1.0.0 · this commit and the next
**Request (verbatim):** "run dps test on illy and update the artifact then test coop, then upload v1.0.0 on melty with wraith blacked out and unselectable!"
**Questions asked → answers:** none (the request names every step).
**Done:**
- Bench `run_bench.sh 30 billy` (one run, `VERSION` v0.11.0b): all **7957** vs Mercenary 5112 (+2845, above the band); bat 2664, Ram 902,
  Bashdown 683, Biquette 297, Chain Gang 1000 (30000 % per use). Merged into `docs/dps-library.json`. Docs 05 §0/§3.15, 06.
- `kit_pages.py`: Billy's analysis and a v1.0.0 "what changed" list; `LAST_USE` gains Wraith (the rebuild had lost her ult row) and Billy.
  Billy's page and the balance overview republished to their links.
- `run_coop_test.sh billy`: pass (coop-run, coop-billy, coop-shots-seen, coop-effects-seen); all five Billy moves hit as a joined player,
  47 relayed effects. The air Bashdown drag was not part of the co-op steps (ground Bashdown only).
- 1.0.0 prepared: `Plugin.Version` 1.0.0, `release.json` `melty` + billy, `comingSoon` wraith, CHANGELOG 1.0.0, listing text
  (`thunderstore/melty-description.md`: eight heroes, Wraith coming soon, Billy's page). `package.py 1.0.0 --melty` built
  `build/DeadlockOfRain-1.0.0.zip` (heroes.txt 8, coming-soon.txt wraith; `setup.done.contains` would be `|1.7.0|524B1AE7A140494E`).
**Not done / open:** the creator said "hold off on the upload for 2 seconds": nothing uploaded. Still to do before upload:
  `RELEASE_ZIP=build/DeadlockOfRain-1.0.0.zip run_test.sh billy` (also runs Billy's new float check, never run) and the BREAK check,
  then the recipe (checks from that run), upload, submit, listing, publish.

### 2026-10-24 · Discord community, title-screen button · v1.0.0 (unreleased) · this commit
**Request (summarised; the creator asked to keep their own Discord server's details out of the repo):** a place in the creator's Discord for
people who join through the mod, linked from the Melty and Thunderstore pages: a "risk of deadlock player" role, a Risk of Deadlock section
with faq and hero requests that everyone can see, the role limited to that section plus the server's general and promote-yourself channels,
and a join rule: "done be an idiot f you are disrespectful to other member you will be removed"; then
"build on the risk of rain main menu page a big obvious button to join this community with the Risk of Deadlock player role on default thorugh this link",
"just built it dont run the game to test it until i tell you".
**Questions asked → answers:** who sets up Discord → "I do it in Chrome"; when the link goes live → "With the next release (Recommended)"; a line in game → "Yes, on the title screen".
**Done:**
- Discord (creator's browser, signed in): role `Risk of Deadlock Player` (orange); category `Risk of Deadlock` (public) with #faq and #hero-requests;
  the role is denied View Channel everywhere else and allowed on the server's #general and #promote-yourself;
  "View Server As Role" shows exactly those plus #faq and #hero-requests. Invite `https://discord.gg/sNByetmcqc`: never expires, no use limit, gives the role, lands in #general.
  Join form: "Server Rules" with "Don't be an idiot. If you are disrespectful to other members you will be removed."
- `plugin/Runtime/CommunityButton.cs` + `Plugin.cs` init: the title-screen button opening the invite. "Community" section in `README.md`, `thunderstore/README.md`, `thunderstore/melty-description.md`; 1.0.0 CHANGELOG line. Docs 00, 02, 03.
**Results:** `dotnet build plugin -c Release` 0 errors. Not installed (game open), not run in game (creator's instruction).
**Not done / open:** players joining through the invite still need the creator's approval unless it bypasses join applications (the creator decides);
  Onboarding was not switched on (the invite role does the job). See the button in game when the creator allows it; rebuild the 1.0.0 zip before uploading.
**Lessons:** Discord invites can carry roles now (Invite → Edit invite link → Roles), so no bot or Onboarding question is needed to tag players from one link.
  Channels with their own permission overrides don't follow their category: check the role with View Server As Role.

### 2026-10-24 · Billy: no fall damage, air snap only on screen, Biquette 10 a bullet · v1.0.0 (not uploaded) · this commit
**Request (verbatim):** "billy needs some work, he should never recieve fall damage, and he should never snap to an eney in air if we is not looking in thier direction. if they are on screen i think it good. also let put billys gun damage up to 15 no other change!", then "when ur done please just stop and dont do dps test on billy close th game when ur done and turn off test mode of course"
**Questions asked → answers:** which "15" (15 a bullet / damage stat 15 / 150 %)? → "ok make it 10 per bullet".
**Done:**
- `heroes.json` billy `noFallDamage` true, passive text says so.
- Biquette `damagePct` 50 → 83.33 (10 a bullet at damage 12), `targetDps` 300 → 500 (preflight's own sum), description.
- `BillyAir.Nearest(body, range, onScreen)` + `BillyAir.OnScreen` (viewport of the camera rig following him; bots: in front of the aim);
  the air bat's dodge-in and the air Bashdown use it. Descriptions of bat and Bashdown.
- `TestBillyAir`: an air swing looking away from a floating enemy must not snap; fall damage must be off.
- Docs 02, 05 §0, 06.
**Results:** PREFLIGHT CLEAN; `run_test.sh billy` 1/1, every check pass (looking away: 10.0 m → 10.6 m, no snap; fall damage off: True;
  the normal air snap still 8.9 → 4.2 m). Game closed, `lab_restore`, test-mode file removed, `winhttp.dll` in place. No bench (creator).
**Not done / open:** the 1.0.0 zip built earlier is stale: re-run `package.py 1.0.0 --melty` before the upload (the setup hash changes with
  the manifest only if Deadlock content changed; recompute it). Billy's kit page still shows 50 % (republish after a bench, or now if the creator wants).
  Upload still on hold (creator: "hold off on the upload").

### 2026-10-24 · Discord roles outline, the button restyled · v1.0.0 (unreleased) · this commit
**Request (verbatim):** "ok outline each role and what they can see and the  also show me the button in risk of rain", then
"now also let make the fucking discord button look pretty use the discord log and put the writing in the same style as risk of rain 2",
"but it [Image #1] in the bottom left and make sure the discord logo and the text are not overlapping", "dont close the game im testing something".
**Questions asked → answers:** none.
**Done:** the Risk of Deadlock Player role re-checked with "View Server As Role" (the outline of the server's other roles is the creator's, not recorded). The button is now a copy of RoR2's own Singleplayer
  button (font, frame, hover, click sound) in blurple with Discord's logo (`plugin/Assets/discord.png` from `tools/discord_logo.py`,
  embedded resource), placed in the bottom-right corner ("sorry put it in the bottom right!"), logo and text in separate areas. Doc 02 §8.
**Results:** first restyled build seen in game (log "copied from GenericMenuButton (Singleplayer)"): it sat top right over the
  profile box with the text over the logo, which led to the move. The bottom-left build compiles; not installed or seen yet (game open, creator testing).
**Not done / open:** install the DLL when the game is closed and look at it; whether the invite skips approval is still the creator's call.
**Lessons:** RoR2's title screen has the profile box and event banner at the top right; put mod UI in or under the menu column.

### 2026-10-24 · Billy: air Bashdown straight down, float ended · v1.0.0 (not uploaded) · this commit
**Request (verbatim):** "we need to fix [Bashdown's description pasted] when billy is in the air he shold go straight donw to the gorund and cancellout the float time from any hit or build up hittin that move in the air ia clear decision to be broughts down to the ground!", then "is bullet famage down to 8"
**Questions asked → answers:** none.
**Done:** air Bashdown: `HeroRuntime.EndHang` (his float ends and no hit restarts it until he lands), straight down at `speed`, the smash at
  the press (2000 % to the enemy it locks, 1000 % + stun within 10 m, chains drag floating enemies down with him). Description; docs 02, 05 §0, 06.
  `TestBillyAir` gives him a float first and checks: float 0 after the press, ≤ 1.5 m sideways, on the ground.
**Results:** `run_test.sh billy` all pass: float left 0.00 s, 0.4 m sideways, down in 0.23 s; 2/2 enemies dragged down beside him.
  Game closed, test mode off. A copy of the game was already running when the test started; `run_test.sh` closes any running copy first.
**Bullet damage:** the sheet is 83.33 % = 10 at level 1; a smaller number in game comes from armor (shown after armor) or range falloff past 14 m.
**Not done / open:** upload still on hold; Billy's page not refreshed.

### 2026-10-24 · Discord #update channel, posts on every Melty release · this commit
**Request (verbatim):** "add new Risk of Deadlock ... Player channel called update and only i am allowed to post there", then
"i want you to post here everytime there is a updtae pushed to melty, add that to the claude.md".
**Questions asked → answers:** none.
**Done:** #update made in the Risk of Deadlock section as an Announcement channel (everyone and the player role can read it; @everyone
  denied Send Messages, Create Public Threads, Send Messages in Threads, so only the owner posts). Webhook "Risk of Deadlock" on it; its link
  saved to `%LOCALAPPDATA%\DeadlockOfRain\discord-update-webhook.txt` straight from the clipboard (never shown). `tools/discord_update.py
  <version> [--dry-run]` posts the CHANGELOG section + Melty link. CLAUDE.md standing rule; doc 03 §6 step 9 and §9; watch fix prompt and doc 10.
  The creator renamed #faq to #faq-issues.
**Results:** dry run for 1.0.0 prints the message; nothing posted yet (no release since).
**Not done / open:** first real post comes with the 1.0.0 publish.


### 2026-10-24 · Melty page issues: our tests, not players; scripts never close the creator's game · no version · this commit
**Request (verbatim):** "ok so check the melty page we seem to have some issues can pull the information do deep research and resolve them?", then "just fix don open up the game to test", "contine witth plan"
**Questions asked → answers:** none (plan approved).
**Done:**
- Melty read (`mod_status`, `install_outcomes`, `player_logs`, `player_reports`, `mashup_stats`). Current: `ended:quick-close` and
  `load:not-loaded` on 0.11.0, 2026-10-24 16:22–16:29 UTC, both ending in RoR2's own `ViewableTag.OnDisable` NullReferenceException
  (a window closed from outside). They match my `run_test.sh billy` exactly: it closed the game the creator had open, then its own copy.
  `mashup_stats` (without the creator's plays): 4 players today, 4 worked, 0 crashed; `player_reports` empty. The older issues were
  already fixed; the co-op "no-marker" count is by design (a Singleplayer run closes the lobby and clears the address).
- `tools/game_guard.sh`: `guard_open_game` (stop with `GAME_OPEN`, exit 2, when a copy no lab script started is running),
  `record_our_game` (PIDs to `%LOCALAPPDATA%\DeadlockOfRain\lab-game-pids.txt`), `close_our_games` (only those; `FORCE_CLOSE=1` closes all).
  Used by run_test, run_bench, run_coop_test, dev_run (records the copy it starts), lab_restore. The watch's fix prompt: on `GAME_OPEN`
  stop and report, never `FORCE_CLOSE` (the plan said the watch would force-close; changed for CLAUDE.md's "never close a game the
  creator has open"). Docs 00, 03 §4, 10; memory `never-close-creators-game`.
**Results:** game never started. `bash -n` on all six scripts. With a stand-in process named `Risk of Rain 2.exe` (a copy of ping.exe):
  not ours → `GAME_OPEN`, exit 2, left running; `close_our_games` left it open; recorded → closed by the guard; `FORCE_CLOSE=1` → closed.
  `lab_restore.sh` by hand: no test-mode file, `winhttp.dll` present.
**Not done / open:** `report_bug_fix` for both issue keys once 1.0.0 is submitted (they clear when it goes live); 1.0.0 still on hold.
**Lessons:** read the time of a Melty report against our own test runs before debugging the mod.
