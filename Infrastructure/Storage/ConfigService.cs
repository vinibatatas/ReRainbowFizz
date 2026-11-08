using System.Text;
using System.Text.Json;
using RedragonFizzMacro.Core.Models;

namespace RedragonFizzMacro.Infrastructure.Storage;

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string GetConfigFilePath()
    {
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReRainbowFizz");
        Directory.CreateDirectory(appData);

        // Migrate legacy config if exists
        string targetFile = Path.Combine(appData, "macros_config.json");
        if (!File.Exists(targetFile))
        {
            string oldFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RedragonFizzMacro",
                "macros_config.json");
            if (File.Exists(oldFile))
            {
                try { File.Copy(oldFile, targetFile, true); } catch { }
            }
        }

        return targetFile;
    }

    public static AppConfig Load()
    {
        string path = GetConfigFilePath();
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
                if (cfg != null)
                {
                    _ = cfg.GetActiveProfile();
                    return cfg;
                }
            }
            catch
            {
                // Fallback to default
            }
        }

        var defaultConfig = new AppConfig();
        defaultConfig.Profiles.Add(AppConfig.CreateDefaultProfile());
        Save(defaultConfig);
        return defaultConfig;
    }

    public static void Save(AppConfig config)
    {
        try
        {
            string path = GetConfigFilePath();
            string json = JsonSerializer.Serialize(config, JsonOpts);
            File.WriteAllText(path, json, Encoding.UTF8);
        }
        catch
        {
            // Ignore transient lock
        }
    }
}
