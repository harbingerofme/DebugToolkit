using BepInEx.Bootstrap;
using RoR2;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

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
            uint result;
            if (TextSerialization.TryParseInvariant(args[0], out uint eventId))
            {
                result = AkSoundEngine.PostEvent(eventId, CameraRigController.readOnlyInstancesList[0].gameObject);
            }
            else
            {
                result = AkSoundEngine.PostEvent(args[0], CameraRigController.readOnlyInstancesList[0].gameObject);
            }
            if (result == 0)
            {
                Log.Message("Sound not found.");
            }
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

        [ConCommand(commandName = "delay", flags = ConVarFlags.None, helpText = Lang.DELAY_HELP)]
        [AutoComplete(Lang.DELAY_ARGS)]
        private static void CCDelay(ConCommandArgs args)
        {
            if (args.Count < 2)
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.DELAY_ARGS);
                return;
            }

            string cmd = args[1];
            if (!TextSerialization.TryParseInvariant(args[0], out float delay))
            {
                Log.Message(string.Format(Lang.PARSE_ERROR, "delay", "float"));
                return;
            }

            DebugToolkit.Instance.StartCoroutine(InvokeRoutine(() => Console.instance.SubmitCmd(args.sender, cmd), delay));

            static IEnumerator InvokeRoutine(System.Action action, float delay)
            {
                yield return new WaitForSeconds(delay);
                action();
            }
        }
 
        [ConCommand(commandName = "dump_mods", flags = ConVarFlags.None, helpText = Lang.DUMPMODS_HELP)]
        public static void CCMods(ConCommandArgs args)
        {
            int requiredByAll = args.TryGetArgInt(0).GetValueOrDefault(0);
 
            StringBuilder log = new StringBuilder();

            log.Append("All loaded mods\n\n");
            foreach (var a in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                log.Append(a.ToString());
                log.Append("\n");
            }
            log.Append("\n");
            if (NetworkModCompatibilityHelper._networkModList.Length == 0)
            {
                log.Append("No mods tagged as RequiredByAll. This mod pack is Vanilla compatible.");
            }
            else
            {
                log.Append("Mods tagged as RequiredByAll\n\n");
                foreach (var a in NetworkModCompatibilityHelper.networkModList)
                {
                    log.Append(a.ToString());
                    log.Append("\n");
                }
            }
            Log.Message(log.ToString());
        }

    }
}
