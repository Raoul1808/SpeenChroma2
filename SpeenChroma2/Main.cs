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
        private const string Version = "2.0.0";

        private static ManualLogSource _logger;

        private static ConfigFile _config = new ConfigFile(Path.Combine(Paths.ConfigPath, "SpeenChroma2.cfg"), true);

        private static ConfigEntry<bool> _enableChromaEntry;
        private static ConfigEntry<bool> _enableChromaTriggersEntry;
        private static ConfigEntry<bool> _enableRainbowEntry;
        private static ConfigEntry<ChromaNoteType> _affectedNotesRainbowEntry;
        private static ConfigEntry<float> _rainbowSpeed;
        
        private void Awake()
        {
            _config.SaveOnConfigSet = true;
            _logger = Logger;
            Logger.LogMessage("Hi from Speen Chroma 2!");

            _enableChromaEntry = _config.Bind("Chroma",
                "Enable",
                true,
                "If set to false, no color-changing effects will occur.");
            ChromaManager.EnableChroma = _enableChromaEntry.Value;

            _enableChromaTriggersEntry = _config.Bind("Chroma",
                "EnableTriggers",
                true,
                "If set to false, no chart-specific color-changing effects will occur.");
            ChromaManager.EnableTriggers = _enableChromaTriggersEntry.Value;
            
            _enableRainbowEntry = _config.Bind("Chroma.Rainbow",
                "Enable",
                true,
                "If set to false, no rainbow effects will occur.");
            ChromaManager.EnableRainbow = _enableRainbowEntry.Value;

            _affectedNotesRainbowEntry = _config.Bind("Chroma.Rainbow",
                "AffectedNotes",
                ChromaNoteType.All,
                "The list of notes affected by chroma effects. The `All` value overrides all other possible values.");
            ChromaManager.AffectedNotesRainbow = ParseAffectedNotes(_affectedNotesRainbowEntry.Value);
            
            _rainbowSpeed = _config.Bind("Chroma.Rainbow",
                "Speed",
                1f,
                new ConfigDescription("Chroma rainbow speed.", new AcceptableValueRange<float>(0f, 10f)));
            ChromaManager.RainbowSpeed = _rainbowSpeed.Value;
            
            ChromaManager.GetDefaultColors();
            ChromaTriggers.Setup();
            
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
            ChromaManager.EnableChroma = enabled;
            _enableChromaEntry.Value = enabled;
            if (!enabled)
                ChromaManager.ResetColorBlenders();
        }

        internal static void SetChromaTriggersEnabled(bool enabled)
        {
            ChromaManager.EnableTriggers = enabled;
            _enableChromaTriggersEntry.Value = enabled;
            if (!enabled)
                ChromaManager.ResetColorBlenders();
        }

        internal static void SetRainbowEnabled(bool enabled)
        {
            ChromaManager.EnableRainbow = enabled;
            _enableRainbowEntry.Value = enabled;
            if (!enabled)
                ChromaManager.ResetColorBlenders();
        }

        internal static void SetNoteTypeRainbowEnabled(ChromaNoteType noteType, bool enabled)
        {
            if (enabled)
            {
                _affectedNotesRainbowEntry.Value |= noteType;
            }
            else
            {
                _affectedNotesRainbowEntry.Value &= ~noteType;
            }

            ChromaManager.AffectedNotesRainbow = ParseAffectedNotes(_affectedNotesRainbowEntry.Value);
            ChromaManager.ResetColorBlenders();
        }

        internal static void SetRainbowSpeed(int speed)
        {
            _rainbowSpeed.Value = speed / 10f;
            ChromaManager.RainbowSpeed = speed / 10f;
        }

        internal static void Log(object msg) => _logger.LogMessage(msg);
    }
}
