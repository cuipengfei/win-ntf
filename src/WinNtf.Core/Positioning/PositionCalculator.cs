namespace WinNtf.Core.Positioning;

public static class PositionCalculator
{
    public static CalculatedPosition Calculate(int slotIndex, PositionConfig config)
    {
        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        if (config.Position == NotificationPosition.Center)
        {
            return new CalculatedPosition(
                config.ScreenLeft + (config.ScreenWidth - config.Width) / 2,
                config.ScreenTop + (config.ScreenHeight - config.Height) / 2);
        }

        var left = config.ScreenLeft + config.ScreenWidth - config.Width - config.ScreenMargin;

        if (config.Position == NotificationPosition.BottomRight)
        {
            var top = config.ScreenTop + config.ScreenHeight - config.ScreenMargin -
                      ((slotIndex + 1) * (config.Height + config.Gap));
            return new CalculatedPosition(left, top);
        }

        return new CalculatedPosition(
            left,
            config.ScreenTop + config.ScreenMargin + (slotIndex * (config.Height + config.Gap)));
    }
}
