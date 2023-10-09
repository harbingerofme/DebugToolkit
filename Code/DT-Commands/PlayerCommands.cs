using System;
using RoR2;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class PlayerCommands
    {
        [ConCommand(commandName = "god", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GOD_HELP)]
        private static void CCGodModeToggle(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool hasNotYetRun = true;
            foreach (var playerInstance in PlayerCharacterMasterController.instances)
            {
                playerInstance.master.ToggleGod();
                if (hasNotYetRun)
                {
                    Log.MessageNetworked($"God mode {(playerInstance.master.godMode ? "enabled" : "disabled")}.", args);
                    hasNotYetRun = false;
                }
            }
        }

        [ConCommand(commandName = "buddha", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        [ConCommand(commandName = "budha", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        [ConCommand(commandName = "buda", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        [ConCommand(commandName = "budda", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        private static void CCBuddhaModeToggle(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool modeOn = Hooks.ToggleBuddha();
            Log.MessageNetworked($"Buddha mode {(modeOn ? "enabled" : "disabled")}.", args);
        }

        [ConCommand(commandName = "noclip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NOCLIP_HELP)]
        private static void CCNoclip(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't toggle noclip while you're dead. " + Lang.USE_RESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }
            NoclipNet.Invoke(args.sender); // callback
        }

        [ConCommand(commandName = "teleport_on_cursor", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CURSORTELEPORT_HELP)]
        private static void CCCursorTeleport(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't toggle noclip while you're dead. " + Lang.USE_RESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }
            TeleportNet.Invoke(args.sender); // callback
        }

        [ConCommand(commandName = "spawn_as", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNAS_HELP)]
        [AutoCompletion(typeof(BodyCatalog), "bodyPrefabBodyComponents", "baseNameToken")]
        private static void CCSpawnAs(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.Count < 2 && args.sender == null))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SPAWNAS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            string character = StringFinder.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Log.MessageNetworked(Lang.BODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            GameObject newBody = BodyCatalog.FindBodyPrefab(character);

            CharacterMaster master = args.senderMaster;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                master = player.master;
            }

            master.bodyPrefab = newBody;
            Log.MessageNetworked(args.sender.userName + " is spawning as " + character, args);

            if (!master.GetBody())
            {
                Log.MessageNetworked(Lang.PLAYER_DEADRESPAWN, args);
                return;
            }

            RoR2.ConVar.BoolConVar stage1pod = Stage.stage1PodConVar;
            bool oldVal = stage1pod.value;
            stage1pod.SetBool(false);
            master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
            stage1pod.SetBool(oldVal);
        }

        [ConCommand(commandName = "respawn", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RESPAWN_HELP)]
        [AutoCompletion(typeof(NetworkUser), "instancesList", "userName", true)]
        //[AutoCompletion(typeof(NetworkUser), "instancesList", "_id/value", true)] // ideathhd : breaks the whole console for me
        private static void CCRespawnPlayer(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null && args.Count < 1)
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.RESPAWN_ARGS, LogLevel.Error);
                return;
            }
            CharacterMaster master = args.senderMaster;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                master = player.master;
            }

            Transform spawnPoint = Stage.instance.GetPlayerSpawnTransform();
            master.Respawn(spawnPoint.position, spawnPoint.rotation);
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_1, master.name), args);
        }

        [ConCommand(commandName = "change_team", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CHANGETEAM_HELP)]
        private static void CCChangeTeam(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.Count < 2 && args.sender == null))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.CHANGETEAM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            CharacterMaster master = args.sender?.master;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                master = player.master;
            }
            if (!master.GetBody())
            {
                Log.MessageNetworked("Can't change a dead player's team. " + Lang.USE_RESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }

            if (!Enum.TryParse(StringFinder.GetEnumFromPartial<TeamIndex>(args[0]).ToString(), true, out TeamIndex teamIndex))
            {
                Log.MessageNetworked(Lang.TEAM_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count)
            {
                master.GetBody().teamComponent.teamIndex = teamIndex;
                master.teamIndex = teamIndex;
                Log.MessageNetworked("Changed to team " + teamIndex, args);
            }
        }

        [ConCommand(commandName = "loadout_set_skin_variant", flags = ConVarFlags.None, helpText = Lang.LOADOUTSKIN_HELP)]
        public static void CCLoadoutSetSkinVariant(ConCommandArgs args)
        {
            if (args.Count < 2)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.LOADOUTSKIN_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            BodyIndex argBodyIndex = BodyIndex.None;
            bool bodyIsSelf = false;

            if (args[0].ToUpperInvariant() == "SELF")
            {
                bodyIsSelf = true;
                if (args.sender == null)
                {
                    Log.Message("Can't choose self if not in-game!", LogLevel.Error);
                    return;
                }
                if (args.senderBody)
                {
                    argBodyIndex = args.senderBody.bodyIndex;
                }
                else
                {
                    if (args.senderMaster && args.senderMaster.bodyPrefab)
                    {
                        argBodyIndex = args.senderMaster.bodyPrefab.GetComponent<CharacterBody>().bodyIndex;
                    }
                    else
                    {
                        argBodyIndex = args.sender.bodyIndexPreference;
                    }
                }
            }
            else
            {
                string requestedBodyName = StringFinder.Instance.GetBodyName(args[0]);
                if (requestedBodyName == null)
                {
                    Log.MessageNetworked(Lang.BODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                argBodyIndex = BodyCatalog.FindBodyIndex(requestedBodyName);
            }

            if (!TextSerialization.TryParseInvariant(args[1], out int requestedSkinIndexChange))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "skin_index", "int"), args, LogLevel.MessageClientOnly);
            }

            Loadout loadout = new Loadout();
            UserProfile userProfile = args.GetSenderLocalUser().userProfile;
            userProfile.loadout.Copy(loadout);
            loadout.bodyLoadoutManager.SetSkinIndex(argBodyIndex, (uint)requestedSkinIndexChange);
            userProfile.SetLoadout(loadout);
            if (args.senderMaster)
            {
                args.senderMaster.SetLoadoutServer(loadout);
            }
            if (args.senderBody)
            {
                args.senderBody.SetLoadoutServer(loadout);
                if (args.senderBody.modelLocator && args.senderBody.modelLocator.modelTransform)
                {
                    var modelSkinController = args.senderBody.modelLocator.modelTransform.GetComponent<ModelSkinController>();
                    if (modelSkinController)
                    {
                        modelSkinController.ApplySkin(requestedSkinIndexChange);
                    }
                }
            }

            if (bodyIsSelf && !args.senderBody)
            {
                Log.MessageNetworked(Lang.PLAYER_SKINCHANGERESPAWN, args, LogLevel.MessageClientOnly);
            }
        }

        internal static bool UpdateCurrentPlayerBody(out NetworkUser networkUser, out CharacterBody characterBody)
        {
            networkUser = LocalUserManager.GetFirstLocalUser()?.currentNetworkUser;
            characterBody = null;

            if (networkUser)
            {
                var master = networkUser.master;

                if (master && master.GetBody())
                {
                    characterBody = master.GetBody();
                    return true;
                }
            }

            return false;
        }
    }
}
