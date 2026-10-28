using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using QuickShot.Models;
using QuickShot.Native;
using QuickShot.Services;
using QuickShot.Views;

namespace QuickShot;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private TrayIconService? _trayIconService;
    private ISettingsStore? _settingsStore;
    private IScreenCaptureService? _captureService;
    private AutoArchiveManager? _archiveManager;
    private IHotkeyService? _hotkeyService;
    private VirtualCanvasWindow? _activeCanvasWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Single Instance Check
        _singleInstanceMutex = new Mutex(true, "QuickShot_Application_Single_Instance_Mutex", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("QuickShot이 이미 실행 중입니다. (시스템 트레이를 확인하세요)", "QuickShot",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // 2. Initialize Services
        _settingsStore = new JsonSettingsStore();
        _captureService = new GdiScreenCaptureService();
        _archiveManager = new AutoArchiveManager(_settingsStore);
        _hotkeyService = new Win32HotkeyService();

        // 3. Register Global Hotkey
        RegisterGlobalHotkey();

        // 4. Setup System Tray Icon
        SetupSystemTray();
    }

    private void RegisterGlobalHotkey()
    {
        if (_hotkeyService == null) return;
        _hotkeyService.HotkeyPressed += TriggerCapture;
        // Default: Ctrl + Shift + A (A is 0x41)
        _hotkeyService.Register(User32.MOD_CONTROL | User32.MOD_SHIFT, 0x41);
    }

    public void TriggerCapture()
    {
        if (_activeCanvasWindow != null && _activeCanvasWindow.IsVisible)
        {
            _activeCanvasWindow.Activate();
            return;
        }

        if (_captureService == null || _archiveManager == null || _settingsStore == null) return;

        try
        {
            // Capture active monitor based on cursor position (1:1 physical pixel perfect)
            var activeMonitor = _captureService.GetActiveMonitorInfo();
            var screenBitmap = _captureService.CaptureActiveMonitor(activeMonitor);

            _activeCanvasWindow = new VirtualCanvasWindow(
                screenBitmap,
                activeMonitor,
                _captureService,
                _archiveManager,
                _settingsStore);

            _activeCanvasWindow.Closed += (s, e) => _activeCanvasWindow = null;
            _activeCanvasWindow.Show();
            _activeCanvasWindow.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"화면 캡처 중 오류가 발생했습니다:\n{ex.Message}", "QuickShot",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void TriggerCaptureAllMonitors()
    {
        if (_captureService == null || _archiveManager == null || _settingsStore == null) return;

        try
        {
            var bitmap = _captureService.CaptureVirtualScreen();
            Clipboard.SetImage(bitmap);

            var settings = _settingsStore.Load();
            if (settings.AutoSaveToFolder)
            {
                _archiveManager.Save(bitmap);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"모든 모니터 캡처 중 오류가 발생했습니다:\n{ex.Message}", "QuickShot",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetupSystemTray()
    {
        _trayIconService = new TrayIconService();
        _trayIconService.CaptureRequested += TriggerCapture;

        var contextMenu = new ContextMenu();

        var captureItem = new MenuItem
        {
            Header = "화면 캡처 (Ctrl+Shift+A)",
            FontWeight = FontWeights.Bold
        };
        captureItem.Click += (s, e) => TriggerCapture();
        contextMenu.Items.Add(captureItem);

        var captureAllItem = new MenuItem { Header = "모든 모니터 전체 캡처" };
        captureAllItem.Click += (s, e) => TriggerCaptureAllMonitors();
        contextMenu.Items.Add(captureAllItem);

        var openFolderItem = new MenuItem { Header = "저장 폴더 열기" };
        openFolderItem.Click += (s, e) => OpenSaveFolder();
        contextMenu.Items.Add(openFolderItem);

        contextMenu.Items.Add(new Separator());

        var startupItem = new MenuItem
        {
            Header = "Windows 시작 시 자동 실행",
            IsCheckable = true,
            IsChecked = IsStartupEnabled()
        };
        startupItem.Click += (s, e) => SetStartupEnabled(startupItem.IsChecked);
        contextMenu.Items.Add(startupItem);

        contextMenu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "종료" };
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _trayIconService.SetContextMenu(contextMenu);
    }

    private void OpenSaveFolder()
    {
        var settings = _settingsStore?.Load() ?? new AppSettings();
        var dir = settings.AutoSaveDirectory;
        if (string.IsNullOrWhiteSpace(dir))
        {
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "QuickShot");
        }

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true
        });
    }

    private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "QuickShot";

    private bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
            return key?.GetValue(APP_NAME) != null;
        }
        catch
        {
            return false;
        }
    }

    private void SetStartupEnabled(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(APP_NAME, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(APP_NAME, false);
            }
        }
        catch
        {
            // Ignore registry permission errors
        }
    }

    private void ExitApplication()
    {
        _trayIconService?.Dispose();
        _trayIconService = null;

        _hotkeyService?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ExitApplication();
        base.OnExit(e);
    }
}
