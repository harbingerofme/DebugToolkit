using RoR2;
using System.Text;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Lists
    {
        [ConCommand(commandName = "list_interactables", flags = ConVarFlags.None, helpText = "Lists all interactables.")]
        private static void CCList_interactables(ConCommandArgs _)
        {
            StringBuilder s = new StringBuilder();
            foreach (InteractableSpawnCard isc in StringFinder.Instance.InteractableSpawnCards)
            {
                s.AppendLine(isc.name.Replace("isc", string.Empty));
            }
            Log.Message(s.ToString(), LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_player", flags = ConVarFlags.None, helpText = Lang.LISTPLAYER_ARGS)]
        private static void CCListPlayer(ConCommandArgs args)
        {
            NetworkUser n;
            StringBuilder list = new StringBuilder();
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
            {
                n = NetworkUser.readOnlyInstancesList[i];
                list.AppendLine($"[{i}]{n.userName}");

            }
            Log.MessageNetworked(list.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_AI", flags = ConVarFlags.None, helpText = Lang.LISTAI_ARGS)]
        private static void CCListAI(ConCommandArgs _)
        {
            string langInvar;
            int i = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var master in MasterCatalog.allAiMasters)
            {
                langInvar = StringFinder.GetLangInvar(master.bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                sb.AppendLine($"[{i}]{master.name}={langInvar}");
                i++;
            }
            Log.Message(sb);
        }

        [ConCommand(commandName = "list_Body", flags = ConVarFlags.None, helpText = Lang.LISTBODY_ARGS)]
        private static void CCListBody(ConCommandArgs _)
        {
            string langInvar;
            int i = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                langInvar = StringFinder.GetLangInvar(body.baseNameToken);
                sb.AppendLine($"[{i}]{body.name}={langInvar}");
                i++;
            }
            Log.Message(sb);
        }

        [ConCommand(commandName = "list_Directorcards", flags = ConVarFlags.None, helpText = Lang.NOMESSAGE)]
        private static void CCListDirectorCards(ConCommandArgs _)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var card in StringFinder.Instance.DirectorCards)
            {
                sb.AppendLine($"{card.spawnCard.name}");
            }
            Log.Message(sb);
        }
    }
}
