using System.Runtime.InteropServices;

namespace QuickShot.Native;

public static partial class DwmApi
{
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out User32.RECT pvAttribute,
        int cbAttribute);
}
