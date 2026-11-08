using RedragonFizzMacro.Core.Enums;
using RedragonFizzMacro.Core.Models;
using RedragonFizzMacro.Core.Services;
using RedragonFizzMacro.Infrastructure.Native;
using RedragonFizzMacro.UI.Common;
using RedragonFizzMacro.UI.Components;
using RedragonFizzMacro.UI.Keyboard;

namespace RedragonFizzMacro.UI.Forms;

public sealed class MacroEditorForm : Form
{
    public MacroBinding? ResultBinding { get; private set; }
    public bool RemoveRequested { get; private set; }

    private readonly KeycapDef _key;
    private readonly MacroHookEngine _hookEngine;
    private MediaCommand _selectedMedia = MediaCommand.PlayPause;
    private int _capturedComboVk;
    private bool _isCapturingKey;

    // Discord custom shortcut state
    private bool _discordCtrl = true;
    private bool _discordShift = true;
    private bool _discordAlt;
    private bool _discordWin;
    private int _discordVk = (int)Keys.M;
    private bool _isCapturingDiscord;

    private readonly AppleSegmentedControl _segCategory;
    private readonly Panel[] _tabPanels = new Panel[4];
    private readonly Dictionary<MediaCommand, AppleButton> _mediaButtons = new();

    private readonly Panel _volStepRow;
    private readonly AppleStepper _stepVolume;
    private readonly Panel _discordRow;
    private readonly AppleButton _btnDiscordShortcut;

    private readonly AppleButton _modCtrl, _modShift, _modAlt, _modWin;
    private readonly AppleButton _btnCaptureKey;

    private readonly TextBox _txtTargetPath, _txtArguments;
    private readonly TextBox _txtSnippet;
    private readonly AppleToggle _tglEnterAfter;
    private readonly TextBox _txtCustomName;

    public MacroEditorForm(KeycapDef key, MacroBinding? existing, MacroHookEngine hookEngine)
    {
        _key = key;
        _hookEngine = hookEngine;

        Text = string.IsNullOrEmpty(key.TopLegend) ? "Espaço" : key.TopLegend;
        ClientSize = new Size(468, 356);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ApplePalette.Background;
        ForeColor = ApplePalette.TextPrimary;
        Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);

        // Top Bar: Key Pill + Segmented Control
        var keyPill = new AppleButton
        {
            Text = string.IsNullOrEmpty(key.TopLegend) ? "Space" : key.TopLegend,
            ButtonStyle = AppleButtonStyle.OutlineRed,
            Location = new Point(18, 14),
            Size = new Size(56, 24)
        };

        _segCategory = new AppleSegmentedControl
        {
            Items = new[] { "Áudio & Mídia", "Atalho", "Abrir", "Texto" },
            Location = new Point(82, 14),
            Size = new Size(368, 24)
        };
        _segCategory.SelectedIndexChanged += (_, _) => SwitchTab(_segCategory.SelectedIndex);

        // Main Outline Card
        var card = new AppleCard
        {
            Location = new Point(18, 48),
            Size = new Size(432, 212),
            Padding = new Padding(12)
        };

        // Tab 0: Media, Volume, Mic Windows & Discord
        var pMedia = new Panel { Dock = DockStyle.Fill, BackColor = ApplePalette.Background };
        (MediaCommand cmd, string label)[] mediaDefs =
        {
            (MediaCommand.PlayPause, "Play / Pause"),
            (MediaCommand.PrevTrack, "Anterior"),
            (MediaCommand.NextTrack, "Próxima"),
            (MediaCommand.VolumeDown, "Volume −"),
            (MediaCommand.VolumeUp, "Volume +"),
            (MediaCommand.VolumeMute, "Mute Som"),
            (MediaCommand.MicMute, "Mic Windows"),
            (MediaCommand.DiscordMute, "Mute Discord"),
            (MediaCommand.StopMedia, "Parar Mídia")
        };

