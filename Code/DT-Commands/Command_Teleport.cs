using RoR2;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable InconsistentNaming

namespace DebugToolkit.Commands
{
    public static class Command_Teleport
    {
        private static CharacterBody _currentBody;

        internal static void InitRPC()
        {
            NetworkManager.DebugToolKitComponents.AddComponent<TeleportNet>();
        }

        internal static void InternalActivation()
        {
            if (PlayerCommands.UpdateCurrentPlayerBody(out _, out _currentBody))
            {
                var inputBank = _currentBody.inputBank;
                if (inputBank)
                {
                    if (Physics.Raycast(inputBank.aimOrigin, inputBank.aimDirection, out var hit, Mathf.Infinity, 1 << 11))
                    {
                        var footOffset = _currentBody.transform.position - _currentBody.footPosition;
                        TeleportHelper.TeleportGameObject(_currentBody.gameObject, hit.point + footOffset);
                    }
                }
            }
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBeMadeStatic.Local
    // ReSharper disable once UnusedParameter.Local
    // ReSharper disable once UnusedMember.Local
    internal class TeleportNet : NetworkBehaviour
    {
        private static TeleportNet _instance;

        private void Awake()
        {
            _instance = this;
        }

        internal static void Invoke(NetworkUser argsSender)
        {
            _instance.TargetToggle(argsSender.connectionToClient);
        }

        [TargetRpc]
        private void TargetToggle(NetworkConnection _)
        {
            Command_Teleport.InternalActivation();
        }
    }
}
