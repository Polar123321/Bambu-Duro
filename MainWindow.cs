using ConsoleApp4.Helpers;
using ConsoleApp4.UI;
using ConsoleApp4.UI.Controls;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;

namespace ConsoleApp4;





public sealed class MainWindow : Form
{
    private readonly string[] _args;
    private readonly string _settingsPath;

    private IHost? _host;
    private bool _running;

    
    private NavRail _nav = null!;
    private Panel _pageHost = null!;
    private Panel _pageDashboard = null!;
    private Panel _pageGeneral = null!;
    private Panel _pageEconomy = null!;

    
    private RichTextBox _logBox = null!;
    private Label _statusPill = null!;
    private AnimatedButton _btnStart = null!;
    private AnimatedButton _btnStop = null!;

    
    private TextField _fToken = null!;
    private TextField _fPrefix = null!;
    private TextField _fOwnerId = null!;
    private TextField _fStatus = null!;
    private TextField _fEmbedColor = null!;
    private AnimatedButton _btnSave = null!;

    
    private NumberField _nDaily = null!;
    private NumberField _nWorkMin = null!;
    private NumberField _nWorkMax = null!;
    private NumberField _nCrimeChance = null!;
    private NumberField _nCrimeMin = null!;
    private NumberField _nCrimeMax = null!;
    private NumberField _nCrimeMinFine = null!;
    private NumberField _nCrimeMaxFine = null!;

    public MainWindow(string[] args)
    {
        _args = args;
        _settingsPath = AppSettingsFile.ResolvePath();

        InitializeChrome();
        BuildLayout();
        RedirectConsole();

        LoadSettings();
        UpdateStatusPill("Offline", Theme.Colors.Muted);
    }

