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
        private static readonly Dictionary<string, GameObject> portals = new Dictionary<string, GameObject>();

        [ConCommand(commandName = "spawn_interactable", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNINTERACTABLE_HELP)]
        [ConCommand(commandName = "spawn_interactible", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNINTERACTABLE_HELP)]
        [AutoComplete(Lang.SPAWNINTERACTABLE_ARGS)]
        private static void CCSpawnInteractable(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertNotServer(args) ||
                !ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertLivingBody(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SPAWNINTERACTABLE_ARGS, 1) ||
                !ArgumentParser.TryParseInteractableCard(args, 0, out var isc))
            {
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
            var result = isc.DoSpawn(position, new Quaternion(), new DirectorSpawnRequest(isc, null, RoR2Application.rng));
            if (!result.success)
            {
                Log.MessageNetworked("Failed to spawn interactable.", args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "spawn_portal", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNPORTAL_HELP)]
        [AutoComplete(Lang.SPAWNPORTAL_ARGS)]
        private static void CCSpawnPortal(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertNotServer(args) ||
                !ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertLivingBody(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SPAWNPORTAL_ARGS, 1))
            {
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
            if (portal.name == "DeepVoidPortal")
            {
                position.y += 4f;
            }
            else if (portal.name == "PortalArtifactworld")
            {
                position.y += 10f;
            }

            var gameObject = UnityEngine.Object.Instantiate(portal, position, Quaternion.LookRotation(args.senderBody.characterDirection.forward));
            var exit = gameObject.GetComponent<SceneExitController>();
            // The artifact portal erroneously points to mysteryspace by default
            if (portalName == "artifact")
            {
                exit.destinationScene = SceneCatalog.FindSceneDef("artifactworld");
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

        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNAI_HELP)]
        [AutoComplete(Lang.SPAWNAI_ARGS)]
        private static void CCSpawnAI(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertNotServer(args) ||
                !ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertLivingBody(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SPAWNAI_ARGS, 1) ||
                !ArgumentParser.TryParseMaster(args, 0, out var masterPrefab) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out var count, min: 1) ||
                !ArgumentParser.TryParseEliteOrDefault(args, 2, null, out var eliteDef) ||
                !ArgumentParser.TryParseOptionalBool(args, 3, "braindead", false, out var braindead))
            {
                return;
            }

            var isAlly = false;
            TeamIndex teamIndex;
            var teamArgumentIndex = 4;
            if (args.Count > teamArgumentIndex && string.Equals(args[teamArgumentIndex], Lang.ALLY, StringComparison.InvariantCultureIgnoreCase))
            {
                isAlly = true;
                teamIndex = args.senderBody.teamComponent.teamIndex;
            }
            else if (!ArgumentParser.TryParseTeamOrDefault(args, teamArgumentIndex, TeamIndex.Monster, out teamIndex))
            {
                return;
            }

            var spawnCard = StringFinder.Instance.GetDirectorCardFromPartial(masterPrefab.name)?.spawnCard;
            if (spawnCard == null)
            {
                spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                spawnCard.prefab = masterPrefab;
                spawnCard.sendOverNetwork = true;
                var body = spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab;
                spawnCard.nodeGraphType = GetBodyPrefabGraphType(body);
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
            spawnRequest.summonerBodyObject = isAlly ? args.senderBody.gameObject : null;
            spawnRequest.teamIndexOverride = teamIndex;
            spawnRequest.ignoreTeamMemberLimit = true;

            var isFlyer = spawnCard.nodeGraphType == MapNodeGroup.GraphType.Air;
            GetSpawnPosition(masterPrefab, args.senderBody, isFlyer, count, out var position, out var radius);

            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_2, count, masterPrefab.name), args);
            for (int i = 0; i < count; i++)
            {
                var spawnPosition = position;
                if (isFlyer)
                {
                    var direction = Quaternion.AngleAxis(360f * ((float)i / count), args.senderBody.transform.up) * args.senderBody.transform.forward;
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
                }
            }
        }

        [ConCommand(commandName = "spawn_body", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNBODY_HELP)]
        [AutoComplete(Lang.SPAWNBODY_ARGS)]
        private static void CCSpawnBody(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertNotServer(args) ||
                !ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertLivingBody(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SPAWNBODY_ARGS, 1) ||
                !ArgumentParser.TryParseBody(args, 0, out var bodyPrefab))
            {
                return;
            }
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(bodyPrefab, args.senderBody.transform.position, Quaternion.identity);
            NetworkServer.Spawn(gameObject);
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_1, bodyPrefab.name), args);
        }

        [ConCommand(commandName = "spawn_drone", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNDRONE_HELP)]
        [AutoComplete(Lang.SPAWNDRONE_ARGS)]
        private static void CCSpawnDrone(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertNotServer(args) ||
                !ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertLivingBody(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SPAWNDRONE_ARGS, 1) ||
                !ArgumentParser.TryParseDrone(args, 0, out var droneDef) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out var amount, min: 0) ||
                !ArgumentParser.TryParseOptionalInt(args, 2, "tier", 0, out var tier, min: 0))
            {
                return;
            }

            var isFlyer = GetBodyPrefabGraphType(droneDef.masterPrefab.GetComponent<CharacterMaster>().bodyPrefab) == MapNodeGroup.GraphType.Air;
            GetSpawnPosition(droneDef.masterPrefab, args.senderBody, isFlyer, amount, out var position, out var radius);

            Log.MessageNetworked($"Spawned {amount} tier {tier} {droneDef.masterPrefab.name}.", args);
            for (int i = 0; i < amount; i++)
            {
                var spawnPosition = position;
                if (isFlyer)
                {
                    var direction = Quaternion.AngleAxis(360f * ((float)i / amount), args.senderBody.transform.up) * args.senderBody.transform.forward;
                    spawnPosition = position + (direction * radius);
                }
                var masterGameObject = new MasterSummon
                {
                    masterPrefab = droneDef.masterPrefab,
                    position = spawnPosition,
                    rotation = Quaternion.identity,
                    summonerBodyObject = args.senderBody.gameObject,
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true,
                    enablePrintController = true
                }.Perform();
                if (masterGameObject)
                {
                    if (masterGameObject.TryGetComponent<CharacterMaster>(out var master))
                    {
                        master.inventory.GiveItemPermanent(DLC3Content.Items.DroneUpgradeHidden, tier);
                    }
                }
            }
        }

        [ConCommand(commandName = "spawn_lemurian", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNLEMURIAN_HELP)]
        [AutoComplete(Lang.SPAWNLEMURIAN_ARGS)]
        private static void CCSpawnLemurian(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertNotServer(args) ||
                !ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertLivingBody(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SPAWNLEMURIAN_ARGS, 1) ||
                !ArgumentParser.TryParseItem(args, 0, out var itemDef) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "level", 0, out var level, min: 0))
            {
                return;
            }

            var masterPrefab = MasterCatalog.GetMasterPrefab(MasterCatalog.FindMasterIndex("DevotedLemurianMaster"));
            // If the level is high enough to transform to the Elder Lemurian, there can be issues if we use
            // master.TransformBody later. Therefore, we bypass this by directly spawning the Elder Lemurian now and
            // reverting the prefab change before we're done. Incidentally, this also ensures we calculate the correct
            // spawning distance so the Elder Lemurian doesn't transform on top of us if it starts as the small version.
            if (level > 1)
            {
                masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = CU8Content.BodyPrefabs.DevotedLemurianBruiserBody.gameObject;
            }
            GetSpawnPosition(masterPrefab, args.senderBody, false, 1, out var position, out _);

            Log.MessageNetworked($"Spawned a level {level} Devoted Lemurian with {itemDef.name}.", args);
            var masterGameObject = new MasterSummon
            {
                masterPrefab = masterPrefab,
                position = position,
                rotation = Quaternion.identity,
                summonerBodyObject = args.senderBody.gameObject,
                ignoreTeamMemberLimit = true,
                useAmbientLevel = true
            }.Perform();

            if (level > 1)
            {
                masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = CU8Content.BodyPrefabs.DevotedLemurianBody.gameObject;
            }

            if (masterGameObject)
            {
                // Temporarily initialise the elite evolution lists if the artifact is disabled.
                var isDevotionArtifactEnabled = DevotionInventoryController.isDevotionEnable;
                if (!isDevotionArtifactEnabled)
                {
                    DevotionInventoryController.OnDevotionArtifactEnabled(RunArtifactManager.instance, CU8Content.Artifacts.Devotion);
                }
                var devotionInventoryController = DevotionInventoryController.GetOrCreateDevotionInventoryController(args.senderBody.GetComponent<Interactor>());
                devotionInventoryController.GiveItem(itemDef.itemIndex, 1 + level);
                var devotedLemurianController = masterGameObject.GetComponent<DevotedLemurianController>();
                devotedLemurianController.InitializeDevotedLemurian(itemDef.itemIndex, devotionInventoryController);
                devotedLemurianController.DevotedEvolutionLevel = level;
                // We must implement `DevotionInventoryController.EvolveDevotedLumerian` manually, because the
                // elite equipment is given via body.inventory, but the body has not been linked to the master yet.
                // We have also taken care of the body transformation earlier in the method.
                {
                    List<EquipmentIndex> eliteUpgradeList = null;
                    if (level == 1)
                    {
                        eliteUpgradeList = DevotionInventoryController.lowLevelEliteBuffs;
                    }
                    else if (level >= 3)
                    {
                        eliteUpgradeList = DevotionInventoryController.highLevelEliteBuffs;
                    }
                    if (eliteUpgradeList != null && eliteUpgradeList.Count > 0)
                    {
                        int index = UnityEngine.Random.Range(0, eliteUpgradeList.Count);
                        masterGameObject.GetComponent<Inventory>().SetEquipmentIndex(eliteUpgradeList[index], isRemovingEquipment: false);
                    }
                }
                devotionInventoryController.UpdateAllMinions(false);
                if (!isDevotionArtifactEnabled)
                {
                    DevotionInventoryController.OnDevotionArtifactDisabled(RunArtifactManager.instance, CU8Content.Artifacts.Devotion);
                }
            }
        }

        internal static void InitPortals()
        {
            portals.Add("artifact", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArtifactworld/PortalArtifactworld.prefab").WaitForCompletion());
            portals.Add("blue", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalShop/PortalShop.prefab").WaitForCompletion());
            portals.Add("celestial", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalMS/PortalMS.prefab").WaitForCompletion());
            portals.Add("deepvoid", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DeepVoidPortal/DeepVoidPortal.prefab").WaitForCompletion());
            portals.Add("gold", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalGoldshores/PortalGoldshores.prefab").WaitForCompletion());
            portals.Add("green", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/PortalColossus.prefab").WaitForCompletion());
            portals.Add("null", Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion());
            portals.Add("void", Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/PortalVoid/PortalVoid.prefab").WaitForCompletion());
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

        private static void GetSpawnPosition(GameObject masterPrefab, CharacterBody spawnerBody, bool isFlyer, int amount, out Vector3 position, out float radius)
        {
            // The size of the monster's radius is required so multiple enemies do not spawn on the same spot.
            // This prevents the player from clipping into the ground, or flyers flinging themselves away.
            radius = 1f;
            var prefab = masterPrefab.GetComponent<CharacterMaster>().bodyPrefab;
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

            position = spawnerBody.footPosition + spawnerBody.transform.forward * (spawnerBody.radius + radius);
            if (isFlyer)
            {
                position = spawnerBody.transform.position;
                if (spawnerBody.characterMotor)
                {
                    position.y += 0.5f * spawnerBody.characterMotor.capsuleHeight + 2f;
                }
                radius *= Mathf.Max(1f, 0.5f * amount);
            }
        }

        private static MapNodeGroup.GraphType GetBodyPrefabGraphType(GameObject bodyPrefab)
        {
            if (bodyPrefab)
            {
                if (bodyPrefab.GetComponent<CharacterMotor>())
                {
                    return MapNodeGroup.GraphType.Ground;
                }
                if (bodyPrefab.GetComponent<RigidbodyMotor>() != null || bodyPrefab.GetComponent<KinematicCharacterMotor>())
                {
                    return MapNodeGroup.GraphType.Air;
                }
                // If it's lacking all of the above, it's an immobile ground body.
            }
            return MapNodeGroup.GraphType.Ground;
        }
    }
}
