using KinematicCharacterController;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace DebugToolkit.Commands
{
    // ReSharper disable once UnusedMember.Global
    internal static class Command_Noclip
    {
        internal static bool IsActivated;

        private static NetworkUser _currentNetworkUser;
        private static CharacterBody _currentBody;

        internal static void InitRPC()
        {
            NetworkManager.DebugToolKitComponents.AddComponent<NoclipNet>();
        }

        internal static void InternalToggle(bool enabled, bool shouldLog)
        {
            if (enabled != IsActivated)
            {
                if (PlayerCommands.UpdateCurrentPlayerBody(out _currentNetworkUser, out _currentBody))
                {
                    var kcm = _currentBody.GetComponent<KinematicCharacterMotor>();
                    var rigid = _currentBody.GetComponent<Rigidbody>();
                    var motor = _currentBody.characterMotor;
                    if (IsActivated)
                    {
                        if (kcm)
                        {
                            kcm.RebuildCollidableLayers();
                        }
                        else if (rigid)
                        {
                            var collider = rigid.GetComponent<Collider>();
                            if (collider)
                            {
                                collider.isTrigger = false;
                            }
                        }
                        if (motor)
                        {
                            motor.useGravity = motor.gravityParameters.CheckShouldUseGravity();
                        }
                    }
                    else
                    {
                        if (kcm)
                        {
                            kcm.CollidableLayers = 0;
                        }
                        else if (rigid)
                        {
                            var collider = rigid.GetComponent<Collider>();
                            if (collider)
                            {
                                collider.isTrigger = true;
                            }
                        }
                        if (motor)
                        {
                            motor.useGravity = false;
                        }
                    }
                }
                IsActivated = !IsActivated;
            }
            if (shouldLog)
            {
                Log.Message(String.Format(IsActivated ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "noclip"));
            }
        }

        internal static void Update()
        {
            if (PlayerCommands.UpdateCurrentPlayerBody(out _currentNetworkUser, out _currentBody))
                Loop();
        }

        private static void Loop()
        {
            var kcm = _currentBody.GetComponent<KinematicCharacterMotor>();
            var rigid = _currentBody.GetComponent<Rigidbody>();
            if (kcm) // when respawning or things like that, call the toggle to set the variables correctly again
            {
                if (kcm.CollidableLayers != 0)
                {
                    InternalToggle(false, shouldLog: false);
                    InternalToggle(true, shouldLog: false);
                }
            }
            else if (rigid)
            {
                var collider = rigid.GetComponent<Collider>();
                if (collider && !collider.isTrigger)
                {
                    InternalToggle(false, shouldLog: false);
                    InternalToggle(true, shouldLog: false);
                }
            }

            var inputBank = _currentBody.GetComponent<InputBankTest>();
            var motor = _currentBody.characterMotor;
            if (inputBank && (motor || rigid))
            {
                var forwardDirection = inputBank.moveVector.normalized;
                var aimDirection = inputBank.aimDirection.normalized;
                var isForward = Vector3.Dot(forwardDirection, aimDirection) > 0f;

                var isSprinting = _currentNetworkUser.inputPlayer.GetButton("Sprint");
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                var isStrafing = _currentNetworkUser.inputPlayer.GetAxis("MoveVertical") != 0f;
                var scalar = isSprinting ? 100f : 50f;

                var velocity = forwardDirection * scalar;
                if (isStrafing)
                {
                    velocity.y = aimDirection.y * (isForward ? scalar : -scalar);
                }
                if (inputBank.jump.down)
                {
                    velocity.y = 50f;
                }

                if (motor)
                {
                    motor.velocity = velocity;
                }
                else
                {
                    rigid.velocity = velocity;
                }
            }
        }

        internal static void DisableOnStopClient(On.RoR2.Networking.NetworkManagerSystem.orig_OnStopClient orig, RoR2.Networking.NetworkManagerSystem self)
        {
            if (IsActivated)
            {
                InternalToggle(false, shouldLog: true);
            }
            orig(self);
        }

        // ReSharper disable once InconsistentNaming
        internal static void DisableOOBCheck(On.RoR2.MapZone.orig_TeleportBody orig, MapZone self, CharacterBody characterBody)
        {
            if (!IsActivated || !characterBody.isPlayerControlled)
                orig(self, characterBody);
        }

        internal static void DisableOnRunDestroy(Run run)
        {
            if (IsActivated)
            {
                InternalToggle(false, shouldLog: true);
            }
        }
    }


    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBeMadeStatic.Local
    // ReSharper disable once UnusedParameter.Local
    internal class NoclipNet : NetworkBehaviour
    {
        private static NoclipNet _instance;

        private void Awake()
        {
            _instance = this;
        }

        internal static void Invoke(NetworkUser argsSender, bool enabled)
        {
            _instance.TargetNoclipSet(argsSender.connectionToClient, enabled);
        }

        [TargetRpc]
        private void TargetNoclipSet(NetworkConnection _, bool enabled)
        {
            Command_Noclip.InternalToggle(enabled, shouldLog: true);
        }

        internal static void Invoke(NetworkUser argsSender)
        {
            _instance.TargetNoclipToggle(argsSender.connectionToClient);
        }

        [TargetRpc]
        private void TargetNoclipToggle(NetworkConnection _)
        {
            Command_Noclip.InternalToggle(!Command_Noclip.IsActivated, shouldLog: true);
        }
    }
}
