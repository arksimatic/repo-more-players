using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MorePlayers
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "local.moreplayers";
        public const string Name = "MorePlayers";
        public const string Version = "1.0.0";

        // The game clamps to GameManager.maxPlayersPhoton (20 by default, lowered automatically if Photon rejects it).
        internal static ConfigEntry<int> MaxPlayers;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            MaxPlayers = Config.Bind("General", "MaxPlayers", 10,
                new ConfigDescription("Maximum players in lobbies you host. Only the host needs this mod.",
                    new AcceptableValueRange<int>(2, 20)));
            MaxPlayers.SettingChanged += (_, _) => Apply();

            new Harmony(Guid).PatchAll();
            Log.LogInfo($"{Name} {Version} loaded, max players = {MaxPlayers.Value}");
        }

        internal static void Apply()
        {
            if (GameManager.instance == null)
            {
                return;
            }
            // SetMaxPlayers also updates the live Photon room and Steam lobby when we are the host.
            GameManager.instance.SetMaxPlayers(MaxPlayers.Value);
        }
    }

    [HarmonyPatch(typeof(GameManager), "Awake")]
    internal static class GameManagerAwakePatch
    {
        private static void Postfix() => Plugin.Apply();
    }

    // Re-apply right before a Steam lobby is created, since it is sized from GameManager.maxPlayers.
    [HarmonyPatch(typeof(SteamLobbyManager), nameof(SteamLobbyManager.HostLobby))]
    internal static class HostLobbyPatch
    {
        private static void Prefix() => Plugin.Apply();
    }

    // Re-apply right before Photon rooms are created (private and public/server-list lobbies).
    [HarmonyPatch(typeof(NetworkConnect), nameof(NetworkConnect.OnConnectedToMaster))]
    internal static class ConnectedToMasterPatch
    {
        private static void Prefix() => Plugin.Apply();
    }
}
