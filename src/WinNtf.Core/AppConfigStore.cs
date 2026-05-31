using System.Text.Json;

namespace WinNtf.Core;

public sealed class AppConfigStore
{
    private readonly string _path;

    public AppConfigStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("config path is required", nameof(path));
        }

        _path = path;
    }

    public AppConfig LoadOrCreate()
    {
        if (!File.Exists(_path))
        {
            var defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        var json = File.ReadAllText(_path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, AppConfig.JsonOptions)
            ?? throw new NotificationValidationException("config is empty");

        config.Validate();
        return config;
    }

    public void Save(AppConfig config)
    {
        config.Validate();
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(config, AppConfig.JsonOptions));
    }

    public static string DefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "win-ntf", "config.json");
    }
}
