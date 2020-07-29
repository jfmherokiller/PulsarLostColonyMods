
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using MelonLoader;

using PulsarCRepl;
using UnityEngine;

[assembly: MelonModInfo(typeof(PulsarCRepMod), "PulsarCheats2", "1.0", "Author Name")]
[assembly: MelonModGame(null, null)]
namespace PulsarCRepl
{
    
    public class PulsarCRepMod : MelonMod
    {
        private JintConsoleGui myconsole;
        public void mycode()
        {
            JintBits.RunMe();
        }
        public override void OnApplicationStart()
        {
            StartConsoleBasedRepl();
            myconsole = new JintConsoleGui();
        }

        private void StartConsoleBasedRepl()
        {
            Action d = mycode;
            d.BeginInvoke(null, null);
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                myconsole.OpenConsole();
            }
        }

        public override void OnGUI()
        {
            myconsole.OnGUICode();
        }
    }

    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            CosturaUtility.Initialize();
        }
    }
}