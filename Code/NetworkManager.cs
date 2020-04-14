using DebugToolkit.Commands;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DebugToolkit.Code
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
            Object.Destroy(dtcn);

            Log.InitRPC();
            Command_Noclip.InitRPC();
            Command_Teleport.InitRPC();
            DebugToolKitComponents.AddComponent<TimescaleNet>();

            ApplyHook();
        }

        private static void ApplyHook()
        {
            SceneDirector.onPostPopulateSceneServer += EnsureDTNetwork;
        }

        // ReSharper disable once UnusedMember.Global
        internal static void UndoHook()
        {
            SceneDirector.onPostPopulateSceneServer -= EnsureDTNetwork;
        }

        // ReSharper disable once InconsistentNaming
        private static void EnsureDTNetwork(SceneDirector _)
        {
            if (!_debugToolKitComponentsSpawned)
            {
                _debugToolKitComponentsSpawned =
                    Object.Instantiate(DebugToolKitComponents);

                NetworkServer.Spawn(_debugToolKitComponentsSpawned);
            }
        }
    }
}
