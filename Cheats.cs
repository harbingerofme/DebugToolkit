using BepInEx;
using BepInEx.Configuration;
using EntityStates.Commando;
using EntityStates.Huntress;
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
using Utilities;
using ArgsHelper = Utilities.Generic.ArgsHelper;

namespace RoR2Cheats {
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.morris1927.RoR2Cheats", "RoR2Cheats", "2.3.1")]
    public class Cheats : BaseUnityPlugin {

        private static ConfigEntry<float> SprintFovMultiplierConfig;
        private static ConfigEntry<float> FovConfig;
        private static ConfigEntry<bool> GodMode;
       // private static ConfigEntry<float> fovConfig { get; set; }

        public static float SprintFoVMultiplier { get { return SprintFovMultiplierConfig.Value; } set { SprintFovMultiplierConfig.Value = value; } }
        public static float FieldOfVision { get { return FovConfig.Value; } set { FovConfig.Value = value; } }


        public static ulong seed = 0;
        public static bool godMode = false;

        public static bool noEnemies = false;

        private ConfigEntry<KeyboardShortcut> KeyMultiplayerPause;
        private ConfigEntry<KeyboardShortcut> KeyGodMode;


        public void Awake()
        {
            SprintFovMultiplierConfig = Config.AddSetting(
                "FOV",
                "sprint Fov Multiplier",
                1.3f,
                new ConfigDescription(
                "What FOV gets multiplied by while sprinting",
                new AcceptableValueRange<float>(1f,2f)
                )
            );
            FovConfig = Config.AddSetting(
                "FOV",
                "Base FOV",
                60f,
                "Your base Field of vision"
            );
            GodMode = Config.AddSetting(
                "Cheats",
                "God mode",
                false,
                "Disables the HealthComponent.takeDamage method for all players."
                );
            if (GodMode.Value) { togglegod();}
            GodMode.SettingChanged += togglegod;
            KeyGodMode = Config.AddSetting("Hotkeys", "Toggle God mode", new KeyboardShortcut());
            KeyMultiplayerPause = Config.AddSetting("Hotkeys","True Multiplayer Pause", new KeyboardShortcut(KeyCode.F2));


            Hooks.InitializeHooks();
            NetworkHandler.RegisterNetworkHandlerAttributes();

        }
        public void Update() {
            if (KeyMultiplayerPause.Value.IsDown()) {
                RoR2.Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], "time_scale " + (Time.timeScale != 0 ? 0 : 1));
            }
            if (KeyGodMode.Value.IsDown())
            {
                GodMode.Value = !godMode;
            }

        }

        [ConCommand(commandName = "god", flags = ConVarFlags.ExecuteOnServer, helpText = "Godmode")]
        private static void CCGodModeToggle(ConCommandArgs _) {
            GodMode.Value = !godMode;
        }
        private static void togglegod(object _=null, EventArgs __=null)
        {
            godMode = !godMode;
            foreach (var playerInstance in PlayerCharacterMasterController.instances)
            {
                playerInstance.master.InvokeMethod("ToggleGod");
            }
            Debug.Log($"God mode {(godMode ? "enabled":"disabled")}.");
        }

        [ConCommand(commandName = "time_scale", flags = ConVarFlags.None | ConVarFlags.ExecuteOnServer, helpText = "Time scale")]
        private static void CCTimeScale(ConCommandArgs args) {
            string scaleString = ArgsHelper.GetValue(args.userArgs, 0);
            float scale = 1f;

            if (args.Count == 0) {
                Debug.Log(Time.timeScale);
                return;
            }

            if (float.TryParse(scaleString, out scale)) {
                Time.timeScale = scale;
                Debug.Log("Time scale set to " + scale);
            } else {
                Debug.Log("Incorrect arguments. Try: time_scale 0.5");
            }

            NetworkWriter networkWriter = new NetworkWriter();
            networkWriter.StartMessage(101);
            networkWriter.Write((double)Time.timeScale);

            networkWriter.FinishMessage();
            NetworkServer.SendWriterToReady(null, networkWriter, QosChannelIndex.time.intVal);
        }

        [NetworkMessageHandler(msgType = 101, client = true, server = false)]
        private static void HandleTimeScale(NetworkMessage netMsg) {
            NetworkReader reader = netMsg.reader;
            Time.timeScale = (float)reader.ReadDouble();
        }

        [ConCommand(commandName = "list_items", flags = ConVarFlags.ExecuteOnServer, helpText = "List all item names and their IDs")]
        private static void CCListItems(ConCommandArgs args) {
            StringBuilder text = new StringBuilder();
            foreach (ItemIndex item in ItemCatalog.allItems)
            {
                int index = (int)item;
                string line = string.Format("{0} = {1}", index, item);
                text.AppendLine(line);
            }

            Debug.Log(text.ToString());
        }


        [ConCommand(commandName = "list_equips", flags = ConVarFlags.None, helpText = "List all equipment items and their IDs")]
        private static void CCListEquipments(ConCommandArgs args) {
            StringBuilder text = new StringBuilder();
            foreach (EquipmentIndex item in EquipmentCatalog.allEquipment)
            {
                int index = (int)item;
                string line = string.Format("{0} = {1}", index, item);
                text.AppendLine(line);
            }

            Debug.Log(text.ToString());
        }

        [ConCommand(commandName = "give_item", flags = ConVarFlags.None, helpText = "Give item")]
        private static void CCGiveItem(ConCommandArgs args) {

            string indexString = ArgsHelper.GetValue(args.userArgs, 0);
            string countString = ArgsHelper.GetValue(args.userArgs, 1);
            string playerString = ArgsHelper.GetValue(args.userArgs, 2);

            NetworkUser player = GetNetUserFromString(playerString);

            Inventory inventory = player != null ? player.master.inventory : args.sender.master.inventory;


            int itemCount = 1;
            if (!int.TryParse(countString, out itemCount))
                itemCount = 1;

            int itemIndex = 0;
            ItemIndex itemType = ItemIndex.Syringe;
            if (int.TryParse(indexString, out itemIndex)) {
                if (itemIndex < (int)ItemIndex.Count && itemIndex >= 0) {
                    itemType = (ItemIndex)itemIndex;
                    inventory.GiveItem(itemType, itemCount);
                }
            } else if (Enum.TryParse<ItemIndex>(indexString, true, out itemType)) {
                inventory.GiveItem(itemType, itemCount);
            } else {
                Debug.Log("Incorrect arguments. Try: give_item syringe 10   --- list_items for a list of items");
            }


        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Give equipment")]
        private static void CCGiveEquipment(ConCommandArgs args) {

            string equipString = ArgsHelper.GetValue(args.userArgs, 0);
            string playerString = ArgsHelper.GetValue(args.userArgs, 1);

            NetworkUser player = GetNetUserFromString(playerString);

            Inventory inventory = player != null ? player.master.inventory : args.sender.master.inventory;

            int equipIndex = 0;
            EquipmentIndex equipType = EquipmentIndex.None;

            if (int.TryParse(equipString, out equipIndex)) {
                if (equipIndex < (int)EquipmentIndex.Count && equipIndex >= -1) {
                    inventory.SetEquipmentIndex((EquipmentIndex)equipIndex);
                }
            } else if (Enum.TryParse<EquipmentIndex>(equipString, true, out equipType)) {
                inventory.SetEquipmentIndex(equipType);
            } else {
                Debug.Log("Incorrect arguments. Try: give_equip meteor   --- list_equips for a list of all equipments");
            }

        }

        [ConCommand(commandName = "give_money", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives money")]
        private static void CCGiveMoney(ConCommandArgs args) {
            if (args.Count == 0) {
                return;
            }

            string moneyString = ArgsHelper.GetValue(args.userArgs, 0);
            string playerString = ArgsHelper.GetValue(args.userArgs, 1);

            NetworkUser player = GetNetUserFromString(playerString);
            CharacterMaster master = player != null ? player.master : args.sender.master;

            uint result;
            if (uint.TryParse(moneyString, out result)) {
                master.GiveMoney(result);
            }
        }

        [ConCommand(commandName = "give_exp", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives experience")]
        private static void CCGiveExperience(ConCommandArgs args) {
            if (args.Count == 0) {
                return;
            }

            string expString = ArgsHelper.GetValue(args.userArgs, 0);
            string playerString = ArgsHelper.GetValue(args.userArgs, 1);

            NetworkUser player = GetNetUserFromString(playerString);
            CharacterMaster master = player != null ? player.master : args.sender.master;

            uint result;
            if (uint.TryParse(expString, out result)) {
                master.GiveExperience(result);
            }
        }

        [ConCommand(commandName = "next_round", flags = ConVarFlags.ExecuteOnServer, helpText = "Start next round. Additional args for specific scene.")]
        private static void CCNextRound(ConCommandArgs args) {
            if (args.Count == 0) {
                Run.instance.AdvanceStage(Run.instance.nextStageScene);
                return;
            }

            string stageString = ArgsHelper.GetValue(args.userArgs, 0);

            List<string> array = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
                array.Add(SceneUtility.GetScenePathByBuildIndex(i).Replace("Assets/RoR2/Scenes/", "").Replace(".unity", ""));
            }

            if (array.Contains(stageString)) {
                Run.instance.AdvanceStage(SceneCatalog.GetSceneDefFromSceneName(stageString));
                return;
            } else {
                Debug.Log("Incorrect arguments. Try: next_round golemplains   --- Here is a list of available scenes");
                Debug.Log(string.Join("\n", array));
            }
        }

        [ConCommand(commandName = "seed", flags = ConVarFlags.None, helpText = "Set seed.")]
        private static void CCUseSeed(ConCommandArgs args) {

            if (args.Count == 0) {
                Debug.Log(seed);
            }

            string stringSeed = ArgsHelper.GetValue(args.userArgs, 0);
            if (!ulong.TryParse(stringSeed, out seed)) {
                Debug.Log("Incorrect arguments given. Try: seed 12345");
            } else {
                Debug.Log("Seed set to " + seed);
            }
        }

        [ConCommand(commandName = "fixed_time", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets fixed time - Affects monster difficulty")]
        private static void CCSetTime(ConCommandArgs args) {

            if (args.Count == 0) {
                Debug.Log(Run.instance.fixedTime);
                return;
            }

            string stringTime = ArgsHelper.GetValue(args.userArgs, 0);
            float setTime;
            if (float.TryParse(stringTime, out setTime)) {
                Run.instance.fixedTime = setTime;
                ResetEnemyTeamLevel();
                Debug.Log("Fixed_time set to " + setTime);
            } else {
                Debug.Log("Incorrect arguments. Try: fixed_time 600");
            }

        }

        [ConCommand(commandName = "stage_clear_count", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets stage clear count - Affects monster difficulty")]
        private static void CCSetClearCount(ConCommandArgs args) {
            string stringClearCount = ArgsHelper.GetValue(args.userArgs, 0);

            if (args.Count == 0) {
                Debug.Log(Run.instance.stageClearCount);
                return;
            }

            int setClearCount;
            if (int.TryParse(stringClearCount, out setClearCount)) {
                Run.instance.stageClearCount = setClearCount;
                ResetEnemyTeamLevel();
                Debug.Log("Stage_clear_count set to " + setClearCount);
            } else {
                Debug.Log("Incorrect arguments. Try: stage_clear_count 5");
            }

        }

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = "Stops enemies from spawning")]
        private static void CCNoEnemies(ConCommandArgs args) {
            noEnemies = !noEnemies;
            Debug.Log("No_enemies toggled " + noEnemies);
        }


        [ConCommand(commandName = "spawn_as", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawn as a new character. Type body_list for a full list of characters")]
        private static void CCSpawnAs(ConCommandArgs args) {
            if (args.Count == 0) {

                return;
            }

            string bodyString = ArgsHelper.GetValue(args.userArgs, 0);
            string playerString = ArgsHelper.GetValue(args.userArgs, 1);

            var character = Character.GetCharacter(bodyString);
            if (character == null) {
                Debug.LogFormat("Could not spawn {0}, Try: spawn_ai GolemBody", character.body);
                return;
            }

            NetworkUser player = GetNetUserFromString(playerString);

            CharacterMaster master = player != null ? player.master : args.sender.master;

            if (!master.alive) {
                Debug.Log("Player is dead and cannot respawn.");
                return;
            }

            GameObject newBody = BodyCatalog.FindBodyPrefab(character.body);
                
            if (newBody == null) {
                List<string> array = new List<string>();
                foreach (var item in BodyCatalog.allBodyPrefabs) {
                    array.Add(item.name);
                }
                string list = string.Join("\n", array);
                Debug.LogFormat("Could not spawn as {0}, Try: spawn_as GolemBody   --- \n{1}", character.body, list);
                return;
            }
            master.bodyPrefab = newBody;
            Debug.Log(args.sender.userName + " is spawning as " + character.body);

            master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
        }

        private static NetworkUser GetNetUserFromString(string playerString) {
            int result = 0;

            if (playerString != "") {
                if (int.TryParse(playerString, out result)) {
                    if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0) {

                        return NetworkUser.readOnlyInstancesList[result];
                    }
                    Debug.Log("Specified player index does not exist");
                    return null;
                } else {
                    foreach (NetworkUser n in NetworkUser.readOnlyInstancesList) {
                        if (n.userName.Equals(playerString, StringComparison.CurrentCultureIgnoreCase)) {
                            return n;
                        }
                    }
                    Debug.Log("Specified player does not exist");
                    return null;
                }
            }

            return null;
        }

        [ConCommand(commandName = "player_list", flags = ConVarFlags.ExecuteOnServer, helpText = "Shows list of players with their ID")]
        private static void CCPlayerList(ConCommandArgs args) {
            NetworkUser n;
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++) {
                n = NetworkUser.readOnlyInstancesList[i];
                Debug.Log(i + ": " + n.userName);
            }
        }

        [ConCommand(commandName = "fov_sprint_multiplier", flags = ConVarFlags.Engine, helpText = "Set your sprint FOV multiplier")]
        private static void CCSetSprintFOVMulti(ConCommandArgs args) {
            if (args.Count == 0) {
                Debug.Log(SprintFoVMultiplier);
                return;
            }
            string multiString = ArgsHelper.GetValue(args.userArgs, 0);

            float sprintFov = 1f;
            if (float.TryParse(multiString, out sprintFov)) {
                SprintFoVMultiplier = sprintFov;
                Debug.Log("Set Sprint FOV Multiplier to " + SprintFoVMultiplier);
            } else
                Debug.Log("Incorrect arguments. Try: sprint_fov_multiplier 1");
        }


        [ConCommand(commandName = "fov", flags = ConVarFlags.Engine, helpText = "Set your FOV")]
        private static void CCSetFov(ConCommandArgs args) {
            if (args.Count == 0) {
                Debug.Log(FieldOfVision);
                return;
            }

            string fovString = ArgsHelper.GetValue(args.userArgs, 0);

            float fovTemp = 60f;
            if (float.TryParse(fovString, out fovTemp)) {
                FieldOfVision = fovTemp;
                DodgeState.dodgeFOV = FieldOfVision - 10f;
                BackflipState.dodgeFOV = FieldOfVision - 10f;
                Debug.Log("Set FOV to " + FieldOfVision);

                List<CameraRigController> instancesList = (List<CameraRigController>)typeof(CameraRigController).GetField("instancesList", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                foreach (CameraRigController c in instancesList) {
                    c.baseFov = FieldOfVision;
                }
            } else {
                Debug.Log("Incorrect arguments. Try: fov 60");
            }
        }

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = "Kill all enemies (not you, don't worry. Unless you talk ^@#$ behind my back. Watch out.)")]
        private static void CCKillAll(ConCommandArgs args) {
            int count = 0;

            foreach (CharacterMaster cm in FindObjectsOfType<CharacterMaster>()) {
                if (cm.teamIndex == TeamIndex.Monster) {
                    CharacterBody cb = cm.GetBody();
                    if (cb) {
                        if (cb.healthComponent) {
                            cb.healthComponent.Suicide(null);
                            count++;
                        }
                    }

                }

            }

            Debug.Log("Killed " + count + " - you monster");
        }

        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawn an AI")]
        private static void CCSpawnAI(ConCommandArgs args) {

            string prefabString = ArgsHelper.GetValue(args.userArgs, 0);
            string eliteString = ArgsHelper.GetValue(args.userArgs, 1);
            string teamString = ArgsHelper.GetValue(args.userArgs, 2);
            string braindeadString = ArgsHelper.GetValue(args.userArgs, 3);

            var character = Character.GetCharacter(prefabString);
            if (character == null) {
                Debug.LogFormat("Could not spawn {0}, Try: spawn_ai GolemBody", character.body);
                return;
            }

            var prefab = MasterCatalog.FindMasterPrefab(character.master);
            var body = BodyCatalog.FindBodyPrefab(character.body);


            var bodyGameObject = Instantiate<GameObject>(prefab, args.sender.master.GetBody().transform.position, Quaternion.identity);
            CharacterMaster master = bodyGameObject.GetComponent<CharacterMaster>();
            NetworkServer.Spawn(bodyGameObject);
            master.SpawnBody(body, args.sender.master.GetBody().transform.position, Quaternion.identity);

            EliteIndex eliteIndex = EliteIndex.None;
            if (Enum.TryParse<EliteIndex>(eliteString, true, out eliteIndex)) {
                if ((int)eliteIndex > (int)EliteIndex.None && (int)eliteIndex < (int)EliteIndex.Count) {
                    master.inventory.SetEquipmentIndex(EliteCatalog.GetEliteDef(eliteIndex).eliteEquipmentIndex);
                }
            }

            TeamIndex teamIndex = TeamIndex.Neutral;
            if (Enum.TryParse<TeamIndex>(teamString, true, out teamIndex)) {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count) {
                    master.teamIndex = teamIndex;
                }
            }

            bool braindead;
            if (bool.TryParse(braindeadString, out braindead)) {
                if (braindead) {
                    Destroy(master.GetComponent<BaseAI>());
                }
            }
            Debug.Log("Attempting to spawn " + character.body);
        }

        [ConCommand(commandName = "spawn_body", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns a CharacterBody")]
        private static void CCSpawnBody(ConCommandArgs args) {
            string prefabString = ArgsHelper.GetValue(args.userArgs, 0);

            var character = Character.GetCharacter(prefabString);
            if (character == null) {
                Debug.LogFormat("Could not spawn {0}, Try: spawn_ai GolemBody", character.body);
                return;
            }

            GameObject body = BodyCatalog.FindBodyPrefab(character.body);

            GameObject gameObject = Instantiate<GameObject>(body, args.sender.master.GetBody().transform.position, Quaternion.identity);

            NetworkServer.Spawn(gameObject);
            Debug.Log("Attempting to spawn " + character.body);
        }

        private static void ResetEnemyTeamLevel() {
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1);
        }
        
        [ConCommand(commandName = "true_kill", flags = ConVarFlags.ExecuteOnServer, helpText = "Truly kill a player, ignoring revival effects")]
		private static void CCTrueKill(ConCommandArgs args)
		{
            string playerString = ArgsHelper.GetValue(args.userArgs, 0);

            NetworkUser player = GetNetUserFromString(playerString);
            player = player ?? args.sender;

            player.master.TrueKill();
		}
        
        [ConCommand(commandName = "add_blue", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a blue portal on completion")]
		private static void CCAddBlueOrb(ConCommandArgs args)
		{
            if (TeleporterInteraction.instance)
			    TeleporterInteraction.instance.Network_shouldAttemptToSpawnShopPortal = true;
		}

		[ConCommand(commandName = "add_gold", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a gold portal on completion")]
		private static void CCAddGoldOrb(ConCommandArgs args)
		{
            if (TeleporterInteraction.instance)
                TeleporterInteraction.instance.Network_shouldAttemptToSpawnGoldshoresPortal = true;
		}

		[ConCommand(commandName = "add_celestial", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a celestial portal on completion")]
		private static void CCAddCelestialOrb(ConCommandArgs args)
		{
            if (TeleporterInteraction.instance)
                TeleporterInteraction.instance.Network_shouldAttemptToSpawnMSPortal = true;
		}

        [ConCommand(commandName = "change_team", flags = ConVarFlags.ExecuteOnServer, helpText = "Change team to Neutral, Player or Monster (0, 1, 2)")]
        private static void CCChangeTeam(ConCommandArgs args) {
            string teamString = ArgsHelper.GetValue(args.userArgs, 0);
            string playerString = ArgsHelper.GetValue(args.userArgs, 1);

            NetworkUser player = GetNetUserFromString(playerString);
            player = player ?? args.sender;

            TeamIndex teamIndex = TeamIndex.Player;
            if (Enum.TryParse(teamString, true, out teamIndex)) {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count) {
                    Debug.Log("Changed to team " + teamIndex);
                    if (player.master.GetBody()) {
                        player.master.GetBody().teamComponent.teamIndex = teamIndex;
                        player.master.teamIndex = teamIndex;
                    }
                }
            }
        }
        
        [ConCommand(commandName = "respawn", flags = ConVarFlags.ExecuteOnServer, helpText = "Respawn a player")]
		private static void RespawnPlayer(ConCommandArgs args)
		{
            string playerString = ArgsHelper.GetValue(args.userArgs, 0);

            NetworkUser player = GetNetUserFromString(playerString);
            player = player ?? args.sender;

            Transform spawnPoint = Stage.instance.GetPlayerSpawnTransform();
            player.master.Respawn(spawnPoint.position, spawnPoint.rotation, false);
		}
    }
}
