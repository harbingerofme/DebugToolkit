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
        /// <param name="startLocation">(int)on the string array, at which index the player string is. Default value is 0</param>
        /// <returns>Returns a NetworkUser if a match is found, or null if not</returns>
        internal static NetworkUser GetNetUserFromString(List<string> args, int index = 0)
        {
            if (args.Count > 0 && index < args.Count)
            {
                return StringFinder.Instance.GetPlayerFromPartial(args[index]);
            }
            return null;
        }

        /// <summary>
        /// Find the target CharacterMaster for a matched player string or pinged entity.
        /// </summary>
        /// <param name="args">(ConCommandArgs)command arguments</param>
        /// <param name="index">(int)on the string array, at which index the target string is</param>
        /// <returns>Returns the found master. Null otherwise</returns>
        internal static CharacterMaster GetTargetFromArgs(ConCommandArgs args, int index)
        {
            if (args.Count > 0 && index < args.Count)
            {
                if (args.sender != null && args[index].ToUpperInvariant() == Lang.PINGED)
                {
                    return Hooks.GetPingedTarget(args.senderMaster).master;
                }
                return GetNetUserFromString(args.userArgs, index)?.master;
            }
            return null;
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
