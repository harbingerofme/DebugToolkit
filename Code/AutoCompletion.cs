using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using R2API.Utils;
using RoR2;

namespace DebugToolkit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class AutoCompletionAttribute : Attribute
    {
        internal readonly dynamic Catalog;
        internal readonly string NestedField;
        internal readonly bool Dynamic;

        public AutoCompletionAttribute(Type classType, string catalogName, string nestedField = "", bool dynamic = false)
        {
            Catalog = classType.GetFieldValue<dynamic>(catalogName);
            NestedField = nestedField;
            Dynamic = dynamic;
        }
    }

    internal static class ArgsAutoCompletion
    {
        private static readonly Dictionary<string, AutoCompletionAttribute[]> Commands;
        internal static HashSet<string> CommandsWithStaticArgs = new HashSet<string>();

        static ArgsAutoCompletion()
        {
            Commands = new Dictionary<string, AutoCompletionAttribute[]>();
        }

        internal static void GatherCommandsAndFillStaticArgs()
        {
            foreach (var methodInfo in Assembly.GetExecutingAssembly().GetTypes().SelectMany(x => x.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)))
            {
                var autoCompletionAttribute = methodInfo.GetCustomAttributes(false).OfType<AutoCompletionAttribute>().ToArray();
                var conCommandAttribute = (ConCommandAttribute)methodInfo.GetCustomAttributes(false).FirstOrDefault(x => x is ConCommandAttribute);

                if (autoCompletionAttribute.Length > 0 && conCommandAttribute != null)
                {
                    Commands.Add(conCommandAttribute.commandName + " ", autoCompletionAttribute);
                }
            }

            RetrieveCommandsArgs(out CommandsWithStaticArgs, Commands);
        }

        internal static HashSet<string> CommandsWithDynamicArgs()
        {
            RetrieveCommandsArgs(out var commandsWithDynamicArgs, Commands.Where(x => x.Value.Any(i => i.Dynamic)));

            return commandsWithDynamicArgs;
        }

        private static void RetrieveCommandsArgs(out HashSet<string> toFill, IEnumerable<KeyValuePair<string, AutoCompletionAttribute[]>> commands)
        {
            toFill = new HashSet<string>();

            foreach (var command in commands)
            {
                var commandName = command.Key;
                var acAttribute = command.Value;

                foreach (var attribute in acAttribute)
                {
                    foreach (object item in attribute.Catalog)
                    {
                        string itemString = "";
                        if (attribute.NestedField != string.Empty)
                        {
                            var block = attribute.NestedField.Split('/');
                            object tmp = null;
                            for (int i = 0; i < block.Length; i++)
                            {
                                if (tmp == null)
                                {
                                    tmp = item.GetFieldValue<object>(block[i]);
                                }
                                else
                                {
                                    tmp = tmp.GetFieldValue<object>(block[i]);
                                }
                            }

                            if (tmp != null)
                            {
                                itemString = tmp.ToString();
                            }
                        }
                        else
                        {
                            itemString = item.ToString();
                        }

                        if (itemString.Contains(" ") || itemString.Contains("'") || itemString.Contains("-"))
                        {
                            itemString = StringFinder.RemoveSpacesAndAlike(itemString);
                        }

                        if (itemString.Contains("(RoR"))
                        {
                            itemString = itemString.Substring(0, itemString.IndexOf('('));
                        }

                        if (!IsToken(itemString))
                        {
                            toFill.Add(commandName + itemString);
                        }
                        else
                        {
                            var languageDictionaries = typeof(Language).GetFieldValue<Dictionary<string, Dictionary<string, string>>>("languageDictionaries");
                            if (languageDictionaries.TryGetValue(Language.currentLanguage, out var dictionary))
                            {
                                foreach (var tokenAndInvar in dictionary)
                                {
                                    if (tokenAndInvar.Key.Contains(itemString.ToUpper()) &&
                                        IsToken(tokenAndInvar.Key))
                                    {
                                        if (!IsToken(tokenAndInvar.Value))
                                        {
                                            toFill.Add(commandName + StringFinder.RemoveSpacesAndAlike(tokenAndInvar.Value));
                                        }
                                    }
                                }
                            }
                        }

                        bool IsToken(string s)
                        {
                            return s.Contains("NAME");
                        }
                    }
                }
            }
        }
    }
}
