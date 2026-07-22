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

            EliteDef eliteDef = null;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
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

            bool braindead = false;
            if (args.Count > 3 && args[3] != Lang.DEFAULT_VALUE && !Util.TryParseBool(args[3], out braindead))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "braindead", "int or bool"), args, LogLevel.MessageClientOnly);
                return;
            }

            bool isAlly = false;
            TeamIndex teamIndex = TeamIndex.Monster;
            if (args.Count > 4 && args[4] != Lang.DEFAULT_VALUE)
            {
                if (args[4].ToUpperInvariant() == Lang.ALLY)
                {
                    isAlly = true;
                    teamIndex = args.senderBody.teamComponent.teamIndex;
                }
                else
                {
                    teamIndex = StringFinder.Instance.GetTeamFromPartial(args[4]);
                    if (teamIndex == StringFinder.TeamIndex_NotFound)
                    {
                        Log.MessageNetworked(Lang.TEAM_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                }
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
            GetSpawnPosition(masterPrefab, args.senderBody, isFlyer, amount, out var position, out var radius);

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

        [ConCommand(commandName = "spawn_drone", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNDRONE_HELP)]
        [AutoComplete(Lang.SPAWNDRONE_ARGS)]
        private static void CCSpawnDrone(ConCommandArgs args)
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
            if (args.Count < 1)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SPAWNDRONE_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't spawn an object with relation to a dead target.", args, LogLevel.MessageClientOnly);
                return;
            }

            var drone = StringFinder.Instance.GetDroneFromPartial(args[0]);
            if (drone == DroneIndex.None)
            {
                Log.MessageNetworked(Lang.SPAWN_ERROR + args[0], args, LogLevel.MessageClientOnly);
                return;
            }
            var droneDef = DroneCatalog.GetDroneDef(drone);

            int amount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }

            int tier = 0;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[2], out tier))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "tier", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (tier < 0)
            {
                tier = 0;
                Log.MessageNetworked("'tier' cannot be negative. Resetting to 0.", args, LogLevel.WarningClientOnly);
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
