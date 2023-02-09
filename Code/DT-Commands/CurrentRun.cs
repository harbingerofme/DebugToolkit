using R2API.Utils;
using RoR2;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    public static class CurrentRun
    {

        internal static bool noEnemies = false;
        internal static bool lockExp = false;
        internal static ulong seed;

        internal static DirectorCard nextBoss;
        internal static int nextBossCount = 1;
        internal static EliteDef nextBossElite;

        [ConCommand(commandName = "add_portal", flags = ConVarFlags.ExecuteOnServer, helpText = "Add a portal to the current Teleporter on completion. " + Lang.ADDPORTAL_ARGS)]
        private static void CCAddPortal(ConCommandArgs args)
        {
            var portalName = args[0].ToLower();
            var teleporterInteraction = TeleporterInteraction.instance;
            if (!teleporterInteraction)
            {
                if (portalName != "arena" && portalName != "null" && portalName != "void")
                {
                    Log.MessageNetworked("No teleporter interaction instance! Can only spawn null portal", args, LogLevel.WarningClientOnly);
                    return;
                }
            }

            switch (portalName)
            {
                case "blue":
                    teleporterInteraction.shouldAttemptToSpawnShopPortal = true;
                    break;
                case "gold":
                    teleporterInteraction.shouldAttemptToSpawnGoldshoresPortal = true;
                    break;
                case "celestial":
                    teleporterInteraction.shouldAttemptToSpawnMSPortal = true;
                    break;
                case "deepvoid":
                    QueueDeepVoidPortal();
                    break;
                case "void":
                    QueueVoidPortal();
                    break;
                case "arena":
                case "null":
                    SpawnArenaPortal();
                    break;
                case "all":
                    teleporterInteraction.shouldAttemptToSpawnGoldshoresPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnShopPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnMSPortal = true;
                    SpawnArenaPortal();
                    QueueDeepVoidPortal();
                    QueueVoidPortal();
                    break;
                default:
                    Log.MessageNetworked(Lang.PORTAL_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
            }

            //the preview child offset will be inside of MSorb's, making it seem like it hasnt spawned.
            //see teleporter base mesh/builtineffects (or however its called) and change the orb position from here.
            void QueueVoidPortal() //the charging stage
            {
                PortalSpawner[] array = teleporterInteraction.portalSpawners;
                for (int i = 0; i < array.Length; i++)
                {
                    /* TBH: The portalSpawnCard check can likely be omitted because the teleporterInteraction seems
                     * to *always* spawn with the first PortalSpawner set to the void charging stage
                     *
                     */
                    if (array[i].portalSpawnCard == LegacyResourcesAPI.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscVoidPortal")
                        && array[i].previewChild
                        && array[i].previewChild.activeSelf == false) //False to make it not double run
                    {
                        var cachedSpawnChance = array[i].spawnChance;
                        var cachedMinStagesCleared = array[i].minStagesCleared;

                        array[i].spawnChance = 1;
                        array[i].minStagesCleared = 0;

                        array[i].Start();

                        // Does it even matter if I cache them?
                        array[i].spawnChance = cachedSpawnChance;
                        array[i].minStagesCleared = cachedMinStagesCleared;
                        break;
                    }
                }
            }

            //Can't be queued, unless an arena portal spawn card is made
            void SpawnArenaPortal()
            {
                var arenaPortal = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/PortalArena"), args.senderBody.corePosition, Quaternion.identity);
                arenaPortal.GetComponent<SceneExitController>().useRunNextStageScene = false;
                NetworkServer.Spawn(arenaPortal);
            }

            void QueueDeepVoidPortal()
            {
                PortalSpawner[] array = teleporterInteraction.portalSpawners;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].portalSpawnCard != LegacyResourcesAPI.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscDeepVoidPortal"))
                    {
                        var list = teleporterInteraction.portalSpawners.ToList();

                        var deepVoidPortalSpawner = teleporterInteraction.gameObject.AddComponent<PortalSpawner>();
                        deepVoidPortalSpawner.maxSpawnDistance = teleporterInteraction.portalSpawners[0].maxSpawnDistance;
                        deepVoidPortalSpawner.minSpawnDistance = teleporterInteraction.portalSpawners[0].minSpawnDistance;
                        deepVoidPortalSpawner.minStagesCleared = 0;
                        deepVoidPortalSpawner.portalSpawnCard = LegacyResourcesAPI.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscDeepVoidPortal");
                        deepVoidPortalSpawner.previewChild = null;
                        deepVoidPortalSpawner.previewChildName = null;
                        deepVoidPortalSpawner.requiredExpansion = teleporterInteraction.portalSpawners[0].requiredExpansion;
                        deepVoidPortalSpawner.rng = teleporterInteraction.portalSpawners[0].rng;
                        deepVoidPortalSpawner.spawnChance = 1;
                        deepVoidPortalSpawner.spawnMessageToken = "PORTAL_DEEPVOID_OPEN";
                        deepVoidPortalSpawner.spawnPreviewMessageToken = "PORTAL_DEEPVOID_WILL_OPEN";
                        list.Add(deepVoidPortalSpawner);

                        teleporterInteraction.portalSpawners = list.ToArray();
                        break;
                    }
                }
            }
        }

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = "Toggle Monster spawning. " + Lang.NOENEMIES_ARGS)]
        private static void CCNoEnemies(ConCommandArgs args)
        {
            noEnemies = !noEnemies;
            typeof(CombatDirector).GetFieldValue<RoR2.ConVar.BoolConVar>("cvDirectorCombatDisable").SetBool(noEnemies);
            if (noEnemies)
            {
                SceneDirector.onPrePopulateSceneServer += Hooks.OnPrePopulateSetMonsterCreditZero;
            }
            else
            {
                SceneDirector.onPrePopulateSceneServer -= Hooks.OnPrePopulateSetMonsterCreditZero;
            }
            Log.MessageNetworked("No_enemies set to " + CurrentRun.noEnemies, args);
        }

        [ConCommand(commandName = "lock_exp", flags = ConVarFlags.ExecuteOnServer, helpText = "Toggle Experience gain. " + Lang.LOCKEXP_ARGS)]
        private static void CCLockExperience(ConCommandArgs args)
        {
            lockExp = !lockExp;
            if (lockExp)
            {
                On.RoR2.ExperienceManager.AwardExperience += Hooks.DenyExperience;
            }
            else
            {
                On.RoR2.ExperienceManager.AwardExperience -= Hooks.DenyExperience;
            }
            Log.MessageNetworked("lock_exp set to " + CurrentRun.lockExp, args);
        }

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = "Kill all entities on the specified team. " + Lang.KILLALL_ARGS)]
        private static void CCKillAll(ConCommandArgs args)
        {
            TeamIndex team;
            if (args.Count ==  0 || args[0] == Lang.DEFAULT_VALUE)
            {
                team = TeamIndex.Monster;
            }
            else
            {
                team = StringFinder.GetEnumFromPartial<TeamIndex>(args[0]);
            }

            int count = 0;

            foreach (CharacterMaster cm in UnityEngine.Object.FindObjectsOfType<CharacterMaster>())
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

                TimescaleNet.Invoke(scale);
            }
            else
            {
                Log.Message(Lang.TIMESCALE_ARGS, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "force_family_event", flags = ConVarFlags.ExecuteOnServer, helpText = "Forces a family event to occur during the next stage. " + Lang.FAMILYEVENT_ARGS)]
        private static void CCFamilyEvent(ConCommandArgs args)
        {
            On.RoR2.DccsPool.GenerateWeightedSelection += Hooks.ForceFamilyEventForDccsPoolStages;
            On.RoR2.ClassicStageInfo.RebuildCards += Hooks.ForceFamilyEventForNonDccsPoolStages;

            Log.MessageNetworked("The next stage will contain a family event!", args);
        }

        [ConCommand(commandName = "next_boss", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets the next teleporter instance to the specified boss. " + Lang.NEXTBOSS_ARGS)]
        [AutoCompletion(typeof(StringFinder), "characterSpawnCard", "spawnCard", true)]
        private static void CCNextBoss(ConCommandArgs args)
        {
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
                                var eliteDef = int.TryParse(args[2], out var eliteIndex) ?
                                    EliteCatalog.GetEliteDef((EliteIndex)eliteIndex) :
                                    EliteCatalog.eliteDefs.FirstOrDefault(d => d.name.ToLowerInvariant().Contains(args[2].ToLowerInvariant()));
                                if (eliteDef)
                                {
                                    nextBossElite = eliteDef;
                                    s.Append("Elite: " + nextBossElite.name);
                                }
                                else
                                {
                                    s.Append($"Elite type {args[2]} not recognized");
                                }
                            }
                        }
                    }

                    // Unsub the last in case the user already used the command and want to change their mind.
                    On.RoR2.CombatDirector.SetNextSpawnAsBoss -= Hooks.CombatDirector_SetNextSpawnAsBoss;

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
        [AutoCompletion(typeof(SceneCatalog), "indexToSceneDef", "_cachedName")]
        private static void CCNextStage(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Run.instance.AdvanceStage(Run.instance.nextStageScene);
                Log.MessageNetworked("Stage advanced.", args);
                return;
            }

            string stageString = args[0];
            var def = Hooks.BetterSceneDefFinder(stageString);

            if (def)
            {
                Run.instance.AdvanceStage(def);
                Log.MessageNetworked($"Stage advanced to {stageString}.", args);
            }
            else
            {
                Log.MessageNetworked(Lang.NEXTROUND_STAGE, args, LogLevel.WarningClientOnly);
                DebugToolkit.InvokeCMD(args.sender, "scene_list");
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

        [ConCommand(commandName = "fixed_time", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets the run timer to the specified value. " + Lang.FIXEDTIME_ARGS)]
        private static void CCSetTime(ConCommandArgs args)
        {

            if (args.Count == 0)
            {
                Log.MessageNetworked("Run time is " + Run.instance.GetRunStopwatch().ToString(), args, LogLevel.MessageClientOnly);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float setTime))
            {
                Run.instance.SetRunStopwatch(setTime);
                ResetEnemyTeamLevel();
                Log.MessageNetworked("Run timer set to " + setTime, args);
            }
            else
            {
                Log.MessageNetworked(Lang.FIXEDTIME_ARGS, args, LogLevel.MessageClientOnly);
            }

        }

        private static void ResetEnemyTeamLevel()
        {
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBeMadeStatic.Local
    // ReSharper disable once UnusedMember.Local
    public class TimescaleNet : NetworkBehaviour
    {
        private static TimescaleNet _instance;

        private void Awake()
        {
            _instance = this;
        }

        internal static void Invoke(float scale)
        {
            _instance.RpcApplyTimescale(scale);
        }

        [ClientRpc]
        private void RpcApplyTimescale(float scale)
        {
            Time.timeScale = scale;
            Message("Timescale set to: " + scale + ". ");
        }
    }
}
