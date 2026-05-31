using System.Text.RegularExpressions;

namespace WinNtf.Core;

public sealed class NotificationNormalizer
{
    public const string DefaultTitle = "win-ntf";
    public const int DefaultDurationMs = 10_000;
    public const int MaxTextLength = 400;

    private static readonly Regex MarkdownMarkers = new("[`*#]", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public NormalizedNotification Normalize(NotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new NotificationValidationException("text is required");
        }

        var variant = request.Variant ?? NotificationVariant.Info;
        var durationMs = request.Persistent ? 0 : request.DurationMs ?? DefaultDurationMs;

        if (durationMs < 0)
        {
            throw new NotificationValidationException("durationMs must be greater than or equal to 0");
        }

        return new NormalizedNotification(
            Title: string.IsNullOrWhiteSpace(request.Title) ? DefaultTitle : request.Title.Trim(),
            Text: Truncate(CleanMarkdown(request.Text), MaxTextLength),
            Variant: variant,
            Color: IsHexColor(request.Color) ? request.Color! : DefaultColorFor(variant),
            DurationMs: durationMs,
            Position: request.Position ?? NotificationPosition.TopRight,
            Persistent: request.Persistent);
    }

    public static string CleanMarkdown(string text)
    {
        var withoutMarkers = MarkdownMarkers.Replace(text, string.Empty);
        return Whitespace.Replace(withoutMarkers, " ").Trim();
    }

    public static string Truncate(string text, int maxChars)
    {
        if (maxChars < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChars));
        }

        return text.Length <= maxChars ? text : $"{text[..maxChars]}...";
    }

    public static string DefaultColorFor(NotificationVariant variant) =>
        variant switch
        {
            NotificationVariant.Success => "#4ADE80",
            NotificationVariant.Warning => "#FBBF24",
            NotificationVariant.Error => "#EF4444",
            NotificationVariant.Tool => "#60A5FA",
            _ => "#60A5FA"
        };

    private static bool IsHexColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return false;
        }

        return Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$");
    }
}
