using System.Windows;
using System.IO;
using WinNtf.Core;
using WinNtf.Server;

namespace WinNtf.App;

public partial class App : System.Windows.Application
{
    private SingleInstanceGuard? _singleInstance;
    private LocalHttpServer? _server;
    private TrayController? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceGuard();
        if (!_singleInstance.IsPrimaryInstance)
        {
            Shutdown();
            return;
        }

        try
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(CurrentExecutablePath())!);

            var configPath = AppConfigStore.DefaultPath();
            var config = new AppConfigStore(configPath).LoadOrCreate();
            new RegistryStartupManager().SetEnabled(config.StartOnLogin, CurrentExecutablePath());

            var sink = new PopupPresenter();
            _server = new LocalHttpServer(
                new LocalHttpServerOptions(config.Port),
                new NotificationNormalizer(),
                sink);
            _ = _server.StartAsync();

            _tray = new TrayController(
                config,
                configPath,
                () => _ = sink.ShowAsync(new NormalizedNotification(
                    "win-ntf",
                    "Local notifier is alive",
                    NotificationVariant.Success,
                    NotificationNormalizer.DefaultColorFor(NotificationVariant.Success),
                    NotificationNormalizer.DefaultDurationMs,
                    config.DefaultPosition,
                    Persistent: false), CancellationToken.None),
                DismissAllPopups,
                Shutdown);
        }
        catch (Exception ex)
        {
            new StartupFailureWindow(ex.Message).Show();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _singleInstance?.Dispose();

        if (_server is not null)
        {
            await _server.DisposeAsync();
        }


        base.OnExit(e);
    }

    private static string CurrentExecutablePath() =>
        Environment.ProcessPath
        ?? throw new InvalidOperationException("Cannot resolve current executable path");

    private static void DismissAllPopups()
    {
        foreach (var window in Current.Windows.OfType<PopupWindow>().ToList())
        {
            window.Close();
        }
    }
}
