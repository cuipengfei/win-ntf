namespace WinNtf.Core;

public sealed class NotificationValidationException : Exception
{
    public NotificationValidationException(string message) : base(message)
    {
    }
}
