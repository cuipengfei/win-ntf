using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinNtf.Core;

namespace WinNtf.Server;

public sealed class LocalHttpServer : IAsyncDisposable
{
    public const long MaxRequestBodyBytes = 16 * 1024;

    private readonly LocalHttpServerOptions _options;
    private readonly NotificationNormalizer _normalizer;
    private readonly INotificationSink _sink;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);
    private TcpListener? _listener;
    private CancellationTokenSource? _stop;
    private Task? _listenTask;

    public LocalHttpServer(
        LocalHttpServerOptions options,
        NotificationNormalizer normalizer,
        INotificationSink sink)
    {
        if (options.Host != "127.0.0.1")
        {
            throw new ArgumentException("win-ntf only supports 127.0.0.1 binding", nameof(options));
        }

        _options = options;
        _normalizer = normalizer;
        _sink = sink;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new KebabCasePositionJsonConverter());
        _jsonOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }

    public Task StartAsync()
    {
        if (_listener is not null)
        {
            return Task.CompletedTask;
        }

        _stop = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Parse(_options.Host), _options.Port);
        _listener.Start();
        _listenTask = ListenAsync(_stop.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_listener is null)
        {
            return;
        }

        _stop?.Cancel();
        _listener.Stop();

        if (_listenTask is not null)
        {
            try
            {
                await _listenTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _listener = null;
        _listenTask = null;
        _stop?.Dispose();
        _stop = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(cancellationToken);
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _ = HandleClientAsync(client, cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var _ = client;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            var stream = client.GetStream();
            var request = await ReadRequestAsync(stream, timeout.Token);

            if (request is null)
            {
                await WriteResponseAsync(stream, 400, "Bad Request", null, timeout.Token);
                return;
            }

            if (request.Method == "GET" && request.Path == "/health")
            {
                await WriteResponseAsync(stream, 200, "OK", "ok", timeout.Token);
                return;
            }

            if (request.Method == "POST" && request.Path == "/notify")
            {
                if (request.BodyTooLarge)
                {
                    await WriteResponseAsync(stream, 413, "Payload Too Large", null, timeout.Token);
                    return;
                }

                await HandleNotifyAsync(stream, request.Body, timeout.Token);
                return;
            }

            await WriteResponseAsync(stream, 404, "Not Found", null, timeout.Token);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
        }
    }

    private async Task HandleNotifyAsync(NetworkStream stream, string body, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<NotificationRequest>(body, _jsonOptions);
            if (request is null)
            {
                await WriteJsonAsync(stream, 400, "Bad Request", new { error = "Invalid request" }, cancellationToken);
                return;
            }

            var notification = _normalizer.Normalize(request);
            await _sink.ShowAsync(notification, cancellationToken);
            await WriteResponseAsync(stream, 202, "Accepted", null, cancellationToken);
        }
        catch (NotificationValidationException ex)
        {
            await WriteJsonAsync(stream, 400, "Bad Request", new { error = ex.Message }, cancellationToken);
        }
        catch (JsonException)
        {
            await WriteJsonAsync(stream, 400, "Bad Request", new { error = "Invalid JSON" }, cancellationToken);
        }
    }

    private static async Task<HttpRequest?> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxRequestBodyBytes + 8192];
        var received = 0;

        while (received < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(received, buffer.Length - received), cancellationToken);
            if (read == 0)
            {
                break;
            }

            received += read;
            var headerEnd = FindHeaderEnd(buffer, received);
            if (headerEnd < 0)
            {
                continue;
            }

            var headerText = Encoding.ASCII.GetString(buffer, 0, headerEnd);
            var lines = headerText.Split("\r\n");
            if (lines.Length == 0)
            {
                return null;
            }

            var start = lines[0].Split(' ');
            if (start.Length < 2)
            {
                return null;
            }

            var contentLength = ContentLength(lines);
            if (contentLength > MaxRequestBodyBytes)
            {
                return new HttpRequest(start[0], PathOnly(start[1]), Body: string.Empty, BodyTooLarge: true);
            }

            var bodyStart = headerEnd + 4;
            while (received - bodyStart < contentLength)
            {
                var readBody = await stream.ReadAsync(buffer.AsMemory(received, buffer.Length - received), cancellationToken);
                if (readBody == 0)
                {
                    return null;
                }

                received += readBody;
            }

            var body = contentLength == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, bodyStart, contentLength);
            return new HttpRequest(start[0], PathOnly(start[1]), body, BodyTooLarge: false);
        }

        return null;
    }

    private static int FindHeaderEnd(byte[] buffer, int length)
    {
        for (var i = 3; i < length; i++)
        {
            if (buffer[i - 3] == '\r' && buffer[i - 2] == '\n' && buffer[i - 1] == '\r' && buffer[i] == '\n')
            {
                return i - 3;
            }
        }

        return -1;
    }

    private static int ContentLength(string[] lines)
    {
        foreach (var line in lines)
        {
            var separator = line.IndexOf(':');
            if (separator < 0)
            {
                continue;
            }

            if (string.Equals(line[..separator], "Content-Length", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(line[(separator + 1)..].Trim(), out var length))
            {
                return length;
            }
        }

        return 0;
    }

    private static string PathOnly(string target)
    {
        var queryStart = target.IndexOf('?');
        return queryStart < 0 ? target : target[..queryStart];
    }

    private static Task WriteJsonAsync<T>(NetworkStream stream, int statusCode, string reason, T body, CancellationToken cancellationToken) =>
        WriteResponseAsync(stream, statusCode, reason, JsonSerializer.Serialize(body, ResponseJsonOptions), cancellationToken, "application/json; charset=utf-8");

    private static async Task WriteResponseAsync(
        NetworkStream stream,
        int statusCode,
        string reason,
        string? body,
        CancellationToken cancellationToken,
        string contentType = "text/plain; charset=utf-8")
    {
        var bodyBytes = body is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
        var header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            "Connection: close\r\n" +
            "\r\n");

        await stream.WriteAsync(header, cancellationToken);
        if (bodyBytes.Length > 0)
        {
            await stream.WriteAsync(bodyBytes, cancellationToken);
        }
    }

    private sealed record HttpRequest(string Method, string Path, string Body, bool BodyTooLarge);
}

internal sealed class KebabCasePositionJsonConverter : JsonConverter<NotificationPosition>
{
    public override NotificationPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("position must be a string");
        }

        return reader.GetString() switch
        {
            "center" or "Center" => NotificationPosition.Center,
            "top-right" or "TopRight" => NotificationPosition.TopRight,
            "bottom-right" or "BottomRight" => NotificationPosition.BottomRight,
            _ => throw new JsonException("Unsupported notification position")
        };
    }

    public override void Write(Utf8JsonWriter writer, NotificationPosition value, JsonSerializerOptions options)
    {
        var text = value switch
        {
            NotificationPosition.Center => "center",
            NotificationPosition.TopRight => "top-right",
            NotificationPosition.BottomRight => "bottom-right",
            _ => throw new JsonException("Unsupported notification position")
        };
        writer.WriteStringValue(text);
    }
}
