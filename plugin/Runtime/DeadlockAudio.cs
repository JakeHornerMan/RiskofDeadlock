using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Plays the Deadlock sounds the extractor decoded from the player's install. RoR2 turns Unity's own audio off
    /// (it uses Wwise), so this is a tiny software mixer on Windows' waveOut: 44.1 kHz stereo, 4 x 20 ms buffers.</summary>
    public static class DeadlockAudio
    {
        const int Rate = 44100, BufferFrames = 882, Buffers = 4;
        sealed class Clip { public float[] L, R; }
        sealed class Voice { public Clip clip; public int pos; public float vol, pan; public bool loop; public int id; }
        sealed class Event { public float volume; public List<Clip> clips = new List<Clip>(); }

        static readonly Dictionary<string, Event> events = new Dictionary<string, Event>(StringComparer.OrdinalIgnoreCase);
        static readonly List<Voice> voices = new List<Voice>();
        static readonly object gate = new object();
        static readonly System.Random rng = new System.Random();
        static Thread thread;
        static volatile bool running;
        static volatile float masterVolume = 1f;
        static IntPtr device;
        static int nextId = 1;
        public static int Played;

        public static void Init()
        {
            try
            {
                LoadIndex();
                if (events.Count == 0) return;
                running = true;
                thread = new Thread(Run) { IsBackground = true, Name = "DeadlockAudio" };
                thread.Start();
                RoR2Application.onUpdate += UpdateVolume;
            }
            catch (Exception e) { Log.Warn("Deadlock audio disabled: " + e.Message); }
        }

        public static void Shutdown()
        {
            running = false;
        }

        static float nextVolumeCheck;
        static void UpdateVolume()
        {
            if (Time.unscaledTime < nextVolumeCheck) return;
            nextVolumeCheck = Time.unscaledTime + 1f;
            float v = 1f;
            try
            {
                var c = RoR2.Console.instance;
                if (c)
                {
                    var m = c.FindConVar("volume_master"); var s = c.FindConVar("volume_sfx");
                    if (m != null && float.TryParse(m.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mv)) v *= mv / 100f;
                    if (s != null && float.TryParse(s.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sv)) v *= sv / 100f;
                }
            }
            catch { }
            if (!Application.isFocused) v *= 0f;
            masterVolume = v * Mathf.Clamp01(Plugin.Volume.Value) * 0.55f;
        }

        static void LoadIndex()
        {
            var dir = System.IO.Path.Combine(DeadlockCache.Dir ?? "", "sounds");
            var index = System.IO.Path.Combine(dir, "sounds.json");
            if (!File.Exists(index)) { Log.Warn("no Deadlock sounds in cache"); return; }
            // tiny parser for {"Event": {"volume": 1.0, "files": ["a.wav", ...]}, ...}
            var text = File.ReadAllText(index);
            int i = 0;
            while (true)
            {
                int q1 = text.IndexOf('"', i); if (q1 < 0) break;
                int q2 = text.IndexOf('"', q1 + 1);
                var name = text.Substring(q1 + 1, q2 - q1 - 1);
                int obj = text.IndexOf('{', q2); int end = text.IndexOf('}', obj);
                if (obj < 0 || end < 0) break;
                var body = text.Substring(obj, end - obj);
                var ev = new Event { volume = 1f };
                int vi = body.IndexOf("\"volume\"", StringComparison.Ordinal);
                if (vi >= 0)
                {
                    int colon = body.IndexOf(':', vi); int comma = body.IndexOfAny(new[] { ',', '\n', '}' }, colon + 1);
                    if (comma < 0) comma = body.Length;
                    float.TryParse(body.Substring(colon + 1, comma - colon - 1).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ev.volume);
                }
                int fi = body.IndexOf('[');
                int fe = body.IndexOf(']', fi);
                foreach (var part in body.Substring(fi + 1, fe - fi - 1).Split(','))
                {
                    var fn = part.Trim().Trim('"');
                    if (fn.Length == 0) continue;
                    var clip = ReadWav(System.IO.Path.Combine(dir, fn));
                    if (clip != null) ev.clips.Add(clip);
                }
                if (ev.clips.Count > 0) events[name] = ev;
                i = end + 1;
            }
            Log.Info($"Deadlock sounds: {events.Count} events loaded");
        }

        static Clip ReadWav(string file)
        {
            try
            {
                using (var r = new BinaryReader(File.OpenRead(file)))
                {
                    r.ReadBytes(12);
                    int channels = 1, rate = Rate, bits = 16;
                    byte[] data = null;
                    while (r.BaseStream.Position < r.BaseStream.Length - 8)
                    {
                        var id = new string(r.ReadChars(4)); int len = r.ReadInt32();
                        if (id == "fmt ") { r.ReadInt16(); channels = r.ReadInt16(); rate = r.ReadInt32(); r.ReadInt32(); r.ReadInt16(); bits = r.ReadInt16(); r.ReadBytes(len - 16); }
                        else if (id == "data") { data = r.ReadBytes(len); break; }
                        else r.ReadBytes(len);
                    }
                    if (data == null || bits != 16) return null;
                    int frames = data.Length / 2 / channels;
                    int outFrames = (int)((long)frames * Rate / rate);
                    var c = new Clip { L = new float[outFrames], R = new float[outFrames] };
                    for (int o = 0; o < outFrames; o++)
                    {
                        double src = (double)o * rate / Rate; int a = (int)src; float t = (float)(src - a);
                        int b = Math.Min(a + 1, frames - 1);
                        float S(int f, int ch) => BitConverter.ToInt16(data, (f * channels + Math.Min(ch, channels - 1)) * 2) / 32768f;
                        c.L[o] = S(a, 0) * (1 - t) + S(b, 0) * t;
                        c.R[o] = S(a, 1) * (1 - t) + S(b, 1) * t;
                    }
                    return c;
                }
            }
            catch { return null; }
        }

        public static bool Has(string ev) => ev != null && ev != "none" && events.ContainsKey(ev);

        /// <summary>Play a Deadlock sound event at a world position (null = 2D). Returns a voice id for loops.</summary>
        public static int Play(string ev, Vector3? at = null, bool loop = false, float gain = 1f)
        {
            if (!running || !Has(ev)) return 0;
            var e = events[ev];
            float vol = Mathf.Clamp(e.volume, 0.2f, 2f) * gain, pan = 0f;
            if (at.HasValue)
            {
                var cam = Camera.main;
                if (cam)
                {
                    var d = at.Value - cam.transform.position;
                    float dist = d.magnitude;
                    vol *= Mathf.Clamp01(1f - Mathf.Max(0, dist - 10f) / 90f);
                    pan = Mathf.Clamp(Vector3.Dot(d.normalized, cam.transform.right), -1f, 1f) * 0.6f;
                }
            }
            if (vol <= 0.01f) return 0;
            lock (gate)
            {
                if (voices.Count > 24) voices.RemoveAt(0);
                var v = new Voice { clip = e.clips[rng.Next(e.clips.Count)], vol = vol, pan = pan, loop = loop, id = nextId++ };
                voices.Add(v);
                Played++;
                return v.id;
            }
        }

        public static void Stop(int id)
        {
            if (id == 0) return;
            lock (gate) voices.RemoveAll(v => v.id == id);
        }

        // ---------------- waveOut ----------------
        [StructLayout(LayoutKind.Sequential)]
        struct WaveFormatEx { public short wFormatTag, nChannels; public int nSamplesPerSec, nAvgBytesPerSec; public short nBlockAlign, wBitsPerSample, cbSize; }
        [StructLayout(LayoutKind.Sequential)]
        struct WaveHdr { public IntPtr lpData; public int dwBufferLength, dwBytesRecorded; public IntPtr dwUser; public int dwFlags, dwLoops; public IntPtr lpNext, reserved; }
        [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr hwo, int dev, ref WaveFormatEx fmt, IntPtr cb, IntPtr inst, int flags);
        [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr hwo, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr hwo, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr hwo);
        [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr hwo);

        static void Run()
        {
            var fmt = new WaveFormatEx { wFormatTag = 1, nChannels = 2, nSamplesPerSec = Rate, wBitsPerSample = 16, nBlockAlign = 4, nAvgBytesPerSec = Rate * 4 };
            if (waveOutOpen(out device, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0) { Log.Warn("waveOutOpen failed; Deadlock sounds off"); return; }
            int hdrSize = Marshal.SizeOf(typeof(WaveHdr));
            int bytes = BufferFrames * 4;
            var hdrs = new IntPtr[Buffers];
            var datas = new IntPtr[Buffers];
            var pcm = new short[BufferFrames * 2];
            var mixL = new float[BufferFrames]; var mixR = new float[BufferFrames];
            for (int b = 0; b < Buffers; b++)
            {
                datas[b] = Marshal.AllocHGlobal(bytes);
                hdrs[b] = Marshal.AllocHGlobal(hdrSize);
                Marshal.StructureToPtr(new WaveHdr { lpData = datas[b], dwBufferLength = bytes, dwFlags = 1 /*WHDR_DONE*/ }, hdrs[b], false);
            }
            int flagsOffset = (int)Marshal.OffsetOf(typeof(WaveHdr), "dwFlags");
            bool[] prepared = new bool[Buffers];
            try
            {
                while (running)
                {
                    bool wrote = false;
                    for (int b = 0; b < Buffers; b++)
                    {
                        int flags = Marshal.ReadInt32(hdrs[b], flagsOffset);
                        if ((flags & 1) == 0) continue; // still playing
                        if (prepared[b]) { waveOutUnprepareHeader(device, hdrs[b], hdrSize); prepared[b] = false; }
                        Array.Clear(mixL, 0, mixL.Length); Array.Clear(mixR, 0, mixR.Length);
                        float mv = masterVolume;
                        lock (gate)
                        {
                            for (int i = voices.Count - 1; i >= 0; i--)
                            {
                                var v = voices[i];
                                float gl = v.vol * mv * (1f - Math.Max(0, v.pan)), gr = v.vol * mv * (1f + Math.Min(0, v.pan));
                                var c = v.clip; int n = c.L.Length;
                                for (int f = 0; f < BufferFrames; f++)
                                {
                                    if (v.pos >= n) { if (v.loop) v.pos = 0; else break; }
                                    mixL[f] += c.L[v.pos] * gl; mixR[f] += c.R[v.pos] * gr; v.pos++;
                                }
                                if (!v.loop && v.pos >= n) voices.RemoveAt(i);
                            }
                        }
                        for (int f = 0; f < BufferFrames; f++)
                        {
                            pcm[f * 2] = (short)(Math.Max(-1f, Math.Min(1f, mixL[f])) * 32767);
                            pcm[f * 2 + 1] = (short)(Math.Max(-1f, Math.Min(1f, mixR[f])) * 32767);
                        }
                        Marshal.Copy(pcm, 0, datas[b], pcm.Length);
                        Marshal.WriteInt32(hdrs[b], flagsOffset, 0);
                        waveOutPrepareHeader(device, hdrs[b], hdrSize); prepared[b] = true;
                        waveOutWrite(device, hdrs[b], hdrSize);
                        wrote = true;
                    }
                    if (!wrote) Thread.Sleep(4);
                }
            }
            finally
            {
                waveOutReset(device);
                for (int b = 0; b < Buffers; b++) { if (prepared[b]) waveOutUnprepareHeader(device, hdrs[b], hdrSize); }
                waveOutClose(device);
            }
        }
    }
}
