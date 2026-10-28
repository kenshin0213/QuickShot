using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using QuickShot.Native;

namespace QuickShot.Services;

public class TrayIconService : IDisposable
{
    private const int TRAY_ID = 1001;
    private readonly HwndSource _source;
    private readonly IntPtr _hIcon;
    private readonly bool _ownsIcon;
    private bool _isCreated;
    private ContextMenu? _contextMenu;

    public event Action? CaptureRequested;

    public TrayIconService()
    {
        var parameters = new HwndSourceParameters("QuickShot_Tray_Listener")
        {
            WindowStyle = 0,
            ExtendedWindowStyle = 0,
            Height = 0,
            Width = 0,
            PositionX = 0,
            PositionY = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(HwndHook);

        _hIcon = LoadTrayIcon(out _ownsIcon);

        CreateTrayIcon();
    }

    public void SetContextMenu(ContextMenu menu)
    {
        _contextMenu = menu;
    }

    private void CreateTrayIcon()
    {
        var data = new Shell32.NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<Shell32.NOTIFYICONDATA>(),
            hWnd = _source.Handle,
            uID = TRAY_ID,
            uFlags = Shell32.NIF_MESSAGE | Shell32.NIF_ICON | Shell32.NIF_TIP,
            uCallbackMessage = Shell32.WM_TRAYICON,
            hIcon = _hIcon,
            szTip = "QuickShot (Ctrl+Shift+A)"
        };

        _isCreated = Shell32.Shell_NotifyIcon(Shell32.NIM_ADD, ref data);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Shell32.WM_TRAYICON && wParam.ToInt32() == TRAY_ID)
        {
            int eventMsg = (int)(lParam.ToInt64() & 0xFFFF);
            if (eventMsg == Shell32.WM_LBUTTONDBLCLK)
            {
                CaptureRequested?.Invoke();
                handled = true;
            }
            else if (eventMsg == Shell32.WM_RBUTTONUP)
            {
                ShowContextMenu();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        if (_contextMenu != null)
        {
            _contextMenu.IsOpen = true;
        }
    }

    private static IntPtr LoadTrayIcon(out bool ownsIcon)
    {
        ownsIcon = false;
        int iconWidth = User32.GetSystemMetrics(User32.SM_CXSMICON);
        int iconHeight = User32.GetSystemMetrics(User32.SM_CYSMICON);
        if (iconWidth <= 0) iconWidth = 16;
        if (iconHeight <= 0) iconHeight = 16;

        // 1. Try loading app.ico from application base directory
        string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
            IntPtr hIcon = User32.LoadImage(IntPtr.Zero, iconPath, User32.IMAGE_ICON, iconWidth, iconHeight, User32.LR_LOADFROMFILE);
            if (hIcon != IntPtr.Zero)
            {
                ownsIcon = true;
                return hIcon;
            }
        }

        // 2. Try extracting small icon from current running process executable
        try
        {
            string? exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                if (Shell32.ExtractIconEx(exePath, 0, out _, out IntPtr hSmallIcon, 1) > 0 && hSmallIcon != IntPtr.Zero)
                {
                    ownsIcon = true;
                    return hSmallIcon;
                }
            }
        }
        catch
        {
            // Fallback to stock icon
        }

        // 3. Fallback to standard application icon (IDI_APPLICATION = 32512)
        return Shell32.LoadIcon(IntPtr.Zero, (IntPtr)32512);
    }

    public void Dispose()
    {
        if (_isCreated)
        {
            var data = new Shell32.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<Shell32.NOTIFYICONDATA>(),
                hWnd = _source.Handle,
                uID = TRAY_ID
            };
            Shell32.Shell_NotifyIcon(Shell32.NIM_DELETE, ref data);
            _isCreated = false;
        }

        if (_ownsIcon && _hIcon != IntPtr.Zero)
        {
            Shell32.DestroyIcon(_hIcon);
        }

        _source.RemoveHook(HwndHook);
        _source.Dispose();
        GC.SuppressFinalize(this);
    }
}
