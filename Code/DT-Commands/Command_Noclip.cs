using KinematicCharacterController;
using MonoMod.RuntimeDetour;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace DebugToolkit.Commands
{
    // ReSharper disable once UnusedMember.Global
    internal static class Command_Noclip
    {
        internal delegate void d_ServerChangeScene(UnityEngine.Networking.NetworkManager instance, string newSceneName);
        internal static d_ServerChangeScene origServerChangeScene;
        internal static Hook OnServerChangeSceneHook;
        internal delegate void d_ClientChangeScene(UnityEngine.Networking.NetworkManager instance, string newSceneName, bool forceReload);
        internal static d_ClientChangeScene origClientChangeScene;
        internal static Hook OnClientChangeSceneHook;

        internal static bool IsActivated;

        private static NetworkUser _currentNetworkUser;
        private static CharacterBody _currentBody;

        internal static void InitRPC()
        {
            NetworkManager.DebugToolKitComponents.AddComponent<NoclipNet>();
        }

        internal static void InternalToggle()
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
            if (IsActivated)
            {
                UndoHooks();
            }
            else
            {
                ApplyHooks();
            }
            IsActivated = !IsActivated;
            Log.Message(string.Format(Lang.NOCLIP_TOGGLE, IsActivated));
        }

        private static void ApplyHooks()
        {
            if (!IsActivated)
            {
                OnServerChangeSceneHook.Apply();
                OnClientChangeSceneHook.Apply();
                On.RoR2.Networking.NetworkManagerSystem.OnStopClient += DisableOnStopClient;
                On.RoR2.MapZone.TeleportBody += DisableOOBCheck;
            }
        }

        private static void UndoHooks()
        {
            if (IsActivated)
            {
                OnServerChangeSceneHook.Undo();
                OnClientChangeSceneHook.Undo();
                On.RoR2.Networking.NetworkManagerSystem.OnStopClient -= DisableOnStopClient;
                On.RoR2.MapZone.TeleportBody -= DisableOOBCheck;
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
                    InternalToggle();
                    InternalToggle();
                }
            }
            else if (rigid)
            {
                var collider = rigid.GetComponent<Collider>();
                if (collider && !collider.isTrigger)
                {
                    InternalToggle();
                    InternalToggle();
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

        private static void DisableOnServerSceneChange(UnityEngine.Networking.NetworkManager instance, string newSceneName)
        {
            if (IsActivated)
                Console.instance.SubmitCmd(LocalUserManager.GetFirstLocalUser().currentNetworkUser, "noclip");

            origServerChangeScene(instance, newSceneName);
        }

        private static void DisableOnClientSceneChange(UnityEngine.Networking.NetworkManager instance, string newSceneName, bool forceReload)
        {
            if (IsActivated)
                Console.instance.SubmitCmd(LocalUserManager.GetFirstLocalUser().currentNetworkUser, "noclip");

            origClientChangeScene(instance, newSceneName, forceReload);
        }

        private static void DisableOnStopClient(On.RoR2.Networking.NetworkManagerSystem.orig_OnStopClient orig, RoR2.Networking.NetworkManagerSystem self)
        {
            if (IsActivated)
            {
                InternalToggle();
            }
            orig(self);
        }

        // ReSharper disable once InconsistentNaming
        private static void DisableOOBCheck(On.RoR2.MapZone.orig_TeleportBody orig, MapZone self, CharacterBody characterBody)
        {
            if (!characterBody.isPlayerControlled)
                orig(self, characterBody);
        }

        public static void SetUseGravity(this CharacterMotor motor, bool value)
        {
            typeof(CharacterMotor).GetProperty(nameof(CharacterMotor.useGravity)).GetSetMethod(true).Invoke(motor, new object[] { value });
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

        internal static void Invoke(NetworkUser argsSender)
        {
            _instance.TargetToggle(argsSender.connectionToClient);
        }

        [TargetRpc]
        private void TargetToggle(NetworkConnection _)
        {
            Command_Noclip.InternalToggle();
        }
    }
}
