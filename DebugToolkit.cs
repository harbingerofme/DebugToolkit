using BepInEx;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using MiniRpcLib;
using RoR2.Networking;
using LogLevel = DebugToolkit.Log.LogLevel;

namespace DebugToolkit
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("dev.wildbook.libminirpc")]
    [BepInPlugin(GUID, modname, modver)]
    public class DebugToolkit : BaseUnityPlugin
    {
        public const string modname = "DebugToolkit", modver = "3.2.0";
        public const string GUID = "com.harbingerofme." + modname;

        internal static ConfigFile Configuration;

        internal static bool noEnemies = false;
        internal static ulong seed = 0;
        internal static DirectorCard nextBoss;
        internal static int nextBossCount = 1;
        internal static EliteIndex nextBossElite = EliteIndex.None;

        private static MiniRpcLib.Action.IRpcAction<float> TimeScaleNetwork;

        private void Awake()
        {
            Configuration = base.Config;

            var miniRpc = MiniRpc.CreateInstance(GUID);
            new Log(Logger, miniRpc);

#if DEBUG   //Additional references in this block must be fully qualifed as to not use them in Release Builds.
            string gitVersion = "";
            using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"{this.GetType().Namespace}.CurrentCommit"))
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                gitVersion= reader.ReadToEnd();
            }
            Log.MessageWarning($"This is an experimental  build! Commit: {gitVersion.Trim()}");
#endif

            Log.Message("Created by Harb, iDeathHD and . Based on RoR2Cheats by Morris1927.", LogLevel.Info, Log.Target.Bepinex);

            PermissionSystem.Init();
            Hooks.InitializeHooks();
            Command_Noclip.InitRPC(miniRpc);
            Command_Teleport.InitRPC(miniRpc);
            TimeScaleNetwork = miniRpc.RegisterAction(Target.Client, (NetworkUser _, float f) => { HandleTimeScale(f); });
        }

        private void Start()
        {
            var _ = StringFinder.Instance;
            ArgsAutoCompletion.GatherCommandsAndFillStaticArgs();
        }

        private void Update()
        {
            if (Run.instance && Command_Noclip.IsActivated)
            {
                Command_Noclip.Update();
            }
        }

        [ConCommand(commandName = "reload_all_config", flags = ConVarFlags.None, helpText = "Reload all default config files from all loaded plugins.")]
        private static void CCReloadAllConfig(ConCommandArgs args)
        {
            foreach (var pluginInfo in Chainloader.PluginInfos.Values)
            {
                try
                {
                    pluginInfo.Instance.Config?.Reload();
                }
                catch (Exception e)
                {
                    // exception if the config file of that plugin doesnt exist. Also, can't reload a plugins config if it has a custom name. 
                }
            }
        }

