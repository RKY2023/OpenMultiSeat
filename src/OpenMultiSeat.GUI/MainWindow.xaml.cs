using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI;

public partial class MainWindow : Window
{
    private Button[] _navButtons = [];

    public MainWindow()
    {
        InitializeComponent();
        _navButtons = [DashboardBtn, DevicesBtn, SeatsBtn, SystemBtn, DisplaysBtn, InputBtn, AudioBtn, SettingsBtn, AboutBtn];
        LoadDashboard();
    }

    private void SetActiveNav(Button active)
    {
        foreach (var button in _navButtons)
        {
            button.Tag = ReferenceEquals(button, active) ? "Active" : null;
        }
    }

    private void OnNavigateDashboard(object sender, RoutedEventArgs e)
    {
        LoadDashboard();
    }

    private void OnNavigateDevices(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.DevicesPage());
        StatusText.Text = "Devices - Hardware enumeration and configuration";
        SetActiveNav(DevicesBtn);
    }

    private void OnNavigateSeats(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.SeatsPage());
        StatusText.Text = "Seats - Multi-seat configuration and management";
        SetActiveNav(SeatsBtn);
    }

    private void OnNavigateSystem(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.SystemPage());
        StatusText.Text = "System - Every device, display, and audio endpoint across all seats";
        SetActiveNav(SystemBtn);
    }

    private void OnNavigateDisplays(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.DisplaysPage());
        StatusText.Text = "Displays - Monitor assignment and topology";
        SetActiveNav(DisplaysBtn);
    }

    private void OnNavigateInput(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.InputPage());
        StatusText.Text = "Input Isolation - Keyboard and mouse assignment";
        SetActiveNav(InputBtn);
    }

    private void OnNavigateAudio(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.AudioPage());
        StatusText.Text = "Audio - Sound device routing to seats";
        SetActiveNav(AudioBtn);
    }

    private void OnNavigateSettings(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.SettingsPage());
        StatusText.Text = "Settings - Application preferences";
        SetActiveNav(SettingsBtn);
    }

    private void OnNavigateAbout(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new Pages.AboutPage());
        StatusText.Text = "About - Version and system information";
        SetActiveNav(AboutBtn);
    }

    private void LoadDashboard()
    {
        ContentFrame.Navigate(new Pages.DashboardPage());
        StatusText.Text = "Dashboard - System overview and status";
        SetActiveNav(DashboardBtn);
    }
}
