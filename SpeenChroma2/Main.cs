using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SpeenChroma2
{
    [BepInPlugin(Guid, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        private const string Guid = "srxd.raoul1808.speenchroma2";
        private const string Name = "Speen Chroma 2";
        private const string Version = "0.1.0";

        private static ManualLogSource _logger;
        
        private void Awake()
        {
            _logger = Logger;
            Logger.LogMessage("Hi from Speen Chroma 2!");
            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(QuickPatches));
            Logger.LogMessage("Patched methods: " + harmony.GetPatchedMethods().Count());
        }

        internal static void Log(object msg) => _logger.LogMessage(msg);

        internal class QuickPatches
        {
            private static List<ColorValueWrapper> _colors = new List<ColorValueWrapper>();
            
            [HarmonyPatch(
                 typeof(ColorValueWrapper),
                 MethodType.Constructor,
                 new Type []
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
                Log(colorProfileIndex + " " + __instance.NoteType + " " + __instance.Hue);
                if (colorProfileIndex == 1)
                {
                    _colors.Add(__instance);
                }
            }

            [HarmonyPatch(typeof(Track), nameof(Track.Update))]
            [HarmonyPostfix]
            private static void Track_Update_Postfix()
            {
                foreach (var wrapper in _colors)
                {
                    float hue = wrapper.Hue;
                    hue += 1f * Time.deltaTime;
                    if (hue > 1f)
                        hue -= 1f;
                    if (hue < 0f)
                        hue += 1f;
                    wrapper.Hue = hue;
                }
            }
        }
    }
}
