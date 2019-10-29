using System.Reflection;
using System;
using System.IO;
using RoR2;
using Console = System.Console;
using System.Text;

namespace CommandDiscover
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please supply an assembly.");
                return 1;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFile(args[0]);
            }
            catch (Exception e) when (e.Message == "Absolute path information is required.")
            {
                assembly = Assembly.LoadFile(Directory.GetCurrentDirectory() + @"\" + args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception loading '{args[0]}':" + e.Message);
                return 2;
            }

            StringBuilder justNames = new StringBuilder();
            StringBuilder namesHelp = new StringBuilder();
            StringBuilder allAttr = new StringBuilder();

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var types = assembly.GetTypes();
            foreach (Type type in types)
            {
                var methods = type.GetMethods(flags);
                foreach (MethodInfo method in methods)
                {
                    ConCommandAttribute CCA = method.GetCustomAttribute<ConCommandAttribute>();
                    if (CCA != null)
                    {
                        justNames.AppendLine(CCA.commandName);
                        namesHelp.AppendLine(CCA.commandName + " - " + CCA.helpText);
                        allAttr.AppendLine($"{CCA.commandName}, flags = {CCA.flags}, help= {CCA.helpText}");
                    }
                }
            }

            File.WriteAllText("justNames.txt", justNames.ToString());
            File.WriteAllText("NamesHelp.txt", namesHelp.ToString());
            File.WriteAllText("AllAttributes.txt", allAttr.ToString());
            return 0;
        }
    }
}