        for (int i = 0; i < mediaDefs.Length; i++)
        {
            var item = mediaDefs[i];
            int col = i % 3;
            int row = i / 3;
            var btn = new AppleButton
            {
                Text = item.label,
                Size = new Size(130, 32),
                Location = new Point(4 + col * 138, 4 + row * 40)
            };
            btn.Click += (_, _) => SelectMedia(item.cmd);
            _mediaButtons[item.cmd] = btn;
            pMedia.Controls.Add(btn);
        }

        _volStepRow = new Panel
        {
            Location = new Point(4, 134),
            Size = new Size(400, 36),
            BackColor = ApplePalette.Background
        };
        var lblStep = new Label
        {
            Text = "Passo do volume por toque",
            AutoSize = true,
            Location = new Point(2, 9),
            ForeColor = ApplePalette.TextSecondary,
            BackColor = ApplePalette.Background
        };
        _stepVolume = new AppleStepper
        {
            Minimum = 1,
            Maximum = 15,
            Value = 2,
            Formatter = v => $"{v * 2}%",
            Location = new Point(316, 6),
            Size = new Size(84, 22)
        };
        _volStepRow.Controls.Add(lblStep);
        _volStepRow.Controls.Add(_stepVolume);

        _discordRow = new Panel
        {
            Location = new Point(4, 130),
            Size = new Size(400, 52),
            BackColor = ApplePalette.Background,
            Visible = false
        };
        var lblDiscordHint = new Label
        {
            Text = "Atalho enviado ao Discord (clique para gravar):",
            AutoSize = true,
            Location = new Point(2, 2),
            ForeColor = ApplePalette.TextSecondary,
            Font = new Font("Segoe UI", 7.6f)
        };
        _btnDiscordShortcut = new AppleButton
        {
            Text = "Ctrl + Shift + M",
            Location = new Point(2, 22),
            Size = new Size(398, 26),
            ButtonStyle = AppleButtonStyle.Outline
        };
        _btnDiscordShortcut.Click += StartDiscordCapture;
        _discordRow.Controls.Add(lblDiscordHint);
        _discordRow.Controls.Add(_btnDiscordShortcut);

        pMedia.Controls.Add(_volStepRow);
        pMedia.Controls.Add(_discordRow);
        _tabPanels[0] = pMedia;

        // Tab 1: Key Combo
        var pCombo = new Panel { Dock = DockStyle.Fill, BackColor = ApplePalette.Background, Visible = false };
        _modCtrl = CreateTogglePill("Ctrl", 4, 8, 94);
        _modShift = CreateTogglePill("Shift", 106, 8, 94);
        _modAlt = CreateTogglePill("Alt", 208, 8, 94);
        _modWin = CreateTogglePill("Win", 310, 8, 94);

        _btnCaptureKey = new AppleButton
        {
            Text = "Gravar tecla...",
            Location = new Point(4, 50),
            Size = new Size(400, 32)
        };
        _btnCaptureKey.Click += StartKeyCapture;

        AddComboPreset(pCombo, "Captura", 4, 102, false, true, false, true, (int)Keys.S);
        AddComboPreset(pCombo, "Desktop", 106, 102, false, false, false, true, (int)Keys.D);
        AddComboPreset(pCombo, "Tarefas", 208, 102, true, true, false, false, (int)Keys.Escape);
        AddComboPreset(pCombo, "Bloquear", 310, 102, false, false, false, true, (int)Keys.L);

        pCombo.Controls.AddRange(new Control[] { _modCtrl, _modShift, _modAlt, _modWin, _btnCaptureKey });
        _tabPanels[1] = pCombo;

        // Tab 2: Launch App / URL
        var pApp = new Panel { Dock = DockStyle.Fill, BackColor = ApplePalette.Background, Visible = false };
        _txtTargetPath = CreateDarkTextBox(4, 10, 344, "Aplicativo, pasta ou https://...");
        var btnBrowse = new AppleButton
        {
            Text = "...",
            Location = new Point(356, 8),
            Size = new Size(48, 24)
        };
        btnBrowse.Click += BrowseFile;

