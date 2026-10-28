using System.IO;
using QuickShot.Models;
using QuickShot.Services;
using Xunit;

namespace QuickShot.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Load_NonExistentFile_ReturnsDefaultSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"quickshot_test_{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonSettingsStore(tempFile);
            var settings = store.Load();

            Assert.NotNull(settings);
            Assert.Equal("Control, Shift", settings.HotkeyModifiers);
            Assert.Equal("A", settings.HotkeyKey);
            Assert.True(settings.AutoSaveToFolder);
            Assert.True(settings.AutoCopyToClipboard);
            Assert.False(string.IsNullOrWhiteSpace(settings.AutoSaveDirectory));
            Assert.Equal(16.0, settings.DefaultFontSize);
            Assert.Equal(AppCaptureMode.Rectangle, settings.LastCaptureMode);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundtripsSuccessfully()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"quickshot_test_{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonSettingsStore(tempFile);
            var original = new AppSettings
            {
                HotkeyKey = "F1",
                HotkeyModifiers = "Alt",
                AutoSaveToFolder = false,
                AutoCopyToClipboard = true,
                AutoSaveDirectory = @"C:\CustomSnips",
                DefaultAnnotationColor = "#00FF00",
                DefaultStrokeThickness = 5.0,
                DefaultFontSize = 24.0,
                LastCaptureMode = AppCaptureMode.Window
            };

            store.Save(original);
            Assert.True(File.Exists(tempFile));

            var loaded = store.Load();
            Assert.Equal("F1", loaded.HotkeyKey);
            Assert.Equal("Alt", loaded.HotkeyModifiers);
            Assert.False(loaded.AutoSaveToFolder);
            Assert.True(loaded.AutoCopyToClipboard);
            Assert.Equal(@"C:\CustomSnips", loaded.AutoSaveDirectory);
            Assert.Equal("#00FF00", loaded.DefaultAnnotationColor);
            Assert.Equal(5.0, loaded.DefaultStrokeThickness);
            Assert.Equal(24.0, loaded.DefaultFontSize);
            Assert.Equal(AppCaptureMode.Window, loaded.LastCaptureMode);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
