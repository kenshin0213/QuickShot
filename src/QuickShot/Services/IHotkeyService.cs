namespace QuickShot.Services;

public interface IHotkeyService : IDisposable
{
    event Action? HotkeyPressed;
    bool Register(uint modifiers, uint virtualKey);
    void Unregister();
}
