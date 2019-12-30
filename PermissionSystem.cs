using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace DebugToolkit
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class RequiredPermissionLevel : Attribute
    {
        internal readonly PermissionLevel Level;

        public RequiredPermissionLevel(PermissionLevel level = PermissionLevel.SubAdmin)
        {
            Level = level;
        }
    }

    internal static class PermissionSystem
    {
        internal static ConfigEntry<bool> IsEnabled;

        private static ConfigEntry<string> _adminList;
        private static ConfigEntry<string> _subAdminList;

        private static readonly Dictionary<string, ConfigEntry<PermissionLevel>> _adminCommands = new Dictionary<string, ConfigEntry<PermissionLevel>>();

        internal static void Init()
        {
            IsEnabled = DebugToolkit.Config.Bind("Permission System", "1. Enable", true,
                "Is the Permission System enabled.");

            if (!IsEnabled.Value)
                return;

            _adminList = DebugToolkit.Config.Bind("Permission System", "2. Admin", "userName1, userName2",
                "Who is/are the admin(s).");
            _subAdminList = DebugToolkit.Config.Bind("Permission System", "3. Sub Admin List", "userName3, userName4",
                "Who is/are the sub admin(s).");

            _adminCommands.Clear();

            foreach (var methodInfo in Assembly.GetExecutingAssembly().GetTypes().SelectMany(x => x.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)))
            {
                var adminCommandAttribute = methodInfo.GetCustomAttributes(false).OfType<RequiredPermissionLevel>().ToArray();
                var conCommandAttribute = (ConCommandAttribute)methodInfo.GetCustomAttributes(false).FirstOrDefault(x => x is ConCommandAttribute);

                if (adminCommandAttribute.Length > 0 && conCommandAttribute != null)
                {
                    var overrideConfigEntry = DebugToolkit.Config.Bind("Permission System", $"Override: {conCommandAttribute.commandName}", adminCommandAttribute[0].Level,
                        $"Override Required Permission Level for the {conCommandAttribute.commandName} command");

                    _adminCommands.Add(conCommandAttribute.commandName, overrideConfigEntry);
                }
            }
        }

        [ConCommand(commandName = "perm_reload", flags = ConVarFlags.ExecuteOnServer, helpText = "Reload the permission system, updates user and commands permissions.")]
        [RequiredPermissionLevel(PermissionLevel.Admin)]
        private static void CCReloadPermissionSystem(ConCommandArgs args)
        {
            DebugToolkit.Config.Reload();
            Init();
            Log.MessageNetworked("Config File of DebugToolkit / Permission System successfully reloaded.", args, Log.LogLevel.Info);
        }

        [ConCommand(commandName = "perm_enable", flags = ConVarFlags.ExecuteOnServer, helpText = "Enable or disable the permission system." + Lang.PERM_ENABLE_ARGS)]
        [RequiredPermissionLevel(PermissionLevel.Admin)]
        private static void CCPermissionEnable(ConCommandArgs args)
        {
            IsEnabled.Value = !IsEnabled.Value;
            var res = IsEnabled.Value ? "enabled" : "disabled";
            Log.MessageNetworked($"Permission System is {res}", args, Log.LogLevel.Info);
        }

        [ConCommand(commandName = "perm_mod", flags = ConVarFlags.ExecuteOnServer, helpText = "Change the permission level of the specified playerid/username" + Lang.PERM_MOD_ARGS)]
        [RequiredPermissionLevel(PermissionLevel.Admin)]
        private static void CCPermissionAddUser(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.PERM_MOD_ARGS, args, Log.LogLevel.Error);
                return;
            }
            try
            {
                NetworkUser nu = Util.GetNetUserFromString(args.userArgs);

                if (IsEnabled.Value)
                {
                    if (Enum.TryParse(args[0], out PermissionLevel level))
                    {
                        // TODO: finish that lol
                        Log.MessageNetworked("Please edit the users permissions through the config file for now", args, Log.LogLevel.Error);
                    }
                    else
                    {
                        Log.MessageNetworked($"Couldn't parse correctly the level you provided ({args[0]})", args, Log.LogLevel.Error);
                    }
                }
                else
                {
                    Log.MessageNetworked("The permission system is currently disabled, enable it first with perm_enable", args, Log.LogLevel.Error);
                }
            }
            catch
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, Log.LogLevel.Error);
            }
        }

        public static bool CanUserExecute(NetworkUser networkUser, string conCommandName, List<string> userArgs)
        {
            if (_adminCommands.TryGetValue(conCommandName, out var requiredLevel))
            {
                var userLevel = GetPermissionLevel(networkUser);

                if (userLevel >= requiredLevel.Value)
                {
                    return true;
                }

                var conCommandArgs = new ConCommandArgs
                {
                    commandName = conCommandName,
                    sender = networkUser,
                    userArgs = userArgs
                };

                Log.MessageNetworked(string.Format(Lang.PS_NO_REQUIRED_LEVEL, requiredLevel.Value.ToString()), conCommandArgs);
                return false;
            }

            return true;
        }

        public static PermissionLevel GetPermissionLevel(this NetworkUser networkUser)
        {
            var adminList = _adminList.Value.Split(',');

            if (adminList.Contains(networkUser.userName))
            {
                return PermissionLevel.Admin;
            }

            var subAdminList = _subAdminList.Value.Split(',');

            if (subAdminList.Contains(networkUser.userName))
            {
                return PermissionLevel.SubAdmin;
            }

            return PermissionLevel.None;
        }
    }

    internal enum PermissionLevel
    {
        None,
        SubAdmin,
        Admin
    }
}
