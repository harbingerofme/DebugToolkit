using System.Collections.Generic;

namespace RoR2Cheats
{
    public class ArgsHelper
    {

        public static string GetValue(List<string> args, int index)
        {
            if (index < args.Count && index >= 0)
            {
                return args[index];
            }

            return "";
        }
    }
}