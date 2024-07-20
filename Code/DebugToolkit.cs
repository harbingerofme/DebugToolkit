using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using DebugToolkit.Code;
using DebugToolkit.Commands;
using DebugToolkit.Permissions;
using R2API.Utils;
using RoR2;
using LogLevel = DebugToolkit.Log.LogLevel;

namespace DebugToolkit
{
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInPlugin(GUID, modname, modver)]
    public class DebugToolkit : BaseUnityPlugin
    {
        public const string modname = "DebugToolkit", modver = "3.16.0";
        public const string GUID = "iHarbHD." + modname;

        internal static ConfigFile Configuration;

        private void Awake()
        {
            Configuration = base.Config;

            new Log(Logger);

            //LogBuildInfo();

            Log.Message("Created by Harb, iDeathHD and . Based on RoR2Cheats by Morris1927.", LogLevel.Info, Log.Target.Bepinex);

            MacroSystem.Init();
            PermissionSystem.Init();
            Hooks.InitializeHooks();
            NetworkManager.Init();
        }

        private void LogBuildInfo()
        {
            #region Not Release Message
#if !RELEASE   //Additional references in this block must be fully qualifed as to not use them in Release Builds.
            string gitVersion = "";
            using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"{GetType().Namespace}.Resources.CurrentCommit"))
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                gitVersion = reader.ReadToEnd();
            }

            Log.MessageWarning(
#if DEBUG
                $"This is a debug build!"
#elif NONETWORK
    $"This is a non-networked build!"
#elif BLEEDING
                $"This is a Bleeding-Edge build!"
#endif
    , Log.Target.Bepinex);
            Log.MessageWarning($"Commit: {gitVersion.Trim()}", Log.Target.Bepinex);
#endif
            #endregion
        }

        private void Start()
        {
            var _ = StringFinder.Instance;
        }

        private void Update()
        {
            MacroSystem.Update();

            if (Run.instance && Command_Noclip.IsActivated)
            {
                Command_Noclip.Update();
            }
        }

        public static void InvokeCMD(NetworkUser user, string commandName, params string[] arguments)
        {
            var args = arguments.ToList();
            var consoleUser = new Console.CmdSender(user);
            if (Console.instance)
                Console.instance.RunCmd(consoleUser, commandName, args);
            else
                Log.Message("InvokeCMD called whilst no console instance exists.", LogLevel.Error, Log.Target.Bepinex);
        }


        /// <summary>
        /// Required for automated manifest building.
        /// </summary>
        /// <returns>Returns the TS manifest Version</returns>
        public static string GetModVer()
        {
            return modver;
        }
    }
}
