namespace WinNtf.Core;

public sealed class NotificationSlotQueue
{
    private readonly SortedSet<int> _availableSlots;

    public NotificationSlotQueue(int maxVisible)
    {
        if (maxVisible < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxVisible));
        }

        _availableSlots = new SortedSet<int>(Enumerable.Range(0, maxVisible));
    }

    public int? TryAcquire()
    {
        if (_availableSlots.Count == 0)
        {
            return null;
        }

        var slot = _availableSlots.Min;
        _availableSlots.Remove(slot);
        return slot;
    }

    public void Release(int slot)
    {
        if (slot < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        _availableSlots.Add(slot);
    }
}
