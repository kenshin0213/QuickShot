using QuickShot.Models;
using Xunit;

namespace QuickShot.Tests;

public class SelectionBoxTests
{
    [Fact]
    public void SelectionBox_ForwardDrag_CalculatesCorrectCoordinates()
    {
        var box = new SelectionBox(100, 200, 300, 150);

        Assert.Equal(100, box.Left);
        Assert.Equal(200, box.Top);
        Assert.Equal(400, box.Right);
        Assert.Equal(350, box.Bottom);
        Assert.Equal(300, box.NormalizedWidth);
        Assert.Equal(150, box.NormalizedHeight);
        Assert.False(box.IsEmpty);
    }

    [Fact]
    public void SelectionBox_ReverseDrag_NormalizesProperly()
    {
        // Dragging from (400, 350) to (100, 200) -> width = -300, height = -150
        var box = new SelectionBox(400, 350, -300, -150);

        Assert.Equal(100, box.Left);
        Assert.Equal(200, box.Top);
        Assert.Equal(400, box.Right);
        Assert.Equal(350, box.Bottom);
        Assert.Equal(300, box.NormalizedWidth);
        Assert.Equal(150, box.NormalizedHeight);

        box.Normalize();
        Assert.Equal(100, box.X);
        Assert.Equal(200, box.Y);
        Assert.Equal(300, box.Width);
        Assert.Equal(150, box.Height);
    }

    [Fact]
    public void SelectionBox_Contains_ReturnsTrueForInteriorPoints()
    {
        var box = new SelectionBox(100, 100, 200, 200);

        Assert.True(box.Contains(150, 150));
        Assert.True(box.Contains(100, 100));
        Assert.True(box.Contains(300, 300));
        Assert.False(box.Contains(50, 150));
        Assert.False(box.Contains(350, 150));
    }

    [Fact]
    public void SelectionBox_HitTest_DetectsCornersAndEdges()
    {
        var box = new SelectionBox(100, 100, 200, 200);

        Assert.Equal(SelectionHandleType.TopLeft, box.HitTest(100, 100));
        Assert.Equal(SelectionHandleType.TopRight, box.HitTest(300, 100));
        Assert.Equal(SelectionHandleType.BottomLeft, box.HitTest(100, 300));
        Assert.Equal(SelectionHandleType.BottomRight, box.HitTest(300, 300));

        Assert.Equal(SelectionHandleType.Top, box.HitTest(200, 100));
        Assert.Equal(SelectionHandleType.Bottom, box.HitTest(200, 300));
        Assert.Equal(SelectionHandleType.Left, box.HitTest(100, 200));
        Assert.Equal(SelectionHandleType.Right, box.HitTest(300, 200));

        Assert.Equal(SelectionHandleType.Inside, box.HitTest(200, 200));
        Assert.Equal(SelectionHandleType.None, box.HitTest(50, 50));
    }

    [Fact]
    public void SelectionBox_Move_TranslatesPosition()
    {
        var box = new SelectionBox(100, 100, 200, 200);
        box.Move(50, -30);

        Assert.Equal(150, box.X);
        Assert.Equal(70, box.Y);
        Assert.Equal(200, box.Width);
        Assert.Equal(200, box.Height);
    }

    [Fact]
    public void SelectionBox_Resize_BottomRightExpands()
    {
        var box = new SelectionBox(100, 100, 200, 200);
        box.Resize(SelectionHandleType.BottomRight, 20, 30);

        Assert.Equal(100, box.X);
        Assert.Equal(100, box.Y);
        Assert.Equal(220, box.Width);
        Assert.Equal(230, box.Height);
    }

    [Fact]
    public void SelectionBox_Resize_TopLeftShrinks()
    {
        var box = new SelectionBox(100, 100, 200, 200);
        box.Resize(SelectionHandleType.TopLeft, 20, 30);

        Assert.Equal(120, box.X);
        Assert.Equal(130, box.Y);
        Assert.Equal(180, box.Width);
        Assert.Equal(170, box.Height);
    }
}
