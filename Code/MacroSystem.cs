using BepInEx.Configuration;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DebugToolkit.Code
{
    internal static class MacroSystem
    {
        private static readonly string[] MODIFIERS = [
            "left alt",
            "left ctrl",
            "left shift",
            "right alt",
            "right ctrl",
            "right shift"
        ];

        private class MacroConfigEntry
        {
            internal ConfigEntry<string> ConfigEntry { get; }
            private string[] BlobArray { get; }

            internal string KeyBind { get; private set; }
            internal string Key { get; private set; }
            internal string[] Modifiers { get; private set; }
            internal string[] MissingModifiers { get; private set; }
            internal string[] ConsoleCommands { get; set; }
            private string _consoleCommandsBlob;

            internal MacroConfigEntry(ConfigEntry<string> configEntry)
            {
                BlobArray = SplitBindCmd(configEntry.Value);
                ConfigEntry = configEntry;
            }

            private static string[] SplitBindCmd(string bindCmdBlob)
            {
                var match = Regex.Match(bindCmdBlob, @"dt_bind\s+(\((?<key>.+)\)\s+(?<command>.+)|(?<key>[^\s]+)\s+(?<command>.+))");
                if (match.Success)
                {
                    return [match.Groups["key"].Value, match.Groups["command"].Value];
                }
                return [];
            }

            internal bool IsCorrectlyFormatted()
            {
                if (BlobArray.Length < 2)
                {
                    Log.Message($"Missing parameters for macro config entry called {ConfigEntry.Definition.Key}.", Log.LogLevel.ErrorClientOnly);
                    return false;
                }

                KeyBind = BlobArray[0];
                _consoleCommandsBlob = BlobArray[1];
                ConsoleCommands = SplitConsoleCommandsBlob(_consoleCommandsBlob);
                // Split key combinations with +, but '+' and '[+]' can be legitimate keys
                var keys = Regex.Matches(KeyBind, @"(.+?)(?:(?<!\[)\+(?!\])|$)").Select(x => x.Groups[1].Value).ToHashSet();
                var modifiers = new List<string>();
                var missingModifiers = new List<string>();
                foreach (var modifier in MODIFIERS)
                {
                    if (keys.Contains(modifier))
                    {
                        modifiers.Add(modifier);
                        keys.Remove(modifier);
                    }
                    else
                    {
                        missingModifiers.Add(modifier);
                    }
                }
                if (keys.Count == 0)
                {
                    Log.Message($"No key was defined for the macro '{KeyBind}'.", Log.LogLevel.ErrorClientOnly);
                    return false;
                }
                else if (keys.Count > 1)
                {
                    Log.Message($"Multiple keys '{string.Join("+", keys)}' were defined for the macro '{KeyBind}'.", Log.LogLevel.ErrorClientOnly);
                    return false;
                }
                Modifiers = [..modifiers];
                MissingModifiers = [..missingModifiers];
                Key = keys.First();

                try
                {
                    Input.GetKeyDown(Key);
                }
                catch (ArgumentException)
                {
                    Log.Message($"Specified key '{Key}' for the macro '{KeyBind}' does not exist.", Log.LogLevel.ErrorClientOnly);
                    return false;
                }

                return true;
            }
        }

        private static readonly Dictionary<string, MacroConfigEntry> MacroConfigEntries = new Dictionary<string, MacroConfigEntry>();

        private static readonly System.Reflection.PropertyInfo OrphanedEntriesProperty =
            typeof(ConfigFile).GetProperty("OrphanedEntries",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private static Dictionary<ConfigDefinition, string> OrphanedEntries =>
            (Dictionary<ConfigDefinition, string>)OrphanedEntriesProperty.GetValue(DebugToolkit.Configuration);

        private static ConfigEntry<bool> AllowKeybindsWithUi;

        private const string MACRO_SECTION_NAME = "Macros";
        private const string DEFAULT_MACRO_NAME = "Do not remove this example macro";
        private const string DEFAULT_MACRO_KEYBIND = "f15";
        private const string DEFAULT_MACRO_VALUE = "dt_bind " + DEFAULT_MACRO_KEYBIND + " help";
        private const string DEFAULT_MACRO_DESCRIPTION = "Custom keybind for executing console commands.";
        private const char DEFAULT_COMMAND_SEPARATOR = ';';

        private const string MACRO_MINI_TUTORIAL =
            "\nMust start with dt_bind {KeyBind} {ConsoleCommands}.\n" +
            "Example : dt_bind x noclip;kill_all\n" +
            "When you'll press x key on keyboard it'll activate noclip and kill every monster.\n" +
            "For adding new macros, just add new lines under the example, must be formatted like this :\n" +
            "Macro 2 = dt_bind z no_enemies;give_item hoof 10\n" +
            "Macro 3 = dt_bind x give_item dagger 5;give_item syringe 10\n" +
            "Or use the in-game console and use the dt_bind console command.\n" +
            "When doing it from the in game console, don't forget to use double quotes, especially when chaining commands !\n" +
            "dt_bind b \"give_item dio 1;spawn_ai beetle 1\"\n" +
            "Binding a key whose name has a space also requires double quotes from the console:\n" +
            "dt_bind \"page down\" kill_all\n" +
            "While here it is formatted with brackets like this:\n" +
            "dt_bind (page down) kill_all\n" +
            "It is also possible to define a key combination with \"+\" and any combination of \"alt\", \"ctrl\", or \"shift\" as modifiers:\n" +
            "dt_bind \"left shift+r\" respawn\n" +
            "You can also delete existing bind like this:\n" +
            "dt_bind_delete {KeyBind}";

        internal static void Init()
        {
            DebugToolkit.Configuration.Save();
            Reload();

            DebugToolkit.Configuration.SettingChanged += OnMacroSettingChanged;
            RoR2Application.onLoad += CollectInspectorObjects;
        }

        private static GameObject runtimeInspector;
        private static GameObject unityInspector;

        private static void CollectInspectorObjects()
        {
            var objs = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var obj in objs)
            {
                if (obj.gameObject.name == "RuntimeInspectorCanvas")
                {
                    runtimeInspector = obj.gameObject;
                }
                else if (obj.gameObject.name == "com.sinai.unityexplorer_Root")
                {
                    unityInspector = obj.gameObject;
                }
            }
        }

        private static void OnMacroSettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (e.ChangedSetting.Definition.Section == MACRO_SECTION_NAME)
            {
                Reload();
            }
        }

        private static void Reload()
        {
            MacroConfigEntries.Clear();
            DebugToolkit.Configuration.Reload();

            BindExampleMacro();

            BindExistingEntries();
            RetrieveOrphanedMacroEntries();

            BindSettings();
        }

        private static void BindExampleMacro()
        {
            AddMacroFromConfigEntry(DebugToolkit.Configuration.Bind(MACRO_SECTION_NAME, DEFAULT_MACRO_NAME,
                DEFAULT_MACRO_VALUE, DEFAULT_MACRO_DESCRIPTION + MACRO_MINI_TUTORIAL));
        }

        private static void BindExistingEntries()
        {
            foreach (var configDef in DebugToolkit.Configuration.Keys)
            {
                if (configDef.Section == MACRO_SECTION_NAME && configDef.Key != DEFAULT_MACRO_NAME)
                {
                    AddMacroFromConfigEntry(DebugToolkit.Configuration.Bind(configDef, DEFAULT_MACRO_VALUE, new ConfigDescription(DEFAULT_MACRO_DESCRIPTION)));
                }
            }
        }

        private static void RetrieveOrphanedMacroEntries()
        {
            var orphanedEntries = OrphanedEntries;
            var newEntries = new List<(ConfigDefinition, string)>();

            foreach (var (configDef, value) in orphanedEntries)
            {
                if (configDef.Section == MACRO_SECTION_NAME && !string.IsNullOrEmpty(value))
                {
                    newEntries.Add((configDef, value));
                }
            }

            foreach (var (configDef, value) in newEntries)
            {
                orphanedEntries.Remove(configDef);

                AddMacroFromConfigEntry(BindNewConfigEntry(value));
            }
        }

        private static void BindSettings()
        {
            AllowKeybindsWithUi = DebugToolkit.Configuration.Bind("Macros.Settings", "AllowKeybindsWithUi", true,
                "Allow keybinds to execute when a UI window is open, e.g., Console or an Inspector.");
        }

        private static ConfigEntry<string> BindNewConfigEntry(string customValue)
        {
            var configEntry = DebugToolkit.Configuration.Bind(MACRO_SECTION_NAME,
                "Macro " + (MacroConfigEntries.Count + 1), DEFAULT_MACRO_VALUE, DEFAULT_MACRO_DESCRIPTION);
            configEntry.Value = customValue;

            return configEntry;
        }

        private static void AddMacroFromConfigEntry(ConfigEntry<string> macroEntry)
        {
            var macroConfigEntry = new MacroConfigEntry(macroEntry);
            if (macroConfigEntry.IsCorrectlyFormatted())
            {
                MacroConfigEntries[macroConfigEntry.KeyBind] = macroConfigEntry;
            }
        }

        private static string[] SplitConsoleCommandsBlob(string consoleCommandsBlob)
        {
            return consoleCommandsBlob.Split(new[] { DEFAULT_COMMAND_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static void Update()
        {
            if (!IsAnyInputFieldActive()
                && (!PauseManager.pauseScreenInstance || !PauseManager.pauseScreenInstance.activeSelf)
                && (AllowKeybindsWithUi.Value || !IsAnyUIWindowOpen())
            )
            {
                // Iterating on a copy of the keys in case a macro executes a bind/delete and modifies the collection
                var keyBinds = new List<string>(MacroConfigEntries.Keys);
                foreach (var keyBind in keyBinds)
                {
                    if (MacroConfigEntries.TryGetValue(keyBind, out var macroConfigEntry))
                    {
                        if (Input.GetKeyDown(macroConfigEntry.Key)
                            && macroConfigEntry.Modifiers.All(Input.GetKey)
                            && !macroConfigEntry.MissingModifiers.Any(Input.GetKey))
                        {
                            foreach (var consoleCommand in macroConfigEntry.ConsoleCommands)
                            {
                                RoR2.Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList.FirstOrDefault(), consoleCommand);
                            }
                        }
                    }
                }
            }
        }

        private static bool IsAnyUIWindowOpen()
        {
            return ConsoleWindow.instance
                || runtimeInspector && runtimeInspector.activeSelf
                || unityInspector && unityInspector.activeSelf;
        }

        private static bool IsAnyInputFieldActive()
        {
            return (MPEventSystem.instancesList != null && MPEventSystem.instancesList.Any(eventSystem => eventSystem && eventSystem.currentSelectedGameObject))
                || (UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject);
        }

        [ConCommand(commandName = "dt_bind", flags = ConVarFlags.None,
            helpText = "Bind a key to execute specific commands." + Lang.BIND_ARGS)]
        private static void CCBindMacro(ConCommandArgs args)
        {
            if (args.Count == 2)
            {
                BindExampleMacro();

                // We only want 2 substrings. (the key bind, and the console commands)
                // We need to identify keys that can have spaces, e.g. "page down", since
                // the bind arguments are parsed from the config by space splitting.
                // Quotes seem to get messed up in the config file, so brackets it is.
                var bindCmdBlob = "dt_bind ";
                if (args[0].Contains(" "))
                {
                    bindCmdBlob += $"({args[0]}) {args[1]}";
                }
                else
                {
                    bindCmdBlob += string.Join(" ", args.userArgs);
                }

                var keyBind = args[0];
                var consoleCommandsBlob = args[1];
                if (MacroConfigEntries.TryGetValue(keyBind, out var existingMacro))
                {
                    existingMacro.ConfigEntry.Value = bindCmdBlob;
                    existingMacro.ConsoleCommands = SplitConsoleCommandsBlob(consoleCommandsBlob);
                }
                else
                {
                    AddMacroFromConfigEntry(BindNewConfigEntry(bindCmdBlob));
                }
            }
            else if (args.Count == 1)
            {
                Log.Message(MacroConfigEntries.TryGetValue(args[0], out var existingMacro)
                    ? existingMacro.ConfigEntry.Value
                    : "This key has no macro associated to it.");
            }
            else
            {
                Log.Message("Usage Error. " + Lang.BIND_ARGS, Log.LogLevel.ErrorClientOnly);
            }
        }

        [ConCommand(commandName = "dt_bind_delete", flags = ConVarFlags.None,
            helpText = "Remove a custom bind from the macro system of DebugToolkit." + Lang.BIND_DELETE_ARGS)]
        private static void CCBindDeleteMacro(ConCommandArgs args)
        {
            if (args.Count == 1)
            {
                var keyBind = args[0];

                if (keyBind == DEFAULT_MACRO_KEYBIND)
                {
                    Log.Message("You can't delete the default macro.", Log.LogLevel.ErrorClientOnly);
                    return;
                }

                if (MacroConfigEntries.TryGetValue(keyBind, out var macroEntry))
                {
                    DebugToolkit.Configuration.Remove(macroEntry.ConfigEntry.Definition);
                    DebugToolkit.Configuration.Save();
                    MacroConfigEntries.Remove(keyBind);
                }
            }
            else
            {
                Log.Message("Usage Error. " + Lang.BIND_DELETE_ARGS, Log.LogLevel.ErrorClientOnly);
            }
        }

        [ConCommand(commandName = "dt_bind_reload", flags = ConVarFlags.None,
            helpText = "Reload the macro system of DebugToolkit." + Lang.BIND_DELETE_ARGS)]
        private static void CCBindReloadMacro(ConCommandArgs _)
        {
            Reload();
        }
    }
}
