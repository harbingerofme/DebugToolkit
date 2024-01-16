using RoR2;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Lists
    {
        [ConCommand(commandName = "list_interactables", flags = ConVarFlags.None, helpText = Lang.LISTINTERACTABLE_HELP)]
        [ConCommand(commandName = "list_interactibles", flags = ConVarFlags.None, helpText = Lang.LISTINTERACTABLE_HELP)]
        private static void CCListInteractables(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var cards = new HashSet<InteractableSpawnCard>(StringFinder.Instance.GetInteractableSpawnCardsFromPartial(arg));
            for (int i = 0; i < StringFinder.Instance.InteractableSpawnCards.Count; i++)
            {
                var isc = StringFinder.Instance.InteractableSpawnCards[i];
                if (cards.Contains(isc))
                {
                    var langInvar = StringFinder.GetLangInvar(StringFinder.GetInteractableName(isc.prefab));
                    sb.AppendLine($"[{i}]{isc.name}={langInvar}");
                }
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "interactables", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_player", flags = ConVarFlags.None, helpText = Lang.LISTPLAYER_HELP)]
        private static void CCListPlayer(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var players = new HashSet<NetworkUser>(StringFinder.Instance.GetPlayersFromPartial(arg));
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
            {
                var user = NetworkUser.readOnlyInstancesList[i];
                if (players.Contains(user))
                {
                    sb.AppendLine($"[{i}]{user.userName}");
                }
            }
            if (sb.Length > 0)
            {
                Log.MessageNetworked(sb.ToString().TrimEnd('\n'), args, LogLevel.MessageClientOnly);
            }
            else
            {
                var s = NetworkClient.active ? string.Format(Lang.NOMATCH_ERROR, "players", arg) : Lang.NOCONNECTION_ERROR;
                Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "list_ai", flags = ConVarFlags.None, helpText = Lang.LISTAI_HELP)]
        private static void CCListAI(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetAisFromPartial(arg);
            foreach (var index in indices)
            {
                var master = MasterCatalog.GetMasterPrefab(index).GetComponent<CharacterMaster>();
                var langInvar = StringFinder.GetLangInvar(StringFinder.GetMasterName(master));
                sb.AppendLine($"[{(int)index}]{master.name}={langInvar}");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "masters", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_body", flags = ConVarFlags.None, helpText = Lang.LISTBODY_HELP)]
        private static void CCListBody(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetBodiesFromPartial(arg);
            foreach (var index in indices)
            {
                var body = BodyCatalog.GetBodyPrefabBodyComponent(index);
                var langInvar = StringFinder.GetLangInvar(body.baseNameToken);
                sb.AppendLine($"[{(int)index}]{body.name}={langInvar}");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "bodies", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_elite", flags = ConVarFlags.None, helpText = Lang.LISTELITE_HELP)]
        private static void CCListElites(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetElitesFromPartial(arg);
            foreach (var index in indices)
            {
                var elite = EliteCatalog.GetEliteDef(index);
                var name = elite?.name ?? "None";
                var langInvar = StringFinder.GetLangInvar(elite?.modifierToken).Replace("{0}", "");
                sb.AppendLine($"[{(int)index}]{name}={langInvar}");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "elites", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_team", flags = ConVarFlags.None, helpText = Lang.LISTTEAM_HELP)]
        private static void CCListTeams(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetTeamsFromPartial(arg);
            foreach (var index in indices)
            {
                sb.AppendLine($"[{(int)index}]{index}");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "teams", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_directorcards", flags = ConVarFlags.None, helpText = Lang.LISTDIRECTORCARDS_HELP)]
        private static void CCListDirectorCards(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var cards = new HashSet<DirectorCard>(StringFinder.Instance.GetDirectorCardsFromPartial(arg));
            for (int i = 0; i < StringFinder.Instance.DirectorCards.Count; i++)
            {
                var card = StringFinder.Instance.DirectorCards[i];
                if (cards.Contains(card))
                {
                    var langInvar = StringFinder.GetLangInvar(StringFinder.GetMasterName(card.spawnCard.prefab.GetComponent<CharacterMaster>()));
                    sb.AppendLine($"[{i}]{card.spawnCard.name}={langInvar}");
                }
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "director cards", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_skin", flags = ConVarFlags.None, helpText = Lang.LISTSKIN_HELP)]
        private static void CCListSkin(ConCommandArgs args)
        {
            //string langInvar;
            StringBuilder sb = new StringBuilder();
            if (args.Count == 0)
            {
                args.userArgs.Add(Lang.ALL); //simple
            }
            if (args.Count >= 1)
            {
                string bodyName = args[0];
                string upperBodyName = bodyName.ToUpperInvariant();

                switch (upperBodyName)
                {
                    case "BODY":
                        foreach (var bodyComponent in BodyCatalog.allBodyPrefabBodyBodyComponents)
                        {
                            AppendSkinIndices(sb, bodyComponent);
                        }
                        break;
                    case Lang.ALL:
                        HashSet<SkinDef> skinDefs = new HashSet<SkinDef>();
                        foreach (var skin in SkinCatalog.allSkinDefs)
                        {
                            if (skinDefs.Contains(skin))
                                continue;
                            skinDefs.Add(skin);
                            var langInvar = StringFinder.GetLangInvar(skin.nameToken);
                            sb.AppendLine($"[{skin.skinIndex}] {skin.name}={langInvar}");
                        }
                        break;
                    default:
                        CharacterBody body;
                        if (upperBodyName == "SELF")
                        {
                            if (args.sender == null)
                            {
                                Log.Message("Can't choose self if not in-game!", LogLevel.Error);
                                return;
                            }
                            if (args.senderBody)
                            {
                                body = args.senderBody;
                            }
                            else
                            {
                                if (args.senderMaster && args.senderMaster.bodyPrefab)
                                {
                                    body = args.senderMaster.bodyPrefab.GetComponent<CharacterBody>();
                                }
                                else
                                {
                                    body = BodyCatalog.GetBodyPrefabBodyComponent(args.sender.bodyIndexPreference);
                                    // a little redundant
                                }
                            }
                        }
                        else
                        {
                            var bodyIndex = StringFinder.Instance.GetBodyFromPartial(args[0]);
                            if (bodyIndex == BodyIndex.None)
                            {
                                Log.MessageNetworked("Please use list_body to print CharacterBodies", args, LogLevel.MessageClientOnly);
                                return;
                            }
                            body = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
                        }
                        if (body)
                        {
                            AppendSkinIndices(sb, body);
                        }
                        else
                        {
                            Log.MessageNetworked("Please use list_body to print CharacterBodies", args, LogLevel.MessageClientOnly);
                            return;
                        }
                        break;
                }
            }

            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        private static void AppendSkinIndices(StringBuilder stringBuilder, CharacterBody body)
        {
            var skins = BodyCatalog.GetBodySkins(body.bodyIndex);
            if (skins.Length > 0)
            {
                var langInvar = StringFinder.GetLangInvar(body.baseNameToken);
                stringBuilder.AppendLine($"[{body.bodyIndex}]{body.name}={langInvar}");
                int i = 0;
                foreach (var skinDef in skins)
                {
                    langInvar = StringFinder.GetLangInvar(skinDef.nameToken);
                    stringBuilder.AppendLine($"\t[{i}={skinDef.skinIndex}] {skinDef.name}={langInvar}");
                    i++;
                }
            }
        }
    }
}
