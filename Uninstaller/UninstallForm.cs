using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;
using Microsoft.Win32;

namespace RedragonFizzMacro.Uninstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new UninstallForm());
    }
}

internal sealed class UninstallForm : Form
{
    private static readonly Color BgColor = Color.FromArgb(17, 17, 19);
    private static readonly Color SurfaceColor = Color.FromArgb(26, 26, 29);
    private static readonly Color TextColor = Color.FromArgb(242, 242, 247);
    private static readonly Color SubTextColor = Color.FromArgb(142, 142, 147);
    private static readonly Color RedColor = Color.FromArgb(255, 69, 58);
    private static readonly Color BorderColor = Color.FromArgb(46, 46, 50);

    private readonly string _installDir;
    private readonly CheckBox _chkCleanAppData;
    private readonly Button _btnUninstall;
    private readonly Label _lblStatus;
    private readonly ProgressBar _progressBar;

    public UninstallForm()
    {
        _installDir = DetectInstallDirectory();

        Text = "Desinstalador — ReRainbowFizz";
        ClientSize = new Size(500, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 8.5f);

        string icoPath = Path.Combine(AppContext.BaseDirectory, "app_icon.ico");
        if (!File.Exists(icoPath))
            icoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app_icon.ico");
        if (File.Exists(icoPath))
        {
            try { Icon = new Icon(icoPath); } catch { }
        }

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = BgColor,
            Padding = new Padding(24, 14, 24, 8)
        };

        var picBox = new PictureBox
        {
            Size = new Size(42, 42),
            Location = new Point(24, 16),
            SizeMode = PictureBoxSizeMode.Zoom
        };

        string pngIconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app_icon.png");
        if (!File.Exists(pngIconPath))
            pngIconPath = Path.Combine(AppContext.BaseDirectory, "..", "Resources", "app_icon.png");
        if (File.Exists(pngIconPath))
        {
            try { picBox.Image = Image.FromFile(pngIconPath); } catch { }
        }

        var lblTitle = new Label
        {
            Text = "Desinstalar ReRainbowFizz",
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = RedColor,
            AutoSize = true,
            Location = new Point(76, 16)
        };

        var lblSubtitle = new Label
        {
            Text = "Remover o utilitário de macros e atalhos do computador",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = SubTextColor,
            AutoSize = true,
            Location = new Point(77, 40)
        };

        pnlHeader.Controls.AddRange(new Control[] { picBox, lblTitle, lblSubtitle });

        // Content
        var lblDesc = new Label
        {
            Text = "O aplicativo será desinstalado da seguinte pasta:",
            Location = new Point(24, 88),
            AutoSize = true,
            ForeColor = TextColor
        };

        var txtPath = new TextBox
        {
            Text = _installDir,
            Location = new Point(24, 112),
            Width = 452,
            ReadOnly = true,
            BackColor = SurfaceColor,
            ForeColor = SubTextColor,
            BorderStyle = BorderStyle.FixedSingle
        };

        _chkCleanAppData = new CheckBox
        {
            Text = "Apagar também perfis, macros e histórico salvos em %LocalAppData%",
            Checked = true,
            Location = new Point(24, 150),
            AutoSize = true,
            ForeColor = TextColor
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(24, 190),
            Size = new Size(452, 6),
            Visible = false
        };

        _lblStatus = new Label
        {
            Text = "Clique em 'Desinstalar' para iniciar a remoção.",
            Location = new Point(24, 204),
            Size = new Size(452, 24),
            ForeColor = SubTextColor
        };

