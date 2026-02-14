using System.Drawing.Drawing2D;

namespace ConsoleApp4.UI.Controls;

internal abstract class FieldBase : UserControl
{
    private bool _focused;
    private string _label = "";
    private string _hint = "";
    private bool _invalid;

    public string Label
    {
        get => _label;
        set { _label = value ?? ""; Invalidate(); }
    }

    public string Hint
    {
        get => _hint;
        set { _hint = value ?? ""; Invalidate(); }
    }

    public bool Invalid
    {
        get => _invalid;
        set { _invalid = value; Invalidate(); }
    }

    protected FieldBase()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;
        ForeColor = Theme.Colors.Text;
        Font = Theme.Fonts.Ui(9.75f);
        Padding = new Padding(0);
        Height = 84;
        MinimumSize = new Size(220, 84);
    }

    protected abstract Control InnerControl { get; }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();

        if (TryGetInnerControl() is { } inner)
        {
            inner.GotFocus += (_, _) => { _focused = true; Invalidate(); };
            inner.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        }
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        var inner = TryGetInnerControl();
        if (inner == null)
        {
            return;
        }

        var top = 26;
        var h = 36;
        inner.Bounds = new Rectangle(0, top, Width, h);
    }

    private Control? TryGetInnerControl()
    {
        try
        {
            return InnerControl;
        }
        catch
        {
            // Derived controls might not have initialized their inner widget yet.
            return null;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        GraphicsUtil.SetHighQuality(g);

        // Label
        using var labelFont = Theme.Fonts.Ui(9f, FontStyle.Bold);
        var labelRect = new Rectangle(0, 0, Width, 18);
        TextRenderer.DrawText(g, Label, labelFont, labelRect, Theme.Colors.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        // Field container
        var boxRect = new Rectangle(0, 24, Width, 38);
        using var path = GraphicsUtil.RoundedRect(boxRect, Theme.Radii.R14);

        var fillTop = Color.FromArgb(255, Theme.Colors.Surface1);
        var fillBottom = Color.FromArgb(255, Theme.Colors.Surface2);
        using (var fill = new LinearGradientBrush(boxRect, fillTop, fillBottom, 90f))
        {
            fill.WrapMode = WrapMode.TileFlipXY;
            g.FillPath(fill, path);
        }

        var strokeColor = Invalid
            ? Theme.Colors.Danger
            : _focused
                ? Theme.Colors.Accent2
                : Theme.Colors.Stroke0;

        using (var pen = new Pen(Color.FromArgb(_focused ? 230 : 180, strokeColor), _focused ? 2f : 1f))
        {
            g.DrawPath(pen, path);
        }

        // Hint
        if (!string.IsNullOrWhiteSpace(Hint))
        {
            using var hintFont = Theme.Fonts.Ui(8.5f);
            var hintRect = new Rectangle(0, 66, Width, 16);
            var hintColor = Invalid ? Theme.Colors.Danger : Theme.Colors.Faint;
            TextRenderer.DrawText(g, Hint, hintFont, hintRect, hintColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }
}

internal sealed class TextField : FieldBase
{
    private readonly TextBox _textBox;
    private readonly Button _toggle;
    private bool _usePasswordChar;

    public TextBox TextBox => _textBox;

    public bool UsePasswordChar
    {
        get => _usePasswordChar;
        set
        {
            _usePasswordChar = value;
            _textBox.UseSystemPasswordChar = value && !_reveal;
            _toggle.Visible = value;
            Invalidate();
        }
    }

    private bool _reveal;

    protected override Control InnerControl => _textBox;

    public TextField()
    {
        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Colors.Surface2,
            ForeColor = Theme.Colors.Text,
            Font = Theme.Fonts.Ui(10f),
            Margin = new Padding(12, 8, 12, 8)
        };

        _toggle = new Button
        {
            FlatStyle = FlatStyle.Flat,
            Text = "\uE8F4", // View (eye)
            Font = Theme.Fonts.Icon(12f),
            ForeColor = Theme.Colors.Muted,
            BackColor = Color.Transparent,
            TabStop = false,
            Cursor = Cursors.Hand,
            Visible = false
        };
        _toggle.FlatAppearance.BorderSize = 0;
        _toggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, Theme.Colors.Text);
        _toggle.Click += (_, _) =>
        {
            _reveal = !_reveal;
            _textBox.UseSystemPasswordChar = UsePasswordChar && !_reveal;
            _toggle.ForeColor = _reveal ? Theme.Colors.Text : Theme.Colors.Muted;
        };

        Controls.Add(_textBox);
        Controls.Add(_toggle);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        if (_textBox == null)
        {
            return;
        }

        var boxRect = new Rectangle(0, 24, Width, 38);
        var padL = 12;
        var padR = UsePasswordChar ? 44 : 12;

        _textBox.Bounds = new Rectangle(
            boxRect.Left + padL,
            boxRect.Top + 10,
            boxRect.Width - padL - padR,
            18);

        if (_toggle == null)
        {
            return;
        }

        _toggle.Bounds = new Rectangle(boxRect.Right - 36, boxRect.Top + 6, 30, 26);
    }
}

internal sealed class NumberField : FieldBase
{
    private readonly NumericUpDown _num;
    private readonly StepperGlyph _up;
    private readonly StepperGlyph _down;
    protected override Control InnerControl => _num;

    public NumericUpDown Numeric => _num;

    public NumberField()
    {
        _num = new NumericUpDown
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Colors.Surface2,
            ForeColor = Theme.Colors.Text,
            Font = Theme.Fonts.Ui(10f),
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 0,
            ThousandsSeparator = true,
            InterceptArrowKeys = true
        };

        // Hide the default WinForms spinner buttons (they don't theme well and show up white).
        if (_num.Controls.Count > 0)
        {
            _num.Controls[0].Visible = false;
            _num.Controls[0].Enabled = false;
        }

        _up = new StepperGlyph { Glyph = FluentGlyphs.ChevronUp };
        _down = new StepperGlyph { Glyph = FluentGlyphs.ChevronDown };
        _up.Pressed += (_, _) =>
        {
            _num.UpButton();
            _num.Focus();
        };
        _down.Pressed += (_, _) =>
        {
            _num.DownButton();
            _num.Focus();
        };

        Controls.Add(_num);
        Controls.Add(_up);
        Controls.Add(_down);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        if (_num == null)
        {
            return;
        }

        var boxRect = new Rectangle(0, 24, Width, 38);
        var stepW = 28;
        var stepPad = 6;
        var rightPad = stepW + stepPad + 10;

        _num.Bounds = new Rectangle(
            boxRect.Left + 12,
            boxRect.Top + 8,
            boxRect.Width - 24 - rightPad,
            22);

        // Custom stepper, aligned inside the field container.
        var x = boxRect.Right - stepW - 8;
        _up.Bounds = new Rectangle(x, boxRect.Top + 6, stepW, 13);
        _down.Bounds = new Rectangle(x, boxRect.Top + 19, stepW, 13);
    }
}
