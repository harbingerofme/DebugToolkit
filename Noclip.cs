using RoR2;
using UnityEngine;
using KinematicCharacterController;
using MiniRpcLib;

namespace RoR2Cheats
{
    // ReSharper disable once UnusedMember.Global
    internal static class Noclip
    {
        internal static bool IsActivated;
        internal static MiniRpcLib.Action.IRpcAction<bool> Toggle;

        private static NetworkUser _currentNetworkUser;
        private static CharacterBody _currentBody;
        private static int _collidableLayersCached;

        internal static void Init(MiniRpcInstance miniRpc)
        {
            Toggle = miniRpc.RegisterAction(Target.Client, (NetworkUser _, bool __) =>
            {
                if (UpdateCurrentPlayerBody())
                {
                    if (IsActivated)
                    {
                        _currentBody.GetComponent<KinematicCharacterMotor>().CollidableLayers = _collidableLayersCached;
                    }
                    else
                    {
                        _collidableLayersCached = _currentBody.GetComponent<KinematicCharacterMotor>().CollidableLayers;

                        _currentBody.GetComponent<KinematicCharacterMotor>().CollidableLayers = 0;
                    }

                    _currentBody.characterMotor.useGravity = !_currentBody.characterMotor.useGravity;
                    IsActivated = !IsActivated;
                    Log.Message(string.Format(Lang.NOCLIP_TOGGLE, IsActivated));
                }
            });
        }

        internal static void Update()
        {
            if (UpdateCurrentPlayerBody())
            {
                Loop();
            }
        }

        private static void Loop()
        {
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
                    {
                        _currentBody.characterMotor.velocity.y = aimDirection.y * 100f;
                    }
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
                    {
                        _currentBody.characterMotor.velocity.y = aimDirection.y * 50;
                    }
                    else
                    {
                        _currentBody.characterMotor.velocity.y = aimDirection.y * -50;
                    }
                       
                }
            }

            var inputBank = _currentBody.GetComponent<InputBankTest>();
            if (inputBank && inputBank.jump.down)
            {
                _currentBody.characterMotor.velocity.y = 50f;
            } 
        }

        private static bool UpdateCurrentPlayerBody()
        {
            _currentNetworkUser = LocalUserManager.GetFirstLocalUser().currentNetworkUser;
            CharacterMaster master = null;
            if (_currentNetworkUser)
            {
                master = _currentNetworkUser.master;
            }

            if (_currentNetworkUser && master && master.GetBody())
            {
                _currentBody = master.GetBody();
                return true;
            }

            return false;
        }
    }
}
