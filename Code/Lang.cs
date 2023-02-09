namespace DebugToolkit
{
    public static class Lang
    {
        // `{}` denotes necessary, `()` a list of specifc choices, `[]` optional, `:` denotes a default
        public const string
            ADDPORTAL_ARGS = "Requires 1 argument: ('blue'|'gold'|'celestial'|'null'|'void'|'deepvoid'|'all')",
            BAN_ARGS = "Requires 1 argument: {player}",
            BIND_ARGS = "Requires 2 arguments: {key} {console_commands}",
            BIND_DELETE_ARGS = "Requires 1 argument: {key}",
            CHANGETEAM_ARGS = "Requires 1 argument: {team} [player]",
            CREATEPICKUP_ARGS = "Requires 1 argument: (object|'coin') [search ('item'|'equip'):<both>]",
            FAMILYEVENT_ARGS = "Requires 0 arguments.",
            FIXEDTIME_ARGS = "Requires 1 argument: [time]",
            GIVEEQUIP_ARGS = "Requires 1 (2 if from server) argument: [equip} [(player|'pinged'):<self>]",
            GIVEITEM_ARGS = "Requires 1 (3 if from server) argument: {item} [count:1] [(player|'pinged'):<self>]",
            GIVELUNAR_ARGS = "Requires 0 arguments: [count:1]",
            GIVEMONEY_ARGS = "Requires 1 argument: {count} [(player|'all')]",
            GOD_ARGS = "Requires 0 arguments.",
            NOCLIP_ARGS = "Requires 0 arguments.",
            CURSORTELEPORT_ARGS = "Requires no arguments: teleport_on_cursor",
            LOADOUTSKIN_ARGS = "Requires 2 argument: loadout_set_skin_variant {(body|'self')} {skin_index}",
            LOCKEXP_ARGS = "Requires 0 arguments.",
            KICK_ARGS = "Requires 1 argument: {player}",
            KILLALL_ARGS = "Requires 0 arguments: [team:Monster]",
            NEXTBOSS_ARGS = "Requires 1 argument: (csc|director_card) [count:1] [elite:None]",
            NEXTSTAGE_ARGS = "Requires 0 arguments: [stage]",
            NOENEMIES_ARGS = "Requires 0 arguments.",
            PERM_ENABLE_ARGS = "Requires no argument.",
            PERM_MOD_ARGS = "Requires 2 arguments: (PermissionLevel (0 OR None|1 OR SubAdmin|2 OR Admin) (player)",
            RANDOM_ITEM_ARGS = "Requires 1 (2 if from server) argument: {count} [(player|'pinged'):<self>]",
            REMOVEALLITEMS_ARGS = "Requires 0 (1 if from server) arguments: [(player|'pinged'):<self>]",
            REMOVEEQUIP_ARGS = "Requires 0 (1 if from server) arguments: [(player|'pinged'):<self>]",
            REMOVEITEM_ARGS = "Requires 1 (3 if from server) argument: {item} [count:1] [(player|'pinged'):<self>]",
            REMOVEITEMSTACKS_ARGS = "Requires 1 (2 if from server) argument: {item} [(player|'pinged'):<self>]",
            RESPAWN_ARGS = "Requires 0 arguments: [(player|'pinged'):<self>]",
            SEED_ARGS = "Requires 0 or 1 argument: [new_seed]",
            SPAWNAI_ARGS = "Requires 1 argument: {ai} [count:1] [elite:None] [braindead (0|1):0/false] [team:Monster]",
            SPAWNAS_ARGS = "Requires 1 argument: [body] [(player|'pinged'):<self>]",
            SPAWNBODY_ARGS = "Requires 1 argument: {body}",
            SPAWNINTERACTABLE_ARGS = "Requires 1 argument: {interactable}",
            TIMESCALE_ARGS = "Requires 1 argument: {time_scale}",
            TRUEKILL_ARGS = "Requires 0 arguments: [(player|'pinged'):<self>]"
            ;

        public const string
            LISTITEM_ARGS = "List all item names and their IDs. Requires 0 arguments: [query]",
            LISTEQUIP_ARGS = "List all equipment items and their IDs. Requires 0 arguments: [query]",
            LISTAI_ARGS = "List all Masters and their language invariants. Requires 0 arguments: [query]",
            LISTELITE_ARGS = "List all Elites and their language invariants. Requires 0 arguments: [query]",
            LISTTEAM_ARGS = "List all Teams and their language invariants. Requires 0 arguments: [query]",
            LISTBODY_ARGS = "List all Bodies and their language invariants. Requires 0 arguments: [query]",
            LISTPLAYER_ARGS = "List all players and their ID. Requires 0 arguments: [query]",
            LISTSKIN_ARGS = "List all bodies with skins. Requires 0 arguments: list_skin [{localised_objectname}|'all'|'body' <separated by body>|'self'):'all']",
            LISTINTERACTABLE_ARGS = "Lists all interactables. Requires 0 arguments: [query]"
            ;

        public const string
            CREATEPICKUP_AMBIGIOUS_2 = "Could not choose between {0} and {1}, please be more precise. Consider using 'equip' or 'item' as the second argument.",
            CREATEPICKUP_NOTFOUND = "Could not find any item nor equipment with that name. It's not a coin either.",
            CREATEPICKUP_SUCCES_1 = "Succesfully created the pickup {0}.",
            COUNTISNUMERIC = "Count must be numeric!",

            GIVELUNAR_2 = "{0} {1} lunar coin(s).",
            GIVEOBJECT = "Gave {0} {1}",
            INTEGER_EXPECTED = "Argument must be a whole number.",
            OBJECT_NOTFOUND = "The requested object could not be found: ",
            OBSOLETEWARNING = "This command has become obsolete and will be removed in the next version. ",
            NETWORKING_OTHERPLAYER_4 = "{0}({1}) issued: {2} {3}",
            NEXTROUND_STAGE = "Invalid Stage. Please choose from the following: ",
            NOMESSAGE = "Yell at the modmakers if you see this message!",
            NOCLIP_TOGGLE = "Noclip toggled to {0}",
            PARTIALIMPLEMENTATION_WARNING = "WARNING: PARTIAL IMPLEMENTATION. WIP.",
            PLAYER_DEADRESPAWN = "Player is will spawn as the specified body next round. (Use 'respawn' to skip the wait)",
            PLAYER_SKINCHANGERESPAWN = "Player will spawn with the specified skin next round. (Use 'respawn' to skip the wait)",
            PLAYER_NOTFOUND = "Specified player does not exist",
            PINGEDBODY_NOTFOUND = "Pinged target not found. Either the last ping was not a character, or it has been destroyed since.",
            PORTAL_NOTFOUND = "The specified portal could not be found. Valid portals: 'blue','gold','celestial','null','void','deepvoid','all'",
            RUNSETSTAGESCLEARED_HELP = "Sets the amount of stages cleared. This does not change the current stage.",
            SPAWN_ATTEMPT_1 = "Attempting to spawn: {0}",
            SPAWN_ATTEMPT_2 = "Attempting to spawn {0}: {1}",
            SPAWN_ERROR = "Could not spawn: ",
            NOTINARUN_ERROR = "This command only works when in a Run !",
            INVENTORY_ERROR = "The selected target has no inventory.",
            ALL = "ALL",
            DEFAULT_VALUE = "",
            PINGED = "PINGED"
            ;

        public const string
            PS_ARGUSER_HAS_MORE_PERM = "Specified user {0} has a greater permission level than you.",
            PS_ARGUSER_HAS_SAME_PERM = "Specified user {0} has the same permission level as you.",
            PS_NO_REQUIRED_LEVEL = "You don't have the required permission {0} to use this command."
            ;

        public const string
            DS_REQUIREFULLQUALIFY = "Command must be fully qualified on dedicated servers.",
            DS_NOTAVAILABLE = "This command doesn't make sense to run from a dedicated server.",
            DS_NOTYETIMPLEMENTED = "This command has not yet been implemented to be run from a dedicated server,"
            ;

    }
}
