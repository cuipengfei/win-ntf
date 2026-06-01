namespace WinNtf.Core;

public readonly record struct NotificationDisplayKey(
    string Title,
    string Text,
    NotificationVariant Variant,
    string Color,
    NotificationPosition Position)
{
    public static NotificationDisplayKey From(NormalizedNotification notification) => new(
        notification.Title,
        notification.Text,
        notification.Variant,
        notification.Color,
        notification.Position);
}
