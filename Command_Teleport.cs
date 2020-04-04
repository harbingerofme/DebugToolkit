using RoR2;
using UnityEngine;
using KinematicCharacterController;
using MiniRpcLib;
using DebugToolkit.Commands;
// ReSharper disable InconsistentNaming

namespace DebugToolkit
{
    internal static class Command_Teleport
    {
        internal static MiniRpcLib.Action.IRpcAction<bool> Activator;
        
        private static CharacterBody _currentBody;
        
        internal static void InitRPC(MiniRpcInstance miniRpc)
        {
            Activator = miniRpc.RegisterAction(Target.Client, (NetworkUser _, bool __) =>
            {
                InternalActivation();
            });
        }

        private static void InternalActivation()
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
}
