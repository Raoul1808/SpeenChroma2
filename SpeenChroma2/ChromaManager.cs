using System.Collections.Generic;
using System.Data;

namespace SpeenChroma2
{
    public static class ChromaManager
    {
        public static bool EnableChroma { get; set; }
        public static bool EnableTriggers { get; set; }
        public static NoteColorType[] AffectedNotesRainbow { get; set; }
        public static bool EnableRainbow { get; set; }
        public static float RainbowSpeed { get; set; }

        internal static bool AreTriggersLoaded { get; set; }

        private static Dictionary<NoteColorType, (float Hue, float Saturation, float Lightness)> _defaultColors;
        private static Dictionary<NoteColorType, ChromaBlender> _colorBlenders = new Dictionary<NoteColorType, ChromaBlender>();

        public static void GetDefaultColors()
        {
            _defaultColors = ColorValueWrapper.colorDefaults;
        }

        public static HslColor GetDefaultColorForNoteType(NoteColorType colorType)
        {
            var col = _defaultColors[colorType];
            return new HslColor(col.Hue, col.Saturation, col.Lightness);
        }
        
        public static void ResetColorBlenders()
        {
            foreach (var pair in _colorBlenders)
            {
                var b = pair.Value;
                var k = pair.Key;
                b.Hue = _defaultColors[k].Hue;
                b.Saturation = _defaultColors[k].Saturation;
                b.Lightness = _defaultColors[k].Lightness;
            }
        }

        public static void AddColorBlender(NoteColorType color, ChromaBlender blender)
        {
            _colorBlenders.Add(color, blender);
        }

        public static ChromaBlender GetBlenderForNoteType(NoteColorType color)
        {
            return _colorBlenders[color];
        }

        public static void SetColorForNoteType(NoteColorType colorType, HslColor color)
        {
            if (!EnableChroma)
                return;
            color.WrapAndClamp();
            _colorBlenders[colorType].Hue = color.H;
            _colorBlenders[colorType].Saturation = color.S;
            _colorBlenders[colorType].Lightness = color.L;
        }

        public static void PropagateAllColors()
        {
            foreach (var blender in _colorBlenders.Values)
            {
                blender.PropagateColors();
            }
        }
    }
}