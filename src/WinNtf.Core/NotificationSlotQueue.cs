namespace WinNtf.Core;

public sealed record NotificationSlotLease(int Slot, long Generation, int? DroppedSlot);

public sealed class NotificationSlotQueue
{
    private readonly SortedSet<int> _availableSlots;
    private readonly Queue<int> _activeOrder = new();
    private readonly Dictionary<int, long> _activeGenerations = new();
    private long _nextGeneration;

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
        Activate(slot);
        return slot;
    }

    public NotificationSlotLease AcquireDroppingOldest()
    {
        int slot;
        int? droppedSlot = null;

        if (_availableSlots.Count > 0)
        {
            slot = _availableSlots.Min;
            _availableSlots.Remove(slot);
        }
        else
        {
            slot = _activeOrder.Dequeue();
            _activeGenerations.Remove(slot);
            droppedSlot = slot;
        }

        var generation = Activate(slot);
        return new NotificationSlotLease(slot, generation, droppedSlot);
    }

    public void Release(int slot)
    {
        if (slot < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        _activeGenerations.Remove(slot);
        RemoveFromActiveOrder(slot);
        _availableSlots.Add(slot);
    }

    public void Release(int slot, long generation)
    {
        if (slot < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        if (_activeGenerations.TryGetValue(slot, out var activeGeneration) && activeGeneration == generation)
        {
            Release(slot);
        }
    }

    public bool BumpToNewest(int slot)
    {
        if (slot < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        if (!_activeGenerations.ContainsKey(slot))
        {
            return false;
        }

        RemoveFromActiveOrder(slot);
        _activeOrder.Enqueue(slot);
        return true;
    }

    private long Activate(int slot)
    {
        var generation = ++_nextGeneration;
        _activeGenerations[slot] = generation;
        _activeOrder.Enqueue(slot);
        return generation;
    }

    private void RemoveFromActiveOrder(int slot)
    {
        var remaining = _activeOrder.Where(activeSlot => activeSlot != slot).ToArray();
        _activeOrder.Clear();
        foreach (var activeSlot in remaining)
        {
            _activeOrder.Enqueue(activeSlot);
        }
    }
}
