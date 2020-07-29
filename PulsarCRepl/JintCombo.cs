using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jint;
using Jint.Runtime.Interop;

namespace PulsarCRepl
{
    class JintAdditons
    {
        //Prefix to use in javascript, Assembly File Name
        private static readonly Dictionary<string, string> GameDllNames = new Dictionary<string, string>
        {
            {"cs_", "Assembly-CSharp"},
            {"cs1_", "Assembly-CSharp-firstpass"}
        };

        public static Engine AddGameSpecificClasses(Engine myengine, Dictionary<string, string> yourLibraries = null)
        {
            //
            var finalLibs = new Dictionary<string,string>(GameDllNames);
            if (yourLibraries != null)
            {
                yourLibraries.ToList().ForEach(x => finalLibs[x.Key] = x.Value);
            }
            
            foreach (var PrefixAndLibName in finalLibs)
            {
                var gameAssemblyClasses = Assembly.Load(PrefixAndLibName.Value).GetTypes()
                    .Where(item => item.IsClass && !item.IsSpecialName);
                foreach (var gameAssemblyClass in gameAssemblyClasses)
                {
                    var typename = PrefixAndLibName.Key + gameAssemblyClass.Name;
                    myengine = myengine.SetValue(typename,
                        TypeReference.CreateTypeReference(myengine, gameAssemblyClass));
                }
            }

            return myengine;
        }
    }
}