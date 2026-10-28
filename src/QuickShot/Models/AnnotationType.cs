namespace QuickShot.Models;

public enum AnnotationTool
{
    None,
    Rectangle,
    Arrow,
    Text,
    Pixelate,
    Highlighter
}

public class AnnotationElement
{
    public AnnotationTool Tool { get; set; }
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public string ColorHex { get; set; } = "#FF0000";
    public double StrokeThickness { get; set; } = 3.0;
    public string Text { get; set; } = string.Empty;
    public double FontSize { get; set; } = 16.0;
    public int PixelateBlockSize { get; set; } = 12;
}
