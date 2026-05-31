using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinNtf.Core;

namespace WinNtf.Server;

public sealed class LocalHttpServer
{
    public const long MaxRequestBodyBytes = 16 * 1024;
    private readonly LocalHttpServerOptions _options;
    private readonly NotificationNormalizer _normalizer;
    private readonly INotificationSink _sink;

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
    }

    public WebApplication Build()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(_options.Url);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new KebabCasePositionJsonConverter());
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        });

        var app = builder.Build();

        app.MapGet("/health", () => Results.Text("ok"));

        app.MapPost("/notify", async (NotificationRequest request, CancellationToken cancellationToken) =>
        {
            try
            {
                var notification = _normalizer.Normalize(request);
                await _sink.ShowAsync(notification, cancellationToken);
                return Results.Accepted();
            }
            catch (NotificationValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }
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
