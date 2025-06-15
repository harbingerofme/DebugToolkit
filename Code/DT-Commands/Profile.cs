using RoR2;
using System;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    internal class Profile
    {
        private static bool canSaveProfile = true;

        [ConCommand(commandName = "prevent_profile_writing", flags = ConVarFlags.None, helpText = Lang.PREVENT_PROFILE_WRITING_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCPreventProfileWriting(ConCommandArgs args)
        {
            if (args.Count > 0)
            {
                if (!Util.TryParseBool(args[0], out var result))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "'flag'", "'bool'"), args, LogLevel.MessageClientOnly);
                    return;
                }
                canSaveProfile = !result;
            }
            Log.MessageNetworked(String.Format(!canSaveProfile ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "Prevent writing"), args);
        }

        internal static bool PreventSave(On.RoR2.SaveSystem.orig_Save orig, SaveSystem self, UserProfile data, bool blocking)
        {
            return canSaveProfile && orig(self, data, blocking);
        }
    }
}
