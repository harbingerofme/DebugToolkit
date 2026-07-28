using RoR2;
using System;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit
{
    /// <summary>
    /// A collection of helper methods to streamline command parsing and argument validation.
    /// </summary>
    public static class ArgumentParser
    {
        #region Generic Checks
        /// <summary>
        /// Check whether the command is executed in the context of a run.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <returns>Returns true if a run is active, else false.</returns>
        public static bool AssertInARun(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the command is executed in the context of a Simulacrum run.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <returns>Returns true if a Simulacrum run is active, else false.</returns>
        public static bool AssertInSimulacrumARun(ConCommandArgs args)
        {
            if (!Run.instance || Run.instance is not InfiniteTowerRun)
            {
                Log.MessageNetworked(Lang.NOTINASIMULACRUMRUN_ERROR, args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the command has the minimum required arguments.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="defaultArgs">The string of arguments this command accepts. Used for logging when the method returns false.</param>
        /// <param name="required">How many arguments are necessary by default.</param>
        /// <param name="requiredFromServer">How many arguments are necessary from a Dedicated Server.</param>
        /// <returns>Returns true if the minimum number of arguments are provided, else false.</returns>
        public static bool AssertRequiredArguments(ConCommandArgs args, string defaultArgs, int required, int? requiredFromServer = null)
        {
            if (args.Count < required)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + defaultArgs, args, LogLevel.MessageClientOnly);
                return false;
            }
            if (args.sender == null && requiredFromServer.HasValue)
            {
                if (args.Count < requiredFromServer.Value || args[requiredFromServer.Value - 1] == Lang.DEFAULT_VALUE)
                {
                    Log.Message(Lang.INSUFFICIENT_ARGS + defaultArgs, LogLevel.Message);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check whether the command is executed by a Dedicated Server.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <returns>Returns true if args.sender is not null, else false.</returns>
        public static bool AssertNotServer(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.Message(Lang.DS_NOTYETIMPLEMENTED);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether an expansion is enabled for the current run.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="expansionDef">The expansion to check for availability.</param>
        /// <param name="typeName">The name of the expansion type to be logged when the check fails.</param>
        /// <returns>Returns true if the expansion is null, no run is active, or the expansion is available, else false.</returns>
        public static bool AssertExpansionAvailable(ConCommandArgs args, RoR2.ExpansionManagement.ExpansionDef expansionDef, string typeName)
        {
            if (expansionDef && Run.instance && !Run.instance.IsExpansionEnabled(expansionDef))
            {
                Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, typeName, Util.GetExpansion(expansionDef)), args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the command sender has a valid CharacterBody.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <returns>Returns true is args.senderBody is valid, else false.</returns>
        public static bool AssertLivingBody(ConCommandArgs args)
        {
            if (args.senderBody == null)
            {
                Log.MessageNetworked($"Can't use this command while dead. {Lang.USE_RESPAWN}", args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }
        #endregion

        #region Catalog parsing
        /// <summary>
        /// Match a command argument to an artifact.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="artifactDef">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseArtifact(ConCommandArgs args, int index, out ArtifactDef artifactDef)
        {
            artifactDef = null;
            var artifactIndex = StringFinder.Instance.GetArtifactFromPartial(args[index]);
            if (artifactIndex == ArtifactIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "artifact", args[index], "list_artifact"), args, LogLevel.MessageClientOnly);
                return false;
            }
            artifactDef = ArtifactCatalog.GetArtifactDef(artifactIndex);
            return AssertExpansionAvailable(args, artifactDef.requiredExpansion, "artifact");
        }

        /// <summary>
        /// Match a command argument to a buff.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="buffDef">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseBuff(ConCommandArgs args, int index, out BuffDef buffDef)
        {
            buffDef = null;
            var buffIndex = StringFinder.Instance.GetBuffFromPartial(args[index]);
            if (buffIndex == BuffIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "buff", args[index], "list_buff"), args, LogLevel.MessageClientOnly);
                return false;
            }
            buffDef = BuffCatalog.GetBuffDef(buffIndex);
            return true;
        }

        /// <summary>
        /// Match a command argument to a DoT.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="dotIndex">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseDot(ConCommandArgs args, int index, out DotController.DotIndex dotIndex)
        {
            dotIndex = StringFinder.Instance.GetDotFromPartial(args[index]);
            if (dotIndex == DotController.DotIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "dot", args[index], "list_dot"), args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Match a command argument to an item.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="itemDef">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseItem(ConCommandArgs args, int index, out ItemDef itemDef)
        {
            itemDef = null;
            var itemIndex = StringFinder.Instance.GetItemFromPartial(args[index]);
            if (itemIndex == ItemIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "item", args[index], "list_item"), args, LogLevel.MessageClientOnly);
                return false;
            }
            itemDef = ItemCatalog.GetItemDef(itemIndex);
            return AssertExpansionAvailable(args, itemDef.requiredExpansion, "item");
        }

        /// <summary>
        /// Match a command argument to either the string 'random' or an equipment.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="equipmentDef">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseEquipmentOrRandom(ConCommandArgs args, int index, out EquipmentDef equipmentDef)
        {
            equipmentDef = null;
            if (string.Equals(args[index], Lang.RANDOM, StringComparison.InvariantCultureIgnoreCase))
            {
                var pickupIndex = RoR2Application.rng.NextElementUniform(Run.instance.availableEquipmentDropList);
                equipmentDef = EquipmentCatalog.GetEquipmentDef(PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
                return true;
            }
            var equipmentIndex = StringFinder.Instance.GetEquipFromPartial(args[index]);
            if (equipmentIndex == EquipmentIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "equip", args[index], "list_equip"), args, LogLevel.MessageClientOnly);
                return false;
            }
            equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
            return AssertExpansionAvailable(args, equipmentDef.requiredExpansion, "equipment");
        }

        /// <summary>
        /// Match a command argument to a body prefab.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="bodyPrefab">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseBody(ConCommandArgs args, int index, out GameObject bodyPrefab)
        {
            bodyPrefab = null;
            var bodyIndex = StringFinder.Instance.GetBodyFromPartial(args[index]);
            if (bodyIndex == BodyIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "body", args[index], "list_body"), args, LogLevel.MessageClientOnly);
                return false;
            }
            bodyPrefab = BodyCatalog.GetBodyPrefab(bodyIndex);
            return true;
        }

        /// <summary>
        /// Match a command argument to a drone.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="droneDef">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseDrone(ConCommandArgs args, int index, out DroneDef droneDef)
        {
            droneDef = null;
            var droneIndex = StringFinder.Instance.GetDroneFromPartial(args[index]);
            if (droneIndex == DroneIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "drone", args[index], "list_drone"), args, LogLevel.MessageClientOnly);
                return false;
            }
            droneDef = DroneCatalog.GetDroneDef(droneIndex);
            // No need to check for expansion availability since `spawn_interactable` for the broken drone is allowed.
            return true;
        }

        /// <summary>
        /// Match a command argument to a master prefab.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="masterObject">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseMaster(ConCommandArgs args, int index, out GameObject masterObject)
        {
            masterObject = null;
            var masterIndex = StringFinder.Instance.GetAiFromPartial(args[index]);
            if (masterIndex == MasterCatalog.MasterIndex.none)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "ai", args[index], "list_ai"), args, LogLevel.MessageClientOnly);
                return false;
            }
            masterObject = MasterCatalog.GetMasterPrefab(masterIndex);
            return true;
        }

        /// <summary>
        /// Match an optional command argument to an elite or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="defaultValue">The default value to return if the argument hasn't been defined.</param>
        /// <param name="eliteDef">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParseEliteOrDefault(ConCommandArgs args, int index, EliteDef defaultValue, out EliteDef eliteDef)
        {
            eliteDef = defaultValue;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                var eliteIndex = StringFinder.Instance.GetEliteFromPartial(args[index]);
                if (eliteIndex == StringFinder.EliteIndex_NotFound)
                {
                    Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "elite", args[index], "list_elite"), args, LogLevel.MessageClientOnly);
                    return false;
                }
                eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
            }
            return AssertExpansionAvailable(args, eliteDef?.eliteEquipmentDef?.requiredExpansion, "elite equipment");
        }

        /// <summary>
        /// Match a command argument to a team.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="teamIndex">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseTeam(ConCommandArgs args, int index, out TeamIndex teamIndex)
        {
            teamIndex = StringFinder.Instance.GetTeamFromPartial(args[index]);
            if (teamIndex == StringFinder.TeamIndex_NotFound)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "team", args[index], "list_team"), args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Match an optional command argument to a default or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="defaultValue">The default value to return if the argument hasn't been defined.</param>
        /// <param name="teamIndex">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParseTeamOrDefault(ConCommandArgs args, int index, TeamIndex defaultValue, out TeamIndex teamIndex)
        {
            teamIndex = defaultValue;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
                return TryParseTeam(args, index, out teamIndex);
            return true;
        }

        /// <summary>
        /// Match a command argument to a difficulty.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="difficultyIndex">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseDifficulty(ConCommandArgs args, int index, out DifficultyIndex difficultyIndex)
        {
            difficultyIndex = StringFinder.Instance.GetDifficultyFromPartial(args[index]);
            if (difficultyIndex == DifficultyIndex.Invalid)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "difficulty", args[index], "list_difficulty"), args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Match a command argument to a scene.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="includeOffline">Whether offline scenes are included in the pattern matching, e.g., intro, title, logbook, etc.</param>
        /// <param name="sceneDef">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseScene(ConCommandArgs args, int index, bool includeOffline, out SceneDef sceneDef)
        {
            sceneDef = null;
            var sceneIndex = StringFinder.Instance.GetSceneFromPartial(args[index], includeOffline);
            if (sceneIndex == SceneIndex.Invalid)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "scene", args[index], "list_scene"), args, LogLevel.MessageClientOnly);
                return false;
            }
            sceneDef = SceneCatalog.GetSceneDef(sceneIndex);
            return AssertExpansionAvailable(args, sceneDef.requiredExpansion, "scene");
        }

        /// <summary>
        /// Match a command argument to an interactable spawn card.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="isc">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseInteractableCard(ConCommandArgs args, int index, out InteractableSpawnCard isc)
        {
            isc = StringFinder.Instance.GetInteractableSpawnCardFromPartial(args[index]);
            if (isc == null)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "isc", args[index], "list_interactables"), args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Match a command argument to a director card for characters.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="directorCard">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is successfully parsed, else false.</returns>
        public static bool TryParseDirectorCard(ConCommandArgs args, int index, out DirectorCard directorCard)
        {
            directorCard = StringFinder.Instance.GetDirectorCardFromPartial(args[index]);
            if (directorCard == null)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "director_card", args[index], "list_directorcards"), args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }
        #endregion

        #region Optional Parsing
        /// <summary>
        /// Parse an optional command argument as a bool or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="variableName">The name of the argument. Used for logging when parsing fails.</param>
        /// <param name="defaultValue">The default value to return if the argument hasn't been defined.</param>
        /// <param name="value">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParseOptionalBool(ConCommandArgs args, int index, string variableName, bool defaultValue, out bool value)
        {
            value = defaultValue;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                if (!Util.TryParseBool(args[index], out value))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, variableName, "bool"), args, LogLevel.MessageClientOnly);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Parse an optional command argument as an int or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="variableName">The name of the argument. Used for logging when parsing fails.</param>
        /// <param name="defaultValue">The default value to return if the argument hasn't been defined.</param>
        /// <param name="value">The parsed value. May be junk if parsing fails.</param>
        /// <param name="min">The minimum value allowed. If less than this, parsing fails.</param>
        /// <param name="max">The maximum value allowed. If greater than this, parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParseOptionalInt(ConCommandArgs args, int index, string variableName, int defaultValue, out int value, int? min = null, int? max = null)
        {
            value = defaultValue;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                if (!TextSerialization.TryParseInvariant(args[index], out value))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, variableName, "int"), args, LogLevel.MessageClientOnly);
                    return false;
                }
            }
            if (min.HasValue && value < min.Value)
            {
                Log.MessageNetworked($"'{variableName}' can't be less than {min.Value}.", args, LogLevel.MessageClientOnly);
                return false;
            }
            if (max.HasValue && value > max.Value)
            {
                Log.MessageNetworked($"'{variableName}' can't be greater than {max.Value}.", args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Parse an optional command argument as a uint or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="variableName">The name of the argument. Used for logging when parsing fails.</param>
        /// <param name="defaultValue">The default value to return if the argument hasn't been defined.</param>
        /// <param name="value">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParseOptionalUInt(ConCommandArgs args, int index, string variableName, uint defaultValue, out uint value)
        {
            value = defaultValue;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                if (!TextSerialization.TryParseInvariant(args[index], out value))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, variableName, "uint"), args, LogLevel.MessageClientOnly);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Parse an optional command argument as a ulong or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="variableName">The name of the argument. Used for logging when parsing fails.</param>
        /// <param name="defaultValue">The default value to return if the argument hasn't been defined.</param>
        /// <param name="value">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParseOptionalULong(ConCommandArgs args, int index, string variableName, ulong defaultValue, out ulong value)
        {
            value = defaultValue;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                if (!TextSerialization.TryParseInvariant(args[index], out value))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, variableName, "ulong"), args, LogLevel.MessageClientOnly);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Parse an optional command argument as a float or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="variableName">The name of the argument. Used for logging when parsing fails.</param>
        /// <param name="defaultValue">The default value to return if the argument hasn't been defined.</param>
        /// <param name="value">The parsed value. May be junk if parsing fails.</param>
        /// <param name="min">The minimum value allowed. If less than this, parsing fails.</param>
        /// <param name="max">The maximum value allowed. If greater than this, parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParseOptionalFloat(ConCommandArgs args, int index, string variableName, float defaultValue, out float value, float? min = null, float? max = null)
        {
            value = defaultValue;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                if (!TextSerialization.TryParseInvariant(args[index], out value))
                {
                    Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, variableName, "float"), args, LogLevel.MessageClientOnly);
                    return false;
                }
            }
            if (min.HasValue && value < min.Value)
            {
                Log.MessageNetworked($"'{variableName}' can't be less than {min.Value}.", args, LogLevel.MessageClientOnly);
                return false;
            }
            if (max.HasValue && value > max.Value)
            {
                Log.MessageNetworked($"'{variableName}' can't be greater than {max.Value}.", args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }
        #endregion

        #region Target Parsing
        /// <summary>
        /// Match an optional command argument to a player or return the default value.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="master">The parsed value. May be junk if parsing fails.</param>
        /// <param name="requireLiving">Whether the found target has a valid body.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParsePlayerOrDefault(ConCommandArgs args, int index, out CharacterMaster master, bool requireLiving = false)
        {
            NetworkUser player = args.sender;
            master = args.senderMaster;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                player = Util.GetNetUserFromString(args.userArgs, index);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return false;
                }
                master = player.master;
            }
            if (requireLiving && !master.bodyInstanceObject)
            {
                // We could possibly use `player.master.deathFootPosition` instead
                Log.MessageNetworked("The target player is required alive for this command.", args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Return the pinged target from the command sender or match the argument to a player.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="target">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParsePlayerOrPingedTarget(ConCommandArgs args, int index, out Util.CommandTarget target)
        {
            target = default;
            var master = args.senderMaster;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                master = Util.GetTargetFromArgs(args, index);
                if (master == null && args.sender != null && args[index].ToUpperInvariant() == Lang.PINGED)
                {
                    master = Hooks.GetPingedTarget(args.senderMaster).master;
                    if (master == null)
                    {
                        MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return false;
                    }
                }
            }
            if (master == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return false;
            }

            target.inventory = master.inventory;
            target.name = master.playerCharacterMasterController?.GetDisplayName() ?? master.gameObject.name;
            return true;
        }

        /// <summary>
        /// Return the pinged CharacterBody from the command sender or match the argument to a player.
        /// </summary>
        /// <param name="args">The incoming data for this command.</param>
        /// <param name="index">The argument index for this value.</param>
        /// <param name="target">The parsed value. May be junk if parsing fails.</param>
        /// <returns>Returns true if the argument is optional or is successfully parsed, else false.</returns>
        public static bool TryParsePlayerOrPingedBodyTarget(ConCommandArgs args, int index, out Util.CommandTarget target)
        {
            target = default;
            var targetBody = args.senderBody;
            var isDedicatedServer = args.sender == null;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                // Try to get target from the master initially to account for ping -> target revival
                // as in that case the cached pinged body would be stale.
                var targetMaster = Util.GetTargetFromArgs(args, index);
                if (targetMaster == null && !isDedicatedServer && args[index].ToUpperInvariant() == Lang.PINGED)
                {
                    // Account for masterless bodies
                    targetBody = Hooks.GetPingedTarget(args.senderMaster).body;
                    if (targetBody == null)
                    {
                        Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return false;
                    }
                }
                else
                {
                    targetBody = targetMaster?.GetBody();
                }
            }
            if (targetBody == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return false;
            }

            var player = targetBody.master?.playerCharacterMasterController;
            target.body = targetBody;
            target.name = player?.GetDisplayName() ?? targetBody.gameObject.name;
            return true;
        }
        #endregion
    }
}
