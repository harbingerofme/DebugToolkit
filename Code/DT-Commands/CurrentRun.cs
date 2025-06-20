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

        internal static bool forceFamilyEvent = false;

        internal static DirectorCard nextBoss;
        internal static int nextBossCount = 1;
        internal static EliteDef nextBossElite;
        internal static GameObject selectedWavePrefab;

        internal static void ResetNextBoss()
        {
            nextBoss = null;
            nextBossCount = 0;
            nextBossElite = null;
        }

        [ConCommand(commandName = "add_portal", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.ADDPORTAL_HELP)]
        [AutoComplete(Lang.ADDPORTAL_ARGS)]
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
                case "GREEN":
                    QueuePortal("GREEN");
                    break;
                case "CELESTIAL":
                    teleporterInteraction.shouldAttemptToSpawnMSPortal = true;
                    break;
                case "VOID":
                    QueuePortal("VOID");
                    break;
                case Lang.ALL:
                    teleporterInteraction.shouldAttemptToSpawnShopPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnMSPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnGoldshoresPortal = true;
                    QueuePortal("GREEN");
                    QueuePortal("VOID");
                    break;
                default:
                    Log.MessageNetworked(string.Format(Lang.INVALID_ARG_VALUE, "portal"), args, LogLevel.MessageClientOnly);
                    return;
            }

            void QueuePortal(string portalName)
            {
                string spawnCardName;
                // Fix the initial spawning position of an orb in case it overlaps with another.
                // For example, the void portal copies the celestial portal's starting location
                // and it is extremely likely they forgot to use a unique value. This should be
                // revisited when new orbs are added to ensure no interference.
                Quaternion? rotation = null;
                switch (portalName)
                {
                    case "GREEN":
                        spawnCardName = "iscColossusPortal";
                        break;
                    case "VOID":
                        spawnCardName = "iscVoidPortal";
                        rotation = Quaternion.Euler(0f, 0f, 270f);
                        break;
                    default:
                        Log.MessageNetworked(Lang.NOMESSAGE, args, LogLevel.MessageClientOnly);
                        return;
                }

                foreach (var portal in teleporterInteraction.portalSpawners)
                {
                    if (portal.portalSpawnCard.name == spawnCardName
                        && portal.previewChild
                        && portal.previewChild.activeSelf == false) //False to make it not double run
                    {
                        if (portal.requiredExpansion && !Run.instance.IsExpansionEnabled(portal.requiredExpansion))
                        {
                            Log.MessageNetworked($"The {portalName.ToLower()} portal requires an expansion to be enabled.", args, LogLevel.MessageClientOnly);
                            return;
                        }
                        if (!string.IsNullOrEmpty(portal.bannedEventFlag) && Run.instance.GetEventFlag(portal.bannedEventFlag))
                        {
                            Log.MessageNetworked($"The {portalName.ToLower()} portal cannot spawn in this game mode.", args, LogLevel.MessageClientOnly);
                            return;
                        }
                        portal.spawnChance = 1f;
                        portal.minStagesCleared = 0;
                        portal.validStages = [];
                        portal.invalidStages = [];
                        portal.validStageTiers = [];
                        if (rotation != null)
                        {
                            portal.previewChild.transform.localRotation = rotation.Value;
                        }
                        portal.Start();
                        return;
                    }
                }
            }
        }

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NOENEMIES_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCNoEnemies(ConCommandArgs args)
        {
            bool enabled = !noEnemies;
            if (args.Count > 0)
            {
                if (!Util.TryParseBool(args[0], out enabled))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "enable", "bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            noEnemies = enabled;
            CombatDirector.cvDirectorCombatDisable.SetBool(noEnemies);
            if (noEnemies)
            {
                On.RoR2.CombatDirector.SpendAllCreditsOnMapSpawns += Hooks.DenyMapSpawns;
            }
            else
            {
                On.RoR2.CombatDirector.SpendAllCreditsOnMapSpawns -= Hooks.DenyMapSpawns;
            }
            Log.MessageNetworked(String.Format(noEnemies ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "no_enemies"), args);
        }

        [ConCommand(commandName = "lock_exp", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.LOCKEXP_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCLockExperience(ConCommandArgs args)
        {
            bool enabled = !lockExp;
            if (args.Count > 0)
            {
                if (!Util.TryParseBool(args[0], out enabled))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "enable", "bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            lockExp = enabled;
            if (lockExp)
            {
                On.RoR2.ExperienceManager.AwardExperience += Hooks.DenyExperience;
            }
            else
            {
                On.RoR2.ExperienceManager.AwardExperience -= Hooks.DenyExperience;
            }
            Log.MessageNetworked(String.Format(lockExp ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "lock_exp"), args);
        }

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.KILLALL_HELP)]
        [AutoComplete(Lang.KILLALL_ARGS)]
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
        [AutoComplete(Lang.TIMESCALE_ARGS)]
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

        [ConCommand(commandName = "stop_timer", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.STOPTIMER_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCPauseTimer(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var currentSceneDef = SceneCatalog.mostRecentSceneDef;
            var canPauseTimer = currentSceneDef.sceneType == SceneType.Stage || currentSceneDef.sceneType == SceneType.TimedIntermission;

            if (!canPauseTimer)
            {
                Log.MessageNetworked("The run timer can't be changed for this stage.", args, LogLevel.MessageClientOnly);
                return;
            }

            bool enabled = !Run.instance.isRunStopwatchPaused;
            if (args.Count > 0)
            {
                if (!Util.TryParseBool(args[0], out enabled))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "enable", "bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            Run.instance.SetForcePauseRunStopwatch(enabled);
            Log.MessageNetworked(String.Format(Run.instance.isRunStopwatchPaused ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "Paused timer"), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "force_family_event", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.FAMILYEVENT_HELP)]
        private static void CCFamilyEvent(ConCommandArgs args)
        {
            forceFamilyEvent = true;
            Log.MessageNetworked("The next stage will contain a family event if available!", args);
        }

        [ConCommand(commandName = "next_boss", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTBOSS_HELP)]
        [AutoComplete(Lang.NEXTBOSS_ARGS)]
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
                    ResetNextBoss();
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
                    ResetNextBoss();
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
            Log.MessageNetworked(s.ToString(), args);
        }

        [ConCommand(commandName = "next_stage", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTSTAGE_HELP)]
        [AutoComplete(Lang.NEXTSTAGE_ARGS)]
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

            var sceneIndex = StringFinder.Instance.GetSceneFromPartial(args[0], false);
            if (sceneIndex == SceneIndex.Invalid)
            {
                Log.MessageNetworked(Lang.STAGE_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            var def = SceneCatalog.GetSceneDef(sceneIndex);
            if (def.requiredExpansion != null && !Run.instance.IsExpansionEnabled(def.requiredExpansion))
            {
                Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "scene", Util.GetExpansion(def.requiredExpansion)), args, LogLevel.MessageClientOnly);
                return;
            }
            Run.instance.AdvanceStage(def);
            Log.MessageNetworked($"Stage advanced to {def.cachedName}.", args);
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
        [AutoComplete(Lang.RUNSETWAVESCLEARED_ARGS)]
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
        [AutoComplete(Lang.FORCEWAVE_ARGS)]
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

        [ConCommand(commandName = "charge_zone", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CHARGEZONE_HELP)]
        [AutoComplete(Lang.CHARGEZONE_ARGS)]
        private static void CCChargeZone(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.CHARGEZONE_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!TextSerialization.TryParseInvariant(args[0], out float charge))
            {
                Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "charge", "float"), args, LogLevel.MessageClientOnly);
                return;
            }
            charge /= 100f;

            foreach (var zone in InstanceTracker.GetInstancesList<HoldoutZoneController>())
            {
                zone.charge = charge;
                // Trigger the onCharged event manually, since Pillars of Soul discharging means the
                // zone will be deactivated without updating the mission tracker successfully.
                var charged = charge >= 1f;
                if (charged && zone.wasCharged != charged && zone.onCharged != null)
                {
                    zone.wasCharged = charged;
                    zone.onCharged.Invoke(zone);
                }
                // For the teleporter the zone is not deactivated at full charge with the boss
                // still alive, so we can reduce the charge again. However, we must manually
                // reactivate the combat director related to it.
                // This also means that if a mod subscribes an event which removes itself
                // upon getting triggered, we have no way of resubscribing it.
                var teleporterInteraction = zone.GetComponent<TeleporterInteraction>();
                if (teleporterInteraction && charge < 1f)
                {
                    teleporterInteraction.bonusDirector.enabled = true;
                }
                // The zone recreates the Lepton Daisy generators when the zone toggles
                // "isCharging" for each team. Therefore, if the player is charging the
                // teleporter and reduces the charge, the heal won't be triggered when
                // crossing a previously triggered threshold unless the player leaves and
                // reenters the zone.
                foreach (var novaGenerator in zone.healingNovaGeneratorsByTeam)
                {
                    if (novaGenerator && novaGenerator.TryGetComponent<EntityStateMachine>(out var esm))
                    {
                        var state = esm.state as EntityStates.TeleporterHealNovaController.TeleporterHealNovaGeneratorMain;
                        if (state != null)
                        {
                            state.previousPulseFraction = charge;
                        }
                    }
                }
            }
        }

        [ConCommand(commandName = "set_artifact", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SETARTIFACT_HELP)]
        [AutoComplete(Lang.SETARTIFACT_ARGS)]
        private static void CCSetArtifact(ConCommandArgs args)
        {
            if (!Run.instance || !RunArtifactManager.instance) // the manager check is superfluous but just in case
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count < 1)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SETARTIFACT_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args[0].ToUpperInvariant() == Lang.ALL && args.Count < 2)
            {
                Log.MessageNetworked("The 'enable' argument is required when using 'all'", args, LogLevel.MessageClientOnly);
                return;
            }
            var enabled = false;
            if (args.Count > 1)
            {
                if (!Util.TryParseBool(args[1], out enabled))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "enable", "int or bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            if (args[0].ToUpperInvariant() == Lang.ALL)
            {
                // Toggling Evolution triggers a UI refresh to update the Kin monster
                var willRefresh = RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.MonsterTeamGainsItems) != enabled;
                foreach (var artifact in ArtifactCatalog.artifactDefs)
                {
                    if (!artifact.requiredExpansion || Run.instance.IsExpansionEnabled(artifact.requiredExpansion))
                    {
                        RunArtifactManager.instance.SetArtifactEnabled(artifact, enabled);
                    }
                }
                // Cleaning up after Kin because the game won't
                if (!enabled && Stage.instance)
                {
                    Stage.instance.singleMonsterTypeBodyIndex = BodyIndex.None;
                }
                if (!willRefresh)
                {
                    RoR2.UI.EnemyInfoPanel.RefreshAll();
                }
                Log.MessageNetworked(String.Format(enabled ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "All artifacts"), args);
            }
            else
            {
                var artifactIndex = StringFinder.Instance.GetArtifactFromPartial(args[0]);
                if (artifactIndex == ArtifactIndex.None)
                {
                    Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "artifact", args[0]), args, LogLevel.MessageClientOnly);
                    return;
                }
                var artifact = ArtifactCatalog.GetArtifactDef(artifactIndex);
                if (artifact.requiredExpansion && !Run.instance.IsExpansionEnabled(artifact.requiredExpansion))
                {
                    Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "artifact", Util.GetExpansion(artifact.requiredExpansion)), args, LogLevel.MessageClientOnly);
                    return;
                }
                if (args.Count < 2)
                {
                    enabled = !RunArtifactManager.instance.IsArtifactEnabled(artifact);
                }
                if (RunArtifactManager.instance.IsArtifactEnabled(artifact) == enabled)
                {
                    Log.MessageNetworked("Nothing happened", args);
                    return;
                }
                RunArtifactManager.instance.SetArtifactEnabled(artifact, enabled);
                if (artifact == RoR2Content.Artifacts.SingleMonsterType && Stage.instance)
                {
                    if (!enabled)
                    {
                        Stage.instance.singleMonsterTypeBodyIndex = BodyIndex.None;
                    }
                    RoR2.UI.EnemyInfoPanel.RefreshAll();
                }
                else if (artifact == RoR2Content.Artifacts.MixEnemy)
                {
                    if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.SingleMonsterType))
                    {
                        RoR2.UI.EnemyInfoPanel.RefreshAll();
                    }
                }
                Log.MessageNetworked(String.Format(enabled ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, artifact.cachedName), args);
            }
        }

        [ConCommand(commandName = "seed", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SEED_HELP)]
        [AutoComplete(Lang.SEED_ARGS)]
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
        [AutoComplete(Lang.FIXEDTIME_ARGS)]
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
