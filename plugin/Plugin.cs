using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2.ContentManagement;
using UnityEngine;

namespace DeadlockOfRain
{
    [BepInPlugin(Guid, "Risk of Deadlock", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "melty.deadlockofrain";
        public const string Version = "1.5.0";
        public static Plugin Instance;
        public static ConfigEntry<float> Volume;
#if !OWN_FILES
        public static ConfigEntry<string> DeadlockFolder;
#endif
        public static string PluginDir;

        void Awake()
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            Instance = this;
            Log.Source = Logger;
            PluginDir = Path.GetDirectoryName(Info.Location);
            Volume = Config.Bind("Audio", "Deadlock sound volume", 1f, "Volume of the Deadlock sounds (0-1), on top of the game's master and SFX volume.");

            // The creator's own hero build (LocalHero: heroes/ beside the DLL, the r2modman package) never looks for Deadlock: no
            // "Deadlock folder" setting and no extractor (a reviewer asked why it seemed to launch one, 2026-11-09)
#if OWN_FILES
            LocalHero.Init();
#else
            DeadlockFolder = Config.Bind("Deadlock", "Deadlock folder", "", "Only needed if Deadlock is not found by itself: your Deadlock folder, e.g. D:\\SteamLibrary\\steamapps\\common\\Deadlock (Steam: Deadlock > Manage > Browse local files). Its game or game\\citadel folder works too.");
#endif

            // every step of the start in the log and start-trail.txt, so a "closed right after starting" report says why (1.2.0)
            StartTrail.Init();
            QuitSafety.Init();
            // test-mode "off": "mod": nothing but the quit fixes and the test itself, to see whether a problem needs the mod
            if (MeltyTest.Off("mod")) { MeltyTest.Init(); return; }
            BuiltWith.Read();
            // every start: the loading screen says the Deadlock heroes are being read and built, and names each one (FirstRunOverlay)
            FirstRunOverlay.Show();
            // Thunderstore install: read the heroes from Deadlock now; the loading screen waits for it (Survivors.cs).
            // Melty: the cache is already there (FirstRun.Start finds no extract/ folder). Own files: straight to the cache.
#if OWN_FILES
            OpenCache();
#else
            if (!FirstRun.Start(DeadlockCache.DefaultDir(), DeadlockFolder.Value)) OpenCache();
#endif

            if (!MeltyTest.Off("harmony")) new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            ContentManager.collectContentPackProviders += add => add(new DeadlockContentProvider());
            Equipment.Init();
            Buffs.InitHooks();
            States.GunHits.Init();
            States.VictorPain.Init();
            WraithThrown.Init();
            Net.Init();
            if (!FirstRun.Started && !MeltyTest.Off("audio")) DeadlockAudio.Init();
            Voice.Init();
            DlrHopper.InitTrophies();
            FirstRun.Init();
            CommunityButton.Init();
            MeltyTest.Init();
            DpsBench.Init();
            // a player's freeze log ended inside this method (1.0.2): how long it took, so the next log shows it
            Log.Info($"mod start took {started.Elapsed.TotalSeconds:F1} s (the sounds load on their own thread)");
        }

        /// <summary>Opens the extracted cache (at start, or after the first-run extraction on the loading screen).</summary>
        public static void OpenCache()
        {
            DeadlockCache.Open(DeadlockCache.DefaultDir());
            Log.Info($"Risk of Deadlock {Version}: Deadlock content from build {DeadlockCache.DeadlockBuild} at {DeadlockCache.Dir}");
            if (!DeadlockCache.Ready && FirstRun.Problem == null)
                FirstRun.Problem = "The Deadlock heroes are missing. Install Deadlock (free on Steam) and restart Risk of Rain 2.\n\n" + BuiltWith.Versions();
        }

        void OnDestroy() => DeadlockAudio.Shutdown();
        void OnApplicationQuit() => DeadlockAudio.Shutdown();
    }

    public static class Log
    {
        public static BepInEx.Logging.ManualLogSource Source;
        public static void Info(string s) => Source?.LogInfo(s);
        public static void Warn(string s) => Source?.LogWarning(s);
        public static void Error(string s) => Source?.LogError(s);
    }
}
