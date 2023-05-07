using DebugToolkit.Commands;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
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

            ApplyHook();
        }

        private static void ApplyHook()
        {
            SceneManager.sceneLoaded += EnsureDTNetwork;
        }

        internal static void UndoHook()
        {
            SceneManager.sceneLoaded -= EnsureDTNetwork;
        }

        private static void EnsureDTNetwork(Scene _, LoadSceneMode __)
        {
            if (!_debugToolKitComponentsSpawned && NetworkServer.active)
            {
                _debugToolKitComponentsSpawned = UnityObject.Instantiate(DebugToolKitComponents);

                NetworkServer.Spawn(_debugToolKitComponentsSpawned);
            }
        }
    }
}
