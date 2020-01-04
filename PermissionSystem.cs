using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using RoR2;

namespace DebugToolkit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
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

        private static ConfigEntry<PermissionLevel> _defaultPermissionLevel;
        private static readonly Dictionary<string, ConfigEntry<PermissionLevel>> AdminCommands = new Dictionary<string, ConfigEntry<PermissionLevel>>();

        internal static void Init(object _ = null, EventArgs __ = null)
        {
            IsEnabled = DebugToolkit.Configuration.Bind("Permission System", "1. Enable", false,
                "Is the Permission System enabled.");
            IsEnabled.SettingChanged += Init;

            if (!IsEnabled.Value)
                return;

            _adminList = DebugToolkit.Configuration.Bind("Permission System", "2. Admin", "76561197960265728, 76561197960265729",
                "Who is/are the admin(s). They are identified using their STEAM64 ID, the ID can be seen when the player connects to the server.");
            _subAdminList = DebugToolkit.Configuration.Bind("Permission System", "3. Sub Admin List", "76561197960265730, 76561197960265731",
                "Who is/are the sub admin(s).");

            _defaultPermissionLevel = DebugToolkit.Configuration.Bind("Permission System", "4. Default Permission Level", PermissionLevel.SubAdmin,
                "What is the default permission level to use DebugToolkit commands, available levels : None (0), SubAdmin (1), Admin (2)");

            AdminCommands.Clear();

            AddPermissionCheckToConCommands();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void AddPermissionCheckToConCommands()
        {
            foreach (var methodInfo in Assembly.GetCallingAssembly().GetTypes().SelectMany(x => x.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)))
            {
                var adminCommandAttribute = methodInfo.GetCustomAttribute<RequiredPermissionLevel>(false);
                var conCommandAttribute = (ConCommandAttribute)methodInfo.GetCustomAttributes(false).FirstOrDefault(x => x is ConCommandAttribute);
                if (conCommandAttribute != null)
                {
                    if (adminCommandAttribute != null)
                    {
                        var overrideConfigEntry = DebugToolkit.Configuration.Bind("Permission System", $"Override: {conCommandAttribute.commandName}", adminCommandAttribute[0].Level,
                            $"Override Required Permission Level for the {conCommandAttribute.commandName} command");

                        AdminCommands.Add(conCommandAttribute.commandName, overrideConfigEntry);
                    }
                    else
                    {
                        var overrideConfigEntry = DebugToolkit.Configuration.Bind("Permission System", $"Override: {conCommandAttribute.commandName}", _defaultPermissionLevel.Value,
                            $"Override Required Permission Level for the {conCommandAttribute.commandName} command");

                        AdminCommands.Add(conCommandAttribute.commandName, overrideConfigEntry);
                    }
                }
            }
        }

        [ConCommand(commandName = "perm_reload", flags = ConVarFlags.ExecuteOnServer, helpText = "Reload the permission system, updates user and commands permissions.")]
        [RequiredPermissionLevel(PermissionLevel.Admin)]
        private static void CCReloadPermissionSystem(ConCommandArgs args)
        {
            DebugToolkit.Configuration.Reload();
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
                        Log.MessageNetworked("Please edit the users permissions through the config file for now and reload using perm_reload in the console", args, Log.LogLevel.Error);
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
            if (AdminCommands.TryGetValue(conCommandName, out var requiredLevel))
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

        public static bool HasMorePerm(NetworkUser sender, NetworkUser target, ConCommandArgs args)
        {
            var senderElevationLevel = sender ? sender.GetPermissionLevel() : PermissionLevel.Admin + 1; // +1 for server console
            var targetElevationLevel = target.GetPermissionLevel();

            if (senderElevationLevel < targetElevationLevel)
            {
                Log.MessageNetworked(string.Format(Lang.PS_ARGUSER_HAS_MORE_PERM, target.userName), args, Log.LogLevel.Error);
                return false;
            }

            if (senderElevationLevel == targetElevationLevel)
            {
                Log.MessageNetworked(string.Format(Lang.PS_ARGUSER_HAS_SAME_PERM, target.userName), args, Log.LogLevel.Error);
                return false;
            }

            return true;
        }

        private static PermissionLevel GetPermissionLevel(this NetworkUser networkUser)
        {
            var adminList = _adminList.Value.Split(',');

            if (adminList.Contains(networkUser.GetNetworkPlayerName().steamId.value.ToString()))
            {
                return PermissionLevel.Admin;
            }

            var subAdminList = _subAdminList.Value.Split(',');

            if (subAdminList.Contains(networkUser.GetNetworkPlayerName().steamId.value.ToString()))
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
