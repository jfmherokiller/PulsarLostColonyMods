using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Harmony;
using MelonLoader;
using PulsarMod;
using UnityEngine;

// ...
[assembly: MelonModInfo(typeof(MyMod), "PulsarCheats", "1.0", "Author Name")]
[assembly: MelonModGame(null, null)]

namespace PulsarMod
{
    public class MyMod : MelonMod
    {
        private bool ShowCheatHud;

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
            GUILayout.BeginArea(new Rect(0, 0, 200, 600));
            GUILayout.Box("Cheat Menu");

            if (GUILayout.Button("Add 1 mil credits"))
            {
                PLServer.Instance.CurrentCrewCredits += 1000000;
            }

            if (GUILayout.Button("flip internal build current value:" + PLNetworkManager.Instance.IsInternalBuild))
            {
                if (PLNetworkManager.Instance.IsInternalBuild)
                {
                    PLNetworkManager.Instance.VersionString =
                        PLNetworkManager.Instance.VersionString.GetDecrypted().Replace("i", "");
                    PLNetworkManager.Instance.IsInternalBuild = false;
                }
                else
                {
                    PLNetworkManager.Instance.VersionString =
                        $"{PLNetworkManager.Instance.VersionString.GetDecrypted()}i";
                    PLNetworkManager.Instance.IsInternalBuild = true;
                }
            }

            if (GUILayout.Button("Add 5 resources"))
            {
                PLServer.Instance.CurrentUpgradeMats += 5;
            }
            if (GUILayout.Button("Decrese Chaos by 1"))
            {
                if(((PLServer.Instance.ChaosLevel.GetDecrypted() -1.0f) <= 0.0f))
                {
                    PLServer.Instance.ChaosLevel = 0;
                }
                else
                {
                    PLServer.Instance.ChaosLevel = PLServer.Instance.ChaosLevel.GetDecrypted() - 1;
                }
            }
            if (GUILayout.Button("Print Chat Commands"))
            {
                ExtractCommands();
            }
            if (GUILayout.Button("Comet Info"))
            {
                var Comet = PLGlobal.Instance.Galaxy.AllSectorInfos.First(item =>
                    item.Value.m_VisualIndication == ESectorVisualIndication.COMET).Value;
                MelonModLogger.Log(Comet.Name);
                MelonModLogger.Log(Comet.ID.ToString());
                MelonModLogger.Log(Comet.ToString());
            }

            if (GUILayout.Button("Kill All Nearby Hostiles"))
            {
                killNearbyEnemies();
            }
            // Make the second button.
            if (GUILayout.Button("Close cheats"))
            {
                ShowCheatHud = false;
            }

            GUILayout.EndArea();
        }

        private void ExtractCommands()
        {
            //Grab the chat method
            var method = typeof(PLNetworkManager).GetMethod("ProcessCurrentChatText").GetRuntimeBaseDefinition();
            //extract the available chat commands and write them to the console
            var strings = StringExtractors.FindLiterals(method).Distinct()
                .Where(Starget => (Starget.Trim().Replace("\r", "").Replace("\n", "") != "") && Starget.StartsWith("/")).ToList();
            foreach (var IString in strings)
            {
                PLServer.Instance.ClientAddToShipLog("SYS",IString,Color.cyan);
            }
            
        }

        public void killNearbyEnemies()
        {
            foreach (PLPawnBase plpawnBase in PLGameStatic.Instance.AllPawnBases.Where(mpawn => !mpawn.GetIsFriendly()))
            {
                //filters for more specific like if pawn is in room
                if (ExtraUtiltiies.ScanPawnCheckIfValid(plpawnBase))
                {
                    plpawnBase.TakeDamage(float.MaxValue,true,PLServer.Instance.AllPlayers.First().GetPlayerID());
                }
            }
        }
    }
}