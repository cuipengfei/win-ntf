using System.Windows;

namespace WinNtf.App;

public partial class StartupFailureWindow : Window
{
    public StartupFailureWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void ExitClicked(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
}
