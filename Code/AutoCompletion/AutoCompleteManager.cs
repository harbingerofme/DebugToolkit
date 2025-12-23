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
        internal static List<AutoCompleteParser.AutoCompleteOption>[] CurrentParameters { get; private set; }
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
                CurrentParameters = new List<AutoCompleteParser.AutoCompleteOption>[0];
                CurrentSignature = null;
                return;
            }
            var parser = data.parser;
            var options = new List<AutoCompleteParser.AutoCompleteOption>[data.parameters.Length];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new List<AutoCompleteParser.AutoCompleteOption>();
                foreach (var name in data.parameters[i])
                {
                    if (name.StartsWith("'") && name.EndsWith("'"))
                    {
                        options[i].Add(new AutoCompleteParser.AutoCompleteOption(name.Trim('\''), 0));
                    }
                    else if (parser.TryGetStaticVariable(name, out var staticCatalog))
                    {
                        options[i].AddRange(staticCatalog.options);
                    }
                    else if (parser.TryGetDynamicVariable(name, out var dynamicCatalog))
                    {
                        options[i].AddRange(dynamicCatalog.Rebuild());
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
            parser.RegisterStaticVariable("ai", MasterCatalog.allAiMasters.Select(i => $"{(int)i.masterIndex}|{i.name}|{StringFinder.GetLangInvar(StringFinder.GetMasterName(i))}"), 1);
            parser.RegisterStaticVariable("artifact", ArtifactCatalog.artifactDefs.Select(i => $"{(int)i.artifactIndex}|{i.cachedName}|{StringFinder.GetLangInvar(i.nameToken)}"), 1);
            parser.RegisterStaticVariable("body", BodyCatalog.allBodyPrefabBodyBodyComponents.Select(i => $"{(int)i.bodyIndex}|{i.name}|{StringFinder.GetLangInvar(i.baseNameToken)}"), 1);
            parser.RegisterStaticVariable("buff", BuffCatalog.buffDefs.Select(i => $"{(int)i.buffIndex}|{StringFinder.GetLangInvar(i.name)}"), 1);
            parser.RegisterStaticVariable("tier", ItemTierCatalog.allItemTierDefs.OrderBy(i => i.tier).Select(i => $"{(int)i.tier}|{i.name}"), 1);
            parser.RegisterStaticVariable("dot", DotController.dotDefs.Select((d, i) => $"{i}|{(DotController.DotIndex)i}"), 1);
            parser.RegisterStaticVariable("elite", new string[] { "-1|None" }.
                Concat(EliteCatalog.eliteDefs.Select(i => $"{(int)i.eliteIndex}|{i.name}|{StringFinder.GetLangInvar(i.modifierToken)}")),
                1
            );
            parser.RegisterStaticVariable("equip", EquipmentCatalog.equipmentDefs.Select(i => $"{(int)i.equipmentIndex}|{i.name}|{StringFinder.GetLangInvar(i.nameToken)}"), 1);
            parser.RegisterStaticVariable("item", ItemCatalog.allItemDefs.Select(i => $"{(int)i.itemIndex}|{i.name}|{StringFinder.GetLangInvar(i.nameToken)}"), 1);
            parser.RegisterStaticVariable("drone", DroneCatalog.allDroneDefs.Select(i => $"{(int)i.droneIndex}|{i.name}|{StringFinder.GetLangInvar(i.nameToken)}"), 1);
            parser.RegisterStaticVariable("specific_stage", SceneCatalog.allSceneDefs.Where(i => !i.isOfflineScene).Select(i => $"{(int)i.sceneDefIndex}|{i.cachedName}|{StringFinder.GetLangInvar(i.nameToken)}"), 1);
            parser.RegisterStaticVariable("team", new string[] { "-1|None" }.
                Concat(TeamCatalog.teamDefs.Select((t, i) => $"{i}|{(TeamIndex)i}")),
                1
            );

            parser.RegisterStaticVariable("permission_level", CollectEnumNames(typeof(Permissions.Level), typeof(int)), 1);

            parser.RegisterDynamicVariable("director_card", StringFinder.Instance.DirectorCards, "spawnCard", autocompleteIndex: 1);
            //parser.RegisterDynamicVariable("interactable", StringFinder.Instance.InteractableSpawnCards, autocompleteIndex: 1);
            parser.RegisterDynamicVariable("interactable", StringFinder.Instance.InteractableSpawnCards.Select(i => $"{StringFinder.Instance.InteractableSpawnCards.IndexOf(i)}|{i.name}|{StringFinder.GetLangInvar(i.prefab?.GetComponent<IDisplayNameProvider>()?.GetDisplayName())}"), autocompleteIndex: 1);

            parser.RegisterDynamicVariable("player", NetworkUser.instancesList, "userName");

            parser.RegisterStaticVariable("itemTypes", new string[] {
                //"0|None",
                "1|Permanent",
                "2|Temp",
                "3|Channelled",
                },
                1
            );

            parser.Scan(System.Reflection.Assembly.GetExecutingAssembly());
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

            string[] enumNames = Enum.GetNames(enumType);
            Array enumValues = Enum.GetValues(enumType);

            for (int i = 0; i < enumValues.Length; i++)
            {
                if (enumNames[i] != "Count")
                {
                    yield return $"{Convert.ChangeType(enumValues.GetValue(i), castTo)}|{enumNames[i]}";
                }
            }
        }
    }
}
