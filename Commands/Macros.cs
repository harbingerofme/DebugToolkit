using RoR2;
using static DebugToolkit.Log;
using DebugToolkit;
using System;

namespace DebugToolkit.Commands
{
    static class Macros
    {
        [ConCommand(commandName = "midgame", flags = ConVarFlags.ExecuteOnServer, helpText = "sets the current run to the 'midgame' as defined by HG. This command is DESTRUCTIVE.")]
        private static void Midgame(ConCommandArgs args)
        {
            NetworkUser a = args.sender;
            Invoke(a, "fixed_time", "1325");
            Invoke(a, "run_set_stages_cleared", "5");
            Invoke(a, "team_set_level", "1" ,"15");
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                Invoke(a, "remove_item", Lang.ALL, user.userName);
                Invoke(a, "random_items", "23", user.userName);
                Invoke(a, "give_equip", "random", user.userName);
            }
            Invoke(a, "set_scene", "bazaar");
        }

        [ConCommand(commandName = "lategame", flags = ConVarFlags.ExecuteOnServer, helpText = "sets the current run to the 'lategame' as defined by HG. This command is DESTRUCTIVE.")]
        private static void LateGame(ConCommandArgs args)
        {
            NetworkUser a = args.sender;
            Invoke(a, "fixed_time", "3420");
            Invoke(a, "run_set_stages_cleared", "8");
            Invoke(a, "team_set_level", "1", "24" );
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                Invoke(a, "remove_item", Lang.ALL, user.userName);
                Invoke(a, "random_items", "75" , user.userName);
                Invoke(a, "give_equip", "random", user.userName);
            }
            Invoke(a, "set_scene", "bazaar");
        }

        [ConCommand(commandName = "dtzoom", flags = ConVarFlags.ExecuteOnServer, helpText = "gives you 20 hooves and 200 feathers for getting around quickly.")]
        private static void Zoom(ConCommandArgs args)
        {
            Invoke(args.sender, "give_item", "hoof", "20");
            Invoke(args.sender, "give_item", "feather", "200");
        }

        private static void Invoke(NetworkUser user, string commandname, params string[] args)
        {
            DebugToolkit.InvokeCMD(user, commandname, args);
        }
    }
}
