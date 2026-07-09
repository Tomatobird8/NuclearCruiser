using HarmonyLib;
using NuclearCruiser.MonoBehaviours;
using UnityEngine;

namespace NuclearCruiser.Patches;

[HarmonyPatch(typeof(VehicleController))]
public static class VehicleControllerPatch
{
    [HarmonyPatch(nameof(VehicleController.Start))]
    [HarmonyPostfix]
    public static void Start_Postfix(VehicleController __instance)
    {
        if (__instance.vehicleID != 0)
        {
            return;
        }           
        // Cruiser state is only randomized upon purchase.
        ItemDropship ship = Object.FindAnyObjectByType<ItemDropship>(FindObjectsInactive.Exclude);
        if (!__instance.NetworkManager.IsServer || ship == null || __instance.magnetedToShip)
        {
            return;
        }
        System.Random random = new();
        float value = (float)random.NextDouble();
        if (value < NuclearCruiser.nuclearCruiserChance)
        {
            Network.NetworkHandler.Instance.AddCruiserNukerRpc(__instance);
            Terminal terminalScript = HUDManager.Instance.terminalScript;
            if (!terminalScript || NuclearCruiser.nuclearCruiserCompensation.Value <= 0) return;
            terminalScript.groupCredits += NuclearCruiser.nuclearCruiserCompensation.Value;
            terminalScript.SyncGroupCreditsClientRpc(terminalScript.groupCredits, terminalScript.numberOfItemsInDropship);
            if (NuclearCruiser.compensationNotification.Value) HUDManager.Instance.AddTextToChatOnServer($"Nuclear cruiser compensation: ${NuclearCruiser.nuclearCruiserCompensation.Value}");
        }          
    }

    [HarmonyPatch(nameof(VehicleController.UseTurboBoostLocalClient))]
    [HarmonyPostfix]
    public static void UseTurboBoostLocalClient_Postfix(VehicleController __instance)
    {
        if (__instance.vehicleID != 0)
        {
            return;
        }
        if (!__instance.TryGetComponent<CruiserNuker>(out _))
        {
            return;
        }
        if (!NuclearCruiser.infiniteBoosts)
        {
            return;
        }     
        __instance.turboBoosts = 5;      
    }

    [HarmonyPatch(nameof(VehicleController.DestroyCar))]
    [HarmonyPrefix]
    public static void DestroyCar_Postfix(VehicleController __instance)
    {
        if (__instance.vehicleID != 0)
        {
            return;
        }       
        if (__instance.carDestroyed)
        {
            return;
        }
        if (!__instance.TryGetComponent(out CruiserNuker cruiserNuker))
        {
            return;
        }  
        MeshRenderer[] meshRenderers = __instance.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i].transform.name.Contains("MainBody") || 
                meshRenderers[i].transform.name == "CarHoodMesh" || 
                meshRenderers[i].transform.name == "Door")
            {
                meshRenderers[i].materials[0].mainTexture = NuclearCruiser.destroyedCruiserTexture;
            }
        } 
        cruiserNuker.Explode();
    }

    [HarmonyPatch(nameof(VehicleController.OnCollisionEnter))]
    [HarmonyPostfix]
    public static void OnCollisionEnter_Postfix(VehicleController __instance, Collision collision)
    {
        if (__instance.vehicleID != 0)
        {
            return;
        }
        if (!__instance.TryGetComponent(out CruiserNuker cruiserNuker))
        {
            return;
        }   
        if (!__instance.IsOwner || 
            __instance.magnetedToShip || 
            !__instance.hasBeenSpawned || 
            collision.collider.gameObject.layer != 8 || 
            __instance.averageCount < 18)
        {
            return;
        }
        if (NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Default ||
            __instance.averageVelocity.magnitude < NuclearCruiser.minimumCrashVelocity ||
            cruiserNuker.isProtected)
        {
            return;
        }
        float collisionImpulse = 0f;
        int contactCount = collision.GetContacts(__instance.contacts);
        for (int i = 0; i < contactCount; i++)
        {
            if (__instance.contacts[i].impulse.magnitude > collisionImpulse)
            {
                collisionImpulse = __instance.contacts[i].impulse.magnitude;
            }
        }
        collisionImpulse /= Time.fixedDeltaTime;
        if (collisionImpulse > __instance.mediumBumpForce)
        {
            __instance.DealPermanentDamage(NuclearCruiser.crashDamage);
        }
        if (collisionImpulse > __instance.maximumBumpForce && 
            __instance.averageVelocity.magnitude > NuclearCruiser.minimumCrashVelocity * 3)
        {
            __instance.DealPermanentDamage(NuclearCruiser.crashDamage * 2);
        }
    }

    [HarmonyPatch(nameof(VehicleController.CarReactToObstacle))]
    [HarmonyPrefix]
    public static bool CarReactToObstacle_Prefix(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return false;         
        }        
        if (__instance.vehicleID != 0)
        {
            return true;
        }
        if (!__instance.TryGetComponent(out CruiserNuker cruiserNuker))
        {
            return true;
        }
        if (!__instance.IsOwner || __instance.magnetedToShip || __instance.carDestroyed || 
            __instance.averageVelocity.magnitude < NuclearCruiser.minimumCrashVelocity ||
            cruiserNuker.isProtected)
        {
            return true;
        }
        if (NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Extreme)
        {
            __instance.DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
            __instance.DestroyCar();
        }
        if (NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Fragile)
        {
            __instance.DealPermanentDamage(NuclearCruiser.crashDamage);
        }
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.DealPermanentDamage))]
    [HarmonyPrefix]
    public static bool DealPermanentDamage_Prefix(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return false;         
        }
        if (__instance.vehicleID != 0)
        {
            return true;
        }
        if (!__instance.TryGetComponent(out CruiserNuker cruiserNuker))
        {
            return true;
        }
        if (cruiserNuker.isProtected)
        {
            return true;
        }
        if (!__instance.IsOwner || __instance.magnetedToShip || __instance.carDestroyed || 
            NuclearCruiser.cruiserFragility != NuclearCruiser.Fragility.Extreme)
        {
            return true;
        }        
        __instance.DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);            
        __instance.DestroyCar();
        return false;
    }
}
