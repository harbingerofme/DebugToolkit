using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugToolkit.Permissions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequiredLevel : Attribute
    {
        internal readonly Level Level;

        public RequiredLevel(Level level = Level.SubAdmin)
        {
            Level = level;
        }
    }

    public enum Level
    {
        None,
        SubAdmin,
        Admin
    }

    public static class PermissionSystem
    {
        internal static ConfigEntry<bool> IsEnabled;

        private static ConfigEntry<string> _adminList;
        private static ConfigEntry<string> _subAdminList;

        private static readonly HashSet<ulong> AdminSteamIdList = new HashSet<ulong>();
        private static readonly HashSet<ulong> SubAdminSteamIdList = new HashSet<ulong>();

        private static ConfigEntry<bool> _roR2CommandsNeedPermission;

        private static ConfigEntry<Level> _defaultPermissionLevel;
        private static readonly Dictionary<string, ConfigEntry<Level>> AdminCommands = new Dictionary<string, ConfigEntry<Level>>();

        public const string PERMISSION_SYSTEM_CONFIG_SECTION = "Permission System";
        public const char PERMISSION_SYSTEM_ADMIN_LIST_SEPARATOR = ',';
        public const string ADMIN_LIST_CONFIG_NAME = "2. Admin";
        public const string SUBADMIN_LIST_CONFIG_NAME = "3. Sub Admin List";

        internal static void Init()
        {
            IsEnabled = DebugToolkit.Configuration.Bind("Permission System", "1. Enable", false,
                "Is the Permission System enabled.");

            if (!IsEnabled.Value)
                return;

            _adminList = DebugToolkit.Configuration.Bind("Permission System", ADMIN_LIST_CONFIG_NAME, "76561197960265728, 76561197960265729",
                "Who is/are the admin(s). They are identified using their STEAM64 ID, the ID can be seen when the player connects to the server.");
            _subAdminList = DebugToolkit.Configuration.Bind("Permission System", SUBADMIN_LIST_CONFIG_NAME, "76561197960265730, 76561197960265731",
                "Who is/are the sub admin(s).");

            _defaultPermissionLevel = DebugToolkit.Configuration.Bind("Permission System", "4. Default Permission Level", Level.SubAdmin,
                "What is the default permission level to use DebugToolkit commands, available levels : None (0), SubAdmin (1), Admin (2)");

            _roR2CommandsNeedPermission = DebugToolkit.Configuration.Bind("Permission System",
                "5. RoR2 Console Commands use the Permission System", false,
                "When enabled, all the RoR2 Console Commands will be added to this config file and will only be fired if the permission check says so.");

            UpdateAdminListCache();
            UpdateSubAdminListCache();
            DebugToolkit.Configuration.SettingChanged += UpdateAdminListCacheOnConfigChange;

            AdminCommands.Clear();

            AddPermissionCheckToConCommands();

            if (_roR2CommandsNeedPermission.Value)
                AddPermissionCheckToConCommands(typeof(RoR2Application).Assembly);
        }

        private static void UpdateAdminListCache()
        {
            var adminSteamIds = _adminList.Value.Split(PERMISSION_SYSTEM_ADMIN_LIST_SEPARATOR).Select(s => s.Trim()).ToList();

            AdminSteamIdList.Clear();
            foreach (var steamId in adminSteamIds)
            {
                if (ulong.TryParse(steamId, out var steamIdULong))
                {
                    AdminSteamIdList.Add(steamIdULong);
                }
                else
                {
                    Log.Message($"Can't parse correctly the given STEAMID64 : ${steamId} for admins", Log.LogLevel.Error);
                }
            }
        }

        private static void UpdateSubAdminListCache()
        {
            var subAdminSteamIds = _subAdminList.Value.Split(PERMISSION_SYSTEM_ADMIN_LIST_SEPARATOR).Select(s => s.Trim()).ToList();

            SubAdminSteamIdList.Clear();
            foreach (var steamId in subAdminSteamIds)
            {
                if (ulong.TryParse(steamId, out var steamIdULong))
                {
                    SubAdminSteamIdList.Add(steamIdULong);
                }
                else
                {
                    Log.Message($"Can't parse correctly the given STEAMID64 : ${steamId} for sub admins", Log.LogLevel.Error);
                }
            }
        }

        private static void UpdateAdminListCacheOnConfigChange(object _, SettingChangedEventArgs settingChangedEventArgs)
        {
            var changedSetting = settingChangedEventArgs.ChangedSetting;

            if (changedSetting.Definition.Section == PERMISSION_SYSTEM_CONFIG_SECTION)
            {
                if (changedSetting.Definition.Key == ADMIN_LIST_CONFIG_NAME)
                {
                    UpdateAdminListCache();
                }
                else if (changedSetting.Definition.Key == SUBADMIN_LIST_CONFIG_NAME)
                {
                    UpdateSubAdminListCache();
                }
            }
        }

        // If no specific assembly is specified,
        // the assembly that calls this method will see their methods
        // being added to the permission system config file
        // and the permission will be checked if enabled.
        //
        // ReSharper disable once MemberCanBePrivate.Global
        public static void AddPermissionCheckToConCommands(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetCallingAssembly();

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(x => x != null).ToArray();
                foreach (var e in ex.LoaderExceptions)
                {
                    Log.Message(ex.Message, Log.LogLevel.Error, Log.Target.Bepinex);
                }
            }
            foreach (var methodInfo in types.SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)))
            {
                object[] attributes;
                try
                {
                    attributes = methodInfo.GetCustomAttributes(false);
                }
                catch (TypeLoadException ex)
                {
                    Log.Message(ex.Message, Log.LogLevel.Error, Log.Target.Bepinex);
                    continue;
                }
                catch (InvalidOperationException ex)
                {
                    Log.Message(ex.Message, Log.LogLevel.Error, Log.Target.Bepinex);
                    continue;
                }
                if (attributes == null)
                {
                    continue;
                }
                var conCommandAttributes = attributes.OfType<ConCommandAttribute>().ToArray();
                if (conCommandAttributes.Length > 0)
                {
                    var adminCommandAttribute = methodInfo.GetCustomAttribute<RequiredLevel>(false);
                    var usedPermissionLevel = adminCommandAttribute?.Level ?? _defaultPermissionLevel.Value;
                    foreach (var conCommand in conCommandAttributes)
                    {
                        if ((conCommand.flags & ConVarFlags.ExecuteOnServer) == ConVarFlags.ExecuteOnServer)
                        {
                            if (conCommand.commandName == "say")
                            {
                                usedPermissionLevel = Level.None;
                            }
                            var overrideConfigEntry = DebugToolkit.Configuration.Bind("Permission System", $"Override: {conCommand.commandName}", usedPermissionLevel,
                                $"Override Required Permission Level for the {conCommand.commandName} command");

                            AdminCommands.Add(conCommand.commandName, overrideConfigEntry);
                        }
                    }
                }
            }
        }

        [ConCommand(commandName = "perm_reload", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.PERM_RELOAD_HELP)]
        [RequiredLevel(Level.Admin)]
        private static void CCReloadPermissionSystem(ConCommandArgs args)
        {
            DebugToolkit.Configuration.Reload();
            Init();
            Log.MessageNetworked("Config File of DebugToolkit / Permission System successfully reloaded.", args, Log.LogLevel.Info);
        }

        [ConCommand(commandName = "perm_enable", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.PERM_ENABLE_HELP)]
        [AutoComplete(Lang.PERM_ENABLE_ARGS)]
        [RequiredLevel(Level.Admin)]
        private static void CCPermissionEnable(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                IsEnabled.Value = !IsEnabled.Value;
            }
            else
            {
                if (!Util.TryParseBool(args[0], out bool value))
                {
                    Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "value", "bool"), args, Log.LogLevel.ErrorClientOnly);
                    return;
                }
                IsEnabled.Value = value;
            }

            var res = string.Format(IsEnabled.Value ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "Permission System");
            if (IsEnabled.Value)
            {
                res += ". Saving and reloading the permission system.";
            }

            Log.MessageNetworked(res, args, Log.LogLevel.Info);

            DebugToolkit.Configuration.Save();
            Init();
        }

        [ConCommand(commandName = "perm_mod", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.PERM_MOD_HELP)]
        [AutoComplete(Lang.PERM_MOD_ARGS)]
        [RequiredLevel(Level.Admin)]
        private static void CCPermissionAddUser(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.PERM_MOD_ARGS, args, Log.LogLevel.Error);
                return;
            }
            try
            {
                NetworkUser nu = Util.GetNetUserFromString(args.userArgs);

                if (IsEnabled.Value)
                {
                    if (Enum.TryParse(args[0], out Level level))
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
                var userLevel = networkUser.GetPermissionLevel();

                if (userLevel >= requiredLevel.Value)
                    return true;

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
            var senderElevationLevel = sender ? sender.GetPermissionLevel() : Level.Admin + 1; // +1 for server console
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

        private static Level GetPermissionLevel(this NetworkUser networkUser)
        {
            var userSteamId = (ulong)networkUser.GetNetworkPlayerName().playerId.value;

            if (AdminSteamIdList.Contains(userSteamId))
                return Level.Admin;

            if (SubAdminSteamIdList.Contains(userSteamId))
                return Level.SubAdmin;

            return Level.None;
        }
    }
}