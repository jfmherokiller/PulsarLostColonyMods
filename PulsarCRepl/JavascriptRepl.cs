using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;
using MelonLoader;
using Console = System.Console;
namespace PulsarCRepl
{
    class JintBits
    {
        public static void RunMe()
        {
            var engine = new Engine(cfg => { cfg.AllowClr(AppDomain.CurrentDomain.GetAssemblies()); });

            engine
                .SetValue("print", new Action<object>(Value =>
                {
                    MelonModLogger.Log(Value.ToString());
                }))
                .SetValue("load", new Func<string, object>(
                    path => engine.Execute(File.ReadAllText(path))
                        .GetCompletionValue()));
            engine = JintAdditons.AddUnityGameSpecifics(engine);
                
           MelonModLogger.Log("Type 'exit' to leave, " +
                              "'print()' to write on the console, " +
                              "'load()' to load scripts.");
           MelonModLogger.Log("");

            var defaultColor = Console.ForegroundColor;
            while (true)
            {
                Console.ForegroundColor = defaultColor;
                MelonModLogger.Log("jint> ");
                var input = Console.ReadLine();
                if (input == "exit")
                {
                    return;
                }

                try
                {
                    var result = engine.GetValue(engine.Execute(input).GetCompletionValue());
                    if (result.Type != Types.None && result.Type != Types.Null && result.Type != Types.Undefined)
                    {
                        var str = TypeConverter.ToString(engine.Json.Stringify(engine.Json,
                            Arguments.From(result, Undefined.Instance, "  ")));
                        MelonModLogger.Log("=> {0}", str);
                    }
                }
                catch (JavaScriptException je)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    MelonModLogger.Log(je.ToString());
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    MelonModLogger.Log(e.Message);
                }
            }
        }
    }
}