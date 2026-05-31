using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WinNtf.Core;
using WinNtf.Core.Positioning;

namespace WinNtf.App;

public partial class PopupWindow : Window
{
    private readonly Action _onClosed;
    private readonly DispatcherTimer? _closeTimer;
    private bool _closed;

    public PopupWindow(NormalizedNotification notification, int slot, Action onClosed)
    {
        InitializeComponent();
        _onClosed = onClosed;

        TitleText.Text = notification.Title;
        BodyText.Text = notification.Text;
        IconText.Text = IconFor(notification.Variant);
        StateBorder.BorderBrush = BrushFor(notification.Color);
        IconContainer.Background = TintedBrushFor(notification.Color);

        SourceInitialized += (_, _) => PreventActivation();
        Loaded += (_, _) => ApplyPosition(notification.Position, slot);
        Closed += (_, _) => ReleaseOnce();

        if (notification.DurationMs > 0)
        {
            _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(notification.DurationMs) };
            _closeTimer.Tick += (_, _) => Close();
            _closeTimer.Start();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void PreventActivation()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(handle, GwlExStyle);
        SetWindowLong(handle, GwlExStyle, style | WsExNoActivate | WsExToolWindow);
    }

    private void ApplyPosition(NotificationPosition position, int slot)
    {
        var workArea = SystemParameters.WorkArea;
        var calculated = PositionCalculator.Calculate(slot, new PositionConfig(
            position,
            Width: (int)Width,
            Height: (int)Height,
            ScreenWidth: (int)workArea.Width,
            ScreenHeight: (int)workArea.Height,
            ScreenLeft: (int)workArea.Left,
            ScreenTop: (int)workArea.Top));

        Left = calculated.Left;
        Top = calculated.Top;
    }

    private void ReleaseOnce()
    {
        if (_closed)
        {
            return;
        }

        _closeTimer?.Stop();
        _closed = true;
        _onClosed();
    }

    private static string IconFor(NotificationVariant variant) =>
        variant switch
        {
            NotificationVariant.Success => "✅",
            NotificationVariant.Warning => "⚠️",
            NotificationVariant.Error => "❌",
            NotificationVariant.Tool => "🛠️",
            _ => "ℹ️"
        };

    private static System.Windows.Media.Brush BrushFor(string color) =>
        (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(color)!;

    private static System.Windows.Media.Brush TintedBrushFor(string color)
    {
        var parsed = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x26, parsed.R, parsed.G, parsed.B));
    }

    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

}