        _txtArguments = CreateDarkTextBox(4, 48, 400, "Argumentos (opcional)");

        AddAppPreset(pApp, "Calc", 4, 96, "calc.exe");
        AddAppPreset(pApp, "Arquivos", 106, 96, "explorer.exe");
        AddAppPreset(pApp, "Notas", 208, 96, "notepad.exe");
        AddAppPreset(pApp, "Mixer", 310, 96, "sndvol.exe");

        pApp.Controls.AddRange(new Control[] { _txtTargetPath, btnBrowse, _txtArguments });
        _tabPanels[2] = pApp;

        // Tab 3: Type Text
        var pText = new Panel { Dock = DockStyle.Fill, BackColor = ApplePalette.Background, Visible = false };
        _txtSnippet = new TextBox
        {
            Location = new Point(4, 6),
            Size = new Size(400, 128),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = ApplePalette.Surface,
            ForeColor = ApplePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Texto a digitar..."
        };
        var lblEnt = new Label
        {
            Text = "Enviar Enter ao final",
            AutoSize = true,
            Location = new Point(4, 146),
            ForeColor = ApplePalette.TextSecondary,
            BackColor = ApplePalette.Background
        };
        _tglEnterAfter = new AppleToggle { Location = new Point(372, 144) };
        pText.Controls.AddRange(new Control[] { _txtSnippet, lblEnt, _tglEnterAfter });
        _tabPanels[3] = pText;

        foreach (var tp in _tabPanels)
            card.Controls.Add(tp);

        _txtCustomName = CreateDarkTextBox(18, 272, 432, "Rótulo curto na tecla (opcional)");

        if (existing != null)
        {
            var btnRemove = new AppleButton
            {
                Text = "Remover",
                ButtonStyle = AppleButtonStyle.OutlineRed,
                Location = new Point(18, 312),
                Size = new Size(80, 26)
            };
            btnRemove.Click += (_, _) =>
            {
                RemoveRequested = true;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btnRemove);
        }

