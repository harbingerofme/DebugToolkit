using RoR2;
using RoR2.Quests;
using RoR2.Quests.CompulsionQuests;
using RoR2.Quests.TrialQuests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    public static class CurrentRun
    {
        internal static bool noEnemies = false;
        internal static bool noInteractables = false;
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
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.ADDPORTAL_ARGS, 1))
            {
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
                case "VIRTUAL":
                    QueuePortalVirtual();
                    break;
                case "VOID":
                    QueuePortal("VOID");
                    break;
                case Lang.ALL:
                    teleporterInteraction.shouldAttemptToSpawnShopPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnMSPortal = true;
                    teleporterInteraction.shouldAttemptToSpawnGoldshoresPortal = true;
                    QueuePortal("GREEN");
                    QueuePortalVirtual();
                    QueuePortal("VOID");
                    break;
                default:
                    Log.MessageNetworked(string.Format(Lang.INVALID_ARG_VALUE, "portal"), args, LogLevel.MessageClientOnly);
                    return;
            }

            void QueuePortal(string portalName)
            {
                string spawnCardName;
                switch (portalName)
                {
                    case "GREEN":
                        spawnCardName = "iscColossusPortal";
                        break;
                    case "VOID":
                        spawnCardName = "iscVoidPortal";
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
                            Log.MessageNetworked(string.Format(Lang.PORTAL_EXPANSION_LOCKED, portalName.ToLower(), Util.GetExpansion(portal.requiredExpansion)), args, LogLevel.MessageClientOnly);
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
                        portal.Start();
                        return;
                    }
                }
            }

            void QueuePortalVirtual()
            {
                if (teleporterInteraction.shouldAttemptToSpawnHiddenRealmsPortal)
                {
                    // Already enabled
                    return;
                }
                var solusFight = UnityEngine.Object.FindObjectOfType<SolusFight>();
                if (solusFight == null)
                {
                    Log.MessageNetworked($"The virtual portal is not available on this stage.", args, LogLevel.MessageClientOnly);
                    return;
                }
                if (solusFight.RunHasRequiredExpansion() == false)
                {
                    Log.MessageNetworked(string.Format(Lang.PORTAL_EXPANSION_LOCKED, "virtual", Util.GetExpansion(DLC3Content.DLC3)), args, LogLevel.MessageClientOnly);
                    return;
                }
                if (Stage.instance.sceneDef.cachedName == "shipgraveyard")
                {
                    solusFight.TriggerSirensCallPortal();
                }
                else
                {
                    solusFight.TriggerServer();
                }
            }
        }

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NOENEMIES_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCNoEnemies(ConCommandArgs args)
        {
            if (!ArgumentParser.TryParseOptionalBool(args, 0, "enable", !noEnemies, out var enabled))
            {
                return;
            }
            noEnemies = enabled;
            CombatDirector.cvDirectorCombatDisable.SetBool(noEnemies);
            Log.MessageNetworked(String.Format(noEnemies ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "no_enemies"), args);
        }

        [ConCommand(commandName = "no_interactables", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NOINTERACTABLES_HELP)]
        [ConCommand(commandName = "no_interactibles", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NOINTERACTABLES_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCNoInteractables(ConCommandArgs args)
        {
            if (!ArgumentParser.TryParseOptionalBool(args, 0, "enable", !noInteractables, out var enabled))
            {
                return;
            }
            noInteractables = enabled;
            Log.MessageNetworked(String.Format(noInteractables ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, args.commandName), args);
        }

        [ConCommand(commandName = "lock_exp", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.LOCKEXP_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCLockExperience(ConCommandArgs args)
        {
            if (!ArgumentParser.TryParseOptionalBool(args, 0, "enable", !lockExp, out var enabled))
            {
                return;
            }
            lockExp = enabled;
            Log.MessageNetworked(String.Format(lockExp ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "lock_exp"), args);
        }

        [ConCommand(commandName = "kill", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.KILL_HELP)]
        [AutoComplete(Lang.KILL_ARGS)]
        private static void CCKill(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertNotServer(args) ||
                !ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.TryParseOptionalBool(args, 0, "true_kill", false, out var trueKill))
            {
                return;
            }

            var body = Hooks.GetPingedTarget(args.senderMaster).body;
            if (body == null)
            {
                Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            var targetName = body.master?.playerCharacterMasterController?.GetDisplayName() ?? body.gameObject.name;

            if (body.healthComponent.godMode)
            {
                Log.MessageNetworked($"Cannot kill {targetName} because they have god mode.", args);
                return;
            }

            if (trueKill)
            {
                if (body.master)
                {
                    body.master.TrueKill();
                }
                else
                {
                    // If there is no master, there are no reviving items. We still need to kill the body.
                    // In theory a pot with the reviving buff can reach this logic path, but there isn't
                    // much we can do unless we reinvent the wheel for TrueKill. Super niche anyway.
                    body.healthComponent.Suicide(null);
                }
            }
            else
            {
                body.healthComponent.Suicide(null);
            }
            Log.MessageNetworked($"Killed {targetName}.", args);
        }

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.KILLALL_HELP)]
        [AutoComplete(Lang.KILLALL_ARGS)]
        private static void CCKillAll(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }

            var teamMask = TeamMask.AllExcept(TeamIndex.Neutral);
            teamMask.RemoveTeam(TeamIndex.Player);
            var teamName = "enemies";
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && args[0].ToUpperInvariant() != Lang.ENEMIES)
            {
                if (!ArgumentParser.TryParseTeam(args, 0, out var teamIndex))
                {
                    return;
                }
                teamMask = TeamMask.none;
                teamMask.AddTeam(teamIndex);
                teamName = $"{teamIndex} characters";
            }

            if (!ArgumentParser.TryParseOptionalBool(args, 1, "true_kill", false, out var trueKill))
            {
                return;
            }

            int count = 0;
            for (TeamIndex teamIndex = 0; teamIndex < (TeamIndex)TeamCatalog.teamDefs.Length; teamIndex++)
            {
                if (teamMask.HasTeam(teamIndex))
                {
                    foreach (var teamComponent in TeamComponent.GetTeamMembers(teamIndex).ToList())
                    {
                        var healthComponent = teamComponent.GetComponent<HealthComponent>();
                        if (healthComponent && !healthComponent.godMode && healthComponent.alive)
                        {
                            if (trueKill)
                            {
                                if (healthComponent.body.master)
                                {
                                    healthComponent.body.master.TrueKill();
                                }
                                else
                                {
                                    // If there is no master, there are no reviving items. We still need to kill the body.
                                    // In theory a pot with the reviving buff can reach this logic path, but there isn't
                                    // much we can do unless we reinvent the wheel for TrueKill. Super niche anyway.
                                    healthComponent.Suicide(null);
                                }
                            }
                            else
                            {
                                healthComponent.Suicide(null);
                            }
                            if (!healthComponent.alive)
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            if (count == 1)
            {
                teamName = teamName == "enemies" ? "enemy" : teamName.TrimEnd('s');
            }
            Log.MessageNetworked($"Killed {count} {teamName}.", args);
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

            if (!ArgumentParser.TryParseOptionalFloat(args, 0, "time_scale", 1f, out var scale))
            {
                return;
            }
            Time.timeScale = scale;
            RunNet.InvokeTimescale(scale);
        }

        [ConCommand(commandName = "stop_timer", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.STOPTIMER_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCPauseTimer(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }

            var currentSceneDef = SceneCatalog.mostRecentSceneDef;
            var canPauseTimer = currentSceneDef.sceneType == SceneType.Stage || currentSceneDef.sceneType == SceneType.TimedIntermission;

            if (!canPauseTimer)
            {
                Log.MessageNetworked("The run timer can't be changed for this stage.", args, LogLevel.MessageClientOnly);
                return;
            }

            if (!ArgumentParser.TryParseOptionalBool(args, 0, "enable", !Run.instance.isRunStopwatchPaused, out var enabled))
            {
                return;
            }
            Run.instance.SetForcePauseRunStopwatch(enabled);
            Log.MessageNetworked(String.Format(Run.instance.isRunStopwatchPaused ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "Paused timer"), args);
        }

        [ConCommand(commandName = "force_family_event", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.FAMILYEVENT_HELP)]
        private static void CCFamilyEvent(ConCommandArgs args)
        {
            forceFamilyEvent = true;
            Log.MessageNetworked("The next stage will contain a family event if available!", args);
        }

        [ConCommand(commandName = "force_trial", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.FORCETRIAL_HELP)]
        [AutoComplete(Lang.FORCETRIAL_ARGS)]
        private static void CCForceTrial(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertRequiredArguments(args, Lang.FORCETRIAL_ARGS, 1))
            {
                return;
            }

            TrialController._preassignedVariantIndex = null;
            TrialController._preassignedVariantName = null;
            if (TextSerialization.TryParseInvariant(args[0], out int trialIndex))
            {
                TrialController._preassignedVariantIndex = trialIndex;
                Log.MessageNetworked($"The next trial stage will attempt to use the trial variant at index {trialIndex}.", args, LogLevel.MessageClientOnly);
                return;
            }
            else
            {
                TrialController._preassignedVariantName = args[0];
                Log.MessageNetworked($"The next trial stage will attempt to use the trial variant with partial name {trialIndex}.", args, LogLevel.MessageClientOnly);
                return;
            }
        }

        [ConCommand(commandName = "trial_timer", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.TRIALTIMER_HELP)]
        [AutoComplete(Lang.TRIALTIMER_ARGS)]
        private static void CCTrialTimer(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }
            if (!DLC4ProgressionController.instance || !DLC4ProgressionController.instance.trialSelector)
            {
                Log.MessageNetworked($"{Language.GetString(DLC4Content.DLC4.nameToken)} must be enabled for this.", args, LogLevel.MessageClientOnly);
                return;
            }

            if (args.Count == 0)
            {
                Log.MessageNetworked($"Trial time is {DLC4ProgressionController.instance.trialSelector.GetRemainingTrialTimeServer()}.", args, LogLevel.MessageClientOnly);
                return;
            }

            // Not optional technically
            if (!ArgumentParser.TryParseOptionalFloat(args, 0, "time", default, out var time, min: 0f))
            {
                return;
            }
            DLC4ProgressionController.instance.trialSelector.SetRemainingTrialTimeServer(time);
            var trialController = InstanceTracker.FirstOrNull<TrialController>();
            if (trialController && trialController.TryGetComponent<QuestRunner>(out var runner) && runner.quest is BaseTrialQuest trialQuest)
            {
                trialQuest.SpawnTrialTimersOnPlayersServer();
            }
            RunNet.InvokeTrialPortalTimerUpdate(time);
            Log.MessageNetworked($"Trial timer set to {time}.", args);
        }

        [ConCommand(commandName = "dump_trial_variants", flags = ConVarFlags.None, helpText = Lang.DUMPTRIALVARIANTS_HELP)]
        private static void CCDumpTrialVariants(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }
            var controller = InstanceTracker.FirstOrNull<TrialController>();
            if (controller)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < controller.trialVariants.Length; i++)
                {
                    sb.AppendLine($"[{i}]{controller.trialVariants[i].name}");
                }
                Log.MessageNetworked(sb.ToString().TrimEnd('\n'), args, LogLevel.MessageClientOnly);
                return;
            }
            Log.MessageNetworked("Not a trial stage.", args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "start_quest", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.STARTQUEST_HELP)]
        [AutoComplete(Lang.STARTQUEST_ARGS)]
        private static void CCStartQuest(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.STARTQUEST_ARGS, 1) ||
                !ArgumentParser.TryParseQuestOrRandom(args, 0, out var questIndex))
            {
                return;
            }

            if (!DLC4ProgressionController.instance)
            {
                Log.MessageNetworked($"{Language.GetString(DLC4Content.DLC4.nameToken)} must be enabled for this.", args, LogLevel.MessageClientOnly);
                return;
            }
            var questTracker = DLC4ProgressionController.instance.GetOrCreateQuestTracker();
            QuestCatalog.RegisteredQuest quest;
            if (questIndex == QuestIndex.None)
            {
                // Random quest
                var questSelection = questTracker._compulsionQuests;
                QuestCatalog.PopulateWeightedSelectionWithQuestsOfType<BaseCompulsionQuest>(questSelection, questTracker.questRunner);
                if (questSelection.Count == 0)
                {
                    Log.MessageNetworked($"No valid random compulsion quest was found.", args, LogLevel.MessageClientOnly);
                    return;
                }
                quest = questSelection.Evaluate(DLC4ProgressionController.instance.rng.nextNormalizedFloat);
            }
            else
            {
                quest = QuestCatalog.GetRegisteredQuest(questIndex).Value;
                // Random quests filter for validity, but for explicit ones we must check ourselves
                if (quest.canStartQuest(questTracker.questRunner) == false)
                {
                    Log.MessageNetworked($"Cannot start {quest.identifier}.", args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            questTracker.BeginQuest(args.senderBody, quest);
            Log.MessageNetworked($"Started {quest.identifier}.", args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "next_boss", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTBOSS_HELP)]
        [AutoComplete(Lang.NEXTBOSS_ARGS)]
        private static void CCNextBoss(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.NEXTBOSS_ARGS, 1) ||
                !ArgumentParser.TryParseDirectorCard(args, 0, out nextBoss) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out nextBossCount, min: 1, max: Run.instance is InfiniteTowerRun ? 10 : 6) ||
                !ArgumentParser.TryParseEliteOrDefault(args, 2, null, out nextBossElite))
            {
                ResetNextBoss();
                return;
            }

            string result;
            if (nextBossElite)
            {
                result = $"Next boss: {nextBoss.spawnCard.name}, count: {nextBossCount}, elite: {nextBossElite.name}.";
            }
            else
            {
                result = $"Next boss: {nextBoss.spawnCard.name}, count: {nextBossCount}.";
            }
            Log.MessageNetworked(result, args);
        }

        [ConCommand(commandName = "next_stage", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTSTAGE_HELP)]
        [AutoComplete(Lang.NEXTSTAGE_ARGS)]
        private static void CCNextStage(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }
            if (args.Count == 0)
            {
                Run.instance.AdvanceStage(Run.instance.nextStageScene);
                Log.MessageNetworked("Stage advanced.", args);
                return;
            }
            if (!ArgumentParser.TryParseScene(args, 0, false, out var sceneDef))
            {
                return;
            }
            Run.instance.AdvanceStage(sceneDef);
            Log.MessageNetworked($"Stage advanced to {sceneDef.cachedName}.", args);
        }

        [ConCommand(commandName = "next_wave", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NEXTWAVE_HELP)]
        private static void CCNextWave(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInSimulacrumARun(args))
            {
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
            if (!ArgumentParser.AssertInSimulacrumARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.RUNSETWAVESCLEARED_ARGS, 1) ||
                // Not optional technically
                !ArgumentParser.TryParseOptionalInt(args, 0, "wave", default, out var wave, min: 0))
            {
                return;
            }
            var run = Run.instance as InfiniteTowerRun;
            run.Network_waveIndex = wave;
        }

        [ConCommand(commandName = "force_wave", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.FORCEWAVE_HELP)]
        [AutoComplete(Lang.FORCEWAVE_ARGS)]
        private static void CCForceWave(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInSimulacrumARun(args))
            {
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
                    Log.MessageNetworked("Selected " + kvp.Key, args);
                    return;
                }
            }
            Log.MessageNetworked("Wave prefab not found. You can choose from: " + string.Join(", ", waves.Keys), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "reload_stage", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RELOADSTAGE_HELP)]
        private static void CCReloadStage(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }
            var networkManager = UnityEngine.Networking.NetworkManager.singleton;
            var currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            if (networkManager && currentScene)
            {
                networkManager.ServerChangeScene(currentScene.cachedName);
            }
        }

        [ConCommand(commandName = "charge_zone", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CHARGEZONE_HELP)]
        [AutoComplete(Lang.CHARGEZONE_ARGS)]
        private static void CCChargeZone(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.TryParseOptionalFloat(args, 0, "charge", float.MaxValue, out var charge))
            {
                return;
            }

            var isMaxCharge = charge == float.MaxValue;
            charge /= 100f;

            foreach (var zone in InstanceTracker.GetInstancesList<HoldoutZoneController>())
            {
                charge = isMaxCharge ? zone.maxCharge : charge;
                zone.charge = charge;
                // Trigger the onCharged event manually, since Pillars of Soul discharging means the
                // zone will be deactivated without updating the mission tracker successfully.
                var charged = charge >= zone.maxCharge;
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
                if (teleporterInteraction && charge < zone.maxCharge)
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

        [ConCommand(commandName = "evolve_lemurians", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.EVOLVELEMURIANS_HELP)]
        private static void CCEvolveLemurians(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }
            var isDevotionArtifactEnabled = DevotionInventoryController.isDevotionEnable;
            if (!isDevotionArtifactEnabled)
            {
                DevotionInventoryController.OnDevotionArtifactEnabled(RunArtifactManager.instance, CU8Content.Artifacts.Devotion);
            }
            // Temporarily initialise the elite evolution lists if the artifact is disabled.
            DevotionInventoryController.ActivateAllDevotedEvolution();
            if (!isDevotionArtifactEnabled)
            {
                DevotionInventoryController.OnDevotionArtifactDisabled(RunArtifactManager.instance, CU8Content.Artifacts.Devotion);
            }
            if (DevotionInventoryController.InstanceList.Count > 0)
            {
                Log.MessageNetworked($"Evolved all Devoted Lemurians.", args);
                return;
            }
            Log.MessageNetworked($"No Devoted Lemurians found.", args);
        }

        [ConCommand(commandName = "set_artifact", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SETARTIFACT_HELP)]
        [AutoComplete(Lang.SETARTIFACT_ARGS)]
        private static void CCSetArtifact(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SETARTIFACT_ARGS, 1) ||
                // We parse the artifact later since its logic is more complex.
                // The enable argument's value may also be overriden later so we use a dummy default.
                !ArgumentParser.TryParseOptionalBool(args, 1, "enable", default, out var enabled))
            {
                return;
            }
            if (args[0].ToUpperInvariant() == Lang.ALL && args.Count < 2)
            {
                Log.MessageNetworked("The 'enable' argument is required when using 'all'.", args, LogLevel.MessageClientOnly);
                return;
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
                if (!ArgumentParser.TryParseArtifact(args, 0, out var artifactDef))
                {
                    return;
                }
                if (args.Count < 2)
                {
                    enabled = !RunArtifactManager.instance.IsArtifactEnabled(artifactDef);
                }
                if (RunArtifactManager.instance.IsArtifactEnabled(artifactDef) == enabled)
                {
                    Log.MessageNetworked("Nothing happened", args);
                    return;
                }
                RunArtifactManager.instance.SetArtifactEnabled(artifactDef, enabled);
                if (artifactDef == RoR2Content.Artifacts.SingleMonsterType && Stage.instance)
                {
                    if (!enabled)
                    {
                        Stage.instance.singleMonsterTypeBodyIndex = BodyIndex.None;
                    }
                    RoR2.UI.EnemyInfoPanel.RefreshAll();
                }
                else if (artifactDef == RoR2Content.Artifacts.MixEnemy)
                {
                    if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.SingleMonsterType))
                    {
                        RoR2.UI.EnemyInfoPanel.RefreshAll();
                    }
                }
                Log.MessageNetworked(String.Format(enabled ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, artifactDef.cachedName), args);
            }
        }

        [ConCommand(commandName = "set_difficulty", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SETDIFFICULTY_HELP)]
        [AutoComplete(Lang.SETDIFFICULTY_ARGS)]
        private static void CCSetDifficulty(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.SETDIFFICULTY_ARGS, 1) ||
                !ArgumentParser.TryParseDifficulty(args, 0, out var difficultyIndex))
            {
                return;
            }

            if (Run.instance.selectedDifficulty == difficultyIndex)
            {
                Log.MessageNetworked("The difficulty remained unchanged.", args);
                return;
            }

            var difficultyDef = StringFinder.difficultyDefs[difficultyIndex];

            // Ensure proper Helper items for vanilla difficulties.
            // Some minions and player ghosts/doppelgangers inherit these items,
            // but we ignore them because they're ephemeral spawns.
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                player.master.inventory.ResetItemPermanent(RoR2Content.Items.DrizzlePlayerHelper);
                player.master.inventory.ResetItemPermanent(RoR2Content.Items.MonsoonPlayerHelper);
                if (difficultyIndex == DifficultyIndex.Easy)
                {
                    player.master.inventory.GiveItemPermanent(RoR2Content.Items.DrizzlePlayerHelper, 1);
                }
                else if (difficultyDef.countsAsHardMode)
                {
                    player.master.inventory.GiveItemPermanent(RoR2Content.Items.MonsoonPlayerHelper, 1);
                }
            }

            Run.instance.selectedDifficulty = difficultyIndex;
            Log.MessageNetworked($"Difficulty changed to {StringFinder.GetLangInvar(difficultyDef.nameToken)}.", args);

            // There's nothing we can do for clients that don't have DebugToolkit installed.
            RunNet.InvokeHudDifficultyUpdate(difficultyIndex);

            // A modded difficulty may initialise special functionality at the beginning of the run,
            // which is bypassed here, so a warning is warranted.
            if (difficultyIndex < 0 || (int)difficultyIndex > DifficultyCatalog.difficultyDefs.Length)
            {
                Log.MessageNetworked("Changing to a modded difficulty in the middle of a run may have unintended consequences.", args, LogLevel.Warning);
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

            // Not optional technically
            if (!ArgumentParser.TryParseOptionalULong(args, 0, "new_seed", default, out ulong result))
            {
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
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }
            if (args.Count == 0)
            {
                Log.MessageNetworked($"Run time is {Run.instance.GetRunStopwatch()}.", args, LogLevel.MessageClientOnly);
                return;
            }

            // Not optional technically
            if (!ArgumentParser.TryParseOptionalFloat(args, 0, "time", default, out var setTime, min: 0f))
            {
                return;
            }
            Run.instance.SetRunStopwatch(setTime);
            Log.MessageNetworked($"Run timer set to {setTime}.", args);
        }

        [ConCommand(commandName = "karma", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.KARMA_HELP)]
        private static void CCSetKarma(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }

            if (args.Count > 0)
            {
                if (!ArgumentParser.TryParseOptionalInt(args, 0, "karma", default, out var karma, max: KarmaManager.maxKarma))
                {
                    return;
                }
                var setKarmaEnabled = karma >= 0;
                if (setKarmaEnabled != KarmaManager.instance.isKarmaEnabled)
                {
                    KarmaManager.instance.NetworkisKarmaEnabled = setKarmaEnabled;
                    // There's nothing we can do for clients that don't have DebugToolkit installed.
                    RunNet.InvokeHudKarmaUpdate(setKarmaEnabled);
                }
                KarmaManager.instance.SetKarmaServer(karma);
            }

            var s = KarmaManager.instance.isKarmaEnabled ? $"Current karma is {KarmaManager.instance.effectiveKarma}." : "Karma is disabled.";
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
            return;
        }

        [ConCommand(commandName = "dump_karma_triggers", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.DUMPKARMATRIGGERS_HELP)]
        private static void CCDumpKarmaTriggers(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args))
            {
                return;
            }

            var sb = new StringBuilder();
            foreach (var trigger in KarmaManager.karmaTriggers)
            {
                string status = "";
                if (KarmaManager.instance.IsRunUsingTrigger(trigger.karmaTriggerIndex))
                {
                    var karmaInstance = KarmaManager.instance.FindInstancedTrigger(trigger.karmaTriggerIndex);
                    if (karmaInstance.isComplete)
                    {
                        status = "<style=cIsHealing>completed</style>";
                    }
                    else
                    {
                        status = karmaInstance.IsRepeatable() ? "<style=cIsUtility>active, repeatable</style>" : "<style=cIsUtility>active</style>";
                    }
                }
                else
                {
                    status = "<style=cDeath>inactive</style>";
                }
                sb.AppendLine($"[{trigger.karmaTriggerIndex.intValue}] {trigger.identifier} ({status})");
            }

            Log.MessageNetworked(sb.ToString().TrimEnd('\n'), args, LogLevel.MessageClientOnly);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBeMadeStatic.Local
    // ReSharper disable once UnusedMember.Local
    public class RunNet : NetworkBehaviour
    {
        private static RunNet _instance;

        private void Awake()
        {
            _instance = this;
        }

        internal static void InvokeTimescale(float scale)
        {
            _instance.RpcApplyTimescale(scale);
        }

        [ClientRpc]
        private void RpcApplyTimescale(float scale)
        {
            Time.timeScale = scale;
            Message($"Timescale set to {scale}.");
        }

        internal static void InvokeHudDifficultyUpdate(DifficultyIndex difficultyIndex)
        {
            _instance.RpcUpdateHudDifficulty(difficultyIndex);
        }

        [ClientRpc]
        private void RpcUpdateHudDifficulty(DifficultyIndex difficultyIndex)
        {
            if (Run.instance && StringFinder.difficultyDefs.TryGetValue(difficultyIndex, out var difficultyDef))
            {
                foreach (var ui in Run.instance.uiInstances)
                {
                    var controller = ui.GetComponentInChildren<RoR2.UI.CurrentDifficultyIconController>();
                    if (controller && controller.TryGetComponent<Image>(out var image))
                    {
                        image.sprite = difficultyDef.GetIconSprite();
                    }
                }
            }
        }

        internal static void InvokeHudKarmaUpdate(bool enabled)
        {
            _instance.RpcUpdateHudKarma(enabled);
        }

        [ClientRpc]
        private void RpcUpdateHudKarma(bool enabled)
        {
            if (Run.instance)
            {
                foreach (var ui in Run.instance.uiInstances)
                {
                    var icon = ui.GetComponentInChildren<RoR2.UI.KarmaIcon>();
                    if (icon && icon.holder)
                    {
                        icon.holder.gameObject.SetActive(enabled);
                    }
                }
            }
        }

        internal static void InvokeTrialPortalTimerUpdate(float seconds)
        {
            _instance.RpcUpdateTrialPortalTimer(seconds);
        }

        [ClientRpc]
        private void RpcUpdateTrialPortalTimer(float seconds)
        {
            var sceneExitControllers = InstanceTracker.GetInstancesList<SceneExitController>();
            foreach (var controller in sceneExitControllers)
            {
                var timeProvider = controller.GetComponentInChildren<TrialTimeProvider>();
                if (timeProvider)
                {
                    timeProvider.timerText.seconds = seconds;
                }
            }
        }
    }
}
