using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using WinNtf.Core;

namespace WinNtf.App;

public sealed class TrayController : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly AppConfig _config;
    private readonly string _configPath;
    private bool _disposed;

    public TrayController(AppConfig config, string configPath, Action showTestNotification, Action exit)
    {
        _config = config;
        _configPath = configPath;

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = $"win-ntf alive on 127.0.0.1:{config.Port}",
            Visible = true,
            ContextMenuStrip = BuildMenu(showTestNotification, exit)
        };
    }

    private Forms.ContextMenuStrip BuildMenu(Action showTestNotification, Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(new Forms.ToolStripMenuItem($"win-ntf alive: 127.0.0.1:{_config.Port}") { Enabled = false });
        menu.Items.Add(new Forms.ToolStripMenuItem($"Start on login: {(_config.StartOnLogin ? "on" : "off")}") { Enabled = false });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Show test notification", null, (_, _) => showTestNotification()));
        menu.Items.Add(new Forms.ToolStripMenuItem("Open config folder", null, (_, _) => OpenConfigFolder()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Exit", null, (_, _) => exit()));
        return menu;
    }

    private void OpenConfigFolder()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _disposed = true;
    }
}
