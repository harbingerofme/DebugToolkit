using DebugToolkit.Commands;
using R2API;
using RoR2;
using System;
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

            ApplyHook();
        }

        private static void ApplyHook()
        {
            On.RoR2.SceneDirector.Start += EnsureDTNetwork;
        }

        // ReSharper disable once UnusedMember.Global
        internal static void UndoHook()
        {
            On.RoR2.SceneDirector.Start -= EnsureDTNetwork;
        }

        // ReSharper disable once InconsistentNaming
        private static void EnsureDTNetwork(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Log.Message($"Vanilla Exception : {e}", Log.LogLevel.Warning, Log.Target.Bepinex);
            }

            if (!_debugToolKitComponentsSpawned)
            {
                _debugToolKitComponentsSpawned =
                    UnityObject.Instantiate(DebugToolKitComponents);

                NetworkServer.Spawn(_debugToolKitComponentsSpawned);
            }
        }
    }
}
