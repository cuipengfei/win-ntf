using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using WinNtf.Core;
using WinNtf.Server;

namespace WinNtf.App;

public partial class App : System.Windows.Application
{
    private SingleInstanceGuard? _singleInstance;
    private WebApplication? _server;
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
            var configPath = AppConfigStore.DefaultPath();
            var config = new AppConfigStore(configPath).LoadOrCreate();
            new RegistryStartupManager().SetEnabled(config.StartOnLogin, CurrentExecutablePath());

            var sink = new PopupPresenter();
            _server = new LocalHttpServer(
                new LocalHttpServerOptions(config.Port),
                new NotificationNormalizer(),
                sink).Build();
            _server.Start();

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
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _server.StopAsync(stopTimeout.Token);
            await _server.DisposeAsync();
        }


        base.OnExit(e);
    }

    private static string CurrentExecutablePath() =>
        Environment.ProcessPath
        ?? throw new InvalidOperationException("Cannot resolve current executable path");
}
