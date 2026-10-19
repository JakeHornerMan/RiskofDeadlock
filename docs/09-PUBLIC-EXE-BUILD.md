# 09: The public exe build (DeadlockExtract.exe on GitHub)

What we did on 2026-10-19, why, and how to use it. Short version: **GitHub now builds the extractor exe from the public
source code and publishes it with a checksum, so anyone can check it is safe and matches the code.**

## 1. Why
- Thunderstore rejected "Risk_of_Deadlock" 0.7.0 ("Invalid submission"). On the RoR2 modding Discord, the moderator Harb
  asked: "Why does the extractor need to be it's own exe?" He added: "This is c# code, you could load it as library or
  just directly embed it into your dll".
- Moderators are careful with exes in mods, because hiding malware in a mod's exe is a known trick.
- **Why it is an exe:** the Valve file reader we use (ValveResourceFormat, "VRF") only runs on .NET 10. Risk of Rain 2
  runs mods on Unity's older .NET, which can't load it.

## 2. The options we weighed
| Option | Work | Upkeep after a Deadlock update | Chosen? |
|---|---|---|---|
| **Keep the exe and have GitHub build it in public** | About an hour | None extra: update VRF, tag, done | **Yes** |
| Move the reader into the mod DLL (what Harb suggested) | Several days | We keep our own patched copy of VRF and redo our patches on every VRF update: often an hour, sometimes days | Kept as a fallback plan |
| Ship without the reader; players download it themselves | Small | None | No: clunky for players |

**Why the DLL route costs more upkeep.** With the exe, other people (the VRF authors) fix the reader when Valve changes
Deadlock's files. We bump one version number and rebuild; the 6795 fix took minutes. With the DLL we would also have to
port their fix into our own copy every time. Either way a rebuild is needed when Valve changes the file format. When Valve
only moves files to other folders, neither route needs one: the extractor looks models up in Deadlock's own hero list
(doc 01, extractor 1.4.0).

The creator first picked the DLL rewrite, then chose to keep that plan as notes and asked: "have GitHub build the exe".

## 3. What was built
- **`.github/workflows/extractor.yml`**: a GitHub Actions workflow. When a tag `extractor-X.Y.Z` is pushed, it:
  1. checks out the repo on GitHub's Windows machine and installs .NET 10
  2. fails if the tag doesn't match `Program.Version` in `extractor/Program.cs`
  3. builds the exe with the same `dotnet publish` line as doc 01 §6.7
  4. computes its SHA-256 checksum
  5. makes a public **Release** with:
     - the exe, `DeadlockExtract.exe.sha256`, THIRD-PARTY-NOTICES and LICENSE
     - a link to the source at that exact commit and the build log
     - what the exe does and why it is an exe

  "Run workflow" on the Actions page builds without making a release (a test build).
- **`tools/fetch_extractor.py <version>`**: downloads the release's exe into `build/extract/`, checks it against the
  release's checksum, and records it in `thunderstore/extractor.json`.
- **`tools/package.py --thunderstore`** refuses to build the store zip unless the exe matches `thunderstore/extractor.json`.
  The Thunderstore download therefore always carries exactly the public exe. Two builds on different PCs are never
  byte-identical, so the downloaded one is used and not a local build.
- **"About DeadlockExtract.exe"** section in `README.md` and `thunderstore/README.md`: what it does, why it is a separate
  program, and a link to the release.

## 4. The first release
- https://github.com/JakeHornerMan/RiskofDeadlock/releases/tag/extractor-1.4.0
- SHA-256 `175b88451351c7d7416abea0a33bacc7a0a5087f91a9fa43bb32f96c1c0475a1`
- **Checked:**
  - the GitHub exe read Haze from Deadlock build 6795 in 37 s (exit 0), and its `haze.dlm` is byte-identical to the local build's
  - `package.py 0.7.2 --thunderstore` packed it, with the same checksum inside the zip

