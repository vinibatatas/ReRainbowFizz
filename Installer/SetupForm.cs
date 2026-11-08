using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RedragonFizzMacro.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm());
    }
}

internal sealed class SetupForm : Form
{
    private static readonly Color BgColor = Color.FromArgb(17, 17, 19);
    private static readonly Color TextColor = Color.FromArgb(242, 242, 247);
    private static readonly Color SubTextColor = Color.FromArgb(142, 142, 147);
    private static readonly Color RedColor = Color.FromArgb(255, 69, 58);
    private static readonly Color BorderColor = Color.FromArgb(46, 46, 50);

    private readonly string _defaultInstallDir;
    private readonly TextBox _txtPath;
    private readonly CheckBox _chkDesktop;
    private readonly CheckBox _chkStartWithWindows;
    private readonly Button _btnInstall;
    private readonly Label _lblStatus;
    private readonly ProgressBar _progressBar;

    public SetupForm()
    {
        _defaultInstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "ReRainbowFizz");

        Text = "Instalador — ReRainbowFizz";
        ClientSize = new Size(520, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 8.5f);

        string icoPath = Path.Combine(AppContext.BaseDirectory, "app_icon.ico");
        if (File.Exists(icoPath))
        {
            try { Icon = new Icon(icoPath); } catch { }
        }

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = BgColor,
            Padding = new Padding(24, 16, 24, 8)
        };

        var picBox = new PictureBox
        {
            Size = new Size(48, 48),
            Location = new Point(24, 16),
            SizeMode = PictureBoxSizeMode.Zoom
        };

        string pngIconPath = Path.Combine(AppContext.BaseDirectory, "..", "Resources", "app_icon.png");
        if (!File.Exists(pngIconPath))
            pngIconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app_icon.png");

        if (File.Exists(pngIconPath))
        {
            try { picBox.Image = Image.FromFile(pngIconPath); } catch { }
        }

        var lblTitle = new Label
        {
            Text = "ReRainbowFizz",
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            ForeColor = RedColor,
            AutoSize = true,
            Location = new Point(84, 16)
        };

        var lblSubtitle = new Label
        {
            Text = "Instalação do utilitário de macros para Redragon Fizz Rainbow (K617-R)",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = SubTextColor,
            AutoSize = true,
            Location = new Point(85, 44)
        };

        pnlHeader.Controls.AddRange(new Control[] { picBox, lblTitle, lblSubtitle });

        // Content
        var lblDir = new Label
        {
            Text = "Pasta de destino:",
            Location = new Point(24, 96),
            AutoSize = true,
            ForeColor = TextColor
        };

        _txtPath = new TextBox
        {
            Text = _defaultInstallDir,
            Location = new Point(24, 120),
            Width = 370,
            BackColor = Color.FromArgb(26, 26, 29),
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnBrowse = new Button
        {
            Text = "Procurar...",
            Location = new Point(404, 118),
            Size = new Size(92, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(35, 35, 39),
            ForeColor = TextColor
        };
        btnBrowse.FlatAppearance.BorderColor = BorderColor;
        btnBrowse.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog { SelectedPath = _txtPath.Text };
            if (fbd.ShowDialog(this) == DialogResult.OK)
                _txtPath.Text = fbd.SelectedPath;
        };

        _chkDesktop = new CheckBox
        {
            Text = "Criar atalho na Área de Trabalho",
            Checked = true,
            Location = new Point(24, 164),
            AutoSize = true,
            ForeColor = TextColor
        };

        _chkStartWithWindows = new CheckBox
        {
            Text = "Iniciar automaticamente com o Windows",
            Checked = true,
            Location = new Point(24, 194),
            AutoSize = true,
            ForeColor = TextColor
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(24, 236),
            Size = new Size(472, 8),
            Visible = false
        };

        _lblStatus = new Label
        {
            Text = "Pronto para instalar.",
            Location = new Point(24, 252),
            Size = new Size(472, 24),
            ForeColor = SubTextColor
        };

        // Footer
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = BgColor
        };

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Location = new Point(292, 14),
            Size = new Size(96, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(26, 26, 29),
            ForeColor = TextColor
        };
        btnCancel.FlatAppearance.BorderColor = BorderColor;
        btnCancel.Click += (_, _) => Close();

        _btnInstall = new Button
        {
            Text = "Instalar",
            Location = new Point(400, 14),
            Size = new Size(96, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = RedColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };
        _btnInstall.FlatAppearance.BorderSize = 0;
        _btnInstall.Click += DoInstall;

        pnlFooter.Controls.AddRange(new Control[] { btnCancel, _btnInstall });

        Controls.AddRange(new Control[]
        {
            pnlHeader, lblDir, _txtPath, btnBrowse, _chkDesktop, _chkStartWithWindows,
            _progressBar, _lblStatus, pnlFooter
        });
    }

    private async void DoInstall(object? sender, EventArgs e)
    {
        string targetDir = _txtPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(targetDir))
        {
            MessageBox.Show(this, "Selecione uma pasta válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnInstall.Enabled = false;
        _progressBar.Visible = true;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _lblStatus.Text = "Instalando arquivos...";

        try
        {
            await Task.Run(() =>
            {
                // Stop running instance if any
                foreach (var proc in Process.GetProcessesByName("ReRainbowFizz").Concat(Process.GetProcessesByName("RedragonFizzMacro")))
                {
                    try { proc.Kill(); proc.WaitForExit(3000); } catch { }
                }

                Directory.CreateDirectory(targetDir);

                // Extract embedded payload.zip
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                    .First(n => n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));

                using var stream = assembly.GetManifestResourceStream(resourceName)!;
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(targetDir, overwriteFiles: true);

                string installedExe = Path.Combine(targetDir, "ReRainbowFizz.exe");

                // Start with Windows option
                if (_chkStartWithWindows.Checked)
                {
                    using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                    runKey?.SetValue("ReRainbowFizz", $"\"{installedExe}\" --minimized");
                }

                // Desktop shortcut
                if (_chkDesktop.Checked)
                {
                    string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string shortcutPath = Path.Combine(desktopDir, "ReRainbowFizz.lnk");
                    CreateWindowsShortcut(shortcutPath, installedExe, targetDir);
                }

                // Start Menu shortcut
                string startMenuDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "ReRainbowFizz");
                Directory.CreateDirectory(startMenuDir);
                string startMenuShortcut = Path.Combine(startMenuDir, "ReRainbowFizz.lnk");
                CreateWindowsShortcut(startMenuShortcut, installedExe, targetDir);

                string uninstallerExe = Path.Combine(targetDir, "Desinstalador_ReRainbowFizz.exe");
                if (File.Exists(uninstallerExe))
                {
                    string startMenuUninstall = Path.Combine(startMenuDir, "Desinstalar ReRainbowFizz.lnk");
                    CreateWindowsShortcut(startMenuUninstall, uninstallerExe, targetDir);
                }

                // Add to Windows Add/Remove Programs
                RegisterUninstaller(targetDir, installedExe);
            });

            _lblStatus.ForeColor = Color.FromArgb(52, 199, 89);
            _lblStatus.Text = "Instalação concluída com sucesso!";
            _progressBar.Visible = false;

            var dr = MessageBox.Show(
                this,
                "O ReRainbowFizz foi instalado com sucesso!\r\n\r\nDeseja iniciar o aplicativo agora?",
                "Instalação Concluída",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (dr == DialogResult.Yes)
            {
                string exeToRun = Path.Combine(targetDir, "ReRainbowFizz.exe");
                Process.Start(new ProcessStartInfo { FileName = exeToRun, UseShellExecute = true });
            }

            Close();
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            _lblStatus.ForeColor = RedColor;
            _lblStatus.Text = "Erro na instalação.";
            _btnInstall.Enabled = true;
            MessageBox.Show(this, "Falha na instalação: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void CreateWindowsShortcut(string shortcutPath, string targetPath, string workDir)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workDir;
            shortcut.Description = "Redragon Fizz Macro Studio";
            shortcut.IconLocation = targetPath + ",0";
            shortcut.Save();
        }
        catch { }
    }

    private static void RegisterUninstaller(string installDir, string exePath)
    {
        try
        {
            string uninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ReRainbowFizz";
            using var key = Registry.CurrentUser.CreateSubKey(uninstallKeyPath);
            if (key == null) return;

            string uninstallerExe = Path.Combine(installDir, "Desinstalador_ReRainbowFizz.exe");
            string uninstallCmd = File.Exists(uninstallerExe)
                ? $"\"{uninstallerExe}\""
                : $"cmd.exe /c rmdir /s /q \"{installDir}\"";

            key.SetValue("DisplayName", "ReRainbowFizz");
            key.SetValue("DisplayIcon", exePath);
            key.SetValue("DisplayVersion", "2.0.0");
            key.SetValue("Publisher", "Redragon Community");
            key.SetValue("InstallLocation", installDir);
            key.SetValue("UninstallString", uninstallCmd);
            key.SetValue("QuietUninstallString", uninstallCmd);
        }
        catch { }
    }
}
