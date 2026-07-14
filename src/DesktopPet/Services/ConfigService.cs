using System.Text.Json;
using DesktopPet.Models;

namespace DesktopPet.Services;

/// <summary>
/// Manages application configuration (load/save JSON)
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopPet");

    private static readonly string ConfigPath = System.IO.Path.Combine(ConfigDir, "config.json");

    private AppConfig? _config;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppConfig Config => _config ??= Load();

    public AppConfig Load()
    {
        try
        {
            if (System.IO.File.Exists(ConfigPath))
            {
                var json = System.IO.File.ReadAllText(ConfigPath);
                _config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
                return _config;
            }
        }
        catch { }

        _config = new AppConfig();
        Save(); // Create default config file
        return _config;
    }

    public void Save()
    {
        try
        {
            if (!System.IO.Directory.Exists(ConfigDir))
                System.IO.Directory.CreateDirectory(ConfigDir);

            var json = JsonSerializer.Serialize(_config ?? new AppConfig(), JsonOptions);
            System.IO.File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }

    public string GetSystemPrompt()
    {
        var cfg = Config;
        string profile = string.IsNullOrWhiteSpace(cfg.UserProfile)
            ? $"用户的名字是{cfg.UserName}。"
            : cfg.UserProfile;

        return cfg.SystemPrompt.Replace("{user_profile}", profile);
    }
}
