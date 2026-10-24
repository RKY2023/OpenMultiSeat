using System.Windows;
using System.Windows.Controls;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// ASTER's "Workplace Tab Settings" window, scoped to what actually applies to OpenMultiSeat's
/// current grid-based (not tile-based) Seats/Devices/Displays pages — see
/// docs/control-panel/workplace-tab-settings.md for the full comparison against ASTER's icon-size
/// and tile-distribution options, which have no equivalent here.
/// </summary>
public partial class WorkplaceTabSettingsWindow : Window
{
    private readonly IWorkplaceViewSettingsPersistence _persistence;

    public WorkplaceTabSettingsWindow(IWorkplaceViewSettingsPersistence persistence, WorkplaceViewSettings current)
    {
        InitializeComponent();
        _persistence = persistence;

        ShowUnassignedDisplaysCheckBox.IsChecked = current.ShowUnassignedDisplays;
        HighlightSecondsSlider.Value = current.NewDeviceHighlightSeconds;
        HighlightSecondsText.Text = current.NewDeviceHighlightSeconds.ToString();

        HighlightSecondsSlider.ValueChanged += (_, e) => HighlightSecondsText.Text = ((int)e.NewValue).ToString();
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        var settings = new WorkplaceViewSettings
        {
            ShowUnassignedDisplays = ShowUnassignedDisplaysCheckBox.IsChecked == true,
            NewDeviceHighlightSeconds = (int)HighlightSecondsSlider.Value
        };

        try
        {
            await _persistence.SaveAsync(settings);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Couldn't save: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
