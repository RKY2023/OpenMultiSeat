using System.Windows;

namespace OpenMultiSeat.GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadDashboard();
    }

    private void OnNavigateDashboard(object sender, RoutedEventArgs e)
    {
        LoadDashboard();
    }

    private void OnNavigateDevices(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.DevicesPage());
        StatusText.Text = "Devices - Hardware enumeration and configuration";
    }

    private void OnNavigateSeats(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.SeatsPage());
        StatusText.Text = "Seats - Multi-seat configuration and management";
    }

    private void OnNavigateDisplays(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.DisplaysPage());
        StatusText.Text = "Displays - Monitor assignment and topology";
    }

    private void OnNavigateInput(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.InputPage());
        StatusText.Text = "Input Isolation - Keyboard and mouse assignment";
    }

    private void OnNavigateAudio(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.AudioPage());
        StatusText.Text = "Audio - Sound device routing to seats";
    }

    private void OnNavigateSettings(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.SettingsPage());
        StatusText.Text = "Settings - Application preferences";
    }

    private void OnNavigateAbout(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.AboutPage());
        StatusText.Text = "About - Version and system information";
    }

    private void LoadDashboard()
    {
        ContentFrame.Navigate(new Pages.DashboardPage());
        StatusText.Text = "Dashboard - System overview and status";
    }
}
