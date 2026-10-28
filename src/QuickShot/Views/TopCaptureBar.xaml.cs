using System.Windows;
using System.Windows.Controls;
using QuickShot.Models;

namespace QuickShot.Views;

public partial class TopCaptureBar : UserControl
{
    public event Action<AppCaptureMode>? ModeChanged;
    public event Action? CloseRequested;

    public AppCaptureMode CurrentMode { get; private set; } = AppCaptureMode.Rectangle;

    public TopCaptureBar()
    {
        InitializeComponent();
    }

    public void SetMode(AppCaptureMode mode)
    {
        CurrentMode = mode;
        switch (mode)
        {
            case AppCaptureMode.Rectangle:
                RadioRect.IsChecked = true;
                break;
            case AppCaptureMode.Window:
                RadioWindow.IsChecked = true;
                break;
            case AppCaptureMode.FullScreen:
                RadioFullScreen.IsChecked = true;
                break;
            case AppCaptureMode.AllMonitors:
                RadioAllMonitors.IsChecked = true;
                break;
        }
    }

    private void OnModeClicked(object sender, RoutedEventArgs e)
    {
        if (sender == RadioRect)
        {
            CurrentMode = AppCaptureMode.Rectangle;
        }
        else if (sender == RadioWindow)
        {
            CurrentMode = AppCaptureMode.Window;
        }
        else if (sender == RadioFullScreen)
        {
            CurrentMode = AppCaptureMode.FullScreen;
        }
        else if (sender == RadioAllMonitors)
        {
            CurrentMode = AppCaptureMode.AllMonitors;
        }

        ModeChanged?.Invoke(CurrentMode);
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke();
    }
}
