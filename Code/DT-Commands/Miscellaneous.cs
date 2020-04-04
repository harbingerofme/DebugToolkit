using BepInEx.Bootstrap;
using RoR2;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Miscellaneous
    {
        [ConCommand(commandName = "post_sound_event", flags = ConVarFlags.ExecuteOnServer, helpText = "Post a sound event to the AkSoundEngine (WWise) by its event name.")]
        private static void CCPostSoundEvent(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            AkSoundEngine.PostEvent(args[0], CameraRigController.readOnlyInstancesList[0].localUserViewer.currentNetworkUser.master.GetBodyObject());
        }

        [ConCommand(commandName = "reload_all_config", flags = ConVarFlags.None, helpText = "Reload all default config files from all loaded plugins.")]
        private static void CCReloadAllConfig(ConCommandArgs _)
        {
            foreach (var pluginInfo in Chainloader.PluginInfos.Values)
            {
                try
                {
                    pluginInfo.Instance.Config?.Reload();
                }
                catch
                {
                    // exception if the config file of that plugin doesnt exist. Also, can't reload a plugins config if it has a custom name. 
                }
            }
        }


    }
}