        var btnCancel = new AppleButton
        {
            Text = "Cancelar",
            ButtonStyle = AppleButtonStyle.Outline,
            Location = new Point(282, 312),
            Size = new Size(80, 26)
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnSave = new AppleButton
        {
            Text = "Salvar",
            ButtonStyle = AppleButtonStyle.PrimaryRed,
            Location = new Point(370, 312),
            Size = new Size(80, 26)
        };
        btnSave.Click += SaveClicked;

        Controls.AddRange(new Control[] { keyPill, _segCategory, card, _txtCustomName, btnCancel, btnSave });

        if (existing != null)
        {
            _txtCustomName.Text = existing.Name;
            _stepVolume.Value = Math.Clamp(existing.VolumeSteps, 1, 15);
            _modCtrl.Selected = existing.ModCtrl;
            _modShift.Selected = existing.ModShift;
            _modAlt.Selected = existing.ModAlt;
            _modWin.Selected = existing.ModWin;
            _capturedComboVk = existing.ComboVkCode;

            if (existing.MediaCmd == MediaCommand.DiscordMute &&
                (existing.ComboVkCode != 0 || existing.ModCtrl || existing.ModShift || existing.ModAlt))
            {
                _discordCtrl = existing.ModCtrl;
                _discordShift = existing.ModShift;
                _discordAlt = existing.ModAlt;
                _discordWin = existing.ModWin;
                _discordVk = existing.ComboVkCode;
            }

            UpdateCaptureLabel();
            UpdateDiscordButtonLabel();
            _txtTargetPath.Text = existing.TargetPath;
            _txtArguments.Text = existing.Arguments;
            _txtSnippet.Text = existing.TextSnippet;
            _tglEnterAfter.Checked = existing.PressEnterAfterText;
            SelectMedia(existing.MediaCmd);
            _segCategory.SelectedIndex = (int)existing.ActionType;
            SwitchTab((int)existing.ActionType);
        }
        else
        {
            SelectMedia(MediaCommand.PlayPause);
            SwitchTab(0);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        EnableDarkTitleBar(Handle);

        if (Owner?.Icon != null)
        {
            Icon = Owner.Icon;
            _ = NativeMethods.SendMessage(Handle, 0x0080, (IntPtr)0, Owner.Icon.Handle);
            _ = NativeMethods.SendMessage(Handle, 0x0080, (IntPtr)1, Owner.Icon.Handle);
        }
    }

    private void SwitchTab(int idx)
    {
        StopKeyCapture();
        for (int i = 0; i < 4; i++)
            _tabPanels[i].Visible = i == idx;
    }

    private void SelectMedia(MediaCommand cmd)
    {
        StopKeyCapture();
        _selectedMedia = cmd;
        foreach (var kv in _mediaButtons)
            kv.Value.Selected = kv.Key == cmd;
        _volStepRow.Visible = cmd is MediaCommand.VolumeUp or MediaCommand.VolumeDown;
        _discordRow.Visible = cmd == MediaCommand.DiscordMute;
    }

    private void StartDiscordCapture(object? sender, EventArgs e)
    {
        if (_isCapturingDiscord)
        {
            StopKeyCapture();
            return;
        }
        _isCapturingDiscord = true;
        _btnDiscordShortcut.Selected = true;
        _btnDiscordShortcut.Text = "Segure modificadores (ex: Ctrl+Shift) e aperte a tecla...";

        _hookEngine.KeyCaptureOverride = vk =>
        {
            if (vk is 0x10 or 0x11 or 0x12 or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0x5B or 0x5C)
                return true;

            bool c = (Control.ModifierKeys & Keys.Control) != 0;
            bool s = (Control.ModifierKeys & Keys.Shift) != 0;
            bool a = (Control.ModifierKeys & Keys.Alt) != 0;

            BeginInvoke(() =>
            {
                _discordCtrl = c;
                _discordShift = s;
                _discordAlt = a;
                _discordWin = false;
                _discordVk = vk;
                StopKeyCapture();
                UpdateDiscordButtonLabel();
            });
            return true;
        };
    }

    private void UpdateDiscordButtonLabel()
    {
        var parts = new List<string>();
        if (_discordCtrl) parts.Add("Ctrl");
        if (_discordShift) parts.Add("Shift");
        if (_discordAlt) parts.Add("Alt");
        if (_discordWin) parts.Add("Win");
        if (_discordVk != 0) parts.Add(MacroHookEngine.FormatKeyShort(_discordVk));
        _btnDiscordShortcut.Text = parts.Count > 0 ? string.Join(" + ", parts) : "Ctrl + Shift + M";
    }

    private void StartKeyCapture(object? sender, EventArgs e)
    {
        if (_isCapturingKey)
        {
            StopKeyCapture();
            return;
        }
        _isCapturingKey = true;
        _btnCaptureKey.Selected = true;
        _btnCaptureKey.Text = "Pressione uma tecla...";

        _hookEngine.KeyCaptureOverride = vk =>
        {
            if (vk is 0x10 or 0x11 or 0x12 or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0x5B or 0x5C)
                return true;

            BeginInvoke(() =>
            {
                _capturedComboVk = vk;
                StopKeyCapture();
                UpdateCaptureLabel();
            });
            return true;
        };
    }

    private void StopKeyCapture()
    {
        _isCapturingKey = false;
        _isCapturingDiscord = false;
        _hookEngine.KeyCaptureOverride = null;
        _btnCaptureKey.Selected = false;
        _btnDiscordShortcut.Selected = false;
        UpdateCaptureLabel();
        UpdateDiscordButtonLabel();
    }

    private void UpdateCaptureLabel()
    {
        _btnCaptureKey.Text = _capturedComboVk == 0
            ? "Gravar tecla..."
            : $"Tecla: {MacroHookEngine.FormatKeyShort(_capturedComboVk)}";
    }

    private static AppleButton CreateTogglePill(string text, int x, int y, int w)
    {
        var b = new AppleButton { Text = text, Location = new Point(x, y), Size = new Size(w, 28) };
        b.Click += (_, _) => b.Selected = !b.Selected;
        return b;
    }

    private void AddComboPreset(Panel parent, string label, int x, int y, bool c, bool s, bool a, bool w, int vk)
    {
        var b = new AppleButton { Text = label, Location = new Point(x, y), Size = new Size(94, 26), ButtonStyle = AppleButtonStyle.Outline };
        b.Click += (_, _) =>
        {
            _modCtrl.Selected = c;
            _modShift.Selected = s;
            _modAlt.Selected = a;
            _modWin.Selected = w;
            _capturedComboVk = vk;
            _txtCustomName.Text = label;
            UpdateCaptureLabel();
        };
        parent.Controls.Add(b);
    }

    private void AddAppPreset(Panel parent, string label, int x, int y, string target)
    {
        var b = new AppleButton { Text = label, Location = new Point(x, y), Size = new Size(94, 26), ButtonStyle = AppleButtonStyle.Outline };
        b.Click += (_, _) =>
        {
            _txtTargetPath.Text = target;
            _txtCustomName.Text = label;
        };
        parent.Controls.Add(b);
    }

    private static TextBox CreateDarkTextBox(int x, int y, int w, string placeholder) => new()
    {
        Location = new Point(x, y),
        Width = w,
        BackColor = ApplePalette.Surface,
        ForeColor = ApplePalette.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
        PlaceholderText = placeholder
    };

    private void BrowseFile(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Executáveis (*.exe;*.bat;*.cmd;*.ps1)|*.exe;*.bat;*.cmd;*.ps1|Todos (*.*)|*.*"
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _txtTargetPath.Text = ofd.FileName;
            if (string.IsNullOrWhiteSpace(_txtCustomName.Text))
                _txtCustomName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
        }
    }

