using System.Windows;
using System.Windows.Controls;
using QuickShot.Models;

namespace QuickShot.Views;

public partial class FloatingToolbar : UserControl
{
    public event Action<AnnotationTool>? ToolChanged;
    public event Action<string>? ColorChanged;
    public event Action<double>? FontSizeChanged;
    public event Action? UndoRequested;
    public event Action? SaveRequested;
    public event Action? CompleteRequested;
    public event Action? CancelRequested;

    public AnnotationTool CurrentTool { get; private set; } = AnnotationTool.None;
    public string CurrentColor { get; private set; } = "#FFFF0000";
    public double CurrentFontSize { get; private set; } = 16.0;

    public FloatingToolbar()
    {
        InitializeComponent();
    }

    public void ResetTools()
    {
        BtnRect.IsChecked = false;
        BtnArrow.IsChecked = false;
        BtnText.IsChecked = false;
        BtnPixelate.IsChecked = false;
        BtnHighlighter.IsChecked = false;
        CurrentTool = AnnotationTool.None;
    }

    private void OnToolSelected(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            if (Enum.TryParse<AnnotationTool>(tag, out var tool))
            {
                CurrentTool = tool;
                ToolChanged?.Invoke(tool);
            }
        }
    }

    private void OnColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ColorCombo?.SelectedItem is ComboBoxItem item && item.Tag is string hex)
        {
            CurrentColor = hex;
            ColorChanged?.Invoke(hex);
        }
    }

    private void OnFontSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontSizeCombo?.SelectedItem is ComboBoxItem item &&
            item.Tag is string tagStr &&
            double.TryParse(tagStr, out double size))
        {
            CurrentFontSize = size;
            FontSizeChanged?.Invoke(size);
        }
    }

    public void SetFontSize(double size)
    {
        CurrentFontSize = size;
        foreach (var item in FontSizeCombo.Items)
        {
            if (item is ComboBoxItem cbi && cbi.Tag is string tagStr && double.TryParse(tagStr, out double s) && Math.Abs(s - size) < 0.1)
            {
                FontSizeCombo.SelectedItem = cbi;
                break;
            }
        }
    }

    private void OnUndoClicked(object sender, RoutedEventArgs e) => UndoRequested?.Invoke();
    private void OnSaveClicked(object sender, RoutedEventArgs e) => SaveRequested?.Invoke();
    private void OnCompleteClicked(object sender, RoutedEventArgs e) => CompleteRequested?.Invoke();
    private void OnCancelClicked(object sender, RoutedEventArgs e) => CancelRequested?.Invoke();
}
