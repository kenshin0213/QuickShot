using System.Runtime.InteropServices;

namespace QuickShot.Native;

public static partial class Shcore
{
    public const int MDTS_EFFECTIVE_DPI = 0;
    public const int MDTS_ANGULAR_DPI = 1;
    public const int MDTS_RAW_DPI = 2;

    [LibraryImport("shcore.dll")]
    public static partial int GetDpiForMonitor(
        IntPtr hmonitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);
}
