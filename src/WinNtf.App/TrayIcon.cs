using System.Drawing;
using System.Drawing.Drawing2D;

namespace WinNtf.App;

internal static class TrayIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var background = new LinearGradientBrush(
            new Rectangle(0, 0, 32, 32),
            Color.FromArgb(255, 16, 24, 39),
            Color.FromArgb(255, 30, 41, 59),
            LinearGradientMode.ForwardDiagonal);
        graphics.FillEllipse(background, 2, 2, 28, 28);

        using var accent = new SolidBrush(Color.FromArgb(255, 96, 165, 250));
        graphics.FillEllipse(accent, 6, 6, 20, 20);

        using var cutout = new SolidBrush(Color.FromArgb(255, 16, 24, 39));
        graphics.FillEllipse(cutout, 11, 11, 10, 10);

        using var spark = new SolidBrush(Color.White);
        graphics.FillEllipse(spark, 21, 7, 4, 4);

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
