using System.Windows;
using System.Windows.Controls;

namespace OpenMultiSeat.GUI.Pages;

public partial class AudioPage : Page
{
    public AudioPage()
    {
        InitializeComponent();
    }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Configure Audio", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
