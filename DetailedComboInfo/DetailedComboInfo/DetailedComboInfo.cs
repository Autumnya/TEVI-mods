using BepInEx;
using HarmonyLib;
using UnityEngine;
using TMPro;
using System;

namespace DetailedComboInfo
{
    public class Preload
    {
        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(Preload));
        }
    }

    [BepInPlugin("ekat.release0.DetailedComboInfo", "DetailedComboInfo", "1.0")]
    public class DetailedComboInfo : BaseUnityPlugin
    {
        public static TextMeshPro dpsText;
        public static float damagePerSecond = 0f;
        public static int totalDamage = 0;
        public static float damageTime = 0f;
        public static bool acting = false;
        public static bool patched = false;

        private static bool damaged()
        {
            return totalDamage > 0;
        }

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(DetailedComboInfo));
        }
        private void Update()
        {

        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComboSystem), "_Update")]
        public static void ComboSystem__Update_Postfix()
        {
            if (GameSystem.Instance.isAnyPause())
            {
                return;
            }
            if (damaged())
            {
                damageTime += Time.deltaTime;
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveManager), "LoadGame")]
        public static void SaveManager_LoadGame_Postfix()
        {
            acting = false;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComboSystem), "UpdateText")]
        public static void ComboSystem_UpdateText_Postfix(ComboSystem __instance)
        {
            if (!acting)
            {
                dpsText = Traverse.Create(__instance).Field("tmp").GetValue<TextMeshPro>();
                dpsText.transform.localPosition = new Vector3(dpsText.transform.localPosition.x, dpsText.transform.localPosition.y - 86, 0);
                acting = true;
            }
            totalDamage = __instance.totalDamage;
            string text1 = __instance.GetCombo().ToString() + "HITS!";
            string text2;

            if (__instance.GetCombo() > 1)
            {
                if (damageTime > 1f)
                    text2 = string.Format("{0:F2}", totalDamage / damageTime);
                else
                    text2 = string.Format("{0:F2}", totalDamage);
                dpsText.text = $"{text1}\n\n\n{totalDamage}TTD!\n{text2}DPS!";
            }
            else
                dpsText.text = $"{text1}\n\n\n\n ";
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComboSystem), "ComboBreak")]
        public static void ComboSystem_ComboBreak_Postfix(ComboSystem __instance)
        {
            if (Traverse.Create(__instance).Field("Combo").GetValue<int>() == 0)
            {
                totalDamage = 0;
                damagePerSecond = 0f;
                damageTime = 0f;
            }
        }
    }
}
