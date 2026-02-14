using System.Drawing.Drawing2D;

namespace ConsoleApp4.UI;

internal static class GraphicsUtil
{
    internal static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var r = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        var path = new GraphicsPath();
        if (r == 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var d = r * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    internal static void SetHighQuality(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
    }
}

