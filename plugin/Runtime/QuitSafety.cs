using System;
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
        /// <summary>The one Mono build the offset was verified on (its PE SizeOfImage and TimeDateStamp): any other build
        /// (a RoR2 or Unity update) is left alone, so the patch can never land in an unrelated function.</summary>
        const int MonoImageSize = 0xA54000;
        const uint MonoTimeStamp = 0x66FD8CF6;

        [DllImport("kernel32", SetLastError = true)] static extern bool VirtualProtect(IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
        [DllImport("kernel32")] static extern bool FlushInstructionCache(IntPtr process, IntPtr addr, UIntPtr size);
        [DllImport("kernel32")] static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
        [DllImport("kernel32")] static extern UIntPtr VirtualQuery(IntPtr addr, out MemoryInfo info, UIntPtr size);

        [StructLayout(LayoutKind.Sequential)]
        struct MemoryInfo { public IntPtr BaseAddress, AllocationBase; public uint AllocationProtect; public IntPtr RegionSize; public uint State, Protect, Type; }

        /// <summary>True when [addr, addr+size) is committed memory with these protection bits and no guard page.</summary>
        static bool Readable(IntPtr addr, int size, uint protectBits)
        {
            if (VirtualQuery(addr, out var mi, (UIntPtr)Marshal.SizeOf(typeof(MemoryInfo))) == UIntPtr.Zero) return false;
            long end = mi.BaseAddress.ToInt64() + mi.RegionSize.ToInt64();
            return mi.State == 0x1000 /*MEM_COMMIT*/ && (mi.Protect & protectBits) != 0 && (mi.Protect & 0x100 /*PAGE_GUARD*/) == 0
                   && addr.ToInt64() + size <= end;
        }

        public static void Init()
        {
            if (!MeltyTest.Off("consolefix")) KeepConsoleOnQuit();
            if (!MeltyTest.Off("soefix")) FixStackOverflowRecovery();
        }

        static void KeepConsoleOnQuit()
        {
            try
            {
                var redirector = typeof(RoR2.RoR2Application).Assembly.GetType("RoR2.UnitySystemConsoleRedirector");
                var disengage = redirector == null ? null : AccessTools.Method(redirector, "Disengage");
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
                var mono = GetModuleHandleW("mono-2.0-bdwgc.dll");
                if (mono == IntPtr.Zero) { Log.Warn("stack overflow fix: Mono not found"); return; }
                // this exact Mono build only: its PE header's SizeOfImage and TimeDateStamp
                int pe = Marshal.ReadInt32(mono + 0x3C);
                uint stamp = (uint)Marshal.ReadInt32(mono + pe + 8);
                int size = Marshal.ReadInt32(mono + pe + 0x50);
                if (size != MonoImageSize || stamp != MonoTimeStamp) { Log.Info($"stack overflow fix: not needed for this Mono build ({size:X}/{stamp:X})"); return; }
                var thunk = Marshal.ReadIntPtr(mono + RestoreStackPointerOffset);
                // the thunk must be real, executable, committed code before anything reads or writes it
                if (thunk == IntPtr.Zero || !Readable(thunk, 8, 0x10 | 0x20 | 0x40 | 0x80 /*PAGE_EXECUTE*/)) { Log.Warn("stack overflow fix: Mono's thunk isn't there"); return; }
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
