using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Money
    {
        [ConCommand(commandName = "give_lunar", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVELUNAR_HELP)]
        [ConCommand(commandName = "give_lunarcoin", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVELUNAR_HELP)]
        [AutoComplete(Lang.GIVELUNAR_ARGS)]
        private static void CCGiveLunar(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                Log.Message("Can't modify Lunar coins of other users directly.", LogLevel.MessageClientOnly);
                return;
            }
            int amount = 1;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[0], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "amount", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            string str = "Nothing happened. Big surprise.";
            NetworkUser target = args.sender;
            if (amount > 0)
            {
                target.AwardLunarCoins((uint)amount);
                str = string.Format(Lang.GIVELUNAR_2, "Gave", amount);
            }
            if (amount < 0)
            {
                amount *= -1;
                target.DeductLunarCoins((uint)(amount));
                str = string.Format(Lang.GIVELUNAR_2, "Removed", amount);
            }
            Log.MessageNetworked(str, args);
        }

        [ConCommand(commandName = "give_void", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEVOID_HELP)]
        [ConCommand(commandName = "give_voidcoin", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEVOID_HELP)]
        [AutoComplete(Lang.GIVELUNAR_ARGS)]
        private static void CCGiveVoidCoin(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                Log.Message("Can't modify Lunar coins of other users directly.", LogLevel.MessageClientOnly);
                return;
            }
            int amount = 1;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[0], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "amount", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            string str = "Nothing happened. Big surprise.";
            CharacterMaster master = args.senderMaster;
            if (!master)
            {
                Log.MessageNetworked("No master to send coins to",args, LogLevel.MessageClientOnly);
                return;
            }
            if (amount > 0)
            {
                master.GiveVoidCoins((uint)amount);
                str = string.Format(Lang.GIVEVOIDC_2, "Gave", amount);
            }
            if (amount < 0)
            {
                master.GiveVoidCoins((uint)(amount));
                str = string.Format(Lang.GIVEVOIDC_2, "Removed", amount);
            }
            Log.MessageNetworked(str, args);
        }


        [ConCommand(commandName = "give_money", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEMONEY_HELP)]
        [AutoComplete(Lang.GIVEMONEY_ARGS)]
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

            if (!TextSerialization.TryParseInvariant(args[0], out int result))
            {
                Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "amount", "int"), args, LogLevel.MessageClientOnly);
                return;
            }

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.funkfrog_sipondo.sharesuite"))
            {
                if (UseShareSuite())
                {
                    ShareSuiteGive(result);
                    return;
                }
            }

            if (args.Count > 1 && args[1].ToUpperInvariant() != Lang.ALL && args[1].ToUpperInvariant() != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player != null)
                {
                    GiveMasterMoney(player.master, result);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            else
            {
                var teamIndex = args.senderMaster == null ? TeamIndex.Player : args.senderMaster.teamIndex;
                GiveTeamMoney(teamIndex, result);
            }

            Log.MessageNetworked("$$$", args);
        }

        [ConCommand(commandName = "rich", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.ALLMONEY_HELP)]
        [ConCommand(commandName = "poor", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NO_MONEY_HELP)]
        [AutoComplete(Lang.MONEY_ARGS)]
        public static void CC_poor(ConCommandArgs args)
        {
            bool rich = args.commandName == "rich";
            if (args.Count > 0 && args[0].ToUpperInvariant() != Lang.ALL && args[0].ToUpperInvariant() != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player != null)
                {
                    if (rich)
                    {
                        player.master.GiveMoney(2000000000 - player.master.money);
                        Log.MessageNetworked("$$$", args);
                    }
                    else
                    {
                        player.master.money = 0;
                        Log.MessageNetworked("-$", args);
                    }           
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            else
            {
                foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
                {
                    if (rich)
                    {
                        player.master.GiveMoney(2000000000 - player.master.money); 
                    }
                    else
                    {
                        player.master.money = 0;
                    }
                }
                if (rich)
                {
                    Log.MessageNetworked("We're rich", args);
                }
                else
                {
                    Log.MessageNetworked("We're poor", args);
                }
            }
        }
 

        private static void GiveMasterMoney(CharacterMaster master, int amount)
        {
            if (amount > 0)
            {
                master.GiveMoney((uint)amount);
                return;
            }
            master.money = (uint)Math.Max(0, master.money + amount);
        }

        private static void GiveTeamMoney(TeamIndex teamIndex, int money)
        {
            var players = new List<CharacterMaster>();
            foreach (var member in TeamComponent.GetTeamMembers(teamIndex))
            {
                var body = member.body;
                if (body && body.isPlayerControlled && body.master)
                {
                    players.Add(body.master);
                }
            }
            int num = players.Count;
            if (num != 0)
            {
                money = Mathf.CeilToInt(money / (float)num);
                foreach (var master in players)
                {
                    GiveMasterMoney(master, money);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void ShareSuiteGive(int amount)
        {
            var moneyPool = ShareSuite.MoneySharingHooks.SharedMoneyValue;
            amount = Math.Max(-moneyPool, amount);
            ShareSuite.MoneySharingHooks.AddMoneyExternal(amount);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool UseShareSuite()
        {
            return ShareSuite.ShareSuite.MoneyIsShared.Value && ShareSuite.GeneralHooks.IsMultiplayer();
        }
    }
}
