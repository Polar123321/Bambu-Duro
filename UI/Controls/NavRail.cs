using System.Drawing.Drawing2D;

namespace ConsoleApp4.UI.Controls;

internal sealed class NavRail : Control
{
    private readonly List<NavItem> _items = new();
    private int _selectedIndex;

    public event EventHandler<int>? SelectedIndexChanged;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var v = Math.Clamp(value, 0, Math.Max(0, _items.Count - 1));
            if (_selectedIndex == v) return;
            _selectedIndex = v;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, _selectedIndex);
        }
    }

    public NavRail()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        
        BackColor = Theme.Colors.Surface0;
        ForeColor = Theme.Colors.Text;
        Font = Theme.Fonts.Ui(10.5f, FontStyle.Bold);
        Width = 230;
        Cursor = Cursors.Hand;
    }

    public void SetItems(IEnumerable<NavItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        SelectedIndex = Math.Min(SelectedIndex, Math.Max(0, _items.Count - 1));
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        var idx = HitTest(e.Location);
        if (idx >= 0) SelectedIndex = idx;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var idx = HitTest(e.Location);
        Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;
    }

    private int HitTest(Point p)
    {
        var y = Theme.Spacing.S20 + 52; 
        var itemH = 44;
        for (var i = 0; i < _items.Count; i++)
        {
            var r = new Rectangle(Theme.Spacing.S12, y + i * itemH, Width - Theme.Spacing.S24, 40);
            if (r.Contains(p)) return i;
        }
        return -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        GraphicsUtil.SetHighQuality(g);

        var rect = ClientRectangle;
        using (var bg = new LinearGradientBrush(rect, Theme.Colors.Surface0, Theme.Colors.Bg0, 90f))
        {
            g.FillRectangle(bg, rect);
        }

        
        using (var pen = new Pen(Color.FromArgb(130, Theme.Colors.Stroke0), 1f))
        {
            g.DrawLine(pen, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom);
        }

        
        var brandRect = new Rectangle(Theme.Spacing.S12, Theme.Spacing.S16, Width - Theme.Spacing.S24, 52);
        using (var titleFont = Theme.Fonts.Ui(14.5f, FontStyle.Bold))
        using (var subtitleFont = Theme.Fonts.Ui(9.25f))
        {
            TextRenderer.DrawText(g, "Shaco", titleFont, new Rectangle(brandRect.Left, brandRect.Top + 2, brandRect.Width, 22),
                Theme.Colors.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "Control Center", subtitleFont, new Rectangle(brandRect.Left, brandRect.Top + 26, brandRect.Width, 18),
                Theme.Colors.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        
        var y = Theme.Spacing.S20 + 52;
        var itemH = 44;
        using var iconFont = Theme.Fonts.Icon(14f);
        using var textFont = Theme.Fonts.Ui(10.5f, FontStyle.Bold);

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var isSelected = i == SelectedIndex;

            var r = new Rectangle(Theme.Spacing.S12, y + i * itemH, Width - Theme.Spacing.S24, 40);
            using var path = GraphicsUtil.RoundedRect(r, Theme.Radii.R14);

            if (isSelected)
            {
                using var fill = new LinearGradientBrush(r,
                    Color.FromArgb(70, Theme.Colors.Accent2),
                    Color.FromArgb(20, Theme.Colors.Accent2),
                    90f);
                g.FillPath(fill, path);

                using var glowPen = new Pen(Color.FromArgb(200, Theme.Colors.Accent2), 1.5f);
                g.DrawPath(glowPen, path);

                
                var ind = new Rectangle(r.Left - 2, r.Top + 10, 4, r.Height - 20);
                using var indPath = GraphicsUtil.RoundedRect(ind, 4);
                using var indBrush = new SolidBrush(Theme.Colors.Accent2);
                g.FillPath(indBrush, indPath);
            }
            else
            {
                using var fill = new SolidBrush(Color.FromArgb(10, Theme.Colors.Text));
                g.FillPath(fill, path);
            }

            var iconRect = new Rectangle(r.Left + 12, r.Top, 24, r.Height);
            var textRect = new Rectangle(r.Left + 42, r.Top, r.Width - 54, r.Height);
            var fg = isSelected ? Theme.Colors.Text : Theme.Colors.Muted;

            TextRenderer.DrawText(g, item.Glyph, iconFont, iconRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, item.Text, textFont, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }
}

internal sealed record NavItem(string Text, string Glyph);
