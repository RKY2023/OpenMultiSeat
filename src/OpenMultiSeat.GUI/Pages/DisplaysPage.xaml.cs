using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI.Pages;

public partial class DisplaysPage : Page
{
    public DisplaysPage()
    {
        InitializeComponent();
    }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Configure Displays", "Displays", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
