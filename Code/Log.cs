using BepInEx.Logging;
using RoR2.ConVar;
using RoR2;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using NetworkManager = DebugToolkit.Code.NetworkManager;

namespace DebugToolkit
{
    /* This class may be re-used and modified without attribution, as long as this notice remains.*/

    internal class Log
    {
        private const bool BepinexInfoAlwaysLogs = true;
        private const int NetworkEnum = 69;

        private static ManualLogSource logger;

        /** <summary>Unless added to the game and modified by the user, this convar is equivalent to #if DEBUG</summary>
         */
        public static BoolConVar DebugConvar = new BoolConVar
            (
            $"{DebugToolkit.modname.ToLower()}_debug",
            RoR2.ConVarFlags.None,
#if DEBUG
            "1",
#else
            "0",
#endif
            $"{DebugToolkit.modname} extensive debugging");

        public Log(ManualLogSource bepLogger)
        {
            logger = bepLogger;
        }

        internal static void InitRPC()
        {
            NetworkManager.DebugToolKitComponents.AddComponent<LogNet>();
        }

        /** <summary>Sends a message to a console.</summary>
         * <param name="input">The message to display</param>
         * <param name="level">The level of the message, note that info may always be displayed in some cases</param>
         * <param name="target">Target console, note that everything to ror2 is also passed to bepinex.</param>
         */
        public static void Message(object input, LogLevel level = LogLevel.Message, Target target = Target.Ror2)
        {
            switch (target)
            {
                case Target.Ror2:
                    Ror2Log(input, level);
                    break;
                case Target.Bepinex:
                    BepinexLog(input, level);
                    break;
                default:
                    int targetNr = (int)target;
                    if (NetworkUser.readOnlyInstancesList.Count-1>= targetNr && targetNr>=0)
                    {
                        if(input.GetType() != typeof(string))
                        {
                            Message($"Couldn't send network message because the message was not a string: {input}.", LogLevel.Error, Target.Bepinex);
                            return;
                        }
                        NetworkUser user = NetworkUser.readOnlyInstancesList[targetNr];
                        MessageInfo($"Send a network message to {targetNr}, length={((string) input).Length}");
                        Message((string) input, user, level);
                    }
                    else
                    {
                        Message($"Couldn't find target {targetNr} for message: {input}", LogLevel.Error, Target.Bepinex);
                    }
                    break;
            }
        }

        /** <summary>Sends a message back to the client that issued the command.</summary>
         * <param name="input">A string for the input, since we sending it over the network, we can't use arbitrary types.</param>
         * <param name="args">the commandargs</param>
         * <param name="level">The level to send the message at.</param>
         */
        public static void MessageNetworked(string input, ConCommandArgs args, LogLevel level = LogLevel.Message)
        {
            if (args.sender != null && !args.sender.isLocalPlayer)
            {
                Message(input, args.sender, level);
            }
            if ((int) level < NetworkEnum || args.sender == null || args.sender.isLocalPlayer)
            {
                Message(input, level);
            }

        }
            

        /** <summary></summary>
         *  <param name="input">The string to send</param>
         *  <param name="networkUser">The user to target, may not be null</param>
         *  <param name="level">The level, defaults to LogLevel.Message</param>
         *  */
        public static void Message(string input, NetworkUser networkUser, LogLevel level = LogLevel.Message)
        {
            if (networkUser == null)
            {
                return;
            }
            
            LogNet.Invoke(networkUser, input, (int)level);
        }

        /** <summary>Sends a warning to a console.</summary>
         * <param name="input">The message to display</param>
         * <param name="target">Target console, note that everything to ror2 is also passed to bepinex.</param>
         */
        public static void MessageWarning(object input, Target target = Target.Ror2)
        {
            Message(input, LogLevel.Warning, target);
        }

        /** <summary>Sends info to a console, note that it may be surpressed in certain targets under certain conditions.</summary>
         * <param name="input">The message to display</param>
         * <param name="target">Target console, note that everything to ror2 is also passed to bepinex.</param>
         */
        public static void MessageInfo(object input, Target target = Target.Ror2)
        {
            Message(input, LogLevel.Info, target);
        }

        private static void Ror2Log(object input, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                case LogLevel.InfoClientOnly:
                    if (DebugConvar.value)
                    {
                        Debug.Log(input);
                    }
                    break;
                case LogLevel.Message:
                case LogLevel.MessageClientOnly:
                    Debug.Log(input);
                    break;
                case LogLevel.Warning:
                case LogLevel.WarningClientOnly:
                    Debug.LogWarning(input);
                    break;
                case LogLevel.Error:
                case LogLevel.ErrorClientOnly:
                    Debug.LogError(input);
                    break;
            }
        }

        private static void BepinexLog(object input, LogLevel level)
        {
            if(logger == null)
            {
                throw new System.NullReferenceException("Log Class in " + Assembly.GetExecutingAssembly().GetName().Name + " not initialized prior to message call!");
            }
            switch (level)
            {
                case LogLevel.Info:
                case LogLevel.InfoClientOnly:
                    if (BepinexInfoAlwaysLogs || DebugConvar.value)
                    {
                        logger.LogInfo(input);
                    }
                    break;
                case LogLevel.Message:
                case LogLevel.MessageClientOnly:
                    logger.LogMessage(input);
                    break;
                case LogLevel.Warning:
                case LogLevel.WarningClientOnly:
                    logger.LogWarning(input);
                    break;
                case LogLevel.Error:
                case LogLevel.ErrorClientOnly:
                    logger.LogError(input);
                    break;
                

            }
        }

        public enum LogLevel
        {
            Info    = 0,
            Message = 1,
            Warning = 2,
            Error   = 3,
            InfoClientOnly = NetworkEnum+Info,
            MessageClientOnly = NetworkEnum+Message,
            WarningClientOnly = NetworkEnum+Warning,
            ErrorClientOnly = NetworkEnum+Error
        }

        public enum Target
        {
            Bepinex = -2,
            Ror2 = -1
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBeMadeStatic.Local
    // ReSharper disable once UnusedParameter.Local
    // ReSharper disable once UnusedMember.Local
    internal class LogNet : NetworkBehaviour
    {
        private static LogNet _instance;

        private void Awake()
        {
            _instance = this;
        }

        internal static void Invoke(NetworkUser networkUser, string msg, int level)
        {
            _instance.TargetLog(networkUser.connectionToClient, msg, level);
        }
        
        [TargetRpc]
        private void TargetLog(NetworkConnection target, string msg, int level)
        {
            Log.Message(msg, (Log.LogLevel)level);
            Hooks.ScrollConsoleDown();
        }
    }
}
