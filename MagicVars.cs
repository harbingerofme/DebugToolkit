using System;
using System.Collections.Generic;
using System.Text;

namespace RoR2Cheats
{
    public static class MagicVars
    {
        public static string SPAWN_ERROR = "Could not spawn: ",
            SPAWN_ATTEMPT = "Attempting to spawn ",
            PLAYER_NOTFOUND = "Specified player does not exist",
            PLAYER_DEADRESPAWN = "Player is dead and cannot respawn.",
            OBJECT_NOTFOUND = "The requested object could not be found: ",
            GIVEEQUIP_ARGS = "Requires 1 argument: give_equip {localised_object_name} {playername}",
            GIVEITEM_ARGS = "Requires 1 argument: give_item {localised_object_name} {count} {playername}",
            SPAWNAS_ARGS = "Requires 1 argument: spawn {localised_objectname} {playername}",
            INTEGER_EXPECTED = "Argument must be a whole number.",
            RUNSETSTAGESCLEARED_HELP = "Sets the amount of stages cleared. This does not change the current stage.",
            OBSOLETEWARNING = "This command has become obsolete and will be removed in the next version. "
            ;
    }
}
