using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using RoR2;

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

        private static Dictionary<string, ConfigEntry<PermissionLevel>> _adminCommands;

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

            _adminCommands = new Dictionary<string, ConfigEntry<PermissionLevel>>();

            foreach (var methodInfo in Assembly.GetExecutingAssembly().GetTypes().SelectMany(x => x.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)))
            {
                var adminCommandAttribute = methodInfo.GetCustomAttributes(false).OfType<RequiredPermissionLevel>().ToArray();
                var conCommandAttribute = (ConCommandAttribute)methodInfo.GetCustomAttributes(false).FirstOrDefault(x => x is ConCommandAttribute);

                if (adminCommandAttribute.Length > 0 && conCommandAttribute != null)
                {
                    var overrideConfigEntry = DebugToolkit.Config.Bind("Permission System", $"Override: {conCommandAttribute.commandName}", adminCommandAttribute[0].Level,
                        $"Override Required Permission Level for Command {conCommandAttribute.commandName}");

                    _adminCommands.Add(conCommandAttribute.commandName, overrideConfigEntry);
                }
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

                var conCommandArgs = new ConCommandArgs()
                {
                    commandName = conCommandName,
                    sender = networkUser,
                    userArgs = userArgs
                };

                Log.MessageNetworked($"You don't have the required permission {requiredLevel.Value.ToString()} to use this command.", conCommandArgs);
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
