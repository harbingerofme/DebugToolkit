using System;
using System.Collections.Generic;
using System.Text;

namespace RoR2Cheats
{
    public static class MagicVars
    {
        public const string
            CONFIG_SECTION_FOV = "FOV",
            FOVBASE_DESCR = "Your base Field of vision",
            FOVBASE_SHORTDESCR = "Base FOV",
            FOVMULTI_DESCR = "What FOV gets multiplied by while sprinting",
            FOVMULTI_SHORTDESCR = "sprint Fov Multiplier",
            GIVEEQUIP_ARGS = "Requires 1 argument: give_equip {localised_object_name} {playername}",
            GIVEITEM_ARGS = "Requires 1 argument: give_item {localised_object_name} {count} {playername}",
            INTEGER_EXPECTED = "Argument must be a whole number.",
            OBJECT_NOTFOUND = "The requested object could not be found: ",
            OBSOLETEWARNING = "This command has become obsolete and will be removed in the next version. ",
            PLAYER_DEADRESPAWN = "Player is dead and cannot respawn.",
            PLAYER_NOTFOUND = "Specified player does not exist",
            PORTAL_NOTFOUND = "The specified portal could not be found: add_portal {blue|celestial|gold}",
            RUNSETSTAGESCLEARED_HELP = "Sets the amount of stages cleared. This does not change the current stage.",
            SPAWN_ATTEMPT = "Attempting to spawn: ",
            SPAWN_ERROR = "Could not spawn: ",
            SPAWNAS_ARGS = "Requires 1 argument: spawn_as {localised_objectname} {playername}",
            SPAWNBODY_ARGS = "Requires 1 argument: spawn_body {localised_objectname}",
            TEAM_ARGS = "Requires 1 argument: change_team {TeamIndex} {playername}"
            ;
    }
}
