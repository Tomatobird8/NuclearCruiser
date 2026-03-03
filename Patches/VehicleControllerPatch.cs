using HarmonyLib;
using NuclearCruiser.Utils;
using UnityEngine;

namespace NuclearCruiser.Patches
{
    [HarmonyPatch(typeof(VehicleController))]
    internal class VehicleControllerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        internal static void StartPatch(VehicleController __instance)
        {
            if (NuclearCruiser.onlyPatchVanillaCruiser && __instance.vehicleID != 0)
            {
                return;
            }
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
        [HarmonyPrefix]
        internal static void DestroyCarPatch(VehicleController __instance)
        {
            if (__instance.carDestroyed)
            {
                return;
            }
            CruiserNuker cn = __instance.GetComponent<CruiserNuker>();
            if (cn == null) return;
            if (__instance.vehicleID != 0) return;
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

        [HarmonyPatch("OnCollisionEnter")]
        [HarmonyPostfix]
        internal static void OnCollisionEnterPatch(VehicleController __instance, ref Collision collision)
        {
            if (!__instance.IsOwner || __instance.magnetedToShip || !__instance.hasBeenSpawned || collision.collider.gameObject.layer != 8 || __instance.averageCount < 18 || NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Normal || __instance.GetComponent<CruiserNuker>() == null || __instance.averageVelocity.magnitude < NuclearCruiser.minimumCrashVelocity)
            {
                return;
            }
            float num = 0f;
            int num2 = collision.GetContacts(__instance.contacts);
            for (int i = 0; i < num2; i++)
            {
                if (__instance.contacts[i].impulse.magnitude > num)
                {
                    num = __instance.contacts[i].impulse.magnitude;
                }
            }
            num /= Time.fixedDeltaTime;

            if (num > __instance.mediumBumpForce)
            {
                __instance.DealPermanentDamage(NuclearCruiser.crashDamage);
            }

            if (num > __instance.maximumBumpForce && __instance.averageVelocity.magnitude > NuclearCruiser.minimumCrashVelocity * 3)
            {
                __instance.DealPermanentDamage(NuclearCruiser.crashDamage * 2);
            }
        }

        [HarmonyPatch("CarReactToObstacle")]
        [HarmonyPrefix]
        internal static bool CarReactToObstaclePatch(VehicleController __instance)
        {
            if (__instance.GetComponent<CruiserNuker>() == null) return true;
            if (StartOfRound.Instance.testRoom == null && !StartOfRound.Instance.inShipPhase && !__instance.magnetedToShip && !__instance.carDestroyed && __instance.IsOwner && __instance.averageVelocity.magnitude > NuclearCruiser.minimumCrashVelocity)
            {
                if (NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Extreme)
                {
                    __instance.DestroyCar();
                    __instance.DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                }
                if (NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Fragile)
                {
                    __instance.DealPermanentDamage(NuclearCruiser.crashDamage);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch("DealPermanentDamage")]
        [HarmonyPrefix]
        internal static bool DealPermanentDamagePatch(VehicleController __instance)
        {
            if (__instance.GetComponent<CruiserNuker>() == null) return true;
            if (NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Extreme && StartOfRound.Instance.testRoom == null && !StartOfRound.Instance.inShipPhase && !__instance.magnetedToShip && !__instance.carDestroyed && __instance.IsOwner)
            {
                __instance.DestroyCar();
                __instance.DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                return false;
            }
            return true;
        }
    }
}
