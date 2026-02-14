using Microsoft.Extensions.Hosting;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Concurrent;
using System.Threading;
using ConsoleApp4.Helpers;

namespace ConsoleApp4;

public sealed class MainForm : Form
{
    private readonly string[] _args;
    private readonly string _settingsPath;
    private IHost? _host;
    private bool _running;

    
    private Color _colorBg;
    private Color _colorSurface;
    private Color _colorPanel;
    private Color _colorAccent;
    private Color _colorAccent2;
    private Color _colorText;
    private Color _colorMuted;
    private Color _colorDanger;

    
    private RichTextBox _logBox = null!;
    private Button _btnStart = null!;
    private Button _btnStop = null!;
    private Button _btnSave = null!;
    private Label _lblStatus = null!;

    
    private TextBox _txtToken = null!;
    private TextBox _txtPrefix = null!;
    private TextBox _txtOwnerId = null!;
    private TextBox _txtStatusMsg = null!;
    private TextBox _txtEmbedColor = null!;

    
    private NumericUpDown _numDaily = null!;
    private NumericUpDown _numWorkMin = null!;
    private NumericUpDown _numWorkMax = null!;
    private NumericUpDown _numCrimeChance = null!;
    private NumericUpDown _numCrimeMin = null!;
    private NumericUpDown _numCrimeMax = null!;
    private NumericUpDown _numCrimeMinFine = null!;
    private NumericUpDown _numCrimeMaxFine = null!;

    public MainForm(string[] args)
    {
        _args = args;
        _settingsPath = AppSettingsFile.ResolvePath();
        InitializeUI();
        RedirectConsole();
        LoadSettings();
    }

