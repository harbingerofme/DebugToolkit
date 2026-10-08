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

        internal static void TeleportOnCursor()
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

        internal static void GotoBoss()
        {
            if (PlayerCommands.UpdateCurrentPlayerBody(out _, out _currentBody))
            {
                if (SkipToBossZone.instance)
                {
                    Vector3 pos = SkipToBossZone.instance.transform.position;
                    TeleportHelper.TeleportGameObject(_currentBody.gameObject, pos);
                }
                else
                {
                    Log.Message("No SkipToBossZone found.");
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

        internal static void InvokeTeleportOnCursor(NetworkUser argsSender)
        {
            _instance.TargetTeleportOnCursor(argsSender.connectionToClient);
        }

        [TargetRpc]
        private void TargetTeleportOnCursor(NetworkConnection _)
        {
            Command_Teleport.TeleportOnCursor();
        }

        internal static void InvokeGotoBoss(NetworkUser argsSender)
        {
            _instance.TargetGotoBoss(argsSender.connectionToClient);
        }

        [TargetRpc]
        private void TargetGotoBoss(NetworkConnection _)
        {
            Command_Teleport.GotoBoss();
        }
    }
}
