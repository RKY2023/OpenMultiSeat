using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI.Pages;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Configure About", "About", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
