using System.Collections.Generic;

namespace DebugToolkit
{
    internal static class AutoCompleteManager
    {
        private static readonly Dictionary<string, AutoCompleteAttribute.Result> commands = new Dictionary<string, AutoCompleteAttribute.Result>();

        internal static string CurrentCommand { get; private set; }
        internal static List<string>[] CurrentParameters { get; private set; }

        internal static void PrepareCommandOptions(string commandName)
        {
            commandName = commandName.ToLower();
            if (CurrentCommand == commandName)
            {
                return;
            }
            CurrentCommand = commandName;
            if (!commands.TryGetValue(commandName, out var data))
            {
                CurrentParameters = new List<string>[0];
                return;
            }
            var parser = data.parser;
            var options = new List<string>[data.parameters.Length];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new List<string>();
                foreach (var name in data.parameters[i])
                {
                    if (name.StartsWith("'") && name.EndsWith("'"))
                    {
                        options[i].Add(name.Trim('\''));
                    }
                    else if (parser.TryGetStaticVariable(name, out var strings))
                    {
                        options[i].AddRange(strings);
                    }
                    else if (parser.TryGetDynamicVariable(name, out var catalog))
                    {
                        options[i].AddRange(catalog.Rebuild());
                    }
                }
            }
            CurrentParameters = options;
        }

        internal static void ClearCommandOptions()
        {
            CurrentCommand = null;
            CurrentParameters = null;
        }

        internal static void RegisterCommand(string commandName, AutoCompleteAttribute.Result parameters)
        {
            commands[commandName.ToLower()] = parameters;
        }
    }
}
