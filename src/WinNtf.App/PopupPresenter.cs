using System.Windows;
using WinNtf.Core;
using WinNtf.Server;

namespace WinNtf.App;

public sealed class PopupPresenter : INotificationSink
{
    private readonly NotificationSlotQueue _slots;

    public PopupPresenter(int maxVisible = 3)
    {
        _slots = new NotificationSlotQueue(maxVisible);
    }

    public Task ShowAsync(NormalizedNotification notification, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        var slot = _slots.TryAcquire();
        if (slot is null)
        {
            return Task.CompletedTask;
        }

        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = new PopupWindow(notification, slot.Value, () => _slots.Release(slot.Value));
            window.Show();
        }).Task;
    }
}
