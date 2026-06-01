using WinNtf.Core;

namespace WinNtf.Core.Tests;

public static class NotificationDisplayKeyTests
{
    public static void FromTreatsSameVisibleNotificationAsDuplicate()
    {
        var first = new NormalizedNotification(
            "agent",
            "空闲，等待你的输入",
            NotificationVariant.Info,
            "#60A5FA",
            10_000,
            NotificationPosition.TopRight,
            false);
        var second = first with { DurationMs = 90_000, Persistent = true };

        TestAssert.Equal(NotificationDisplayKey.From(first), NotificationDisplayKey.From(second));
    }

    public static void FromKeepsDifferentVisibleNotificationSeparate()
    {
        var first = new NormalizedNotification(
            "agent",
            "空闲，等待你的输入",
            NotificationVariant.Info,
            "#60A5FA",
            90_000,
            NotificationPosition.TopRight,
            false);
        var second = first with { Variant = NotificationVariant.Warning, Color = "#FBBF24" };

        TestAssert.True(NotificationDisplayKey.From(first) != NotificationDisplayKey.From(second));
    }
}
