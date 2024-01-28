using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugToolkit
{
    /// <summary>
    /// Register autocompletion options for ConCommands with the AutoCompleteAttribute.
    /// </summary>
    public sealed class AutoCompleteParser
    {
        private readonly Dictionary<string, string[]> staticVariables = new Dictionary<string, string[]>();
        private readonly Dictionary<string, DynamicCatalog> dynamicVariables = new Dictionary<string, DynamicCatalog>();

        public AutoCompleteParser() { }

        /// <summary>
        /// Register a variable name to represent a string.
        /// </summary>
        /// <param name="name">Name of the variable</param>
        /// <param name="value">The value it represents</param>
        public void RegisterStaticVariable(string name, string value)
        {
            RegisterStaticVariable(name, new string[] { value });
        }

        /// <summary>
        /// Register a variable name to represent a static collection of strings.
        /// </summary>
        /// <param name="name">Name of the variable</param>
        /// <param name="values">Fixed-size iterable of the values</param>
        public void RegisterStaticVariable(string name, IEnumerable<string> values)
        {
            staticVariables[name] = values.ToArray();
        }

        /// <summary>
        /// Register a variable name to represent a dynamic collection of strings.
        /// </summary>
        /// <param name="name">Name of the variable</param>
        /// <param name="catalog">Iterable that may change in size or values</param>
        /// <param name="nestedField">Concatenated strings with "/" that represent the field to select (optional)</param>
        /// <param name="isToken">Whether the selected field is a language token (optional)</param>
        /// <param name="showIndex">Whether the final string will include its positional index in the collection (optional)</param>
        public void RegisterDynamicVariable(string name, IEnumerable<object> catalog, string nestedField = "", bool isToken = false, bool showIndex = true)
        {
            dynamicVariables[name] = new DynamicCatalog(catalog, nestedField, isToken, showIndex);
        }

        internal bool TryGetStaticVariable(string name, out string[] strings)
        {
            return staticVariables.TryGetValue(name, out strings);
        }

        internal bool TryGetDynamicVariable(string name, out DynamicCatalog catalog)
        {
            return dynamicVariables.TryGetValue(name, out catalog);
        }

        /// <summary>
        /// Scan the assembly for commands with autocompletion options.
        /// </summary>
        public void ScanAssembly()
        {
            foreach (var methodInfo in Assembly.GetCallingAssembly().GetTypes().SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)))
            {
                var autoCompletionAttribute = methodInfo.GetCustomAttributes(false).OfType<AutoCompleteAttribute>().DefaultIfEmpty(null).FirstOrDefault();
                if (autoCompletionAttribute != null)
                {
                    var conCommandAttributes = methodInfo.GetCustomAttributes(false).OfType<ConCommandAttribute>().ToArray();
                    if (conCommandAttributes.Length > 0)
                    {
                        var p = autoCompletionAttribute.Parse(this);
                        foreach (var conCommand in conCommandAttributes)
                        {
                            AutoCompleteManager.RegisterCommand(conCommand.commandName, p);
                        }
                    }
                }
            }
        }

        internal class DynamicCatalog
        {
            private readonly IEnumerable<object> catalog;
            private readonly string nestedField;
            private readonly bool isToken;
            private readonly bool showIndex;

            internal DynamicCatalog(IEnumerable<object> catalog, string nestedField, bool isToken, bool showIndex)
            {
                this.catalog = catalog;
                this.nestedField = nestedField;
                this.isToken = isToken;
                this.showIndex = showIndex;
            }

            internal IEnumerable<string> Rebuild()
            {
                var block = !string.IsNullOrEmpty(nestedField) ? nestedField.Split('/') : new string[0];
                var index = 0;
                foreach (object item in catalog)
                {
                    string itemString;
                    if (block.Length > 0)
                    {
                        var tmp = item.GetFieldValue<object>(block[0]);
                        for (int i = 1; i < block.Length; i++)
                        {
                            tmp = tmp.GetFieldValue<object>(block[i]);
                        }
                        itemString = tmp.ToString();
                    }
                    else
                    {
                        itemString = item.ToString();
                    }

                    if (itemString.Contains("(RoR"))
                    {
                        itemString = itemString.Substring(0, itemString.IndexOf('('));
                    }

                    itemString = isToken ? StringFinder.GetLangInvar(itemString) : StringFinder.RemoveSpacesAndAlike(itemString);
                    yield return showIndex ? $"{index}|{itemString}" : itemString;

                    index += 1;
                }
            }
        }
    }
}
