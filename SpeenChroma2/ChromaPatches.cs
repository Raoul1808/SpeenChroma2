using System;
using HarmonyLib;
using UnityEngine;
using XDMenuPlay;
using XDMenuPlay.Customise;

namespace SpeenChroma2
{
    public static class ChromaPatches
    {
        private static bool _currentlyIngame;
        private static bool _restarting;
        
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

        [HarmonyPatch(typeof(XDColorPickerPopout), nameof(XDColorPickerPopout.Start))]
        [HarmonyPostfix]
        private static void InsertCopyColorButton(XDColorPickerPopout __instance)
        {
            ChromaUI.AddCopyButtons(__instance);
        }

        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
        [HarmonyPostfix]
        private static void UpdateStateWhenGaming()
        {
            _currentlyIngame = true;
            if (_restarting)
                _restarting = false;
        }

        [HarmonyPatch(typeof(Track), nameof(Track.RestartTrack))]
        [HarmonyPostfix]
        private static void PreventRestartBug()
        {
            _restarting = true;
        }
        
        [HarmonyPatch(typeof(Track), nameof(Track.StopTrack))]
        [HarmonyPrefix]
        private static void LeaveGame()
        {
            _currentlyIngame = false;
        }

        [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.BackButtonPressed))]
        [HarmonyPatch(typeof(Track), nameof(Track.StopTrack))]
        [HarmonyPostfix]
        private static void ClearEffects()
        {
            if (_currentlyIngame || _restarting) return;
            ChromaTriggers.ClearAll();
            ChromaManager.ResetColorBlenders();
        }

        [HarmonyPatch(typeof(Track), nameof(Track.Update))]
        [HarmonyPostfix]
        private static void Track_Update_Postfix()
        {
            if (!ChromaManager.EnableChroma || !ChromaManager.EnableRainbow) return;
            foreach (var note in ChromaManager.AffectedNotesRainbow)
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

        [HarmonyPatch(typeof(SplineTrackData.DataToGenerate), MethodType.Constructor, typeof(PlayableTrackData))]
        [HarmonyPostfix]
        private static void ConstructorPatch(PlayableTrackData trackData)
        {
            ChromaTriggers.LoadTriggers(trackData);
        }
    }
}
