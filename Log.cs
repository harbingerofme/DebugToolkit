using BepInEx.Logging;
using RoR2.ConVar;
using RoR2.Networking;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2Cheats
{
    /* This class may be re-used and modified without attribution, as long as this notice remains.*/

    class Log
    {
        private const bool BepinexInfoAlwaysLogs = true;

        private static ManualLogSource logger;

        /** <summary>Unless added to the game and modified by the user, this convar is equivalent to #if DEBUG</summary>
         */
        public static BoolConVar DebugConvar = new BoolConVar
            (
            "ror2cheats_debug",
            RoR2.ConVarFlags.None,
#if DEBUG
            "1",
#else
            "0",
#endif
            "Ror2cheats extensive debugging");

        public Log(ManualLogSource bepLogger)
        {
            logger = bepLogger;
        }

        /** <summary>Sends a message to a console.</summary>
         * <param name="input">The message to display</param>
         * <param name="level">The level of the message, note that info may always be displayed in some cases</param>
         * <param name="target">Target console, note that everything to ror2 is also passed to bepinex.</param>
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    if (input.GetType() == typeof (string))
                    {
                        var msg = new LogNetworkMessageClass()
                        {
                            level = (int)level,
                            message = (string) input
                        };
                        MessageInfo($"Send {msg.level}:{msg.message} to {target - 1}");
                        NetworkServer.SendToClient(((int) target) - 1, 102, msg);
                    }
                    break;
            }
        }

        /** <summary>Sends a warning to a console.</summary>
         * <param name="input">The message to display</param>
         * <param name="target">Target console, note that everything to ror2 is also passed to bepinex.</param>
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MessageWarning(object input, Target target = Target.Ror2)
        {
            Message(input, LogLevel.Warning, target);
        }

        /** <summary>Sends info to a console, note that it may be surpressed in certain targets under certain conditions.</summary>
         * <param name="input">The message to display</param>
         * <param name="target">Target console, note that everything to ror2 is also passed to bepinex.</param>
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MessageInfo(object input, Target target = Target.Ror2)
        {
            Message(input, LogLevel.Info, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.NoInlining)]
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

        [NetworkMessageHandler(msgType = 102, client = true, server = false)]
        private static void HandleNetworkMessage(NetworkMessage networkMessage)
        {
            var msg = networkMessage.ReadMessage<LogNetworkMessageClass>();
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
            Bepinex = -1,
            Ror2 = 0
        }

        public class LogNetworkMessageClass : MessageBase
        {
            public int level;
            public string message;
        }
    }
}
