using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class PlayerCommands
    {
        [ConCommand(commandName = "god", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GOD_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCGodModeToggle(ConCommandArgs args)
        {
            bool modeOn;
            if (args.Count > 0)
            {
                if (!Util.TryParseBool(args[0], out modeOn))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "enable", "bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            else
            {
                modeOn = Hooks.ToggleGod();
            }
            foreach (var playerInstance in PlayerCharacterMasterController.instances)
            {
                playerInstance.master.godMode = modeOn;
                playerInstance.master.UpdateBodyGodMode();
            }
            Log.MessageNetworked(String.Format(modeOn ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "God mode"), args);
        }

        [ConCommand(commandName = "buddha", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        [ConCommand(commandName = "budha", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        [ConCommand(commandName = "buda", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        [ConCommand(commandName = "budda", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.BUDDHA_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCBuddhaModeToggle(ConCommandArgs args)
        {
            bool modeOn;
            if (args.Count > 0)
            {
                if (!Util.TryParseBool(args[0], out modeOn))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "enable", "bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            else
            {
                modeOn = Hooks.ToggleBuddha();
            }
            Log.MessageNetworked(String.Format(modeOn ? Lang.SETTING_ENABLED : Lang.SETTING_DISABLED, "Buddha mode"), args);
        }

        [ConCommand(commandName = "noclip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.NOCLIP_HELP)]
        [AutoComplete(Lang.ENABLE_ARGS)]
        private static void CCNoclip(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool enabled = !Command_Noclip.IsActivated;
            if (args.Count > 0)
            {
                if (!Util.TryParseBool(args[0], out enabled))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "enable", "bool"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            NoclipNet.Invoke(args.sender, enabled); // callback
        }

        [ConCommand(commandName = "teleport_on_cursor", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CURSORTELEPORT_HELP)]
        private static void CCCursorTeleport(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!args.senderBody)
            {
                Log.MessageNetworked("Can't teleport while you're dead. " + Lang.USE_RESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }
            TeleportNet.Invoke(args.sender); // callback
        }

        [ConCommand(commandName = "spawn_as", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.SPAWNAS_HELP)]
        [AutoComplete(Lang.SPAWNAS_ARGS)]
        private static void CCSpawnAs(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.sender == null && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.SPAWNAS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            var bodyIndex = StringFinder.Instance.GetBodyFromPartial(args[0]);
            if (bodyIndex == BodyIndex.None)
            {
                Log.MessageNetworked(Lang.BODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            GameObject newBody = BodyCatalog.GetBodyPrefab(bodyIndex);

            CharacterMaster master = args.senderMaster;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                master = player.master;
            }

            var expansion = newBody.GetComponent<ExpansionRequirementComponent>();
            if (expansion && !expansion.PlayerCanUseBody(master.playerCharacterMasterController))
            {
                Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "body", Util.GetExpansion(expansion.requiredExpansion)), args, LogLevel.MessageClientOnly);
                return;
            }

            master.bodyPrefab = newBody;
            Log.MessageNetworked(args.sender.userName + " is spawning as " + newBody.name, args);

            if (!master.GetBody())
            {
                Log.MessageNetworked(Lang.PLAYER_DEADRESPAWN, args);
                return;
            }

            master.TransformBody(newBody.name);
        }

        [ConCommand(commandName = "respawn", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RESPAWN_HELP)]
        [AutoComplete(Lang.RESPAWN_ARGS)]
        private static void CCRespawnPlayer(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null && (args.Count < 1 || args[0] == Lang.DEFAULT_VALUE))
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.RESPAWN_ARGS, LogLevel.Error);
                return;
            }
            CharacterMaster master = args.senderMaster;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                master = player.master;
            }

            var body = master.GetBody() ?? master.bodyPrefab.GetComponent<CharacterBody>();
            var position = master.hasBody ? master.GetBody().footPosition : master.deathFootPosition;
            var rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            if (DirectorCore.instance)
            {
                position = Run.instance.FindSafeTeleportPositionSimplified(body.hullClassification, position);
            }
            else
            {
                position = Vector3.zero;
                var spawnPoint = Stage.instance.GetPlayerSpawnTransform();
                if (spawnPoint)
                {
                    position = spawnPoint.position;
                    rotation = spawnPoint.rotation;
                }
            }
            master.Respawn(position, rotation);
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_1, master.playerCharacterMasterController.GetDisplayName()), args);
        }

        [ConCommand(commandName = "hurt", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.HURT_HELP)]
        [AutoComplete(Lang.HURT_ARGS)]
        private static void CCHurt(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.sender == null && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.HURT_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            var target = Buffs.ParseTarget(args, 1);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!TextSerialization.TryParseInvariant(args[0], out float amount))
            {
                Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "value", "float"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (amount < 0f)
            {
                Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "value"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (amount == 0f)
            {
                Log.MessageNetworked("Nothing happened.", args, LogLevel.MessageClientOnly);
                return;
            }
            target.body.healthComponent.TakeDamage(new DamageInfo()
            {
                damage = amount,
                position = target.body.corePosition,
            });
            Log.MessageNetworked($"Damaged {target.name} for {amount} hp.", args);
        }

        [ConCommand(commandName = "heal", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.HEAL_HELP)]
        [AutoComplete(Lang.HEAL_ARGS)]
        private static void CCHeal(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.sender == null && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.HEAL_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            var target = Buffs.ParseTarget(args, 1);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!TextSerialization.TryParseInvariant(args[0], out float amount))
            {
                Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "amount", "float"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (amount < 0f)
            {
                Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "amount"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (amount == 0f)
            {
                Log.MessageNetworked("Nothing happened.", args, LogLevel.MessageClientOnly);
                return;
            }
            target.body.healthComponent.Heal(amount, default);
            Log.MessageNetworked($"Healed {target.name} for {amount} hp.", args);
        }

        [ConCommand(commandName = "change_team", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CHANGETEAM_HELP)]
        [AutoComplete(Lang.CHANGETEAM_HELP)]
        private static void CCChangeTeam(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.sender == null && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.CHANGETEAM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            CharacterMaster master = args.sender?.master;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
                master = player.master;
            }
            if (!master.GetBody())
            {
                Log.MessageNetworked("Can't change a dead player's team. " + Lang.USE_RESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }
            var teamIndex = StringFinder.Instance.GetTeamFromPartial(args[0]);
            if (teamIndex == StringFinder.TeamIndex_NotFound)
            {
                Log.MessageNetworked(Lang.TEAM_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            master.GetBody().teamComponent.teamIndex = teamIndex;
            master.teamIndex = teamIndex;
            Log.MessageNetworked("Changed to team " + teamIndex, args);
        }

        [ConCommand(commandName = "dump_stats", flags = ConVarFlags.None, helpText = Lang.DUMPSTATS_HELP)]
        [AutoComplete(Lang.DUMPSTATS_ARGS)]
        private static void CCDumpStats(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.DUMPSTATS_ARGS, args, LogLevel.ErrorClientOnly);
                return;
            }
            var bodyIndex = StringFinder.Instance.GetBodyFromPartial(args[0]);
            if (bodyIndex == BodyIndex.None)
            {
                Log.MessageNetworked(Lang.BODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            var body = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Body: {body.name}");
            sb.AppendLine($"Health: {body.baseMaxHealth} ({FormatLevelStat(body.levelMaxHealth)}/level)");
            sb.AppendLine($"Regen: {body.baseRegen} ({FormatLevelStat(body.levelRegen)}/level)");
            sb.AppendLine($"Damage: {body.baseDamage} ({FormatLevelStat(body.levelDamage)}/level)");
            sb.AppendLine($"Armor: {body.baseArmor}");
            sb.AppendLine($"Speed: {body.baseMoveSpeed}");
            sb.AppendLine($"Acceleration: {body.baseAcceleration}");
            sb.AppendLine($"Sprinting Modifier: {body.sprintingSpeedMultiplier}");
            sb.AppendLine($"Crit: {body.baseCrit}");
            sb.AppendLine($"Attack speed: {body.baseAttackSpeed}");
            sb.AppendLine($"Jump Count: {body.baseJumpCount}");
            sb.AppendLine($"Jump Power: {body.baseJumpPower}");
            sb.AppendLine($"Vision Distance: {body.baseVisionDistance}");
            sb.Append($"Body Flags: {body.bodyFlags}");
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        private static string FormatLevelStat(float value)
        {
            // Done so the value is formatted properly as +value or -value, e.g., Heretic negative regen
            return value.ToString("+0.##;-0.##;+0");
        }

        [ConCommand(commandName = "dump_state", flags = ConVarFlags.None, helpText = Lang.DUMPSTATE_HELP)]
        [AutoComplete(Lang.DUMPSTATE_ARGS)]
        private static void CCDumpState(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count < 1 && isDedicatedServer)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.DUMPSTATE_ARGS, args, LogLevel.ErrorClientOnly);
                return;
            }

            CharacterMaster target = args.senderMaster;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args, 0);
                if (target == null && !isDedicatedServer && args[0].ToUpperInvariant() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            var body = target.GetBody();
            var healthComponent = body.GetComponent<HealthComponent>();
            var sb = new System.Text.StringBuilder();
            var suffix = "(Clone)";
            sb.AppendLine($"Body: {(body.name.EndsWith(suffix) ? body.name.Substring(0, body.name.Length - suffix.Length) : body.name)}");
            sb.AppendLine($"Level: {body.level}");
            sb.AppendLine($"Health: {healthComponent.health}/{body.maxHealth} ({body.regen} regen/sec)");
            sb.AppendLine($"Shield: {healthComponent.shield}/{body.maxShield}");
            sb.AppendLine($"Barrier: {healthComponent.barrier}/{body.maxBarrier} ({-body.barrierDecayRate} decay/sec)");
            sb.AppendLine($"Acceleration: {body.acceleration}");
            string movementType;
            if (body.moveSpeed == 0f)
            {
                movementType = "stationary";
            }
            else if (body.notMovingStopwatch > 1f)
            {
                movementType = "standing";
            }
            else if (body.isSprinting)
            {
                movementType = "sprintning";
            }
            else
            {
                movementType = "walking";
            }
            sb.AppendLine($"Speed: {body.moveSpeed} ({movementType})");
            sb.AppendLine($"Attack speed: {body.attackSpeed}");
            sb.AppendLine($"Damage: {body.damage}");
            sb.AppendLine($"Armor: {body.armor} (+{healthComponent.adaptiveArmorValue} adaptive)");
            var motor = body.characterMotor;
            sb.AppendLine($"Jump: {(motor != null ? body.maxJumpCount - motor.jumpCount : 0)}/{body.maxJumpCount} ({body.jumpPower} jump power)");
            var skills = body.skillLocator;
            if (skills != null)
            {
                var skillMap = new List<KeyValuePair<string, GenericSkill>>()
                {
                    new KeyValuePair<string, GenericSkill>("Primary", skills.primary),
                    new KeyValuePair<string, GenericSkill>("Secondary", skills.secondary),
                    new KeyValuePair<string, GenericSkill>("Utility", skills.utility),
                    new KeyValuePair<string, GenericSkill>("Special", skills.special)
                };
                foreach (var currentSkill in skillMap)
                {
                    if (currentSkill.Value != null)
                    {
                        var skill = currentSkill.Value;
                        // Various skills have a name for any combination of the following skillName/skillNameToken,
                        // so we're capturing all to help with identification
                        sb.AppendLine($"{currentSkill.Key}: [{skill.baseSkill.skillName} {skill.skillName} {skill.skillNameToken}] {skill.stock}/{skill.maxStock} ({skill.cooldownRemaining}/{skill.finalRechargeInterval} cooldown)");
                    }
                    else
                    {
                        sb.AppendLine($"{currentSkill.Key}: -");
                    }
                }
            }
            var inventory = body.inventory;
            if (inventory != null)
            {
                for (int i = 0; i < inventory.GetEquipmentSlotCount(); i++)
                {
                    var slot = inventory.equipmentStateSlots[i];
                    var equip = slot.equipmentDef;
                    if (equip != null)
                    {
                        sb.AppendLine($"Equipment {i + 1}: {equip.name} {slot.charges}/{inventory.GetEquipmentSlotMaxCharges((byte)i)} ({slot.chargeFinishTime.timeUntil}/{equip.cooldown * inventory.CalculateEquipmentCooldownScale()} cooldown)");
                    }
                    else
                    {
                        // There may not be an equipment, but the cooldown still matters
                        sb.AppendLine($"Equipment {i + 1}: - 0/0 ({slot.chargeFinishTime.timeUntil}/0 cooldown)");
                    }
                }
            }
            GatherObjectState(sb, body);
            var master = body.master;
            if (master != null)
            {
                GatherObjectState(sb, master);
                var aiComponents = master.GetComponents<RoR2.CharacterAI.BaseAI>();
                foreach (var ai in aiComponents)
                {
                    sb.AppendLine($"AI skill driver: {ai.skillDriverEvaluation.dominantSkillDriver?.customName ?? string.Empty}");
                }
            }
            Log.MessageNetworked(sb.ToString().TrimEnd('\n'), args, LogLevel.MessageClientOnly);
        }

        private static void GatherObjectState(System.Text.StringBuilder sb, Component component)
        {
            var esmComponents = component.GetComponents<EntityStateMachine>();
            foreach (var esm in esmComponents)
            {
                sb.AppendLine($"{esm.customName} state: {esm.state?.ToString() ?? string.Empty}");
            }
        }

        [ConCommand(commandName = "loadout_set_skin_variant", flags = ConVarFlags.None, helpText = Lang.LOADOUTSKIN_HELP)]
        [AutoComplete(Lang.LOADOUTSKIN_ARGS)]
        public static void CCLoadoutSetSkinVariant(ConCommandArgs args)
        {
            if (args.Count < 2)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.LOADOUTSKIN_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            BodyIndex argBodyIndex = BodyIndex.None;
            bool bodyIsSelf = false;

            if (args[0].ToUpperInvariant() == "SELF")
            {
                bodyIsSelf = true;
                if (args.sender == null)
                {
                    Log.Message("Can't choose self if not in-game!", LogLevel.Error);
                    return;
                }
                if (args.senderBody)
                {
                    argBodyIndex = args.senderBody.bodyIndex;
                }
                else
                {
                    if (args.senderMaster && args.senderMaster.bodyPrefab)
                    {
                        argBodyIndex = args.senderMaster.bodyPrefab.GetComponent<CharacterBody>().bodyIndex;
                    }
                    else
                    {
                        argBodyIndex = args.sender.bodyIndexPreference;
                    }
                }
            }
            else
            {
                argBodyIndex = StringFinder.Instance.GetBodyFromPartial(args[0]);
                if (argBodyIndex == BodyIndex.None)
                {
                    Log.MessageNetworked(Lang.BODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            if (!TextSerialization.TryParseInvariant(args[1], out int requestedSkinIndexChange))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "skin_index", "int"), args, LogLevel.MessageClientOnly);
            }

            Loadout loadout = new Loadout();
            UserProfile userProfile = args.GetSenderLocalUser().userProfile;
            userProfile.loadout.Copy(loadout);
            loadout.bodyLoadoutManager.SetSkinIndex(argBodyIndex, (uint)requestedSkinIndexChange);
            userProfile.SetLoadout(loadout);
            if (args.senderMaster)
            {
                args.senderMaster.SetLoadoutServer(loadout);
            }
            if (args.senderBody)
            {
                args.senderBody.SetLoadoutServer(loadout);
                if (args.senderBody.modelLocator && args.senderBody.modelLocator.modelTransform)
                {
                    var modelSkinController = args.senderBody.modelLocator.modelTransform.GetComponent<ModelSkinController>();
                    if (modelSkinController)
                    {
                        modelSkinController.ApplySkin(requestedSkinIndexChange);
                    }
                }
            }

            if (bodyIsSelf && !args.senderBody)
            {
                Log.MessageNetworked(Lang.PLAYER_SKINCHANGERESPAWN, args, LogLevel.MessageClientOnly);
            }
        }

        internal static bool UpdateCurrentPlayerBody(out NetworkUser networkUser, out CharacterBody characterBody)
        {
            networkUser = LocalUserManager.GetFirstLocalUser()?.currentNetworkUser;
            characterBody = null;

            if (networkUser)
            {
                var master = networkUser.master;

                if (master && master.GetBody())
                {
                    characterBody = master.GetBody();
                    return true;
                }
            }

            return false;
        }
    }
}
