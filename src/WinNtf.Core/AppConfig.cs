using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinNtf.Core;

public sealed record AppConfig(
    int Port = 9876,
    bool StartOnLogin = true,
    NotificationPosition DefaultPosition = NotificationPosition.TopRight,
    int MaxVisible = 10)
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public void Validate()
    {
        if (Port is < 1 or > 65535)
        {
            throw new NotificationValidationException("port must be between 1 and 65535");
        }

        if (MaxVisible < 1)
        {
            throw new NotificationValidationException("maxVisible must be at least 1");
        }
    }
}
