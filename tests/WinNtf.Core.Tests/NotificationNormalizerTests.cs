using WinNtf.Core;

namespace WinNtf.Core.Tests;

public static class NotificationNormalizerTests
{
    public static void NormalizeRejectsEmptyText()
    {
        var normalizer = new NotificationNormalizer();
        TestAssert.Throws<NotificationValidationException>(() =>
            normalizer.Normalize(new NotificationRequest(null, "   ")));
    }

    public static void NormalizeAppliesDefaults()
    {
        var normalizer = new NotificationNormalizer();
        var result = normalizer.Normalize(new NotificationRequest(null, "done"));

        TestAssert.Equal("win-ntf", result.Title);
        TestAssert.Equal("done", result.Text);
        TestAssert.Equal(NotificationVariant.Info, result.Variant);
        TestAssert.Equal("#60A5FA", result.Color);
        TestAssert.Equal(10_000, result.DurationMs);
        TestAssert.Equal(NotificationPosition.TopRight, result.Position);
    }

    public static void NormalizeCleansMarkdownAndWhitespace()
    {
        var normalizer = new NotificationNormalizer();
        var result = normalizer.Normalize(new NotificationRequest(" t ", "**Done**\n\n# now `ok`"));

        TestAssert.Equal("t", result.Title);
        TestAssert.Equal("Done now ok", result.Text);
    }

    public static void NormalizePersistentDisablesAutoClose()
    {
        var normalizer = new NotificationNormalizer();
        var result = normalizer.Normalize(new NotificationRequest(null, "wait", DurationMs: 5000, Persistent: true));

        TestAssert.Equal(0, result.DurationMs);
        TestAssert.True(result.Persistent);
    }
}
