using RoR2;
using UnityEngine.Networking;

namespace DebugToolkit.Commands
{
    static class Macros
    {
        [ConCommand(commandName = "midgame", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_MIDGAME_HELP)]
        private static void Midgame(ConCommandArgs args)
        {
            NetworkUser a = args.sender;
            Invoke(a, "fixed_time", "1325");
            Invoke(a, "run_set_stages_cleared", "5");
            Invoke(a, "team_set_level", "1", "15");
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                Invoke(a, "remove_all_items", user.userName);
                Invoke(a, "random_items", "23", "Tier1:100,Tier2:60,Tier3:4", user.userName);
                Invoke(a, "give_equip", "random", user.userName);
            }
            Invoke(a, "set_scene", "bazaar");
        }

        [ConCommand(commandName = "lategame", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_LATEGAME_HELP)]
        private static void LateGame(ConCommandArgs args)
        {
            NetworkUser a = args.sender;
            Invoke(a, "fixed_time", "3420");
            Invoke(a, "run_set_stages_cleared", "8");
            Invoke(a, "team_set_level", "1", "24");
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                Invoke(a, "remove_all_items", user.userName);
                Invoke(a, "random_items", "75", "Tier1:100,Tier2:60,Tier3:4", user.userName);
                Invoke(a, "give_equip", "random", user.userName);
            }
            Invoke(a, "set_scene", "bazaar");
        }

        //Is there any conflict in having it be the same convar as vanilla indev builds?
        [ConCommand(commandName = "peace", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_DTPEACE_HELP)]
        [ConCommand(commandName = "dtpeace", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_DTPEACE_HELP)]
        private static void Peace(ConCommandArgs args)
        {
            Invoke(args.sender, "kill_all", "2", "1");
            Invoke(args.sender, "kill_all", "4", "1");
            Invoke(args.sender, "no_enemies", "1");
            //Vanilla peace also sets god 1 but ehh idk i dont need that
        }

        [ConCommand(commandName = "dtzoom", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_DTZOOM_HELP)]
        private static void Zoom(ConCommandArgs args)
        {
            Invoke(args.sender, "give_item", "hoof", "20");
            Invoke(args.sender, "give_item", "feather", "200");
        }

        [ConCommand(commandName = "dtdamage", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_DTDAMAGE_HELP)]
        private static void Damage(ConCommandArgs args)
        {
            Invoke(args.sender, "give_item", "boostdamage", "9999990");
        }

        [ConCommand(commandName = "scanner", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_SCANNER_HELP)]
        [ConCommand(commandName = "dtscanner", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.MACRO_SCANNER_HELP)]
        public static void CCScanner(ConCommandArgs args)
        {
            if (!args.senderMaster)
            {
                return;
            }
            if (!NetworkServer.active)
            {
                return;
            }
            Invoke(args.sender, "give_item", "BoostEquipmentRecharge", "100");
            Invoke(args.sender, "give_equip", "Scanner"); 
            //args.senderMaster.inventory.SetEquipmentIndex(RoR2Content.Equipment.Scanner.equipmentIndex, false);
            //args.senderMaster.inventory.GiveItemPermanent(RoR2Content.Items.BoostEquipmentRecharge, 100);
        }


        public static void Invoke(NetworkUser user, string commandname, params string[] args)
        {
            DebugToolkit.InvokeCMD(user, commandname, args);
        }
    }
}
