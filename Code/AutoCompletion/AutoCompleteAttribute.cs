using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DebugToolkit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AutoCompleteAttribute : Attribute
    {
        public string args;

        public AutoCompleteAttribute(string args)
        {
            this.args = args;
        }

        internal Result Parse(AutoCompleteParser parser)
        {
            var matches = Regex.Matches(args, @"\{(.*?)\}|\[(.*?)\]|<(.*?)>");
            var parameters = new List<string>[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                var groups = matches[i].Groups;
                var match = (!string.IsNullOrEmpty(groups[1].Value) ? groups[1]
                    : !string.IsNullOrEmpty(groups[2].Value) ? groups[2] : groups[3]
                ).Value.Split(':')[0];
                var options = Regex.Match(match, @"\((.*)\)");
                parameters[i] = new List<string>();
                if (options.Success)
                {
                    var names = options.Groups[1].Value.Split('|').Select(s => s.Trim());
                    foreach (var name in names)
                    {
                        var trimmed = name.Trim();
                        if (trimmed.StartsWith("'") && trimmed.EndsWith("'"))
                        {
                            parameters[i].Add(trimmed);
                        }
                        else
                        {
                            if (parser.TryGetStaticVariable(trimmed, out _) || parser.TryGetDynamicVariable(trimmed, out _))
                            {
                                parameters[i].Add(trimmed);
                            }
                        }
                    }
                }
                else
                {
                    match = match.Trim();
                    if (parser.TryGetStaticVariable(match, out _) || parser.TryGetDynamicVariable(match, out _))
                    {
                        parameters[i].Add(match);
                    }
                }
            }
            return new Result(parser, string.Join(" ", matches.Cast<Match>().Select(m => m.Groups[0].Value)), parameters);
        }

        internal class Result
        {
            internal AutoCompleteParser parser;
            internal string signature;
            internal List<string>[] parameters;

            internal Result(AutoCompleteParser parser, string signature, List<string>[] parameters)
            {
                this.parser = parser;
                this.signature = signature;
                this.parameters = parameters;
            }
        }
    }
}
