using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SpeenChroma2
{
    public static class ChromaPatches
    {
        private static Dictionary<NoteColorType, ColorValueWrapper> _colorValueWrappers = new Dictionary<NoteColorType, ColorValueWrapper>();

        public static bool EnableChroma { get; set; }
        public static NoteColorType[] AffectedNotes { get; set; }
        public static float ChromaSpeed { get; set; }

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
            if (colorProfileIndex == 1)
            {
                _colorValueWrappers.Add(noteType, __instance);
            }
        }

        [HarmonyPatch(typeof(Track), nameof(Track.Update))]
        [HarmonyPostfix]
        private static void Track_Update_Postfix()
        {
            if (!EnableChroma) return;
            foreach (var note in AffectedNotes)
            {
                if (!_colorValueWrappers.TryGetValue(note, out var wrapper)) return;
                float hue = wrapper.Hue;
                hue += ChromaSpeed * 0.1f * Time.deltaTime;
                if (hue > 1f)
                    hue -= 1f;
                if (hue < 0f)
                    hue += 1f;
                wrapper.Hue = hue;
            }
        }
    }
}
