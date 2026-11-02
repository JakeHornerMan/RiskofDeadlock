RISK OF DEADLOCK - JakeHornerMan's own build
============================================

What this is
------------
Risk of Deadlock puts Deadlock's heroes into Risk of Rain 2 as survivors: Haze, Abrams, Seven, Infernus,
Mo & Krill, Viscous, Rat King, Billy, Wraith, Mina and Pocket, each with their model, animations, sounds,
voice lines and abilities.

This package is my own copy (JakeHornerMan). Every hero's files are built INTO the DLL, so Deadlock does not
need to be installed and nothing is read from it when the game starts. It is for my PC only: never upload or
share this zip, the DLL holds Deadlock's files.

What is in this package (laid out like a Thunderstore release)
--------------------------------------------------------------
  manifest.json                 name Risk_of_Deadlock, author JakeHornerMan, version 1.5.x, its two dependencies
                                (BepInExPack by bbepis, RoR2BepInExPack by RiskofThunder)
  icon.png                      the picture r2modman shows (our own in-game renders of the heroes)
  README.md                     the mod's page in r2modman: the heroes, requirements, installation, hero pages, credits
  CHANGELOG.md                  what changed in every 1.5.x version, with the "Built with" line
  INSTALL.txt                   how to install it by hand or with r2modman, update it and remove everything
  README.txt                    this file
  plugins/DeadlockOfRain.dll    the mod (BepInEx 5 plugin) with all 11 heroes' files inside it (about 600 MB)
  plugins/LICENSE.txt           the mod's licence
  plugins/THIRD-PARTY-NOTICES.txt  the libraries the project uses and their licences
  plugins/built-with.txt        the Risk of Rain 2 build it was made with, the version and the date
r2modman (and INSTALL.txt step A6) puts all of these, plugins/ included, together in one folder:
BepInEx\plugins\JakeHornerMan-Risk_of_Deadlock\

Versions
--------
One version line: 1.5.1, 1.5.2, 1.5.3 ... every new build is one number up (the last one used is in
tools\r2modman_version.txt in the RiskofDeadlock repo). Only ever keep ONE copy of the mod installed.

Where things go on the PC
-------------------------
  The mod (r2modman)       %APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\<profile>\BepInEx\plugins\JakeHornerMan-Risk_of_Deadlock\
  The mod (by hand)        <Risk of Rain 2 folder>\BepInEx\plugins\JakeHornerMan-Risk_of_Deadlock\
  Files the mod unpacks    %LOCALAPPDATA%\DeadlockOfRain\embedded\  (written on the first start of each new DLL, a few seconds)
  The log                  <the BepInEx folder in use>\LogOutput.log  (look for "Playing ... from your own files")

Where the build comes from
--------------------------
My edited hero files are in charactersFinal\<hero>Files\ of the RiskofDeadlock repo. To make a new package:
    bash tools/build_local_hero.sh all charactersFinal --r2modman
(add --r2modman-install to also put it straight into r2modman). The full rebuild guide is
charactersFinal\build&install.md. Never delete the charactersFinal folder.
