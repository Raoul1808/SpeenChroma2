using System;
using System.Linq;
using SpinCore.UI;
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
        private static GameObject _buttonBase;

        private static CustomGroup _rainbowSection;
        
        public static void Initialize()
        {
            var page = UIHelper.CreateSettingsPage("Speen Chroma");
            page.OnPageLoad += pageParent =>
            {
                {
                    var group = UIHelper.CreateGroup(pageParent, "General Settings");
                    UIHelper.CreateSectionHeader(
                        group.Transform,
                        "General Header",
                        "SpeenChroma2_ModSettings_GeneralHeader",
                        false
                    );
                    UIHelper.CreateLargeToggle(
                        group.Transform,
                        "EnableChroma",
                        "SpeenChroma2_ModSettings_EnableChroma",
                        ChromaManager.EnableChroma,
                        v =>
                        {
                            Main.SetChromaEnabled(v);
                            _rainbowSection.Active = ChromaManager.EnableRainbow && ChromaManager.EnableChroma;
                        });
                    UIHelper.CreateLargeToggle(
                        group.Transform,
                        "EnableRainbow",
                        "SpeenChroma2_ModSettings_EnableRainbow",
                        ChromaManager.EnableRainbow,
                        v =>
                        {
                            Main.SetRainbowEnabled(v);
                            _rainbowSection.Active = ChromaManager.EnableRainbow && ChromaManager.EnableChroma;
                        });
                    UIHelper.CreateLargeToggle(
                        group.Transform,
                        "EnableTriggers",
                        "SpeenChroma2_ModSettings_EnableTriggers",
                        ChromaManager.EnableTriggers,
                        Main.SetChromaTriggersEnabled
                    );
                }
                {
                    var group = UIHelper.CreateGroup(pageParent, "Rainbow Settings");
                    UIHelper.CreateSectionHeader(
                        group.Transform,
                        "Rainbow Header",
                        "SpeenChroma2_ModSettings_RainbowHeader",
                        true
                    );
                    UIHelper.CreateLargeMultiChoiceButton(
                        group.Transform,
                        "RainbowSpeed",
                        "SpeenChroma2_ModSettings_RainbowSpeed",
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

                        var noteIsAffected = ChromaManager.AffectedNotesRainbow.Contains(noteType.ToNoteColorType());

                        UIHelper.CreateLargeToggle(
                            group.Transform,
                            "EnableNote" + noteType,
                            "SpeenChroma2_ModSettings_EnableRainbowFor" + noteType.ToString(),
                            noteIsAffected,
                            v => Main.SetNoteTypeRainbowEnabled(noteType, v)
                        );
                    }
                    _rainbowSection = group;
                }
                _rainbowSection.Active = ChromaManager.EnableRainbow && ChromaManager.EnableChroma;
            };
            UIHelper.RegisterMenuInModSettingsRoot("SpeenChroma2_ModSettings_Name", page);
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

                    instance.currentWrapper.Hue = col.H;
                    instance.currentWrapper.Saturation = col.S;
                    instance.currentWrapper.Lightness = col.L;
                    instance.hue.normalizedValue = col.H;
                    instance.saturation.TargetIndex = (int)Math.Round(col.S * 100f);
                    instance.lightness.TargetIndex = (int)Math.Round(col.L * 100f);
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
                _buttonBase = XDCustomiseMenu.Instance.transform.Find("VRContainerOffset/MenuContainer/PopoutControlContainer/ColorPickerPopout(Clone)/ResetButton").gameObject;
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
    }
}
