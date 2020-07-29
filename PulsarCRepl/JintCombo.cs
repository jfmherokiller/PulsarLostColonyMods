using System.Reflection;
using Jint;
using Jint.Runtime.Interop;

namespace PulsarCRepl
{
    class JintAdditons
    {
        public static Engine AddUnityGameSpecifics(Engine myengine)
        {
            var GameAssenblyParts = Assembly.Load("Assembly-CSharp").GetTypes();
            
            foreach (var gameAssenblyPart in GameAssenblyParts)
            {
                if(!gameAssenblyPart.IsClass) continue;
                var typename = "cs_" +gameAssenblyPart.Name;
                myengine = myengine.SetValue(typename, TypeReference.CreateTypeReference(myengine, gameAssenblyPart));
            }
            return myengine;
        }
    }
}