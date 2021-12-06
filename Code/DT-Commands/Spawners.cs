using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using static DebugToolkit.Log;
using RoR2.CharacterAI;
using System.Linq;
using System.Globalization;

namespace DebugToolkit.Commands
{
    class Spawners
    {

        [ConCommand(commandName = "spawn_interactable", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns the specified interactable. List_Interactable for options. " + Lang.SPAWNINTERACTABLE_ARGS)]
        [AutoCompletion(typeof(StringFinder), "interactableSpawnCards", "", true)]
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

        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns the specified CharacterMaster. " + Lang.SPAWNAI_ARGS)]
        [AutoCompletion(typeof(MasterCatalog), "aiMasterPrefabs")]
        private static void CCSpawnAI(ConCommandArgs args)
        {
            //- Spawns the specified CharacterMaster. Requires 1 argument: spawn_ai 0:{localised_objectname} 1:[Count:1] 2:[EliteIndex:-1/None] 3:[Braindead:0/false(0|1)] 4:[TeamIndex:0/Neutral]

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

            int amount = 1;
            if (args.Count > 1)
            {
                if (int.TryParse(args[1], out amount) == false)
                {
                    Log.MessageNetworked(Lang.SPAWNAI_ARGS, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            Vector3 location = args.sender.master.GetBody().transform.position;
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_2, amount, character), args);
            for (int i = 0; i < amount; i++)
            {
                var bodyGameObject = UnityEngine.Object.Instantiate<GameObject>(masterprefab, location, Quaternion.identity);
                CharacterMaster master = bodyGameObject.GetComponent<CharacterMaster>();
                NetworkServer.Spawn(bodyGameObject);
                master.bodyPrefab = body;
                master.SpawnBody(args.sender.master.GetBody().transform.position, Quaternion.identity);

                if (args.Count > 2)
                {
                    var eliteDef = int.TryParse(args[2], out var eliteIndex) ?
                        EliteCatalog.GetEliteDef((EliteIndex)eliteIndex) :
                        EliteCatalog.eliteDefs.FirstOrDefault(d => d.name.ToLower().Contains(args[2].ToLower(CultureInfo.InvariantCulture)));
                    if (eliteDef)
                    {
                        master.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentDef.equipmentIndex);
                        var eliteTierDef = GetTierDef(eliteDef.eliteIndex);
                        if (eliteTierDef != null)
                        {
                            master.inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt((eliteTierDef.healthBoostCoefficient - 1) * 10));
                            master.inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt((GetTierDef(eliteDef.eliteIndex).damageBoostCoefficient - 1) * 10));
                        }
                        else
                        {
                            Debug.LogWarning($"That eliteDef {eliteDef.name} is not properly registered by the game");
                        }
                    }
                }

                if (args.Count > 3 && Util.TryParseBool(args[3], out bool braindead) && braindead)
                {
                    UnityEngine.Object.Destroy(master.GetComponent<BaseAI>());
                }

                TeamIndex teamIndex = TeamIndex.Monster;
                if (args.Count > 4)
                {
                    StringFinder.TryGetEnumFromPartial(args[4], out teamIndex);
                }

                if (teamIndex >= TeamIndex.None && teamIndex < TeamIndex.Count)
                {
                    master.teamIndex = teamIndex;
                    master.GetBody().teamComponent.teamIndex = teamIndex;
                }
            }
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
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(body, args.sender.master.GetBody().transform.position, Quaternion.identity);
            NetworkServer.Spawn(gameObject);
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_1, character), args);
        }


        internal static CombatDirector.EliteTierDef GetTierDef(EliteIndex index)
        {
            foreach (var eliteTier in CombatDirector.eliteTiers)
            {
                if (eliteTier != null)
                {
                    foreach (var eliteDef in eliteTier.eliteTypes)
                    {
                        if (eliteDef)
                        {
                            if (eliteDef.eliteIndex == index)
                            {
                                return eliteTier;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