    private void InitializeUI()
    {
        Text = "Shaco Control";
        Width = 980;
        Height = 680;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Bahnschrift", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;
        MinimumSize = new Size(900, 620);

        _colorBg = Color.FromArgb(18, 20, 26);
        _colorSurface = Color.FromArgb(26, 30, 38);
        _colorPanel = Color.FromArgb(34, 39, 49);
        _colorAccent = Color.FromArgb(0, 199, 153);
        _colorAccent2 = Color.FromArgb(90, 149, 255);
        _colorText = Color.FromArgb(230, 234, 242);
        _colorMuted = Color.FromArgb(140, 148, 164);
        _colorDanger = Color.FromArgb(242, 92, 92);

        BackColor = _colorBg;

        var tabControl = new TabControl { Dock = DockStyle.Fill };
        tabControl.Appearance = TabAppearance.Normal;
        tabControl.ItemSize = new Size(130, 30);
        tabControl.SizeMode = TabSizeMode.Fixed;
        tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabControl.Padding = new Point(14, 6);
        tabControl.BackColor = _colorBg;
        tabControl.DrawItem += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var selected = e.Index == tabControl.SelectedIndex;
            var tabRect = e.Bounds;
            tabRect.Inflate(-2, -2);
            using var bg = new SolidBrush(selected ? _colorPanel : _colorSurface);
            using var fg = new SolidBrush(selected ? _colorText : _colorMuted);
            e.Graphics.FillRectangle(bg, tabRect);
            var text = tabControl.TabPages[e.Index].Text;
            var textSize = e.Graphics.MeasureString(text, Font);
            var textX = tabRect.X + (tabRect.Width - textSize.Width) / 2;
            var textY = tabRect.Y + (tabRect.Height - textSize.Height) / 2;
            e.Graphics.DrawString(text, Font, fg, textX, textY);
        };

        
        var tabDashboard = new TabPage("Dashboard")
        {
            BackColor = _colorBg
        };
        var dashboardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(16)
        };
        dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var headerPanel = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _colorSurface,
            BorderColor = Color.FromArgb(45, 48, 60),
            CornerRadius = 14,
            Padding = new Padding(18)
        };

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));

        var titlePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };

        var title = new Label
        {
            Text = "Shaco Bot Control",
            ForeColor = _colorText,
            Font = new Font("Bahnschrift", 16F, FontStyle.Bold),
            AutoSize = true
        };
        var subtitle = new Label
        {
            Text = "Monitoramento e configuracoes em tempo real",
            ForeColor = _colorMuted,
            Font = new Font("Bahnschrift", 9.5F, FontStyle.Regular),
            AutoSize = true
        };
        titlePanel.Controls.Add(title);
        titlePanel.Controls.Add(subtitle);

        _btnStart = new Button
        {
            Text = "Iniciar Bot",
            Width = 140,
            Height = 38,
            BackColor = _colorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Bahnschrift", 10F, FontStyle.Bold)
        };
        _btnStart.UseVisualStyleBackColor = false;
        _btnStart.FlatAppearance.BorderSize = 0;
        _btnStart.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 215, 172);
        _btnStart.Click += StartClicked;

        _btnStop = new Button
        {
            Text = "Parar Bot",
            Width = 140,
            Height = 38,
            BackColor = _colorDanger,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Bahnschrift", 10F, FontStyle.Bold),
            Enabled = false
        };
        _btnStop.UseVisualStyleBackColor = false;
        _btnStop.FlatAppearance.BorderSize = 0;
        _btnStop.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 115, 115);
        _btnStop.Click += StopClicked;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        _btnStart.Margin = new Padding(0, 0, 10, 0);
        _btnStop.Margin = new Padding(0, 0, 0, 0);
        buttonPanel.Controls.Add(_btnStart);
        buttonPanel.Controls.Add(_btnStop);

        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = _colorPanel,
            ForeColor = Color.FromArgb(190, 255, 210),
            Font = new Font("Cascadia Code", 10F),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Text = ""
        };

        var logHeader = new Label
        {
            Text = "LOG DO SISTEMA",
            Dock = DockStyle.Fill,
            ForeColor = _colorMuted,
            Font = new Font("Bahnschrift", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblStatus = new Label
        {
            Text = "Status: Offline",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Bahnschrift", 9.5F, FontStyle.Bold),
            ForeColor = _colorMuted
        };

        headerLayout.Controls.Add(titlePanel, 0, 0);
        headerLayout.Controls.Add(buttonPanel, 1, 0);
        headerPanel.Controls.Add(headerLayout);

        var logContainer = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _colorSurface,
            BorderColor = Color.FromArgb(45, 48, 60),
            CornerRadius = 14,
            Padding = new Padding(14)
        };
        var logLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var logHeaderPanel = new Panel { Dock = DockStyle.Fill };
        var logAccent = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 2,
            BackColor = _colorAccent2
        };
        logHeaderPanel.Controls.Add(logHeader);
        logHeaderPanel.Controls.Add(logAccent);

        logLayout.Controls.Add(logHeaderPanel, 0, 0);
        logLayout.Controls.Add(_logBox, 0, 1);
        logContainer.Controls.Add(logLayout);

        var statusPanel = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _colorSurface,
            BorderColor = Color.FromArgb(45, 48, 60),
            CornerRadius = 12,
            Padding = new Padding(12, 6, 12, 6)
        };
        statusPanel.Controls.Add(_lblStatus);

        dashboardLayout.Controls.Add(headerPanel, 0, 0);
        dashboardLayout.Controls.Add(logContainer, 0, 1);
        dashboardLayout.Controls.Add(statusPanel, 0, 2);

        tabDashboard.Controls.Add(dashboardLayout);

        
        var tabGeneral = new TabPage("Geral")
        {
            BackColor = _colorBg
        };
        var generalCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _colorSurface,
            BorderColor = Color.FromArgb(45, 48, 60),
            CornerRadius = 14,
            Padding = new Padding(20)
        };
        var generalLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 2,
            Padding = new Padding(20)
        };
        generalLayout.BackColor = _colorSurface;
        generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtToken = AddLabelAndText(generalLayout, "Bot Token:", 0, true, _colorText, _colorPanel);
        _txtPrefix = AddLabelAndText(generalLayout, "Prefixo:", 1, false, _colorText, _colorPanel);
        _txtOwnerId = AddLabelAndText(generalLayout, "ID Dono:", 2, false, _colorText, _colorPanel);
        _txtStatusMsg = AddLabelAndText(generalLayout, "Status (Atividade):", 3, false, _colorText, _colorPanel);
        _txtEmbedColor = AddLabelAndText(generalLayout, "Cor Embed:", 4, false, _colorText, _colorPanel);

        generalCard.Controls.Add(generalLayout);
        tabGeneral.Controls.Add(generalCard);

        
        var tabEconomy = new TabPage("Economia")
        {
            BackColor = _colorBg
        };
        var economyCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _colorSurface,
            BorderColor = Color.FromArgb(45, 48, 60),
            CornerRadius = 14,
            Padding = new Padding(16)
        };
        var economyPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10),
            BackColor = _colorSurface
        };

        _numDaily = AddNumericSetting(economyPanel, "Recompensa Diaria", 1, 10000, _colorPanel, _colorText, _colorAccent2);
        _numWorkMin = AddNumericSetting(economyPanel, "Trabalho Min", 1, 10000, _colorPanel, _colorText, _colorAccent2);
        _numWorkMax = AddNumericSetting(economyPanel, "Trabalho Max", 1, 10000, _colorPanel, _colorText, _colorAccent2);
        _numCrimeChance = AddNumericSetting(economyPanel, "Chance Crime (%)", 0, 100, _colorPanel, _colorText, _colorAccent2);
        _numCrimeMin = AddNumericSetting(economyPanel, "Crime Recompensa Min", 1, 20000, _colorPanel, _colorText, _colorAccent2);
        _numCrimeMax = AddNumericSetting(economyPanel, "Crime Recompensa Max", 1, 20000, _colorPanel, _colorText, _colorAccent2);
        _numCrimeMinFine = AddNumericSetting(economyPanel, "Multa Crime Min", 1, 20000, _colorPanel, _colorText, _colorAccent2);
        _numCrimeMaxFine = AddNumericSetting(economyPanel, "Multa Crime Max", 1, 20000, _colorPanel, _colorText, _colorAccent2);

        economyCard.Controls.Add(economyPanel);
        tabEconomy.Controls.Add(economyCard);

        
        tabControl.TabPages.Add(tabDashboard);
        tabControl.TabPages.Add(tabGeneral);
        tabControl.TabPages.Add(tabEconomy);

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        _btnSave = new Button
        {
            Text = "Salvar Configuracoes",
            Dock = DockStyle.Fill,
            Width = 220,
            Height = 40,
            BackColor = _colorAccent2,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Bahnschrift", 10F, FontStyle.Bold)
        };
        _btnSave.UseVisualStyleBackColor = false;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(120, 175, 255);
        _btnSave.Click += SaveClicked;

        var bottomBar = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _colorSurface,
            BorderColor = Color.FromArgb(45, 48, 60),
            CornerRadius = 12,
            Padding = new Padding(16, 10, 16, 10)
        };
        var bottomLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));

        var hintLabel = new Label
        {
            Text = "Altere as configuracoes e clique em Salvar. Reinicie para aplicar.",
            Dock = DockStyle.Fill,
            ForeColor = _colorMuted,
            Font = new Font("Bahnschrift", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        bottomLayout.Controls.Add(hintLabel, 0, 0);
        bottomLayout.Controls.Add(_btnSave, 1, 0);
        bottomBar.Controls.Add(bottomLayout);

        mainLayout.Controls.Add(tabControl, 0, 0);
        mainLayout.Controls.Add(bottomBar, 0, 1);

        Controls.Add(mainLayout);
    }

    private TextBox AddLabelAndText(
        TableLayoutPanel panel,
        string labelText,
        int row,
        bool isPassword,
        Color labelColor,
        Color inputBackColor)
    {
        panel.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = labelColor,
            Font = new Font("Bahnschrift", 9.5F, FontStyle.Bold)
        }, 0, row);
        var txt = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = inputBackColor,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Bahnschrift", 9.5F, FontStyle.Regular)
        };
        txt.Margin = new Padding(0, 4, 0, 4);
        if (isPassword) txt.UseSystemPasswordChar = true;
        panel.Controls.Add(txt, 1, row);
        return txt;
    }

    private NumericUpDown AddNumericSetting(
        FlowLayoutPanel panel,
        string labelText,
        int min,
        int max,
        Color groupBackColor,
        Color textColor,
        Color accent)
    {
        var group = new GroupBox
        {
            Text = labelText,
            Width = 220,
            Height = 72,
            Margin = new Padding(10),
            BackColor = groupBackColor,
            ForeColor = textColor,
            Font = new Font("Bahnschrift", 9F, FontStyle.Bold)
        };
        var num = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Width = 190,
            Location = new Point(12, 28),
            BackColor = Color.FromArgb(20, 23, 30),
            ForeColor = textColor,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Bahnschrift", 9F, FontStyle.Regular)
        };
        if (num.Controls.Count > 0)
        {
            num.Controls[0].BackColor = accent;
        }
        group.Controls.Add(num);
        panel.Controls.Add(group);
        return num;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            ClientRectangle,
            _colorBg,
            Color.FromArgb(14, 17, 24),
            90f);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    private void RedirectConsole()
    {
        var writer = new RichTextBoxWriter(_logBox);
        Console.SetOut(writer);
        Console.SetError(writer);
    }

    private void LoadSettings()
    {
        try
        {
            var root = AppSettingsFile.Load(_settingsPath, out var error);
            if (root == null)
            {
                Log($"Erro ao carregar configuracoes ({_settingsPath}): {(error?.Message ?? "arquivo invalido")}");
                return;
            }

            
            _txtToken.Text = root["Bot"]?["Token"]?.ToString() ?? "";
            _txtPrefix.Text = root["Bot"]?["Prefix"]?.ToString() ?? "";
            _txtOwnerId.Text = root["Bot"]?["OwnerUserId"]?.ToString() ?? "";
            _txtStatusMsg.Text = root["Bot"]?["Status"]?.ToString() ?? "";
            _txtEmbedColor.Text = root["Bot"]?["EmbedColor"]?.ToString() ?? "";

            
            var eco = root["Economy"];
            if (eco != null)
            {
                SetVal(_numDaily, eco["DailyReward"]);
                SetVal(_numWorkMin, eco["WorkMinReward"]);
                SetVal(_numWorkMax, eco["WorkMaxReward"]);
                SetVal(_numCrimeChance, eco["CrimeSuccessChancePercent"]);
                SetVal(_numCrimeMin, eco["CrimeMinReward"]);
                SetVal(_numCrimeMax, eco["CrimeMaxReward"]);
                SetVal(_numCrimeMinFine, eco["CrimeMinFine"]);
                SetVal(_numCrimeMaxFine, eco["CrimeMaxFine"]);
            }
        }
        catch (Exception ex)
        {
            Log($"Erro ao carregar configuracoes: {ex.Message}");
        }
    }

    private void SetVal(NumericUpDown control, JsonNode? node)
    {
        if (node != null && int.TryParse(node.ToString(), out int val))
        {
            control.Value = Math.Max(control.Minimum, Math.Min(control.Maximum, val));
        }
    }

    private void SaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var root = AppSettingsFile.Load(_settingsPath, out var loadError);
            if (root == null)
            {
                MessageBox.Show(
                    $"Erro ao carregar {_settingsPath}: {(loadError?.Message ?? "arquivo invalido")}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            
            var bot = GetOrCreateObject(root, "Bot");
            bot["Token"] = _txtToken.Text;
            bot["Prefix"] = _txtPrefix.Text;
            if (ulong.TryParse(_txtOwnerId.Text, out var oid))
            {
                bot["OwnerUserId"] = oid;
            }
            bot["Status"] = _txtStatusMsg.Text;
            bot["EmbedColor"] = _txtEmbedColor.Text;

            
            var eco = GetOrCreateObject(root, "Economy");
            eco["DailyReward"] = (int)_numDaily.Value;
            eco["WorkMinReward"] = (int)_numWorkMin.Value;
            eco["WorkMaxReward"] = (int)_numWorkMax.Value;
            eco["CrimeSuccessChancePercent"] = (int)_numCrimeChance.Value;
            eco["CrimeMinReward"] = (int)_numCrimeMin.Value;
            eco["CrimeMaxReward"] = (int)_numCrimeMax.Value;
            eco["CrimeMinFine"] = (int)_numCrimeMinFine.Value;
            eco["CrimeMaxFine"] = (int)_numCrimeMaxFine.Value;

            if (!AppSettingsFile.Save(_settingsPath, root, out var saveError))
            {
                MessageBox.Show(
                    $"Erro ao salvar {_settingsPath}: {(saveError?.Message ?? "falha desconhecida")}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show("Configuracoes salvas! Reinicie o bot para aplicar.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Log($"Configuracoes salvas em {_settingsPath}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void StartClicked(object? sender, EventArgs e)
    {
        if (_running) return;

        _btnStart.Enabled = false;
        _btnStop.Enabled = true;
        _lblStatus.Text = "Status: Iniciando...";
        _lblStatus.ForeColor = _colorAccent2;

        try
        {
            _host = BotHost.Build(_args);
            await BotHost.StartAsync(_host);
            _running = true;
            _lblStatus.Text = "Status: Online";
            _lblStatus.ForeColor = _colorAccent;
            Log("Bot iniciado com sucesso.");
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Status: Erro";
            _lblStatus.ForeColor = _colorDanger;
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            try
            {
                _host?.Dispose();
            }
            catch
            {
                
            }
            _host = null;
            Log($"ERRO FATAL: {ex.Message}");
            MessageBox.Show($"Falha ao iniciar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void StopClicked(object? sender, EventArgs e)
    {
        if (!_running || _host == null) return;

        _btnStop.Enabled = false;
        _lblStatus.Text = "Status: Parando...";

        try
        {
            await BotHost.StopAsync(_host);
            _host.Dispose();
            _host = null;
            _running = false;

            _lblStatus.Text = "Status: Offline";
            _lblStatus.ForeColor = _colorMuted;
            _btnStart.Enabled = true;
            Log("Bot parado.");
        }
        catch (Exception ex)
        {
            Log($"Erro ao parar: {ex.Message}");
            _btnStop.Enabled = true;
        }
    }

    private void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_running && _host != null)
        {
            BotHost.StopAsync(_host).GetAwaiter().GetResult();
            _host.Dispose();
        }
        base.OnFormClosing(e);
    }

    private static JsonObject GetOrCreateObject(JsonNode root, string key)
    {
        if (root is not JsonObject obj)
        {
            throw new InvalidOperationException("Settings root must be a JSON object.");
        }

        if (obj[key] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        obj[key] = created;
        return created;
    }

    private sealed class CardPanel : Panel
    {
        public int CornerRadius { get; set; } = 12;
        public Color BorderColor { get; set; } = Color.FromArgb(45, 48, 60);
        public int BorderThickness { get; set; } = 1;

        public CardPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using var path = CreateRoundedRectangle(rect, CornerRadius);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);

            if (BorderThickness > 0)
            {
                using var pen = new Pen(BorderColor, BorderThickness);
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width < 2 || Height < 2)
            {
                return;
            }

            using var path = CreateRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
            Region = new Region(path);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var r = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
            if (r == 0)
            {
                var path = new GraphicsPath();
                path.AddRectangle(rect);
                return path;
            }

            var diameter = r * 2;
            var pathRounded = new GraphicsPath();
            pathRounded.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            pathRounded.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            pathRounded.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            pathRounded.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            pathRounded.CloseFigure();
            return pathRounded;
        }
    }

    private sealed class RichTextBoxWriter : TextWriter
    {
        private readonly RichTextBox _box;
        private readonly ConcurrentQueue<string> _pending = new();
        private int _flushScheduled;

        public RichTextBoxWriter(RichTextBox box) => _box = box;

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            Enqueue(value.ToString());
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Enqueue(value);
        }

        public override void WriteLine(string? value)
        {
            Enqueue((value ?? string.Empty) + Environment.NewLine);
        }

        private void Enqueue(string text)
        {
            if (_box.IsDisposed)
            {
                return;
            }

            _pending.Enqueue(text);
            ScheduleFlush();
        }

        private void ScheduleFlush()
        {
            if (_box.IsDisposed || !_box.IsHandleCreated)
            {
                return;
            }

            
            if (Interlocked.Exchange(ref _flushScheduled, 1) == 1)
            {
                return;
            }

            try
            {
                _box.BeginInvoke(new Action(FlushPending));
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref _flushScheduled, 0);
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _flushScheduled, 0);
            }
        }

        private void FlushPending()
        {
            Interlocked.Exchange(ref _flushScheduled, 0);
            if (_box.IsDisposed)
            {
                return;
            }

            var sb = new StringBuilder();
            while (_pending.TryDequeue(out var s))
            {
                sb.Append(s);
            }

            if (sb.Length == 0)
            {
                return;
            }

            _box.AppendText(sb.ToString());
            _box.ScrollToCaret();

            
            if (!_pending.IsEmpty)
            {
                ScheduleFlush();
            }
        }
    }
}
