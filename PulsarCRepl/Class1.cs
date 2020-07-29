
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
        private bool ShowCheatHud;
        public string CodeString = "";
        Rect windowRect = new Rect(20, 20, 120, 50);
        public void mycode()
        {
            JintBits.RunMe();
        }
        public override void OnApplicationStart()
        {
            Action d = mycode;
            d.BeginInvoke(null,null);
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                ShowCheatHud = true;
            }
        }

        public override void OnGUI()
        {
            if (!ShowCheatHud) return;
            // Make a background box
            windowRect = GUILayout.Window(0, windowRect,ConsoleWindowDisplay, "Javascript Console");
            GUILayout.BeginArea(new Rect(0, 0, 200, 600));
            
            GUILayout.Box("Cheat Menu");
            CodeString = GUILayout.TextArea(CodeString);
            GUILayout.EndArea();
        }

        public void ConsoleWindowDisplay(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Insert Javascript Code below");
            CodeString = GUILayout.TextArea(CodeString);
            if (GUILayout.Button("Run Code"))
            {
                
            }
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