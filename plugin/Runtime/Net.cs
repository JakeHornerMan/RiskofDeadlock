using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadlockOfRain.Sheets;
using DeadlockOfRain.States;
using HarmonyLib;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Co-op plumbing (doc 02 §7.7).
    /// <para>The skill states run on every machine (RoR2 networks them from the player who owns the hero), so their own
    /// sounds and visuals already show for everyone; damage stays on the server. Code that runs on the server only (gun
    /// on-hit passives, stack triggers, hits found by a server-side sweep) plays its sounds and visuals through
    /// <see cref="Relay"/>: one RoR2 effect of ours whose EffectData carries what to play, which RoR2's EffectManager already
    /// sends to every player exactly once.</para>
    /// <para>Also: the mod list hash (players with a different version can't join each other), and Melty's join contract
    /// (the host's Steam lobby as "Melty join address", a join= line to join a friend's game).</para></summary>
    public static class Net
    {
        public static EffectDef RelayDef;

        public static void Init()
        {
            MeltyJoin.Init();
        }

        /// <summary>RoR2 refuses players whose mod list differs (KICK_REASON_MOD_MISMATCH). Our entry names the version and the
        /// heroes actually built, because bodies and equipment are numbered by catalog position: a player with other heroes
        /// (another release, or none because Deadlock is missing) must be turned away, not let into a game that disagrees.
        /// Called at the end of content loading, once the heroes are known (the list is read when a lobby is joined).</summary>
        public static void RegisterModList()
        {
            var id = Plugin.Guid + ";" + Plugin.Version + ";" + string.Join(",", Survivors.Released.Where(h => Survivors.Bodies.ContainsKey(h.Id)).Select(h => h.Id));
            if (!NetworkModCompatibilityHelper.networkModList.Contains(id))
                NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Concat(new[] { id });
            Log.Info("Mod list entry: " + id);
        }

        public static void Build(Transform holder)
        {
            var go = new GameObject("DeadlockOfRainRelay");
            go.transform.SetParent(holder, false);
            go.AddComponent<EffectComponent>();
            var va = go.AddComponent<VFXAttributes>();
            va.DoNotPool = true;
            va.vfxPriority = VFXAttributes.VFXPriority.Always;
            va.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            go.AddComponent<RelayFx>();
            RelayDef = new EffectDef(go);
        }
    }

    /// <summary>Plays something on every player's machine, once. Call it only from code that runs on the server alone (or, for
    /// Shot/Reload, on the hero's owner); code that already runs everywhere (the skill states) plays locally instead.</summary>
    public static class Relay
    {
        internal enum Kind : uint { Vfx = 1, Sound = 2, Impact = 3, Flash = 4, Bolt = 5, Shot = 6, Reload = 7 }
        internal static readonly string[] Moments = { "cast", "loop", "hit", "end", "wave", "muzzle", "trigger", "aim", "reload", "hop", "slam" };
        public static int Received;

        static void Send(Kind k, int index, int moment, EffectData d)
        {
            if (Net.RelayDef == null || Net.RelayDef.index == EffectIndex.Invalid || index < 0 || moment < 0) return;
            d.genericUInt = (uint)k | (uint)moment << 4 | (uint)index << 8;
            EffectManager.SpawnEffect(Net.RelayDef.index, d, true);
        }

        static int Moment(string m) => Array.IndexOf(Moments, m);

        public static void Vfx(string id, string moment, Vector3 at, float radius = 1f, GameObject follow = null, Vector3? to = null, float angle = 0f)
        {
            if (id == null || id == "none") return;
            var d = new EffectData { origin = at, scale = radius, genericFloat = angle, genericBool = to.HasValue, start = to ?? at };
            if (follow && follow.GetComponent<NetworkIdentity>()) d.SetNetworkedObjectReference(follow);
            Send(Kind.Vfx, Array.FindIndex(EffectRow.All, r => r.Id == id), Moment(moment), d);
        }

        public static void Sound(string skillId, string moment, Vector3 at) =>
            Send(Kind.Sound, Array.FindIndex(SoundRow.All, r => r.Skill == skillId), Moment(moment), new EffectData { origin = at });

        public static void Impact(string effectId, Vector3 at, float radius = 1f) =>
            Send(Kind.Impact, Array.FindIndex(EffectRow.All, r => r.Id == effectId), 0, new EffectData { origin = at, scale = radius });

        public static void Flash(Vector3 at, Color c, float range, float life) =>
            Send(Kind.Flash, 0, 0, new EffectData { origin = at, color = c, scale = range, genericFloat = life });

        public static void Bolt(Vector3 a, Vector3 b, Color c, float width, float life) =>
            Send(Kind.Bolt, 0, 0, new EffectData { origin = a, start = b, color = c, scale = width, genericFloat = life });

        /// <summary>The owner of a hero fired one shot of its gun: the other players play its sound, flash and recoil.</summary>
        /// <param name="aim">the shot's direction (a projectile gun flies its blob along it on every machine).</param>
        public static void Shot(CharacterBody body, bool left, Vector3 aim = default)
        {
            if (!body || !NetworkServer.active && !NetworkClient.active) return;
            var d = new EffectData { origin = body.corePosition, genericBool = left, start = aim };
            d.SetNetworkedObjectReference(body.gameObject);
            Send(Kind.Shot, 0, 0, d);
        }

        public static void Reload(CharacterBody body)
        {
            if (!body || !NetworkServer.active && !NetworkClient.active) return;
            var d = new EffectData { origin = body.corePosition };
            d.SetNetworkedObjectReference(body.gameObject);
            Send(Kind.Reload, 0, 0, d);
        }

        internal static void Play(EffectData d)
        {
            Received++;
            var k = (Kind)(d.genericUInt & 15);
            int moment = (int)(d.genericUInt >> 4 & 15), index = (int)(d.genericUInt >> 8);
            var root = d.ResolveNetworkedObjectReference();
            switch (k)
            {
                case Kind.Vfx:
                    if (index < EffectRow.All.Length)
                        DeadlockOfRain.Vfx.Play(EffectRow.All[index].Id, Moments[moment], d.origin, d.scale, root ? root.transform : null, d.genericBool ? d.start : (Vector3?)null, d.genericFloat);
                    break;
                case Kind.Sound:
                    if (index < SoundRow.All.Length) Fx.Sound(SoundRow.All[index].Skill, Moments[moment], d.origin);
                    break;
                case Kind.Impact:
                    if (index < EffectRow.All.Length) Fx.Impact(EffectRow.All[index].Id, d.origin, d.scale);
                    break;
                case Kind.Flash:
                    DeadlockOfRain.Vfx.Flash(d.origin, d.color, d.scale, d.genericFloat);
                    break;
                case Kind.Bolt:
                    DeadlockOfRain.Vfx.Bolt(d.origin, d.start, d.color, d.scale, d.genericFloat);
                    break;
                case Kind.Shot:
                case Kind.Reload:
                {
                    var body = root ? root.GetComponent<CharacterBody>() : null;
                    if (!body) break;
                    if (k == Kind.Shot) GunState.BreakSmoke(body);
                    // Wraith's Full Auto ends when its magazine is spent and the gun reloads (the server takes the buff off)
                    if (k == Kind.Reload && NetworkServer.active) Buffs.Clear(body, GunState.FullAutoBuff);
                    // the owner already played it when it fired
                    if (body.hasEffectiveAuthority) break;
                    if (k == Kind.Shot) GunState.ShotFx(body, d.genericBool, d.start);
                    else GunState.ReloadFx(body);
                    MeltyTest.Report("coop-remote-" + (k == Kind.Shot ? "shot" : "reload"), true, body.baseNameToken);
                    break;
                }
            }
        }
    }

    /// <summary>The relay effect's instance: plays what its EffectData describes on this machine, then goes.</summary>
    public sealed class RelayFx : MonoBehaviour
    {
        void Start()
        {
            var ec = GetComponent<EffectComponent>();
            try { if (ec && ec.effectData != null) Relay.Play(ec.effectData); }
            catch (Exception e) { Log.Warn("relay: " + e.Message); }
            Destroy(gameObject);
        }
    }

    /// <summary>Melty's "play together" contract (%LOCALAPPDATA%\DeadlockOfRain\melty.log and melty.properties).
    /// Hosting: whenever this player owns a Steam lobby (RoR2's Multiplayer, Host), its id goes in the log as
    /// "Melty join address: &lt;lobby id&gt;.steamlobby", so Melty can hand friends a join link. Joining: a join= address in
    /// melty.properties is joined once the title screen is up (a Steam lobby, or ip:port for the two-copy test).</summary>
    public sealed class MeltyJoin : MonoBehaviour
    {
        static string dir, log, settings;
        /// <summary>The address this copy was started to join (melty.properties join=), "" when it hosts.</summary>
        public static string JoinAddress = "";
        string join, announced;
        bool tried, done, inLobby;
        float giveUpAt;

        public static void Init()
        {
            try
            {
                dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain");
                Directory.CreateDirectory(dir);
                log = System.IO.Path.Combine(dir, "melty.log");
                settings = System.IO.Path.Combine(dir, "melty.properties");
                File.WriteAllText(log, "");
                var go = new GameObject("DeadlockOfRainMeltyJoin");
                DontDestroyOnLoad(go);
                var mj = go.AddComponent<MeltyJoin>();
                mj.join = JoinAddress = ReadJoin();
                if (mj.join != "") Log.Info("Melty: joining " + mj.join);
            }
            catch (Exception e) { Log.Warn("Melty join contract off: " + e.Message); }
        }

        static string ReadJoin()
        {
            if (!File.Exists(settings)) return "";
            foreach (var line in File.ReadAllLines(settings))
            {
                var p = line.Split(new[] { '=' }, 2);
                if (p[0].Trim() == "join") return p.Length > 1 ? p[1].Trim() : "";
            }
            return "";
        }

        public static void Say(string line)
        {
            try { File.AppendAllText(log, line + "\n"); } catch (Exception e) { Log.Warn(e.Message); }
            Log.Info(line);
        }

        /// <summary>Test mode: the host of a two-copy test listens on this PC's loopback (one Steam account can't join its own lobby).</summary>
        public static void AnnounceDirect(int port) => Say("Melty join address: 127.0.0.1:" + port);

        void Update()
        {
            // the co-op test announces its loopback address instead (one Steam account can't join its own lobby)
            if (join == "") { if (!MeltyTest.Coop) Host(); }
            else Join();
        }

        float nextHostCheck, nextOpen;
        /// <summary>Melty treats every Play as a game friends can join, but RoR2 only has a Steam lobby while the Multiplayer
        /// menu or a multiplayer run is open. Started by Melty (melty.properties exists), the title screen opens one the way
        /// the Multiplayer menu does; a Singleplayer run (host 0, no listening) leaves it again, so solo stays solo. The log only
        /// ever holds the lobby this player owns now: it is emptied when the lobby goes, so Melty never hands out a dead address.</summary>
        void Host()
        {
            if (Time.unscaledTime < nextHostCheck) return;
            nextHostCheck = Time.unscaledTime + 1f;
            try
            {
                var lm = PlatformSystems.lobbyManager;
                if (lm == null) return;
                bool owns = lm.isInLobby && lm.ownsLobby;
                if (owns && NetworkServer.active && NetworkServer.dontListen)
                {
                    Log.Info("Melty: a Singleplayer run started; closing the Steam lobby");
                    RoR2.Console.instance.SubmitCmd(null, "steam_lobby_leave");
                    owns = false;
                }
                var id = owns ? lm.GetLobbyID() : null;
                if (string.IsNullOrEmpty(id) || id == "0") id = null;
                if (id == null && announced != null)
                {
                    announced = null;
                    File.WriteAllText(log, "");
                    Log.Info("Melty: Steam lobby closed; join address cleared");
                }
                if (id != null && id != announced)
                {
                    announced = id;
                    Say("Melty join address: " + id + ".steamlobby");
                }
                if (!lm.isInLobby && !lm.awaitingCreate && !lm.awaitingJoin && Time.unscaledTime >= nextOpen && AtTitle()
                    && !NetworkClient.active && !NetworkServer.active && File.Exists(settings) && !MeltyTest.Active)
                {
                    nextOpen = Time.unscaledTime + 15f;
                    Log.Info("Melty: opening a Steam lobby so friends can join");
                    RoR2.Console.instance.SubmitCmd(null, "steam_lobby_create_if_none");
                }
            }
            catch (Exception e) { Log.Warn("Melty host: " + e.Message); nextHostCheck = Time.unscaledTime + 30f; }
        }

        static bool AtTitle() => RoR2Application.loadFinished && LocalUserManager.isAnyUserSignedIn && RoR2.Console.instance
            && UnityEngine.Object.FindObjectOfType<RoR2.UI.MainMenu.MainMenuController>();

        void Join()
        {
            if (done) return;
            if (!tried)
            {
                // wait for the title screen with a signed-in profile, as a player clicking Join in the menu would
                if (!AtTitle()) return;
                tried = true;
                giveUpAt = Time.unscaledTime + 90f;
                var addr = join.EndsWith(".steamlobby", StringComparison.OrdinalIgnoreCase) ? join.Substring(0, join.Length - ".steamlobby".Length) : join;
                if (addr.Contains(":") || addr.Contains(".")) RoR2.Console.instance.SubmitCmd(null, "connect " + addr);
                else RoR2.Console.instance.SubmitCmd(null, "steam_lobby_join " + addr);
                return;
            }
            // in: connected to the host's game (a lobby join connects to the host's server once its game is open)
            if (NetworkClient.active && !NetworkServer.active && ClientScene.readyConnection != null && NetworkUser.readOnlyInstancesList.Count >= 2)
            {
                done = true;
                Say("Melty joined: " + join);
                return;
            }
            // in the host's lobby but the host hasn't started a game yet: that is waiting, not a failure
            var lm = PlatformSystems.lobbyManager;
            if (!inLobby && lm != null && lm.isInLobby && !lm.ownsLobby)
            {
                inLobby = true;
                Say("Melty in the host's lobby: " + join + " (waiting for the host to start)");
            }
            if (Time.unscaledTime > giveUpAt && !inLobby)
            {
                done = true;
                Say("Melty join failed: no connection to " + join + " after 90 s");
            }
        }
    }

    /// <summary>Two copies of the game on one PC (the co-op test only) are signed in to the same Steam account, which RoR2
    /// refuses ("account already on server"). Only in that test, the joining copy presents a different id and the host
    /// skips the Steam ticket check. Never active outside the test (the test-mode file must exist).</summary>
    static class CoopTestPatches
    {
        public static void Apply(Harmony h, bool joiner)
        {
            if (joiner)
            {
                var auth = AccessTools.Method(typeof(NetworkManagerSystemSteam), "PlatformAuth");
                if (auth != null) h.Patch(auth, postfix: new HarmonyMethod(typeof(CoopTestPatches), nameof(FakeId)));
                else Log.Warn("co-op test: PlatformAuth not found");
            }
            else
            {
                foreach (var name in new[] { "OnAuthDataReceivedFromClient", "OnAuthChange" })
                {
                    var m = AccessTools.Method(AccessTools.TypeByName("RoR2.SteamworksServerManager"), name);
                    if (m != null) h.Patch(m, prefix: new HarmonyMethod(typeof(CoopTestPatches), nameof(Skip)));
                    else Log.Warn("co-op test: " + name + " not found");
                }
            }
        }

        static void FakeId(ref ClientAuthData authData)
        {
            authData.platformId = new RoR2.PlatformID(authData.platformId.ID + 1);
        }

        static bool Skip() => false;
    }
}
