using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI.Pages;

public partial class DevicesPage : Page
{
    public DevicesPage()
    {
        InitializeComponent();
    }

    private void OnScanDevices(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Scanning for input devices...", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Device list refreshed", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnExportReport(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Exporting device report...", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
