using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using QuickShot.Models;
using QuickShot.Native;
using QuickShot.Services;
using Point = System.Windows.Point;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace QuickShot.Views;

public partial class VirtualCanvasWindow : Window
{
    private readonly BitmapSource _frozenScreen;
    private readonly ActiveMonitorInfo _monitor;
    private readonly IScreenCaptureService _captureService;
    private readonly AutoArchiveManager _archiveManager;
    private readonly ISettingsStore _settingsStore;
    private readonly double _scaleX;
    private readonly double _scaleY;

    private readonly SelectionBox _selection = new();
    private SelectionHandleType _currentHandle = SelectionHandleType.None;
    private Point _dragStartPoint;
    private bool _isDragging;
    private bool _isAnnotating;

    private readonly WindowDetectionService _windowDetection = new();
    private AppCaptureMode _currentCaptureMode = AppCaptureMode.Rectangle;
    private Rect? _hoveredWindowRect;
    private DispatcherTimer? _toastTimer;

    private readonly List<UIElement> _annotationElements = new();
    private UIElement? _currentAnnotationVisual;

    private enum InteractionMode
    {
        None,
        CreatingSelection,
        MovingSelection,
        ResizingSelection,
        DrawingAnnotation
    }

    private InteractionMode _mode = InteractionMode.None;

    public VirtualCanvasWindow(
        BitmapSource frozenScreen,
        ActiveMonitorInfo monitor,
        IScreenCaptureService captureService,
        AutoArchiveManager archiveManager,
        ISettingsStore settingsStore)
    {
        InitializeComponent();

        _frozenScreen = frozenScreen;
        _monitor = monitor;
        _captureService = captureService;
        _archiveManager = archiveManager;
        _settingsStore = settingsStore;

        _scaleX = monitor.ScaleX;
        _scaleY = monitor.ScaleY;

        Left = monitor.DipLeft;
        Top = monitor.DipTop;
        Width = monitor.DipWidth;
        Height = monitor.DipHeight;

        BackgroundImage.Source = frozenScreen;
        FullScreenGeometry.Rect = new Rect(0, 0, Width, Height);
        SizeChanged += (s, e) =>
        {
            if (ActualWidth > 0 && ActualHeight > 0)
            {
                FullScreenGeometry.Rect = new Rect(0, 0, ActualWidth, ActualHeight);
            }
        };

        Toolbar.ToolChanged += OnToolbarToolChanged;
        Toolbar.ColorChanged += OnToolbarColorChanged;
        Toolbar.FontSizeChanged += OnToolbarFontSizeChanged;
        Toolbar.UndoRequested += OnUndo;
        Toolbar.SaveRequested += OnSaveToFile;
        Toolbar.CompleteRequested += OnComplete;
        Toolbar.CancelRequested += OnCancel;

        var initialSettings = _settingsStore.Load();
        Toolbar.SetFontSize(initialSettings.DefaultFontSize);

        // Initialize TopBar & CaptureMode
        _currentCaptureMode = initialSettings.LastCaptureMode;
        TopBar.SetMode(_currentCaptureMode);
        TopBar.ModeChanged += OnTopBarModeChanged;
        TopBar.CloseRequested += () => Close();

        // Capture snapshot of active windows for DWM window detection
        _windowDetection.RefreshWindows();

        KeyDown += OnWindowKeyDown;
        Loaded += (s, e) => Focus();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        User32.SetWindowPos(
            hwnd,
            IntPtr.Zero,
            _monitor.PhysicalLeft,
            _monitor.PhysicalTop,
            _monitor.PhysicalWidth,
            _monitor.PhysicalHeight,
            User32.SWP_NOZORDER | User32.SWP_NOACTIVATE);
    }

    private void OnToolbarToolChanged(AnnotationTool tool)
    {
        // Ready to draw
    }

    private void OnToolbarColorChanged(string hex)
    {
        // Color changed
    }

