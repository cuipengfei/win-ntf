using WinNtf.Core;

namespace WinNtf.Core.Tests;

public static class AppConfigStoreTests
{
    public static void LoadOrCreateWritesDefaultConfig()
    {
        var path = Path.Combine(Path.GetTempPath(), $"win-ntf-{Guid.NewGuid():N}", "config.json");
        var store = new AppConfigStore(path);

        var config = store.LoadOrCreate();

        TestAssert.Equal(9876, config.Port);
        TestAssert.True(config.StartOnLogin);
        TestAssert.True(File.Exists(path));
        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }

    public static void LoadOrCreateRejectsInvalidPort()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"win-ntf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "config.json");
        File.WriteAllText(path, "{\"port\":70000,\"startOnLogin\":true,\"defaultPosition\":\"TopRight\"}");
        var store = new AppConfigStore(path);

        TestAssert.Throws<NotificationValidationException>(() => store.LoadOrCreate());
        Directory.Delete(directory, recursive: true);
    }
}
