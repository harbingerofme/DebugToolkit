using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Lists
    {
        [ConCommand(commandName = "list_interactables", flags = ConVarFlags.None, helpText = "Lists all interactables. "+Lang.LISTINTERACTABLE_ARGS)]
        private static void CCList_interactables(ConCommandArgs args)
        {
            //edits based on StringFinder.GetInteractableSpawnCard()
            StringBuilder s = new StringBuilder();
            int resultCount = 0;
            if (args.Count > 0)
            {
                string name = args.GetArgString(0);
                // s.AppendLine($"Checking for: ");
                foreach (InteractableSpawnCard isc in StringFinder.Instance.InteractableSpawnCards)
                {
                    var iscName = isc.name.ToUpper().Replace("ISC", string.Empty);
                    if (iscName.Equals(name.ToUpper().Replace("ISC", string.Empty)) || iscName.Contains(name.ToUpper()))
                    {
                        resultCount++;
                        s.AppendLine(isc.name.Replace("isc", string.Empty));
                    }
                }
                if (resultCount == 0)
                {
                    s.AppendLine($"No interactables found that match \"{name}\".");
                }
            } else
            {
                foreach (InteractableSpawnCard isc in StringFinder.Instance.InteractableSpawnCards)
                {
                    s.AppendLine(isc.name.Replace("isc", string.Empty));
                }
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

        [ConCommand(commandName = "list_skin", flags = ConVarFlags.None, helpText = "List all bodies with skins. " + Lang.LISTSKIN_ARGS)]
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
                string bodyName = args.GetArgString(0);
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
                            string requestedBodyName = StringFinder.Instance.GetBodyName(args[0]);
                            if (requestedBodyName == null)
                            {
                                Log.MessageNetworked("Please use list_body to print CharacterBodies", args, LogLevel.MessageClientOnly);
                                return;
                            }
                            GameObject newBody = BodyCatalog.FindBodyPrefab(requestedBodyName);
                            body = newBody.GetComponent<CharacterBody>();
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

            Log.Message(sb);
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