    private void SaveClicked(object? sender, EventArgs e)
    {
        var type = (MacroActionType)_segCategory.SelectedIndex;

        if (type == MacroActionType.KeyCombo && _capturedComboVk == 0 && !_modCtrl.Selected && !_modShift.Selected && !_modAlt.Selected && !_modWin.Selected)
            return;
        if (type == MacroActionType.LaunchAppOrUrl && string.IsNullOrWhiteSpace(_txtTargetPath.Text))
            return;
        if (type == MacroActionType.TypeText && string.IsNullOrEmpty(_txtSnippet.Text) && !_tglEnterAfter.Checked)
            return;

        bool isDiscord = type == MacroActionType.MediaVolume && _selectedMedia == MediaCommand.DiscordMute;

        ResultBinding = new MacroBinding
        {
            TriggerVkCode = _key.VkCode,
            Name = _txtCustomName.Text.Trim(),
            ActionType = type,
            MediaCmd = _selectedMedia,
            VolumeSteps = _stepVolume.Value,
            ModCtrl = isDiscord ? _discordCtrl : _modCtrl.Selected,
            ModShift = isDiscord ? _discordShift : _modShift.Selected,
            ModAlt = isDiscord ? _discordAlt : _modAlt.Selected,
            ModWin = isDiscord ? _discordWin : _modWin.Selected,
            ComboVkCode = isDiscord ? _discordVk : _capturedComboVk,
            TargetPath = _txtTargetPath.Text.Trim(),
            Arguments = _txtArguments.Text.Trim(),
            TextSnippet = _txtSnippet.Text,
            PressEnterAfterText = _tglEnterAfter.Checked
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopKeyCapture();
        base.OnFormClosed(e);
    }

    public static void EnableDarkTitleBar(IntPtr hwnd)
    {
        int useDark = 1;
        _ = NativeMethods.DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int));
    }
}
