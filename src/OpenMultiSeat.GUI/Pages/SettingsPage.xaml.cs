using System.Windows;
using System.Windows.Controls;
using OpenMultiSeat.GUI.Windows;

namespace OpenMultiSeat.GUI.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Configure Settings", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnAssignCpuCores(object sender, RoutedEventArgs e)
    {
        var window = new AssignCpuCoresWindow
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
    }
}
