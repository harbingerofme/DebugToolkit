using RoR2;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class MoneyAndExp
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
            }

            Log.MessageNetworked("$$$", args);
        }



    }
}
