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

    public static void AcquireDroppingOldestReusesEarliestSlotWhenFull()
    {
        var queue = new NotificationSlotQueue(2);

        var first = queue.AcquireDroppingOldest();
        var second = queue.AcquireDroppingOldest();
        var third = queue.AcquireDroppingOldest();

        TestAssert.Equal(0, first.Slot);
        TestAssert.Null(first.DroppedSlot);
        TestAssert.Equal(1, second.Slot);
        TestAssert.Null(second.DroppedSlot);
        TestAssert.Equal(0, third.Slot);
        TestAssert.Equal(0, third.DroppedSlot);
    }

    public static void ReleaseIgnoresStaleDroppedLease()
    {
        var queue = new NotificationSlotQueue(2);

        var first = queue.AcquireDroppingOldest();
        queue.AcquireDroppingOldest();
        queue.AcquireDroppingOldest();

        queue.Release(first.Slot, first.Generation);

        TestAssert.Null(queue.TryAcquire());
    }

    public static void BumpToNewestPreventsSlotFromBeingDroppedNext()
    {
        var queue = new NotificationSlotQueue(2);
        var first = queue.AcquireDroppingOldest();
        queue.AcquireDroppingOldest();

        TestAssert.True(queue.BumpToNewest(first.Slot));

        var third = queue.AcquireDroppingOldest();
        TestAssert.Equal(1, third.DroppedSlot);
    }

    public static void BumpToNewestReturnsFalseForInactiveSlot()
    {
        var queue = new NotificationSlotQueue(2);

        TestAssert.True(!queue.BumpToNewest(0));
    }
}
