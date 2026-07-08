using HarmonyLib;
using NuclearCruiser.Utils;
using UnityEngine;

namespace NuclearCruiser.Patches;

[HarmonyPatch(typeof(VehicleController))]
public static class VehicleControllerPatch
{
    [HarmonyPatch(nameof(VehicleController.Start))]
    [HarmonyPostfix]
    public static void Start_Post(VehicleController __instance)
    {
        // best to just never patch custom vehicles, as who knows what litany of issues this could just end up causing.
        if (__instance.vehicleID != 0)
        {
            return;
        }           
        // not sure what the intent is here, if it's supposed to be randomised when bought, using inShipPhase is probably better here (or check magnet state)
        // also we can just check server instead of "is host or is server" since both will yield true anyways, and server makes the most sense here
        if (!__instance.NetworkManager.IsServer || StartOfRound.Instance.inShipPhase || __instance.magnetedToShip)
        {
            return;
        }
        System.Random random = new();
        float value = (float)random.NextDouble();
        if (value < NuclearCruiser.nuclearCruiserChance)
        {
            Network.NetworkHandler.Instance.AddCruiserNukerRpc(__instance);
        }          
        /*
        if ((__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer) && StartOfRound.Instance.attachedVehicle != __instance)
        {
            System.Random random = new();
            float value = (float)random.NextDouble();
            if (value < NuclearCruiser.nuclearCruiserChance)
            {
                Network.NetworkHandler.Instance.AddCruiserNukerClientRpc(__instance.NetworkObject);
            }
        }
        */
    }

    [HarmonyPatch(nameof(VehicleController.UseTurboBoostLocalClient))]
    [HarmonyPostfix]
    public static void UseTurboBoostLocalClient_Post(VehicleController __instance)
    {
        if (__instance.vehicleID != 0)
        {
            return;
        }
        // discard
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
    public static void DestroyCar_Post(VehicleController __instance)
    {
        if (__instance.vehicleID != 0)
        {
            return;
        }       
        if (__instance.carDestroyed)
        {
            return;
        }
        // TryGetComponent is cheaper than GetComponent
        if (!__instance.TryGetComponent<CruiserNuker>(out var cruiserNuker))
        {
            return;
        }  
        MeshRenderer[] meshRenderers = __instance.GetComponentsInChildren<MeshRenderer>();
        // for loop is cheaper than a foreach
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i].transform.name.Contains("MainBody") || 
                meshRenderers[i].transform.name == "CarHoodMesh" || 
                meshRenderers[i].transform.name == "Door")
            {
                meshRenderers[i].materials[0].mainTexture = NuclearCruiser.destroyedCruiserTexture;
            }
        }
        /*
        foreach (var renderer in meshRenderers)
        {
            if (renderer.transform.name.Contains("MainBody") || renderer.transform.name == "CarHoodMesh" || renderer.transform.name == "Door")
            {
                renderer.materials[0].mainTexture = NuclearCruiser.destroyedCruiserTexture;
            }
        }
        */

        /*
        MeshRenderer? m = __instance.transform.Find("Meshes")?.Find("MainBodyDestroyed").GetComponent<MeshRenderer>();
        if (m != null) m.materials[0].mainTexture = NuclearCruiser.destroyedCruiserTexture;
        */

        // destroyedTruckMesh is a GO, weird naming zeekerss.
        if (__instance.destroyedTruckMesh.TryGetComponent<MeshRenderer>(out var destroyedMesh)) 
        {
            // ideally, you would do a material swap on OnNetworkSpawn() or when-ever the Nuker is added to the truck, not replace a MainTexture upon destruction.
            // unfortunately, i don't have access to those assets, this is something you'll have to do. - Scandal
            destroyedMesh.materials[0].mainTexture = NuclearCruiser.destroyedCruiserTexture;
        }      
        cruiserNuker.Explode();
    }

    [HarmonyPatch(nameof(VehicleController.OnCollisionEnter))]
    [HarmonyPostfix]
    public static void OnCollisionEnter_Post(VehicleController __instance, Collision collision) // removed the ref because we are not changing the collision
    {
        if (__instance.vehicleID != 0)
        {
            return;
        }
        //_ = discard
        if (!__instance.TryGetComponent<CruiserNuker>(out _))
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
        if (NuclearCruiser.cruiserFragility == NuclearCruiser.Fragility.Normal ||
            __instance.averageVelocity.magnitude < NuclearCruiser.minimumCrashVelocity)
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
    public static bool CarReactToObstacle_Pre(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal) // harmonyX
        {
            return false;         
        }        
        if (__instance.vehicleID != 0)
        {
            return true;
        }
        if (!__instance.TryGetComponent<CruiserNuker>(out var cruiserNuker))
        {
            return true;
        }
        if (!__instance.IsOwner || __instance.magnetedToShip || __instance.carDestroyed || 
            __instance.averageVelocity.magnitude < NuclearCruiser.minimumCrashVelocity)
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
    public static bool DealPermanentDamage_Pre(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal) // harmonyX
        {
            return false;         
        }
        if (__instance.vehicleID != 0)
        {
            return true;
        }
        if (!__instance.TryGetComponent<CruiserNuker>(out _))
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
