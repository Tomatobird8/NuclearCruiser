using HarmonyLib;
using UnityEngine;
using NuclearCruiser.Utils;

namespace NuclearCruiser.Patches
{
    [HarmonyPatch(typeof(VehicleController))]
    internal class VehicleControllerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        internal static void StartPatch(VehicleController __instance)
        {
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                System.Random random = new();
                float value = (float)random.NextDouble();
                if (value < NuclearCruiser.nuclearCruiserChance)
                {
                    Network.NetworkHandler.Instance.AddCruiserNukerClientRpc(__instance.NetworkObject);
                }
            }
        }

        [HarmonyPatch("UseTurboBoostLocalClient")]
        [HarmonyPostfix]
        internal static void UseTurboBoostLocalClientPatch(VehicleController __instance)
        {
            if (__instance.GetComponent<CruiserNuker>() == null) return;
            if (NuclearCruiser.infiniteBoosts)
                __instance.turboBoosts = 5;
        }

        [HarmonyPatch("DestroyCar")]
        [HarmonyPostfix]
        internal static void DestroyCarPatch(VehicleController __instance)
        {
            CruiserNuker cn = __instance.GetComponent<CruiserNuker>();
            if (cn == null) return;
            MeshRenderer[] meshRenderers = __instance.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                if (renderer.transform.name == "MainBodyDestroyed" || renderer.transform.name == "CarHoodMesh" || renderer.transform.name == "Door")
                {
                    renderer.materials[0].mainTexture = NuclearCruiser.destroyedCruiserTexture;
                }
            }
            cn.Explode();
        }

    }
}
