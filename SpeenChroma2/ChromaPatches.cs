using System;
using HarmonyLib;
using UnityEngine;
using XDMenuPlay;
using XDMenuPlay.Customise;

namespace SpeenChroma2
{
    public static class ChromaPatches
    {
        [HarmonyPatch(
            typeof(ColorValueWrapper),
            MethodType.Constructor,
            new Type[]
            {
                typeof(NoteColorType),
                typeof(int),
                typeof(IPlayerValue),
                typeof(IPlayerValue),
                typeof(IPlayerValue),
            })]
        [HarmonyPostfix]
        private static void ColorValueWrapper_Constructor_Postfix(
            ColorValueWrapper __instance,
            NoteColorType noteType,
            int colorProfileIndex,
            IPlayerValue hueValue,
            IPlayerValue saturationValue,
            IPlayerValue lightnessValue)
        {
            Main.Log(colorProfileIndex + " " + __instance.NoteType + " " + __instance.Hue);
            if (colorProfileIndex == 0)
            {
                ChromaManager.AddColorBlender(noteType, GameSystemSingleton<ColorSystem, ColorSystemSettings>.Instance.ColorBlenderForNoteColorType(noteType, 0));
            }
        }

        [HarmonyPatch(typeof(XDCustomiseMenu), nameof(XDCustomiseMenu.Update))]
        [HarmonyPostfix]
        private static void InsertCustomChromaSection(XDCustomiseMenu __instance)
        {
            if (!ChromaUI.Initialized)
                ChromaUI.Initialize(__instance);
        }

        [HarmonyPatch(typeof(XDColorPickerPopout), nameof(XDColorPickerPopout.Start))]
        [HarmonyPostfix]
        private static void InsertCopyColorButton(XDColorPickerPopout __instance)
        {
            ChromaUI.AddCopyButton(__instance);
        }

        [HarmonyPatch(typeof(Track), nameof(Track.Update))]
        [HarmonyPostfix]
        private static void Track_Update_Postfix()
        {
            if (!ChromaManager.EnableChroma) return;
            foreach (var note in ChromaManager.AffectedNotes)
            {
                var blender = ChromaManager.GetBlenderForNoteType(note);
                float hue = blender.hue;
                hue += ChromaManager.RainbowSpeed * 0.1f * Time.deltaTime;
                if (hue > 1f)
                    hue -= 1f;
                if (hue < 0f)
                    hue += 1f;
                blender.Hue = hue;
            }
        }
    }
}
