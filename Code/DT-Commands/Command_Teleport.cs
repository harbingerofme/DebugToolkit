using KinematicCharacterController;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable InconsistentNaming

namespace DebugToolkit.Commands
{
    internal static class Command_Teleport
    {
        private static CharacterBody _currentBody;

        internal static TeleportNet teleportNet;

        internal static void InitRPC()
        {
            teleportNet = DebugToolkit.DebugToolKitComponents.AddComponent<TeleportNet>();
        }

        internal static void InternalActivation()
        {
            if (PlayerCommands.UpdateCurrentPlayerBody(out _, out _currentBody))
            {
                var playerTransform = _currentBody.GetComponentInChildren<KinematicCharacterMotor>().transform;
                var aimDirection = _currentBody.GetComponentInChildren<InputBankTest>().aimDirection;

                if (Physics.Raycast(playerTransform.position, aimDirection, out var hit, Mathf.Infinity, 1 << 11))
                {
                    _currentBody.GetComponentInChildren<KinematicCharacterMotor>().SetPosition(hit.point + new Vector3(0, 5));
                }
            }
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBeMadeStatic.Local
    // ReSharper disable once UnusedParameter.Local
    internal class TeleportNet : NetworkBehaviour
    {
        internal void Invoke(NetworkUser argsSender)
        {
            TargetToggle(argsSender.connectionToClient);
        }

        [TargetRpc]
        private void TargetToggle(NetworkConnection target)
        {
            Command_Teleport.InternalActivation();
        }
    }
}
