using DebugToolkit.Permissions;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class LobbyManagement
    {
        [ConCommand(commandName = "kick", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.KICK_HELP)]
        [AutoComplete(Lang.PLAYER_ARGS)]
        [RequiredLevel]
        private static void CCKick(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.PLAYER_ARGS, args, LogLevel.Error);
                return;
            }
            var client = GetClientFromArgs(args);
            if (client == null)
            {
                return;
            }
            var reason = new NetworkManagerSystem.SimpleLocalizedKickReason("KICK_REASON_KICK");
            NetworkManagerSystem.singleton.ServerKickClient(client, reason);
        }

        [ConCommand(commandName = "ban", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BAN_HELP)]
        [AutoComplete(Lang.BAN_ARGS)]
        [RequiredLevel]
        private static void CCBan(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.BAN_ARGS, args, LogLevel.Error);
                return;
            }
            var client = GetClientFromArgs(args);
            if (client == null)
            {
                return;
            }
            NetworkManagerSystem.singleton.ServerBanClient(client);
        }

        [ConCommand(commandName = "true_kill", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.TRUEKILL_HELP)]
        [AutoComplete(Lang.PLAYERPINGED_OR_ALL)]
        private static void CCTrueKill(ConCommandArgs args)
        {
            if (args.sender == null && (args.Count < 1 || args[0] == Lang.DEFAULT_VALUE))
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.PLAYER_OR_PINGED, LogLevel.Error);
                return;
            } 
            CharacterMaster master = args.sender.master;
            if (args.Count > 0)
            {
                if (args[0].ToUpperInvariant() == Lang.ALL)
                {
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        player.master.godMode = false;
                        player.master.UpdateBodyGodMode();
                        player.master.TrueKill();
                    }
                    Log.MessageNetworked("All players were killed by server.", args);
                    return;
                }
                else
                {
                    var target = Buffs.ParseTarget(args, 0);
                    if (target.failMessage != null)
                    {
                        Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                        return;
                    }
                    master = target.body.master;
                }          
            }
            if (!master)
            {
                Log.MessageNetworked(Lang.BODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            master.godMode = false;
            master.UpdateBodyGodMode();
            master.TrueKill();
            Log.MessageNetworked(master.name + " was killed by server.", args);
        }
       

        private static NetworkConnection GetClientFromArgs(ConCommandArgs args)
        {
            NetworkUser nu = Util.GetNetUserFromString(args.userArgs);
            if (nu == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.Error);
                return null;
            }

            // Check if we can kick targeted user.
            if (!Application.isBatchMode)
            {
                foreach (var serverLocalUsers in NetworkUser.readOnlyLocalPlayersList)
                {
                    if (serverLocalUsers == nu)
                    {
                        Log.MessageNetworked("Specified user is hosting.", args, LogLevel.Error);
                        return null;
                    }
                }
            }
            else if (PermissionSystem.IsEnabled.Value)
            {
                if (!PermissionSystem.HasMorePerm(args.sender, nu, args))
                {
                    Log.MessageNetworked("The target has a higher permission level that you.", args, LogLevel.Error);
                    return null;
                }
            }

            NetworkConnection client = null;
            foreach (var connection in NetworkServer.connections)
            {
                if (nu.connectionToClient == connection)
                {
                    client = connection;
                    break;
                }
            }

            if (client == null)
            {
                Log.MessageNetworked("Error trying to find the associated connection with the user", args, LogLevel.Error);
                return null;
            }

            return client;
        }
    }
}
