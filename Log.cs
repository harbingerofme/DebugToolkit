using BepInEx.Logging;
using RoR2.ConVar;
using RoR2;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;
using MiniRpcLib;

namespace RoR2Cheats
{
    /* This class may be re-used and modified without attribution, as long as this notice remains.*/

    internal class Log
    {
        private const bool BepinexInfoAlwaysLogs = true;

        private static ManualLogSource logger;
        private static MiniRpcLib.Action.IRpcAction<LogNetworkMessageClass> networkMessageClientRPC;

        /** <summary>Unless added to the game and modified by the user, this convar is equivalent to #if DEBUG</summary>
         */
        public static BoolConVar DebugConvar = new BoolConVar
            (
            $"{RoR2Cheats.modname.ToLower()}_debug",
            RoR2.ConVarFlags.None,
#if DEBUG
            "1",
#else
            "0",
#endif
            $"{RoR2Cheats.modname} extensive debugging");

        public Log(ManualLogSource bepLogger, MiniRpcInstance miniRpc)
        {
            logger = bepLogger;
            networkMessageClientRPC = miniRpc.RegisterAction(MiniRpcLib.Target.Client, (NetworkUser _, LogNetworkMessageClass message) => { HandleNetworkMessage(message); });
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

            var msg = new LogNetworkMessageClass()
            {
                level = (int)level,
                message = input
            };
            MessageInfo($"Send {msg.level}:{msg.message} to {networkUser}");
            networkMessageClientRPC.Invoke(msg, networkUser);
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
                    if (DebugConvar.value)
                    {
                        Debug.Log(input);
                    }
                    break;
                case LogLevel.Message:
                    Debug.Log(input);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(input);
                    break;
                case LogLevel.Error:
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
                    if (BepinexInfoAlwaysLogs || DebugConvar.value)
                    {
                        logger.LogInfo(input);
                    }
                    break;
                case LogLevel.Message:
                    logger.LogMessage(input);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(input);
                    break;
                case LogLevel.Error:
                    logger.LogError(input);
                    break;
            }
        }

        private static void HandleNetworkMessage(LogNetworkMessageClass msg)
        {
            Message(msg.message, (LogLevel) msg.level);
        }

        public enum LogLevel
        {
            Info    = -1,
            Message = 0,
            Warning = 1,
            Error   = 2
        }

        public enum Target
        {
            Bepinex = -2,
            Ror2 = -1
        }

        public class LogNetworkMessageClass : MessageBase
        {
            public int level;
            public string message;
        }
    }
}
