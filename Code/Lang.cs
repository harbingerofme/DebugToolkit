namespace DebugToolkit
{
    public static class Lang
    {
        // `{}` denotes necessary, `()` a list of specific choices, `[]` optional, `:` denotes a default

        // Command arguments
        public const string
            ADDPORTAL_ARGS = "Requires 1 argument: {portal ('blue'|'gold'|'celestial'|'null'|'void'|'deepvoid'|'all')}",
            BAN_ARGS = "Requires 1 argument: {player}",
            BIND_ARGS = "Requires 2 arguments: {key} {console_commands}",
            BIND_DELETE_ARGS = "Requires 1 argument: {key}",
            CHANGETEAM_ARGS = "Requires 1 (2 if from server) argument: {team} [player]",
            CREATEPICKUP_ARGS = "Requires 1 (3 if from server) argument: {object (item|equip|'lunarcoin'|'voidcoin')} [search ('item'|'equip'|'both'):'both'] *[player:<self>]",
            DUMPSTATE_ARGS = "Requires 0 arguments [target (player|'pinged'):<self>]",
            DUMPSTATS_ARGS = "Requires 1 argument: {body}",
            FIXEDTIME_ARGS = "Requires 0 or 1 argument: [time]",
            FORCEWAVE_ARGS = "Requires 0 or 1 argument [wave_prefab]",
            GIVEBUFF_ARGS = "Requires 1 (4 if from server) arguments: {buff} [count:1] [duration:0] [target (player|'pinged'):<self>]",
            GIVEDOT_ARGS = "Requires 1 (4 if from server) argument: {dot} [count:1] [target (player|'pinged'):<self>] [attacker (player|'pinged'):<self>]",
            GIVEEQUIP_ARGS = "Requires 1 (2 if from server) argument: {equip} [target (player|'pinged'):<self>]",
            GIVEITEM_ARGS = "Requires 1 (3 if from server) argument: {item} [count:1] [target (player|'pinged'):<self>]",
            GIVELUNAR_ARGS = "Requires 0 arguments: [amount:1]",
            GIVEMONEY_ARGS = "Requires 1 argument: {amount} [target (player|'all')]",
            KICK_ARGS = "Requires 1 argument: {player}",
            KILLALL_ARGS = "Requires 0 arguments: [team:Monster]",
            LISTSKIN_ARGS = "Requires 0 arguments: [body|'all'|'body' <separated by body>|'self'):'all']",
            LISTQUERY_ARGS = "Requires 0 arguments: [query]",
            LOADOUTSKIN_ARGS = "Requires 2 argument: {(body|'self')} {skin_index}",
            NEXTBOSS_ARGS = "Requires 1 argument: {director_card} [count:1] [elite:None]",
            NEXTSTAGE_ARGS = "Requires 0 arguments: [stage]",
            NO_ARGS = "Requires 0 arguments.",
            PERM_ENABLE_ARGS = "Requires 0 or 1 arguments: [value]",
            PERM_MOD_ARGS = "Requires 2 arguments: (permission_level (0 OR None|1 OR SubAdmin|2 OR Admin) (player)",
            POSTSOUNDEVENT_ARGS = "Requires 1 argument: {event_name}",
            RANDOMITEM_ARGS = "Requires 1 (3 if from server) argument: {count} [tiers ('all'|Any comma-separated tier names):'all'] [target (player|'pinged'):<self>]",
            REMOVEALLBUFFS_ARGS = "Requires 0 (2 if from server) arguments: [timed (0|1):0/false] [target (player|'pinged'):<self>]",
            REMOVEALLDOTS_ARGS = "Requires 0 (1 if from server) arguments: [target (player|'pinged'):<self>]",
            REMOVEALLITEMS_ARGS = "Requires 0 (1 if from server) arguments: [target (player|'pinged'):<self>]",
            REMOVEBUFF_ARGS = "Requires 1 (4 if from server) arguments: {buff} [count:1] [timed (0|1):0/false] [target (player|'pinged'):<self>]",
            REMOVEBUFFSTACKS_ARGS = "Requires 1 (3 if from server) arguments: {buff} [timed (0|1):0/false] [target (player|'pinged'):<self>]",
            REMOVEDOT_ARGS = "Requires 1 (3 if from server) argument: {dot} [count:1] [target (player|'pinged'):<self>]",
            REMOVEDOTSTACKS_ARGS = "Requires 1 (2 if from server) argument: {dot} [target (player|'pinged'):<self>]",
            REMOVEEQUIP_ARGS = "Requires 0 (1 if from server) arguments: [target (player|'pinged'):<self>]",
            REMOVEITEMSTACKS_ARGS = "Requires 1 (2 if from server) argument: {item} [target (player|'pinged'):<self>]",
            RESPAWN_ARGS = "Requires 0 (1 if from server) arguments: [player:<self>]",
            SEED_ARGS = "Requires 0 or 1 argument: [new_seed]",
            SETRUNWAVESCLEARED_ARGS = "Requires 1 argument {wave}",
            SPAWNAI_ARGS = "Requires 1 argument: {ai} [count:1] [elite:None] [braindead (0|1):0/false] [team:Monster]",
            SPAWNAS_ARGS = "Requires 1 argument: {body} [player:<self>]",
            SPAWNBODY_ARGS = "Requires 1 argument: {body}",
            SPAWNINTERACTABLE_ARGS = "Requires 1 argument: {interactable}",
            TIMESCALE_ARGS = "Requires 1 argument: {time_scale}",
            TRUEKILL_ARGS = "Requires 0 (1 if from server) arguments: [player:<self>]"
            ;

        // Command help texts
        public const string
            ADDPORTAL_HELP = "Add a portal to the current Teleporter on completion. " + ADDPORTAL_ARGS,
            BAN_HELP = "Bans the specified player from the session. " + BAN_ARGS,
            BUDDHA_HELP = "Become immortal. Instead of refusing damage you just refuse to take lethal damage.\nIt works by giving all damage a player takes DamageType.NonLethal" + NO_ARGS,
            CHANGETEAM_HELP = "Change the specified player to the specified team. " + CHANGETEAM_ARGS,
            CREATEPICKUP_HELP = "Creates a PickupDroplet infront of your position. " + CREATEPICKUP_ARGS,
            CURSORTELEPORT_HELP = "Teleport you to where your cursor is currently aiming at. " + NO_ARGS,
            DUMPBUFFS_HELP = "List the buffs/debuffs of all spawned bodies. " + NO_ARGS,
            DUMPINVENTORIES_HELP = "List the inventory items and equipment of all spawned bodies. " + NO_ARGS,
            DUMPSTATE_HELP = "List the current stats, entity state, and skill cooldown of a specified body. " + DUMPSTATE_ARGS,
            DUMPSTATS_HELP = "List the base stats of a specific body. " + DUMPSTATS_ARGS,
            FAMILYEVENT_HELP = "Forces a family event to occur during the next stage. " + NO_ARGS,
            FIXEDTIME_HELP = "Sets the run timer to the specified value. " + FIXEDTIME_ARGS,
            FORCEWAVE_HELP = "Set the next wave prefab. Leave empty to see all options. " + FORCEWAVE_ARGS,
            GIVEBUFF_HELP = "Gives the specified buff to a character. A duration of 0 means permanent. " + GIVEBUFF_ARGS,
            GIVEDOT_HELP = "Gives the specified DoT to a character. " + GIVEDOT_ARGS,
            GIVEEQUIP_HELP = "Gives the specified equipment to a character. " + GIVEEQUIP_ARGS,
            GIVEITEM_HELP = "Gives the specified item to a character. " + GIVEITEM_ARGS,
            GIVELUNAR_HELP = "Gives a lunar coin to you. " + GIVELUNAR_ARGS,
            GIVEMONEY_HELP = "Gives the specified amount of money to the specified player. " + GIVEMONEY_ARGS,
            GOD_HELP = "Become invincible. " + NO_ARGS,
            KICK_HELP = "Kicks the specified player from the session. " + KICK_ARGS,
            KILLALL_HELP = "Kill all entities on the specified team. " + KILLALL_ARGS,
            LISTAI_HELP = "List all Masters and their language invariants. " + LISTQUERY_ARGS,
            LISTBODY_HELP = "List all Bodies and their language invariants. " + LISTQUERY_ARGS,
            LISTBUFF_HELP = "List all Buffs and whether they stack. " + LISTQUERY_ARGS,
            LISTDIRECTORCARDS_HELP = "List all Director Cards. " + LISTQUERY_ARGS,
            LISTDOT_HELP = "List all DoTs. " + LISTQUERY_ARGS,
            LISTELITE_HELP = "List all Elites and their language invariants. " + LISTQUERY_ARGS,
            LISTEQUIP_HELP = "List all equipment and their availability. " + LISTQUERY_ARGS,
            LISTINTERACTABLE_HELP = "Lists all interactables. " + LISTQUERY_ARGS,
            LISTITEMTIER_HELP = "List all item tiers. " + LISTQUERY_ARGS,
            LISTITEM_HELP = "List all items and their availability. " + LISTQUERY_ARGS,
            LISTPLAYER_HELP = "List all players and their ID. " + LISTQUERY_ARGS,
            LISTSKIN_HELP = "List all bodies with skins. " + LISTSKIN_ARGS,
            LISTTEAM_HELP = "List all Teams and their language invariants. " + LISTQUERY_ARGS,
            LOADOUTSKIN_HELP = "Change your loadout's skin.  " + LOADOUTSKIN_ARGS,
            LOCKEXP_HELP = "Toggle Experience gain. " + NO_ARGS,
            MACRO_DTZOOM_HELP = "Gives you 20 hooves and 200 feathers for getting around quickly.",
            MACRO_LATEGAME_HELP = "Sets the current run to the 'lategame' as defined by HG. This command is DESTRUCTIVE.",
            MACRO_MIDGAME_HELP = "Sets the current run to the 'midgame' as defined by HG. This command is DESTRUCTIVE.",
            NEXTBOSS_HELP = "Sets the next teleporter/simulacrum boss to the specified boss. " + NEXTBOSS_ARGS,
            NEXTSTAGE_HELP = "Forces a stage change to the specified stage. " + NEXTSTAGE_ARGS,
            NEXTWAVE_HELP = "Advance to the next Simulacrum wave. " + NO_ARGS,
            NOCLIP_HELP = "Allow flying and going through objects. Sprinting will double the speed. " + NO_ARGS,
            NOENEMIES_HELP = "Toggle Monster spawning. " + NO_ARGS,
            PERM_ENABLE_HELP = "Enable or disable the permission system." + PERM_ENABLE_ARGS,
            PERM_MOD_HELP = "Change the permission level of the specified playerid/username" + PERM_MOD_ARGS,
            PERM_RELOAD_HELP = "Reload the permission system, updates user and commands permissions.",
            POSTSOUNDEVENT_HELP = "Post a sound event to the AkSoundEngine (WWise) by its event name. " + POSTSOUNDEVENT_ARGS,
            RANDOMITEM_HELP = "Generate random items from the available item tiers. " + RANDOMITEM_ARGS,
            RELOADCONFIG_HELP = "Reload all default config files from all loaded plugins.",
            REMOVEALLBUFFS_HELP = "Removes all buffs from a character. " + REMOVEALLBUFFS_ARGS,
            REMOVEALLDOTS_HELP = "Removes all DoTs from a character. " + REMOVEALLDOTS_ARGS,
            REMOVEALLITEMS_HELP = "Removes all items from a character. " + REMOVEALLITEMS_ARGS,
            REMOVEBUFF_HELP = "Removes a specified buff from a character. Timed buffs prioritise the longest expiration stack. " + REMOVEBUFF_ARGS,
            REMOVEBUFFSTACKS_HELP = "Removes all stacks of a specified buff from a character. " + REMOVEBUFFSTACKS_ARGS,
            REMOVEDOT_HELP = "Remove a DoT stack with the longest expiration from a character. " + REMOVEDOT_ARGS,
            REMOVEDOTSTACKS_HELP = "Remove all stacks of a specified DoT from a character. " + REMOVEDOTSTACKS_ARGS,
            REMOVEEQUIP_HELP = "Removes the equipment from a character. " + REMOVEEQUIP_ARGS,
            REMOVEITEM_HELP = "Removes the specified quantities of an item from a character. " + GIVEITEM_ARGS,
            REMOVEITEMSTACKS_HELP = "Removes all the stacks of a specified item from a character. " + REMOVEITEMSTACKS_ARGS,
            RESPAWN_HELP = "Respawns the specified player. " + RESPAWN_ARGS,
            SEED_HELP = "Gets/Sets the game seed until game close. Use 0 to reset to vanilla generation. " + SEED_ARGS,
            SETRUNWAVESCLEARED_HELP = "Set the Simulacrum waves cleared. Must be positive. " + SETRUNWAVESCLEARED_ARGS,
            SPAWNAS_HELP = "Respawn the specified player using the specified body prefab. " + SPAWNAS_ARGS,
            SPAWNAI_HELP = "Spawns the specified CharacterMaster. " + SPAWNAI_ARGS,
            SPAWNBODY_HELP = "Spawns the specified dummy body. " + SPAWNBODY_ARGS,
            SPAWNINTERACTABLE_HELP = "Spawns the specified interactable. List_Interactable for options. " + SPAWNINTERACTABLE_ARGS,
            TIMESCALE_HELP = "Sets the Time Delta. " + TIMESCALE_ARGS,
            TRUEKILL_HELP = "Ignore Dio's and kill the entity. " + TRUEKILL_ARGS
            ;

        // Messages
        public const string
            CREATEPICKUP_AMBIGIOUS_2 = "Could not choose between {0} and {1}, please be more precise. Consider using 'equip' or 'item' as the second argument.",
            CREATEPICKUP_NOTFOUND = "Could not find any item nor equipment with that name.",
            CREATEPICKUP_SUCCES_1 = "Succesfully created the pickup {0}.",
            GIVELUNAR_2 = "{0} {1} lunar coin(s).",
            GIVEOBJECT = "Gave {0} {1}",
            OBSOLETEWARNING = "This command has become obsolete and will be removed in the next version. ",
            NETWORKING_OTHERPLAYER_4 = "{0}({1}) issued: {2} {3}",
            NOMESSAGE = "Yell at the modmakers if you see this message!",
            NOCLIP_TOGGLE = "Noclip toggled to {0}",
            PARTIALIMPLEMENTATION_WARNING = "WARNING: PARTIAL IMPLEMENTATION. WIP.",
            PLAYER_DEADRESPAWN = "Player will spawn as the specified body next round. " + USE_RESPAWN,
            PLAYER_SKINCHANGERESPAWN = "Player will spawn with the specified skin next round. " + USE_RESPAWN,
            RUNSETSTAGESCLEARED_HELP = "Sets the amount of stages cleared. This does not change the current stage.",
            SPAWN_ATTEMPT_1 = "Attempting to spawn: {0}",
            SPAWN_ATTEMPT_2 = "Attempting to spawn {0}: {1}",
            USE_RESPAWN = "Use 'respawn' to skip the wait."
            ;

        // Errors
        public const string
            BODY_NOTFOUND = "Specified body not found. Please use list_body for options.",
            ELITE_NOTFOUND = "Elite type not recognized. Please use list_elite for options.",
            INSUFFICIENT_ARGS = "Insufficient number of arguments. ",
            INTERACTABLE_NOTFOUND = "Interactable not found. Please use list_interactables for options.",
            INVALID_ARG_VALUE = "Invalid value for {0}.",
            INVENTORY_ERROR = "The selected target has no inventory.",
            NEGATIVE_ARG = "Negative value for {0} encountered. It needs to be zero or higher.",
            NOTINARUN_ERROR = "This command only works when in a Run!",
            NOTINASIMULACRUMRUN_ERROR = "Must be in a Simulacrum Run!",
            OBJECT_NOTFOUND = "The requested object could not be found: ",
            PARSE_ERROR = "Unable to parse {0} to {1}.",
            PINGEDBODY_NOTFOUND = "Pinged target not found. Either the last ping was not a character, or it has been destroyed since.",
            PLAYER_NOTFOUND = "Specified player not found or isn't alive. Please use list_player for options.",
            PORTAL_NOTFOUND = "The specified portal could not be found. Valid portals: 'blue','gold','celestial','null','void','deepvoid','all'",
            SPAWN_ERROR = "Could not spawn: ",
            STAGE_NOTFOUND = "Stage not found. Please use scene_list for options.",
            TEAM_NOTFOUND = "Team type not found. Please use list_team for options."
            ;

        // Keywords
        public const string
            ALL = "ALL",
            BOTH = "BOTH",
            COIN_LUNAR = "LUNARCOIN",
            COIN_VOID = "VOIDCOIN",
            DEFAULT_VALUE = "",
            EQUIP = "EQUIP",
            ITEM = "ITEM",
            PINGED = "PINGED",
            RANDOM = "RANDOM"
            ;

        // Permissions
        public const string
            PS_ARGUSER_HAS_MORE_PERM = "Specified user {0} has a greater permission level than you.",
            PS_ARGUSER_HAS_SAME_PERM = "Specified user {0} has the same permission level as you.",
            PS_NO_REQUIRED_LEVEL = "You don't have the required permission {0} to use this command."
            ;

        // Dedicated server
        public const string
            DS_NOTAVAILABLE = "This command doesn't make sense to run from a dedicated server.",
            DS_NOTYETIMPLEMENTED = "This command has not yet been implemented to be run from a dedicated server,"
            ;

    }
}
