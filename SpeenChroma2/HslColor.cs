using System;
using System.Globalization;
using UnityEngine;

namespace SpeenChroma2
{
    public struct HslColor
    {
        public float Hue;
        public float Saturation;
        public float Lightness;

        public HslColor(float h, float s, float l)
        {
            Hue = h;
            Saturation = s;
            Lightness = l;
        }

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

        public void WrapAndClamp()
        {
            while (Hue >= 1f)
            {
                Hue -= 1f;
            }

            while (Hue < 0f)
            {
                Hue += 1f;
            }

            Saturation = Mathf.Clamp01(Saturation);
            Lightness = Mathf.Clamp01(Lightness);
        }

        public static HslColor FromHexRgb(string hex)
        {
            if (hex[0] == '#')
            {
                hex = hex.Replace("#", "");
            }

            if (hex.Length != 6)
                throw new ArgumentException($"Hex color should have 6 characters, argument has {hex.Length}");

            int red = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            int green = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            int blue = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            return FromRgb(red, green, blue);
        }

        // Source: http://www.easyrgb.com/en/math.php
        public static HslColor FromRgb(int red, int green, int blue)
        {
            double r = red / 255f;
            double g = green / 255f;
            double b = blue / 255f;

            var min = Math.Min(r, Math.Min(g, b));
            var max = Math.Max(r, Math.Max(g, b));
            var diff = max - min;
            double l = (max + min) / 2;

            if (diff == 0) return new HslColor(0f, 0f, (float)l);

            double s;
            if (l < 0.5) s = diff / (max + min);
            else s = diff / (2 - max - min);

            double r2 = (((max - r) / 6) + (max / 2)) / diff;
            double g2 = (((max - g) / 6) + (max / 2)) / diff;
            double b2 = (((max - b) / 6) + (max / 2)) / diff;

            double h = 0;
            if (r == max) h = b2 - g2;
            else if (g == max) h = (1 / 3f) + r2 - b2;
            else if (b == max) h = (2 / 3f) + g2 - r2;

            if (h < 0) h += 1;
            if (h > 1) h -= 1;
            return new HslColor((float)h, (float)s, (float)l);
        }
    }
}
