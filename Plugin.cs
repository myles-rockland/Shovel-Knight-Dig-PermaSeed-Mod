using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;
using BepInEx.Logging;
using System.IO;

namespace PermanentSeedPlugin
{
    //Plugin to load patch
    [BepInPlugin("rockm3000.skdig.permaseed", "PermaSeed Mod", "1.0.0.1")]
    [BepInProcess("skDig64.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin rockm3000.skdig.permaseed is loaded!");

            //Applying the mechanic patch
            var harmony = new Harmony("rockm3000.skdig.permaseed");

            var original = typeof(GameSessionController).GetMethod(nameof(GameSessionController.GenerateRandomSeed));
            var prefix = typeof(PermaSeedPatch).GetMethod(nameof(PermaSeedPatch.SetSameSeed));
            harmony.Patch(original, prefix: new HarmonyMethod(prefix));

            original = typeof(Player).GetMethod(nameof(Player.ActionPressedDown));
            prefix = typeof(ToggleSeedPatch).GetMethod(nameof(ToggleSeedPatch.ToggleRandomSeeds));
            harmony.Patch(original, prefix: new HarmonyMethod(prefix));

            //Applying the popup patch
            original = typeof(TitleScreen).GetMethod(nameof(TitleScreen.TitleScreenJingle));
            prefix = typeof(ShowingPopupPatch).GetMethod(nameof(ShowingPopupPatch.ChangeShowingPopup));
            harmony.Patch(original, prefix: new HarmonyMethod(prefix));

            //Applying the popup patch
            original = typeof(GenericMessagePopup).GetMethod(nameof(GenericMessagePopup.OpenQueue));
            prefix = typeof(PermaSeedPopupPatch).GetMethod(nameof(PermaSeedPopupPatch.SpawnPermaseedPopup));
            var postfix = typeof(PermaSeedPopupPatch).GetMethod(nameof(PermaSeedPopupPatch.ChangePopupText));
            harmony.Patch(original, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));

            //Applying the popup patch
            original = typeof(GenericMessagePopup).GetMethod(nameof(GenericMessagePopup.Close));
            postfix = typeof(PermaSeedClosePopupPatch).GetMethod(nameof(PermaSeedClosePopupPatch.ChangeNextPopupText));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            Logger.LogInfo("PermaSeed patch should be applied");
        }
    }

    //PermaSeed mechanic patch
    [HarmonyPatch(typeof(GameSessionController), nameof(GameSessionController.GenerateRandomSeed))]
    class PermaSeedPatch
    {
        private static ManualLogSource seedPatchLog = BepInEx.Logging.Logger.CreateLogSource("PermaSeedPatchLog");
        public static bool permanentSeedOn = true;
        [HarmonyPrefix]
        public static bool SetSameSeed(ref int __result)
        {
            if (permanentSeedOn)
            {
                seedPatchLog.LogInfo("Seed should be the same");
                __result = GameSessionController.Seed;
                return false;
            }
            return true;
        }
    }

    //PermaSeed mechanic patch
    [HarmonyPatch(typeof(Player), nameof(Player.ActionPressedDown))]
    class ToggleSeedPatch
    {
        private static ManualLogSource togglePatchLog = BepInEx.Logging.Logger.CreateLogSource("TogglePatchLog");
        [HarmonyPrefix]
        public static void ToggleRandomSeeds(Player __instance)
        {
            if (__instance.Input.m_HoldingUp)
            {
                PermaSeedPatch.permanentSeedOn = !PermaSeedPatch.permanentSeedOn;
                togglePatchLog.LogInfo("Permanent seed: " + (PermaSeedPatch.permanentSeedOn ? "ON" : "OFF"));
                UICanvas.UI.DisplayStageNameWithDelay("Permanent seed: " + (PermaSeedPatch.permanentSeedOn ? "ON" : "OFF"), 0);
            }
        }
    }

    //Show popup patch
    [HarmonyPatch(typeof(TitleScreen), nameof(TitleScreen.TitleScreenJingle))]
    class ShowingPopupPatch
    {
        [HarmonyPrefix]
        public static void ChangeShowingPopup(ref bool ___m_ShowingPopups)
        {
            ___m_ShowingPopups = true;
        }
    }

    //FireBall active mod popup patch
    [HarmonyPatch(typeof(GenericMessagePopup), nameof(GenericMessagePopup.OpenQueue))]
    class PermaSeedPopupPatch
    {
        private static ManualLogSource popupPatchLog = BepInEx.Logging.Logger.CreateLogSource("PopupPatchLog");
        public static bool textChanged = false;
        [HarmonyPrefix]
        public static void SpawnPermaseedPopup(GenericMessagePopup __instance, ref Queue<GenericMessagePopup.QueuedPopup> popupQueue)
        {
            popupQueue.Enqueue(new GenericMessagePopup.QueuedPopup(GenericMessagePopup.TYPE.TRUE_ENDING_COMPLETE, new Action(OverworldEvents.SetShownKnightmareModePopup), string.Empty, true));
        }

        [HarmonyPostfix]
        public static void ChangePopupText(GenericMessagePopup __instance)
        {
            if(!textChanged)
            {
                Transform childByName = Utilities.GetChildByName(__instance.m_Pannels[4].transform, "title text");
                Transform childByName2 = Utilities.GetChildByName(__instance.m_Pannels[4].transform, "body text");
                childByName.GetComponent<TextMeshProUGUI>().text = "permaseed mod activated!";
                childByName2.GetComponent<TextMeshProUGUI>().text = "you gained the power of <color=#00FF00>seeds<color=#FFFFFF>! press up + attack to toggle a permanent seed!";
                textChanged = true;
                popupPatchLog.LogInfo("Changed text to PermaSeed mod text");
            }
        }
    }

    //Popup close patch
    [HarmonyPatch(typeof(GenericMessagePopup), nameof(GenericMessagePopup.Close))]
    class PermaSeedClosePopupPatch
    {
        private static ManualLogSource nextPopupPatchLog = BepInEx.Logging.Logger.CreateLogSource("NextPopupPatchLog");
        [HarmonyPostfix]
        public static void ChangeNextPopupText(GenericMessagePopup __instance)
        {
            nextPopupPatchLog.LogInfo("Popup was closed.");
            if(!PermaSeedPopupPatch.textChanged)
            {
                Transform childByName = Utilities.GetChildByName(__instance.m_Pannels[4].transform, "title text");
                Transform childByName2 = Utilities.GetChildByName(__instance.m_Pannels[4].transform, "body text");
                childByName.GetComponent<TextMeshProUGUI>().text = "permaseed mod activated!";
                childByName2.GetComponent<TextMeshProUGUI>().text = "you gained the power of <color=#00FF00>seeds<color=#FFFFFF>! press up + attack to toggle a permanent seed!";
                PermaSeedPopupPatch.textChanged = true;
                nextPopupPatchLog.LogInfo("Changed text to PermaSeed mod text");
            }
        }
    }
}
