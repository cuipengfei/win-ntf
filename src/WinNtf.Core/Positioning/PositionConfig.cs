namespace WinNtf.Core.Positioning;

public sealed record PositionConfig(
    NotificationPosition Position,
    int Width,
    int Height,
    int Gap = 12,
    int ScreenMargin = 20,
    int ScreenWidth = 1920,
    int ScreenHeight = 1080,
    int ScreenLeft = 0,
    int ScreenTop = 0);
