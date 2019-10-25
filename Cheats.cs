using BepInEx;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using MiniRpcLib;

using LogLevel = RoR2Cheats.Log.LogLevel;

namespace RoR2Cheats
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("dev.wildbook.libminirpc")]
    [BepInPlugin(GUID, modname, modver)]
    public class RoR2Cheats : BaseUnityPlugin
    {
        public const string modname = "RoR2Cheats", modver = "3.1.0";
        public const string GUID = "com.harbingerofme." + modname;
        public static bool noEnemies = false;
        public static ulong seed = 0;
        public static float TickIntervalMulti = 1f;
        public static float TickRate = 1f / 60f;
        public static readonly RoR2Cheats instance;
        public static bool nextBossSet = false;
        public static string nextBossName;
        public static DirectorCard nextBoss;
        public static int nextBossCount = 1;
        public static EliteIndex nextBossElite = EliteIndex.None;
        public static float FAMCHANCE = 0.02f;

        private static MiniRpcLib.Action.IRpcAction<float> TimeScaleNetwork;

        public void Awake()
        {
            var miniRpc = MiniRpc.CreateInstance(GUID);
            new Log(Logger, miniRpc);
            TimeScaleNetwork = miniRpc.RegisterAction(Target.Client, (NetworkUser _, float f) => { HandleTimeScale(f); });
            Log.Message("Harb's and 's Version. Original by Morris1927.", LogLevel.Info, Log.Target.Bepinex);/*Check github for the other contributor, lmao*/

            Hooks.InitializeHooks();
            NetworkHandler.RegisterNetworkHandlerAttributes();
        }

        #region DEBUG
#if DEBUG
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

            Log.Message(str, LogLevel.Message, target);
        }

        [ConCommand(commandName = "getItemName", flags = ConVarFlags.None, helpText = "Match a partial localised item name to an ItemIndex")]
        private static void CCGetItemName(ConCommandArgs args)
        {
            Log.Message(Alias.Instance.GetItemFromPartial(args[0]).ToString());
        }

        [ConCommand(commandName = "getBodyName", flags = ConVarFlags.None, helpText = "Match a bpartial localised body name to a character body name")]
        private static void CCGetBodyName(ConCommandArgs args)
        {
            Log.Message(Alias.Instance.GetBodyName(args[0]));
        }

        [ConCommand(commandName = "getEquipName", flags = ConVarFlags.None, helpText = "Match a partial localised equip name to an EquipIndex")]
        private static void CCGetEquipName(ConCommandArgs args)
        {
            Log.Message(Alias.Instance.GetEquipFromPartial(args[0]).ToString());
        }

        [ConCommand(commandName = "getMasterName", flags = ConVarFlags.None, helpText = "Match a partial localised Master name to a CharacterMaster")]
        private static void CCGetMasterName(ConCommandArgs args)
        {
            Log.Message(Alias.Instance.GetMasterName(args[0]));
        }

        [ConCommand(commandName = "getTeamIndexPartial", flags = ConVarFlags.None, helpText = "Match a partial TeamIndex")]
        private static void CCGetTeamIndexPartial(ConCommandArgs args)
        {
            //Alias.Instance.GetMasterName(args[0]);
            Log.Message(Alias.GetEnumFromPartial<TeamIndex>(args[0]).ToString());
        }

        [ConCommand(commandName = "getDirectorCardPartial", flags = ConVarFlags.None, helpText = "Match a partial DirectorCard")]
        private static void CCGetDirectorCardPartial(ConCommandArgs args)
        {
            Log.Message(Alias.Instance.GetDirectorCardFromPartial(args[0]).spawnCard.prefab.name);
        }

