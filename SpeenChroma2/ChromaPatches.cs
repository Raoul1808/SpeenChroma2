using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using XDMenuPlay;
using XDMenuPlay.Customise;
using Object = UnityEngine.Object;

namespace SpeenChroma2
{
    public static class ChromaPatches
    {
        private static Dictionary<NoteColorType, (float Hue, float Saturation, float Lightness)> _defaultColors;
        private static Dictionary<NoteColorType, GameplayColorBlender> _colorValueWrappers = new Dictionary<NoteColorType, GameplayColorBlender>();

        public static bool EnableChroma { get; set; }
        public static NoteColorType[] AffectedNotes { get; set; }
        public static float ChromaSpeed { get; set; }

        private static GameObject _chromaToggle;

        public static void GentlyStealIMeanBorrowDefaultColorValues()
        {
            _defaultColors = (Dictionary<NoteColorType, (float, float, float)>) AccessTools.Field(typeof(ColorValueWrapper), "colorDefaults").GetValue(null);
            Main.Log("Gently stole-I mean borrowed default colors: " + _defaultColors);
        }

        public static void ResetColorBlenders()
        {
            foreach (var pair in _colorValueWrappers)
            {
                var b = pair.Value;
                var k = pair.Key;
                b.Hue = _defaultColors[k].Hue;
                b.Saturation = _defaultColors[k].Saturation;
                b.Lightness = _defaultColors[k].Lightness;
            }
        }

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
                _colorValueWrappers.Add(noteType, GameSystemSingleton<ColorSystem, ColorSystemSettings>.Instance.ColorBlenderForNoteColorType(noteType, 0));
            }
        }

        [HarmonyPatch(typeof(XDCustomiseMenu), nameof(XDCustomiseMenu.Update))]
        [HarmonyPostfix]
        private static void InsertCustomChromaSection(XDCustomiseMenu __instance)
        {
            if (_chromaToggle != null) return;
            var container = __instance.transform.Find("MenuContainer/CustomiseTabsContainer");
            var colorSettingsSection = container.Find("CustomiseSkinsTab/Scroll View/Viewport/Content CustomiseSkinsTab Prefab(Clone)/Note Colors Section Variant");
            var noteColorProfileButton = colorSettingsSection.Find("NoteColorProfile");
            Main.Log(colorSettingsSection);
            _chromaToggle = Object.Instantiate(noteColorProfileButton.gameObject, colorSettingsSection);
            _chromaToggle.name = "ChromaToggle";
            _chromaToggle.transform.name = "ChromaToggle";
            _chromaToggle.transform.SetSiblingIndex(1);
            var multiChoice = _chromaToggle.GetComponent<XDNavigableOptionMultiChoice>();
            Object.Destroy(_chromaToggle.GetComponent<XDNavigableOptionMultiChoice_IntValue>());
            multiChoice.state.callbacks = new XDNavigableOptionMultiChoice.Callbacks();
            multiChoice.SetCallbacksAndValue(EnableChroma ? 1 : 0, v => Main.SetChromaEnabled(v == 1), () => new IntRange(0, 2), v => v == 0 ? "UI_No" : "UI_Yes");
            var optionLabel = _chromaToggle.transform.Find("OptionLabel").GetComponent<TranslatedTextMeshPro>();
            optionLabel.text.text = "Enable Chroma";
        }

        [HarmonyPatch(typeof(Track), nameof(Track.Update))]
        [HarmonyPostfix]
        private static void Track_Update_Postfix()
        {
            if (!EnableChroma) return;
            foreach (var note in AffectedNotes)
            {
                if (!_colorValueWrappers.TryGetValue(note, out var wrapper)) return;
                float hue = wrapper.hue;
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
