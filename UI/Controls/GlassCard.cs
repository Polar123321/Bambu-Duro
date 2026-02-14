using System.Drawing.Drawing2D;

namespace ConsoleApp4.UI.Controls;

internal sealed class GlassCard : Panel
{
    public int CornerRadius { get; set; } = Theme.Radii.R18;
    // Opaque fills avoid "ghosting" where underlying controls show through the card.
    public Color FillTop { get; set; } = Theme.Colors.Surface0;
    public Color FillBottom { get; set; } = Theme.Colors.Surface1;
    public Color StrokeTop { get; set; } = Color.FromArgb(200, Theme.Colors.Stroke1);
    public Color StrokeBottom { get; set; } = Color.FromArgb(90, Theme.Colors.Stroke0);
    public int StrokeWidth { get; set; } = 1;

    public GlassCard()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.Transparent;
        Padding = new Padding(Theme.Spacing.S16);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Avoid default background painting to prevent flicker; we paint everything in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        GraphicsUtil.SetHighQuality(g);

        var rect = ClientRectangle;
        if (rect.Width < 2 || rect.Height < 2)
        {
            return;
        }

        // Soft "ambient shadow" behind the card (very subtle, not harsh black).
        var shadowRect = rect;
        shadowRect.Inflate(-2, -2);
        shadowRect.Offset(0, 3);
        using (var shadowPath = GraphicsUtil.RoundedRect(shadowRect, CornerRadius))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
        {
            g.FillPath(shadowBrush, shadowPath);
        }

        var cardRect = rect;
        cardRect.Inflate(-2, -2);
        using var path = GraphicsUtil.RoundedRect(cardRect, CornerRadius);

        using (var fill = new LinearGradientBrush(cardRect, FillTop, FillBottom, 90f))
        {
            fill.WrapMode = WrapMode.TileFlipXY;
            g.FillPath(fill, path);
        }

        // "Glass" highlight line.
        using (var highlight = new LinearGradientBrush(cardRect,
                   Color.FromArgb(160, Theme.Colors.Text),
                   Color.FromArgb(0, Theme.Colors.Text),
                   90f))
        using (var highlightPen = new Pen(highlight, 1f))
        {
            var hi = cardRect;
            hi.Height = Math.Min(3, hi.Height);
            g.DrawLine(highlightPen, hi.Left + CornerRadius, hi.Top + 1, hi.Right - CornerRadius, hi.Top + 1);
        }

        if (StrokeWidth > 0)
        {
            using var stroke = new LinearGradientBrush(cardRect, StrokeTop, StrokeBottom, 90f);
            using var pen = new Pen(stroke, StrokeWidth);
            g.DrawPath(pen, path);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Width < 2 || Height < 2)
        {
            return;
        }

        using var path = GraphicsUtil.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        Region = new Region(path);
    }
}
