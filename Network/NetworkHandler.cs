using NuclearCruiser.Utils;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace NuclearCruiser.Network
{
    internal class NetworkHandler : NetworkBehaviour
    {
        private static GameObject? prefab = null;
        public static NetworkHandler Instance { get; private set; } = null!;


        public static void CreateAndRegisterPrefab()
        {
            if (prefab != null)
                return;
            
            prefab = new GameObject(MyPluginInfo.PLUGIN_GUID + " Prefab");
            prefab.hideFlags |= HideFlags.HideAndDontSave;
            NetworkObject networkObject = prefab.AddComponent<NetworkObject>();
            var fieldInfo = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo!.SetValue(networkObject, GetHash(MyPluginInfo.PLUGIN_GUID + " Prefab"));
            prefab.AddComponent<NetworkHandler>();
            NetworkManager.Singleton.AddNetworkPrefab(prefab);

            NuclearCruiser.Logger.LogInfo("Nuclear Cruiser network prefab done.");
            
        }

        public static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                Instantiate(prefab)?.GetComponent<NetworkObject>().Spawn();
                NuclearCruiser.Logger.LogInfo($"Spawned network handler.");
            }
        }

        public static void DespawnNetworkHandler()
        {
            if (Instance != null && Instance.gameObject.GetComponent<NetworkObject>().IsSpawned && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost))
            {
                Instance.gameObject.GetComponent<NetworkObject>().Despawn();
                NuclearCruiser.Logger.LogInfo($"Despawned network handler.");
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

        [ClientRpc]
        public void AddCruiserNukerClientRpc(NetworkObjectReference target)
        {
            if (target.TryGet(out NetworkObject networkObject))
            {
                networkObject.gameObject.AddComponent<CruiserNuker>();
            }
            else
            {
                NuclearCruiser.Logger.LogError($"NetworkObject couldn't be found for AddCruiserNukerClientRpc");
            }
                
        }

    }
}
