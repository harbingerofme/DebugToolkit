﻿using DebugToolkit.Permissions;
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
        [RequiredLevel]
        private static void CCKick(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.KICK_ARGS, args, LogLevel.Error);
                return;
            }
            NetworkUser nu = Util.GetNetUserFromString(args.userArgs);
            if (nu == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.Error);
                return;
            }

            // Check if we can kick targeted user.
            if (!Application.isBatchMode)
            {
                foreach (var serverLocalUsers in NetworkUser.readOnlyLocalPlayersList)
                {
                    if (serverLocalUsers == nu)
                    {
                        Log.MessageNetworked("Specified user is hosting.", args, LogLevel.Error);
                        return;
                    }
                }
            }
            else if (PermissionSystem.IsEnabled.Value)
            {
                if (!PermissionSystem.HasMorePerm(args.sender, nu, args))
                {
                    Log.MessageNetworked("The target has a higher permission level that you.", args, LogLevel.Error);
                    return;
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
                return;
            }
            var reason = new NetworkManagerSystem.SimpleLocalizedKickReason("KICK_REASON_KICK");
            NetworkManagerSystem.singleton.ServerKickClient(client, reason);
        }

        [ConCommand(commandName = "ban", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BAN_HELP)]
        [RequiredLevel]
        private static void CCBan(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.BAN_ARGS, args, LogLevel.Error);
                return;
            }
            NetworkUser nu = Util.GetNetUserFromString(args.userArgs);
            if (nu == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.Error);
                return;
            }

            // Check if we can kick targeted user.
            if (!Application.isBatchMode)
            {
                foreach (var serverLocalUsers in NetworkUser.readOnlyLocalPlayersList)
                {
                    if (serverLocalUsers == nu)
                    {
                        Log.MessageNetworked("Specified user is hosting.", args, LogLevel.Error);
                        return;
                    }
                }
            }
            else if (PermissionSystem.IsEnabled.Value)
            {
                if (!PermissionSystem.HasMorePerm(args.sender, nu, args))
                {
                    Log.MessageNetworked("The target has a higher permission level that you.", args, LogLevel.Error);
                    return;
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

            if (client != null)
            {
                Log.MessageNetworked("Error trying to find the associated connection with the user", args, LogLevel.Error);
                return;
            }
            NetworkManagerSystem.singleton.ServerBanClient(client);
        }

        [ConCommand(commandName = "true_kill", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.TRUEKILL_HELP)]
        private static void CCTrueKill(ConCommandArgs args)
        {
            if (args.sender == null && args.Count < 1)
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.TRUEKILL_ARGS, LogLevel.Error);
                return;
            }
            CharacterMaster master = args.sender?.master;
            if (args.Count > 0)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                master = player.master;
            }

            master.TrueKill();
            Log.MessageNetworked(master.name + " was killed by server.", args);
        }
    }
}
