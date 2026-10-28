namespace QuickShot.Models;

public enum SelectionHandleType
{
    None,
    Inside,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}

public class SelectionBox
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public double Left => Math.Min(X, X + Width);
    public double Top => Math.Min(Y, Y + Height);
    public double Right => Math.Max(X, X + Width);
    public double Bottom => Math.Max(Y, Y + Height);
    public double NormalizedWidth => Math.Abs(Width);
    public double NormalizedHeight => Math.Abs(Height);

    public bool IsEmpty => NormalizedWidth < 1 || NormalizedHeight < 1;

    public SelectionBox() { }

    public SelectionBox(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public void Normalize()
    {
        var l = Left;
        var t = Top;
        var w = NormalizedWidth;
        var h = NormalizedHeight;
        X = l;
        Y = t;
        Width = w;
        Height = h;
    }

    public bool Contains(double px, double py)
    {
        return px >= Left && px <= Right && py >= Top && py <= Bottom;
    }

    public SelectionHandleType HitTest(double px, double py, double handleRadius = 8.0)
    {
        if (IsEmpty) return SelectionHandleType.None;

        var l = Left;
        var t = Top;
        var r = Right;
        var b = Bottom;
        var cx = (l + r) / 2.0;
        var cy = (t + b) / 2.0;

        bool Near(double hx, double hy) =>
            Math.Abs(px - hx) <= handleRadius && Math.Abs(py - hy) <= handleRadius;

        if (Near(l, t)) return SelectionHandleType.TopLeft;
        if (Near(r, t)) return SelectionHandleType.TopRight;
        if (Near(l, b)) return SelectionHandleType.BottomLeft;
        if (Near(r, b)) return SelectionHandleType.BottomRight;

        if (Near(cx, t)) return SelectionHandleType.Top;
        if (Near(cx, b)) return SelectionHandleType.Bottom;
        if (Near(l, cy)) return SelectionHandleType.Left;
        if (Near(r, cy)) return SelectionHandleType.Right;

        if (Contains(px, py)) return SelectionHandleType.Inside;

        return SelectionHandleType.None;
    }

    public void Move(double dx, double dy)
    {
        X += dx;
        Y += dy;
    }

    public void Resize(SelectionHandleType handle, double dx, double dy)
    {
        Normalize();
        switch (handle)
        {
            case SelectionHandleType.TopLeft:
                X += dx;
                Y += dy;
                Width -= dx;
                Height -= dy;
                break;
            case SelectionHandleType.Top:
                Y += dy;
                Height -= dy;
                break;
            case SelectionHandleType.TopRight:
                Y += dy;
                Width += dx;
                Height -= dy;
                break;
            case SelectionHandleType.Right:
                Width += dx;
                break;
            case SelectionHandleType.BottomRight:
                Width += dx;
                Height += dy;
                break;
            case SelectionHandleType.Bottom:
                Height += dy;
                break;
            case SelectionHandleType.BottomLeft:
                X += dx;
                Width -= dx;
                Height += dy;
                break;
            case SelectionHandleType.Left:
                X += dx;
                Width -= dx;
                break;
        }

        if (Width < 0)
        {
            X += Width;
            Width = -Width;
        }
        if (Height < 0)
        {
            Y += Height;
            Height = -Height;
        }
    }
}
