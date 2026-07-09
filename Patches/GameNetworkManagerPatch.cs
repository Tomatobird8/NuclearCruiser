using HarmonyLib;
using NuclearCruiser.MonoBehaviours;
using System;

namespace NuclearCruiser.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public static class GameNetworkManagerPatch
{
    [HarmonyPatch(nameof(GameNetworkManager.Start))]
    [HarmonyPostfix]
    public static void Start_Postfix()
    {
        Network.NetworkHandler.CreateAndRegisterPrefab();
    }

    [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
    [HarmonyPrefix]
    public static void Disconnect_Prefix()
    {
        Network.NetworkHandler.DespawnNetworkHandler();
    }

    [HarmonyPatch(nameof(GameNetworkManager.SaveItemsInShip))]
    [HarmonyPostfix]
    public static void SaveItemsInShip_Postfix(GameNetworkManager __instance)
    {
        try
        {
            VehicleController vehicleController = StartOfRound.Instance.attachedVehicle;        
            if (vehicleController && vehicleController.vehicleID == 0 && vehicleController.TryGetComponent<CruiserNuker>(out _))
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
