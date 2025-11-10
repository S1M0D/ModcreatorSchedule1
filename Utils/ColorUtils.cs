using System;
using System.Globalization;
using System.Windows.Media;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Helper methods for working with HEX color strings shared between WPF and generated Unity code.
    /// </summary>
    public static class ColorUtils
    {
        public static string NormalizeHex(string? value, string fallback = "#FFFFFFFF")
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var hex = value.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex[1..];

            if (hex.Length == 3)
            {
                // RGB -> duplicate characters and prefix opaque alpha
                hex = string.Format(
                    CultureInfo.InvariantCulture,
                    "FF{0}{0}{1}{1}{2}{2}",
                    hex[0], hex[1], hex[2]);
            }
            else if (hex.Length == 4)
            {
                // ARGB nibble notation
                hex = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{0}{1}{1}{2}{2}{3}{3}",
                    hex[0], hex[1], hex[2], hex[3]);
            }
            else if (hex.Length == 6)
            {
                // Assume opaque
                hex = $"FF{hex}";
            }
            else if (hex.Length != 8)
            {
                return fallback;
            }

            return $"#{hex.ToUpperInvariant()}";
        }

        public static (byte A, byte R, byte G, byte B) ParseHex(string? value)
        {
            var normalized = NormalizeHex(value);
            var span = normalized.AsSpan(1); // skip '#'
            var a = byte.Parse(span.Slice(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var r = byte.Parse(span.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var g = byte.Parse(span.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b = byte.Parse(span.Slice(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return (a, r, g, b);
        }

        public static SolidColorBrush ToBrush(string? value)
        {
            var (a, r, g, b) = ParseHex(value);
            var color = System.Windows.Media.Color.FromArgb(a, r, g, b);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static string ToUnityColorExpression(string? value)
        {
            var (a, r, g, b) = ParseHex(value);
            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;
            float af = a / 255f;

            if (a == 255)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "new Color({0:0.###}f, {1:0.###}f, {2:0.###}f)",
                    rf, gf, bf);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "new Color({0:0.###}f, {1:0.###}f, {2:0.###}f, {3:0.###}f)",
                rf, gf, bf, af);
        }

        public static string ToUnityColor32Expression(string? value)
        {
            var (a, r, g, b) = ParseHex(value);
            return $"new Color32({r}, {g}, {b}, {a})";
        }

        public static string ToTupleExpression(float left, float right)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.###}f, {1:0.###}f)",
                left, right);
        }

        /// <summary>
        /// Converts a Schedule One clothing color integer value to its readable name.
        /// </summary>
        /// <param name="colorValue">The integer color value (0-26)</param>
        /// <returns>The color name (e.g., "Tan", "Navy", "Black") or "White" if invalid</returns>
        public static string ClothingColorToString(int colorValue)
        {
            return colorValue switch
            {
                0 => "White",
                1 => "LightGrey",
                2 => "DarkGrey",
                3 => "Charcoal",
                4 => "Black",
                5 => "LightRed",
                6 => "Red",
                7 => "Crimson",
                8 => "Orange",
                9 => "Tan",
                10 => "Brown",
                11 => "Coral",
                12 => "Beige",
                13 => "Yellow",
                14 => "Lime",
                15 => "LightGreen",
                16 => "DarkGreen",
                17 => "Cyan",
                18 => "SkyBlue",
                19 => "Blue",
                20 => "DeepBlue",
                21 => "Navy",
                22 => "DeepPurple",
                23 => "Purple",
                24 => "Magenta",
                25 => "BrightPink",
                26 => "HotPink",
                _ => "White"
            };
        }

        /// <summary>
        /// Converts a Schedule One clothing color name to its integer value.
        /// </summary>
        /// <param name="colorName">The color name (e.g., "Tan", "Navy", "Black")</param>
        /// <returns>The integer color value (0-26) or 0 (White) if invalid</returns>
        public static int ClothingColorToInt(string? colorName)
        {
            if (string.IsNullOrWhiteSpace(colorName))
                return 0;

            return colorName.Trim() switch
            {
                "White" => 0,
                "LightGrey" => 1,
                "DarkGrey" => 2,
                "Charcoal" => 3,
                "Black" => 4,
                "LightRed" => 5,
                "Red" => 6,
                "Crimson" => 7,
                "Orange" => 8,
                "Tan" => 9,
                "Brown" => 10,
                "Coral" => 11,
                "Beige" => 12,
                "Yellow" => 13,
                "Lime" => 14,
                "LightGreen" => 15,
                "DarkGreen" => 16,
                "Cyan" => 17,
                "SkyBlue" => 18,
                "Blue" => 19,
                "DeepBlue" => 20,
                "Navy" => 21,
                "DeepPurple" => 22,
                "Purple" => 23,
                "Magenta" => 24,
                "BrightPink" => 25,
                "HotPink" => 26,
                _ => 0
            };
        }
    }
}
