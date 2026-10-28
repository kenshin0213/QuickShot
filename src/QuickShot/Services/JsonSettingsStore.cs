using System.IO;
using System.Text.Json;
using QuickShot.Models;

namespace QuickShot.Services;

public class JsonSettingsStore : ISettingsStore
{
    private readonly string _filePath;

    public JsonSettingsStore(string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "QuickShot");
            _filePath = Path.Combine(dir, "settings.json");
        }
        else
        {
            _filePath = filePath;
        }
    }

    public string FilePath => _filePath;

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    EnsureDefaults(settings);
                    return settings;
                }
            }
        }
        catch
        {
            // Fallback to defaults on read error
        }

        var defaultSettings = new AppSettings();
        EnsureDefaults(defaultSettings);
        return defaultSettings;
    }

    public void Save(AppSettings settings)
    {
        EnsureDefaults(settings);
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private static void EnsureDefaults(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AutoSaveDirectory))
        {
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            settings.AutoSaveDirectory = Path.Combine(pictures, "QuickShot");
        }
    }
}
