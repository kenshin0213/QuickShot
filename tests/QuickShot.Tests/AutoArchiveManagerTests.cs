using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickShot.Models;
using QuickShot.Services;
using Xunit;

namespace QuickShot.Tests;

public class AutoArchiveManagerTests
{
    private class MockSettingsStore : ISettingsStore
    {
        public AppSettings CurrentSettings { get; set; } = new();
        public AppSettings Load() => CurrentSettings;
        public void Save(AppSettings settings) => CurrentSettings = settings;
    }

    [Fact]
    public void GenerateFilePath_CreatesTimestampFormat()
    {
        var store = new MockSettingsStore();
        var manager = new AutoArchiveManager(store);

        var time = new DateTime(2026, 9, 18, 14, 30, 0);
        var path = manager.GenerateFilePath(@"C:\TestDir", time);

        Assert.Equal(@"C:\TestDir\20260918_143000.png", path);
    }

    [Fact]
    public void GenerateFilePath_AvoidsCollisions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"quickshot_archive_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var store = new MockSettingsStore();
            var manager = new AutoArchiveManager(store);

            var time = new DateTime(2026, 9, 18, 14, 30, 0);
            var existingFile = Path.Combine(tempDir, "20260918_143000.png");
            File.WriteAllText(existingFile, "dummy");

            var newPath = manager.GenerateFilePath(tempDir, time);
            Assert.Equal(Path.Combine(tempDir, "20260918_143000_1.png"), newPath);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Save_CreatesValidPngFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"quickshot_archive_save_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var store = new MockSettingsStore
            {
                CurrentSettings = new AppSettings { AutoSaveDirectory = tempDir }
            };
            var manager = new AutoArchiveManager(store);

            // Create a small 10x10 dummy bitmap
            var rawPixels = new byte[10 * 10 * 4];
            var bitmap = BitmapSource.Create(10, 10, 96, 96, PixelFormats.Bgra32, null, rawPixels, 10 * 4);

            var savedPath = manager.Save(bitmap);

            Assert.True(File.Exists(savedPath));
            Assert.True(new FileInfo(savedPath).Length > 0);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
