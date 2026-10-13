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
        public const string Version = "0.1.0";
        public static Plugin Instance;
        public static ConfigEntry<float> Volume;
        public static string PluginDir;

        void Awake()
        {
            Instance = this;
            Log.Source = Logger;
            PluginDir = Path.GetDirectoryName(Info.Location);
            Volume = Config.Bind("Audio", "Deadlock sound volume", 1f, "Volume of the Deadlock sounds (0-1), on top of the game's master and SFX volume.");

            DeadlockCache.Open(DeadlockCache.DefaultDir());
            Log.Info($"Deadlock of Rain {Version}: Deadlock content from build {DeadlockCache.DeadlockBuild} at {DeadlockCache.Dir}");

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            ContentManager.collectContentPackProviders += add => add(new DeadlockContentProvider());
            Ult.Init();
            Buffs.InitHooks();
            DeadlockAudio.Init();
            MeltyTest.Init();
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
