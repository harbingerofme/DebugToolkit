﻿using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Lists
    {
        [ConCommand(commandName = "list_interactables", flags = ConVarFlags.None, helpText = Lang.LISTINTERACTABLE_HELP)]
        private static void CCListInteractables(ConCommandArgs args)
        {
            //edits based on StringFinder.GetInteractableSpawnCard()
            StringBuilder s = new StringBuilder();
            IEnumerable<InteractableSpawnCard> list;
            if (args.Count > 0)
            {
                list = StringFinder.Instance.GetInteractableSpawnCards(args[0]);

                if (list.Count() == 0)
                    s.AppendLine($"No interactables found that match \"{args[0]}\".");
            } else
            {
                list = StringFinder.Instance.InteractableSpawnCards;
            }

            foreach (InteractableSpawnCard isc in list)
            {
                s.AppendLine(isc.name.Replace("isc", string.Empty));
            }
            Log.MessageNetworked(s.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_player", flags = ConVarFlags.None, helpText = Lang.LISTPLAYER_HELP)]
        private static void CCListPlayer(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            int resultCount = 0;

            if (args.Count > 0)
            {
                string name = args[0];
                foreach (var user in NetworkUser.readOnlyInstancesList)
                {
                    var userName = user.userName;
                    if (int.TryParse(name, out int iName) && i == iName || userName.ToUpper().Contains(name.ToUpper()))
                    {
                        sb.AppendLine($"[{i}]{userName}");
                        resultCount++;
                    }
                    i++;
                }
                if (resultCount == 0)
                {
                    sb.AppendLine($"No players found that match \"{name}\".");
                }
            }
            else
            {
                foreach (var user in NetworkUser.readOnlyInstancesList)
                {
                    sb.AppendLine($"[{i}]{user.userName}");
                    i++;

                }
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_ai", flags = ConVarFlags.None, helpText = Lang.LISTAI_HELP)]
        private static void CCListAI(ConCommandArgs args)
        {
            string langInvar;
            int i = 0;
            StringBuilder sb = new StringBuilder();
            int resultCount = 0;
            if (args.Count > 0)
            {
                string name = args[0];
                foreach (var master in MasterCatalog.allAiMasters)
                {
                    var masterName = master.name.ToUpper();
                    if (int.TryParse(name, out int iName) && i == iName || masterName.Equals(name.ToUpper().Replace("MASTER", string.Empty)) || masterName.Contains(name.ToUpper()))
                    {
                        resultCount++;
                        langInvar = StringFinder.GetLangInvar(master.bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                        sb.AppendLine($"[{i}]{master.name}={langInvar}");
                    }
                    i++;
                }
                if (resultCount == 0)
                {
                    sb.AppendLine($"No masters found that match \"{name}\".");
                }
            } else
            {
                foreach (var master in MasterCatalog.allAiMasters)
                {
                    langInvar = StringFinder.GetLangInvar(master.bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                    sb.AppendLine($"[{i}]{master.name}={langInvar}");
                    i++;
                }
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_body", flags = ConVarFlags.None, helpText = Lang.LISTBODY_HELP)]
        private static void CCListBody(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            string langInvar;
            int resultCount = 0;
            int i = 0;

            if (args.Count > 0)
            {
                string name = args[0];
                foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
                {
                    var upperBodyName = body.name.ToUpper();
                    if (int.TryParse(name, out int iName) && i == iName || upperBodyName.Equals(name.ToUpper().Replace("BODY", string.Empty)) || upperBodyName.Contains(name.ToUpper()) )
                    {
                        langInvar = StringFinder.GetLangInvar(body.baseNameToken);
                        sb.AppendLine($"[{i}]{body.name}={langInvar}");
                        resultCount++;
                    }
                    i++;
                }
                if (resultCount == 0)
                {
                    sb.AppendLine($"No bodies found that match \"{name}\".");
                }
            }
            else
            {
                foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
                {
                    langInvar = StringFinder.GetLangInvar(body.baseNameToken);
                    sb.AppendLine($"[{i}]{body.name}={langInvar}");
                    i++;
                }
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_elite", flags = ConVarFlags.None, helpText = Lang.LISTELITE_HELP)]
        private static void CCListElites(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            int resultCount = 0;
            int i = 0;

            if (args.Count > 0)
            {
                string name = args[0];
                if (int.TryParse(name, out int iName) && iName == -1 || "NONE".Contains(name.ToUpper()))
                {
                    sb.AppendLine("[-1]None");
                    resultCount++;
                }
                foreach (var elite in EliteCatalog.eliteDefs)
                {
                    var eliteName = Regex.Replace(elite.name, "^ed", "");
                    if (int.TryParse(name, out iName) && i == iName || eliteName.ToUpper().Contains(name.ToUpper()))
                    {
                        sb.AppendLine($"[{i}][{eliteName}");
                        resultCount++;
                    }
                    i++;
                }
                if (resultCount == 0)
                {
                    sb.AppendLine($"No elites found that match \"{name}\".");
                }
            }
            else
            {
                sb.AppendLine("[-1]None");
                foreach (var elite in EliteCatalog.eliteDefs)
                {
                    var eliteName = elite.name.Substring(2);
                    sb.AppendLine($"[{i}]{eliteName}");
                    i++;
                }
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_team", flags = ConVarFlags.None, helpText = Lang.LISTTEAM_HELP)]
        private static void CCListTeams(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            int resultCount = 0;
            sbyte i = 0;

            if (args.Count > 0)
            {
                string name = args[0];
                if (int.TryParse(name, out int iName) && iName == -1 || "NONE".Contains(name.ToUpper()))
                {
                    sb.AppendLine("[-1]None");
                    resultCount++;
                }
                foreach (var team in TeamCatalog.teamDefs)
                {
                    var teamName = ((TeamIndex)i).ToString();
                    if (int.TryParse(name, out iName) && i == iName || teamName.ToUpper().Contains(name.ToUpper()))
                    {
                        sb.AppendLine($"[{i}]{teamName}");
                        resultCount++;
                    }
                    i++;
                }
                if (resultCount == 0)
                {
                    sb.AppendLine($"No teams found that match \"{name}\".");
                }
            }
            else
            {
                sb.AppendLine("[-1]None");
                foreach (var team in TeamCatalog.teamDefs)
                {
                    var teamName = ((TeamIndex)i).ToString();
                    sb.AppendLine($"[{i}]{teamName}");
                    i++;
                }
            }
            Log.Message(sb);
        }

        [ConCommand(commandName = "list_directorcards", flags = ConVarFlags.None, helpText = Lang.LISTDIRECTORCARDS_HELP)]
        private static void CCListDirectorCards(ConCommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var card in StringFinder.Instance.DirectorCards)
            {
                sb.AppendLine($"{card.spawnCard.name}");
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
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
