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
