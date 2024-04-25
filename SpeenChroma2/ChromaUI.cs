using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using XDMenuPlay;
using XDMenuPlay.Customise;
using Object = UnityEngine.Object;

namespace SpeenChroma2
{
    public static class ChromaUI
    {
        public static bool Initialized { get; private set; }

        private static GameObject _chromaSection;

        private static GameObject _multiChoiceBase;
        private static GameObject _buttonBase;
        
        public static void Initialize(XDCustomiseMenu instance)
        {
            if (Initialized) return;
            Initialized = true;
            CreateChromaSection(instance);
            
            CreateMultiChoiceButton(
                _chromaSection.transform,
                "EnableChroma",
                "Enable Chroma",
                ChromaManager.EnableChroma ? 1 : 0,
                v => Main.SetChromaEnabled(v == 1),
                () => new IntRange(0, 2),
                v => v == 0 ? "UI_No" : "UI_Yes"
            );

            CreateMultiChoiceButton(
                _chromaSection.transform,
                "RainbowSpeed",
                "Rainbow Speed",
                (int)(ChromaManager.RainbowSpeed * 10),
                Main.SetRainbowSpeed,
                () => new IntRange(0, 101),
                v => v.ToString()
            );

            var noteTypes = (ChromaNoteType[]) Enum.GetValues(typeof(ChromaNoteType));
            foreach (var noteType in noteTypes)
            {
                if (noteType == ChromaNoteType.All)
                    continue;

                var noteIsAffected = ChromaManager.AffectedNotes.Contains(noteType.ToNoteColorType());

                CreateMultiChoiceButton(
                    _chromaSection.transform,
                    "EnableNote" + noteType,
                    "Enable chroma on " + noteType.GetName(),
                    noteIsAffected ? 1 : 0,
                    v => Main.SetNoteTypeEnabled(noteType, v == 1),
                    () => new IntRange(0, 2),
                    v => v == 0 ? "UI_No" : "UI_Yes"
                );
            }
        }

        public static void AddCopyButtons(XDColorPickerPopout instance)
        {
            var popout = instance.gameObject.transform;
            if (popout.Find("CopyHexButton") != null)
                return;
            var copyHexButton = CreateButtonWithClipboardIcon(
                popout,
                "CopyHexButton",
                "Copy Hex Code",
                () =>
                {
                    var col = new HslColor(instance.currentWrapper.Hue, instance.currentWrapper.Saturation, instance.currentWrapper.Lightness);
                    string hex = "#" + col.ToHexRgb();
                    GUIUtility.systemCopyBuffer = hex;
                    NotificationSystemGUI.AddMessage($"Copied hex color {hex} to clipboard");
                }
            );
            var importHexButton = CreateButtonWithClipboardIcon(
                popout,
                "ImportHexButton",
                "Import Clipboard",
                () =>
                {
                    HslColor col;
                    try
                    {
                        col = HslColor.FromHexRgb(GUIUtility.systemCopyBuffer);
                    }
                    catch
                    {
                        NotificationSystemGUI.AddMessage("Invalid hex code");
                        return;
                    }

                    instance.currentWrapper.Hue = col.Hue;
                    instance.currentWrapper.Saturation = col.Saturation;
                    instance.currentWrapper.Lightness = col.Lightness;
                    instance.hue.normalizedValue = col.Hue;
                    instance.saturation.TargetIndex = (int)Math.Round(col.Saturation * 100f);
                    instance.lightness.TargetIndex = (int)Math.Round(col.Lightness * 100f);
                }
            );
        }

        private static GameObject CreateButtonWithClipboardIcon(
            Transform parent, 
            string name,
            string label, 
            UnityAction buttonAction)
        {
            if (_buttonBase == null)
            {
                _buttonBase = XDCustomiseMenu.Instance.transform.Find("MenuContainer/PopoutControlContainer/ColorPickerPopout(Clone)/ResetButton").gameObject;
            }
            var button = Object.Instantiate(_buttonBase, parent);
            button.name = name;
            button.transform.name = name;
            var text = button.transform.Find("OptionLabel").GetComponent<TranslatedTextMeshPro>();
            text.text.text = label;
            Object.Destroy(button.transform.Find("ValueContainer/Visuals/Icon").gameObject);
            var icon = Object.Instantiate(
                BuildSettingsAsset.Instance.popoutButtonPrefab.transform.Find("ValueContainer/Visuals/Icon")
                    .gameObject,
                button.transform.Find("ValueContainer/Visuals"));
            icon.name = "Icon";
            icon.transform.name = "Icon";
            var xdButton = button.GetComponent<XDNavigableButton>();
            xdButton.onClick = new Button.ButtonClickedEvent();
            xdButton.onClick.AddListener(buttonAction);
            return button;
        }

        private static void CreateChromaSection(XDCustomiseMenu instance)
        {
            var container = instance.transform.Find("MenuContainer/CustomiseTabsContainer/CustomiseSkinsTab/Scroll View/Viewport/Content CustomiseSkinsTab Prefab(Clone)");
            _chromaSection = Object.Instantiate(container.Find("Menu Skins Section").gameObject, container.transform);
            _chromaSection.name = "Chroma Section";
            _chromaSection.transform.name = "Chroma Section";
            _chromaSection.transform.SetSiblingIndex(3);
            Object.Destroy(_chromaSection.transform.GetChild(2).gameObject);
            Object.Destroy(_chromaSection.transform.GetChild(1).gameObject);

            var sectionLabel = _chromaSection.transform.GetChild(0);
            var text = sectionLabel.Find("LabelContainer/Label").GetComponent<TranslatedTextMeshPro>();
            text.text.text = "Chroma";
        }

        private static GameObject CreateMultiChoiceButton(
            Transform parent,
            string name,
            string label,
            int defaultValue,
            XDNavigableOptionMultiChoice.OnValueChanged valueChanged,
            XDNavigableOptionMultiChoice.OnValueRangeRequested valueRangeRequested, 
            XDNavigableOptionMultiChoice.OnValueTextRequested valueTextRequested)
        {
            if (_multiChoiceBase == null)
            {
                var container = XDCustomiseMenu.Instance.transform.Find("MenuContainer/CustomiseTabsContainer");
                var colorSettingsSection = container.Find("CustomiseSkinsTab/Scroll View/Viewport/Content CustomiseSkinsTab Prefab(Clone)/Note Colors Section Variant");
                _multiChoiceBase = colorSettingsSection.Find("NoteColorProfile").gameObject;
            }

            var button = Object.Instantiate(_multiChoiceBase, parent);
            button.name = name;
            button.transform.name = name;
            Object.Destroy(button.GetComponent<XDNavigableOptionMultiChoice_IntValue>());
            var multiChoice = button.GetComponent<XDNavigableOptionMultiChoice>();
            multiChoice.state.callbacks = new XDNavigableOptionMultiChoice.Callbacks();
            multiChoice.SetCallbacksAndValue(defaultValue, valueChanged, valueRangeRequested, valueTextRequested);
            var optionLabel = button.transform.Find("OptionLabel").GetComponent<TranslatedTextMeshPro>();
            optionLabel.text.text = label;
            return button;
        }
    }
}
