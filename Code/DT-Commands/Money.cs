using RoR2;
using System.Runtime.CompilerServices;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Money
    {
        [ConCommand(commandName = "give_money", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEMONEY_HELP)]
        private static void CCGiveMoney(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEMONEY_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out uint result))
            {
                Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "amount", "uint"), args, LogLevel.MessageClientOnly);
                return;
            }

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.funkfrog_sipondo.sharesuite"))
            {
                if (UseShareSuite())
                {
                    ShareSuiteGive(result);
                }
            }

            if (args.sender != null && args.Count < 2 || args[1].ToUpper() != Lang.ALL || args[1].ToUpper() != Lang.DEFAULT_VALUE)
            {
                CharacterMaster master = args.sender?.master;
                if (args.Count >= 2)
                {
                    NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                    if (player != null)
                    {
                        master = player.master;
                    }
                    else if (args.sender == null)
                    {
                        Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                }

                if (master)
                {
                    master.GiveMoney(result);
                }
                else
                {
                    Log.MessageNetworked("Error: Master was null", args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            else
            {
                if (args.sender != null)
                {
                    TeamManager.instance.GiveTeamMoney(args.sender.master.teamIndex, result);
                }
                else
                {
                    TeamManager.instance.GiveTeamMoney(TeamIndex.Player, result);
                }
            }

            Log.MessageNetworked("$$$", args);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void ShareSuiteGive(uint amount)
        {
            ShareSuite.MoneySharingHooks.AddMoneyExternal((int)amount);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool UseShareSuite()
        {
            return ShareSuite.ShareSuite.MoneyIsShared.Value;
        }
    }
}
