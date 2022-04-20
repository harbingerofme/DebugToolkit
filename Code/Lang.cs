namespace DebugToolkit
{
    public static class Lang
    {
        //() denotes a list of specifc choices. : denotes a default. [] denotes a collection, and {} denotes freeform
        public const string
            ADDPORTAL_ARGS = "Requires 1 argument: add_portal (all|blue|celestial|gold|null|void|deepvoid)",
            BAN_ARGS = "Requires 1 argument: ({PlayerID:self}|{Playername})",
            BIND_ARGS = "Requires 2 arguments: {Key} {ConsoleCommands}",
            BIND_DELETE_ARGS = "Requires 1 argument: {Key}",
            CHANGETEAM_ARGS = "Requires 1 argument: change_team {TeamIndex:0/Neutral} ({PlayerID:self}|{Playername})",
            CREATEPICKUP_ARGS = "Requires 1 argument: create_pickup (\"coin\"|{localised_object_name}) (\"item\"|\"equip\")",
            FAMILYEVENT_ARGS = "Requires 0 arguments: force_family_event",
            FIXEDTIME_ARGS = "Requires 1 argument: fixed_time {NewTime}",
            GIVEEQUIP_ARGS = "Requires 1 (2 if from server) argument: give_equip {localised_object_name} ({PlayerID:self}|{Playername})",
            GIVEITEM_ARGS = "Requires 1 (2 if from server) argument: give_item {localised_object_name} {Count:1} ({PlayerID:self}|{Playername})",
            GIVELUNAR_ARGS = "Requires 0 arguments: give_lunar {Count:1}",
            GIVEMONEY_ARGS = "Requires 1 argument: give_money {Count} ({PlayerID:self}|{Playername}|\"all\")",
            GOD_ARGS = "Requires 0 arguments: god",
            NOCLIP_ARGS = "Requires 0 arguments: noclip",
            CURSORTELEPORT_ARGS = "Requires 0 arguments: teleport_on_cursor",
            LOADOUTSKIN_ARGS = "Requires 2 argument: loadout_set_skin_variant {skinIndex} {localised_objectname|self}",
            LOCKEXP_ARGS = "Requires 0 arguments: lock_exp",
            KICK_ARGS = "Requires 1 argument: ({PlayerID:self}|{Playername})",
            KILLALL_ARGS = "Requires 0 arguments: kill_all {TeamIndex:2/Monster}",
            NEXTBOSS_ARGS = "Requires 1 argument: next_boss ({localised_object_name}|{DirectorCard}) {(int)Count:1} {EliteIndex:-1/None}",
            NEXTSTAGE_ARGS = "Requires 0 arguments: next_stage {Stage}",
            NOENEMIES_ARGS = "Requires 0 arguments: no_enemies",
            PERM_ENABLE_ARGS = "Requires 0 argument: perm_enable",
            PERM_MOD_ARGS = "Requires 2 argument: perm_mod (PermissionLevel (0, 1, 2 OR None, SubAdmin, Admin) ({PlayerID}|{Playername}) ",
            RANDOM_ITEM_ARGS = "Requires 1 argument: random_items {Count} ({PlayerID:self}|{Playername})",
            REMOVEITEM_ARGS = "Requires 1 argument: remove_item {localised_object_name} ({Count}|\"all\") ({PlayerID:self}|{Playername})",
            REMOVEEQUIP_ARGS = "Requires 0 arguments: remove_equip ({PlayerID:self}|{Playername})",
            RESPAWN_ARGS = "Requires 0 arguments: respawn ({PlayerID:self}|{Playername})",
            SEED_ARGS = "Requires no or 1 argument: seed [new seed]",
            SPAWNAI_ARGS = "Requires 1 argument: spawn_ai {localised_objectname} [Count:1] [EliteIndex: Number(start at 0 or Name) : Fire / Gold / Haunted / Ice / Lightning / Lunar / Poison] [Braindead:0/false(0|1)] [TeamIndex:2/Monster]",
            SPAWNAS_ARGS = "Requires 1 argument: spawn_as {localised_objectname} ({PlayerID:self}|{Playername})",
            SPAWNBODY_ARGS = "Requires 1 argument: spawn_body {localised_objectname}",
            SPAWNINTERACTABLE_ARGS = "Requires 1 argument: spawn_interactable {InteractableSpawnCard}",
            TIMESCALE_ARGS = "Requires 1 argument: time_scale {TimeIncrement}",
            TRUEKILL_ARGS = "Requires 0 arguments: true_kill ({PlayerID:self}|{Playername})"
            ;

        public const string
            LISTITEM_ARGS = "List all item names and their IDs",
            LISTEQUIP_ARGS = "List all equipment items and their IDs",
            LISTAI_ARGS = "List all Masters and their language invariants",
            LISTBODY_ARGS  = "List all Bodies and their language invariants",
            LISTPLAYER_ARGS = "List all players and their ID",
            LISTSKIN_ARGS  = "Requires 0 arguments: list_skin ({localised_objectname}|\"all\"|\"body\" (separated by body)|\"self\"|:\"all\"})"
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
            PORTAL_NOTFOUND = "The specified portal could not be found. Valid portals: 'blue','gold','celestial','null','void','deepvoid','all'",
            RUNSETSTAGESCLEARED_HELP = "Sets the amount of stages cleared. This does not change the current stage.",
            SPAWN_ATTEMPT_1 = "Attempting to spawn: {0}",
            SPAWN_ATTEMPT_2 = "Attempting to spawn {0}: {1}",
            SPAWN_ERROR = "Could not spawn: ",
            NOTINARUN_ERROR = "This command only works when in a Run !",
            ALL = "ALL"
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
