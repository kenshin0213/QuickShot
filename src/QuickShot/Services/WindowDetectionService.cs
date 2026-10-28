using System.Runtime.InteropServices;
using QuickShot.Native;

namespace QuickShot.Services;

public class WindowDetectionService
{
    public record WindowInfo(IntPtr Hwnd, User32.RECT Rect);

    private readonly List<WindowInfo> _windows = new();

    public void RefreshWindows(IntPtr? excludeHwnd = null)
    {
        _windows.Clear();

        User32.EnumWindows((hwnd, _) =>
        {
            if (excludeHwnd.HasValue && hwnd == excludeHwnd.Value)
                return true;

            if (!User32.IsWindowVisible(hwnd) || User32.IsIconic(hwnd))
                return true;

            // Try DWM Extended Frame Bounds first (precise visible border without shadow)
            User32.RECT rect;
            int hr = DwmApi.DwmGetWindowAttribute(
                hwnd,
                DwmApi.DWMWA_EXTENDED_FRAME_BOUNDS,
                out rect,
                Marshal.SizeOf<User32.RECT>());

            if (hr != 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                if (!User32.GetWindowRect(hwnd, out rect))
                    return true;
            }

            // Ignore zero or tiny invisible windows (e.g. system background helpers)
            if (rect.Width >= 30 && rect.Height >= 30)
            {
                _windows.Add(new WindowInfo(hwnd, rect));
            }

            return true;
        }, IntPtr.Zero);
    }

    public void AddWindow(IntPtr hwnd, User32.RECT rect)
    {
        _windows.Add(new WindowInfo(hwnd, rect));
    }

    public User32.RECT? FindWindowAt(int physicalX, int physicalY)
    {
        foreach (var win in _windows)
        {
            if (physicalX >= win.Rect.Left && physicalX < win.Rect.Right &&
                physicalY >= win.Rect.Top && physicalY < win.Rect.Bottom)
            {
                return win.Rect;
            }
        }

        return null;
    }
}
