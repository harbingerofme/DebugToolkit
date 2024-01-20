using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
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
        internal static GameObject selectedWavePrefab;

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
            var teleporterInteraction = TeleporterInteraction.instance;
            if (!teleporterInteraction)
            {
                Log.MessageNetworked("No teleporter interaction instance!", args, LogLevel.WarningClientOnly);
                return;
            }

            var portalName = args[0].ToUpperInvariant();
            switch (portalName)
            {
                case "BLUE":
                    teleporterInteraction.shouldAttemptToSpawnShopPortal = true;
                    break;
                case "GOLD":
                    teleporterInteraction.shouldAttemptToSpawnGoldshoresPortal = true;
                    break;
                case "CELESTIAL":
                    teleporterInteraction.shouldAttemptToSpawnMSPortal = true;
                    break;
                case "VOID":
                    QueueVoidPortal();
                    break;
                case Lang.ALL:
                    teleporterInteraction.shouldAttemptToSpawnShopPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnMSPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnGoldshoresPortal = true;
                    QueueVoidPortal();
                    break;
                default:
                    Log.MessageNetworked(string.Format(Lang.INVALID_ARG_VALUE, "portal"), args, LogLevel.MessageClientOnly);
                    return;
            }

            void QueueVoidPortal()
            {
                foreach (var portal in teleporterInteraction.portalSpawners)
                {
                    if (portal.portalSpawnCard.name == "iscVoidPortal"
                        && portal.previewChild
                        && portal.previewChild.activeSelf == false) //False to make it not double run
                    {
                        if (portal.requiredExpansion && !Run.instance.IsExpansionEnabled(portal.requiredExpansion))
                        {
                            Log.MessageNetworked("The void portal requires an expansion to be enabled.", args, LogLevel.MessageClientOnly);
                            return;
                        }
                        if (!string.IsNullOrEmpty(portal.bannedEventFlag) && Run.instance.GetEventFlag(portal.bannedEventFlag))
                        {
                            Log.MessageNetworked("The void portal cannot spawn in this game mode.", args, LogLevel.MessageClientOnly);
                            return;
                        }
                        portal.spawnChance = 1f;
                        portal.minStagesCleared = 0;
                        // The portal's orb copies the celestial portal's starting location, therefore
                        // making this invisible if they are spawned at the same time. This fix makes
                        // all orbs equidistant now (as intended?), but if the developers add new
                        // orbs later, we may have to review whether we are intefering with anything.
                        portal.previewChild.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
                        portal.Start();
                        return;
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
        [AutoCompletion(typeof(TeamIndex))]
        private static void CCKillAll(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            TeamIndex team = TeamIndex.Monster;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE)
            {
                team = StringFinder.Instance.GetTeamFromPartial(args[0]);
                if (team == StringFinder.TeamIndex_NotFound)
                {
                    Log.MessageNetworked(Lang.TEAM_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            int count = 0;
            foreach (var teamComponent in TeamComponent.GetTeamMembers(team).ToList())
            {
                var healthComponent = teamComponent.GetComponent<HealthComponent>();
                if (healthComponent)
                {
                    healthComponent.Suicide(null);
                    if (!healthComponent.alive)
                    {
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
            On.RoR2.ClassicStageInfo.RebuildCards -= Hooks.ForceFamilyEvent;
            On.RoR2.ClassicStageInfo.RebuildCards += Hooks.ForceFamilyEvent;
            Log.MessageNetworked("The next stage will contain a family event if available!", args);
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
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "director card", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            s.AppendLine($"Next boss is: {nextBoss.spawnCard.name}. ");

            nextBossCount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                if (!TextSerialization.TryParseInvariant(args[1], out int count))
                {
                    Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                    return;
                }
                var spawnLimit = Run.instance is InfiniteTowerRun ? 10 : 6;
                if (count > spawnLimit)
                {
                    count = spawnLimit;
                    Log.MessageNetworked($"'count' is capped at {spawnLimit}.", args, LogLevel.WarningClientOnly);
                }
                else if (nextBossCount <= 0)
                {
                    count = 1;
                    Log.MessageNetworked("'count' must be non-zero positive. Reseting to 1.", args, LogLevel.WarningClientOnly);
                }
                nextBossCount = count;
                s.Append($"Count:  {nextBossCount}. ");
            }

            nextBossElite = null;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
            {
                var eliteIndex = StringFinder.Instance.GetEliteFromPartial(args[2]);
                if (eliteIndex == StringFinder.EliteIndex_NotFound)
                {
                    Log.MessageNetworked(Lang.ELITE_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                var eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (eliteDef)
                {
                    nextBossElite = eliteDef;
                    s.Append("Elite: " + nextBossElite.name);
                }
            }

            // Unsub the last in case the user already used the command and want to change their mind.
            Hooks.UndoNextBossHooks();
            Hooks.ApplyNextBossHooks();
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

        [ConCommand(commandName = "next_wave", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTWAVE_HELP)]
        private static void CCNextWave(ConCommandArgs args)
        {
            if (!Run.instance || !(Run.instance is InfiniteTowerRun))
            {
                Log.MessageNetworked(Lang.NOTINASIMULACRUMRUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            var run = Run.instance as InfiniteTowerRun;
            if (run.waveInstance && run.waveController && !run.waveController.isFinished)
            {
                run.waveController.combatDirector.totalCreditsSpent = run.waveController.totalWaveCredits;
                run.waveController.KillSquad();
            }
        }

        [ConCommand(commandName = "run_set_waves_cleared", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RUNSETWAVESCLEARED_HELP)]
        private static void CCRunSetWavesCleared(ConCommandArgs args)
        {
            if (!Run.instance || !(Run.instance is InfiniteTowerRun))
            {
                Log.MessageNetworked(Lang.NOTINASIMULACRUMRUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.RUNSETWAVESCLEARED_ARGS, args, LogLevel.ErrorClientOnly);
                return;
            }
            if (!TextSerialization.TryParseInvariant(args[0], out int wave))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "wave", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (wave < 0)
            {
                wave = 0;
                Log.MessageNetworked("'wave' must be positive. Reseting to 0.", args, LogLevel.WarningClientOnly);
            }
            var run = Run.instance as InfiniteTowerRun;
            run.Network_waveIndex = wave;
        }

        [ConCommand(commandName = "force_wave", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.FORCEWAVE_HELP)]
        private static void CCForceWave(ConCommandArgs args)
        {
            if (!Run.instance || !(Run.instance is InfiniteTowerRun))
            {
                Log.MessageNetworked(Lang.NOTINASIMULACRUMRUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            var run = Run.instance as InfiniteTowerRun;
            var waves = new Dictionary<string, GameObject>();
            foreach (var category in run.waveCategories)
            {
                foreach (var wave in category.wavePrefabs)
                {
                    var name = wave.wavePrefab.name;
                    name = name.Replace("InfiniteTowerWave", "").Replace("Artifact", "");
                    waves[name] = wave.wavePrefab;
                }
            }
            selectedWavePrefab = null;
            if (args.Count == 0)
            {
                Log.MessageNetworked("You can choose from: " + string.Join(", ", waves.Keys), args, LogLevel.MessageClientOnly);
                return;
            }
            var waveName = args[0].ToLowerInvariant();
            foreach (var kvp in waves)
            {
                if (kvp.Key.ToLowerInvariant().Contains(waveName))
                {
                    selectedWavePrefab = kvp.Value;
                    Log.MessageNetworked("Selected " + kvp.Key, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            Log.MessageNetworked("Wave prefab not found. You can choose from: " + string.Join(", ", waves.Keys), args, LogLevel.MessageClientOnly);
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
            Log.MessageNetworked("Run timer set to " + setTime, args);
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
