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
        private const int NetworkEnum = 69;

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

        /** <summary>Sends a message back to the client that issued the command.</summary>
         * <param name="input">A string for the input, since we sending it over the network, we can't use arbitrary types.</param>
         * <param name="args">the commandargs</param>
         * <param name="level">The level to send the message at.</param>
         */
        public static void Message(string input, ConCommandArgs args, LogLevel level = LogLevel.Message)
        {
            if ((int) level < NetworkEnum || args.sender.isLocalPlayer == true)
            {
                Message(input, level);
            }
            if (args.sender.isLocalPlayer == false)
            {
                Message(input, args.sender, level);
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

        private static void HandleNetworkMessage(LogNetworkMessageClass msg)
        {
            Message(msg.message, (LogLevel) msg.level);
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

        public class LogNetworkMessageClass : MessageBase
        {
            public int level;
            public string message;

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(level);
                writer.Write(message);
            }

            public override void Deserialize(NetworkReader reader)
            {
                level = reader.ReadInt32();
                message = reader.ReadString();
            }
        }
    }
}
