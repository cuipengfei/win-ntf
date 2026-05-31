namespace WinNtf.Core;

public sealed record NotificationRequest(
    string? Title,
    string Text,
    NotificationVariant? Variant = null,
    string? Color = null,
    int? DurationMs = null,
    NotificationPosition? Position = null,
    bool Persistent = false);
