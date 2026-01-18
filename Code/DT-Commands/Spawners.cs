using KinematicCharacterController;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Spawners
    {
        public static readonly Dictionary<string, GameObject> portals = new Dictionary<string, GameObject>();

        [ConCommand(commandName = "spawn_interactable", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNINTERACTABLE_HELP)]
        [ConCommand(commandName = "spawn_interactible", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNINTERACTABLE_HELP)]
        [AutoComplete(Lang.SPAWNINTERACTABLE_ARGS)]
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
            var isc = StringFinder.Instance.GetInteractableSpawnCardFromPartial(args[0]);
            if (isc == null)
            {
                Log.MessageNetworked(Lang.INTERACTABLE_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            int amount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            // Putting interactables with a collider just far enough to not cause any clipping
            // or spawn under the character's feet. The few exceptions with MeshCollider aren't
            // treated but they aren't much of an issue.
            var colliders = isc.prefab.GetComponentsInChildren<Collider>();
            var distance = 0f;
            foreach (var collider in colliders)
            {
                if (!collider.isTrigger && collider.enabled)
                {
                    var box = collider as BoxCollider;
                    var capsule = collider as CapsuleCollider;
                    var sphere = collider as SphereCollider;
                    var scale = collider.transform.lossyScale;
                    if (box)
                    {
                        var x = box.size.x * scale.x;
                        var y = box.size.y * scale.y;
                        distance = Mathf.Max(distance, Mathf.Sqrt(x * x + y * y) * 0.5f);
                    }
                    else if (capsule)
                    {
                        distance = Mathf.Max(distance, capsule.radius);
                    }
                    else if (sphere)
                    {
                        distance = Mathf.Max(distance, sphere.radius);
                    }
                }
            }
            var position = args.senderBody.footPosition;
            if (distance > 0f)
            {
                var direction = args.senderBody.inputBank.aimDirection;
                position = position + (args.senderBody.radius + distance) * new Vector3(direction.x, 0f, direction.z);
            }
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_2, amount, isc.prefab.name), args);
            int failed = 0;
            for (int i = 0; i < amount; i++)
            {
                var result = isc.DoSpawn(position, new Quaternion(), new DirectorSpawnRequest(isc, null, RoR2Application.rng));
                if (!result.success)
                {
                    failed++;
                }
            }
            if (failed > 0)
            {
                Log.MessageNetworked($"Failed to spawn {failed} interactable.", args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "spawn_portal", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNPORTAL_HELP)]
        [AutoComplete(Lang.SPAWNPORTAL_ARGS)]
        private static void CCSpawnPortal(ConCommandArgs args)
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
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SPAWNPORTAL_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't spawn an object with relation to a dead target.", args, LogLevel.MessageClientOnly);
                return;
            }

            var portalName = args[0].ToLowerInvariant();
            if (!portals.TryGetValue(portalName, out var portal))
            {
                Log.MessageNetworked(string.Format(Lang.INVALID_ARG_VALUE, "portal"), args, LogLevel.MessageClientOnly);
                return;
            }
            var currentScene = Stage.instance.sceneDef;

            if (currentScene.cachedName == "voidraid" && portalName == "deepvoid")
            {
                portal = StringFinder.Instance.GetInteractableSpawnCardFromPartial("VoidOutroPortal").prefab;
            }
            var position = args.senderBody.footPosition;
            // Some portals spawn into the ground
            switch (portal.name)
            {
                case "DeepVoidPortal":
                case "Encrypted":
                case "Decrypted":
                case "Mainline":
                case "Virtaul":
                    position.y += 4f;
                    break;
                case "PortalArtifactworld":
                    position.y += 10f;
                    break;
            }

            var gameObject = UnityEngine.Object.Instantiate(portal, position, Quaternion.LookRotation(args.senderBody.characterDirection.forward));
            var exit = gameObject.GetComponent<SceneExitController>();
            // The artifact portal erroneously points to mysteryspace by default
            if (portalName == "artifact")
            {
                int num = UnityEngine.Random.Range(0, 4);
                SceneDef sceneDef = SceneCatalog.FindSceneDef("artifactworld0" + num);
                if (sceneDef && sceneDef.requiredExpansion && Run.instance.IsExpansionEnabled(sceneDef.requiredExpansion))
                {
                    exit.destinationScene = sceneDef;
                }
                else
                {
                    exit.destinationScene = SceneCatalog.FindSceneDef("artifactworld");
                }
            }
            if (currentScene.cachedName == "voidraid" && gameObject.name.Contains("VoidOutroPortal"))
            {
                exit.useRunNextStageScene = false;
            }
            else
            {
                exit.useRunNextStageScene = exit.destinationScene == currentScene;
            }
            NetworkServer.Spawn(gameObject);
        }

        [ConCommand(commandName = "spawn_minion", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNMINION_HELP)]
        [AutoComplete(Lang.SPAWNMINION_ARGS)]
        private static void CCSpawnMinion(ConCommandArgs args)
        {
            CCSpawnCharacter(args, true);
        }
        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNAI_HELP)]
        [AutoComplete(Lang.SPAWNAI_ARGS)]
        private static void CCSpawnAI(ConCommandArgs args)
        {
            CCSpawnCharacter(args, false);
        }
        private static void CCSpawnCharacter(ConCommandArgs args, bool spawnAsMinion)
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

            var masterIndex = StringFinder.Instance.GetAiFromPartial(args[0]);
            if (masterIndex == MasterCatalog.MasterIndex.none)
            {
                Log.MessageNetworked(Lang.SPAWN_ERROR + args[0], args, LogLevel.MessageClientOnly);
                return;
            }
            var masterPrefab = MasterCatalog.GetMasterPrefab(masterIndex);

            int amount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }

            int droneTier = 0;
            EliteDef eliteDef = null;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
            {
                if (spawnAsMinion)
                {
                    if (!TextSerialization.TryParseInvariant(args[2], out droneTier))
                    {
                        Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "droneTier", "int"), args, LogLevel.MessageClientOnly);
                        return;
                    }
                    if (droneTier > 1 && Run.instance.IsItemExpansionLocked(DLC3Content.Items.DroneUpgradeHidden.itemIndex))
                    {
                        Log.MessageNetworked("Tiered Minions require Alloyed Collective", args, LogLevel.WarningClientOnly);
                    }
                    if (droneTier < 0)
                    {
                        Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "droneTier"), args, LogLevel.MessageClientOnly);
                        return;
                    }
                }
                else
                {
                    var eliteIndex = StringFinder.Instance.GetEliteFromPartial(args[2]);
                    if (eliteIndex == StringFinder.EliteIndex_NotFound)
                    {
                        Log.MessageNetworked(Lang.ELITE_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                    eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                    if (eliteDef && eliteDef.eliteEquipmentDef && Run.instance.IsEquipmentExpansionLocked(eliteDef.eliteEquipmentDef.equipmentIndex))
                    {
                        var expansion = Util.GetExpansion(eliteDef.eliteEquipmentDef.requiredExpansion);
                        Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "elite equipment", expansion), args, LogLevel.WarningClientOnly);
                    }
                }
            }
 
            bool braindead = false;
            if (args.Count > 3 && args[3] != Lang.DEFAULT_VALUE && !Util.TryParseBool(args[3], out braindead))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "braindead", "int or bool"), args, LogLevel.MessageClientOnly);
                return;
            }

         
            TeamIndex teamIndex = TeamIndex.Monster;
            if (args.Count > 4 && args[4] != Lang.DEFAULT_VALUE && !spawnAsMinion)
            {
                teamIndex = StringFinder.Instance.GetTeamFromPartial(args[4]);
                if (teamIndex == StringFinder.TeamIndex_NotFound)
                {
                    Log.MessageNetworked(Lang.TEAM_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            var spawnCard = StringFinder.Instance.GetDirectorCardFromPartial(masterPrefab.name)?.spawnCard;
            if (spawnCard == null)
            {
                spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                spawnCard.prefab = masterPrefab;
                spawnCard.sendOverNetwork = true;
                var body = spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab;
                if (body)
                {
                    spawnCard.nodeGraphType = (body.GetComponent<CharacterMotor>() == null
                        && (body.GetComponent<RigidbodyMotor>() != null || body.GetComponent<KinematicCharacterMotor>()))
                        ? MapNodeGroup.GraphType.Air
                        : MapNodeGroup.GraphType.Ground;
                }
            }
            var spawnRequest = new DirectorSpawnRequest(
                spawnCard,
                new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    position = args.senderBody.footPosition
                },
                RoR2Application.rng
            );
            spawnRequest.summonerBodyObject = spawnAsMinion ? args.senderBody.gameObject : null;
            spawnRequest.teamIndexOverride = spawnAsMinion ? args.senderBody.teamComponent.teamIndex : teamIndex;
            spawnRequest.ignoreTeamMemberLimit = true;

            // The size of the monster's radius is required so multiple enemies do not spawn on the same spot.
            // This prevents the player from clipping into the ground, or flyers flinging themselves away.
            var radius = 1f;
            var prefab = spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab;
            if (prefab)
            {
                var capsule = prefab.GetComponent<CapsuleCollider>();
                if (capsule)
                {
                    radius = capsule.radius;
                }
                else
                {
                    var sphere = prefab.GetComponent<SphereCollider>();
                    if (sphere)
                    {
                        radius = sphere.radius;
                    }
                }
            }
            // Just a hack for the Grandparent which still causes clipping otherwise
            if (prefab.name.Equals("GrandParentBody"))
            {
                radius = 0f;
            }

            var position = args.senderBody.footPosition + args.senderBody.transform.forward * (args.senderBody.radius + radius);
            var isFlyer = spawnCard.nodeGraphType == MapNodeGroup.GraphType.Air;
            if (isFlyer)
            {
                position = args.senderBody.transform.position;
                if (args.senderBody.characterMotor)
                {
                    position.y += 0.5f * args.senderBody.characterMotor.capsuleHeight + 2f;
                }
                radius *= Mathf.Max(1f, 0.5f * amount);
            }

            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_2, amount, masterPrefab.name), args);
            for (int i = 0; i < amount; i++)
            {
                var spawnPosition = position;
                if (isFlyer)
                {
                    var direction = Quaternion.AngleAxis(360f * ((float)i / amount), args.senderBody.transform.up) * args.senderBody.transform.forward;
                    spawnPosition = position + (direction * radius);
                }
                var masterGameObject = spawnCard.DoSpawn(spawnPosition, Quaternion.identity, spawnRequest).spawnedInstance;
                if (masterGameObject)
                {
                    CharacterMaster master = masterGameObject.GetComponent<CharacterMaster>();
                    if (eliteDef)
                    {
                        master.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentDef.equipmentIndex, false);
                        master.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, Mathf.RoundToInt((eliteDef.healthBoostCoefficient - 1) * 10));
                        master.inventory.GiveItemPermanent(RoR2Content.Items.BoostDamage, Mathf.RoundToInt(eliteDef.damageBoostCoefficient - 1) * 10);
                    }
                    if (braindead)
                    {
                        foreach (var ai in master.aiComponents)
                        {
                            UnityEngine.Object.Destroy(ai);
                        }
                        master.aiComponents = Array.Empty<BaseAI>();
                    }
                    if (droneTier > 1)
                    {
                        master.inventory.GiveItemPermanent(DLC3Content.Items.DroneUpgradeHidden, droneTier-1);
                    }
                }
            }
        }

        [ConCommand(commandName = "spawn_body", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNBODY_HELP)]
        [AutoComplete(Lang.SPAWNBODY_ARGS)]
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

            var bodyIndex = StringFinder.Instance.GetBodyFromPartial(args[0]);
            if (bodyIndex == BodyIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.SPAWN_ERROR, args[0]), args, LogLevel.MessageClientOnly);
                return;
            }

            GameObject body = BodyCatalog.GetBodyPrefab(bodyIndex);
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(body, args.senderBody.transform.position, Quaternion.identity);
            NetworkServer.Spawn(gameObject);
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_1, body.name), args);
        }

        internal static void InitPortals()
        {
            portals.Add("artifact", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArtifactworld/PortalArtifactworld.prefab").WaitForCompletion());
            portals.Add("blue", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalShop/PortalShop.prefab").WaitForCompletion());
            portals.Add("celestial", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalMS/PortalMS.prefab").WaitForCompletion());
            portals.Add("gold", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalGoldshores/PortalGoldshores.prefab").WaitForCompletion());
            portals.Add("null", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion());
            //DLC1
            portals.Add("void", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/PortalVoid/PortalVoid.prefab").WaitForCompletion());
            portals.Add("deepvoid", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DeepVoidPortal/DeepVoidPortal.prefab").WaitForCompletion());
            portals.Add("simulacrum", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GameModes/InfiniteTowerRun/PortalInfiniteTower.prefab").WaitForCompletion());
            //DLC2
            portals.Add("green", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/PortalColossus.prefab").WaitForCompletion());
            portals.Add("destination", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/PM DestinationPortal.prefab").WaitForCompletion());
            //DLC3
            portals.Add("encrypted", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/HardwareProgPortal.prefab").WaitForCompletion());
            portals.Add("decrypted", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/SolusWebPortal.prefab").WaitForCompletion());
            portals.Add("virtual", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/CompExchangePortal.prefab").WaitForCompletion());
            portals.Add("mainline", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/EyePortal/EyePortal.prefab").WaitForCompletion());
        }

        internal static CombatDirector.EliteTierDef GetTierDef(EliteDef eliteDef)
        {
            if (!eliteDef)
            {
                return CombatDirector.eliteTiers[0];
            }
            foreach (var eliteTier in CombatDirector.eliteTiers)
            {
                if (eliteTier != null)
                {
                    foreach (var thisEliteDef in eliteTier.eliteTypes)
                    {
                        if (thisEliteDef)
                        {
                            if (thisEliteDef == eliteDef)
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
