using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [ConCommand(commandName = "charge_zone", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CHARGEZONE_HELP)]
        [AutoComplete(Lang.CHARGEZONE_ARGS)]
        private static void CCChargeZone(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.TryParseOptionalFloat(args, 0, "charge", 100f, out var charge))
            {
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
            RunNet.InvokeHudUpdate(difficultyIndex);

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

        internal static void InvokeHudUpdate(DifficultyIndex difficultyIndex)
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
    }
}
