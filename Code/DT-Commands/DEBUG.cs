using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace DebugToolkit.Commands
{
#if DEBUG
    class DEBUG
    {
        [ConCommand(commandName = "network_echo", flags = ConVarFlags.ExecuteOnServer, helpText = "Sends a message to the target network user.")]
        private static void CCNetworkEcho(ConCommandArgs args)
        {
            args.CheckArgumentCount(2);
            Log.Target target = (Log.Target)args.GetArgInt(0);

            //Some fancyspancy thing that concatenates all remaining arguments as a single string.
            StringBuilder s = new StringBuilder();
            args.userArgs.RemoveAt(0);
            args.userArgs.ForEach((string temp) => { s.Append(temp + " "); });
            string str = s.ToString().TrimEnd(' ');

            Log.Message(str, Log.LogLevel.Message, target);
        }

        [ConCommand(commandName = "getItemName", flags = ConVarFlags.None, helpText = "Match a partial localised item name to an ItemIndex")]
        private static void CCGetItemName(ConCommandArgs args)
        {
            Log.Message(StringFinder.Instance.GetItemFromPartial(args[0]).ToString());
        }

        [ConCommand(commandName = "getBodyName", flags = ConVarFlags.None, helpText = "Match a bpartial localised body name to a character body name")]
        private static void CCGetBodyName(ConCommandArgs args)
        {
            Log.Message(StringFinder.Instance.GetBodyName(args[0]));
        }

        [ConCommand(commandName = "getEquipName", flags = ConVarFlags.None, helpText = "Match a partial localised equip name to an EquipIndex")]
        private static void CCGetEquipName(ConCommandArgs args)
        {
            Log.Message(StringFinder.Instance.GetEquipFromPartial(args[0]).ToString());
        }

        [ConCommand(commandName = "getMasterName", flags = ConVarFlags.None, helpText = "Match a partial localised Master name to a CharacterMaster")]
        private static void CCGetMasterName(ConCommandArgs args)
        {
            Log.Message(StringFinder.Instance.GetMasterName(args[0]));
        }

        [ConCommand(commandName = "getTeamIndexPartial", flags = ConVarFlags.None, helpText = "Match a partial TeamIndex")]
        private static void CCGetTeamIndexPartial(ConCommandArgs args)
        {
            //Alias.Instance.GetMasterName(args[0]);
            Log.Message(StringFinder.GetEnumFromPartial<TeamIndex>(args[0]).ToString());
        }

        [ConCommand(commandName = "getDirectorCardPartial", flags = ConVarFlags.None, helpText = "Match a partial DirectorCard")]
        private static void CCGetDirectorCardPartial(ConCommandArgs args)
        {
            Log.Message(StringFinder.Instance.GetDirectorCardFromPartial(args[0]).spawnCard.prefab.name);
        }

        [ConCommand(commandName = "list_family", flags = ConVarFlags.ExecuteOnServer, helpText = "Lists all monster families in the current stage.")]
        private static void CCListFamily(ConCommandArgs args)
        {
            StringBuilder s = new StringBuilder();
            foreach (ClassicStageInfo.MonsterFamily family in ClassicStageInfo.instance.possibleMonsterFamilies)
            {
                s.AppendLine(family.familySelectionChatString);
            }
            Log.MessageNetworked(s.ToString(), args, Log.LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_pcmc", flags = ConVarFlags.None, helpText = "Lists all PlayerCharacterMasterController instances.")]
        private static void CCListPlayerCharacterMasterController(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Number of PCMC instances : " + PlayerCharacterMasterController.instances.Count);
            foreach (var masterController in PlayerCharacterMasterController.instances)
            {
                sb.AppendLine($" is connected : {masterController.isConnected}");
            }
            if (args.sender == null)
            {
                Log.Message(sb.ToString());
            }
            else
            {
                Log.MessageNetworked(sb.ToString(), args, Log.LogLevel.MessageClientOnly);
            }

        }

    }
#endif
}
