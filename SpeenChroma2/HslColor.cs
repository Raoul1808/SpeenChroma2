namespace SpeenChroma2
{
    public struct HslColor
    {
        public float Hue;
        public float Saturation;
        public float Lightness;

        public static HslColor Lerp(HslColor col1, HslColor col2, float t)
        {
            if (t <= 0f)
                return col1;
            if (t >= 1f)
                return col2;
            return new HslColor
            {
                Hue = col1.Hue + t * (col2.Hue - col1.Hue),
                Saturation = col1.Saturation + t * (col2.Saturation - col1.Saturation),
                Lightness = col1.Lightness + t * (col2.Lightness - col1.Lightness),
            };
        }
    }
}
