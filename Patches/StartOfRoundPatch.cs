using HarmonyLib;
using System;
using NuclearCruiser.Utils;

namespace NuclearCruiser.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public static class StartOfRoundPatch
{
    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    public static void Awake_Pre()
    {
        Network.NetworkHandler.SpawnNetworkHandler();
    }

    [HarmonyPatch(nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc))]
    [HarmonyPostfix]
    public static void SyncAlreadyHeldObjectsServerRpc_Post()
    {
        VehicleController vehicleController = StartOfRound.Instance.attachedVehicle;
        if (!vehicleController)
        {
            return;
        }
        if (!vehicleController.gameObject.TryGetComponent<CruiserNuker>(out var cruiserNuker))
        {
            return;
        }  
        Network.NetworkHandler.Instance.AddCruiserNukerClientRpc(vehicleController.NetworkObject);
    }

    [HarmonyPatch(nameof(StartOfRound.LoadAttachedVehicle))]
    [HarmonyPostfix]
    public static void LoadAttachedVehicle_Post() 
    {
        if (!__instance.attachedVehicle || __instance.attachedVehicle.vehicleID != 0)
        {
            return;
        }
        try
        {
            if (ES3.KeyExists(MyPluginInfo.PLUGIN_NAME + NuclearCruiser.IsNuclear, GameNetworkManager.Instance.currentSaveFileName))
            {
                VehicleController vehicleController = StartOfRound.Instance.attachedVehicle;
                bool cruiserState = ES3.Load<bool>(MyPluginInfo.PLUGIN_NAME + NuclearCruiser.IsNuclear, GameNetworkManager.Instance.currentSaveFileName);
                if (cruiserState && !vehicleController.gameObject.TryGetComponent<CruiserNuker>(out _))
                {
                    Network.NetworkHandler.Instance.AddCruiserNukerClientRpc(vehicleController.NetworkObject);
                }
            }
        }
        catch(Exception e)
        {
            NuclearCruiser.Logger.LogError($"Failed to load nuclear cruiser data: {e}");
        }     
    }
