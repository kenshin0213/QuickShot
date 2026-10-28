using QuickShot.Native;
using QuickShot.Services;
using Xunit;

namespace QuickShot.Tests;

public class WindowDetectionServiceTests
{
    [Fact]
    public void FindWindowAt_ReturnsCorrectWindow_InZOrder()
    {
        var service = new WindowDetectionService();

        // Top window (Hwnd 1): 100, 100, 500, 500
        service.AddWindow((IntPtr)1, new User32.RECT { Left = 100, Top = 100, Right = 500, Bottom = 500 });
        // Background window (Hwnd 2): 0, 0, 1920, 1080
        service.AddWindow((IntPtr)2, new User32.RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });

        // Point inside top window should return top window bounds
        var hit1 = service.FindWindowAt(200, 200);
        Assert.NotNull(hit1);
        Assert.Equal(100, hit1.Value.Left);
        Assert.Equal(500, hit1.Value.Right);

        // Point outside top window but inside background window should return background window bounds
        var hit2 = service.FindWindowAt(50, 50);
        Assert.NotNull(hit2);
        Assert.Equal(0, hit2.Value.Left);
        Assert.Equal(1920, hit2.Value.Right);

        // Point outside all windows
        var hit3 = service.FindWindowAt(-100, -100);
        Assert.Null(hit3);
    }
}
