namespace WinNtf.Core;

public sealed record NormalizedNotification(
    string Title,
    string Text,
    NotificationVariant Variant,
    string Color,
    int DurationMs,
    NotificationPosition Position,
    bool Persistent);
