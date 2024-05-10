using System;
using System.Globalization;
using UnityEngine;

namespace SpeenChroma2
{
    public struct HslColor
    {
        public float H;
        public float S;
        public float L;

        public HslColor(float h, float s, float l)
        {
            H = h;
            S = s;
            L = l;
        }

        public static HslColor Lerp(HslColor col1, HslColor col2, float t)
        {
            if (t <= 0f)
                return col1;
            if (t >= 1f)
                return col2;
            return new HslColor
            {
                H = col1.H + t * (col2.H - col1.H),
                S = col1.S + t * (col2.S - col1.S),
                L = col1.L + t * (col2.L - col1.L),
            };
        }

        public void WrapAndClamp()
        {
            while (H >= 1f)
            {
                H -= 1f;
            }

            while (H < 0f)
            {
                H += 1f;
            }

            S = Mathf.Clamp01(S);
            L = Mathf.Clamp01(L);
        }

        public string ToHexRgb()
        {
            WrapAndClamp();
            double hue = H * 360.0;
            double chroma = (1 - Math.Abs(2 * L - 1)) * S;
            double x = chroma * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            double m = L - chroma / 2.0;
            double r1 = 0, g1 = 0, b1 = 0;
            if (0 <= hue && hue < 60)
            {
                r1 = chroma;
                g1 = x;
            }
            else if (60 <= hue && hue < 120)
            {
                r1 = x;
                g1 = chroma;   
            }
            else if (120 <= hue && hue < 180)
            {
                g1 = chroma;
                b1 = x;
            }
            else if (180 <= hue && hue < 240)
            {
                g1 = x;
                b1 = chroma;
            }
            else if (240 <= hue && hue < 300)
            {
                b1 = chroma;
                r1 = x;
            }
            else if (300 <= hue && hue < 360)
            {
                b1 = x;
                r1 = chroma;
            }

            int r = (int)Math.Round((r1 + m) * 255);
            int g = (int)Math.Round((g1 + m) * 255);
            int b = (int)Math.Round((b1 + m) * 255);
            return r.ToString("x2") + g.ToString("x2") + b.ToString("x2");
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

        public override string ToString()
        {
            return $"{{H: {H}, S: {S}, L: {L}}}";
        }
    }
}
