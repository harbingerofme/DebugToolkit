﻿using System;
using System.Linq;
using System.Text;
using DebugToolkit.Code;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    public static class CurrentRun
    {

        internal static bool noEnemies = false;
        internal static ulong seed;

        internal static DirectorCard nextBoss;
        internal static int nextBossCount = 1;
        internal static EliteIndex nextBossElite = EliteIndex.None;

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
                    case "null":
                    case "void":
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
                    var arenaPortal = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("prefabs\\networkedobjects\\portalarena"), args.senderBody.corePosition, Quaternion.identity);
                    arenaPortal.GetComponent<SceneExitController>().useRunNextStageScene = false;
                    NetworkServer.Spawn(arenaPortal);
                }
            }
            else
            {
                Log.MessageNetworked("No teleporter instance!", args, LogLevel.WarningClientOnly);
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
            On.RoR2.ClassicStageInfo.RebuildCards += Hooks.ForceFamilyEvent;
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
            var sceneNames = SceneCatalog.allSceneDefs.Select(sceneDef => sceneDef.baseSceneName).ToList();

            if (sceneNames.Contains(stageString))
            {
                Run.instance.AdvanceStage(SceneCatalog.GetSceneDefFromSceneName(stageString));
                Log.MessageNetworked($"Stage advanced to {stageString}.", args);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine(Lang.NEXTROUND_STAGE);
                sceneNames.ForEach(str => sb.AppendLine(str));
                Log.MessageNetworked(sb.ToString(), args);
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
