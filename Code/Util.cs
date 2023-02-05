using HG.GeneralSerializer;
using RoR2;
using System.Collections.Generic;

namespace DebugToolkit
{
    public static class Util
    {
        /// <summary>
        /// Returns a matched NetworkUser when provided to a player.
        /// </summary>
        /// <param name="args">(string[])args array</param>
        /// <param name="startLocation">(int)on the string array, at which index the player string starts at. Default value is 0</param>
        /// <returns>Returns a NetworkUser if a match is found, or null if not</returns>
        internal static NetworkUser GetNetUserFromString(List<string> args, int startLocation = 0)
        {
            if (args.Count > 0 && startLocation < args.Count)
            {
                if (args[startLocation].StartsWith("\""))
                {
                    var startString = string.Join(" ", args);

                    var startIndex = startString.IndexOf('\"') + 1;
                    var length = startString.LastIndexOf('\"') - startIndex;

                    args[startLocation] = startString.Substring(startIndex, length);
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

        /// <summary>
        /// Try to find the CharacterBody for a matched NetworkUser when provided to a player.
        /// </summary>
        /// <param name="args">(string[])args array</param>
        /// <param name="startLocation">(int)on the string array, at which index the player string starts at</param>
        /// <param name="body">the body of the matched player</param>
        /// <param name="playerName">the display name of the player</param>
        /// <returns>True if the player was found. False otherwise</returns>
        internal static bool GetBodyFromUser(List<string> args, int startLocation, out CharacterBody body, out string playerName)
        {
            var user = GetNetUserFromString(args, startLocation);
            if (user != null)
            {
                body = user.GetCurrentBody();
                playerName = user.masterController.GetDisplayName();
                return true;
            }
            body = null;
            playerName = null;
            return false;
        }

        /// <summary>
        /// Try to parse a bool that's either formatted as "true"/"false" or a whole number "0","1". Values above 0 are considered "truthy" and values equal or lower than zero are considered "false".
        /// </summary>
        /// <param name="input">the string to parse</param>
        /// <param name="result">the result if parsing was correct.</param>
        /// <returns>True if the string was parsed correctly. False otherwise</returns>
        internal static bool TryParseBool(string input, out bool result)
        {
            if (bool.TryParse(input, out result))
            {
                return true;
            }
            if (int.TryParse(input, out int val))
            {
                result = val > 0;
                return true;
            }
            return false;
        }
    }
}
