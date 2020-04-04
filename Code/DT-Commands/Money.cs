using RoR2;
using System.Runtime.CompilerServices;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Money
    {
        [ConCommand(commandName = "give_money", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives the specified amount of money to the specified player. " + Lang.GIVEMONEY_ARGS)]
        private static void CCGiveMoney(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.GIVEMONEY_ARGS, args, LogLevel.WarningClientOnly);
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out uint result))
            {
                return;
            }

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.funkfrog_sipondo.sharesuite"))
            {
                if (UseShareSuite())
                {
                    ShareSuiteGive(result);
                }
            }

                if (args.sender != null && args.Count < 2 || args[1].ToLower() != "all")
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
            ShareSuite.MoneySharingHooks.AddMoneyExternal((int) amount);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool UseShareSuite()
        {
            if (ShareSuite.ShareSuite.MoneyIsShared.Value)
                return true;
            return false;
        }
    }
}
