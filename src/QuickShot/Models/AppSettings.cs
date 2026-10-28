namespace QuickShot.Models;

public class AppSettings
{
    public string HotkeyModifiers { get; set; } = "Control, Shift";
    public string HotkeyKey { get; set; } = "A";
    public string AutoSaveDirectory { get; set; } = string.Empty;
    public bool AutoSaveToFolder { get; set; } = true;
    public bool AutoCopyToClipboard { get; set; } = true;
    public bool RunOnStartup { get; set; } = false;
    public string DefaultAnnotationColor { get; set; } = "#FF0000";
    public double DefaultStrokeThickness { get; set; } = 3.0;
    public double DefaultFontSize { get; set; } = 16.0;
    public AppCaptureMode LastCaptureMode { get; set; } = AppCaptureMode.Rectangle;
}
