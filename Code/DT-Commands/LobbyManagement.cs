using DebugToolkit.Permissions;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class LobbyManagement
    {
        [ConCommand(commandName = "kick", flags = ConVarFlags.ExecuteOnServer, helpText = "Kicks the specified player from the session. " + Lang.KICK_ARGS)]
        [RequiredLevel]
        private static void CCKick(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.KICK_ARGS, args, LogLevel.Error);
                return;
            }
            try
            {
                NetworkUser nu = Util.GetNetUserFromString(args.userArgs);

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
                    var reason = new GameNetworkManager.SimpleLocalizedKickReason("KICK_REASON_KICK");
                    GameNetworkManager.singleton.InvokeMethod("ServerKickClient", client,reason);
                }
                else
                {
                    Log.MessageNetworked("Error trying to find the associated connection with the user", args, LogLevel.Error);
                }
            }
            catch
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.Error);
            }
        }

        [ConCommand(commandName = "ban", flags = ConVarFlags.ExecuteOnServer, helpText = "Bans the specified player from the session. " + Lang.BAN_ARGS)]
        [RequiredLevel]
        private static void CCBan(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.BAN_ARGS, args, LogLevel.Error);
                return;
            }
            try
            {
                NetworkUser nu = Util.GetNetUserFromString(args.userArgs);

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
                    var reason = new GameNetworkManager.SimpleLocalizedKickReason("KICK_REASON_BAN");
                    GameNetworkManager.singleton.InvokeMethod("ServerKickClient", client, reason);
                }
                else
                {
                    Log.MessageNetworked("Error trying to find the associated connection with the user", args, LogLevel.Error);
                }
            }
            catch
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.Error);
            }
        }

        [ConCommand(commandName = "true_kill", flags = ConVarFlags.ExecuteOnServer, helpText = "Ignore Dio's and kill the entity. " + Lang.TRUEKILL_ARGS)]
        private static void CCTrueKill(ConCommandArgs args)
        {
            if (args.sender == null && args.Count < 1)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }
            CharacterMaster master = args.sender?.master;
            if (args.Count > 0)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            master.TrueKill();
            Log.MessageNetworked(master.name + " was killed by server.", args);
        }
    }
}
