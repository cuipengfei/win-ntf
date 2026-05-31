using Microsoft.Win32;

namespace WinNtf.App;

public sealed class RegistryStartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "win-ntf";

    public void SetEnabled(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            key.SetValue(ValueName, Quote(executablePath));
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public static string Quote(string path) => $"\"{path}\"";
}
