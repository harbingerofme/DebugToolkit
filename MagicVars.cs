using System;
using System.Collections.Generic;
using System.Text;

namespace RoR2Cheats
{
    public static class MagicVars
    {
        public const string
            CREATEPICKUP_AMBIGIOUS_2 = "Could not choose between {0} and {1}, please be more precise. Consider using 'equip' or 'item' as the first argument.",
            CREATEPICKUP_ARGS = "Requires 1 or 2 arguments: "+ CREATEPICKUP_NAME + "{item|equipment|coin|localised_object_name} [localised_object_name]",
            CREATEPICKUP_NAME = "create_pickup",
            CREATEPICKUP_NOTFOUND = "Could not find any item nor equipment with that name. It's not a coin either.",
            CREATEPICKUP_SUCCES_1 = "Succesfully created the pickup {0}.",
            COUNTISNUMERIC = "Count must be numeric!",
            GIVEEQUIP_ARGS = "Requires 1 argument: give_equip {localised_object_name} {playername}",
            GIVEITEM_ARGS = "Requires 1 argument: give_item {localised_object_name} {count} {playername}",
            GIVELUNAR_2 = "{0} {1} lunar coins.",
            INTEGER_EXPECTED = "Argument must be a whole number.",
            OBJECT_NOTFOUND = "The requested object could not be found: ",
            OBSOLETEWARNING = "This command has become obsolete and will be removed in the next version. ",
            NEXTROUND_STAGE = "Invalid Stage. Please choose from the following: ",
            NEXTBOSS_ARGS = "Requires 1 argument: next_boss {localised_object_name|DirectorCard} {Count} {EliteIndex}",
            NOMESSAGE = "Yell at the modmakers if you see this message!",
            PARTIAL_IMPLEMENTATION = "WARNING: PARTIAL IMPLEMENTATION. WIP.",
            PLAYER_DEADRESPAWN = "Player is dead and cannot respawn.",
            PLAYER_NOTFOUND = "Specified player does not exist",
            PORTAL_NOTFOUND = "The specified portal could not be found: add_portal {blue,celestial,gold}",
            REMOVEITEM_ARGS = "Requires 1 argument: remove_item {localised_object_name} <count|[ALL]> <playername>",
            REMOVEEQUIP_ARGS = "Requires 0 arguments: remove_equip {playername}",
            RUNSETSTAGESCLEARED_HELP = "Sets the amount of stages cleared. This does not change the current stage.",
            SPAWN_ATTEMPT = "Attempting to spawn: ",
            SPAWN_ERROR = "Could not spawn: ",
            SPAWNAI_ARGS = "Requires 1 argument: spawn_ai {localised_objectname} {EliteIndex[0=Fire,1=Lightning,2=Ice,3=Poison,4=Haunted]} {TeamIndex[0=N,1=P,2=M]} {BrainDead[0,1]}",
            SPAWNAS_ARGS = "Requires 1 argument: spawn_as {localised_objectname} {playername}",
            SPAWNBODY_ARGS = "Requires 1 argument: spawn_body {localised_objectname}",
            TEAM_ARGS = "Requires 1 argument: change_team {TeamIndex[1=P,2=M]} {playername}",
            ALL = "ALL"
            ;
    }
}
