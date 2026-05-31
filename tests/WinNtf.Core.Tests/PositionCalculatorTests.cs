using WinNtf.Core.Positioning;

namespace WinNtf.Core.Tests;

public static class PositionCalculatorTests
{
    public static void CalculateTopRightStacksBySlot()
    {
        var config = new PositionConfig(NotificationPosition.TopRight, Width: 420, Height: 105);

        var first = PositionCalculator.Calculate(0, config);
        var second = PositionCalculator.Calculate(1, config);

        TestAssert.Equal(1480, first.Left);
        TestAssert.Equal(20, first.Top);
        TestAssert.Equal(1480, second.Left);
        TestAssert.Equal(137, second.Top);
    }

    public static void CalculateBottomRightStacksUpward()
    {
        var config = new PositionConfig(NotificationPosition.BottomRight, Width: 420, Height: 105);

        var position = PositionCalculator.Calculate(1, config);

        TestAssert.Equal(1480, position.Left);
        TestAssert.Equal(826, position.Top);
    }

    public static void CalculateCentersWindow()
    {
        var config = new PositionConfig(NotificationPosition.Center, Width: 420, Height: 105);

        var position = PositionCalculator.Calculate(0, config);

        TestAssert.Equal(750, position.Left);
        TestAssert.Equal(487, position.Top);
    }
}