#region Items&Stats
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

        [ConCommand(commandName = "give_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives the specified item to the player in the specified quantity. " + Lang.GIVEITEM_ARGS)]
        [AutoCompletion(typeof(ItemCatalog), "itemDefs", "nameToken")]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                if (args.Count != 3)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    Log.Message(Lang.GIVEITEM_ARGS, LogLevel.Message);
                    return;
                }
            }

            NetworkUser player = args.sender;
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.GIVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            int iCount = 1;
            if (args.Count >= 2)
            {
                int.TryParse(args[1], out iCount);
            }

            Inventory inventory = args.sender?.master.inventory;
            if (args.Count >= 3)
            {
                player = Util.GetNetUserFromString(args.userArgs, 2);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    if(args.sender == null)
                    {
                        return;
                    }
                }

                inventory = (player == null) ? inventory : player.master.inventory;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                inventory?.GiveItem(item, iCount);
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }

            Log.MessageNetworked($"Gave {iCount} {item} to {player.masterController.GetDisplayName()}", args);
        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives the specified item to the player. " + Lang.GIVEEQUIP_ARGS)]
        [AutoCompletion(typeof(EquipmentCatalog), "equipmentDefs", "nameToken")]
        private static void CCGiveEquipment(ConCommandArgs args)
        {
            NetworkUser player = args.sender;
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.GIVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                if (args.Count < 2)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }

            Inventory inventory = args.sender?.master.inventory;
            if (args.Count >= 2)
            {
                player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    if (args.sender == null)
                    {
                        return;
                    }
                }
                inventory = (player == null) ? inventory : player.master.inventory;
            }

            var equip = StringFinder.Instance.GetEquipFromPartial(args[0]);
            if (equip != EquipmentIndex.None)
            {
                inventory?.SetEquipmentIndex(equip);
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + equip, args, LogLevel.MessageClientOnly);
                return;
            }

            Log.MessageNetworked($"Gave {equip} to {player.masterController.GetDisplayName()}", args);
        }

        [ConCommand(commandName = "give_lunar", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives a lunar coin to you. " + Lang.GIVELUNAR_ARGS)]
        private static void CCGiveLunar(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.Message("Can't modify Lunar coins of other users directly.", LogLevel.Error);
                return;
            }
            int amount = 1;
            if (args.Count > 0)
            {
                amount = args.GetArgInt(0);
            }
            string str = "Nothing happened. Big suprise.";
            NetworkUser target = RoR2.Util.LookUpBodyNetworkUser(args.senderBody);
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

        [ConCommand(commandName = "create_pickup", flags = ConVarFlags.ExecuteOnServer, helpText = "Creates a PickupDroplet infront of your position. " + Lang.CREATEPICKUP_ARGS)]
        private static void CCCreatePickup(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
            if(args.sender == null)
            {
                if (args.Count <= 3)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }
            NetworkUser player = args.sender;
            if (args.Count >= 3)
            {
                player = Util.GetNetUserFromString(args.userArgs, 2);
                if (player == null)
                {
                    Log.Message(Lang.PLAYER_NOTFOUND, LogLevel.MessageClientOnly);
                    if (args.sender == null)
                    {
                        return;
                    }
                    player = args.sender;
                }
            }
            Transform transform = player.GetCurrentBody().gameObject.transform;

            bool searchEquip = true, searchItem = true;
            if (args.Count == 2)
            {
                if (args[1].Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    searchEquip = false;
                }
                if (args[1].ToUpper().StartsWith("EQUIP"))
                {
                    searchItem = false;
                }
            }
            PickupIndex final = PickupIndex.none;
            EquipmentIndex equipment = EquipmentIndex.None;
            ItemIndex item = ItemIndex.None;

            if (searchEquip)
            {
                equipment = StringFinder.Instance.GetEquipFromPartial(args[0]);
                final = PickupCatalog.FindPickupIndex(equipment);
            }
            if (searchItem)
            {
                item = StringFinder.Instance.GetItemFromPartial(args[0]);
                final = PickupCatalog.FindPickupIndex(item);
            }
            if (item != ItemIndex.None && equipment != EquipmentIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_AMBIGIOUS_2, item, equipment), args, LogLevel.MessageClientOnly);
                return;
            }

            if (item == ItemIndex.None && equipment == EquipmentIndex.None)
            {
                if (args[0].ToUpper().Contains("COIN"))
                {
                    final = PickupCatalog.FindPickupIndex("LunarCoin.Coin0");
                }
                else
                {
                    Log.MessageNetworked(Lang.CREATEPICKUP_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCES_1, final), args);
            PickupDropletController.CreatePickupDroplet(final, transform.position, transform.forward * 40f);
        }

        [ConCommand(commandName = "remove_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes the specified quantities of an item from a player. " + Lang.REMOVEITEM_ARGS)]
        private static void CCRemoveItem(ConCommandArgs args)
        {
            NetworkUser player = args.sender;
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.REMOVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                if (args.Count < 3)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }
            int iCount = 1;
            if (args.Count >= 2)
            {
                int.TryParse(args[1], out iCount);
            }

            Inventory inventory = args.sender?.master.inventory;
            if (args.Count >= 3)
            {
                player = Util.GetNetUserFromString(args.userArgs, 2);
                if (player == null)
                {
                    Log.Message(Lang.PLAYER_NOTFOUND, LogLevel.MessageClientOnly);
                    if (args.sender == null)
                    {
                        return;
                    }
                }

                inventory = (player == null) ? inventory : player.master.inventory;
            }

            if (args[0].ToUpper() == Lang.ALL)
            {
                
                if (inventory)
                {
                    inventory.CopyItemsFrom(new GameObject().AddComponent<Inventory>());
                    Log.MessageNetworked("Removing inventory", args);
                }

                return;
            }
            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                if (args[1].ToUpper() == Lang.ALL)
                {
                    if (inventory != null)
                    {
                        iCount = inventory.GetItemCount(item);
                    }
                }

                if (inventory != null)
                {
                    inventory.RemoveItem(item, iCount);
                }
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }

            if (player)
            {
                Log.MessageNetworked($"Removed {iCount} {item} from {player.masterController.GetDisplayName()}", args);
            }
        }

        [ConCommand(commandName = "remove_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes the equipment from the specified player. " + Lang.REMOVEEQUIP_ARGS)]
        private static void CCRemoveEquipment(ConCommandArgs args)
        {
            NetworkUser player = args.sender;
            Inventory inventory = player.master.inventory;
            if (args.Count >= 1)
            {
                player = Util.GetNetUserFromString(args.userArgs);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }

                inventory = player.master.inventory;
            }
            inventory.SetEquipmentIndex(EquipmentIndex.None);

            Log.MessageNetworked($"Removed current Equipment from {player.masterController.GetDisplayName()}", args);
        }

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
#endregion

