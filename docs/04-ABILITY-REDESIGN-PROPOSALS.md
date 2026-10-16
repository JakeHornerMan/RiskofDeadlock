# 04: Ability redesign proposals (planning)

> **Status: decided and implemented in v0.3.0. The creator's picks are in §6**; sections 1–5 are the proposals they chose from.
> The creator picks one option (or a mix) per hero. The chosen kit then goes into `design/skills.json` /
> `buffs.json` / `heroes.json`, gets its `expect` lists, and the other docs are updated as usual (see `CLAUDE.md`).
>
> **Scope:**
> - **The gun (Left Click) stays as it is.** Every option below redesigns Right Click (M2), Shift, R and the **passive**.
> - **The passive comes from each hero's least active Deadlock ability.**
> - **R stays the Deadlock ultimate**, locked until it charges from damage and kills (original requirement), so the options only change *how* the ult works.

Sources (read 2026-10-14):
- Deadlock wiki: [Haze](https://deadlock.wiki/Haze), [Abrams](https://deadlock.wiki/Abrams), [Seven](https://deadlock.wiki/Seven), [Infernus](https://deadlock.wiki/Infernus)
- Risk of Rain 2 wiki: [Bandit](https://riskofrain2.wiki.gg/wiki/Bandit), [Loader](https://riskofrain2.wiki.gg/wiki/Loader), [Artificer](https://riskofrain2.wiki.gg/wiki/Artificer), [Acrid](https://riskofrain2.wiki.gg/wiki/Acrid), [Commando](https://riskofrain2.wiki.gg/wiki/Commando), [Survivors](https://riskofrain2.wiki.gg/wiki/Survivors)

---

## 0. What "in line with Risk of Rain 2" means (the yardstick)

What RoR2's survivors have in common, from the wiki pages above:

| Pattern | Examples | What it means for us |
|---|---|---|
| **The passive defines the character** | Bandit **Backstab** (attacks from behind always crit), Acrid **Poison/Blight**, Loader **Scrap Barrier** (hits grant barrier), Artificer **ENV Suit** (hover) | Every hero should get a passive that changes how you play, not just a stat. Today Abrams and Seven have none. |
| **Short cooldowns** | Secondaries 2–5 s (Phase Round 3 s, Serrated Dagger 4 s, Neurotoxin 2 s, Nano-Bomb 5 s). Utilities 4–12 s (Tactical Dive 4 s, Smoke Bomb 6 s, Caustic Leap 6 s, Snapfreeze 12 s). Specials 4–20 s. | Deadlock cooldowns (26–50 s) are about 5× too long for RoR2, so cut them to RoR2 ranges and push the numbers up instead. |
| **The utility is usually movement or defence, often usable while sprinting** | Tactical Dive, Smoke Bomb, Caustic Leap, Grapple Fist | Haze, Abrams and Infernus already have one. Seven has no movement; Alt B fixes that. |
| **Charges (2 stocks) on spammy skills** | Caustic Leap (2), Tectonic Blast (2), Frag Grenade (2) | Deadlock T1/T2 upgrades often add a charge, a natural fit. |
| **Stun, freeze and execute are key verbs** | Smoke Bomb stuns, Nano-Bomb stuns, Nano-Spear freezes and executes under 30 %, Lights Out scales with missing health and resets cooldowns on kill | Sleep Dagger, Static Charge and Seismic Impact map straight onto RoR2 stun. Bullet Dance and Fixation could gain an execute. |
| **Specials are big but not "ultimates"** | Thunderslam 2000 % on 8 s, Ion Surge 800 % on 8 s, Epidemic 10 s | Our R is a charge-meter ultimate (your requirement), so it should hit about 2–3× harder than a vanilla special. That is today's 1500–3000 % band. |
| **Proc coefficients** | Pellets/ticks 0.3–0.5, single hits 1.0 | Keep ticks ≤ 0.5 so item procs don't explode. |

Each option's **"build cost"** says whether it reuses an existing skill kind or needs new code (doc 02 §7.3):
- **reuse:** only sheet rows change
- **small:** a parameter or branch in an existing state
- **new kind:** a new state class

---

## 1. Haze: "The Smoke-Shrouded Assassin" (RoR2 analog: Bandit / Commando)

**Deadlock kit (wiki):**
- **Sleep Dagger:** 65 dmg, sleep 2.75 s, 30 s cd. T1 −10 % bullet resist on wake. T2 +1 s sleep and 15 Fixation. T3 −17 s cd and a 40 % slow on wake.
- **Smoke Bomb:** 8 s invisibility + sprint speed. Attacking breaks it. T3 dispels debuffs and gives 50 % lifesteal.
- **Fixation:** a stack per bullet (3 per headshot), +0.2 dmg/stack, 40 max, 6 s. T1: every 20 stacks, 40 spirit dmg + slow.
- **Bullet Dance (ult):** 3.5 s, 16 m, shoots enemies with perfect accuracy, 30 % evasion.
- **Least active ability: Fixation**, a pure on-hit rider. It is already Haze's passive in v0.2.0.

### Current (v0.2.0)
| Slot | Skill | What it does | Numbers |
|---|---|---|---|
| Passive | **Fixation** | Each SMG hit stacks Fixation on the target; each stack = +2.5 % damage from Haze | max 40, 6 s |
| M2 | **Sleep Dagger** | Thrown dagger, puts the target to sleep (stun) | 350 %, stun 2.75 s, 6 s cd |
| Shift | **Smoke Bomb** | Invisible (vanilla Cloak) + 40 % move speed; firing reveals | 4 s, 8 s cd |
| R | **Bullet Dance** | Auto-fires at random enemies within 25 m; 30 % evasion; slowed to 60 % | 60 % ≈12×/s for 3.5 s; 5000 charge |

### Alt A: "Dream Assassin" (closest to Deadlock, with its upgrade tiers folded in)
| Slot | Skill | What it does | Numbers (RoR2 scale) | Build cost |
|---|---|---|---|---|
| Passive | **Fixation** | Each hit adds 1 stack (**crits add 3**, standing in for Deadlock headshots), +2.5 %/stack. **Every 20 stacks** the target takes a 200 % spirit burst and is slowed 12 % for 2 s (Deadlock T1). | max 40, 6 s | small (Guns on-hit) |
| M2 | **Sleep Dagger** | Sleep 3 s. On waking, the target is **Weakened** (−30 armor, 6 s; Deadlock T1) and gains **15 Fixation** (Deadlock T2). | 300 %, 5 s cd, usable while sprinting | small |
| Shift | **Smoke Bomb** | Invisible for 5 s + sprint speed. Leaving smoke **cleanses debuffs** (Deadlock T3). The **first shot out of smoke always crits.** | 7 s cd, usable while sprinting | small (crit flag) |
| R | **Bullet Dance** | As now, but **40 % evasion** (T3) and **+25 % fire rate**. Shots go at the enemy with the most Fixation first. | 70 % per shot, 3.5 s | small |

*Feel:* the Deadlock combo (dagger → stacks → dance) is kept; the numbers are just RoR2-paced.

### Alt B: "Sandman" (built on RoR2's Bandit: stealth, crits, execution)
| Slot | Skill | What it does | Numbers | Build cost |
|---|---|---|---|---|
| Passive | **Dreamstalker** (from Fixation) | Attacks against **sleeping, stunned or Fixation-capped (40) enemies always crit** (Bandit's Backstab, but earned instead of positional). Gun hits still stack Fixation. | n/a | small (DamagePatch) |
| M2 | **Fan of Knives** | Throws 3 daggers in a spread. Each hit adds 5 Fixation; a target hit by all 3 falls asleep for 1.5 s. **2 charges.** | 3×200 %, 4 s cd | small (projectile ×3) |
| Shift | **Smoke Screen** | Drops an 8 m smoke cloud for 5 s. Haze is invisible inside it, and enemies inside **can't target anyone and fall asleep for 1 s on entry** (like Bandit's Smoke Bomb stun). | 8 s cd, usable while sprinting | **new kind** (zone) |
| R | **Lights Out, Sandman** | A 3.5 s Bullet Dance whose shots **deal +5 % per Fixation stack** on the target. A kill during the dance **refunds 10 % ult charge** (Bandit's reset-on-kill, scaled to an ult). | 60 % base per shot | small |

*Feel:* closer to Bandit/Mercenary. You set up sleeping or capped targets, then delete them.

---

## 2. Abrams: "The Unstoppable Detective" (RoR2 analog: Loader / Acrid)

**Deadlock kit (wiki):**
- **Siphon Life:** 8 m, 4 s, 22 dps, 70 % lifesteal, 42 s cd. T1 −20 s cd, T2 +2 s, T3 +2 m and +18 dps.
- **Shoulder Charge:** 1.4 s (~17.5 m), pulls enemies, wall stun 0.3 s, 33 s cd. T1 slows 32 %, T2 +0.8 s stun, T3 −18 s cd and +1.5 weapon damage on hero hit.
- **Infernal Resilience:** a **passive**. It regenerates 13 % of damage taken over 20 s. T1 +200 HP, T2 +18 % melee lifesteal, T3 +9 % regen and 20 % debuff resist.
- **Seismic Impact (ult):** leap up to about 55 m, 100 dmg, 9 m, stun 1.6 s. T3 Unstoppable while casting, +6 m radius.
- **Least active ability: Infernal Resilience**, which is already a passive in Deadlock. v0.2.0 dropped it, so **Abrams has no passive today.**

### Current (v0.2.0)
| Slot | Skill | What it does | Numbers |
|---|---|---|---|
| Passive | *none* | n/a | n/a |
| M2 | **Siphon Life** | Drains every enemy within 13 m and heals 70 % of it | 50 % per 0.25 s for 4 s, 8 s cd |
| Shift | **Shoulder Charge** | Charges, scoops up enemies, stuns on wall slam | 350 %, 1.4 s, stun 1 s, 7 s cd |
| R | **Seismic Impact** | Leaps to the aimed enemy or ground, slams | 2200 %, 12 m, stun 1.6 s; 4500 charge |

### Alt A: "Hard-Boiled" (closest to Deadlock)
| Slot | Skill | What it does | Numbers | Build cost |
|---|---|---|---|---|
| Passive | **Infernal Resilience** | Regenerates **25 % of damage taken** over 5 s (13 % over 20 s is too slow for RoR2), plus +20 % debuff resist (shorter debuffs). | n/a | small (QueueHeal + DamagePatch, both existed in 0.1.0) |
| M2 | **Siphon Life** | As now, but enemies drained are **slowed 25 %**, and the drain ticks also feed Infernal Resilience. | 50 %/0.25 s, 4 s, 13 m, 8 s cd | reuse + small |
| Shift | **Shoulder Charge** | As now, plus a **32 % slow for 3 s** on everything hit (T1) and a **1.1 s** wall stun (T2). | 350 %, 6 s cd | small |
| R | **Seismic Impact** | As now, but **Unstoppable** (no knockback, +50 armor) from cast to landing (T3), and a 15 m impact. | 2200 %, stun 2 s | small |

*Feel:* the Deadlock tank. Charge in, drain, out-heal, then slam.

### Alt B: "Brawler" (built on RoR2's Loader: barrier on hit, close-range punch)
| Slot | Skill | What it does | Numbers | Build cost |
|---|---|---|---|---|
| Passive | **Infernal Resilience: Scrap Edition** | **Shotgun hits within 10 m grant barrier** worth 1 % max HP per pellet, about 9 % for a full point-blank blast (like Loader's Scrap Barrier). Immune to fall damage (he leaps a lot). | n/a | small (Guns on-hit) |
| M2 | **Siphon Grab** | Grapple one enemy (up to 30 m) and drag it to you while draining it. Heavy targets pull Abrams to them instead (like Loader's Spiked Fist). | 300 % + 100 %/s drain for 2 s, 5 s cd | **new kind** |
| Shift | **Bull Rush** | Short charge with **2 charges**. The first enemy hit is knocked back and stunned 1 s; hitting a wall stuns for 2 s and deals double damage. | 400 % (800 % on wall), 0.6 s, 5 s cd | small (DashCharge params) |
| R | **Seismic Impact: Aftershock** | The slam leaves **three expanding shockwave rings** over 1.5 s (3×600 %, each stuns 0.5 s). Kills from the slam **refund 15 % ult charge.** | 1500 % slam + 3×600 % | small |

*Feel:* a Loader-style bruiser. You stay in melee range and live on barrier.

---

## 3. Seven: "The Living Storm" (RoR2 analog: Artificer)

**Deadlock kit (wiki):**
- **Lightning Ball:** a slow ball that damages everything near it. 75 dps, 4.25 m, 5 s, 26 s cd. T1 +1 charge, T2 −28 % move speed on hit, T3 +58 dps and +1.75 m.
- **Static Charge:** tag an enemy; after 3.5 s it stuns 0.9 s and deals 35 in 5 m. 42 s cd. T2 +7 m radius, T3 +0.9 s stun.
- **Power Surge:** 10 s; bullets shock and chain up to 4 jumps in 10 m, once per burst. 50 s cd.
- **Storm Cloud (ult):** a 7 s channel, 10 → 30 m expanding storm, 95 dps, only enemies in line of sight. **Fire marks a lightning strike** (75 dmg, 7 m, knock-away) once per cast.
- **Least active ability: Power Surge**, a timed gun buff. It is a natural RoR2 passive, and v0.2.0 dropped it.

### Current (v0.2.0)
| Slot | Skill | What it does | Numbers |
|---|---|---|---|
| Passive | *none* | n/a | n/a |
| M2 | **Lightning Ball** | Slow orb, shocks everything within 6 m, stops on the first enemy | 25 % per 0.1 s for 5 s, 6 s cd |
| Shift | **Static Charge** | Plants a charge on the aimed enemy or spot; it explodes after 1.5 s and stuns | 450 %, 8 m, stun 0.9 s, 7 s cd |
| R | **Storm Cloud** | Rise and hover. Enemies within a 15 → 35 m expanding radius are struck. +30 armor. | 90 % per 0.3 s for 6 s; 5500 charge |

### Alt A: "Power Surge" (closest to Deadlock)
| Slot | Skill | What it does | Numbers | Build cost |
|---|---|---|---|---|
| Passive | **Power Surge** | The **first bullet of every burst** shocks the target and chains to up to 3 nearby enemies (10 m), once per burst as in Deadlock. Always on. | 60 % per jump, proc 0.2 | small (Guns on-hit; the 0.2.0 chain code exists) |
| M2 | **Lightning Ball** | As now, plus **2 charges** (T1) and a **28 % slow** on enemies it touches (T2). | 25 %/0.1 s, 6 m, 5 s, 6 s cd | small |
| Shift | **Static Charge** | As now, but the stun grows to **1.8 s** (T3) and the radius to 10 m (T2). | 500 %, 8 s cd | reuse (numbers) |
| R | **Storm Cloud** | As now, **plus Seven's gun becomes a lightning-strike marker** while channelling. Each shot calls a strike (750 %, 7 m, knock-away) every 1 s (Deadlock's "Fire marks a strike"). | 90 %/0.3 s for 6 s | small |

*Feel:* true to Deadlock. The gun is always electric, and the storm is the payoff.

### Alt B: "Conductor" (built on RoR2's Artificer, with mobility added)
| Slot | Skill | What it does | Numbers | Build cost |
|---|---|---|---|---|
| Passive | **Capacitor** (from Power Surge) | Hits build **Static** on enemies. At 10 stacks the target **discharges**: 300 % shock to it and 2 nearby enemies, plus a 1 s stun. This mirrors Acrid's Blight/Poison as a stacking identity. | max 10, 5 s | small (buff + Guns on-hit) |
| M2 | **Ball Lightning** | A charged orb (hold up to 1.5 s, like Nano-Bomb). Released, it travels and **arcs to the 3 nearest enemies** every 0.25 s, then bursts. | 400–1200 % burst + arcs 50 %, 5 s cd | small (DlrProjectile charge) |
| Shift | **Static Step** | **Blink 12 m** in the move direction, leaving a Static Charge where you were. It detonates after 1 s: 400 %, 8 m, stun 1 s. Usable while sprinting. | 6 s cd | **new kind** (blink) |
| R | **Thunderhead** | Seven stays grounded and **plants a 20 m storm zone** for 10 s. It strikes up to 4 enemies inside every 0.4 s, and Seven gets **+50 % attack speed** while standing in it. | 150 % per strike | **new kind** (zone ult) |

*Feel:* Artificer-like. You blink, set traps and zone, with mobility Seven currently lacks.

---

## 4. Infernus: "The Burning Man" (RoR2 analog: Acrid; today it is Engineer)

**Deadlock kit (wiki).** In the current game the first ability is called **Napalm**; we used its older name, *Catalyst*.
- **Napalm:** 40 dmg, −28 % move speed for 4 s, the napalm debuff makes the target take +16 % damage for 8 s, 25 s cd. T1 +1 charge, T2 15 % lifesteal from napalmed targets, T3 +17 % damage taken and −33 % healing.
- **Flame Dash:** 3 s dash (hold to extend, up to 20 m), fire trail 30 dps for 4 s, 50 % slow resist, 38 s cd. T3: 2 charges.
- **Afterburn:** a **passive**. Hits build a burn (8.1 %/bullet, 15.4 %/headshot), 14 dps, 3 s, and every hit refreshes and extends it.
- **Concussive Combustion (ult):** becomes a bomb, 3.25 s fuse, 125 dmg, 12 m, stun 1.25 s.
- **Least active ability: Afterburn**, already a passive both in Deadlock and in v0.2.0.

### Current (v0.2.0)
| Slot | Skill | What it does | Numbers |
|---|---|---|---|
| Passive | **Afterburn** | Each hit adds a build-up stack; at 12 the target burns | Burn 100 %/s for 3 s |
| M2 | **Catalyst** | Napalm cone: damage, slow, the target takes +16 % damage from Infernus | 300 %, 22 m cone, 8 s debuff, 6 s cd (real slow ≈22 %) |
| Shift | **Flame Dash** | Fast run leaving a fire trail | ×2.2 speed for 2 s, trail 150 %/s, 7 s cd |
| R | **Concussive Combustion** | 2.5 s fuse, then an explosion that stuns | 2500 %, 18 m, stun 1.25 s; 5000 charge |

### Alt A: "Napalm" (closest to Deadlock)
| Slot | Skill | What it does | Numbers | Build cost |
|---|---|---|---|---|
| Passive | **Afterburn** | **Every hit ignites or refreshes** a burn, as in Deadlock: +0.5 s per hit (crits +1 s), up to 3 s. A lower per-hit tick replaces today's 12-hit wait. | 50 %/s burn, proc 0.3 | small (Ignite per hit) |
| M2 | **Napalm** (renamed, now a lobbed projectile like Deadlock) | A flask that bursts in a 5 m splash. It slows 28 % for 4 s and coats targets in napalm (+16 % damage taken from Infernus for 8 s). **2 charges** (T1). | 300 %, 5 s cd | small (projectile + splash) |
| Shift | **Flame Dash** | **Hold to keep dashing** for up to 3 s (Deadlock), with 50 % slow resistance and a trail lasting 4 s. | ×2.2 speed, trail 150 %/s, 8 s cd | small |
| R | **Concussive Combustion** | As now, but the fuse is **3 s with the stun lengthened to 2 s**, and kills heal 10 % HP each (T2's explosion lifesteal). | 2500 %, 18 m | small |

*Feel:* the Deadlock arsonist. Everything you shoot is always on fire.

### Alt B: "Arsonist" (built on RoR2's Acrid: stacking DoT identity and pools)
| Slot | Skill | What it does | Numbers | Build cost |
|---|---|---|---|---|
| Passive | **Kindling** (Afterburn like Acrid's Blight) | Hits apply **stacking burn**. Each stack is a separate 2 s burn (20 %/s), so damage climbs with sustained fire. Burning enemies that die **explode** for 150 % in 4 m. | stacks uncapped, proc 0 | small (Ignite stacks) |
| M2 | **Molotov** | Lobbed bottle that leaves a **fire pool** for 4 s (like Acrid's Caustic Leap pool). Enemies in the pool gain Kindling and are slowed 28 %. 2 charges. | 200 % impact + 40 %/0.5 s pool, 5 s cd | small (FireTrail/pool) |
| Shift | **Afterburner** | Short burst dash (like Commando's Tactical Dive), **2 charges**, leaving a 6 m flame line. Usable while sprinting. | dash 10 m, line 100 %/s, 4 s cd | reuse (dash_trail, shorter) |
| R | **Supernova** | Becomes a bomb with a 2 s fuse. The explosion **detonates every burning enemy within 30 m** for 300 % + 50 % per Kindling stack, plus a 1.25 s stun. | 1500 % self-blast + detonations | small |

*Feel:* closer to Acrid. You spread fire everywhere, then cash it all in with the ult.

---

## 5. How to choose (recommendation)

| Hero | Recommendation | Why |
|---|---|---|
| Haze | **Alt A** | Her Deadlock kit already plays like a RoR2 survivor. Alt A fixes the pacing (crits stand in for headshots, the first shot out of smoke crits) and keeps the dagger → Fixation → dance combo. Alt B is the pick if you want her closer to Bandit. |
| Abrams | **Alt A** | It restores his real passive (Infernal Resilience), which v0.2.0 dropped. Alt B is great too, but needs a grapple kind. |
| Seven | **Alt B**, or Alt A if you want Deadlock purity | Alt B gives him the mobility every RoR2 survivor has, and a clearer passive. Alt A brings back Power Surge as an always-on passive with no new code. |
| Infernus | **Alt A** | Napalm (correct name) and always-on Afterburn match the Deadlock fantasy. Alt B's stacking-burn and detonate loop is the most "RoR2" option of all. |

A **mix-and-match** is fine: options are per slot, not all-or-nothing. In RoR2 terms, Alt A and Alt B skills could also both ship as **alternate skill variants** that you pick in the loadout. The loadout already shows one family per slot, so adding variants means families with 2 entries (doc 02 §7.2).

**Implementation notes for whichever is chosen:**
- Every new skill gets an `expect` row, so the in-game test proves it works (doc 02 §10).
- **New kinds** (zone, grapple, blink, zone ult) go in `plugin/States`; movement kinds also go in `Survivors.MachineFor`.
- **Passives** live in the gun's on-hit code or `DamagePatch` (doc 02 §7.4). Abrams and Seven need their passive columns filled.
- **Cooldowns** follow the §0 bands. Ult totals stay within 1500–3000 % (preflight).
- **Seven's gun DPS** is really about 440 %/s (doc 02 §7.6). If his passive adds chain damage (Alt A or B), lower his gun's `damagePct` to compensate.
- **Rename Catalyst to Napalm** in every option (it is Deadlock's current name). Its sound events (`Inferno.Incend.*`) are unchanged.

---

## 6. What the creator chose (v0.3.0, 2026-10-16)

Asked slot by slot. Several picks are the creator's own versions rather than A or B.

| Hero | Slot | Pick | Numbers in the sheets |
|---|---|---|---|
| Haze | Passive | **Alt A Fixation** | +2.5 %/stack, max 40, 6 s; crits add 3; every 20 stacks 200 % + 12 % slow 2 s |
| Haze | M2 | **Current Sleep Dagger, retuned** | 350 %, RoR2 stun **4 s**, cooldown **10 s** |
| Haze | Shift | **Current Smoke Bomb, retuned** | invisible **5 s**, **+50 %** speed, cooldown **12 s**; enemies lose her as a target and can't target her while she is invisible |
| Haze | R | **Current Bullet Dance, longer** | **5 s** (49 % per shot, so the total stays in the ult band); "lots of cool VFX built in Unity, similar to Deadlock, not a rip from the Deadlock engine files" |
| Abrams | Passive | **Alt A Infernal Resilience, softened** | heals back **10 % of damage taken over 10 s**; debuffs 20 % shorter |
| Abrams | M2 | **Siphon Life as RoR2's Mired Urn** (creator's idea) | RoR2's own Mired Urn tethers: **3 targets, 25 m, 8 s**, 250 %/s each, heals for all of it; cooldown **15 s** |
| Abrams | Shift | **Current Shoulder Charge, retuned** | lasts **3 s**, **every enemy hit is stunned 1 s** (no wall needed); cooldown **8 s** |
| Abrams | R | **Seismic Impact rebuilt** (creator's version) | rise, **hover up to 3 s with a ground reticle you aim** (≤ 40 m), R or Left Click slams; **he can be hit** while hovering; new effects and re-timed animation; 2200 %, 12 m, stun 1.6 s |
| Seven | Passive | **Alt B Capacitor** | Static max 10, 5 s; at 10: 300 % to the target + 2 nearby, stun 1 s; gun lowered to 48 % per bullet |
| Seven | M2 | **Current Lightning Ball** | its shocks build Static too; **flies through enemies** instead of stopping on the first; rebuilt effects (240 %/s, ticks every 0.25 s) |
| Seven | Shift | **Current Static Charge** | unchanged numbers; a Risk of Rain-style thunderbolt |
| Seven | R | **Alt B Thunderhead**, placed where Seven stands | 20 m zone, 10 s, strikes up to 4 enemies every 0.4 s for 110 %; the attack speed is **RoR2's Warbanner** ward with its ground ring (creator's idea) |
| Infernus | Passive | **Current Afterburn** | unchanged numbers; clear effects on burning enemies |
| Infernus | M2 | **Alt A Napalm** | lobbed flask, 5 m splash, 300 %, 28 % slow 4 s, +16 % damage from Infernus 8 s; 2 charges, 5 s |
| Infernus | Shift | **Alt A Flame Dash** | hold up to 3 s, 50 % slow resistance, burning trail stays **5 s** and hurts enemies in it, enemies he runs into are **knocked aside** to clear his path; 8 s |
| Infernus | R | **Alt A Concussive Combustion** | 3 s fuse, 2500 % in 18 m, stun 2 s, each kill heals 10 % |

How each is built: doc 02 §7.3 (kinds), §7.4 (passives) and §8.1 (effects).
