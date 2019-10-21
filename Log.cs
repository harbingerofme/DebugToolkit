using BepInEx.Logging;
using RoR2.ConVar;
using System.Runtime.CompilerServices;
using UnityEngine;

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
                    Debug.Log(input);
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
                    Debug.LogWarning(input);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BepinexLog(object input, LogLevel level)
        {
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
                    logger.LogWarning(input);
                    break;
            }
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
            Ror2 = 0,
            Bepinex = 1
        }
    }
}
