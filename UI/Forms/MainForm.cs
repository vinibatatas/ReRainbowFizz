using RedragonFizzMacro.Core.Models;
using RedragonFizzMacro.Core.Services;
using RedragonFizzMacro.Infrastructure.Native;
using RedragonFizzMacro.Infrastructure.Storage;
using RedragonFizzMacro.Infrastructure.SystemIntegration;
using RedragonFizzMacro.UI.Common;
using RedragonFizzMacro.UI.Components;
using RedragonFizzMacro.UI.Keyboard;

namespace RedragonFizzMacro.UI.Forms;

public sealed class MainForm : Form
{
    private readonly AppConfig _config;
    private readonly MacroHookEngine _hookEngine;
    private readonly LayerHudForm _hudForm;
    private readonly FizzKeyboardControl _keyboardControl;
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _flashClearTimer;

    private readonly AppleSegmentedControl _segProfiles;
    private readonly AppleButton _btnNewProfile;
    private readonly AppleToggle _tglEngine;
    private readonly Label _lblStatus;

    private readonly AppleToggle _tglFn2Enabled;
    private readonly AppleStepper _stepFn2Timeout;
    private readonly AppleToggle _tglKeepMediaAlive;
    private readonly AppleToggle _tglShowHud;

    private readonly AppleToggle _tglHoldEnabled;
    private readonly AppleSegmentedControl _segHoldKey;

    private readonly AppleToggle _tglMinimizeToTray;
    private readonly AppleToggle _tglStartWithWindows;

    private readonly Icon _appIcon;
    private readonly Image? _logoImage;
    private bool _allowExit;
    private bool _trayHintShown;
    private bool _suppressUiEvents;

    private static readonly (string ShortLabel, int Vk)[] HoldKeyOptions =
    {
        ("Caps", (int)Keys.Capital),
        ("Ctrl", (int)Keys.RControlKey),
        ("AltGr", (int)Keys.RMenu),
        ("Shift", (int)Keys.RShiftKey)
    };

    public MainForm(AppConfig config, bool startMinimized)
    {
        _config = config;
        _hookEngine = new MacroHookEngine(_config);
        _hudForm = new LayerHudForm();

        _appIcon = LoadApplicationIcon();
        _logoImage = LoadApplicationLogo();

        Text = "ReRainbowFizz";
        ClientSize = new Size(960, 575);
        MinimumSize = new Size(880, 540);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ApplePalette.Background;
        ForeColor = ApplePalette.TextPrimary;
        Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        Icon = _appIcon;

        // Top Toolbar
        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = ApplePalette.Background
        };

        _segProfiles = new AppleSegmentedControl
        {
            Location = new Point(24, 12),
            Size = new Size(48, 24)
        };
        _segProfiles.SelectedIndexChanged += ProfileSelectionChanged;
        _segProfiles.SegmentRightClicked += ProfileRightClicked;

        _btnNewProfile = new AppleButton
        {
            Text = "+",
            Location = new Point(78, 12),
            Size = new Size(24, 24),
            ButtonStyle = AppleButtonStyle.Outline
        };
        _btnNewProfile.Click += CreateNewProfileClicked;

        var btnRestoreDefaults = new AppleButton
        {
            Text = "Restaurar Padrão",
            Size = new Size(106, 24),
            ButtonStyle = AppleButtonStyle.OutlineRed
        };
        btnRestoreDefaults.Click += RestoreDefaultsClicked;

        var btnClearAll = new AppleButton
        {
            Text = "Limpar",
            Size = new Size(58, 24),
            ButtonStyle = AppleButtonStyle.Outline
        };
        btnClearAll.Click += ClearProfileClicked;

        var topDivider = new Panel
        {
            Size = new Size(1, 14),
            BackColor = ApplePalette.Separator
        };

        var lblActive = new Label
        {
            Text = "Ativo",
            AutoSize = true,
            ForeColor = ApplePalette.TextSecondary,
            BackColor = ApplePalette.Background
        };

        _tglEngine = new AppleToggle();
        _tglEngine.CheckedChanged += ToggleEngineChanged;

        var picLogo = new PictureBox
        {
            Size = new Size(24, 24),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Image = _logoImage,
            Cursor = Cursors.Default
        };

