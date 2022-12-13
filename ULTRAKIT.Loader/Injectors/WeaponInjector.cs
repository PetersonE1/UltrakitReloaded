﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using ULTRAKIT.Data;
using ULTRAKIT.Extensions;
using UnityEngine.UI;
using System.Linq;
using UMM;
using UnityEngine.InputSystem;

namespace ULTRAKIT.Loader.Injectors
{
    [HarmonyPatch(typeof(GunSetter), "ResetWeapons")]
    public class GunSetterPatch
    {
        public static List<List<GameObject>> modSlots = new List<List<GameObject>>();
        public static Dictionary<GameObject, bool> equippedDict = new Dictionary<GameObject, bool>();

        static void Postfix(GunSetter __instance)
        {
            foreach (var slot in modSlots)
            {
                foreach (var item in slot)
                {
                    if (item)
                    {
                        GameObject.Destroy(item);
                    }
                }

                if (__instance.gunc?.slots?.Contains(slot) ?? false)
                {
                    __instance.gunc.slots.Remove(slot);
                }
            }
            modSlots.Clear();
            foreach (var pair in WeaponLoader.registry)
            {
                foreach (var weap in pair.Value)
                {
                    if (!weap.Unlocked)
                    {
                        continue;
                    }

                    var slot = new List<GameObject>();

                    string loadOrder = string.Join(",", weap.equipOrder);
                    string equippedStatus = string.Join(",", weap.equipStatus);

                    UKMod.SetPersistentModData($@"{weap.modName}.{weap.id}.load", loadOrder, "ULTRAKIT");
                    UKMod.SetPersistentModData($@"{weap.modName}.{weap.id}.equip", equippedStatus, "ULTRAKIT");

                    for (int i = 0; i < weap.All_Variants.Length; i++)
                    {
                        var variant = weap.All_Variants[i];

                        if (!equippedDict.ContainsKey(variant))
                        {
                            int s = (int)Mathf.Repeat(i, weap.Variants.Length);
                            bool equipped = (i < weap.Variants.Length && weap.equipStatus[s] == 1) || (i >= weap.Variants.Length && weap.equipStatus[s] == 2);
                            equippedDict.Add(variant, equipped);
                        }
                    }
                    for (int i = 0; i < weap.Variants.Length; i++)
                    {
                        var variant = weap.All_Variants[weap.equipOrder[i]];
                        if (weap.equipStatus[weap.equipOrder[i]] == 2)
                        {
                            variant = weap.All_Variants[weap.equipOrder[i] + weap.Variants.Length];
                        }

                        if (!equippedDict[variant])
                        {
                            continue;
                        }

                        var go = GameObject.Instantiate(variant, __instance.transform);
                        go.SetActive(false);

                        foreach (var c in go.GetComponentsInChildren<Renderer>(true))
                        {
                            c.gameObject.layer = LayerMask.NameToLayer("AlwaysOnTop");

                            var glow = c.gameObject.GetComponent<Glow>();

                            if (glow)
                            {
                                c.material.shader = Shader.Find("psx/railgun");
                                c.material.SetFloat("_EmissivePosition", 5);
                                c.material.SetFloat("_EmissiveStrength", glow.glowIntensity);
                                c.material.SetColor("_EmissiveColor", glow.glowColor);
                            }
                            else
                            {
                                c.material.shader = Shader.Find(c.material.shader.name);
                            }   
                        }

                        var wi = go.AddComponent<WeaponIcon>();
                        wi.weaponIcon = weap.Icons[i];
                        wi.glowIcon = weap.Icons[i];
                        wi.variationColor = i;
                        wi.SetPrivate("variationColoredMaterials", go.GetComponentsInChildren<Material>().Where(k => k.name.Contains(".var")).ToArray() ?? new Material[0]);
                        wi.SetPrivate("variationColoredRenderers", go.GetComponentsInChildren<Renderer>().Where(k => k.material.name.Contains(".var")).ToArray() ?? new Renderer[0]);
                        wi.SetPrivate("variationColoredImages", new Image[0]);

                        var field = typeof(StyleHUD).GetField("weaponFreshness", BindingFlags.NonPublic | BindingFlags.Instance);
                        Dictionary<GameObject, float> freshnessList = field.GetValue(MonoSingleton<StyleHUD>.Instance) as Dictionary<GameObject, float>;
                        freshnessList.Add(go, 10f);
                        field.SetValue(MonoSingleton<StyleHUD>.Instance, freshnessList);

                        slot.Add(go);

                        __instance.gunc.allWeapons.Add(go);
                    }

                    __instance.gunc.slots.Add(slot);
                    modSlots.Add(slot);
                    UltrakitInputManager.UpdateKeyBinds();
                }
            }
        }
    }

