using System.Windows.Interop;
using QuickShot.Native;

namespace QuickShot.Services;

public class Win32HotkeyService : IHotkeyService
{
    private const int HOTKEY_ID = 9001;
    private HwndSource? _source;
    private bool _isRegistered;

    public event Action? HotkeyPressed;

    public Win32HotkeyService()
    {
        var parameters = new HwndSourceParameters("QuickShot_Hotkey_Listener")
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
    }

    public bool Register(uint modifiers, uint virtualKey)
    {
        if (_source == null) return false;

        Unregister();

        _isRegistered = User32.RegisterHotKey(_source.Handle, HOTKEY_ID, modifiers | User32.MOD_NOREPEAT, virtualKey);
        return _isRegistered;
    }

    public void Unregister()
    {
        if (_isRegistered && _source != null)
        {
            User32.UnregisterHotKey(_source.Handle, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        if (_source != null)
        {
            _source.RemoveHook(HwndHook);
            _source.Dispose();
            _source = null;
        }
        GC.SuppressFinalize(this);
    }
}
