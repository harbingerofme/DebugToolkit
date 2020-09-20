using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using R2API.MiscHelpers;
using RoR2;
using UnityEngine;

namespace DebugToolkit.Code
{
    internal static class MacroSystem
    {
        private class BindCmd
        {
            internal string Value { get; }
            private readonly string[] _array;

            internal string KeyBind { get; private set; }
            internal string[] ConsoleCommands { get; private set; }
            private string _consoleCommandsBlob;

            internal BindCmd(string bindCmdBlob)
            {
                Value = bindCmdBlob;
                _array = bindCmdBlob.Split(new[] {' '}, 3, StringSplitOptions.RemoveEmptyEntries);
            }

            internal bool IsCorrectlyFormatted()
            {
                if (_array.Length < 3)
                {
                    Log.Message("Missing parameters.", Log.LogLevel.ErrorClientOnly);
                    return false;
                }

                KeyBind = _array[1];
                _consoleCommandsBlob = _array[2];
                ConsoleCommands = _consoleCommandsBlob.Split(new[] { DEFAULT_COMMAND_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

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

        private static readonly Dictionary<string, string[]> KeysToMacros = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, ConfigEntry<string>> MacroConfigEntries = new Dictionary<string, ConfigEntry<string>>();

        private static readonly System.Reflection.PropertyInfo OrphanedEntriesProperty =
            typeof(ConfigFile).GetProperty("OrphanedEntries",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private static Dictionary<ConfigDefinition, string> OrphanedEntries =>
            (Dictionary<ConfigDefinition, string>) OrphanedEntriesProperty.GetValue(DebugToolkit.Configuration);

        private const string MACRO_SECTION_NAME = "Macros";
        private const string DEFAULT_MACRO_DESCRIPTION = "Custom keybind for executing console commands.";
        private const string DEFAULT_MACRO_KEYBIND = "x";
        private const string DEFAULT_MACRO_EXAMPLE = "bind " + DEFAULT_MACRO_KEYBIND + " noclip;time_scale 1";
        private const char DEFAULT_COMMAND_SEPARATOR = ';';

        private const string MACRO_MINI_TUTORIAL = 
            "\nMust start with bind {Key} {ConsoleCommands}.\n" + 
            "Example : bind x noclip;kill_all\n" + 
            "When you'll press x key on keyboard it'll activate noclip and kill every monsters.\n" + 
            "For adding new macros, just add new lines formatted like this :\n" + 
            "Macro 2 = bind z no_enemies;give_item hoof 10\n" + 
            "Macro 3 = bind z give_item dagger 5;give_item syringe 10\n" + 
            "Or use the in-game console and use the bind console command : bind b give_item dio 1";

        internal static void Init()
        {
            var firstMacroEntry = DebugToolkit.Configuration.Bind("Macros", "Macro 1",
                DEFAULT_MACRO_EXAMPLE, DEFAULT_MACRO_DESCRIPTION + MACRO_MINI_TUTORIAL);
            MacroConfigEntries.Add(DEFAULT_MACRO_KEYBIND, firstMacroEntry);

            GetMacrosFromConfigFile();
        }

        private static void GetMacrosFromConfigFile()
        {
            FixOrphanedMacroEntries();

            foreach (var (keyBind, macroEntry) in MacroConfigEntries)
            {
                var bindCmd = new BindCmd(macroEntry.Value);
                if (bindCmd.IsCorrectlyFormatted())
                {
                    AddMacro(keyBind, bindCmd.ConsoleCommands);
                }
                else
                {
                    Log.Message("Bind command not correctly formatted. Example usage : " + DEFAULT_MACRO_EXAMPLE,
                        Log.LogLevel.ErrorClientOnly);
                }
            }
        }

        private static void FixOrphanedMacroEntries()
        {
            var orphanedEntries = OrphanedEntries;
            var newEntries = new Dictionary<ConfigDefinition, string>();

            foreach (var (configDef, value) in orphanedEntries)
            {
                if (configDef.Section == MACRO_SECTION_NAME && !string.IsNullOrEmpty(value))
                {
                    newEntries.Add(configDef, value);
                }
            }

            foreach (var (configDef, value) in newEntries)
            {
                orphanedEntries.Remove(configDef);
                var bindCmd = new BindCmd(value);
                if (bindCmd.IsCorrectlyFormatted())
                {
                    AddMacroToConfigFile(bindCmd);
                }
            }
        }

        internal static void Update()
        {
            if (!RoR2.UI.ConsoleWindow.instance)
            {
                foreach (var (keyBind, consoleCommands) in KeysToMacros)
                {
                    if (Input.GetKeyDown(keyBind))
                    {
                        foreach (var consoleCommand in consoleCommands)
                        {
                            RoR2.Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList.FirstOrDefault(), consoleCommand);
                        }
                    }
                }
            }
        }

        private static void AddMacroToConfigFile(BindCmd bindCmd, string macroDescription = null)
        {
            if (MacroConfigEntries.TryGetValue(bindCmd.KeyBind, out var existingMacro))
            {
                existingMacro.Value = bindCmd.Value;
            }
            else
            {
                var macroEntry = DebugToolkit.Configuration.Bind(MACRO_SECTION_NAME,
                    "Macro " + (MacroConfigEntries.Count + 1), DEFAULT_MACRO_EXAMPLE, 
                    macroDescription ?? DEFAULT_MACRO_DESCRIPTION);
                macroEntry.Value = bindCmd.Value;
                MacroConfigEntries.Add(bindCmd.KeyBind, macroEntry);   
            }
        }

        private static void AddMacro(string keyBind, string[] consoleCommands)
        {
            KeysToMacros[keyBind] = consoleCommands;
        }

        [ConCommand(commandName = "bind", flags = ConVarFlags.None,
            helpText = "Bind a key to execute specific commands." + Lang.BIND_ARGS)]
        private static void CCBindMacro(ConCommandArgs args)
        {
            // We only want 2 substrings. (the key bind, and the console commands)
            var bindCmdBlob = "bind " + string.Join(" ", args.userArgs);
            var bindCmd = new BindCmd(bindCmdBlob);

            if (bindCmd.IsCorrectlyFormatted())
            {
                AddMacroToConfigFile(bindCmd);
                AddMacro(bindCmd.KeyBind, bindCmd.ConsoleCommands);
            }
            else
            {
                Log.Message("Usage Error. " + Lang.BIND_ARGS, Log.LogLevel.ErrorClientOnly);
            }
        }

        [ConCommand(commandName = "bind_delete", flags = ConVarFlags.None,
            helpText = "Remove a custom bind from the macro system of DebugToolkit." + Lang.BIND_DELETE_ARGS)]
        private static void CCBindDeleteMacro(ConCommandArgs args)
        {
            if (args.Count == 1)
            {
                var keyBind = args[0];
                if (MacroConfigEntries.TryGetValue(keyBind, out var macroEntry))
                {
                    macroEntry.BoxedValue = null;
                    KeysToMacros.Remove(keyBind);
                }
            }
            else
            {
                Log.Message("Usage Error. " + Lang.BIND_DELETE_ARGS, Log.LogLevel.ErrorClientOnly);
            }
        }

        [ConCommand(commandName = "bind_reload", flags = ConVarFlags.None,
            helpText = "Reload the macro system of DebugToolkit." + Lang.BIND_DELETE_ARGS)]
        private static void CCBindReloadMacro(ConCommandArgs args)
        {
            DebugToolkit.Configuration.Reload();
            GetMacrosFromConfigFile();
        }
    }
}
