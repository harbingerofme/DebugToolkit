using BepInEx.Configuration;
using R2API.MiscHelpers;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DebugToolkit.Code
{
    internal static class MacroSystem
    {
        private class MacroConfigEntry
        {
            internal ConfigEntry<string> ConfigEntry { get; }
            private string[] BlobArray { get; }

            internal string KeyBind { get; private set; }
            internal string[] ConsoleCommands { get; set; }
            private string _consoleCommandsBlob;

            internal MacroConfigEntry(ConfigEntry<string> configEntry)
            {
                BlobArray = SplitBindCmd(configEntry.Value);
                ConfigEntry = configEntry;
            }

            private static string[] SplitBindCmd(string bindCmdBlob)
            {
                return bindCmdBlob.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            }

            internal bool IsCorrectlyFormatted()
            {
                if (BlobArray.Length < 3)
                {
                    Log.Message($"Missing parameters for macro config entry called {ConfigEntry.Definition.Key}.", Log.LogLevel.ErrorClientOnly);
                    return false;
                }

                KeyBind = BlobArray[1];
                _consoleCommandsBlob = BlobArray[2];
                ConsoleCommands = SplitConsoleCommandsBlob(_consoleCommandsBlob);

                try
                {
                    Input.GetKeyDown(KeyBind);
                }
                catch (ArgumentException)
                {
                    Log.Message($"Specified key : {KeyBind} for the macro : {_consoleCommandsBlob} does not exist.", Log.LogLevel.ErrorClientOnly);
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

        private const string MACRO_SECTION_NAME = "Macros";
        private const string DEFAULT_MACRO_NAME = "Do not remove this example macro";
        private const string DEFAULT_MACRO_KEYBIND = "f15";
        private const string DEFAULT_MACRO_VALUE = "dt_bind " + DEFAULT_MACRO_KEYBIND + " help";
        private const string DEFAULT_MACRO_DESCRIPTION = "Custom keybind for executing console commands.";
        private const char DEFAULT_COMMAND_SEPARATOR = ';';

        private const string MACRO_MINI_TUTORIAL =
            "\nMust start with dt_bind {KeyBind} {ConsoleCommands}.\n" +
            "Example : dt_bind x noclip;kill_all\n" +
            "When you'll press x key on keyboard it'll activate noclip and kill every monsters.\n" +
            "For adding new macros, just add new lines under the example, must be formatted like this :\n" +
            "Macro 2 = dt_bind z no_enemies;give_item hoof 10\n" +
            "Macro 3 = dt_bind x give_item dagger 5;give_item syringe 10\n" +
            "Or use the in-game console and use the dt_bind console command.\n" +
            "When doing it from the in game console, don't forget to use double quotes, especially when chaining commands !\n" +
            "dt_bind b \"give_item dio 1;spawn_ai 1 beetle\"\n" +
            "You can also delete existing bind like this:\n" +
            "dt_bind_delete {KeyBind}";

        internal static void Init()
        {
            DebugToolkit.Configuration.Save();
            Reload();

            DebugToolkit.Configuration.SettingChanged += OnMacroSettingChanged;
            Run.onRunStartGlobal += Run_onRunStartGlobal;
        }

        private static IEnumerable<ChatBox> chatboxes = null;

        private static void Run_onRunStartGlobal(Run obj)
        {
            chatboxes = HUD.readOnlyInstanceList.Select(hud => hud.GetComponent<ChatBox>());
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
            var anyChatInputActive = chatboxes?.Any(chat => chat && chat.inputField && chat.inputField.isActiveAndEnabled) ?? false;

            if (!ConsoleWindow.instance && !anyChatInputActive)
            {
                foreach (var (keyBind, macroConfigEntry) in MacroConfigEntries)
                {
                    if (Input.GetKeyDown(keyBind))
                    {
                        foreach (var consoleCommand in macroConfigEntry.ConsoleCommands)
                        {
                            RoR2.Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList.FirstOrDefault(), consoleCommand);
                        }
                    }
                }
            }
        }

        [ConCommand(commandName = "dt_bind", flags = ConVarFlags.None,
            helpText = "Bind a key to execute specific commands." + Lang.BIND_ARGS)]
        private static void CCBindMacro(ConCommandArgs args)
        {
            if (args.Count == 2)
            {
                BindExampleMacro();

                // We only want 2 substrings. (the key bind, and the console commands)
                var bindCmdBlob = "dt_bind " + string.Join(" ", args.userArgs);

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
