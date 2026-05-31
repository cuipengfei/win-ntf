using WinNtf.Core;
using WinNtf.Server;

namespace WinNtf.Core.Tests;

public static class LocalHttpServerTests
{
    public static async Task ConstructorRejectsNonLoopbackHost()
    {
        TestAssert.Throws<ArgumentException>(() => new LocalHttpServer(
            new LocalHttpServerOptions(9876, "0.0.0.0"),
            new NotificationNormalizer(),
            new CapturingSink()));
        await Task.CompletedTask;
    }

    public static async Task HealthReturnsOk()
    {
        var port = FreeTcpPort();
        await using var app = new LocalHttpServer(
            new LocalHttpServerOptions(port),
            new NotificationNormalizer(),
            new CapturingSink()).Build();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        TestAssert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        TestAssert.Equal("ok", body);
    }

    public static async Task NotifyInvokesSinkWithNormalizedNotification()
    {
        var port = FreeTcpPort();
        var sink = new CapturingSink();
        await using var app = new LocalHttpServer(
            new LocalHttpServerOptions(port),
            new NotificationNormalizer(),
            sink).Build();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var response = await client.PostAsync("/notify", Json("{\"title\":\"t\",\"text\":\"**done**\",\"variant\":\"success\"}"));

        TestAssert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        TestAssert.NotNull(sink.Last);
        TestAssert.Equal("t", sink.Last!.Title);
        TestAssert.Equal("done", sink.Last.Text);
        TestAssert.Equal("#4ADE80", sink.Last.Color);
    }

    public static async Task NotifyRejectsEmptyText()
    {
        var port = FreeTcpPort();
        await using var app = new LocalHttpServer(
            new LocalHttpServerOptions(port),
            new NotificationNormalizer(),
            new CapturingSink()).Build();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var response = await client.PostAsync("/notify", Json("{\"text\":\"   \"}"));

        TestAssert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }


    public static async Task NotifyAcceptsLowercaseVariant()
    {
        var port = FreeTcpPort();
        var sink = new CapturingSink();
        await using var app = new LocalHttpServer(
            new LocalHttpServerOptions(port),
            new NotificationNormalizer(),
            sink).Build();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var response = await client.PostAsync("/notify", Json("{\"title\":\"t\",\"text\":\"done\",\"variant\":\"success\"}"));

        TestAssert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        TestAssert.NotNull(sink.Last);
        TestAssert.Equal(NotificationVariant.Success, sink.Last!.Variant);
    }


    public static async Task NotifyAcceptsKebabCasePosition()
    {
        var port = FreeTcpPort();
        var sink = new CapturingSink();
        await using var app = new LocalHttpServer(
            new LocalHttpServerOptions(port),
            new NotificationNormalizer(),
            sink).Build();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var response = await client.PostAsync("/notify", Json("{\"title\":\"t\",\"text\":\"done\",\"position\":\"bottom-right\"}"));

        TestAssert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        TestAssert.NotNull(sink.Last);
        TestAssert.Equal(NotificationPosition.BottomRight, sink.Last!.Position);
    }

    public static async Task NotifyRejectsOversizedPayload()
    {
        var port = FreeTcpPort();
        await using var app = new LocalHttpServer(
            new LocalHttpServerOptions(port),
            new NotificationNormalizer(),
            new CapturingSink()).Build();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var largeText = new string('x', (int)LocalHttpServer.MaxRequestBodyBytes + 1024);
        var response = await client.PostAsync("/notify", Json("{\"text\":\"" + largeText + "\"}"));

        TestAssert.Equal(System.Net.HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    private static StringContent Json(string json) => new(json, System.Text.Encoding.UTF8, "application/json");

    private static int FreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class CapturingSink : INotificationSink
    {
        public NormalizedNotification? Last { get; private set; }

        public Task ShowAsync(NormalizedNotification notification, CancellationToken cancellationToken)
        {
            Last = notification;
            return Task.CompletedTask;
        }
    }
}
