using WinNtf.Core;

namespace WinNtf.Server;

public interface INotificationSink
{
    Task ShowAsync(NormalizedNotification notification, CancellationToken cancellationToken);
}
