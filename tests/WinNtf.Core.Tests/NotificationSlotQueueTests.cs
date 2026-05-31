using WinNtf.Core;

namespace WinNtf.Core.Tests;

public static class NotificationSlotQueueTests
{
    public static void TryAcquireReturnsNullWhenFull()
    {
        var queue = new NotificationSlotQueue(2);

        TestAssert.Equal(0, queue.TryAcquire());
        TestAssert.Equal(1, queue.TryAcquire());
        TestAssert.Null(queue.TryAcquire());
    }

    public static void ReleaseReusesLowestAvailableSlot()
    {
        var queue = new NotificationSlotQueue(2);

        TestAssert.Equal(0, queue.TryAcquire());
        TestAssert.Equal(1, queue.TryAcquire());

        queue.Release(0);

        TestAssert.Equal(0, queue.TryAcquire());
    }
}
