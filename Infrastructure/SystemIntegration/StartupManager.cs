using Microsoft.Win32;

namespace RedragonFizzMacro.Infrastructure.SystemIntegration;

public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "ReRainbowFizz";

    public static bool IsStartupEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(AppValueName) is string val && !string.IsNullOrWhiteSpace(val);
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartupEnabled(bool enable)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            // Clean legacy value if present
            key.DeleteValue("RedragonFizzMacroStudio", throwOnMissingValue: false);

            if (enable)
            {
                string candidateExe = Path.Combine(AppContext.BaseDirectory, "ReRainbowFizz.exe");
                string exePath = File.Exists(candidateExe)
                    ? candidateExe
                    : (Environment.ProcessPath ?? Application.ExecutablePath);
                key.SetValue(AppValueName, $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue(AppValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Ignore if restricted
        }
    }
}