#region Run.instance
        private static void HandleTimeScale(float newTimeScale)
        {
            Time.timeScale = newTimeScale;
            Log.Message("Timescale set to: " + newTimeScale + ". This message may appear twice if you are the host.");
        }

        [ConCommand(commandName = "kick", flags = ConVarFlags.ExecuteOnServer, helpText = "Kicks the specified player from the session. " + Lang.KICK_ARGS)]
        [RequiredPermissionLevel]
        private static void CCKick(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.KICK_ARGS, args, LogLevel.Error);
                return;
            }
            try
            {
                NetworkUser nu = Util.GetNetUserFromString(args.userArgs);

                // Check if we can kick targeted user.
                if (!Application.isBatchMode)
                {
                    foreach (var serverLocalUsers in NetworkUser.readOnlyLocalPlayersList)
                    {
                        if (serverLocalUsers == nu)
                        {
                            Log.MessageNetworked("Specified user is hosting.", args, LogLevel.Error);
                            return;
                        }
                    }
                }
                else if (PermissionSystem.IsEnabled.Value)
                {
                    if (!PermissionSystem.HasMorePerm(args.sender, nu, args))
                    {
                        return;
                    }
                }

                NetworkConnection client = null;
                foreach (var connection in NetworkServer.connections)
                {
                    if (nu.connectionToClient == connection)
                    {
                        client = connection;
                        break;
                    }
                }

                if (client != null)
                {
                    GameNetworkManager.singleton.InvokeMethod("ServerKickClient", client, GameNetworkManager.KickReason.Kick);
                }
                else
                {
                    Log.MessageNetworked("Error trying to find the associated connection with the user", args, LogLevel.Error);
                }
            }
            catch
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.Error);
            }
        }

        [ConCommand(commandName = "ban", flags = ConVarFlags.ExecuteOnServer, helpText = "Bans the specified player from the session. " + Lang.BAN_ARGS)]
        [RequiredPermissionLevel]
        private static void CCBan(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.BAN_ARGS, args, LogLevel.Error);
                return;
            }
            try
            {
                NetworkUser nu = Util.GetNetUserFromString(args.userArgs);
                
                // Check if we can kick targeted user.
                if (!Application.isBatchMode)
                {
                    foreach (var serverLocalUsers in NetworkUser.readOnlyLocalPlayersList)
                    {
                        if (serverLocalUsers == nu)
                        {
                            Log.MessageNetworked("Specified user is hosting.", args, LogLevel.Error);
                            return;
                        }
                    }
                }
                else if (PermissionSystem.IsEnabled.Value)
                {
                    if (!PermissionSystem.HasMorePerm(args.sender, nu, args))
                    {
                        return;
                    }
                }

                NetworkConnection client = null;
                foreach (var connection in NetworkServer.connections)
                {
                    if (nu.connectionToClient == connection)
                    {
                        client = connection;
                        break;
                    }
                }

                if (client != null)
                {
                    GameNetworkManager.singleton.InvokeMethod("ServerKickClient", client, GameNetworkManager.KickReason.Ban);
                }
                else
                {
                    Log.MessageNetworked("Error trying to find the associated connection with the user", args, LogLevel.Error);
                }
            }
            catch
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.Error);
            }
        }

        [ConCommand(commandName = "force_family_event", flags = ConVarFlags.ExecuteOnServer, helpText = "Forces a family event to occur during the next stage. " + Lang.FAMILYEVENT_ARGS)]
        private static void CCFamilyEvent(ConCommandArgs args)
        {
            IL.RoR2.ClassicStageInfo.Awake += Hooks.ForceFamilyEvent;
            On.RoR2.Stage.Start += Hooks.RemoveFamilyEvent;
            Log.MessageNetworked("The next stage will contain a family event!", args);
        }

        [ConCommand(commandName = "next_boss", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets the next teleporter instance to the specified boss. " + Lang.NEXTBOSS_ARGS)]
        [AutoCompletion(typeof(StringFinder), "characterSpawnCard", "spawnCard/prefab")]
        private static void CCNextBoss(ConCommandArgs args)
        {
            //Log.MessageNetworked("This feature is currently not working. We'll hopefully provide an update to this soon.", args);
            //return;
            Log.MessageNetworked(Lang.PARTIALIMPLEMENTATION_WARNING, args, LogLevel.MessageClientOnly);
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.NEXTBOSS_ARGS, args);
            }
            StringBuilder s = new StringBuilder();
            if (args.Count >= 1)
            {
                try
                {
                    nextBoss = StringFinder.Instance.GetDirectorCardFromPartial(args[0]);
                    s.AppendLine($"Next boss is: {nextBoss.spawnCard.name}. ");
                    if (args.Count >= 2)
                    {
                        if (!int.TryParse(args[1], out nextBossCount))
                        {
                            Log.MessageNetworked(Lang.COUNTISNUMERIC, args, LogLevel.MessageClientOnly);
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
                                nextBossElite = StringFinder.GetEnumFromPartial<EliteIndex>(args[2]);
                                s.Append("Elite: " + nextBossElite.ToString());
                            }
                        }
                    }
                    On.RoR2.CombatDirector.SetNextSpawnAsBoss += Hooks.CombatDirector_SetNextSpawnAsBoss;
                    Log.MessageNetworked(s.ToString(), args);
                }
                catch (Exception ex)
                {
                    Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0], args, LogLevel.ErrorClientOnly);
                    Log.MessageNetworked(ex.ToString(), args, LogLevel.ErrorClientOnly);
                }
            }
        }

        [ConCommand(commandName = "next_stage", flags = ConVarFlags.ExecuteOnServer, helpText = "Forces a stage change to the specified stage. " + Lang.NEXTSTAGE_ARGS)]
        private static void CCNextStage(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Run.instance.AdvanceStage(Run.instance.nextStageScene);
                Log.MessageNetworked("Stage advanced.", args);
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
                Log.MessageNetworked($"Stage advanced to {stageString}.", args);
                return;
            }
            else
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine(Lang.NEXTROUND_STAGE);
                array.ForEach((string str) => { s.AppendLine(str); });
                Log.MessageNetworked(s.ToString(), args);
            }
        }

        [ConCommand(commandName = "seed", flags = ConVarFlags.ExecuteOnServer, helpText = "Gets/Sets the game seed until game close. Use 0 to reset to vanilla generation. " + Lang.SEED_ARGS)]
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
                Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
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
            if (seed == 0 && result != 0)
            {
                On.RoR2.PreGameController.Awake += Hooks.SeedHook;
            }
            else
            {
                if (seed != 0 && result == 0)
                {
                    On.RoR2.PreGameController.Awake -= Hooks.SeedHook;
                }
            }
            seed = result;
            Log.MessageNetworked($"Seed set to {((seed == 0) ? "vanilla generation" : seed.ToString())}.", args);
        }

        [ConCommand(commandName = "fixed_time", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets the FixedTime to the specified value. " + Lang.FIXEDTIME_ARGS)]
        private static void CCSetTime(ConCommandArgs args)
        {

            if (args.Count == 0)
            {
                Log.MessageNetworked(Run.instance.fixedTime.ToString(), args, LogLevel.MessageClientOnly);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float setTime))
            {
                Run.instance.fixedTime = setTime;
                ResetEnemyTeamLevel();
                Log.MessageNetworked("Fixed_time set to " + setTime, args);
            }
            else
            {
                Log.MessageNetworked(Lang.FIXEDTIME_ARGS, args, LogLevel.MessageClientOnly);
            }

        }

        [ConCommand(commandName = "time_scale", flags = ConVarFlags.Engine | ConVarFlags.ExecuteOnServer, helpText = "Sets the Time Delta. " + Lang.TIMESCALE_ARGS)]
        private static void CCTimeScale(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Time.timeScale.ToString(), args, LogLevel.MessageClientOnly);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float scale))
            {
                Time.timeScale = scale;
                Log.MessageNetworked("Time scale set to " + scale, args);
                TimeScaleNetwork.Invoke(scale);
            }
            else
            {
                Log.Message(Lang.TIMESCALE_ARGS, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "post_sound_event", flags = ConVarFlags.ExecuteOnServer, helpText = "Post a sound event to the AkSoundEngine (WWise) by its event name.")]
        private static void CCPostSoundEvent(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            AkSoundEngine.PostEvent(args[0], CameraRigController.readOnlyInstancesList[0].localUserViewer.currentNetworkUser.master.GetBodyObject());
        }
