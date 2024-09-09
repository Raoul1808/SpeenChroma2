using UnityEngine;

namespace SpeenChroma2
{
    public class ChromaBlender
    {
        private GameplayColorBlender _blender;
        private bool _dirty;

        public ChromaBlender(GameplayColorBlender blender)
        {
            _blender = blender;
            _dirty = false;
        }

        public float Hue
        {
            get => _blender.hue;
            set
            {
                _blender.hue = value;
                _dirty = true;
            }
        }

        public float Saturation
        {
            get => _blender.saturation;
            set
            {
                _blender.saturation = value;
                _dirty = true;
            }
        }

        public float Lightness
        {
            get => _blender.lightness;
            set
            {
                _blender.lightness = value;
                _dirty = true;
            }
        }

        public void PropagateColors()
        {
            if (!_dirty) return;
            _dirty = false;
            _blender.GenerateBlend();
        }

        public bool MatchesColor(HslColor color)
        {
            return Mathf.Approximately(color.H, Hue) && Mathf.Approximately(color.S, Saturation) && Mathf.Approximately(color.L, Lightness);
        }
    }
}
