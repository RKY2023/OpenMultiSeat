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

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _persistence = new SeatPersistence(loggerFactory.CreateLogger<SeatPersistence>());
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
