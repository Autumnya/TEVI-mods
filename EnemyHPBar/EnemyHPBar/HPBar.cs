using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Character;
using HarmonyLib;
using UnityEngine;

namespace EnemyHPBar
{
    [BepInPlugin("ekat.release0.EnemyHPBar", "EnemyHPBar", "1.0")]
    public class HPBar : BaseUnityPlugin
    {
        public static AssetBundle asset;
        public static GameObject barObject;

        public static ConfigEntry<bool> enablePlayerBar;
        public static ConfigEntry<bool> enableBossBar;
        public static List<Bar> bars = new List<Bar>();
        public static List<Bar> disappearing = new List<Bar>();
        public static CharacterManager _CharacterManager;

        public class Bar
        {
            public GameObject hpBar;
            public GameObject outsideBar;
            public GameObject insideBar;
            public GameObject hpSquare;
            public bool visible = false;
            public bool isHurt = false;

            public Bar()
            {
                hpBar = Instantiate(barObject);
                outsideBar = hpBar.transform.GetChild(0).gameObject;
                insideBar = hpBar.transform.GetChild(1).gameObject;
                hpSquare = hpBar.transform.GetChild(2).gameObject;
            }
            public void SetBar_OverHeight(CharacterBase enemy)
            {
                int layer = enemy.spranim_prefer.pixel.basesprite.sortingOrder;
                hpBar.transform.position = new Vector3(enemy.t.position.x, enemy.t.position.y + enemy.charHeight, enemy.t.position.z);
                outsideBar.GetComponent<SpriteRenderer>().sortingOrder = layer;
                insideBar.GetComponent<SpriteRenderer>().sortingOrder = layer + 1;
                hpSquare.GetComponent<SpriteRenderer>().sortingOrder = layer + 2;
            }
            public void SetBarPercent(float per)
            {
                if (per <= 0f)
                {
                    hpSquare.transform.localScale = new Vector3(0f, 4f, 1f);
                }
                else
                {
                    hpSquare.transform.localScale = new Vector3(120f * per, 4f, 1f);
                    hpSquare.transform.localPosition = new Vector3(-60f * (1f - per), 0f, 0f);
                    float r = per > 0.5f ? -1.56f * per + 1.56f : 0.78f;
                    float g = per < 0.5f ? 1.56f * per : 0.78f;
                    hpSquare.GetComponent<SpriteRenderer>().color = new Color(r, g, 0f, 1f);
                }
            }
            public void AppearByAlpha(float appearTime)
            {
                if (visible)
                    return;
                Color c1 = outsideBar.GetComponent<SpriteRenderer>().color;
                c1.a += Time.deltaTime / appearTime;
                Color c2 = insideBar.GetComponent<SpriteRenderer>().color;
                c2.a = c1.a;
                Color c3 = hpSquare.GetComponent<SpriteRenderer>().color;
                c3.a = c1.a;
                outsideBar.GetComponent<SpriteRenderer>().color = c1;
                insideBar.GetComponent<SpriteRenderer>().color = c2;
                hpSquare.GetComponent<SpriteRenderer>().color = c3;
                if (c1.a >= 1f)
                {
                    visible = true;
                }
            }
            public void DisappearByAlpha(float disappearTime, bool destoryThis)
            {
                if (!visible)
                    return;
                Color c1 = outsideBar.GetComponent<SpriteRenderer>().color;
                c1.a -= Time.deltaTime / disappearTime;
                Color c2 = insideBar.GetComponent<SpriteRenderer>().color;
                c2.a = c1.a;
                Color c3 = hpSquare.GetComponent<SpriteRenderer>().color;
                c3.a = c1.a;
                outsideBar.GetComponent<SpriteRenderer>().color = c1;
                insideBar.GetComponent<SpriteRenderer>().color = c2;
                hpSquare.GetComponent<SpriteRenderer>().color = c3;
                if (c1.a <= 0f)
                {
                    visible = false;
                    if (destoryThis)
                        Disappear();
                    return;
                }
            }
            public void Disappear()
            {
                Destroy(hpBar);
                Destroy(outsideBar);
                Destroy(insideBar);
                Destroy(hpSquare);
            }
        }
        private void Awake()
        {
            enablePlayerBar = Config.Bind("", "Enable Player HPBar", false);
            enableBossBar = Config.Bind("", "Enable Boss HPBar", false);
            asset = AssetBundle.LoadFromFile($"{Paths.GameRootPath}/BepInEx/plugins/EnemyHPBar/hpbar.ab");
            barObject = asset.LoadAsset<GameObject>("HPBar");
            Harmony.CreateAndPatchAll(typeof(HPBar));
        }
        private void Update()
        {

        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterManager), "Awake")]
        public static void CharacterManager_Awake_HarmonyPostfix(ref CharacterManager __instance)
        {
            foreach (Bar b in bars)
                b.Disappear();
            bars.Clear();
            _CharacterManager = __instance;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterManager), "_Update")]
        public static void CharacterManager__Update_HarmonyPostfix(ref CharacterManager __instance)
        {
            if (GameSystem.Instance.isAnyPause())
                return;
            while (bars.Count < _CharacterManager.characters.Count)
            {
                bars.Add(new Bar());
                Debug.Log($"[ekat.release0.EnemyHPBar]Add a new bar {bars.Count}");
            }
            for (int i = _CharacterManager.characters.Count - 1; i > -1; i--)
            {
                if (bars[i].visible)
                {
                    bars[i].SetBar_OverHeight(_CharacterManager.characters[i]);
                    bars[i].SetBarPercent((float)_CharacterManager.characters[i].health / _CharacterManager.characters[i].maxhealth);
                }
                if (_CharacterManager.characters[i].isBoss == BossType.BOSS)
                {
                    if (enableBossBar.Value)
                        bars[i].AppearByAlpha(0.1f);
                    else
                        bars[i].DisappearByAlpha(0.1f, false);
                    continue;
                }
                if (_CharacterManager.characters[i].isPlayer())
                {
                    if (enablePlayerBar.Value)
                        bars[i].AppearByAlpha(0.1f);
                    else
                        bars[i].DisappearByAlpha(0.1f, false);
                    continue;
                }
                if (!bars[i].visible && bars[i].isHurt)
                {
                    bars[i].AppearByAlpha(0.4f);
                }
            }
            for (int i = disappearing.Count - 1; i > -1; i--)
            {
                if (disappearing[i].visible)
                    disappearing[i].DisappearByAlpha(0.4f, true);
                else
                    disappearing.RemoveAt(i);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterBase), "ReduceHealth")]
        public static void CharacterBase_ReduceHealth_HarmonyPrefix(CharacterBase __instance)
        {
            bars[_CharacterManager.characters.IndexOf(__instance)].isHurt = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterBase), "DespawnMe")]
        public static void CharacterBase_DespawnMe_Prefix(CharacterBase __instance)
        {
            if (!__instance.isPlayer())
            {
                int index = _CharacterManager.characters.IndexOf(__instance);
                if (index == -1)
                    return;
                disappearing.Add(bars[index]);
                bars.RemoveAt(index);
                Debug.Log("[ekat.release0.EnemyHPBar]A character despawned");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterBase), "GameOver")]
        public static void CharacterBase_GameOver_Prefix()
        {
            foreach (Bar bar in bars)
            {
                bar.Disappear();
            }
            bars.Clear();
            foreach (Bar bar in disappearing)
            {
                bar.Disappear();
            }
            disappearing.Clear();
        }
    }
}
