namespace WinNtf.Core.Tests;

public static class Program
{
    public static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            (nameof(NotificationNormalizerTests.NormalizeRejectsEmptyText), Sync(NotificationNormalizerTests.NormalizeRejectsEmptyText)),
            (nameof(NotificationNormalizerTests.NormalizeAppliesDefaults), Sync(NotificationNormalizerTests.NormalizeAppliesDefaults)),
            (nameof(NotificationNormalizerTests.NormalizeCleansMarkdownAndWhitespace), Sync(NotificationNormalizerTests.NormalizeCleansMarkdownAndWhitespace)),
            (nameof(NotificationNormalizerTests.NormalizePersistentDisablesAutoClose), Sync(NotificationNormalizerTests.NormalizePersistentDisablesAutoClose)),
            (nameof(PositionCalculatorTests.CalculateTopRightStacksBySlot), Sync(PositionCalculatorTests.CalculateTopRightStacksBySlot)),
            (nameof(PositionCalculatorTests.CalculateBottomRightStacksUpward), Sync(PositionCalculatorTests.CalculateBottomRightStacksUpward)),
            (nameof(PositionCalculatorTests.CalculateCentersWindow), Sync(PositionCalculatorTests.CalculateCentersWindow)),
            (nameof(NotificationSlotQueueTests.TryAcquireReturnsNullWhenFull), Sync(NotificationSlotQueueTests.TryAcquireReturnsNullWhenFull)),
            (nameof(NotificationSlotQueueTests.ReleaseReusesLowestAvailableSlot), Sync(NotificationSlotQueueTests.ReleaseReusesLowestAvailableSlot)),
            (nameof(AppConfigStoreTests.LoadOrCreateWritesDefaultConfig), Sync(AppConfigStoreTests.LoadOrCreateWritesDefaultConfig)),
            (nameof(AppConfigStoreTests.LoadOrCreateRejectsInvalidPort), Sync(AppConfigStoreTests.LoadOrCreateRejectsInvalidPort)),
            (nameof(LocalHttpServerTests.ConstructorRejectsNonLoopbackHost), LocalHttpServerTests.ConstructorRejectsNonLoopbackHost),
            (nameof(LocalHttpServerTests.HealthReturnsOk), LocalHttpServerTests.HealthReturnsOk),
            (nameof(LocalHttpServerTests.NotifyInvokesSinkWithNormalizedNotification), LocalHttpServerTests.NotifyInvokesSinkWithNormalizedNotification),
            (nameof(LocalHttpServerTests.NotifyRejectsEmptyText), LocalHttpServerTests.NotifyRejectsEmptyText),
            (nameof(LocalHttpServerTests.NotifyAcceptsLowercaseVariant), LocalHttpServerTests.NotifyAcceptsLowercaseVariant),
            (nameof(LocalHttpServerTests.NotifyAcceptsKebabCasePosition), LocalHttpServerTests.NotifyAcceptsKebabCasePosition),
            (nameof(LocalHttpServerTests.NotifyRejectsOversizedPayload), LocalHttpServerTests.NotifyRejectsOversizedPayload),
            (nameof(LocalHttpServerTests.NotifyRejectsShortBody), LocalHttpServerTests.NotifyRejectsShortBody),
        };

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures += 1;
                Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
            }
        }

        return failures == 0 ? 0 : 1;
    }

    private static Func<Task> Sync(Action action) => () =>
    {
        action();
        return Task.CompletedTask;
    };
}
