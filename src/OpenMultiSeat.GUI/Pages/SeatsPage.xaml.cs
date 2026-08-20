using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI.Pages;

public partial class SeatsPage : Page
{
    public SeatsPage()
    {
        InitializeComponent();
    }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Configure Seats", "Seats", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