    private void InitializeChrome()
    {
        Text = "Shaco Control Center";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1040, 720);
        Font = Theme.Fonts.Ui(9.75f);
        BackColor = Theme.Colors.Bg0;
        ForeColor = Theme.Colors.Text;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var bg = Theme.BgGradient(ClientRectangle);
        e.Graphics.FillRectangle(bg, ClientRectangle);
    }

    private void BuildLayout()
    {
        _nav = new NavRail { Dock = DockStyle.Left };
        _nav.SetItems(new[]
        {
            new NavItem("Dashboard", FluentGlyphs.Dashboard),
            new NavItem("Geral", FluentGlyphs.Settings),
            new NavItem("Economia", FluentGlyphs.Wallet),
        });
        _nav.SelectedIndexChanged += (_, idx) => ShowPage(idx);

        _pageHost = new BufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(Theme.Spacing.S24, Theme.Spacing.S20, Theme.Spacing.S24, Theme.Spacing.S20)
        };

        Controls.Add(_pageHost);
        Controls.Add(_nav);

        _pageDashboard = BuildDashboardPage();
        _pageGeneral = BuildGeneralPage();
        _pageEconomy = BuildEconomyPage();

        _pageHost.Controls.Add(_pageDashboard);
        _pageHost.Controls.Add(_pageGeneral);
        _pageHost.Controls.Add(_pageEconomy);

        ShowPage(0);
    }

    private void ShowPage(int index)
    {
        
        
        _pageHost.SuspendLayout();
        try
        {
            _pageDashboard.Visible = index == 0;
            _pageGeneral.Visible = index == 1;
            _pageEconomy.Visible = index == 2;

            var selected = index switch
            {
                0 => _pageDashboard,
                1 => _pageGeneral,
                2 => _pageEconomy,
                _ => _pageDashboard
            };

            selected.BringToFront();
        }
        finally
        {
            _pageHost.ResumeLayout(performLayout: true);
        }
    }

    private Panel BuildDashboardPage()
    {
        var root = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        
        var header = new GlassCard
        {
            Dock = DockStyle.Fill,
            CornerRadius = Theme.Radii.R18,
            Padding = new Padding(Theme.Spacing.S20)
        };

        var hGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        hGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));

        var titlePanel = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = new Label
        {
            Text = "Shaco Bot Control",
            Dock = DockStyle.Top,
            Height = 30,
            Font = Theme.Fonts.Ui(18f, FontStyle.Bold),
            ForeColor = Theme.Colors.Text
        };
        var subtitle = new Label
        {
            Text = "Monitoramento em tempo real, configuracoes e operacoes seguras",
            Dock = DockStyle.Top,
            Height = 22,
            Font = Theme.Fonts.Ui(10f),
            ForeColor = Theme.Colors.Muted
        };

        _statusPill = new Label
        {
            AutoSize = false,
            Height = 28,
            Width = 160,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Fonts.Ui(9.25f, FontStyle.Bold),
            BackColor = Color.FromArgb(35, Theme.Colors.Stroke0),
            ForeColor = Theme.Colors.Muted
        };

        var statusWrap = new BufferedPanel { Dock = DockStyle.Top, Height = 38, BackColor = Color.Transparent };
        statusWrap.Controls.Add(_statusPill);
        _statusPill.Location = new Point(0, 6);

        titlePanel.Controls.Add(statusWrap);
        titlePanel.Controls.Add(subtitle);
        titlePanel.Controls.Add(title);

        
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnStart = new AnimatedButton { Text = "Iniciar Bot", IconGlyph = FluentGlyphs.Play, Accent = Theme.Colors.Accent2, Accent2 = Theme.Colors.Accent };
        _btnStop = new AnimatedButton { Text = "Parar Bot", IconGlyph = FluentGlyphs.Stop, IsDanger = true, Enabled = false };
        
        _btnStart.Height = 38;
        _btnStop.Height = 38;
        _btnStart.Margin = new Padding(0, 0, 0, 10);
        _btnStop.Margin = new Padding(0, 0, 0, 0);
        _btnStart.Click += StartClicked;
        _btnStop.Click += StopClicked;

        actions.Controls.Add(_btnStart);
        actions.Controls.Add(_btnStop);

        hGrid.Controls.Add(titlePanel, 0, 0);
        hGrid.Controls.Add(actions, 1, 0);
        header.Controls.Add(hGrid);

        
        var logCard = new GlassCard
        {
            Dock = DockStyle.Fill,
            CornerRadius = Theme.Radii.R18,
            Padding = new Padding(Theme.Spacing.S16)
        };

        var logLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Color.Transparent };
        logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var logHeader = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var logTitle = new Label
        {
            Text = "SYSTEM LOG",
            Dock = DockStyle.Left,
            Width = 220,
            Font = Theme.Fonts.Ui(9f, FontStyle.Bold),
            ForeColor = Theme.Colors.Muted
        };
        var logAccent = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Color.FromArgb(200, Theme.Colors.Accent2) };
        logHeader.Controls.Add(logTitle);
        logHeader.Controls.Add(logAccent);

        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Colors.Surface2,
            ForeColor = Color.FromArgb(225, 210, 255, 230),
            Font = Theme.Fonts.Mono(10f),
            ReadOnly = true,
            HideSelection = false
        };

        logLayout.Controls.Add(logHeader, 0, 0);
        logLayout.Controls.Add(_logBox, 0, 1);
        logCard.Controls.Add(logLayout);

        
        var quick = new GlassCard
        {
            Dock = DockStyle.Fill,
            CornerRadius = Theme.Radii.R18,
            Padding = new Padding(Theme.Spacing.S16)
        };

        var qTitle = new Label
        {
            Text = "Quick Actions",
            Dock = DockStyle.Top,
            Height = 24,
            Font = Theme.Fonts.Ui(11f, FontStyle.Bold),
            ForeColor = Theme.Colors.Text
        };

        var qBody = new Label
        {
            Text = $"Config: {_settingsPath}\nDatabase: bot.db\n\nStatus e logs ficam no Dashboard.\nConfiguracoes ficam em Geral/Economia.\n\nDica: use variaveis de ambiente para o Token.",
            Dock = DockStyle.Fill,
            Font = Theme.Fonts.Ui(9.25f),
            ForeColor = Theme.Colors.Muted
        };

        var qCopy = new AnimatedButton
        {
            Text = "Copiar Caminho Config",
            IconGlyph = FluentGlyphs.Copy,
            Accent = Theme.Colors.Accent2,
            Accent2 = Theme.Colors.Accent
        };
        qCopy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(_settingsPath);
                Log($"Caminho copiado: {_settingsPath}");
            }
            catch (Exception ex)
            {
                Log($"Falha ao copiar: {ex.Message}");
            }
        };

        var qStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        qStack.Controls.Add(qCopy);

        quick.Controls.Add(qStack);
        quick.Controls.Add(qBody);
        quick.Controls.Add(qTitle);

        grid.Controls.Add(header, 0, 0);
        grid.SetColumnSpan(header, 2);
        grid.Controls.Add(logCard, 0, 1);
        grid.Controls.Add(quick, 1, 1);

        root.Controls.Add(grid);
        return root;
    }

    private Panel BuildGeneralPage()
    {
        var root = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new GlassCard { Dock = DockStyle.Fill, CornerRadius = Theme.Radii.R18, Padding = new Padding(Theme.Spacing.S20) };
        var h1 = new Label
        {
            Text = "Geral",
            Dock = DockStyle.Top,
            Height = 28,
            Font = Theme.Fonts.Ui(18f, FontStyle.Bold),
            ForeColor = Theme.Colors.Text
        };
        var h2 = new Label
        {
            Text = "Token, prefixo, dono e aparencia. Mudancas sao persistidas no appsettings usado em runtime.",
            Dock = DockStyle.Top,
            Height = 22,
            Font = Theme.Fonts.Ui(10f),
            ForeColor = Theme.Colors.Muted
        };
        header.Controls.Add(h2);
        header.Controls.Add(h1);
        grid.Controls.Add(header, 0, 0);
        grid.SetColumnSpan(header, 2);

        var formCard = new GlassCard { Dock = DockStyle.Fill, CornerRadius = Theme.Radii.R18, Padding = new Padding(Theme.Spacing.S20) };
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 6,
            AutoSize = true
        };
        fields.RowStyles.Clear();

        _fToken = new TextField { Dock = DockStyle.Top, Label = "Bot Token", Hint = "Recomendado: use DISCORD_BOT_Bot__Token (variavel de ambiente).", UsePasswordChar = true };
        _fPrefix = new TextField { Dock = DockStyle.Top, Label = "Prefixo", Hint = "Ex: *" };
        _fOwnerId = new TextField { Dock = DockStyle.Top, Label = "Owner User ID", Hint = "Discord user id (ulong)" };
        _fStatus = new TextField { Dock = DockStyle.Top, Label = "Status (Atividade)", Hint = "Texto exibido no Discord" };
        _fEmbedColor = new TextField { Dock = DockStyle.Top, Label = "Cor do Embed", Hint = "Nome da cor ou hex (ex: Red ou #ff0000)" };

        fields.Controls.Add(_fToken);
        fields.Controls.Add(_fPrefix);
        fields.Controls.Add(_fOwnerId);
        fields.Controls.Add(_fStatus);
        fields.Controls.Add(_fEmbedColor);

        _btnSave = new AnimatedButton { Text = "Salvar Configuracoes", IconGlyph = FluentGlyphs.Save, Accent = Theme.Colors.Accent2, Accent2 = Theme.Colors.Accent };
        _btnSave.Click += SaveClicked;

        var footer = new BufferedPanel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent };
        _btnSave.Location = new Point(0, 6);
        footer.Controls.Add(_btnSave);

        formCard.Controls.Add(footer);
        formCard.Controls.Add(fields);

        var safetyCard = new GlassCard { Dock = DockStyle.Fill, CornerRadius = Theme.Radii.R18, Padding = new Padding(Theme.Spacing.S20) };
        var sTitle = new Label
        {
            Text = "Seguranca",
            Dock = DockStyle.Top,
            Height = 24,
            Font = Theme.Fonts.Ui(11f, FontStyle.Bold),
            ForeColor = Theme.Colors.Text
        };
        var sBody = new Label
        {
            Text = "Evite armazenar o Token em disco.\nUse variaveis de ambiente:\nDISCORD_BOT_Bot__Token\n\nO arquivo em uso:\n" + _settingsPath,
            Dock = DockStyle.Fill,
            Font = Theme.Fonts.Ui(9.25f),
            ForeColor = Theme.Colors.Muted
        };
        safetyCard.Controls.Add(sBody);
        safetyCard.Controls.Add(sTitle);

        grid.Controls.Add(formCard, 0, 1);
        grid.Controls.Add(safetyCard, 1, 1);

        root.Controls.Add(grid);
        return root;
    }

    private Panel BuildEconomyPage()
    {
        var root = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var header = new GlassCard { Dock = DockStyle.Top, Height = 86, CornerRadius = Theme.Radii.R18, Padding = new Padding(Theme.Spacing.S20) };
        var h1 = new Label
        {
            Text = "Economia",
            Dock = DockStyle.Top,
            Height = 28,
            Font = Theme.Fonts.Ui(18f, FontStyle.Bold),
            ForeColor = Theme.Colors.Text
        };
        var h2 = new Label
        {
            Text = "Recompensas e probabilidades. Ajuste com cuidado.",
            Dock = DockStyle.Top,
            Height = 22,
            Font = Theme.Fonts.Ui(10f),
            ForeColor = Theme.Colors.Muted
        };
        header.Controls.Add(h2);
        header.Controls.Add(h1);

        var card = new GlassCard { Dock = DockStyle.Fill, CornerRadius = Theme.Radii.R18, Padding = new Padding(Theme.Spacing.S20) };

        var flow = new BufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };

        _nDaily = MakeNum("Recompensa diaria", 1, 10000);
        _nWorkMin = MakeNum("Trabalho minimo", 1, 10000);
        _nWorkMax = MakeNum("Trabalho maximo", 1, 10000);
        _nCrimeChance = MakeNum("Chance crime (%)", 0, 100);
        _nCrimeMin = MakeNum("Crime recompensa min", 1, 20000);
        _nCrimeMax = MakeNum("Crime recompensa max", 1, 20000);
        _nCrimeMinFine = MakeNum("Multa crime min", 1, 20000);
        _nCrimeMaxFine = MakeNum("Multa crime max", 1, 20000);

        flow.Controls.Add(_nDaily);
        flow.Controls.Add(_nWorkMin);
        flow.Controls.Add(_nWorkMax);
        flow.Controls.Add(_nCrimeChance);
        flow.Controls.Add(_nCrimeMin);
        flow.Controls.Add(_nCrimeMax);
        flow.Controls.Add(_nCrimeMinFine);
        flow.Controls.Add(_nCrimeMaxFine);

        card.Controls.Add(flow);

        root.Controls.Add(card);
        root.Controls.Add(header);
        root.Padding = new Padding(0);
        return root;
    }

    private static NumberField MakeNum(string label, int min, int max)
    {
        var f = new NumberField { Width = 280, Label = label, Hint = "", Margin = new Padding(0, 0, Theme.Spacing.S16, Theme.Spacing.S16) };
        f.Numeric.Minimum = min;
        f.Numeric.Maximum = max;
        return f;
    }

    private void RedirectConsole()
    {
        var writer = new RichTextBoxWriter(_logBox);
        Console.SetOut(writer);
        Console.SetError(writer);
    }

    private void LoadSettings()
    {
        var root = AppSettingsFile.Load(_settingsPath, out var error);
        if (root == null)
        {
            Log($"Erro ao carregar configuracoes ({_settingsPath}): {(error?.Message ?? "arquivo invalido")}");
            return;
        }

        _fToken.TextBox.Text = root["Bot"]?["Token"]?.ToString() ?? "";
        _fPrefix.TextBox.Text = root["Bot"]?["Prefix"]?.ToString() ?? "";
        _fOwnerId.TextBox.Text = root["Bot"]?["OwnerUserId"]?.ToString() ?? "";
        _fStatus.TextBox.Text = root["Bot"]?["Status"]?.ToString() ?? "";
        _fEmbedColor.TextBox.Text = root["Bot"]?["EmbedColor"]?.ToString() ?? "";

        var eco = root["Economy"];
        if (eco != null)
        {
            SetVal(_nDaily.Numeric, eco["DailyReward"]);
            SetVal(_nWorkMin.Numeric, eco["WorkMinReward"]);
            SetVal(_nWorkMax.Numeric, eco["WorkMaxReward"]);
            SetVal(_nCrimeChance.Numeric, eco["CrimeSuccessChancePercent"]);
            SetVal(_nCrimeMin.Numeric, eco["CrimeMinReward"]);
            SetVal(_nCrimeMax.Numeric, eco["CrimeMaxReward"]);
            SetVal(_nCrimeMinFine.Numeric, eco["CrimeMinFine"]);
            SetVal(_nCrimeMaxFine.Numeric, eco["CrimeMaxFine"]);
        }
    }

    private static void SetVal(NumericUpDown control, JsonNode? node)
    {
        if (node != null && int.TryParse(node.ToString(), out var val))
        {
            control.Value = Math.Max(control.Minimum, Math.Min(control.Maximum, val));
        }
    }

    private void SaveClicked(object? sender, EventArgs e)
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

        
        _fOwnerId.Invalid = !string.IsNullOrWhiteSpace(_fOwnerId.TextBox.Text) && !ulong.TryParse(_fOwnerId.TextBox.Text, out _);

        var bot = GetOrCreateObject(root, "Bot");
        bot["Token"] = _fToken.TextBox.Text;
        bot["Prefix"] = _fPrefix.TextBox.Text;
        if (ulong.TryParse(_fOwnerId.TextBox.Text, out var oid))
        {
            bot["OwnerUserId"] = oid;
        }
        bot["Status"] = _fStatus.TextBox.Text;
        bot["EmbedColor"] = _fEmbedColor.TextBox.Text;

        var eco = GetOrCreateObject(root, "Economy");
        eco["DailyReward"] = (int)_nDaily.Numeric.Value;
        eco["WorkMinReward"] = (int)_nWorkMin.Numeric.Value;
        eco["WorkMaxReward"] = (int)_nWorkMax.Numeric.Value;
        eco["CrimeSuccessChancePercent"] = (int)_nCrimeChance.Numeric.Value;
        eco["CrimeMinReward"] = (int)_nCrimeMin.Numeric.Value;
        eco["CrimeMaxReward"] = (int)_nCrimeMax.Numeric.Value;
        eco["CrimeMinFine"] = (int)_nCrimeMinFine.Numeric.Value;
        eco["CrimeMaxFine"] = (int)_nCrimeMaxFine.Numeric.Value;

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

    private async void StartClicked(object? sender, EventArgs e)
    {
        if (_running) return;

        _btnStart.Enabled = false;
        _btnStop.Enabled = true;
        UpdateStatusPill("Iniciando...", Theme.Colors.Accent2);

        try
        {
            _host = BotHost.Build(_args);
            await BotHost.StartAsync(_host);
            _running = true;
            UpdateStatusPill("Online", Theme.Colors.Ok);
            Log("Bot iniciado com sucesso.");
        }
        catch (Exception ex)
        {
            UpdateStatusPill("Erro", Theme.Colors.Danger);
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            try { _host?.Dispose(); } catch { }
            _host = null;
            Log($"ERRO FATAL: {ex.Message}");
            MessageBox.Show($"Falha ao iniciar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void StopClicked(object? sender, EventArgs e)
    {
        if (!_running || _host == null) return;

        _btnStop.Enabled = false;
        UpdateStatusPill("Parando...", Theme.Colors.Warning);

        try
        {
            await BotHost.StopAsync(_host);
            _host.Dispose();
            _host = null;
            _running = false;

            UpdateStatusPill("Offline", Theme.Colors.Muted);
            _btnStart.Enabled = true;
            Log("Bot parado.");
        }
        catch (Exception ex)
        {
            Log($"Erro ao parar: {ex.Message}");
            _btnStop.Enabled = true;
            UpdateStatusPill("Online", Theme.Colors.Ok);
        }
    }

    private void UpdateStatusPill(string text, Color accent)
    {
        _statusPill.Text = "Status: " + text;
        _statusPill.ForeColor = accent;
        _statusPill.BackColor = Color.FromArgb(40, accent);
        _statusPill.Padding = new Padding(10, 0, 10, 0);
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

    private sealed class RichTextBoxWriter : TextWriter
    {
        private readonly RichTextBox _box;
        private readonly ConcurrentQueue<string> _pending = new();
        private int _flushScheduled;

        public RichTextBoxWriter(RichTextBox box) => _box = box;
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value) => Enqueue(value.ToString());

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Enqueue(value);
            }
        }

        public override void WriteLine(string? value) => Enqueue((value ?? string.Empty) + Environment.NewLine);

        private void Enqueue(string text)
        {
            if (_box.IsDisposed) return;
            _pending.Enqueue(text);
            ScheduleFlush();
        }

        private void ScheduleFlush()
        {
            if (_box.IsDisposed || !_box.IsHandleCreated) return;
            if (Interlocked.Exchange(ref _flushScheduled, 1) == 1) return;

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
            if (_box.IsDisposed) return;

            var sb = new StringBuilder();
            while (_pending.TryDequeue(out var s))
            {
                sb.Append(s);
            }

            if (sb.Length == 0) return;

            _box.AppendText(sb.ToString());
            _box.ScrollToCaret();

            if (!_pending.IsEmpty)
            {
                ScheduleFlush();
            }
        }
    }
}
