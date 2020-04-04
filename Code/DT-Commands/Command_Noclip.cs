using RoR2;
using UnityEngine;
using KinematicCharacterController;
using MiniRpcLib;
using MonoMod.RuntimeDetour;
using UnityEngine.Networking;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace DebugToolkit.Commands
{
    // ReSharper disable once UnusedMember.Global
    internal static class Command_Noclip
    {
        internal delegate void d_ServerChangeScene(NetworkManager instance, string newSceneName);
        internal static d_ServerChangeScene origServerChangeScene;
        internal static Hook OnServerChangeSceneHook;
        internal delegate void d_ClientChangeScene(NetworkManager instance, string newSceneName, bool forceReload);
        internal static d_ClientChangeScene origClientChangeScene;
        internal static Hook OnClientChangeSceneHook;

        internal static bool IsActivated;
        internal static MiniRpcLib.Action.IRpcAction<bool> Toggle;

        private static NetworkUser _currentNetworkUser;
        private static CharacterBody _currentBody;
        private static int _collidableLayersCached;

        internal static void InitRPC(MiniRpcInstance miniRpc)
        {
            Toggle = miniRpc.RegisterAction(Target.Client, (NetworkUser _, bool __) =>
            {
                InternalToggle();
            });
        }

        private static void InternalToggle()
        {
            if (PlayerCommands.UpdateCurrentPlayerBody(out _currentNetworkUser, out _currentBody))
            {
                if (IsActivated)
                {
                    if (_collidableLayersCached != 0)
                        _currentBody.GetComponent<KinematicCharacterMotor>().CollidableLayers = _collidableLayersCached;
                    _currentBody.characterMotor.useGravity = true;
                    UndoHooks();
                }
                else
                {
                    _collidableLayersCached = _currentBody.GetComponent<KinematicCharacterMotor>().CollidableLayers;
                    _currentBody.GetComponent<KinematicCharacterMotor>().CollidableLayers = 0;
                    _currentBody.characterMotor.useGravity = false;
                    ApplyHooks();
                }

                IsActivated = !IsActivated;
                Log.Message(string.Format(Lang.NOCLIP_TOGGLE, IsActivated));
            }
        }

        private static void ApplyHooks()
        {
            if (!IsActivated)
            {
                OnServerChangeSceneHook.Apply();
                OnClientChangeSceneHook.Apply();
                On.RoR2.Networking.GameNetworkManager.Disconnect += DisableOnDisconnect;
                On.RoR2.MapZone.TeleportBody += DisableOOBCheck;
            }
        }

        private static void UndoHooks()
        {
            if (IsActivated)
            {
                OnServerChangeSceneHook.Undo();
                OnClientChangeSceneHook.Undo();
                On.RoR2.Networking.GameNetworkManager.Disconnect -= DisableOnDisconnect;
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
            if (_currentBody.characterMotor.useGravity) // when respawning or things like that, call the toggle to set the variables correctly again
            {
                InternalToggle();
                InternalToggle();
            }

            var forwardDirection = _currentBody.GetComponent<InputBankTest>().moveVector.normalized;
            var aimDirection = _currentBody.GetComponent<InputBankTest>().aimDirection.normalized;
            var isForward = Vector3.Dot(forwardDirection, aimDirection) > 0f;

            var isSprinting = _currentNetworkUser.inputPlayer.GetButton("Sprint");
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var isStrafing = _currentNetworkUser.inputPlayer.GetAxis("MoveVertical") != 0f;

            if (isSprinting)
            {
                _currentBody.characterMotor.velocity = forwardDirection * 100f;
                if (isStrafing)
                {
                    if (isForward)
                        _currentBody.characterMotor.velocity.y = aimDirection.y * 100f;
                    else
                    {
                        _currentBody.characterMotor.velocity.y = aimDirection.y * -100f;
                    }
                }

            }
            else
            {
                _currentBody.characterMotor.velocity = forwardDirection * 50;
                if (isStrafing)
                {
                    if (isForward)
                        _currentBody.characterMotor.velocity.y = aimDirection.y * 50;
                    else
                    {
                        _currentBody.characterMotor.velocity.y = aimDirection.y * -50;
                    }

                }
            }

            var inputBank = _currentBody.GetComponent<InputBankTest>();
            if (inputBank && inputBank.jump.down)
                _currentBody.characterMotor.velocity.y = 50f;
        }

        private static void DisableOnServerSceneChange(NetworkManager instance, string newSceneName)
        {
            if (IsActivated)
                Console.instance.SubmitCmd(LocalUserManager.GetFirstLocalUser().currentNetworkUser, "noclip");

            origServerChangeScene(instance, newSceneName);
        }

        private static void DisableOnClientSceneChange(NetworkManager instance, string newSceneName, bool forceReload)
        {
            if (IsActivated)
                Console.instance.SubmitCmd(LocalUserManager.GetFirstLocalUser().currentNetworkUser, "noclip");

            origClientChangeScene(instance, newSceneName, forceReload);
        }

        private static void DisableOnDisconnect(On.RoR2.Networking.GameNetworkManager.orig_Disconnect orig, RoR2.Networking.GameNetworkManager self)
        {
            if (IsActivated)
            {
                if (_currentBody)
                {
                    _currentBody.GetComponent<KinematicCharacterMotor>().CollidableLayers = _collidableLayersCached;
                    _currentBody.characterMotor.useGravity = !_currentBody.characterMotor.useGravity;
                }

                IsActivated = !IsActivated;
                UndoHooks();
                Log.Message(string.Format(Lang.NOCLIP_TOGGLE, IsActivated));
            }

            orig(self);
        }

        // ReSharper disable once InconsistentNaming
        private static void DisableOOBCheck(On.RoR2.MapZone.orig_TeleportBody orig, MapZone self, CharacterBody characterBody)
        {
            if (!characterBody.isPlayerControlled)
                orig(self, characterBody);
        }
    }
}
