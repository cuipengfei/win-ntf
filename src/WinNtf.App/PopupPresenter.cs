using System.Windows;
using WinNtf.Core;
using WinNtf.Server;

namespace WinNtf.App;

public sealed class PopupPresenter : INotificationSink
{
    private readonly NotificationSlotQueue _slots;
    private readonly Dictionary<int, PopupWindow> _windowsBySlot = new();
    private readonly Dictionary<NotificationDisplayKey, ActivePopup> _popupsByKey = new();
    private readonly Dictionary<int, NotificationDisplayKey> _keysBySlot = new();
    private readonly List<int> _displayOrder = new();

    public PopupPresenter(int maxVisible = 10)
    {
        _slots = new NotificationSlotQueue(maxVisible);
    }

    public Task ShowAsync(NormalizedNotification notification, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var key = NotificationDisplayKey.From(notification);
            if (_popupsByKey.TryGetValue(key, out var activePopup) &&
                _windowsBySlot.TryGetValue(activePopup.Lease.Slot, out var activeWindow) &&
                _slots.BumpToNewest(activePopup.Lease.Slot))
            {
                activeWindow.RestartTimer(notification.DurationMs);
                MoveToFront(activePopup.Lease.Slot);
                return;
            }

            var lease = _slots.AcquireDroppingOldest();
            if (lease.DroppedSlot is int droppedSlot && _windowsBySlot.Remove(droppedSlot, out var droppedWindow))
            {
                if (_keysBySlot.Remove(droppedSlot, out var droppedKey))
                {
                    _popupsByKey.Remove(droppedKey);
                }
                _displayOrder.Remove(droppedSlot);
                droppedWindow.Close();
            }

            PopupWindow? window = null;
            window = new PopupWindow(notification, _displayOrder.Count, () =>
            {
                _slots.Release(lease.Slot, lease.Generation);
                if (window is not null && _windowsBySlot.TryGetValue(lease.Slot, out var activeWindow) && ReferenceEquals(activeWindow, window))
                {
                    _windowsBySlot.Remove(lease.Slot);
                    if (_keysBySlot.Remove(lease.Slot, out var activeKey))
                    {
                        _popupsByKey.Remove(activeKey);
                    }
                    _displayOrder.Remove(lease.Slot);
                }
            });
            _windowsBySlot[lease.Slot] = window;
            _popupsByKey[key] = new ActivePopup(lease);
            _keysBySlot[lease.Slot] = key;
            _displayOrder.Add(lease.Slot);
            window.Show();
            ReflowDisplayOrder();
        }).Task;
    }

    private void MoveToFront(int slot)
    {
        _displayOrder.Remove(slot);
        _displayOrder.Insert(0, slot);
        ReflowDisplayOrder();
    }

    private void ReflowDisplayOrder()
    {
        for (var index = 0; index < _displayOrder.Count; index++)
        {
            if (_windowsBySlot.TryGetValue(_displayOrder[index], out var window))
            {
                window.MoveToSlot(index);
            }
        }
    }

    private sealed record ActivePopup(NotificationSlotLease Lease);
}