    [HarmonyPatch(typeof(GunControl))]
    class GunControlPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPrefix(GunControl __instance)
        {
            if (PlayerPrefs.GetInt("CurSlo", 1) > __instance.slots.Count)
            {
                PlayerPrefs.SetInt("CurSlo", 1);
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(GunControl __instance)
        {
            if ((UltrakitInputManager.Slot7?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 7 && (__instance.slots[6]?.Count > 0 || __instance.currentSlot != 7))
            {
                if (__instance.slots[6]?.Count > 0 && __instance.slots[6][0] != null)
                {
                    __instance.SwitchWeapon(7, __instance.slots[6]);
                }
            }

            if ((UltrakitInputManager.Slot8?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 8 && (__instance.slots[7]?.Count > 0 || __instance.currentSlot != 8))
            {
                if (__instance.slots[7]?.Count > 0 && __instance.slots[7][0] != null)
                {
                    __instance.SwitchWeapon(8, __instance.slots[7]);
                }
            }

            if ((UltrakitInputManager.Slot9?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 9 && (__instance.slots[8]?.Count > 0 || __instance.currentSlot != 9))
            {
                if (__instance.slots[8]?.Count > 0 && __instance.slots[8][0] != null)
                {
                    __instance.SwitchWeapon(9, __instance.slots[8]);
                }
            }

            if ((UltrakitInputManager.Slot10?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 10 && (__instance.slots[9]?.Count > 0 || __instance.currentSlot != 10))
            {
                if (__instance.slots[9]?.Count > 0 && __instance.slots[9][0] != null)
                {
                    __instance.SwitchWeapon(10, __instance.slots[9]);
                }
            }

            if ((UltrakitInputManager.Slot11?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 11 && (__instance.slots[10]?.Count > 0 || __instance.currentSlot != 11))
            {
                if (__instance.slots[10]?.Count > 0 && __instance.slots[10][0] != null)
                {
                    __instance.SwitchWeapon(11, __instance.slots[10]);
                }
            }

            if ((UltrakitInputManager.Slot12?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 12 && (__instance.slots[11]?.Count > 0 || __instance.currentSlot != 12))
            {
                if (__instance.slots[11]?.Count > 0 && __instance.slots[11][0] != null)
                {
                    __instance.SwitchWeapon(12, __instance.slots[11]);
                }
            }
            
            if ((UltrakitInputManager.Slot13?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 13 && (__instance.slots[12]?.Count > 0 || __instance.currentSlot != 13))
            {
                if (__instance.slots[12]?.Count > 0 && __instance.slots[12][0] != null)
                {
                    __instance.SwitchWeapon(13, __instance.slots[12]);
                }
            }

            if ((UltrakitInputManager.Slot14?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 14 && (__instance.slots[13]?.Count > 0 || __instance.currentSlot != 14))
            {
                if (__instance.slots[13]?.Count > 0 && __instance.slots[13][0] != null)
                {
                    __instance.SwitchWeapon(14, __instance.slots[13]);
                }
            }

            if ((UltrakitInputManager.Slot15?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 15 && (__instance.slots[14]?.Count > 0 || __instance.currentSlot != 15))
            {
                if (__instance.slots[14]?.Count > 0 && __instance.slots[14][0] != null)
                {
                    __instance.SwitchWeapon(15, __instance.slots[14]);
                }
            }

            if ((UltrakitInputManager.Slot16?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 16 && (__instance.slots[15]?.Count > 0 || __instance.currentSlot != 16))
            {
                if (__instance.slots[15]?.Count > 0 && __instance.slots[15][0] != null)
                {
                    __instance.SwitchWeapon(16, __instance.slots[15]);
                }
            }

            if ((UltrakitInputManager.Slot17?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 17 && (__instance.slots[16]?.Count > 0 || __instance.currentSlot != 17))
            {
                if (__instance.slots[16]?.Count > 0 && __instance.slots[16][0] != null)
                {
                    __instance.SwitchWeapon(17, __instance.slots[16]);
                }
            }

            if ((UltrakitInputManager.Slot18?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 18 && (__instance.slots[17]?.Count > 0 || __instance.currentSlot != 18))
            {
                if (__instance.slots[17]?.Count > 0 && __instance.slots[17][0] != null)
                {
                    __instance.SwitchWeapon(18, __instance.slots[17]);
                }
            }

            if ((UltrakitInputManager.Slot19?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 19 && (__instance.slots[18]?.Count > 0 || __instance.currentSlot != 19))
            {
                if (__instance.slots[18]?.Count > 0 && __instance.slots[18][0] != null)
                {
                    __instance.SwitchWeapon(19, __instance.slots[18]);
                }
            }

            if ((UltrakitInputManager.Slot20?.WasPerformedThisFrame ?? false) && __instance.slots.Count >= 20 && (__instance.slots[19]?.Count > 0 || __instance.currentSlot != 20))
            {
                if (__instance.slots[19]?.Count > 0 && __instance.slots[19][0] != null)
                {
                    __instance.SwitchWeapon(20, __instance.slots[19]);
                }
            }
        }
    }
}