## 5. How to release a new extractor version
Only needed when `extractor/*.cs` or the VRF version changes:
1. Bump `Version` in `extractor/Program.cs`, then build and test locally as usual (doc 01 §6.7).
2. Commit and `git push origin main`.
3. `git tag extractor-X.Y.Z` and `git push origin extractor-X.Y.Z`.
4. Wait for the Release to appear, about 3 minutes: https://github.com/JakeHornerMan/RiskofDeadlock/releases
5. `python tools/fetch_extractor.py X.Y.Z`, then commit `thunderstore/extractor.json`.
6. Update the release link in both READMEs' "About DeadlockExtract.exe" sections.
7. `python tools/package.py <mod version> --thunderstore`.

Melty zips are not affected. They may use either exe, and Melty's recipe has no checksum rule.

## 6. How anyone can check the exe
1. Open the release and the build log it links to: you can see the code it was built from and every step.
2. Hash your copy and compare with the release's `DeadlockExtract.exe.sha256`:
   - Windows: `certutil -hashfile DeadlockExtract.exe SHA256`
   - PowerShell: `Get-FileHash DeadlockExtract.exe`
3. What it does: opens Deadlock's `pak01_dir.vpk` read-only and writes `%LOCALAPPDATA%\DeadlockOfRain\cache`. It makes no
   network connections and touches no other files.

## 7. What is still open
- The creator replies to Harb with the release link and re-uploads to Thunderstore.
- **If Thunderstore refuses any exe**, the fallback is the DLL plan in the appendix.

## Appendix: the DLL fallback plan (not started)
**Goal:** a `DeadlockReader.dll` (netstandard2.1) that the plugin loads and runs in-process on a background thread.
- It writes the **same cache** (DLM, PNGs, sounds.json, ready.json), so the rest of the mod is unchanged.
- No exe and no native DLLs ship.
- Every rule of CLAUDE.md "Fallback system" is kept.

**Blockers found:** VRF 20.1.7467 and its dependencies target only .NET 10 (ValveKeyValue, ValvePak, TinyBCSharp net8,
KeyValues2 net9, SkiaSharp native). VRF also uses .NET-10-only APIs: CollectionsMarshal, BitOperations, SearchValues,
FrozenDictionary, GeneratedRegex, ReadExactly and Vector128. It uses no generic math.

1. **`reader/DeadlockReader.csproj`**, multi-target `netstandard2.1;net10.0`, with one shared source tree.
   - **`reader/vendor/vrf/`**: only the VRF files the pipeline needs:
     - VPK reading, the Resource blocks, KV3 (including type 26) and the KV object model
     - Model and skeleton, Mesh and meshoptimizer decoding
     - animations, including NmClip
     - Material, Texture and BCn decoders, and Sound
   - **`tools/sync_vrf.py`** copies that list from a VRF tag and re-applies `reader/vendor/vrf.patch`. Patches stay small
     `#if NETSTANDARD2_1` branches, so an update is "bump tag, sync, build".
   - **`reader/Polyfills.cs`** stands in for the missing APIs. Managed only:
     - LZ4 and ZstdSharp, which have netstandard2.1 builds, plus NLayer
     - a managed PNG writer using DeflateStream, with no SkiaSharp
2. **`reader/DlmBuilder.cs`** replaces glTF export → SharpGLTF → `DlmWriter`.
   - It writes DLM1 straight from VRF's skeleton, mesh, animation and material objects, with the same Unity-space conversion.
   - Additive clips are composed over the bind pose, and the normal maps are skipped.
3. **`reader/Extract.cs`** holds `Program.Run` and everything around it, except argument parsing (same stamp, `.tmp` swap,
   `failed` list, progress.txt and log).
   - A small JSON reader and writer replaces System.Text.Json, which Mono doesn't have.
   - `extractor/` becomes a thin net10 wrapper for Melty and the dev tools.
4. **Plugin:** `FirstRun.cs` runs `Extract.Run` on a `Thread` instead of starting the exe. The progress and log files and
   the loading screen stay the same.
5. **Packaging:** the Thunderstore zip ships `DeadlockReader.dll` and its managed dependencies, with no `extract/` folder.
   The packager refuses any `.exe` or native DLL.

**Order of work:**
1. Spike first: Haze only, in-process. If it doesn't run on Mono or takes over 5 minutes, stop and report.
2. Then parity against the current cache with a new `tools/compare_cache.py`.
3. Then all heroes, the exe wrapper, the plugin, packaging and docs.

**Cost:** several days up front. After each VRF update, re-applying the patches takes about an hour, sometimes days.