        void LayoutTopRight()
        {
            int rightEdge = topBar.ClientSize.Width - 24;
            picLogo.Location = new Point(rightEdge - picLogo.Width, 11);
            _tglEngine.Location = new Point(picLogo.Left - 14 - _tglEngine.Width, 15);
            lblActive.Location = new Point(_tglEngine.Left - 36, 16);
            topDivider.Location = new Point(lblActive.Left - 12, 17);
            btnClearAll.Location = new Point(topDivider.Left - 12 - btnClearAll.Width, 12);
            btnRestoreDefaults.Location = new Point(btnClearAll.Left - 6 - btnRestoreDefaults.Width, 12);
        }

        topBar.Resize += (_, _) => LayoutTopRight();
        topBar.Controls.AddRange(new Control[]
        {
            _segProfiles, _btnNewProfile, btnRestoreDefaults, btnClearAll, topDivider, lblActive, _tglEngine, picLogo
        });
        LayoutTopRight();

        // Bottom Outline Settings Cards Container
        var bottomContainer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 168,
            BackColor = ApplePalette.Background
        };

        var bottomPanel = new TableLayoutPanel
        {
            Height = 166,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = ApplePalette.Background,
            Padding = new Padding(0, 2, 0, 14)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));

        void LayoutBottomPanel()
        {
            const int maxBottomWidth = 1040;
            const int horizontalMargin = 20;
            int avail = bottomContainer.ClientSize.Width - horizontalMargin * 2;
            int targetWidth = Math.Min(avail, maxBottomWidth);
            int left = (bottomContainer.ClientSize.Width - targetWidth) / 2;
            bottomPanel.Location = new Point(Math.Max(horizontalMargin, left), 0);
            bottomPanel.Size = new Size(targetWidth, bottomContainer.ClientSize.Height);
        }

        bottomContainer.Resize += (_, _) => LayoutBottomPanel();
        bottomContainer.Controls.Add(bottomPanel);

        // Card 1: Camada FN2
        var cardFn2 = new AppleCard { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
        _tglFn2Enabled = AddToggleRow(cardFn2, "Camada FN2", 12, SettingsControlChanged);
        AddCaptionRow(cardFn2, "Toque em FN2 e depois na tecla (desativa clique direito).", 33);
        _stepFn2Timeout = AddStepperRow(cardFn2, "Espera", 56, 500, 10000, 250, v => $"{v / 1000.0:0.0}s", SettingsControlChanged);
        _tglKeepMediaAlive = AddToggleRow(cardFn2, "Renovar em mídia", 84, SettingsControlChanged);
        _tglShowHud = AddToggleRow(cardFn2, "HUD na tela", 112, SettingsControlChanged);

        // Card 2: Camada Hold
        var cardHold = new AppleCard { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
        _tglHoldEnabled = AddToggleRow(cardHold, "Camada Hold", 12, SettingsControlChanged);
        AddCaptionRow(cardHold, "Segure a tecla abaixo + outra tecla para ativar a macro.", 33);

        _segHoldKey = new AppleSegmentedControl
        {
            Items = HoldKeyOptions.Select(o => o.ShortLabel).ToArray(),
            Location = new Point(14, 58),
            Size = new Size(200, 24)
        };
        _segHoldKey.SelectedIndexChanged += SettingsControlChanged;
        cardHold.Controls.Add(_segHoldKey);
        cardHold.Resize += (_, _) => _segHoldKey.Width = Math.Max(140, cardHold.ClientSize.Width - 28);

        AddCaptionRow(cardHold, "Toque rápido na tecla mantém a função original.", 92);

        // Card 3: Geral & Status
        var cardSys = new AppleCard { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
        _tglMinimizeToTray = AddToggleRow(cardSys, "Fechar na bandeja", 12, SettingsControlChanged);
        _tglStartWithWindows = AddToggleRow(cardSys, "Iniciar no login", 40, (s, _) =>
        {
            if (!_suppressUiEvents && s is AppleToggle t)
                StartupManager.SetStartupEnabled(t.Checked);
        });

        _lblStatus = new Label
        {
            Text = "Pronto · Clique em uma tecla para configurar",
            AutoSize = false,
            Location = new Point(14, 92),
            Size = new Size(240, 38),
            ForeColor = ApplePalette.TextSecondary,
            BackColor = ApplePalette.Background,
            Font = new Font("Segoe UI", 7.8f, FontStyle.Regular)
        };
        cardSys.Controls.Add(_lblStatus);
        cardSys.Resize += (_, _) => _lblStatus.Width = Math.Max(120, cardSys.ClientSize.Width - 28);

        bottomPanel.Controls.Add(cardFn2, 0, 0);
        bottomPanel.Controls.Add(cardHold, 1, 0);
        bottomPanel.Controls.Add(cardSys, 2, 0);
        LayoutBottomPanel();

        // Keyboard Canvas
        _keyboardControl = new FizzKeyboardControl
        {
            Dock = DockStyle.Fill,
            Config = _config
        };
        _keyboardControl.OnKeyHovered += HandleKeyHovered;
        _keyboardControl.OnKeyClicked += HandleKeyClicked;
        _keyboardControl.OnKeyRightClicked += HandleKeyRightClicked;

        Controls.Add(_keyboardControl);
        Controls.Add(bottomContainer);
        Controls.Add(topBar);

        // System Tray
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Abrir", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add("Ativar / Pausar", null, (_, _) => _tglEngine.Checked = !_tglEngine.Checked);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Sair", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Text = "ReRainbowFizz",
            Icon = _appIcon,
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        _flashClearTimer = new System.Windows.Forms.Timer { Interval = 260 };
        _flashClearTimer.Tick += (_, _) =>
        {
            _flashClearTimer.Stop();
            _keyboardControl.LastTriggeredVk = 0;
            _keyboardControl.Invalidate();
        };

        _hookEngine.OnLayerStateChanged += HandleLayerStateChanged;
        _hookEngine.OnMacroTriggered += HandleMacroTriggered;

        LoadSettingsIntoControls();
        _hookEngine.Start();

        if (startMinimized)
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Load += (_, _) => BeginInvoke(Hide);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MacroEditorForm.EnableDarkTitleBar(Handle);

        const int WM_SETICON = 0x0080;
        const int ICON_SMALL = 0;
        const int ICON_BIG = 1;
        if (_appIcon != null)
        {
            _ = NativeMethods.SendMessage(Handle, WM_SETICON, (IntPtr)ICON_SMALL, _appIcon.Handle);
            _ = NativeMethods.SendMessage(Handle, WM_SETICON, (IntPtr)ICON_BIG, _appIcon.Handle);
        }
    }

    private static void AddCaptionRow(AppleCard card, string text, int y)
    {
        var lbl = new Label
        {
            Text = text,
            AutoSize = false,
            Location = new Point(14, y),
            Size = new Size(260, 18),
            ForeColor = ApplePalette.TextTertiary,
            BackColor = ApplePalette.Background,
            Font = new Font("Segoe UI", 7.4f, FontStyle.Regular)
        };
        card.Controls.Add(lbl);
        card.Resize += (_, _) => lbl.Width = Math.Max(100, card.ClientSize.Width - 28);
    }

    private static AppleToggle AddToggleRow(AppleCard card, string label, int y, EventHandler onChanged)
    {
        var lbl = new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(14, y + 1),
            ForeColor = ApplePalette.TextPrimary,
            BackColor = ApplePalette.Background
        };
        var tgl = new AppleToggle
        {
            Location = new Point(card.ClientSize.Width - 46, y)
        };
        tgl.CheckedChanged += onChanged;
        card.Controls.Add(lbl);
        card.Controls.Add(tgl);
        card.Resize += (_, _) => tgl.Left = card.ClientSize.Width - tgl.Width - 14;
        return tgl;
    }

    private static AppleStepper AddStepperRow(AppleCard card, string label, int y, int min, int max, int inc, Func<int, string> fmt, EventHandler onChanged)
    {
        var lbl = new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(14, y + 2),
            ForeColor = ApplePalette.TextPrimary,
            BackColor = ApplePalette.Background
        };
        var stp = new AppleStepper
        {
            Minimum = min,
            Maximum = max,
            Increment = inc,
            Formatter = fmt,
            Location = new Point(card.ClientSize.Width - 94, y),
            Size = new Size(80, 21)
        };
        stp.ValueChanged += onChanged;
        card.Controls.Add(lbl);
        card.Controls.Add(stp);
        card.Resize += (_, _) => stp.Left = card.ClientSize.Width - stp.Width - 14;
        return stp;
    }

    private void UpdateProfileBarLayout()
    {
        _segProfiles.Items = _config.Profiles.Select((_, i) => $"P{i + 1}").ToArray();
        _segProfiles.Width = _segProfiles.CalculateResponsiveWidth(42);
        _segProfiles.SelectedIndex = Math.Clamp(_config.ActiveProfileIndex, 0, _config.Profiles.Count - 1);
        _btnNewProfile.Left = _segProfiles.Right + 6;
        _btnNewProfile.Visible = _config.Profiles.Count < 6;
    }

    private void LoadSettingsIntoControls()
    {
        _suppressUiEvents = true;
        try
        {
            UpdateProfileBarLayout();

            _tglEngine.Checked = _config.EngineEnabled;
            _tglFn2Enabled.Checked = _config.Fn2OneShotEnabled;
            _stepFn2Timeout.Value = Math.Clamp(_config.Fn2TimeoutMs, 500, 10000);
            _tglKeepMediaAlive.Checked = _config.KeepFn2AliveOnMedia;
            _tglShowHud.Checked = _config.ShowHudOverlay;

            _tglHoldEnabled.Checked = _config.HoldLayerEnabled;
            int holdIdx = Array.FindIndex(HoldKeyOptions, x => x.Vk == _config.HoldLayerVkCode);
            _segHoldKey.SelectedIndex = holdIdx >= 0 ? holdIdx : 0;

            _tglMinimizeToTray.Checked = _config.MinimizeToTrayOnClose;
            _tglStartWithWindows.Checked = StartupManager.IsStartupEnabled();

            _keyboardControl.Invalidate();
        }
        finally
        {
            _suppressUiEvents = false;
        }
    }

    private void SettingsControlChanged(object? sender, EventArgs e)
    {
        if (_suppressUiEvents) return;

        _config.Fn2OneShotEnabled = _tglFn2Enabled.Checked;
        _config.Fn2TimeoutMs = _stepFn2Timeout.Value;
        _config.KeepFn2AliveOnMedia = _tglKeepMediaAlive.Checked;
        _config.ShowHudOverlay = _tglShowHud.Checked;

        _config.HoldLayerEnabled = _tglHoldEnabled.Checked;
        if (_segHoldKey.SelectedIndex >= 0 && _segHoldKey.SelectedIndex < HoldKeyOptions.Length)
            _config.HoldLayerVkCode = HoldKeyOptions[_segHoldKey.SelectedIndex].Vk;

        _config.MinimizeToTrayOnClose = _tglMinimizeToTray.Checked;

        if (!_config.ShowHudOverlay)
            _hudForm.HideHud();

        ConfigService.Save(_config);
        _keyboardControl.Invalidate();
    }

    private void ProfileSelectionChanged(object? sender, EventArgs e)
    {
        if (_suppressUiEvents) return;
        _config.ActiveProfileIndex = _segProfiles.SelectedIndex;
        ConfigService.Save(_config);
        _keyboardControl.Invalidate();
    }

    private void CreateNewProfileClicked(object? sender, EventArgs e)
    {
        if (_config.Profiles.Count >= 6) return;
        string name = $"P{_config.Profiles.Count + 1}";
        _config.Profiles.Add(new MacroProfile { Name = name });
        _config.ActiveProfileIndex = _config.Profiles.Count - 1;
        ConfigService.Save(_config);
        LoadSettingsIntoControls();
    }

    private void ProfileRightClicked(int index)
    {
        if (_config.Profiles.Count <= 1 || index < 0 || index >= _config.Profiles.Count)
            return;

        _config.Profiles.RemoveAt(index);
        for (int i = 0; i < _config.Profiles.Count; i++)
            _config.Profiles[i].Name = $"P{i + 1}";

        _config.ActiveProfileIndex = Math.Clamp(_config.ActiveProfileIndex, 0, _config.Profiles.Count - 1);
        ConfigService.Save(_config);
        LoadSettingsIntoControls();
    }

    private void RestoreDefaultsClicked(object? sender, EventArgs e)
    {
        _hookEngine.DisarmAll();
        _config.ResetToDefaults();
        ConfigService.Save(_config);
        LoadSettingsIntoControls();
        _lblStatus.ForeColor = ApplePalette.RedSoftText;
        _lblStatus.Text = "Configurações restauradas para o padrão";
    }

    private void ClearProfileClicked(object? sender, EventArgs e)
    {
        var active = _config.GetActiveProfile();
        if (active.Bindings.Count == 0) return;
        active.Bindings.Clear();
        ConfigService.Save(_config);
        _keyboardControl.Invalidate();
        _lblStatus.Text = "Macros limpas";
    }

    private void ToggleEngineChanged(object? sender, EventArgs e)
    {
        if (_suppressUiEvents) return;
        _config.EngineEnabled = _tglEngine.Checked;
        if (!_config.EngineEnabled)
            _hookEngine.DisarmAll();

        ConfigService.Save(_config);
    }

    private void HandleKeyHovered(KeycapDef? key)
    {
        if (key == null)
        {
            _lblStatus.ForeColor = ApplePalette.TextSecondary;
            _lblStatus.Text = "Pronto · Clique em uma tecla para configurar";
            return;
        }

        if (key.IsHardwareFn1)
        {
            _lblStatus.ForeColor = ApplePalette.TextSecondary;
            _lblStatus.Text = "FN1 · Camada interna de hardware";
            return;
        }

        if (key.IsFn2Key)
        {
            _lblStatus.ForeColor = ApplePalette.RedSoftText;
            _lblStatus.Text = "FN2 · Ativador exclusivo de macros (menu direito bloqueado)";
            return;
        }

        string kName = string.IsNullOrEmpty(key.TopLegend) ? "Espaço" : key.TopLegend;
        var profile = _config.GetActiveProfile();
        if (profile.Bindings.TryGetValue(key.VkCode, out var binding))
        {
            _lblStatus.ForeColor = ApplePalette.RedSoftText;
            _lblStatus.Text = $"{kName} · {binding.GetDisplayDescription()}";
        }
        else
        {
            _lblStatus.ForeColor = ApplePalette.TextSecondary;
            _lblStatus.Text = $"{kName} · Sem macro";
        }
    }

    private void HandleKeyClicked(KeycapDef key)
    {
        if (key.IsHardwareFn1 || key.IsFn2Key)
            return;

        _hookEngine.DisarmAll();
        var profile = _config.GetActiveProfile();
        profile.Bindings.TryGetValue(key.VkCode, out MacroBinding? existing);

        using var editor = new MacroEditorForm(key, existing, _hookEngine);
        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            if (editor.RemoveRequested)
                profile.Bindings.Remove(key.VkCode);
            else if (editor.ResultBinding != null)
                profile.Bindings[key.VkCode] = editor.ResultBinding;

            ConfigService.Save(_config);
            _keyboardControl.Invalidate();
            HandleKeyHovered(key);
        }
    }

    private void HandleKeyRightClicked(KeycapDef key)
    {
        if (key.VkCode == 0 || key.IsFn2Key) return;
        var profile = _config.GetActiveProfile();
        if (profile.Bindings.Remove(key.VkCode))
        {
            ConfigService.Save(_config);
            _keyboardControl.Invalidate();
            HandleKeyHovered(key);
        }
    }

    private void HandleLayerStateChanged(bool active, string label, int durationMs)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => HandleLayerStateChanged(active, label, durationMs));
            return;
        }

        _keyboardControl.IsLayerCurrentlyActive = active;
        _keyboardControl.Invalidate();

        if (active && _config.ShowHudOverlay)
            _hudForm.ShowHud(label, durationMs);
        else
            _hudForm.HideHud();
    }

    private void HandleMacroTriggered(string statusText, MacroBinding? binding)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => HandleMacroTriggered(statusText, binding));
            return;
        }

        _lblStatus.ForeColor = ApplePalette.RedSoftText;
        _lblStatus.Text = statusText;
        if (binding != null)
        {
            _keyboardControl.LastTriggeredVk = binding.TriggerVkCode;
            _keyboardControl.Invalidate();
            _flashClearTimer.Stop();
            _flashClearTimer.Start();
        }
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _trayIcon.Visible = false;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing && _config.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;

            if (!_trayHintShown)
            {
                _trayHintShown = true;
                _trayIcon.ShowBalloonTip(1800, "ReRainbowFizz", "Rodando em segundo plano.", ToolTipIcon.None);
            }
            return;
        }

        _hudForm.HideHud();
        _hudForm.Dispose();
        _hookEngine.Dispose();
        _trayIcon.Dispose();
        _appIcon.Dispose();
        _logoImage?.Dispose();
        base.OnFormClosing(e);
    }

    private static Icon LoadApplicationIcon()
    {
        string icoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app_icon.ico");
        if (File.Exists(icoPath))
        {
            try
            {
                return new Icon(icoPath);
            }
            catch { }
        }

        try
        {
            string? proc = Environment.ProcessPath ?? Application.ExecutablePath;
            if (!string.IsNullOrEmpty(proc) && File.Exists(proc))
            {
                var exeIcon = Icon.ExtractAssociatedIcon(proc);
                if (exeIcon != null) return exeIcon;
            }
        }
        catch { }

        // Fallback procedural icon
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(ApplePalette.Red);
            g.FillEllipse(brush, 2, 2, 28, 28);
        }
        IntPtr h = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(h);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(h);
        }
    }

    private static Image? LoadApplicationLogo()
    {
        string pngPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app_icon.png");
        if (File.Exists(pngPath))
        {
            try
            {
                return Image.FromFile(pngPath);
            }
            catch { }
        }
        return null;
    }
}