    private void OnToolbarFontSizeChanged(double size)
    {
        if (_currentAnnotationVisual is TextBox tb)
        {
            tb.FontSize = size;
        }
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            OnCancel();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            OnComplete();
            e.Handled = true;
        }
        else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OnUndo();
            e.Handled = true;
        }
        else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OnSaveToFile();
            e.Handled = true;
        }
        else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            // If user is actively editing text inside a TextBox and has text selected, allow normal copy
            if (Keyboard.FocusedElement is TextBox tb && tb.SelectionLength > 0)
            {
                return;
            }

            // Copy baked annotation image to clipboard while keeping window open (Option B)
            if (!_selection.IsEmpty)
            {
                var output = BakeCaptureOutput();
                try
                {
                    Clipboard.SetImage(output);
                    ShowToast("수정본이 클립보드에 복사되었습니다");
                }
                catch
                {
                }
                e.Handled = true;
            }
        }
    }

    private void OnTopBarModeChanged(AppCaptureMode mode)
    {
        _currentCaptureMode = mode;
        var settings = _settingsStore.Load();
        settings.LastCaptureMode = mode;
        _settingsStore.Save(settings);

        if (mode == AppCaptureMode.FullScreen)
        {
            SelectWholeScreen();
        }
        else if (mode == AppCaptureMode.AllMonitors)
        {
            CaptureAllMonitorsDirectly();
        }
        else if (mode == AppCaptureMode.Rectangle)
        {
            WindowHoverBorder.Visibility = Visibility.Collapsed;
            _hoveredWindowRect = null;
        }
    }

    private void CaptureAllMonitorsDirectly()
    {
        try
        {
            var allScreenBmp = _captureService.CaptureVirtualScreen();
            Clipboard.SetImage(allScreenBmp);

            var settings = _settingsStore.Load();
            if (settings.AutoSaveToFolder)
            {
                _archiveManager.Save(allScreenBmp);
            }

            ShowToast("모든 모니터 전체 화면이 클립보드에 복사 및 저장되었습니다");

            var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            closeTimer.Tick += (s, e) =>
            {
                closeTimer.Stop();
                Close();
            };
            closeTimer.Start();
        }
        catch (Exception ex)
        {
            ShowToast("모든 모니터 캡처 실패: " + ex.Message);
        }
    }

    private void SelectWholeScreen()
    {
        _selection.X = 0;
        _selection.Y = 0;
        _selection.Width = ActualWidth > 0 ? ActualWidth : Width;
        _selection.Height = ActualHeight > 0 ? ActualHeight : Height;
        _selection.Normalize();

        WindowHoverBorder.Visibility = Visibility.Collapsed;
        _hoveredWindowRect = null;
        TopBar.Visibility = Visibility.Collapsed;

        UpdateSelectionVisuals();
        PositionToolbar();
        AutoCopySelectionToClipboard();
    }

    private void ShowToast(string message)
    {
        _toastTimer?.Stop();
        ToastText.Text = message;
        ToastBorder.Visibility = Visibility.Visible;

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _toastTimer.Tick += (s, e) =>
        {
            ToastBorder.Visibility = Visibility.Collapsed;
            _toastTimer.Stop();
        };
        _toastTimer.Start();
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(OverlayCanvas);

        if (Toolbar.CurrentTool != AnnotationTool.None && !_selection.IsEmpty && _selection.Contains(pos.X, pos.Y))
        {
            // Start Drawing Annotation
            _mode = InteractionMode.DrawingAnnotation;
            _dragStartPoint = pos;
            _isAnnotating = true;
            StartAnnotation(pos);
            OverlayCanvas.CaptureMouse();
            return;
        }

        if (!_selection.IsEmpty)
        {
            var hit = _selection.HitTest(pos.X, pos.Y);
            if (hit != SelectionHandleType.None)
            {
                _currentHandle = hit;
                _dragStartPoint = pos;
                _isDragging = true;
                _mode = hit == SelectionHandleType.Inside
                    ? InteractionMode.MovingSelection
                    : InteractionMode.ResizingSelection;
                OverlayCanvas.CaptureMouse();
                return;
            }
        }

        // Window mode: one-click capture if hovering over a detected window
        if (_currentCaptureMode == AppCaptureMode.Window && _selection.IsEmpty && _hoveredWindowRect.HasValue)
        {
            _selection.X = _hoveredWindowRect.Value.Left;
            _selection.Y = _hoveredWindowRect.Value.Top;
            _selection.Width = _hoveredWindowRect.Value.Width;
            _selection.Height = _hoveredWindowRect.Value.Height;
            _selection.Normalize();

            WindowHoverBorder.Visibility = Visibility.Collapsed;
            _hoveredWindowRect = null;
            TopBar.Visibility = Visibility.Collapsed;

            UpdateSelectionVisuals();
            PositionToolbar();
            AutoCopySelectionToClipboard();
            return;
        }

        // Start new selection box (drag rectangle)
        _selection.X = pos.X;
        _selection.Y = pos.Y;
        _selection.Width = 0;
        _selection.Height = 0;
        _dragStartPoint = pos;
        _isDragging = true;
        _mode = InteractionMode.CreatingSelection;
        Toolbar.Visibility = Visibility.Collapsed;
        TopBar.Visibility = Visibility.Collapsed;
        WindowHoverBorder.Visibility = Visibility.Collapsed;
        _hoveredWindowRect = null;
        ClearAnnotations();
        UpdateSelectionVisuals();
        OverlayCanvas.CaptureMouse();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(OverlayCanvas);

        if (_mode == InteractionMode.DrawingAnnotation && _isAnnotating)
        {
            UpdateCurrentAnnotation(pos);
            return;
        }

        if (_isDragging)
        {
            double dx = pos.X - _dragStartPoint.X;
            double dy = pos.Y - _dragStartPoint.Y;

            if (_mode == InteractionMode.CreatingSelection)
            {
                _selection.Width = pos.X - _selection.X;
                _selection.Height = pos.Y - _selection.Y;
            }
            else if (_mode == InteractionMode.MovingSelection)
            {
                _selection.Move(dx, dy);
                _dragStartPoint = pos;
            }
            else if (_mode == InteractionMode.ResizingSelection)
            {
                _selection.Resize(_currentHandle, dx, dy);
                _dragStartPoint = pos;
            }

            UpdateSelectionVisuals();
            return;
        }

        // If in Window mode with no active selection, highlight window under cursor
        if (_currentCaptureMode == AppCaptureMode.Window && _selection.IsEmpty)
        {
            int physX = _monitor.PhysicalLeft + (int)Math.Round(pos.X * _scaleX);
            int physY = _monitor.PhysicalTop + (int)Math.Round(pos.Y * _scaleY);

            var winRect = _windowDetection.FindWindowAt(physX, physY);
            if (winRect.HasValue)
            {
                double dipLeft = (winRect.Value.Left - _monitor.PhysicalLeft) / _scaleX;
                double dipTop = (winRect.Value.Top - _monitor.PhysicalTop) / _scaleY;
                double dipWidth = winRect.Value.Width / _scaleX;
                double dipHeight = winRect.Value.Height / _scaleY;

                Canvas.SetLeft(WindowHoverBorder, dipLeft);
                Canvas.SetTop(WindowHoverBorder, dipTop);
                WindowHoverBorder.Width = Math.Max(0, dipWidth);
                WindowHoverBorder.Height = Math.Max(0, dipHeight);
                WindowHoverBorder.Visibility = Visibility.Visible;
                _hoveredWindowRect = new Rect(dipLeft, dipTop, dipWidth, dipHeight);
            }
            else
            {
                WindowHoverBorder.Visibility = Visibility.Collapsed;
                _hoveredWindowRect = null;
            }
        }
        else
        {
            WindowHoverBorder.Visibility = Visibility.Collapsed;
            _hoveredWindowRect = null;
        }

        // Update cursor when hovering
        UpdateHoverCursor(pos);
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging || _isAnnotating)
        {
            OverlayCanvas.ReleaseMouseCapture();
            _isDragging = false;

            if (_mode == InteractionMode.DrawingAnnotation)
            {
                FinishCurrentAnnotation();
                _isAnnotating = false;
            }
            else if (_mode == InteractionMode.CreatingSelection ||
                     _mode == InteractionMode.MovingSelection ||
                     _mode == InteractionMode.ResizingSelection)
            {
                _selection.Normalize();
                if (!_selection.IsEmpty)
                {
                    TopBar.Visibility = Visibility.Collapsed;
                    WindowHoverBorder.Visibility = Visibility.Collapsed;
                    _hoveredWindowRect = null;

                    UpdateSelectionVisuals();
                    PositionToolbar();
                    AutoCopySelectionToClipboard();
                }
                else
                {
                    ClearSelection();
                }
            }

            _mode = InteractionMode.None;
        }
    }

    private void UpdateHoverCursor(Point pos)
    {
        if (Toolbar.CurrentTool != AnnotationTool.None && !_selection.IsEmpty && _selection.Contains(pos.X, pos.Y))
        {
            Cursor = Cursors.Cross;
            return;
        }

        if (!_selection.IsEmpty)
        {
            var hit = _selection.HitTest(pos.X, pos.Y);
            Cursor = hit switch
            {
                SelectionHandleType.TopLeft or SelectionHandleType.BottomRight => Cursors.SizeNWSE,
                SelectionHandleType.TopRight or SelectionHandleType.BottomLeft => Cursors.SizeNESW,
                SelectionHandleType.Top or SelectionHandleType.Bottom => Cursors.SizeNS,
                SelectionHandleType.Left or SelectionHandleType.Right => Cursors.SizeWE,
                SelectionHandleType.Inside => Cursors.SizeAll,
                _ => Cursors.Cross
            };
        }
        else
        {
            Cursor = Cursors.Cross;
        }
    }

    private void UpdateSelectionVisuals()
    {
        if (_selection.IsEmpty)
        {
            SelectionGeometry.Rect = Rect.Empty;
            SelectionBorder.Visibility = Visibility.Collapsed;
            HandlesCanvas.Visibility = Visibility.Collapsed;
            return;
        }

        var rect = new Rect(_selection.Left, _selection.Top, _selection.NormalizedWidth, _selection.NormalizedHeight);
        SelectionGeometry.Rect = rect;

        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, rect.Left);
        Canvas.SetTop(SelectionBorder, rect.Top);
        SelectionBorder.Width = rect.Width;
        SelectionBorder.Height = rect.Height;

        RenderHandles(rect);
    }

    private void RenderHandles(Rect rect)
    {
        HandlesCanvas.Children.Clear();
        HandlesCanvas.Visibility = Visibility.Visible;

        double r = 4.0;
        Point[] points =
        [
            new(rect.Left, rect.Top),
            new((rect.Left + rect.Right) / 2.0, rect.Top),
            new(rect.Right, rect.Top),
            new(rect.Right, (rect.Top + rect.Bottom) / 2.0),
            new(rect.Right, rect.Bottom),
            new((rect.Left + rect.Right) / 2.0, rect.Bottom),
            new(rect.Left, rect.Bottom),
            new(rect.Left, (rect.Top + rect.Bottom) / 2.0)
        ];

        foreach (var p in points)
        {
            var rectHandle = new System.Windows.Shapes.Rectangle
            {
                Width = r * 2,
                Height = r * 2,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(rectHandle, p.X - r);
            Canvas.SetTop(rectHandle, p.Y - r);
            HandlesCanvas.Children.Add(rectHandle);
        }
    }

    private void PositionToolbar()
    {
        Toolbar.Visibility = Visibility.Visible;
        Toolbar.UpdateLayout();

        double tbWidth = Toolbar.ActualWidth > 0 ? Toolbar.ActualWidth : 380;
        double tbHeight = Toolbar.ActualHeight > 0 ? Toolbar.ActualHeight : 36;

        double left = _selection.Right - tbWidth;
        if (left < _selection.Left) left = _selection.Left;
        if (left < 10) left = 10;
        if (left + tbWidth > Width - 10) left = Width - tbWidth - 10;

        double top = _selection.Bottom + 8;
        if (top + tbHeight > Height - 10)
        {
            top = _selection.Top - tbHeight - 8;
            if (top < 10) top = _selection.Bottom - tbHeight - 10;
        }

        Canvas.SetLeft(Toolbar, left);
        Canvas.SetTop(Toolbar, top);
    }

    private void ClearSelection()
    {
        _selection.X = 0;
        _selection.Y = 0;
        _selection.Width = 0;
        _selection.Height = 0;
        SelectionGeometry.Rect = Rect.Empty;
        SelectionBorder.Visibility = Visibility.Collapsed;
        HandlesCanvas.Visibility = Visibility.Collapsed;
        Toolbar.Visibility = Visibility.Collapsed;
        WindowHoverBorder.Visibility = Visibility.Collapsed;
        _hoveredWindowRect = null;
        TopBar.Visibility = Visibility.Visible;
        ClearAnnotations();
    }

    private void ClearAnnotations()
    {
        AnnotationCanvas.Children.Clear();
        _annotationElements.Clear();
    }

    private void StartAnnotation(Point start)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(Toolbar.CurrentColor)!;

        switch (Toolbar.CurrentTool)
        {
            case AnnotationTool.Rectangle:
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Stroke = brush,
                    StrokeThickness = 3.0
                };
                Canvas.SetLeft(rect, start.X);
                Canvas.SetTop(rect, start.Y);
                _currentAnnotationVisual = rect;
                AnnotationCanvas.Children.Add(rect);
                break;

            case AnnotationTool.Highlighter:
                var hlBrush = new SolidColorBrush(brush.Color) { Opacity = 0.4 };
                var hlRect = new System.Windows.Shapes.Rectangle
                {
                    Fill = hlBrush
                };
                Canvas.SetLeft(hlRect, start.X);
                Canvas.SetTop(hlRect, start.Y);
                _currentAnnotationVisual = hlRect;
                AnnotationCanvas.Children.Add(hlRect);
                break;

            case AnnotationTool.Arrow:
                var arrowPath = new System.Windows.Shapes.Path
                {
                    Stroke = brush,
                    Fill = brush,
                    StrokeThickness = 3.0
                };
                _currentAnnotationVisual = arrowPath;
                AnnotationCanvas.Children.Add(arrowPath);
                break;

            case AnnotationTool.Text:
                // Create inline TextBox for text input
                var textBox = new TextBox
                {
                    Background = Brushes.Transparent,
                    Foreground = brush,
                    CaretBrush = brush,
                    BorderThickness = new Thickness(1),
                    BorderBrush = brush,
                    FontSize = Toolbar.CurrentFontSize,
                    FontWeight = FontWeights.Bold,
                    MinWidth = 80,
                    AcceptsReturn = true
                };
                Canvas.SetLeft(textBox, start.X);
                Canvas.SetTop(textBox, start.Y);
                AnnotationCanvas.Children.Add(textBox);
                _currentAnnotationVisual = textBox;
                textBox.Loaded += (s, e) => textBox.Focus();
                textBox.LostFocus += (s, e) => FinishTextAnnotation(textBox, brush);
                break;

            case AnnotationTool.Pixelate:
                var mosaicRect = new System.Windows.Shapes.Rectangle
                {
                    Stroke = Brushes.LightGray,
                    StrokeDashArray = [2, 2]
                };
                Canvas.SetLeft(mosaicRect, start.X);
                Canvas.SetTop(mosaicRect, start.Y);
                _currentAnnotationVisual = mosaicRect;
                AnnotationCanvas.Children.Add(mosaicRect);
                break;
        }
    }

    private void UpdateCurrentAnnotation(Point current)
    {
        if (_currentAnnotationVisual == null) return;

        double x = Math.Min(_dragStartPoint.X, current.X);
        double y = Math.Min(_dragStartPoint.Y, current.Y);
        double w = Math.Abs(current.X - _dragStartPoint.X);
        double h = Math.Abs(current.Y - _dragStartPoint.Y);

        if (_currentAnnotationVisual is System.Windows.Shapes.Rectangle rect)
        {
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = w;
            rect.Height = h;
        }
        else if (_currentAnnotationVisual is System.Windows.Shapes.Path path)
        {
            path.Data = CreateArrowGeometry(_dragStartPoint, current);
        }
    }

    private Geometry CreateArrowGeometry(Point start, Point end)
    {
        var geom = new GeometryGroup();
        geom.Children.Add(new LineGeometry(start, end));

        // Arrow head
        var dir = end - start;
        double len = dir.Length;
        if (len > 5)
        {
            dir.Normalize();
            var normal = new Vector(-dir.Y, dir.X);
            double arrowLen = 14.0;
            double arrowWidth = 7.0;

            var p1 = end - dir * arrowLen + normal * arrowWidth;
            var p2 = end - dir * arrowLen - normal * arrowWidth;

            var triangle = new PathGeometry();
            var figure = new PathFigure { StartPoint = end, IsClosed = true, IsFilled = true };
            figure.Segments.Add(new LineSegment(p1, true));
            figure.Segments.Add(new LineSegment(p2, true));
            triangle.Figures.Add(figure);

            geom.Children.Add(triangle);
        }

        return geom;
    }

    private void FinishCurrentAnnotation()
    {
        if (_currentAnnotationVisual == null) return;

        if (Toolbar.CurrentTool == AnnotationTool.Pixelate && _currentAnnotationVisual is System.Windows.Shapes.Rectangle rect)
        {
            AnnotationCanvas.Children.Remove(rect);
            double rx = Canvas.GetLeft(rect);
            double ry = Canvas.GetTop(rect);
            double rw = rect.Width;
            double rh = rect.Height;

            if (rw > 2 && rh > 2)
            {
                int prx = (int)Math.Round(rx * _scaleX);
                int pry = (int)Math.Round(ry * _scaleY);
                int prw = (int)Math.Round(rw * _scaleX);
                int prh = (int)Math.Round(rh * _scaleY);

                var pixelated = _captureService.ApplyPixelate(_frozenScreen, new Int32Rect(prx, pry, prw, prh), (int)Math.Max(4, Math.Round(14 * _scaleX)));
                var croppedPixelated = _captureService.Crop(pixelated, new Int32Rect(prx, pry, prw, prh));

                var image = new Image
                {
                    Source = croppedPixelated,
                    Width = rw,
                    Height = rh,
                    Stretch = Stretch.Fill
                };
                Canvas.SetLeft(image, rx);
                Canvas.SetTop(image, ry);
                AnnotationCanvas.Children.Add(image);
                _annotationElements.Add(image);
            }
        }
        else if (_currentAnnotationVisual is not TextBox)
        {
            _annotationElements.Add(_currentAnnotationVisual);
        }

        _currentAnnotationVisual = null;
    }

    private void FinishTextAnnotation(TextBox textBox, SolidColorBrush brush)
    {
        if (!string.IsNullOrWhiteSpace(textBox.Text))
        {
            var textBlock = new TextBlock
            {
                Text = textBox.Text,
                Foreground = brush,
                FontSize = textBox.FontSize,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(textBlock, Canvas.GetLeft(textBox));
            Canvas.SetTop(textBlock, Canvas.GetTop(textBox));
            AnnotationCanvas.Children.Remove(textBox);
            AnnotationCanvas.Children.Add(textBlock);
            _annotationElements.Add(textBlock);
        }
        else
        {
            AnnotationCanvas.Children.Remove(textBox);
        }
    }

    private void OnUndo()
    {
        if (_annotationElements.Count > 0)
        {
            var last = _annotationElements[^1];
            _annotationElements.RemoveAt(_annotationElements.Count - 1);
            AnnotationCanvas.Children.Remove(last);
        }
    }

    private BitmapSource BakeCaptureOutput()
    {
        _selection.Normalize();
        int sx = (int)Math.Round(_selection.Left * _scaleX);
        int sy = (int)Math.Round(_selection.Top * _scaleY);
        int sw = (int)Math.Round(_selection.NormalizedWidth * _scaleX);
        int sh = (int)Math.Round(_selection.NormalizedHeight * _scaleY);

        if (sw <= 0 || sh <= 0)
        {
            return _frozenScreen;
        }

        // Hide overlay UI elements before render
        DimmerMask.Visibility = Visibility.Collapsed;
        SelectionBorder.Visibility = Visibility.Collapsed;
        HandlesCanvas.Visibility = Visibility.Collapsed;
        Toolbar.Visibility = Visibility.Collapsed;
        TopBar.Visibility = Visibility.Collapsed;
        WindowHoverBorder.Visibility = Visibility.Collapsed;
        ToastBorder.Visibility = Visibility.Collapsed;

        // Render virtual canvas to bitmap at physical resolution
        int pixelWidth = _monitor.PhysicalWidth;
        int pixelHeight = _monitor.PhysicalHeight;
        var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, 96 * _scaleX, 96 * _scaleY, PixelFormats.Pbgra32);
        rtb.Render(RootGrid);

        // Crop to selection in physical pixels
        var cropped = _captureService.Crop(rtb, new Int32Rect(sx, sy, sw, sh));

        // Restore overlay UI for continued editing (e.g. for Ctrl+C)
        DimmerMask.Visibility = Visibility.Visible;
        UpdateSelectionVisuals();
        PositionToolbar();

        return cropped;
    }

    private void OnComplete()
    {
        if (_selection.IsEmpty)
        {
            // If enter pressed with no selection, take entire screen
            _selection.X = 0;
            _selection.Y = 0;
            _selection.Width = ActualWidth > 0 ? ActualWidth : Width;
            _selection.Height = ActualHeight > 0 ? ActualHeight : Height;
        }

        var output = BakeCaptureOutput();

        // 1. Copy edited output to clipboard
        try
        {
            Clipboard.SetImage(output);
        }
        catch
        {
        }

        // 2. Auto-save to Pictures/QuickShot
        var settings = _settingsStore.Load();
        if (settings.AutoSaveToFolder)
        {
            try
            {
                _archiveManager.Save(output);
            }
            catch
            {
                // Non-fatal if folder save fails
            }
        }

        Close();
    }

    private void OnSaveToFile()
    {
        if (_selection.IsEmpty) return;
        var output = BakeCaptureOutput();

        // Ensure edited image is copied to clipboard upon saving!
        try
        {
            Clipboard.SetImage(output);
        }
        catch
        {
        }

        var dlg = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All Files (*.*)|*.*",
            DefaultExt = ".png",
            FileName = DateTime.Now.ToString("yyyyMMdd_HHmmss")
        };

        if (dlg.ShowDialog() == true)
        {
            using var stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(output));
            encoder.Save(stream);
            Close();
        }
        else
        {
            // Restore visibility if dialog canceled
            UpdateSelectionVisuals();
            PositionToolbar();
        }
    }

    private void OnCancel()
    {
        if (!_selection.IsEmpty)
        {
            ClearSelection();
        }
        else
        {
            Close();
        }
    }

    private void AutoCopySelectionToClipboard()
    {
        var settings = _settingsStore.Load();
        if (!settings.AutoCopyToClipboard) return;

        try
        {
            int sx = (int)Math.Round(_selection.Left * _scaleX);
            int sy = (int)Math.Round(_selection.Top * _scaleY);
            int sw = (int)Math.Round(_selection.NormalizedWidth * _scaleX);
            int sh = (int)Math.Round(_selection.NormalizedHeight * _scaleY);

            if (sw > 0 && sh > 0)
            {
                var cropped = _captureService.Crop(_frozenScreen, new Int32Rect(sx, sy, sw, sh));
                Clipboard.SetImage(cropped);
            }
        }
        catch
        {
            // Ignore temporary clipboard access exceptions
        }
    }
}
