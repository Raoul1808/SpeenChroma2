using System;
using System.Linq;
using UnityEngine;
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
        
        public static void Initialize(XDCustomiseMenu instance)
        {
            if (Initialized) return;
            Initialized = true;
            CreateChromaSection(instance);
            
            CreateMultiChoiceButton(
                _chromaSection.transform,
                "EnableChroma",
                "Enable Chroma",
                ChromaPatches.EnableChroma ? 1 : 0,
                v => Main.SetChromaEnabled(v == 1),
                () => new IntRange(0, 2),
                v => v == 0 ? "UI_No" : "UI_Yes"
            );

            var noteTypes = (ChromaNoteType[]) Enum.GetValues(typeof(ChromaNoteType));
            foreach (var noteType in noteTypes)
            {
                if (noteType == ChromaNoteType.All)
                    continue;

                var noteIsAffected = ChromaPatches.AffectedNotes.Contains(noteType.ToNoteColorType());

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
