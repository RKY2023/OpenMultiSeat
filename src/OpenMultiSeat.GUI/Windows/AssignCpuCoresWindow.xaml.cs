using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// Lets an administrator restrict which logical CPU cores each seat's processes may run on.
/// Reads and writes seat configuration directly through <see cref="ISeatPersistence"/> — the
/// same on-disk store (%AppData%\OpenMultiSeat\seats.json) the Service's SeatManager uses —
/// since there is no IPC channel between the GUI and the Service today for any feature.
/// </summary>
public partial class AssignCpuCoresWindow : Window
{
    private readonly ISeatPersistence _persistence;
    private readonly ICpuAffinityProvider _cpuAffinityProvider;
    private readonly Dictionary<string, List<CheckBox>> _checkboxesBySeat = [];
    private IReadOnlyList<Seat> _seats = [];

    public AssignCpuCoresWindow()
    {
        InitializeComponent();

        _persistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        _cpuAffinityProvider = new CpuAffinityProvider();

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _seats = await _persistence.GetAllSeatsAsync();

        if (_seats.Count == 0)
        {
            EmptyStateText.Visibility = Visibility.Visible;
            MatrixScrollViewer.Visibility = Visibility.Collapsed;
            SaveButton.IsEnabled = false;
            return;
        }

        BuildMatrix();
    }

    private void BuildMatrix()
    {
        var coreCount = _cpuAffinityProvider.LogicalCoreCount;
        MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        for (var c = 0; c < coreCount; c++)
            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });

        // Header row
        MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var c = 0; c < coreCount; c++)
        {
            var header = new TextBlock
            {
                Text = $"CPU {c}",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4)
            };
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, c + 1);
            MatrixGrid.Children.Add(header);
        }

        // One row per seat
        for (var s = 0; s < _seats.Count; s++)
        {
            var seat = _seats[s];
            var rowIndex = s + 1;
            MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new TextBlock
            {
                Text = seat.Name,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4)
            };
            Grid.SetRow(nameLabel, rowIndex);
            Grid.SetColumn(nameLabel, 0);
            MatrixGrid.Children.Add(nameLabel);

            var seatCheckboxes = new List<CheckBox>(coreCount);
            var allowedCores = seat.CpuCoreAffinity.Count == 0
                ? null // unrestricted -> every box starts checked
                : new HashSet<int>(seat.CpuCoreAffinity);

            for (var c = 0; c < coreCount; c++)
            {
                var checkbox = new CheckBox
                {
                    IsChecked = allowedCores == null || allowedCores.Contains(c),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(4)
                };
                Grid.SetRow(checkbox, rowIndex);
                Grid.SetColumn(checkbox, c + 1);
                MatrixGrid.Children.Add(checkbox);
                seatCheckboxes.Add(checkbox);
            }

            _checkboxesBySeat[seat.Id] = seatCheckboxes;
        }
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        // A seat's CpuCoreAffinity is empty for two very different reasons: "every box is
        // checked" (explicitly unrestricted) and "every box is unchecked" (nothing selected).
        // Both would collapse to the same empty list, but Windows itself requires a process
        // affinity mask to have at least one bit set (SetProcessAffinityMask fails on 0) — so
        // "zero cores checked" isn't a valid state to save at all, not just an ambiguous one.
        var seatsWithNoCoresChecked = _seats
            .Where(seat => _checkboxesBySeat[seat.Id].All(cb => cb.IsChecked != true))
            .Select(seat => seat.Name)
            .ToList();

        if (seatsWithNoCoresChecked.Count > 0)
        {
            MessageBox.Show(
                "These seats have no CPU cores checked, which isn't a valid affinity — a seat needs " +
                "at least one core to run on:\n\n" + string.Join("\n", seatsWithNoCoresChecked) +
                "\n\nCheck at least one core for each of these seats (or check every core for " +
                "\"no restriction\") before saving.",
                "Assign CPU Cores", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var seat in _seats)
        {
            var checkboxes = _checkboxesBySeat[seat.Id];
            var selectedCores = checkboxes
                .Select((cb, index) => (cb, index))
                .Where(x => x.cb.IsChecked == true)
                .Select(x => x.index)
                .ToList();

            // Every core checked is equivalent to "no restriction" — store as empty so the
            // session launcher skips the affinity call entirely (matches Windows' own default).
            seat.CpuCoreAffinity = selectedCores.Count == _cpuAffinityProvider.LogicalCoreCount
                ? []
                : selectedCores;

            await _persistence.SaveSeatAsync(seat);
        }

        MessageBox.Show(
            "CPU core assignments saved. They take effect the next time each seat's session starts.",
            "Assign CPU Cores", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