#endregion

#region Entities

        private static void ResetEnemyTeamLevel()
        {
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1);
        }

        [ConCommand(commandName = "spawn_interactable", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns the specified interactable. List_Interactable for options. " + Lang.SPAWNINTERACTABLE_ARGS)]
        [AutoCompletion(typeof(StringFinder), "interactableSpawnCards")]
        private static void CCSpawnInteractable(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.SPAWNINTERACTABLE_ARGS, args, LogLevel.ErrorClientOnly);
                return;
            }
            try
            {
                var isc = StringFinder.Instance.GetInteractableSpawnCard(args[0]);
                isc.DoSpawn(args.senderBody.transform.position, new Quaternion(), new DirectorSpawnRequest(
                    isc,
                    new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                        maxDistance = 100f,
                        minDistance = 20f,
                        position = args.senderBody.transform.position,
                        preventOverhead = true
                    },
                    RoR2Application.rng)
                );
            }
            catch (Exception ex)
            {
                Log.MessageNetworked(ex.ToString(), args, LogLevel.ErrorClientOnly);
            }
        }

        [ConCommand(commandName = "god", flags = ConVarFlags.ExecuteOnServer, helpText = "Become invincible. " + Lang.GOD_ARGS)]
        private static void CCGodModeToggle(ConCommandArgs args)
        {
            var godToggleMethod = typeof(CharacterMaster).GetMethodCached("ToggleGod");
            bool hasNotYetRun = true;
            foreach (var playerInstance in PlayerCharacterMasterController.instances)
            {
                godToggleMethod.Invoke(playerInstance.master, null);
                if (hasNotYetRun)
                {
                    Log.MessageNetworked($"God mode {(playerInstance.master.GetBody().healthComponent.godMode ? "enabled" : "disabled")}.", args);
                    hasNotYetRun = false;
                }
            }
        }

        [ConCommand(commandName = "noclip", flags = ConVarFlags.ExecuteOnServer, helpText = "Allow flying and going through objects. Sprinting will double the speed. " + Lang.NOCLIP_ARGS)]
        private static void CCNoclip(ConCommandArgs args)
        {
            if(args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (Run.instance)
            {
                Command_Noclip.Toggle.Invoke(true, args.sender); //callback
            }
            else
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "teleport_on_cursor", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleport you to where your cursor is currently aiming at. " + Lang.CURSORTELEPORT_ARGS)]
        private static void CCCursorTeleport(ConCommandArgs args)
        {
            if(args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (Run.instance && args.senderBody)
            {
                Command_Teleport.Activator.Invoke(true, args.sender); //callback
            }
            else
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = "Kill all entities on the specified team. " + Lang.KILLALL_ARGS)]
        private static void CCKillAll(ConCommandArgs args)
        {
            TeamIndex team;
            if (args.Count == 0)
            {
                team = TeamIndex.Monster;
            }
            else
            {
                team = StringFinder.GetEnumFromPartial<TeamIndex>(args[0]);
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
            Log.MessageNetworked("Killed " + count + " of team " + team + ".", args);
        }

        [ConCommand(commandName = "true_kill", flags = ConVarFlags.ExecuteOnServer, helpText = "Ignore Dio's and kill the entity. " + Lang.TRUEKILL_ARGS)]
        private static void CCTrueKill(ConCommandArgs args)
        {
            if(args.sender==null && args.Count < 1)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }
            CharacterMaster master = args.sender?.master;
            if (args.Count > 0)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            master.TrueKill();
            Log.MessageNetworked(master.name + " was killed by server.", args);
        }

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = "Toggle Monster spawning. " + Lang.NOENEMIES_ARGS)]
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
            Log.MessageNetworked("No_enemies set to " + noEnemies, args);
        }

        [ConCommand(commandName = "spawn_as", flags = ConVarFlags.ExecuteOnServer, helpText = "Respawn the specified player using the specified body prefab. " + Lang.SPAWNAS_ARGS)]
        [AutoCompletion(typeof(BodyCatalog), "bodyPrefabBodyComponents", "baseNameToken")]
        private static void CCSpawnAs(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.SPAWNAS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            string character = StringFinder.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Log.MessageNetworked(Lang.SPAWN_ERROR + args[0], args, LogLevel.MessageClientOnly);
                Log.MessageNetworked("Please use list_body to print CharacterBodies", args, LogLevel.MessageClientOnly);
                return;
            }
            GameObject newBody = BodyCatalog.FindBodyPrefab(character);
            
            if (args.sender == null && args.Count < 2)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }

            CharacterMaster master = args.sender?.master;
            if (args.Count > 1)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            if (!master.alive)
            {
                Log.MessageNetworked(Lang.PLAYER_DEADRESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }

            master.bodyPrefab = newBody;
            Log.MessageNetworked(args.sender.userName + " is spawning as " + character, args);
            RoR2.ConVar.BoolConVar stage1pod = ((RoR2.ConVar.BoolConVar)(typeof(Stage)).GetFieldCached("stage1PodConVar").GetValue(null));
            bool oldVal = stage1pod.value;
            stage1pod.SetBool(false);

            // TODO: Fix so that the respawning player has its noclip disabled no matter what, for now, band aid fix for the local player only
            if (LocalUserManager.GetFirstLocalUser().currentNetworkUser == args.sender && Command_Noclip.IsActivated)
            {
                Command_Noclip.InternalToggle();
            }

            master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
            stage1pod.SetBool(oldVal);
        }

        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns the specified CharacterMaster. " + Lang.SPAWNAI_ARGS)]
        [AutoCompletion(typeof(MasterCatalog), "aiMasterPrefabs")]
        private static void CCSpawnAI(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.Message(Lang.DS_NOTYETIMPLEMENTED, LogLevel.Error);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.SPAWNAI_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            string character = StringFinder.Instance.GetMasterName(args[0]);
            if (character == null)
            {
                Log.MessageNetworked(Lang.SPAWN_ERROR + character, args, LogLevel.MessageClientOnly);
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
                var eliteIndex = StringFinder.GetEnumFromPartial<EliteIndex>(args[1]);
                master.inventory.SetEquipmentIndex(EliteCatalog.GetEliteDef(eliteIndex).eliteEquipmentIndex);
                master.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((GetTierDef(eliteIndex).healthBoostCoefficient - 1) * 10));
                master.inventory.GiveItem(ItemIndex.BoostDamage, Mathf.RoundToInt((GetTierDef(eliteIndex).damageBoostCoefficient - 1) * 10));
            }

            if (args.Count > 2 && Enum.TryParse<TeamIndex>(StringFinder.GetEnumFromPartial<TeamIndex>(args[2]).ToString(), true, out TeamIndex teamIndex))
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
            Log.MessageNetworked(Lang.SPAWN_ATTEMPT + character, args);
        }

        [ConCommand(commandName = "spawn_body", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns the specified dummy body. " + Lang.SPAWNBODY_ARGS)]
        [AutoCompletion(typeof(BodyCatalog), "bodyPrefabBodyComponents", "baseNameToken")]
        private static void CCSpawnBody(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.SPAWNBODY_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                Log.Message(Lang.DS_NOTYETIMPLEMENTED, LogLevel.Error);
                return;
            }

            string character = StringFinder.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Log.MessageNetworked(string.Format(Lang.SPAWN_ERROR, args[0]), args, LogLevel.MessageClientOnly);
                return;
            }

            GameObject body = BodyCatalog.FindBodyPrefab(character);
            GameObject gameObject = Instantiate<GameObject>(body, args.sender.master.GetBody().transform.position, Quaternion.identity);
            NetworkServer.Spawn(gameObject);
            Log.MessageNetworked(Lang.SPAWN_ATTEMPT + character, args);
        }

        [ConCommand(commandName = "respawn", flags = ConVarFlags.ExecuteOnServer, helpText = "Respawns the specified player. " + Lang.RESPAWN_ARGS)]
        [AutoCompletion(typeof(NetworkUser), "instancesList", "userName", true)]
        [AutoCompletion(typeof(NetworkUser), "instancesList", "_id/value", true)]
        private static void RespawnPlayer(ConCommandArgs args)
        {
            if(args.sender==null && args.Count < 1)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }
            CharacterMaster master = args.sender?.master;
            if (args.Count > 0)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            Transform spawnPoint = Stage.instance.GetPlayerSpawnTransform();
            master.Respawn(spawnPoint.position, spawnPoint.rotation, false);
            Log.MessageNetworked(Lang.SPAWN_ATTEMPT + master.name, args);
        }

        [ConCommand(commandName = "change_team", flags = ConVarFlags.ExecuteOnServer, helpText = "Change the specified player to the specified team. " + Lang.CHANGETEAM_ARGS)]
        private static void CCChangeTeam(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.CHANGETEAM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null && args.Count < 2)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }

            CharacterMaster master = args.sender?.master;
            if (args.Count > 1)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            if (Enum.TryParse(StringFinder.GetEnumFromPartial<TeamIndex>(args[0]).ToString(), true, out TeamIndex teamIndex))
            {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count)
                {
                    if (master.GetBody())
                    {
                        master.GetBody().teamComponent.teamIndex = teamIndex;
                        master.teamIndex = teamIndex;
                        Log.MessageNetworked("Changed to team " + teamIndex, args);
                        return;
                    }
                }
            }
            //Note the `return` on succesful evaluation.
            Log.MessageNetworked("Invalid team. Please use 0,'neutral',1,'player',2, or 'monster'", args, LogLevel.MessageClientOnly);

        }

        [ConCommand(commandName = "add_portal", flags = ConVarFlags.ExecuteOnServer, helpText = "Add a portal to the current Teleporter on completion. " + Lang.ADDPORTAL_ARGS)]
        private static void CCAddPortal(ConCommandArgs args)
        {
            if (TeleporterInteraction.instance)
            {
                var TP = TeleporterInteraction.instance;
                switch (args[0].ToLower())
                {
                    case "blue":
                        TP.shouldAttemptToSpawnShopPortal = true;
                        break;
                    case "gold":
                        TP.shouldAttemptToSpawnGoldshoresPortal = true;
                        break;
                    case "celestial":
                        TP.shouldAttemptToSpawnMSPortal = true;
                        break;
                    case "arena":
                        spawnArenaPortal();
                        break;
                    case "all":
                        TP.shouldAttemptToSpawnGoldshoresPortal = true;
                        TP.shouldAttemptToSpawnShopPortal = true;
                        TP.shouldAttemptToSpawnMSPortal = true;
                        spawnArenaPortal();
                        break;
                    default:
                        Log.MessageNetworked(Lang.PORTAL_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                }

                void spawnArenaPortal()
                {
                    var arenaPortal = Instantiate(Resources.Load<GameObject>("prefabs\\networkedobjects\\portalarena"), args.senderBody.corePosition, Quaternion.identity);
                    arenaPortal.GetComponent<SceneExitController>().useRunNextStageScene = false;
                    NetworkServer.Spawn(arenaPortal);
                }
            }
            else
            {
                Log.MessageNetworked("No teleporter instance!", args, LogLevel.WarningClientOnly);
            }
        }

        internal static CombatDirector.EliteTierDef GetTierDef(EliteIndex index)
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

        internal static bool UpdateCurrentPlayerBody(out NetworkUser networkUser, out CharacterBody characterBody)
        {
            networkUser = LocalUserManager.GetFirstLocalUser().currentNetworkUser;
            characterBody = null;

            if (networkUser)
            {
                var master = networkUser.master;

                if (master && master.GetBody())
                {
                    characterBody = master.GetBody();
                    return true;
                }
            }

            return false;
        }

#endregion

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
            Log.MessageNetworked(s.ToString(), args, LogLevel.MessageClientOnly);
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
                Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
            }

        }
#endif
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