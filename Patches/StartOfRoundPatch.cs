using HarmonyLib;

namespace NuclearCruiser.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static void AwakePrefix()
        {
            Network.NetworkHandler.SpawnNetworkHandler();
        }
    }
}
