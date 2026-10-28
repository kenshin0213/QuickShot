using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickShot.Services;

public record VirtualScreenBounds(int Left, int Top, int Width, int Height);

public record ActiveMonitorInfo(
    int PhysicalLeft,
    int PhysicalTop,
    int PhysicalWidth,
    int PhysicalHeight,
    double ScaleX,
    double ScaleY)
{
    public double DipLeft => PhysicalLeft / ScaleX;
    public double DipTop => PhysicalTop / ScaleY;
    public double DipWidth => PhysicalWidth / ScaleX;
    public double DipHeight => PhysicalHeight / ScaleY;
}

public interface IScreenCaptureService
{
    VirtualScreenBounds GetVirtualScreenBounds();
    ActiveMonitorInfo GetActiveMonitorInfo();
    BitmapSource CaptureActiveMonitor(ActiveMonitorInfo monitor);
    BitmapSource CaptureVirtualScreen();
    BitmapSource Crop(BitmapSource source, Int32Rect rect);
    BitmapSource ApplyPixelate(BitmapSource source, Int32Rect region, int blockSize = 12);
}
