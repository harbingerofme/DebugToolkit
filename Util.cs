using System.Collections.Generic;
using RoR2;

namespace DebugToolkit
{
    internal static class Util
    {
        /// <summary>
        /// Returns a matched NetworkUser when provided to a player.
        /// </summary>
        /// <param name="args">(string[])args array</param>
        /// <param name="startLocation">(int)on the string array, at which index the player string starts at. Default value is 0</param>
        /// <returns>Returns a NetworkUser if a match is found, or null if not</returns>
        internal static NetworkUser GetNetUserFromString(List<string> args, int startLocation = 0)
        {
            if (args.Count > 0)
            {
                if (args[startLocation].StartsWith("\""))
                {
                    var startString = string.Join(" ", args);

                    var startIndex = startString.IndexOf('\"') + 1;
                    var length = startString.LastIndexOf('\"') - startIndex;

                    args[startLocation] = startString.Substring(startString.IndexOf('\"') + 1, length);
                }

                if (int.TryParse(args[startLocation], out int result))
                {
                    if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0)
                    {
                        return NetworkUser.readOnlyInstancesList[result];
                    }

                    Log.Message(Lang.PLAYER_NOTFOUND);
                    return null;
                }

                foreach (var n in NetworkUser.readOnlyInstancesList)
                {
                    if (n.userName.ToLower().Contains(args[startLocation].ToLower()))
                    {
                        return n;
                    }
                }
                
                return null;
            }

            return null;
        }
    }
}
