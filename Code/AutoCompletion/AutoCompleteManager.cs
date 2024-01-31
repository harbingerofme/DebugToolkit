using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DebugToolkit
{
    internal static class AutoCompleteManager
    {
        private static readonly Dictionary<string, AutoCompleteAttribute.Result> commands = new Dictionary<string, AutoCompleteAttribute.Result>();

        internal static string CurrentCommand { get; private set; }
        internal static List<string>[] CurrentParameters { get; private set; }
        internal static string CurrentSignature { get; private set; }

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
                CurrentSignature = null;
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
            CurrentSignature = data.signature;
        }

        internal static void ClearCommandOptions()
        {
            CurrentCommand = null;
            CurrentParameters = null;
            CurrentSignature = null;
        }

        internal static void RegisterCommand(string commandName, AutoCompleteAttribute.Result parameters)
        {
            commands[commandName.ToLower()] = parameters;
        }

        internal static void RegisterAutoCompleteCommands()
        {
            var parser = new AutoCompleteParser();
            parser.RegisterStaticVariable("0", "0");
            parser.RegisterStaticVariable("1", "1");
            parser.RegisterStaticVariable("ai", MasterCatalog.allAiMasters.Select(i => $"{(int)i.masterIndex}|{StringFinder.GetLangInvar(i.name)}"));
            parser.RegisterStaticVariable("body", BodyCatalog.allBodyPrefabBodyBodyComponents.Select(i => $"{i.bodyIndex}|{StringFinder.GetLangInvar(i.gameObject.name)}"));
            parser.RegisterStaticVariable("buff", BuffCatalog.buffDefs.Select(i => $"{i.buffIndex}|{StringFinder.GetLangInvar(i.name)}"));
            parser.RegisterStaticVariable("droptable", ItemTierCatalog.allItemTierDefs.OrderBy(i => i.tier).Select(i => $"{(int)i.tier}|{i.name}"));
            parser.RegisterStaticVariable("elite", new string[] { "-1|None" }.
                Concat(EliteCatalog.eliteDefs.Select(i => $"{i.eliteIndex}|{i.name}|{StringFinder.GetLangInvar(i.modifierToken)}"))
            );
            parser.RegisterStaticVariable("equip", EquipmentCatalog.equipmentDefs.Select(i => $"{i.equipmentIndex}|{i.name}|{StringFinder.GetLangInvar(i.nameToken)}"));
            parser.RegisterStaticVariable("item", ItemCatalog.allItemDefs.Select(i => $"{i.itemIndex}|{i.name}|{StringFinder.GetLangInvar(i.nameToken)}"));
            parser.RegisterStaticVariable("specific_stage", SceneCatalog.indexToSceneDef.Select(i => i._cachedName));

            parser.RegisterStaticVariable("dot", CollectEnumNames(typeof(DotController.DotIndex), typeof(sbyte)).Skip(1));
            parser.RegisterStaticVariable("permission_level", CollectEnumNames(typeof(Permissions.Level), typeof(int)));
            parser.RegisterStaticVariable("team", CollectEnumNames(typeof(TeamIndex), typeof(sbyte)));

            parser.RegisterDynamicVariable("director_card", StringFinder.Instance.DirectorCards, "spawnCard");
            parser.RegisterDynamicVariable("interactable", StringFinder.Instance.InteractableSpawnCards);
            parser.RegisterDynamicVariable("player", NetworkUser.instancesList, "userName");

            parser.ScanAssembly();
        }

        private static IEnumerable<string> CollectEnumNames(Type enumType, Type castTo)
        {
            if (enumType == null)
            {
                Log.Message("Input type is null", Log.LogLevel.Warning, Log.Target.Bepinex);
                yield break;
            }
            if (!enumType.IsEnum)
            {
                Log.Message("Input type is not enum: " + enumType.Name, Log.LogLevel.Warning, Log.Target.Bepinex);
                yield break;
            }
            foreach (var field in enumType.GetFields())
            {
                var name = field.Name;
                if (name != "value__" && name != "Count")
                {
                    yield return $"{Convert.ChangeType(Enum.Parse(enumType, name), castTo)}|{name}";
                }
            }
        }
    }
}
