using System.Drawing.Drawing2D;

namespace ConsoleApp4.UI.Controls;

internal sealed class AnimatedButton : Control
{
    private readonly System.Windows.Forms.Timer _timer;
    private float _t; 
    private float _target;
    private bool _pressed;

    public string IconGlyph { get; set; } = "";
    public Color Accent { get; set; } = Theme.Colors.Accent2;
    public Color Accent2 { get; set; } = Theme.Colors.Accent;
    public bool IsDanger { get; set; }

    public int CornerRadius { get; set; } = Theme.Radii.R14;

    public AnimatedButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable, true);

        Cursor = Cursors.Hand;
        Size = new Size(170, 42);
        Font = Theme.Fonts.Ui(10f, FontStyle.Bold);
        ForeColor = Theme.Colors.Text;
        TabStop = true;

        _timer = new System.Windows.Forms.Timer { Interval = 15 };
        _timer.Tick += (_, _) =>
        {
            
            var speed = 0.18f;
            _t += (_target - _t) * speed;
            if (Math.Abs(_target - _t) < 0.01f)
            {
                _t = _target;
                _timer.Stop();
            }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _target = 1f;
        _timer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _target = 0f;
        _timer.Start();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_pressed && e.Button == MouseButtons.Left)
        {
            _pressed = false;
            Invalidate();
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            _pressed = false;
            Invalidate();
            OnClick(EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        GraphicsUtil.SetHighQuality(g);

        var rect = ClientRectangle;
        rect.Inflate(-1, -1);

        if (!Enabled)
        {
            
            using var pathDisabled = GraphicsUtil.RoundedRect(rect, CornerRadius);
            using var fillDisabled = new LinearGradientBrush(rect,
                Color.FromArgb(255, Theme.Colors.Surface0),
                Color.FromArgb(255, Theme.Colors.Surface1),
                90f);
            g.FillPath(fillDisabled, pathDisabled);
            using var penDisabled = new Pen(Color.FromArgb(120, Theme.Colors.Stroke0), 1f);
            g.DrawPath(penDisabled, pathDisabled);

            var padDisabled = Theme.Spacing.S12;
            var xDisabled = rect.Left + padDisabled;
            var cyDisabled = rect.Top + rect.Height / 2;
            var fg = Color.FromArgb(130, Theme.Colors.Text);

            if (!string.IsNullOrWhiteSpace(IconGlyph))
            {
                using var iconFont = Theme.Fonts.Icon(15f);
                var iconRect = new Rectangle(xDisabled, cyDisabled - 8, 22, 16);
                TextRenderer.DrawText(g, IconGlyph, iconFont, iconRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                xDisabled += 26;
            }

            var textRectDisabled = new Rectangle(xDisabled, rect.Top, rect.Right - xDisabled - padDisabled, rect.Height);
            TextRenderer.DrawText(g, Text, Font, textRectDisabled, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            return;
        }

        var a0 = IsDanger ? Theme.Colors.Danger : Accent;
        var a1 = IsDanger ? Color.FromArgb(255, 255, 120, 120) : Accent2;

        
        var lift = (int)Math.Round(_t * 2f);
        var press = _pressed ? 1 : 0;
        rect.Offset(0, -lift + press);

        using var path = GraphicsUtil.RoundedRect(rect, CornerRadius);

        
        var shadow = rect;
        shadow.Offset(0, 4);
        using (var shadowPath = GraphicsUtil.RoundedRect(shadow, CornerRadius))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
        {
            g.FillPath(shadowBrush, shadowPath);
        }

        
        
        
        var baseTop = Theme.Colors.Surface1;
        var baseBottom = Theme.Colors.Surface2;
        using (var fill = new LinearGradientBrush(rect,
                   Blend(baseTop, a0, _t * 0.60f),
                   Blend(baseBottom, a1, _t * 0.45f),
                   90f))
        {
            fill.WrapMode = WrapMode.TileFlipXY;
            g.FillPath(fill, path);
        }

        
        using (var stroke = new LinearGradientBrush(rect,
                   Color.FromArgb(200, Blend(Theme.Colors.Stroke1, a0, _t * 0.7f)),
                   Color.FromArgb(80, Theme.Colors.Stroke0),
                   90f))
        using (var pen = new Pen(stroke, 1f))
        {
            g.DrawPath(pen, path);
        }

        
        if (Focused)
        {
            var ring = rect;
            ring.Inflate(2, 2);
            using var ringPath = GraphicsUtil.RoundedRect(ring, CornerRadius + 2);
            using var ringPen = new Pen(Color.FromArgb(200, a0), 2f);
            g.DrawPath(ringPen, ringPath);
        }

        
        var padX = Theme.Spacing.S12;
        var iconSize = 16;

        var x = rect.Left + padX;
        var cy = rect.Top + rect.Height / 2;

        if (!string.IsNullOrWhiteSpace(IconGlyph))
        {
            using var iconFont = Theme.Fonts.Icon(15f);
            var iconRect = new Rectangle(x, cy - iconSize / 2, iconSize + 6, iconSize);
            TextRenderer.DrawText(g, IconGlyph, iconFont, iconRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            x += iconSize + 10;
        }

        var textRect = new Rectangle(x, rect.Top, rect.Right - x - padX, rect.Height);
        TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static Color Blend(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var ar = a.R + (int)Math.Round((b.R - a.R) * t);
        var ag = a.G + (int)Math.Round((b.G - a.G) * t);
        var ab = a.B + (int)Math.Round((b.B - a.B) * t);
        var aa = a.A + (int)Math.Round((b.A - a.A) * t);
        return Color.FromArgb(aa, ar, ag, ab);
    }
}
