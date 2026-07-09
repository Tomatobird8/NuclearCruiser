using NuclearCruiser.MonoBehaviours;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace NuclearCruiser.Network;

internal class NetworkHandler : NetworkBehaviour
{
    private static GameObject? networkPrefab = null;
    public static NetworkHandler Instance { get; private set; } = null!;


    public static void CreateAndRegisterPrefab()
    {
        if (networkPrefab != null)
            return;

        networkPrefab = new GameObject(MyPluginInfo.PLUGIN_GUID + " NetworkPrefab");
        networkPrefab.hideFlags |= HideFlags.HideAndDontSave;
        NetworkObject networkObject = networkPrefab.AddComponent<NetworkObject>();
        var fieldInfo = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);
        fieldInfo!.SetValue(networkObject, GetHash(MyPluginInfo.PLUGIN_GUID + " NetworkPrefab"));
        networkPrefab.AddComponent<NetworkHandler>();
        NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

        NuclearCruiser.Logger.LogInfo("Nuclear Cruiser network Network-Prefab added.");
    }

    public static void SpawnNetworkHandler()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Instantiate(networkPrefab)?.GetComponent<NetworkObject>().Spawn();
            NuclearCruiser.Logger.LogInfo("Spawned network handler.");
        }
    }

    public static void DespawnNetworkHandler()
    {
        if (Instance == null)
        {
            return;
        }
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }
        if (Instance.gameObject.TryGetComponent<NetworkObject>(out var netObject) && netObject.IsSpawned)
        {
            netObject.Despawn();
            NuclearCruiser.Logger.LogInfo("Despawned network handler.");     
        }
    }

    protected internal static uint GetHash(string value)
    {
        return value?.Aggregate(17u, (current, c) => unchecked((current * 31) ^ c)) ?? 0u;
    }

    private void Awake()
    {
        Instance = this;
    }

    [Rpc(SendTo.Everyone)]
    public void AddCruiserNukerRpc(NetworkBehaviourReference vehicleNetObjRef)
    {
        if (!vehicleNetObjRef.TryGet(out VehicleController vehicleObj))
        {
            NuclearCruiser.Logger.LogError($"Couldn't find a VehicleController component for {vehicleNetObjRef}");
            return;
        }
        if (vehicleObj.vehicleID != 0)
        {
            return;
        }
        if (vehicleObj.TryGetComponent<CruiserNuker>(out _))
        {
            NuclearCruiser.Logger.LogDebug("VehicleController already contains a CruiserNuker component, skipping");        
            return;      
        }
        vehicleObj.NetworkObject.gameObject.AddComponent<CruiserNuker>();
    }
}
