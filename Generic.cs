using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RoR2Cheats
{
    static class Generic
    {

        private static Dictionary<string, UnityEngine.Object> resources = new Dictionary<string, UnityEngine.Object>();


        public static string Remove(this string String, string stringToRemove)
        {
            return String.Replace(stringToRemove, "");
        }

        public static void PrintFields(Type type, object instance)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            List<string> list = new List<string>();
            string listString = "";

            list.Add("============================================================================================");
            foreach (var item in fields)
            {
                list.Add(string.Format("{0}: {1}", item.Name, item.GetValue(instance)));
            }
            list.Add("============================================================================================");
            listString = string.Join("\n", list.ToArray());
            Debug.Log(listString);
        }

        public static TReturn FindResource<TReturn>(string name) where TReturn : UnityEngine.Object
        {
            if (resources.ContainsKey(name))
            {
                try
                {
                    return (TReturn)Convert.ChangeType(resources[name], typeof(TReturn));
                }
                catch (InvalidCastException)
                {
                    return default;
                }
            }
            else
            {
                foreach (var item in Resources.FindObjectsOfTypeAll<TReturn>())
                {
                    if (item.name.Equals(name))
                    {
                        resources.Add(name, item);
                        return (TReturn)Convert.ChangeType(item, typeof(TReturn));
                    }
                }
            }
            return default;
        }

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

        //CommandHelper written by Wildbook
        public class CommandHelper
        {
            public static void RegisterCommands(RoR2.Console self)
            {
                var types = typeof(CommandHelper).Assembly.GetTypes();
                var catalog = self.GetFieldValue<IDictionary>("concommandCatalog");

                foreach (var methodInfo in types.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)))
                {
                    var customAttributes = methodInfo.GetCustomAttributes(false);
                    foreach (var attribute in customAttributes.OfType<ConCommandAttribute>())
                    {
                        var conCommand = Reflection.GetNestedType<RoR2.Console>("ConCommand").Instantiate();

                        conCommand.SetFieldValue("flags", attribute.flags);
                        conCommand.SetFieldValue("helpText", attribute.helpText);
                        conCommand.SetFieldValue("action", (RoR2.Console.ConCommandDelegate)Delegate.CreateDelegate(typeof(RoR2.Console.ConCommandDelegate), methodInfo));

                        catalog[attribute.commandName.ToLower()] = conCommand;
                    }
                }
            }
        }
    }
}