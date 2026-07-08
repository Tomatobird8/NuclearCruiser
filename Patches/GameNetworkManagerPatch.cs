using HarmonyLib;
using NuclearCruiser.Utils;
using System;

namespace NuclearCruiser.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public static class GameNetworkManagerPatch
{
    [HarmonyPatch(nameof(GameNetworkManager.Start))]
    [HarmonyPostfix]
    public static void StartPostfix()
    {
        Network.NetworkHandler.CreateAndRegisterPrefab();
    }

    [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
    [HarmonyPrefix]
    public static void DisconnectPostfix()
    {
        Network.NetworkHandler.DespawnNetworkHandler();
    }

    [HarmonyPatch(nameof(GameNetworkManager.SaveItemsInShip))]
    [HarmonyPostfix]
    public static void SaveItemsInShip_Post(GameNetworkManager __instance)
    {
        try
        {
            if (StartOfRound.Instance.attachedVehicle && StartOfRound.Instance.attachedVehicle.vehicleID == 0 && )
            {
                ES3.Save(MyPluginInfo.PLUGIN_NAME + NuclearCruiser.IsNuclear, true, GameNetworkManager.Instance.currentSaveFileName);
            }
            else
            {
                ES3.DeleteKey(MyPluginInfo.PLUGIN_NAME + NuclearCruiser.IsNuclear, GameNetworkManager.Instance.currentSaveFileName);
            }
        }
        catch (Exception e) 
        {
            NuclearCruiser.Logger.LogError($"Failed to save nuclear cruiser data: {e}");
        }
    }
}
