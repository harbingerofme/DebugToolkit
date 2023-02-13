using BepInEx.Bootstrap;
using RoR2;

namespace DebugToolkit.Commands
{
    class Miscellaneous
    {
        [ConCommand(commandName = "post_sound_event", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.POSTSOUNDEVENT_HELP)]
        private static void CCPostSoundEvent(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.POSTSOUNDEVENT_ARGS, args, Log.LogLevel.MessageClientOnly);
                return;
            }
            AkSoundEngine.PostEvent(args[0], CameraRigController.readOnlyInstancesList[0].localUserViewer.currentNetworkUser.master.GetBodyObject());
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
