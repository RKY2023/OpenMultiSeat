using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI.Pages;

public partial class InputPage : Page
{
    public InputPage()
    {
        InitializeComponent();
    }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Configure Input", "Input", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
