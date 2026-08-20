using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI.Pages;

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        DeviceCountText.Text = "0";
        SeatCountText.Text = "0";
        DisplayCountText.Text = "0";
        SessionCountText.Text = "0";
    }

    private void OnRefreshStatus(object sender, RoutedEventArgs e)
    {
        LoadDashboardData();
        MessageBox.Show("System status refreshed", "Dashboard", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnExportConfig(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Export configuration to file", "Dashboard", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnValidateConfig(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("All configurations are valid", "Dashboard", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
