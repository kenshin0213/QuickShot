using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickShot.Native;

namespace QuickShot.Services;

public class GdiScreenCaptureService : IScreenCaptureService
{
    public VirtualScreenBounds GetVirtualScreenBounds()
    {
        int left = User32.GetSystemMetrics(User32.SM_XVIRTUALSCREEN);
        int top = User32.GetSystemMetrics(User32.SM_YVIRTUALSCREEN);
        int width = User32.GetSystemMetrics(User32.SM_CXVIRTUALSCREEN);
        int height = User32.GetSystemMetrics(User32.SM_CYVIRTUALSCREEN);

        return new VirtualScreenBounds(left, top, width, height);
    }

    public ActiveMonitorInfo GetActiveMonitorInfo()
    {
        User32.GetCursorPos(out var pt);
        IntPtr hMonitor = User32.MonitorFromPoint(pt, User32.MONITOR_DEFAULTTONEAREST);

        var mi = new User32.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<User32.MONITORINFO>() };
        if (!User32.GetMonitorInfo(hMonitor, ref mi))
        {
            // Fallback to Primary monitor
            int pWidth = User32.GetSystemMetrics(0); // SM_CXSCREEN
            int pHeight = User32.GetSystemMetrics(1); // SM_CYSCREEN
            return new ActiveMonitorInfo(0, 0, pWidth, pHeight, 1.0, 1.0);
        }

        double scaleX = 1.0;
        double scaleY = 1.0;
        try
        {
            int hr = Shcore.GetDpiForMonitor(hMonitor, Shcore.MDTS_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
            if (hr == 0 && dpiX > 0 && dpiY > 0)
            {
                scaleX = dpiX / 96.0;
                scaleY = dpiY / 96.0;
            }
        }
        catch
        {
            // Fallback if shcore is unavailable
        }

        return new ActiveMonitorInfo(
            mi.rcMonitor.Left,
            mi.rcMonitor.Top,
            mi.rcMonitor.Width,
            mi.rcMonitor.Height,
            scaleX,
            scaleY);
    }

    public BitmapSource CaptureActiveMonitor(ActiveMonitorInfo monitor)
    {
        IntPtr hDesktopWnd = User32.GetDesktopWindow();
        IntPtr hDesktopDC = User32.GetDC(hDesktopWnd);
        IntPtr hMemoryDC = Gdi32.CreateCompatibleDC(hDesktopDC);
        IntPtr hBitmap = Gdi32.CreateCompatibleBitmap(hDesktopDC, monitor.PhysicalWidth, monitor.PhysicalHeight);
        IntPtr hOldBitmap = Gdi32.SelectObject(hMemoryDC, hBitmap);

        try
        {
            Gdi32.BitBlt(
                hMemoryDC,
                0, 0,
                monitor.PhysicalWidth, monitor.PhysicalHeight,
                hDesktopDC,
                monitor.PhysicalLeft, monitor.PhysicalTop,
                Gdi32.SRCCOPY | Gdi32.CAPTUREBLT);

            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            Gdi32.SelectObject(hMemoryDC, hOldBitmap);
            Gdi32.DeleteObject(hBitmap);
            Gdi32.DeleteDC(hMemoryDC);
            User32.ReleaseDC(hDesktopWnd, hDesktopDC);
        }
    }

    public BitmapSource CaptureVirtualScreen()
    {
        var bounds = GetVirtualScreenBounds();
        IntPtr hDesktopWnd = User32.GetDesktopWindow();
        IntPtr hDesktopDC = User32.GetDC(hDesktopWnd);
        IntPtr hMemoryDC = Gdi32.CreateCompatibleDC(hDesktopDC);
        IntPtr hBitmap = Gdi32.CreateCompatibleBitmap(hDesktopDC, bounds.Width, bounds.Height);
        IntPtr hOldBitmap = Gdi32.SelectObject(hMemoryDC, hBitmap);

        try
        {
            Gdi32.BitBlt(
                hMemoryDC,
                0, 0,
                bounds.Width, bounds.Height,
                hDesktopDC,
                bounds.Left, bounds.Top,
                Gdi32.SRCCOPY | Gdi32.CAPTUREBLT);

            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            Gdi32.SelectObject(hMemoryDC, hOldBitmap);
            Gdi32.DeleteObject(hBitmap);
            Gdi32.DeleteDC(hMemoryDC);
            User32.ReleaseDC(hDesktopWnd, hDesktopDC);
        }
    }

    public BitmapSource Crop(BitmapSource source, Int32Rect rect)
    {
        // Clamp rect to source bounds
        int x = Math.Max(0, Math.Min(rect.X, source.PixelWidth - 1));
        int y = Math.Max(0, Math.Min(rect.Y, source.PixelHeight - 1));
        int w = Math.Max(1, Math.Min(rect.Width, source.PixelWidth - x));
        int h = Math.Max(1, Math.Min(rect.Height, source.PixelHeight - y));

        var cropped = new CroppedBitmap(source, new Int32Rect(x, y, w, h));
        cropped.Freeze();
        return cropped;
    }

    public BitmapSource ApplyPixelate(BitmapSource source, Int32Rect region, int blockSize = 12)
    {
        if (blockSize < 2) return source;

        // Ensure BGRA32 format for pixel manipulation
        var formatted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int width = formatted.PixelWidth;
        int height = formatted.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        formatted.CopyPixels(pixels, stride, 0);

        int rx = Math.Max(0, region.X);
        int ry = Math.Max(0, region.Y);
        int rw = Math.Min(region.Width, width - rx);
        int rh = Math.Min(region.Height, height - ry);

        for (int by = ry; by < ry + rh; by += blockSize)
        {
            for (int bx = rx; bx < rx + rw; bx += blockSize)
            {
                int currentBlockW = Math.Min(blockSize, rx + rw - bx);
                int currentBlockH = Math.Min(blockSize, ry + rh - by);

                // Calculate average color in block
                long sumB = 0, sumG = 0, sumR = 0, sumA = 0;
                int count = 0;

                for (int y = by; y < by + currentBlockH; y++)
                {
                    for (int x = bx; x < bx + currentBlockW; x++)
                    {
                        int idx = y * stride + x * 4;
                        sumB += pixels[idx];
                        sumG += pixels[idx + 1];
                        sumR += pixels[idx + 2];
                        sumA += pixels[idx + 3];
                        count++;
                    }
                }

                if (count == 0) continue;

                byte avgB = (byte)(sumB / count);
                byte avgG = (byte)(sumG / count);
                byte avgR = (byte)(sumR / count);
                byte avgA = (byte)(sumA / count);

                // Fill block with average color
                for (int y = by; y < by + currentBlockH; y++)
                {
                    for (int x = bx; x < bx + currentBlockW; x++)
                    {
                        int idx = y * stride + x * 4;
                        pixels[idx] = avgB;
                        pixels[idx + 1] = avgG;
                        pixels[idx + 2] = avgR;
                        pixels[idx + 3] = avgA;
                    }
                }
            }
        }

        var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }
}
