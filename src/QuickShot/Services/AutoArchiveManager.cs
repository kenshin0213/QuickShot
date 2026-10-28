using System.IO;
using System.Windows.Media.Imaging;

namespace QuickShot.Services;

public class AutoArchiveManager
{
    private readonly ISettingsStore _settingsStore;

    public AutoArchiveManager(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public string GenerateFilePath(string directory, DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            directory = Path.Combine(pictures, "QuickShot");
        }

        var baseFileName = timestamp.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{baseFileName}.png";
        var fullPath = Path.Combine(directory, fileName);

        int counter = 1;
        while (File.Exists(fullPath))
        {
            fileName = $"{baseFileName}_{counter}.png";
            fullPath = Path.Combine(directory, fileName);
            counter++;
        }

        return fullPath;
    }

    public string Save(BitmapSource bitmap)
    {
        var settings = _settingsStore.Load();
        var targetDir = settings.AutoSaveDirectory;
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var filePath = GenerateFilePath(targetDir, DateTime.Now);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(fileStream);

        return filePath;
    }
}
