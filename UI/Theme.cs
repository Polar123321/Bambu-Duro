using System.Drawing.Drawing2D;

namespace ConsoleApp4.UI;

internal static class Theme
{
    internal static class Colors
    {
        // Backgrounds
        internal static readonly Color Bg0 = ColorTranslator.FromHtml("#0b0d12");
        internal static readonly Color Bg1 = ColorTranslator.FromHtml("#101421");
        internal static readonly Color Surface0 = ColorTranslator.FromHtml("#121621");
        internal static readonly Color Surface1 = ColorTranslator.FromHtml("#171c28");
        internal static readonly Color Surface2 = ColorTranslator.FromHtml("#1d2433");

        // Strokes / separators
        internal static readonly Color Stroke0 = ColorTranslator.FromHtml("#263045");
        internal static readonly Color Stroke1 = ColorTranslator.FromHtml("#2f3a52");

        // Text
        internal static readonly Color Text = ColorTranslator.FromHtml("#f0f3f8");
        internal static readonly Color Muted = ColorTranslator.FromHtml("#a7b0c0");
        internal static readonly Color Faint = ColorTranslator.FromHtml("#76819a");

        // Accents
        internal static readonly Color Accent = ColorTranslator.FromHtml("#2ef2c5");
        internal static readonly Color Accent2 = ColorTranslator.FromHtml("#4ea1ff");
        internal static readonly Color Danger = ColorTranslator.FromHtml("#ff5b6e");
        internal static readonly Color Warning = ColorTranslator.FromHtml("#ffcc66");
        internal static readonly Color Ok = ColorTranslator.FromHtml("#49f39a");
    }

    internal static class Spacing
    {
        internal const int S4 = 4;
        internal const int S8 = 8;
        internal const int S12 = 12;
        internal const int S16 = 16;
        internal const int S20 = 20;
        internal const int S24 = 24;
    }

    internal static class Radii
    {
        internal const int R10 = 10;
        internal const int R14 = 14;
        internal const int R18 = 18;
    }

    internal static class Fonts
    {
        internal static Font Ui(float size, FontStyle style = FontStyle.Regular)
        {
            // "Segoe UI Variable" exists on modern Windows; fall back safely.
            var family = TryFontFamily("Segoe UI Variable Text") ??
                         TryFontFamily("Segoe UI Variable") ??
                         TryFontFamily("Segoe UI") ??
                         FontFamily.GenericSansSerif;
            return new Font(family, size, style, GraphicsUnit.Point);
        }

        internal static Font Mono(float size, FontStyle style = FontStyle.Regular)
        {
            var family = TryFontFamily("Cascadia Code") ??
                         TryFontFamily("Consolas") ??
                         FontFamily.GenericMonospace;
            return new Font(family, size, style, GraphicsUnit.Point);
        }

        internal static Font Icon(float size)
        {
            var family = TryFontFamily("Segoe Fluent Icons") ??
                         TryFontFamily("Segoe MDL2 Assets") ??
                         TryFontFamily("Segoe UI Symbol") ??
                         FontFamily.GenericSansSerif;
            return new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        private static FontFamily? TryFontFamily(string name)
        {
            try
            {
                return new FontFamily(name);
            }
            catch
            {
                return null;
            }
        }
    }

    internal static LinearGradientBrush BgGradient(Rectangle bounds)
    {
        var brush = new LinearGradientBrush(bounds, Colors.Bg0, Colors.Bg1, 90f);
        brush.WrapMode = WrapMode.TileFlipXY;
        return brush;
    }
}

