using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;

namespace DeadlockOfRain
{
    /// <summary>Stops Risk of Rain 2 crashing when it closes after a run (ntdll c0000005: 0.7.2/0.7.3 players on Melty, and
    /// this PC even with the mod switched off; doc 02 §12):
    /// 1. The cause: RoR2 remembers the console writer at start (UnitySystemConsoleRedirector.Redirect), which under BepInEx
    ///    is BepInEx's own LoggedTextWriter, and puts it back on quit (Disengage). BepInEx's Console.SetOut patch then makes
    ///    that writer write to itself, so the next console line during the final clean-up recurses until the main thread's
    ///    stack overflows. Disengage is skipped: it only restores the console for a process that is ending anyway.
    /// 2. The safety net, ported from RiskofThunder's RoR2BepInExPack (MIT, Copyright (c) 2023 Risk of Thunder,
    ///    THIRD-PARTY-NOTICES.txt), which r2modman players have and Melty doesn't install: Mono's stack-overflow recovery
    ///    thunk (restore_stack) starts with `push rbp`, which leaves the stack 8 bytes off, so any stack overflow crashed the
    ///    game natively instead of becoming a StackOverflowException. The `push rbp` becomes a nop (only if the bytes are
    ///    exactly the expected ones).
    /// Test-mode "off": "consolefix" / "soefix" switch each one off (tools/quit_test.sh).</summary>
    public static class QuitSafety
    {
        /// <summary>RoR2's mono-2.0-bdwgc.dll (Unity 2021.3.45): where Mono keeps its pointer to the restore_stack thunk.</summary>
        const int RestoreStackPointerOffset = 0x757488;
        const ulong RestoreStackPrologue = 0x20_ec_83_48_ec_8b_48_55;   // push rbp; mov rbp,rsp; sub rsp,20h

        [DllImport("kernel32", SetLastError = true)] static extern bool VirtualProtect(IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
        [DllImport("kernel32")] static extern bool FlushInstructionCache(IntPtr process, IntPtr addr, UIntPtr size);
        [DllImport("kernel32")] static extern IntPtr GetCurrentProcess();

        public static void Init()
        {
            if (!MeltyTest.Off("consolefix")) KeepConsoleOnQuit();
            if (!MeltyTest.Off("soefix")) FixStackOverflowRecovery();
        }

        static void KeepConsoleOnQuit()
        {
            try
            {
                var disengage = AccessTools.Method(AccessTools.TypeByName("RoR2.UnitySystemConsoleRedirector"), "Disengage");
                if (disengage == null) { Log.Warn("console fix: RoR2's console redirector not found"); return; }
                new Harmony(Plugin.Guid + ".quitsafety").Patch(disengage, prefix: new HarmonyMethod(typeof(QuitSafety), nameof(SkipDisengage)));
                Log.Info("console fix: applied");
            }
            catch (Exception e) { Log.Warn("console fix: " + e.Message); }
        }

        static bool SkipDisengage() => false;

        static void FixStackOverflowRecovery()
        {
            try
            {
                var mono = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName == "mono-2.0-bdwgc.dll");
                if (mono == null || mono.ModuleMemorySize <= RestoreStackPointerOffset + 8) { Log.Warn("stack overflow fix: Mono not found"); return; }
                var thunk = Marshal.ReadIntPtr(mono.BaseAddress + RestoreStackPointerOffset);
                if (thunk == IntPtr.Zero) { Log.Warn("stack overflow fix: Mono's thunk isn't there"); return; }
                ulong prologue = (ulong)Marshal.ReadInt64(thunk);
                // RoR2BepInExPack (Thunderstore players) may have patched it already
                if (prologue == ((RestoreStackPrologue & ~0xFFUL) | 0x90)) { Log.Info("stack overflow fix: already applied"); return; }
                if (prologue != RestoreStackPrologue) { Log.Warn($"stack overflow fix: not applied, unexpected Mono code {prologue:X16}"); return; }
                if (!VirtualProtect(thunk, (UIntPtr)8, 0x40 /*PAGE_EXECUTE_READWRITE*/, out uint old)) { Log.Warn("stack overflow fix: memory is read-only"); return; }
                Marshal.WriteByte(thunk, 0x90);
                VirtualProtect(thunk, (UIntPtr)8, old, out _);
                FlushInstructionCache(GetCurrentProcess(), thunk, (UIntPtr)128);
                Log.Info("stack overflow fix: applied");
            }
            catch (Exception e) { Log.Warn("stack overflow fix: " + e.Message); }
        }
    }
}