        // Footer
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = BgColor
        };

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Location = new Point(272, 12),
            Size = new Size(96, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = SurfaceColor,
            ForeColor = TextColor
        };
        btnCancel.FlatAppearance.BorderColor = BorderColor;
        btnCancel.Click += (_, _) => Close();

        _btnUninstall = new Button
        {
            Text = "Desinstalar",
            Location = new Point(380, 12),
            Size = new Size(96, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = RedColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };
        _btnUninstall.FlatAppearance.BorderSize = 0;
        _btnUninstall.Click += DoUninstall;

        pnlFooter.Controls.AddRange(new Control[] { btnCancel, _btnUninstall });

        Controls.AddRange(new Control[]
        {
            pnlHeader, lblDesc, txtPath, _chkCleanAppData, _progressBar, _lblStatus, pnlFooter
        });
    }

    private static string DetectInstallDirectory()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ReRainbowFizz")
                         ?? Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RedragonFizzMacro");
            if (key?.GetValue("InstallLocation") is string loc && !string.IsNullOrWhiteSpace(loc) && Directory.Exists(loc))
                return loc;
        }
        catch { }

        // Check if running from inside the install directory itself
        string currentBase = AppContext.BaseDirectory.TrimEnd('\\', '/');
        if (File.Exists(Path.Combine(currentBase, "ReRainbowFizz.exe")) || File.Exists(Path.Combine(currentBase, "RedragonFizzMacro.exe")))
            return currentBase;

        // Default path
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "ReRainbowFizz");
    }

    private async void DoUninstall(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            this,
            "Tem certeza de que deseja remover o ReRainbowFizz?",
            "Confirmar Desinstalação",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _btnUninstall.Enabled = false;
        _progressBar.Visible = true;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _lblStatus.Text = "Removendo arquivos e configurações...";

        bool wipeAppData = _chkCleanAppData.Checked;
        string dirToRemove = _installDir;

        try
        {
            await Task.Run(() =>
            {
                // 1. Stop active processes
                foreach (var proc in Process.GetProcessesByName("ReRainbowFizz").Concat(Process.GetProcessesByName("RedragonFizzMacro")))
                {
                    try { proc.Kill(); proc.WaitForExit(3000); } catch { }
                }

                // 2. Remove Run registry key
                try
                {
                    using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                    runKey?.DeleteValue("ReRainbowFizz", throwOnMissingValue: false);
                    runKey?.DeleteValue("RedragonFizzMacroStudio", throwOnMissingValue: false);
                }
                catch { }

                // 3. Remove Desktop shortcuts
                try
                {
                    string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string lnk1 = Path.Combine(desktopDir, "ReRainbowFizz.lnk");
                    string lnk2 = Path.Combine(desktopDir, "Fizz Macro.lnk");
                    if (File.Exists(lnk1)) File.Delete(lnk1);
                    if (File.Exists(lnk2)) File.Delete(lnk2);
                }
                catch { }

                // 4. Remove Start Menu shortcuts
                try
                {
                    string startMenuRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                    string dir1 = Path.Combine(startMenuRoot, "ReRainbowFizz");
                    string dir2 = Path.Combine(startMenuRoot, "Redragon Fizz");
                    if (Directory.Exists(dir1)) Directory.Delete(dir1, recursive: true);
                    if (Directory.Exists(dir2)) Directory.Delete(dir2, recursive: true);
                }
                catch { }

                // 5. Remove Uninstall registry keys
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ReRainbowFizz", throwOnMissingSubKey: false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RedragonFizzMacro", throwOnMissingSubKey: false);
                }
                catch { }

                // 6. Optional: Clean LocalAppData configs
                if (wipeAppData)
                {
                    try
                    {
                        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string app1 = Path.Combine(localApp, "ReRainbowFizz");
                        string app2 = Path.Combine(localApp, "RedragonFizzMacro");
                        if (Directory.Exists(app1)) Directory.Delete(app1, recursive: true);
                        if (Directory.Exists(app2)) Directory.Delete(app2, recursive: true);
                    }
                    catch { }
                }

                // 7. Delete installed directory
                if (Directory.Exists(dirToRemove))
                {
                    string currentExePath = Environment.ProcessPath ?? "";
                    bool isSelfInInstallDir = !string.IsNullOrEmpty(currentExePath) &&
                                              currentExePath.StartsWith(dirToRemove, StringComparison.OrdinalIgnoreCase);

                    if (isSelfInInstallDir)
                    {
                        // Delete all files except ourselves, then queue self-deletion
                        foreach (var file in Directory.GetFiles(dirToRemove, "*", SearchOption.AllDirectories))
                        {
                            if (string.Equals(file, currentExePath, StringComparison.OrdinalIgnoreCase)) continue;
                            try { File.Delete(file); } catch { }
                        }

                        // Detached delayed folder cleanup
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c ping 127.0.0.1 -n 2 > nul & rmdir /s /q \"{dirToRemove}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                    }
                    else
                    {
                        try { Directory.Delete(dirToRemove, recursive: true); } catch { }
                    }
                }
            });

            _progressBar.Visible = false;
            _lblStatus.ForeColor = Color.FromArgb(52, 199, 89);
            _lblStatus.Text = "Desinstalação concluída com sucesso!";

            MessageBox.Show(
                this,
                "O ReRainbowFizz foi completamente desinstalado do seu sistema.",
                "Desinstalação Concluída",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Close();
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            _lblStatus.ForeColor = RedColor;
            _lblStatus.Text = "Erro durante a desinstalação.";
            _btnUninstall.Enabled = true;
            MessageBox.Show(this, "Falha na desinstalação: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}