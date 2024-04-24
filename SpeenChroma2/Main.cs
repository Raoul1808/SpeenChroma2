using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SpeenChroma2
{
    [BepInPlugin(Guid, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        private const string Guid = "srxd.raoul1808.speenchroma2";
        private const string Name = "Speen Chroma 2";
        private const string Version = "1.0.0";

        private static ManualLogSource _logger;

        private static ConfigFile _config = new ConfigFile(Path.Combine(Paths.ConfigPath, "SpeenChroma2.cfg"), true);

        private static ConfigEntry<bool> _enableChromaEntry;
        private static ConfigEntry<ChromaNoteType> _affectedNotesEntry;
        
        private void Awake()
        {
            _logger = Logger;
            Logger.LogMessage("Hi from Speen Chroma 2!");

            _enableChromaEntry = _config.Bind("Chroma",
                "Enable",
                true,
                "If set to false, no color-changing effects will occur.");
            ChromaPatches.EnableChroma = _enableChromaEntry.Value;

            _affectedNotesEntry = _config.Bind("Chroma",
                "AffectedNotes",
                ChromaNoteType.All,
                "The list of notes affected by chroma effects. The `All` value overrides all other possible values.");
            ChromaPatches.AffectedNotes = ParseAffectedNotes(_affectedNotesEntry.Value);

            var rainbowSpeed = _config.Bind("Chroma.Rainbow",
                "Speed",
                1f,
                new ConfigDescription("Chroma rainbow speed.", new AcceptableValueRange<float>(0f, 100f)));
            ChromaPatches.ChromaSpeed = rainbowSpeed.Value;
            
            ChromaPatches.GentlyStealIMeanBorrowDefaultColorValues();
            
            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(ChromaPatches));
            Logger.LogMessage("Patched methods: " + harmony.GetPatchedMethods().Count());
        }

        private static NoteColorType[] ParseAffectedNotes(ChromaNoteType value)
        {
            var colorTypes = new List<NoteColorType>();
            if (value.HasFlag(ChromaNoteType.NoteA))
                colorTypes.Add(NoteColorType.NoteA);
            if (value.HasFlag(ChromaNoteType.NoteB))
                colorTypes.Add(NoteColorType.NoteB);
            if (value.HasFlag(ChromaNoteType.Beat))
                colorTypes.Add(NoteColorType.Beat);
            if (value.HasFlag(ChromaNoteType.SpinLeft))
                colorTypes.Add(NoteColorType.SpinLeft);
            if (value.HasFlag(ChromaNoteType.SpinRight))
                colorTypes.Add(NoteColorType.SpinRight);
            if (value.HasFlag(ChromaNoteType.Scratch))
                colorTypes.Add(NoteColorType.Scratch);
            if (value.HasFlag(ChromaNoteType.Ancillary))
                colorTypes.Add(NoteColorType.Ancillary);
            return colorTypes.ToArray();
        }

        internal static void SetChromaEnabled(bool enabled)
        {
            ChromaPatches.EnableChroma = enabled;
            _enableChromaEntry.Value = enabled;
            if (!enabled)
                ChromaPatches.ResetColorBlenders();
        }

        internal static void SetNoteTypeEnabled(ChromaNoteType noteType, bool enabled)
        {
            if (enabled)
            {
                _affectedNotesEntry.Value |= noteType;
            }
            else
            {
                _affectedNotesEntry.Value &= ~noteType;
            }

            ChromaPatches.AffectedNotes = ParseAffectedNotes(_affectedNotesEntry.Value);
            ChromaPatches.ResetColorBlenders();
        }

        internal static void Log(object msg) => _logger.LogMessage(msg);
    }
}
