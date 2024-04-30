using BepInEx.Bootstrap;
using RoR2;

namespace DebugToolkit.Commands
{
    class Miscellaneous
    {
        [ConCommand(commandName = "post_sound_event", flags = ConVarFlags.None, helpText = Lang.POSTSOUNDEVENT_HELP)]
        [AutoComplete(Lang.POSTSOUNDEVENT_ARGS)]
        private static void CCPostSoundEvent(ConCommandArgs args)
        {
            // Hack to not substitute the value of the constant as the DS game version has a different value
            if ((bool)typeof(RoR2Application).GetField("isDedicatedServer").GetValue(RoR2Application.instance))
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (args.Count == 0)
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.POSTSOUNDEVENT_ARGS);
                return;
            }
            AkSoundEngine.PostEvent(args[0], CameraRigController.readOnlyInstancesList[0].gameObject);
        }

        [ConCommand(commandName = "reload_all_config", flags = ConVarFlags.None, helpText = Lang.RELOADCONFIG_HELP)]
        private static void CCReloadAllConfig(ConCommandArgs args)
        {
            foreach (var pluginInfo in Chainloader.PluginInfos.Values)
            {
                try
                {
                    // Will this even fail with the null conditional operator?
                    pluginInfo.Instance.Config?.Reload();
                }
                catch
                {
                    Log.MessageNetworked($"The config file for {pluginInfo} doesn't exist or has a custom name.", args, Log.LogLevel.Warning);
                }
            }
        }
    }
}
