using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AtkPlugin;
using Harmony;
using MelonLoader;
using System.Reflection;
using System.Reflection.Emit;

[assembly: MelonModInfo(typeof(Myplugin), "Ak AntiCheat Bypass (kind of)", "1.0", "Noah")]
[assembly: MelonModGame(null, null)]

namespace AtkPlugin
{
    public class Myplugin : MelonMod
    {
        public override void OnPreInitialization()
        {
            MelonModLogger.Log("Test");
            HarmonyInstance.DEBUG = true;
        }

        public override void OnUpdate()
        {
            PhotonNetwork.offlineMode = true;
        }
    }

    [HarmonyPatch(typeof(CodeStage.AntiCheat.Detectors.InjectionDetector))]
    [HarmonyPatch("AssemblyAllowed")]
    class CheatPatchOne
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false; // make sure you only skip if really necessary
        }
    }

    [HarmonyPatch(typeof(CodeStage.AntiCheat.Detectors.InjectionDetector))]
    [HarmonyPatch("FindInjectionInCurrentAssemblies")]
    class CheatPatchTwo
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false; // make sure you only skip if really necessary
        }
    }

    [HarmonyPatch(typeof(CodeStage.AntiCheat.Detectors.InjectionDetector))]
    [HarmonyPatch("LoadAndParseAllowedAssemblies")]
    class CheatPatchThree
    {
        static bool Prefix()
        {
            return false; // make sure you only skip if really necessary
        }
    }
    
    [HarmonyPatch(typeof(PLUILoadMenu))]
    [HarmonyPatch("ClickEngage")]
    class CheatPatchFour
    {
        static bool Prefix()
        {
            if(PhotonNetwork.offlineMode != true) PLUILoadMenu.Instance.GameNameField.text = "-cheats enabled-";
            return true; // make sure you only skip if really necessary
        }
    }
}