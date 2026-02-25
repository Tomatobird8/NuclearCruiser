using HarmonyLib;

namespace NuclearCruiser.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPostFix()
        {
            Network.NetworkHandler.CreateAndRegisterPrefab();
        }

        [HarmonyPatch("Disconnect")]
        [HarmonyPrefix]
        private static void DisconnectPostfix()
        {
            Network.NetworkHandler.DespawnNetworkHandler();
        }
    }
}
