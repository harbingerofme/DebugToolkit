using DebugToolkit.Commands;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityObject = UnityEngine.Object;

namespace DebugToolkit
{
    internal static class NetworkManager
    {
        internal static GameObject DebugToolKitComponents;
        private static GameObject _debugToolKitComponentsSpawned;

        internal static void Init()
        {
            var dtcn = new GameObject("dtcn");
            dtcn.AddComponent<NetworkIdentity>();
            DebugToolKitComponents = dtcn.InstantiateClone("DebugToolKitComponentsNetworked");
            UnityObject.Destroy(dtcn);

            Log.InitRPC();
            Command_Noclip.InitRPC();
            Command_Teleport.InitRPC();
            DebugToolKitComponents.AddComponent<TimescaleNet>();
            DebugToolKitComponents.AddComponent<SetDontDestroyOnLoad>();
        }

        internal static void CreateNetworkObject(On.RoR2.NetworkSession.orig_Start orig, NetworkSession self)
        {
            orig(self);
            if (!_debugToolKitComponentsSpawned && NetworkServer.active)
            {
                _debugToolKitComponentsSpawned = UnityObject.Instantiate(DebugToolKitComponents);
                NetworkServer.Spawn(_debugToolKitComponentsSpawned);
            }
        }

        internal static void DestroyNetworkObject(On.RoR2.NetworkSession.orig_OnDestroy orig, NetworkSession self)
        {
            if (_debugToolKitComponentsSpawned && NetworkServer.active)
            {
                NetworkServer.Destroy(_debugToolKitComponentsSpawned);
                _debugToolKitComponentsSpawned = null;
            }
            orig(self);
        }
    }
}
