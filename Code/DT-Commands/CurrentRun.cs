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

        [ConCommand(commandName = "add_portal", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.ADDPORTAL_HELP)]
        private static void CCAddPortal(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.ADDPORTAL_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
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

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NOENEMIES_HELP)]
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
            Log.MessageNetworked("no_enemies set to " + CurrentRun.noEnemies, args);
        }

        [ConCommand(commandName = "lock_exp", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.LOCKEXP_HELP)]
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

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.KILLALL_HELP)]
        private static void CCKillAll(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            TeamIndex team = TeamIndex.Monster;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && !StringFinder.TryGetEnumFromPartial(args[0], out team))
            {
                Log.MessageNetworked(Lang.TEAM_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            int count = 0;
            foreach (CharacterMaster cm in UnityEngine.Object.FindObjectsOfType<CharacterMaster>())
            {
                if (cm.teamIndex == team)
                {
                    CharacterBody cb = cm.GetBody();
                    if (cb && cb.healthComponent)
                    {
                        cb.healthComponent.Suicide(null);
                        count++;
                    }
                }
            }
            Log.MessageNetworked($"Killed {count} of team {team}.", args);
        }

        [ConCommand(commandName = "time_scale", flags = ConVarFlags.Engine | ConVarFlags.ExecuteOnServer, helpText = Lang.TIMESCALE_HELP)]
        private static void CCTimeScale(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Time.timeScale.ToString(), args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out float scale))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "time_scale", "float"), args, LogLevel.MessageClientOnly);
                return;
            }
            Time.timeScale = scale;
            TimescaleNet.Invoke(scale);
        }

        [ConCommand(commandName = "force_family_event", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.FAMILYEVENT_HELP)]
        private static void CCFamilyEvent(ConCommandArgs args)
        {
            On.RoR2.DccsPool.GenerateWeightedSelection += Hooks.ForceFamilyEventForDccsPoolStages;
            On.RoR2.ClassicStageInfo.RebuildCards += Hooks.ForceFamilyEventForNonDccsPoolStages;

            Log.MessageNetworked("The next stage will contain a family event!", args);
        }

        [ConCommand(commandName = "next_boss", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTBOSS_HELP)]
        [AutoCompletion(typeof(StringFinder), "characterSpawnCard", "spawnCard", true)]
        private static void CCNextBoss(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.NEXTBOSS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            StringBuilder s = new StringBuilder();
            nextBoss = StringFinder.Instance.GetDirectorCardFromPartial(args[0]);
            if (nextBoss == null)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0], args, LogLevel.MessageClientOnly);
                return;
            }
            s.AppendLine($"Next boss is: {nextBoss.spawnCard.name}. ");
            if (args.Count > 1)
            {
                if (!TextSerialization.TryParseInvariant(args[1], out int nextBossCount))
                {
                    Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                    return;
                }
                if (nextBossCount > 6)
                {
                    nextBossCount = 6;
                    Log.MessageNetworked("'count' is capped at 6.", args, LogLevel.WarningClientOnly);
                }
                else if (nextBossCount <= 0)
                {
                    nextBossCount = 1;
                    Log.MessageNetworked("'count' must be non-zero positive.", args, LogLevel.WarningClientOnly);
                }
                s.Append($"Count:  {nextBossCount}. ");

                if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
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
                        Log.MessageNetworked(Lang.ELITE_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                }
            }

            // Unsub the last in case the user already used the command and want to change their mind.
            On.RoR2.CombatDirector.SetNextSpawnAsBoss -= Hooks.CombatDirector_SetNextSpawnAsBoss;

            On.RoR2.CombatDirector.SetNextSpawnAsBoss += Hooks.CombatDirector_SetNextSpawnAsBoss;
            Log.MessageNetworked(s.ToString(), args);
        }

        [ConCommand(commandName = "next_stage", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTSTAGE_HELP)]
        [AutoCompletion(typeof(SceneCatalog), "indexToSceneDef", "_cachedName")]
        private static void CCNextStage(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
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
                Log.MessageNetworked(Lang.STAGE_NOTFOUND, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "seed", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SEED_HELP)]
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
            if (!TextSerialization.TryParseInvariant(args[0], out ulong result))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "new_seed", "ulong"), args, LogLevel.MessageClientOnly);
                return;
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

        [ConCommand(commandName = "fixed_time", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.FIXEDTIME_HELP)]
        private static void CCSetTime(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked("Run time is " + Run.instance.GetRunStopwatch().ToString(), args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out float setTime))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "time", "float"), args, LogLevel.MessageClientOnly);
                return;
            }
            Run.instance.SetRunStopwatch(setTime);
            ResetEnemyTeamLevel();
            Log.MessageNetworked("Run timer set to " + setTime, args);
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
