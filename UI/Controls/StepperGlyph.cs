using System.Drawing.Drawing2D;

namespace ConsoleApp4.UI.Controls;

internal sealed class StepperGlyph : Control
{
    private bool _hover;
    private bool _down;

    public string Glyph { get; set; } = "";

    public event EventHandler? Pressed;

    public StepperGlyph()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        TabStop = false;
        Cursor = Cursors.Hand;
        BackColor = Theme.Colors.Surface2;
        ForeColor = Theme.Colors.Muted;
        Font = Theme.Fonts.Icon(10.5f);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        _down = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _down = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_down || e.Button != MouseButtons.Left) return;
        _down = false;
        Invalidate();
        if (ClientRectangle.Contains(e.Location))
        {
            Pressed?.Invoke(this, EventArgs.Empty);
        }
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

        
        var fill = _down
            ? Color.FromArgb(255, Theme.Colors.Surface0)
            : _hover
                ? Color.FromArgb(255, Theme.Colors.Surface1)
                : Color.FromArgb(255, Theme.Colors.Surface2);

        using (var brush = new SolidBrush(fill))
        using (var path = GraphicsUtil.RoundedRect(new Rectangle(0, 0, rect.Width - 1, rect.Height - 1), 8))
        {
            g.FillPath(brush, path);

            using var pen = new Pen(Color.FromArgb(_hover ? 170 : 120, Theme.Colors.Stroke0), 1f);
            g.DrawPath(pen, path);
        }

        var fg = _hover ? Theme.Colors.Text : Theme.Colors.Muted;
        TextRenderer.DrawText(g, Glyph, Font, rect, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