#endif
        #endregion


        #region Items&Stats
        [ConCommand(commandName = "list_items", flags = ConVarFlags.None, helpText = "List all item names and their IDs")]
        private static void CCListItems(ConCommandArgs _)
        {
            Log.Message(MagicVars.OBSOLETEWARNING, LogLevel.Warning);
            StringBuilder text = new StringBuilder();
            foreach (ItemIndex item in ItemCatalog.allItems)
            {
                int index = (int)item;
                string invar = Alias.GetLangInvar("ITEM_" + item.ToString().ToUpper() + "_NAME");
                string line = string.Format("[{0}]{1}={2}", index, item, invar);
                text.AppendLine(line);
            }
            Log.Message(text.ToString());
        }

        [ConCommand(commandName = "list_equips", flags = ConVarFlags.None, helpText = "List all equipment items and their IDs")]
        private static void CCListEquipments(ConCommandArgs _)
        {
            Log.Message(MagicVars.OBSOLETEWARNING, LogLevel.Warning);
            StringBuilder text = new StringBuilder();
            foreach (EquipmentIndex equip in EquipmentCatalog.allEquipment)
            {
                int index = (int)equip;
                string invar = Alias.GetLangInvar("EQUIPMENT_" + equip.ToString().ToUpper() + "_NAME");
                string line = string.Format("[{0}]{1}={2}", index, equip, invar);
                text.AppendLine(line);
            }
            Log.Message(text.ToString());
        }

        [ConCommand(commandName = "list_AI", flags = ConVarFlags.None, helpText = "List all Masters and their language invariants")]
        private static void CCListAI(ConCommandArgs _)
        {
            string langInvar; string list = "";
            int i = 0;
            foreach (var master in MasterCatalog.allAiMasters)
            {
                langInvar = Alias.GetLangInvar(master.bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                list += $"[{i}]{master.name}={langInvar}\n";
                i++;
            }
            Log.Message(list.TrimEnd('\n'));
        }

        [ConCommand(commandName = "list_Body", flags = ConVarFlags.None, helpText = "List all Bodies and their language invariants")]
        private static void CCListBody(ConCommandArgs _)
        {
            string langInvar;
            int i = 0;
            string list = "";
            foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                langInvar = Alias.GetLangInvar(body.baseNameToken);
                list += $"[{i}]{body.name}={langInvar}\n";
                i++;
            }
            Log.Message(list.TrimEnd('\n'));
        }

        [ConCommand(commandName = "give_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Give item directly in the player's inventory. give_item <id> <amount> <playerid>")]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.Message(MagicVars.GIVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            int iCount = 1;
            if (args.Count >= 2)
            {
                int.TryParse(args[1], out iCount);
            }

            Inventory inventory = args.sender.master.inventory;
            if (args.Count >= 3)
            {
                NetworkUser player = GetNetUserFromString(args[2]);
                if (player == null)
                {
                    Log.Message(MagicVars.PLAYER_NOTFOUND, LogLevel.MessageClientOnly);
                }

                inventory = (player == null) ? inventory : player.master.inventory;
            }

            var item = Alias.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                inventory.GiveItem(item, iCount);
            }
            else
            {
                Log.Message(MagicVars.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }

            Log.Message(MagicVars.NOMESSAGE, args);
        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Give equipment directly to a player's inventory.")]
        private static void CCGiveEquipment(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.Message(MagicVars.GIVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            Inventory inventory = args.sender.master.inventory;
            if (args.Count >= 2)
            {
                NetworkUser player = GetNetUserFromString(args[1]);
                if (player == null)
                {
                    Log.Message(MagicVars.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }

                inventory = (player == null) ? inventory : player.master.inventory;
            }

            var equip = Alias.Instance.GetEquipFromPartial(args[0]);
            if (equip != EquipmentIndex.None)
            {
                inventory.SetEquipmentIndex(equip);
            }
            else
            {
                Log.Message(MagicVars.OBJECT_NOTFOUND + args[0] + ":" + equip, args, LogLevel.MessageClientOnly);
                return;
            }

            Log.Message(MagicVars.NOMESSAGE, args);
        }

        [ConCommand(commandName = "give_lunar", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives you the specified amount of lunar coins, value may be negative. Default 1.")]
        private static void CCGiveLunar(ConCommandArgs args)
        {
            int amount = 1;
            if (args.Count > 0)
            {
                amount = args.GetArgInt(0);
            }
            string str = "Nothing happened. Big suprise.";
            NetworkUser target = Util.LookUpBodyNetworkUser(args.senderBody);
            if (amount > 0)
            {
                target.AwardLunarCoins((uint)amount);
                str = string.Format(MagicVars.GIVELUNAR_2, "Gave", amount);
            }
            if (amount < 0)
            {
                amount *= -1;
                target.DeductLunarCoins((uint)(amount));
                str = string.Format(MagicVars.GIVELUNAR_2, "Removed", amount);
            }
            Log.Message(str, args);
        }

        [ConCommand(commandName = MagicVars.CREATEPICKUP_NAME, flags = ConVarFlags.ExecuteOnServer, helpText = MagicVars.CREATEPICKUP_ARGS)]
        private static void CCCreatePickup(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);

            Transform transform = args.senderBody.gameObject.transform;
            PickupIndex final = PickupIndex.none;
            if (args.Count == 1)
            {
                ItemIndex item;
                EquipmentIndex equipment;
                equipment = Alias.Instance.GetEquipFromPartial(args[0]);
                item = Alias.Instance.GetItemFromPartial(args[0]);
                if (item != ItemIndex.None && equipment != EquipmentIndex.None)
                {
                    Log.Message(string.Format(MagicVars.CREATEPICKUP_AMBIGIOUS_2, item, equipment), args, LogLevel.MessageClientOnly);
                    return;
                }

                if (equipment == EquipmentIndex.None && item != ItemIndex.None)
                {
                    final = PickupCatalog.FindPickupIndex(item);
                }
                if (item == ItemIndex.None && equipment != EquipmentIndex.None)
                {
                    final = PickupCatalog.FindPickupIndex(equipment);
                }

                if (item == ItemIndex.None && equipment == EquipmentIndex.None)
                {
                    if (args[0].ToUpper().Contains("COIN"))
                    {
                        final = PickupCatalog.FindPickupIndex("LunarCoin.Coin0");
                    }
                    else
                    {
                        Log.Message(MagicVars.CREATEPICKUP_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                }
            }
            else
            {
                if (args[0].Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    ItemIndex itemName = Alias.Instance.GetItemFromPartial(args[1]);
                    if (itemName == ItemIndex.None)
                    {
                        Log.Message(MagicVars.CREATEPICKUP_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                    final = PickupCatalog.FindPickupIndex(itemName);
                }
                if (args[0].ToUpper().StartsWith("EQUIP"))
                {
                    EquipmentIndex equipName = Alias.Instance.GetEquipFromPartial(args[0]);
                    if (equipName == EquipmentIndex.None)
                    {
                        Log.Message(MagicVars.CREATEPICKUP_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                    final = PickupCatalog.FindPickupIndex(equipName);
                }
            }
            Log.Message(string.Format(MagicVars.CREATEPICKUP_SUCCES_1, final), args);
            PickupDropletController.CreatePickupDroplet(final, transform.position, transform.forward * 40f);
        }

        [ConCommand(commandName = "remove_item", flags = ConVarFlags.ExecuteOnServer, helpText = MagicVars.REMOVEITEM_ARGS)]
        private static void CCRemoveItem(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.Message(MagicVars.REMOVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            int iCount = 1;
            if (args.Count >= 2)
            {
                int.TryParse(args[1], out iCount);
            }

            Inventory inventory = args.sender.master.inventory;
            if (args.Count >= 3)
            {
                NetworkUser player = GetNetUserFromString(args[2]);
                if (player == null)
                {
                    Log.Message(MagicVars.PLAYER_NOTFOUND, LogLevel.MessageClientOnly);
                }

                inventory = (player == null) ? inventory : player.master.inventory;
            }

            if (args[0].ToUpper() == MagicVars.ALL)
            {
                Log.Message("Removing inventory", args);
                inventory.CopyItemsFrom(new GameObject().AddComponent<Inventory>());
                return;
            }
            var item = Alias.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                if (args[1].ToUpper() == MagicVars.ALL)
                {
                    iCount = inventory.GetItemCount(item);
                }
                inventory.RemoveItem(item, iCount);
            }
            else
            {
                Log.Message(MagicVars.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }

            Log.Message(MagicVars.NOMESSAGE, args);
        }

        [ConCommand(commandName = "remove_equip", flags = ConVarFlags.ExecuteOnServer, helpText = MagicVars.REMOVEEQUIP_ARGS)]
        private static void CCRemoveEquipment(ConCommandArgs args)
        {
            Inventory inventory = args.sender.master.inventory;
            if (args.Count >= 1)
            {
                NetworkUser player = GetNetUserFromString(args[0]);
                if (player == null)
                {
                    Log.Message(MagicVars.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                inventory = (player == null) ? inventory : player.master.inventory;
            }
            inventory.SetEquipmentIndex(EquipmentIndex.None);

            Log.Message(MagicVars.NOMESSAGE, args);
        }

        [ConCommand(commandName = "give_money", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives money")]
        private static void CCGiveMoney(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out uint result))
            {
                return;
            }

            if (args.Count < 2 || args[1].ToLower() != "all")
            {
                CharacterMaster master = args.sender.master;
                if (args.Count >= 2)
                {
                    NetworkUser player = GetNetUserFromString(args[1]);
                    if (player != null)
                    {
                        master = player.master;
                    }
                }
                master.GiveMoney(result);
            }
            else
            {
                TeamManager.instance.GiveTeamMoney(args.sender.master.teamIndex, result);
            }
            Log.Message("$$$", args);
        }

        [ConCommand(commandName = "give_exp", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives experience. OBSOLETE")]
        private static void CCGiveExperience(ConCommandArgs args)
        {
            Log.Message(MagicVars.OBSOLETEWARNING + "Use team_set_level instead.", args, LogLevel.WarningClientOnly);

            if (args.Count == 0)
            {
                return;
            }

            if (TeamManager.instance && uint.TryParse(args[0], out uint result))
            {
                TeamManager.instance.GiveTeamExperience(args.sender.master.teamIndex, result);
            }
        }
        #endregion

        #region Run.instance
        private static void HandleTimeScale(float newTimeScale)
        {
            Time.timeScale = newTimeScale;
            Log.Message("Network request for timescale.");
        }

        [ConCommand(commandName = "list_family", flags = ConVarFlags.ExecuteOnServer, helpText = "Calls a family event in the next instance.")]
        private static void CCListFamily(ConCommandArgs args)
        {
            StringBuilder s = new StringBuilder();
            foreach (ClassicStageInfo.MonsterFamily family in ClassicStageInfo.instance.possibleMonsterFamilies)
            {
                s.AppendLine(family.familySelectionChatString);
            }
            Log.Message(s.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "family_event", flags = ConVarFlags.ExecuteOnServer, helpText = "Calls a family event in the next stage.")]
        private static void CCFamilyEvent(ConCommandArgs args)
        {
            FAMCHANCE = 1.0f;
            Log.Message("The next stage will contain a family event!", args);
        }

        [ConCommand(commandName = "next_boss", flags = ConVarFlags.ExecuteOnServer, helpText = MagicVars.NEXTBOSS_ARGS)]
        private static void CCNextBoss(ConCommandArgs args)
        {
            Log.Message(MagicVars.PARTIAL_IMPLEMENTATION, args, LogLevel.MessageClientOnly);
            if (args.Count == 0)
            {
                Log.Message(MagicVars.NEXTBOSS_ARGS, args);
            }
            StringBuilder s = new StringBuilder();
            if (args.Count >= 1)
            {
                try
                {
                    nextBoss = Alias.Instance.GetDirectorCardFromPartial(args[0]);
                    s.AppendLine($"Next boss is: {nextBoss.spawnCard.name}. ");
                    if (args.Count >= 2)
                    {
                        if (!int.TryParse(args[1], out nextBossCount))
                        {
                            Log.Message(MagicVars.COUNTISNUMERIC, args, LogLevel.MessageClientOnly);
                            nextBossCount = 1;
                        }
                        else
                        {
                            if (nextBossCount > 6)
                            {
                                nextBossCount = 6;
                            }
                            else if (nextBossCount <= 0)
                            {
                                nextBossCount = 1;
                            }
                            s.Append($"Count:  {nextBossCount}. ");
                            if (args.Count >= 3)
                            {
                                nextBossElite = Alias.GetEnumFromPartial<EliteIndex>(args[2]);
                                s.Append("Elite: " + nextBossElite.ToString());
                            }
                        }
                    }
                    nextBossSet = true;
                    Log.Message(s.ToString(), args);
                }
                catch (Exception ex)
                {
                    Log.Message(MagicVars.OBJECT_NOTFOUND + args[0], args, LogLevel.ErrorClientOnly);
                    Log.Message(ex.ToString(), args, LogLevel.ErrorClientOnly);
                }
            }
        }

        [ConCommand(commandName = "next_stage", flags = ConVarFlags.ExecuteOnServer, helpText = "Start next round. Additional args for specific scene.")]
        private static void CCNextStage(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Run.instance.AdvanceStage(Run.instance.nextStageScene);
                Log.Message("Stage advanced.", args);
                return;
            }

            string stageString = args[0];
            List<string> array = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                array.Add(SceneUtility.GetScenePathByBuildIndex(i).Replace("Assets/RoR2/Scenes/", "").Replace(".unity", ""));
            }

            if (array.Contains(stageString))
            {
                Run.instance.AdvanceStage(SceneCatalog.GetSceneDefFromSceneName(stageString));
                Log.Message($"Stage advanced to {stageString}.", args);
                return;
            }
            else
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine(MagicVars.NEXTROUND_STAGE);
                array.ForEach((string str) => { s.AppendLine(str); });
                Log.Message(s.ToString(), args);
            }
        }

        [ConCommand(commandName = "seed", flags = ConVarFlags.ExecuteOnServer, helpText = "Gets/Sets seed.")]
        private static void CCUseSeed(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                string s = "Current Seed is ";
                if (PreGameController.instance)
                {
                    s += PreGameController.instance.runSeed;
                }
                else
                {
                    s += (seed == 0) ? "random" : seed.ToString();
                }
                Log.Message(s, args, LogLevel.MessageClientOnly);
                return;
            }
            args.CheckArgumentCount(1);
            if (!TextSerialization.TryParseInvariant(args[0], out ulong result))
            {
                throw new ConCommandException("Specified seed is not a parsable uint64.");
            }

            if (PreGameController.instance)
            {
                PreGameController.instance.runSeed = (result == 0) ? RoR2Application.rng.nextUlong : result;
            }
            seed = result;
            Log.Message($"Seed set to {((seed == 0) ? "vanilla generation" : seed.ToString())}.", args);
        }

        [ConCommand(commandName = "fixed_time", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets fixed time - Affects monster difficulty")]
        private static void CCSetTime(ConCommandArgs args)
        {

            if (args.Count == 0)
            {
                Log.Message(Run.instance.fixedTime.ToString(), args, LogLevel.MessageClientOnly);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float setTime))
            {
                Run.instance.fixedTime = setTime;
                ResetEnemyTeamLevel();
                Log.Message("Fixed_time set to " + setTime, args);
            }
            else
            {
                Log.Message("Incorrect arguments. Try: fixed_time 600", args, LogLevel.MessageClientOnly);
            }

        }

        [ConCommand(commandName = "time_scale", flags = ConVarFlags.Engine | ConVarFlags.ExecuteOnServer, helpText = "Time scale")]
        private static void CCTimeScale(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.Message(Time.timeScale, LogLevel.MessageClientOnly);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float scale))
            {
                Time.timeScale = scale;
                Log.Message("Time scale set to " + scale, args);
                TimeScaleNetwork.Invoke(scale);
            }
            else
            {
                Log.Message("Incorrect arguments. Try: time_scale 0.5", LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "stage_clear_count", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets stage clear count - Affects monster difficulty. OBSOLETE")]
        private static void CCSetClearCount(ConCommandArgs args)
        {
            Log.Message(MagicVars.OBSOLETEWARNING + "Use run_set_stages_cleared instead.", args, LogLevel.WarningClientOnly);

            if (args.Count == 0)
            {
                Log.Message(Run.instance.stageClearCount.ToString(), args, LogLevel.MessageClientOnly);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out int setClearCount))
            {
                Run.instance.stageClearCount = setClearCount;
                ResetEnemyTeamLevel();
                Log.Message("Stage_clear_count set to " + setClearCount, args);
            }
            else
            {
                Log.Message("Incorrect arguments. Try: stage_clear_count 5", args, LogLevel.MessageClientOnly);
            }

        }
        #endregion

        #region Entities
        private static NetworkUser GetNetUserFromString(string playerString)
        {
            if (playerString != "")
            {
                if (int.TryParse(playerString, out int result))
                {
                    if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0)
                    {
                        return NetworkUser.readOnlyInstancesList[result];
                    }
                    Log.Message(MagicVars.PLAYER_NOTFOUND);
                    return null;
                }
                else
                {
                    foreach (NetworkUser n in NetworkUser.readOnlyInstancesList)
                    {
                        if (n.userName.Equals(playerString, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return n;
                        }
                    }
                    Log.Message(MagicVars.PLAYER_NOTFOUND);
                    return null;
                }
            }
            return null;
        }

        [ConCommand(commandName = "player_list", flags = ConVarFlags.ExecuteOnServer, helpText = "Shows list of players with their ID")]
        private static void CCPlayerList(ConCommandArgs args)
        {
            NetworkUser n;
            StringBuilder list = new StringBuilder();
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
            {
                n = NetworkUser.readOnlyInstancesList[i];
                list.AppendLine($"[{i}]{n.userName}");

            }
            Log.Message(list.ToString(), args, LogLevel.MessageClientOnly);
        }

        private static void ResetEnemyTeamLevel()
        {
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1);
        }

        [ConCommand(commandName = "god", flags = ConVarFlags.ExecuteOnServer, helpText = "Godmode")]
        private static void CCGodModeToggle(ConCommandArgs args)
        {
            var godToggleMethod = typeof(CharacterMaster).GetMethodCached("ToggleGod");
            bool hasNotYetRun = true;
            foreach (var playerInstance in PlayerCharacterMasterController.instances)
            {
                godToggleMethod.Invoke(playerInstance.master, null);
                if (hasNotYetRun)
                {
                    Log.Message($"God mode {(playerInstance.master.GetBody().healthComponent.godMode ? "enabled" : "disabled")}.", args);
                    hasNotYetRun = false;
                }
            }
        }

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = "Kill all members of a team. Default is monster.")]
        private static void CCKillAll(ConCommandArgs args)
        {
            TeamIndex team;
            if (args.Count == 0)
            {
                team = TeamIndex.Monster;
            }
            else
            {
                team = Alias.GetEnumFromPartial<TeamIndex>(args[0]);
            }

            int count = 0;

            foreach (CharacterMaster cm in FindObjectsOfType<CharacterMaster>())
            {
                if (cm.teamIndex == team)
                {
                    CharacterBody cb = cm.GetBody();
                    if (cb)
                    {
                        if (cb.healthComponent)
                        {
                            cb.healthComponent.Suicide(null);
                            count++;
                        }
                    }

                }
            }
            Log.Message("Killed " + count + " of team " + team + ".", args);
        }

        [ConCommand(commandName = "true_kill", flags = ConVarFlags.ExecuteOnServer, helpText = "Truly kill a player, ignoring revival effects")]
        private static void CCTrueKill(ConCommandArgs args)
        {
            CharacterMaster master = args.sender.master;
            if (args.Count > 0)
            {
                NetworkUser player = GetNetUserFromString(args[0]);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.Message(MagicVars.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            master.TrueKill();
            Log.Message(master.name + " was killed by server.", args);
        }

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = "Toggles enemy spawns")]
        private static void CCNoEnemies(ConCommandArgs args)
        {
            noEnemies = (noEnemies) ? false : true;
            typeof(CombatDirector).GetFieldValue<RoR2.ConVar.BoolConVar>("cvDirectorCombatDisable").SetBool(noEnemies);
            if (noEnemies)
            {
                SceneDirector.onPrePopulateSceneServer += Hooks.OnPrePopulateSetMonsterCreditZero;
            }
            else
            {
                SceneDirector.onPrePopulateSceneServer -= Hooks.OnPrePopulateSetMonsterCreditZero;
            }
            Log.Message("No_enemies set to " + noEnemies, args);
        }

        [ConCommand(commandName = "spawn_as", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawn as a new character. Type body_list for a full list of characters")]
        private static void CCSpawnAs(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.Message(MagicVars.SPAWNAS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            string character = Alias.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Log.Message(MagicVars.SPAWN_ERROR + args[0], args, LogLevel.MessageClientOnly);
                Log.Message("Please use list_body to print CharacterBodies", args, LogLevel.MessageClientOnly);
                return;
            }
            GameObject newBody = BodyCatalog.FindBodyPrefab(character);

            CharacterMaster master = args.sender.master;
            if (args.Count > 1)
            {
                NetworkUser player = GetNetUserFromString(args[1]);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.Message(MagicVars.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            if (!master.alive)
            {
                Log.Message(MagicVars.PLAYER_DEADRESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }

            master.bodyPrefab = newBody;
            Log.Message(args.sender.userName + " is spawning as " + character, args);
            master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
        }

        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawn an AI")]
        private static void CCSpawnAI(ConCommandArgs args)
        {

            if (args.Count == 0)
            {
                Log.Message(MagicVars.SPAWNAI_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            string character = Alias.Instance.GetMasterName(args[0]);
            if (character == null)
            {
                Log.Message(MagicVars.SPAWN_ERROR + character, args, LogLevel.MessageClientOnly);
                return;
            }
            var masterprefab = MasterCatalog.FindMasterPrefab(character);
            var body = masterprefab.GetComponent<CharacterMaster>().bodyPrefab;

            var bodyGameObject = Instantiate<GameObject>(masterprefab, args.sender.master.GetBody().transform.position, Quaternion.identity);
            CharacterMaster master = bodyGameObject.GetComponent<CharacterMaster>();
            NetworkServer.Spawn(bodyGameObject);
            master.SpawnBody(body, args.sender.master.GetBody().transform.position, Quaternion.identity);

            if (args.Count > 1)
            {
                var eliteIndex = Alias.GetEnumFromPartial<EliteIndex>(args[1]);
                master.inventory.SetEquipmentIndex(EliteCatalog.GetEliteDef(eliteIndex).eliteEquipmentIndex);
                master.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((GetTierDef(eliteIndex).healthBoostCoefficient - 1) * 10));
                master.inventory.GiveItem(ItemIndex.BoostDamage, Mathf.RoundToInt((GetTierDef(eliteIndex).damageBoostCoefficient - 1) * 10));
            }

            if (args.Count > 2 && Enum.TryParse<TeamIndex>(Alias.GetEnumFromPartial<TeamIndex>(args[2]).ToString(), true, out TeamIndex teamIndex))
            {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count)
                {
                    master.teamIndex = teamIndex;
                }
            }

            if (args.Count > 3 && bool.TryParse(args[3], out bool braindead) && braindead)
            {
                Destroy(master.GetComponent<BaseAI>());
            }
            Log.Message(MagicVars.SPAWN_ATTEMPT + character, args);
        }

        [ConCommand(commandName = "spawn_body", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns a CharacterBody")]
        private static void CCSpawnBody(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.Message(MagicVars.SPAWNBODY_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            string character = Alias.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Log.Message(string.Format(MagicVars.SPAWN_ERROR, args[0]), args, LogLevel.MessageClientOnly);
                return;
            }

            GameObject body = BodyCatalog.FindBodyPrefab(character);
            GameObject gameObject = Instantiate<GameObject>(body, args.sender.master.GetBody().transform.position, Quaternion.identity);
            NetworkServer.Spawn(gameObject);
            Log.Message(MagicVars.SPAWN_ATTEMPT + character, args);
        }

        [ConCommand(commandName = "respawn", flags = ConVarFlags.ExecuteOnServer, helpText = "Respawn a player at the map spawnpoint.")]
        private static void RespawnPlayer(ConCommandArgs args)
        {
            CharacterMaster master = args.sender.master;
            if (args.Count > 0)
            {
                NetworkUser player = GetNetUserFromString(args[0]);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.Message(MagicVars.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            Transform spawnPoint = Stage.instance.GetPlayerSpawnTransform();
            master.Respawn(spawnPoint.position, spawnPoint.rotation, false);
            Log.Message(MagicVars.SPAWN_ATTEMPT + master.name, args);
        }

        [ConCommand(commandName = "change_team", flags = ConVarFlags.ExecuteOnServer, helpText = "Change team to Neutral, Player or Monster (0, 1, 2)")]
        private static void CCChangeTeam(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.Message(MagicVars.TEAM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            CharacterMaster master = args.sender.master;
            if (args.Count > 1)
            {
                NetworkUser player = GetNetUserFromString(args[1]);
                if (player != null)
                {
                    master = player.master;
                }
            }

            if (Enum.TryParse(Alias.GetEnumFromPartial<TeamIndex>(args[0]).ToString(), true, out TeamIndex teamIndex))
            {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count)
                {
                    if (master.GetBody())
                    {
                        master.GetBody().teamComponent.teamIndex = teamIndex;
                        master.teamIndex = teamIndex;
                        Log.Message("Changed to team " + teamIndex, args);
                        return;
                    }
                }
            }
            //Note the `return` on succesful evaluation.
            Log.Message("Invalid team. Please use 0,'neutral',1,'player',2, or 'monster'", args, LogLevel.MessageClientOnly);

        }

        [ConCommand(commandName = "add_portal", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a blue, gold, or celestial portal")]
        private static void CCAddPortal(ConCommandArgs args)
        {
            if (TeleporterInteraction.instance)
            {
                switch (args[0].ToLower())
                {
                    case "blue":
                        TeleporterInteraction.instance.Network_shouldAttemptToSpawnShopPortal = true;
                        break;
                    case "gold":
                        TeleporterInteraction.instance.Network_shouldAttemptToSpawnGoldshoresPortal = true;
                        break;
                    case "celestial":
                        TeleporterInteraction.instance.Network_shouldAttemptToSpawnMSPortal = true;
                        break;
                    default:
                        Log.Message(MagicVars.PORTAL_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                }
                //Note the return on default.
                ChatMessage.SendColored($"A {args[0].ToLower()} orb spawns.", ColorCatalog.ColorIndex.Teleporter);
            }
            else
            {
                Log.Message("No teleporter instance!", args, LogLevel.WarningClientOnly);
            }
        }

        public static CombatDirector.EliteTierDef GetTierDef(EliteIndex index)
        {
            int tier = 0;
            CombatDirector.EliteTierDef[] tierdefs = typeof(CombatDirector).GetFieldValue<CombatDirector.EliteTierDef[]>("eliteTiers");
            if ((int)index > (int)EliteIndex.None && (int)index < (int)EliteIndex.Count)
            {
                for (int i = 0; i < tierdefs.Length; i++)
                {
                    for (int j = 0; j < tierdefs[i].eliteTypes.Length; j++)
                    {
                        if (tierdefs[i].eliteTypes[j] == (index)) { tier = i; }
                    }
                }
            }
            return tierdefs[tier];
        }

        public static void ResetNextBoss()
        {
            nextBossSet = false;
            nextBossCount = 1;
            nextBossElite = EliteIndex.None;
        }
        #endregion

        /// <summary>
        /// Required for automated manifest building.
        /// </summary>
        /// <returns>Returns the TS manifest Version</returns>
        public static string GetModVer()
        {
            return modver;
        }
    }
}
