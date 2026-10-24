using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2.ContentManagement;
using UnityEngine;

namespace DeadlockOfRain
{
    [BepInPlugin(Guid, "Deadlock of Rain", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "melty.deadlockofrain";
        public const string Version = "1.0.0";
        public static Plugin Instance;
        public static ConfigEntry<float> Volume;
        public static ConfigEntry<string> DeadlockFolder;
        public static string PluginDir;

        void Awake()
        {
            Instance = this;
            Log.Source = Logger;
            PluginDir = Path.GetDirectoryName(Info.Location);
            Volume = Config.Bind("Audio", "Deadlock sound volume", 1f, "Volume of the Deadlock sounds (0-1), on top of the game's master and SFX volume.");

            DeadlockFolder = Config.Bind("Deadlock", "Deadlock folder", "", "Only needed if Deadlock is not found by itself: the folder that holds game/citadel/pak01_dir.vpk.");

            QuitSafety.Init();
            // test-mode "off": "mod": nothing but the quit fixes and the test itself, to see whether a problem needs the mod
            if (MeltyTest.Off("mod")) { MeltyTest.Init(); return; }
            BuiltWith.Read();
            // every start: the loading screen says the Deadlock heroes are being read and built, and names each one (FirstRunOverlay)
            FirstRunOverlay.Show();
            // Thunderstore install: read the heroes from Deadlock now; the loading screen waits for it (Survivors.cs)
            if (!FirstRun.Start(DeadlockCache.DefaultDir(), DeadlockFolder.Value)) OpenCache();

            if (!MeltyTest.Off("harmony")) new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            ContentManager.collectContentPackProviders += add => add(new DeadlockContentProvider());
            Equipment.Init();
            Buffs.InitHooks();
            States.GunHits.Init();
            Net.Init();
            if (!FirstRun.Started && !MeltyTest.Off("audio")) DeadlockAudio.Init();
            Voice.Init();
            DlrHopper.InitTrophies();
            FirstRun.Init();
            MeltyTest.Init();
            DpsBench.Init();
        }

        /// <summary>Opens the extracted cache (at start, or after the first-run extraction on the loading screen).</summary>
        public static void OpenCache()
        {
            DeadlockCache.Open(DeadlockCache.DefaultDir());
            Log.Info($"Deadlock of Rain {Version}: Deadlock content from build {DeadlockCache.DeadlockBuild} at {DeadlockCache.Dir}");
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
