using System.Collections.Generic;
using System.Linq;

namespace SpeenChroma2
{
    public static class ChromaManager
    {
        public static bool EnableChroma { get; set; }
        public static NoteColorType[] AffectedNotes { get; set; }
        public static float RainbowSpeed { get; set; }
        
        private static Dictionary<NoteColorType, (float Hue, float Saturation, float Lightness)> _defaultColors;
        private static Dictionary<NoteColorType, GameplayColorBlender> _colorBlenders = new Dictionary<NoteColorType, GameplayColorBlender>();

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

        public static void AddColorBlender(NoteColorType color, GameplayColorBlender blender)
        {
            _colorBlenders.Add(color, blender);
        }

        public static GameplayColorBlender GetBlenderForNoteType(NoteColorType color)
        {
            return _colorBlenders[color];
        }

        public static void SetColorForNoteType(NoteColorType colorType, HslColor color)
        {
            if (!EnableChroma || !AffectedNotes.Contains(colorType))
                return;
            color.WrapAndClamp();
            _colorBlenders[colorType].Hue = color.Hue;
            _colorBlenders[colorType].Saturation = color.Saturation;
            _colorBlenders[colorType].Lightness = color.Lightness;
        }
    }
}
