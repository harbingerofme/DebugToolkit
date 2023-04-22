using RoR2;
using RoR2.CharacterAI;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Spawners
    {

        [ConCommand(commandName = "spawn_interactable", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNINTERACTABLE_HELP)]
        [AutoCompletion(typeof(StringFinder), "interactableSpawnCards", "", true)]
        private static void CCSpawnInteractable(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                Log.Message(Lang.DS_NOTYETIMPLEMENTED, LogLevel.Error);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SPAWNINTERACTABLE_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't spawn an object with relation to a dead target.", args, LogLevel.MessageClientOnly);
                return;
            }
            var isc = StringFinder.Instance.GetInteractableSpawnCard(args[0]);
            if (isc == null)
            {
                Log.MessageNetworked(Lang.INTERACTABLE_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            var result = isc.DoSpawn(args.senderBody.transform.position, new Quaternion(), new DirectorSpawnRequest(
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
            if (!result.success)
            {
                Log.MessageNetworked("Failed to spawn interactable.", args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "spawn_interactible", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNINTERACTABLE_HELP)]
        [AutoCompletion(typeof(StringFinder), "interactableSpawnCards", "", true)]
        private static void CCSpawnInteractible(ConCommandArgs args)
        {
            CCSpawnInteractable(args);
        }

        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNAI_HELP)]
        [AutoCompletion(typeof(MasterCatalog), "aiMasterPrefabs")]
        private static void CCSpawnAI(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                Log.Message(Lang.DS_NOTYETIMPLEMENTED, LogLevel.Error);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SPAWNAI_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't spawn an object with relation to a dead target.", args, LogLevel.MessageClientOnly);
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
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            Vector3 location = args.senderBody.transform.position;
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_2, amount, character), args);
            for (int i = 0; i < amount; i++)
            {
                var bodyGameObject = UnityEngine.Object.Instantiate<GameObject>(masterprefab, location, Quaternion.identity);
                CharacterMaster master = bodyGameObject.GetComponent<CharacterMaster>();
                NetworkServer.Spawn(bodyGameObject);
                master.bodyPrefab = body;
                master.SpawnBody(args.sender.master.GetBody().transform.position, Quaternion.identity);

                if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
                {
                    var eliteDef = int.TryParse(args[2], out var eliteIndex) ?
                        EliteCatalog.GetEliteDef((EliteIndex)eliteIndex) :
                        EliteCatalog.eliteDefs.FirstOrDefault(d => d.name.ToLower().Contains(args[2].ToLower(CultureInfo.InvariantCulture)));
                    if (eliteDef)
                    {
                        master.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentDef.equipmentIndex);
                        master.inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt((eliteDef.healthBoostCoefficient - 1) * 10));
                        master.inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt(eliteDef.damageBoostCoefficient - 1) * 10);
                    }
                    else
                    {
                        Log.MessageNetworked(Lang.ELITE_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                }

                bool braindead = false;
                if (args.Count > 3 && args[3] != Lang.DEFAULT_VALUE && !Util.TryParseBool(args[3], out braindead))
                {
                    Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "braindead", "int or bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
                if (braindead)
                {
                    UnityEngine.Object.Destroy(master.GetComponent<BaseAI>());
                }

                TeamIndex teamIndex = TeamIndex.Monster;
                if (args.Count > 4 && args[4] != Lang.DEFAULT_VALUE && !StringFinder.TryGetEnumFromPartial(args[4], out teamIndex))
                {
                    Log.MessageNetworked(Lang.TEAM_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                if (teamIndex >= TeamIndex.None && teamIndex < TeamIndex.Count)
                {
                    master.teamIndex = teamIndex;
                    master.GetBody().teamComponent.teamIndex = teamIndex;
                }
            }
        }

        [ConCommand(commandName = "spawn_body", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNBODY_HELP)]
        [AutoCompletion(typeof(BodyCatalog), "bodyPrefabBodyComponents", "baseNameToken")]
        private static void CCSpawnBody(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                Log.Message(Lang.DS_NOTYETIMPLEMENTED, LogLevel.Error);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SPAWNBODY_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't spawn an object with relation to a dead target.", args, LogLevel.MessageClientOnly);
                return;
            }

            string character = StringFinder.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Log.MessageNetworked(string.Format(Lang.SPAWN_ERROR, args[0]), args, LogLevel.MessageClientOnly);
                return;
            }

            GameObject body = BodyCatalog.FindBodyPrefab(character);
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(body, args.senderBody.transform.position, Quaternion.identity);
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

            return CombatDirector.eliteTiers[0];
        }
    }
}
