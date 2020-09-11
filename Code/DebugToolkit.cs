using BepInEx;
using RoR2;
using BepInEx.Configuration;
using LogLevel = DebugToolkit.Log.LogLevel;
using DebugToolkit.Commands;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using R2API;
using R2API.Utils;
using NetworkManager = DebugToolkit.Code.NetworkManager;

namespace DebugToolkit
{
    [BepInDependency("com.bepis.r2api")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [R2APISubmoduleDependency(nameof(PrefabAPI))]
    [BepInPlugin(GUID, modname, modver)]
    public class DebugToolkit : BaseUnityPlugin
    {
        public const string modname = "DebugToolkit", modver = "3.4.0";
        public const string GUID = "com.harbingerofme." + modname;

        private static MethodInfo RunCmdMethod;

        internal static ConfigFile Configuration;

        private void Awake()
        {
            Configuration = base.Config;

            new Log(Logger);

            #region Not Release Message
#if !RELEASE   //Additional references in this block must be fully qualifed as to not use them in Release Builds.
            string gitVersion = "";
            using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"{this.GetType().Namespace}.Resources.CurrentCommit"))
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                gitVersion= reader.ReadToEnd();
            }
            Log.MessageWarning(
#if DEBUG       
                $"This is a debug build!"
#elif NONETWORK
                $"This is a non-networked build!"
#elif NODYN
                $"This is a build without Microsoft.CSharp.dll included. Please put that file manually in your /plugins/ folder."
#elif BLEEDING  
                $"This is a Bleeding-Edge build!"
#endif          
                ,Log.Target.Bepinex);
            Log.MessageWarning($"Commit: {gitVersion.Trim()}",Log.Target.Bepinex);
#endif
            #endregion

            Log.Message("Created by Harb, iDeathHD and . Based on RoR2Cheats by Morris1927.", LogLevel.Info, Log.Target.Bepinex);

            PermissionSystem.Init();
            Hooks.InitializeHooks();
            NetworkManager.Init();

            RunCmdMethod = typeof(Console).GetMethod("RunCmd", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void Start()
        {
            var _ = StringFinder.Instance;
            ArgsAutoCompletion.GatherCommandsAndFillStaticArgs();
        }

        private void Update()
        {
            if (Run.instance && Command_Noclip.IsActivated)
            {
                Command_Noclip.Update();
            }
        }

        public static void InvokeCMD(NetworkUser user, string commandname, params string[] arguments)
        {
            List<string> args = arguments.ToList<string>();
            var consoleUser = new Console.CmdSender(user);
            if (Console.instance)
                RunCmdMethod.Invoke(Console.instance, new object []  { consoleUser, commandname, args});
            else
                Log.Message($"InvokeCMD called whilst no console instance exists.",LogLevel.Error,Log.Target.Bepinex);
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