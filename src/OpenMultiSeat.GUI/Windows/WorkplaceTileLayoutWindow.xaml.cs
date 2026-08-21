using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using OpenMultiSeat.Audio;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.Displays;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// ASTER's Workplaces-tab tile layout: one column per seat plus a "System" column for
/// unassigned resources, built from live data (ISeatManager/IDevicePersistence/
/// IDisplayEnumerator/IAudioManager) rather than a mock-up. Reassignment happens by dragging a
/// tile onto a different column — TileAssignmentDecision (Core) decides whether that's a direct
/// assign, an unassign, or a confirm-then-move, reusing ConfirmDeviceDestinationWindow for the
/// last case exactly like the four dedicated Assign*ToSeatWindow classes already do.
///
/// Two things are deliberately NOT real hardware behavior, stated here rather than left implicit:
/// - "Indicate device" blinks the tile's border in this window only. There's no generic Windows
///   API to flash an arbitrary HID keyboard/mouse/webcam's own indicator LED (some keyboards have
///   a vendor SDK for this; most devices have no addressable indicator at all), so this is a
///   GUI-only "which one is this" aid, not a hardware signal.
/// - Per-workplace status dots reflect Seat.Status, which nothing in this codebase currently
///   updates in real time from an actually-running session (see docs/known-issues.md) — so they
///   reflect the last value SeatManager wrote, not a live poll of whether a process is running.
/// </summary>
public partial class WorkplaceTileLayoutWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly IDevicePersistence _devicePersistence;
    private readonly IDisplayEnumerator _displayEnumerator;
    private readonly IAudioManager _audioManager;

    private Dictionary<string, Seat> _seatsById = new();

    public WorkplaceTileLayoutWindow(
        ISeatManager seatManager,
        IDevicePersistence devicePersistence,
        IDisplayEnumerator displayEnumerator,
        IAudioManager audioManager)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _devicePersistence = devicePersistence;
        _displayEnumerator = displayEnumerator;
        _audioManager = audioManager;

        Loaded += async (_, _) => await LoadAsync();
    }

    private enum TileKind { Display, Keyboard, Mouse, AudioPlayback, AudioCapture, Other }

    private sealed class TileItem
    {
        public required string ResourceId { get; init; }
        public required string Label { get; init; }
        public required TileKind Kind { get; init; }
        public string? CurrentSeatId { get; init; }
    }

    private sealed class TileColumn
    {
        public string? SeatId { get; init; }
        public required string Header { get; init; }
        public SeatStatus? Status { get; init; }
    }

    private async Task LoadAsync()
    {
        ColumnsPanel.Children.Clear();
        StatusText.Text = "Loading...";

        var seats = await _seatManager.GetAllSeatsAsync();
        _seatsById = seats.ToDictionary(s => s.Id);

        var devices = await _devicePersistence.GetAllDevicesAsync();
        var displays = await _displayEnumerator.EnumerateDisplaysAsync();
        var audioDevices = await _audioManager.GetAudioDevicesAsync();
        var topology = await _audioManager.GetAudioTopologyAsync();

        var deviceSeat = new Dictionary<string, string>();
        var displaySeat = new Dictionary<string, string>();
        var audioSeat = new Dictionary<string, string>();
        foreach (var seat in seats)
        {
            foreach (var id in seat.KeyboardIds.Concat(seat.MouseIds).Concat(seat.OtherDeviceIds))
                deviceSeat[id] = seat.Id;
            foreach (var id in seat.DisplayIds)
                displaySeat[id] = seat.Id;
            if (topology.SeatAudioMapping.TryGetValue(seat.Id, out var mapping))
                foreach (var id in mapping.PlaybackDevices.Concat(mapping.RecordingDevices))
                    audioSeat[id] = seat.Id;
        }

        var allTiles = new List<TileItem>();

        foreach (var d in devices)
        {
            var kind = d.DeviceType switch
            {
                "Keyboard" => TileKind.Keyboard,
                "Mouse" => TileKind.Mouse,
                _ => TileKind.Other
            };
            allTiles.Add(new TileItem
            {
                ResourceId = d.StableId,
                Label = d.ProductName ?? d.StableId,
                Kind = kind,
                CurrentSeatId = deviceSeat.GetValueOrDefault(d.StableId)
            });
        }

        foreach (var disp in displays)
        {
            allTiles.Add(new TileItem
            {
                ResourceId = disp.DisplayId,
                Label = disp.FriendlyName ?? disp.DeviceName,
                Kind = TileKind.Display,
                CurrentSeatId = displaySeat.GetValueOrDefault(disp.DisplayId)
            });
        }

        foreach (var a in audioDevices)
        {
            allTiles.Add(new TileItem
            {
                ResourceId = a.DeviceId,
                Label = a.FriendlyName,
                Kind = a.Type == AudioDeviceType.Recording ? TileKind.AudioCapture : TileKind.AudioPlayback,
                CurrentSeatId = audioSeat.GetValueOrDefault(a.DeviceId)
            });
        }

        var systemColumn = new TileColumn { SeatId = null, Header = "System" };
        ColumnsPanel.Children.Add(CreateColumn(systemColumn, allTiles.Where(t => t.CurrentSeatId == null).ToList()));

        foreach (var seat in seats)
        {
            var column = new TileColumn { SeatId = seat.Id, Header = seat.Name, Status = seat.Status };
            ColumnsPanel.Children.Add(CreateColumn(column, allTiles.Where(t => t.CurrentSeatId == seat.Id).ToList()));
        }

        StatusText.Text = seats.Count == 0
            ? "No seats yet -- create one on the Seats page, then drag devices here."
            : $"{seats.Count} seat(s), {allTiles.Count} resource(s).";
    }

    // ---- Column construction ----

    private static readonly SolidColorBrush ColumnBorderBrush = new(Color.FromRgb(0x3D, 0x3D, 0x3D));
    private static readonly SolidColorBrush ColumnDropHighlightBrush = new(Color.FromRgb(0x2D, 0x6C, 0xDF));

    private Border CreateColumn(TileColumn column, List<TileItem> items)
    {
        var outer = new Border
        {
            Width = 230,
            Margin = new Thickness(6, 0, 6, 6),
            Padding = new Thickness(10),
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            BorderBrush = ColumnBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Tag = column,
            AllowDrop = true,
            VerticalAlignment = VerticalAlignment.Top
        };
        outer.DragEnter += OnColumnDragEnter;
        outer.DragLeave += OnColumnDragLeave;
        outer.DragOver += OnColumnDragOver;
        outer.Drop += OnColumnDrop;

        var content = new StackPanel();

        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        if (column.SeatId != null)
        {
            headerPanel.Children.Add(new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = StatusDotBrush(column.Status ?? SeatStatus.Disabled),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        headerPanel.Children.Add(new TextBlock
        {
            Text = column.Header,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        content.Children.Add(headerPanel);

        foreach (var group in items.GroupBy(i => i.Kind))
        {
            content.Children.Add(new TextBlock
            {
                Text = GroupLabel(group.Key),
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontSize = 10,
                Margin = new Thickness(0, 6, 0, 2)
            });
            var wrap = new WrapPanel();
            foreach (var item in group)
                wrap.Children.Add(CreateTile(item));
            content.Children.Add(wrap);
        }

        if (items.Count == 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = column.SeatId == null ? "Nothing unassigned." : "No devices/displays assigned.",
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        outer.Child = content;
        return outer;
    }

    private static string GroupLabel(TileKind kind) => kind switch
    {
        TileKind.Display => "Displays",
        TileKind.Keyboard => "Keyboards",
        TileKind.Mouse => "Mice",
        TileKind.AudioPlayback => "Audio (Playback)",
        TileKind.AudioCapture => "Audio (Recording)",
        _ => "Other Devices"
    };

    private static Brush StatusDotBrush(SeatStatus status) => status switch
    {
        SeatStatus.Running => Brushes.LimeGreen,
        SeatStatus.Configured or SeatStatus.Starting or SeatStatus.Recovering => Brushes.Orange,
        SeatStatus.Error or SeatStatus.LoggedOff => Brushes.Red,
        _ => Brushes.Gray
    };

    // ---- Tile construction ----

    private Border CreateTile(TileItem item)
    {
        var border = new Border
        {
            Width = 76,
            Height = 58,
            Margin = new Thickness(3),
            CornerRadius = new CornerRadius(4),
            Background = TileBackground(item.Kind),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(2),
            Tag = item,
            Cursor = Cursors.Hand,
            ToolTip = item.Label
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(new TextBlock
        {
            Text = TileGlyph(item.Kind),
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.White
        });
        stack.Children.Add(new TextBlock
        {
            Text = item.Label,
            FontSize = 9,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 70,
            Margin = new Thickness(0, 2, 0, 0)
        });
        border.Child = stack;

        border.ContextMenu = BuildTileContextMenu(border);

        Point? dragStart = null;
        border.PreviewMouseLeftButtonDown += (_, e) => dragStart = e.GetPosition(null);
        border.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed || dragStart is not { } start)
                return;

            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            dragStart = null;
            DragDrop.DoDragDrop(border, item, DragDropEffects.Move);
        };

        return border;
    }

    private static string TileGlyph(TileKind kind) => kind switch
    {
        TileKind.Display => "\U0001F5A5",
        TileKind.Keyboard => "⌨",
        TileKind.Mouse => "\U0001F5B1",
        TileKind.AudioPlayback => "\U0001F50A",
        TileKind.AudioCapture => "\U0001F3A4",
        _ => "\U0001F50C"
    };

    private static Brush TileBackground(TileKind kind) => kind switch
    {
        TileKind.Display => new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
        TileKind.Keyboard or TileKind.Mouse => new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D)),
        TileKind.AudioPlayback or TileKind.AudioCapture => new SolidColorBrush(Color.FromRgb(0x24, 0x3A, 0x4A)),
        _ => new SolidColorBrush(Color.FromRgb(0x35, 0x2A, 0x40))
    };

    // ---- "Indicate device" blink ----

    private static readonly SolidColorBrush IndicateBrush = new(Color.FromRgb(0xFF, 0xD5, 0x1A));

    private ContextMenu BuildTileContextMenu(Border border)
    {
        var menu = new ContextMenu();
        var indicate = new MenuItem { Header = "Indicate device" };
        indicate.Click += (_, _) => BlinkTile(border);
        menu.Items.Add(indicate);
        return menu;
    }

    /// <summary>Alternates the tile's border between its normal (transparent) state and a bright
    /// highlight for a few seconds, then restores it -- a GUI-only "which one is this" aid, not a
    /// real hardware indicator (see the class doc comment).</summary>
    private void BlinkTile(Border border)
    {
        var originalBrush = border.BorderBrush;
        var originalThickness = border.BorderThickness;
        var on = false;
        var ticks = 0;
        const int maxTicks = 8; // 8 * 400ms = ~3.2s, an even number so it always ends "off"

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        timer.Tick += (_, _) =>
        {
            on = !on;
            border.BorderBrush = on ? IndicateBrush : originalBrush;
            border.BorderThickness = on ? new Thickness(3) : originalThickness;
            ticks++;
            if (ticks < maxTicks)
                return;

            timer.Stop();
            border.BorderBrush = originalBrush;
            border.BorderThickness = originalThickness;
        };
        timer.Start();
    }

    // ---- Drag & drop ----

    private void OnColumnDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border b && e.Data.GetDataPresent(typeof(TileItem)))
            b.BorderBrush = ColumnDropHighlightBrush;
    }

    private void OnColumnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border b)
            b.BorderBrush = ColumnBorderBrush;
    }

    private void OnColumnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(TileItem)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnColumnDrop(object sender, DragEventArgs e)
    {
        if (sender is not Border { Tag: TileColumn column } border)
            return;
        border.BorderBrush = ColumnBorderBrush;

        if (!e.Data.GetDataPresent(typeof(TileItem)) || e.Data.GetData(typeof(TileItem)) is not TileItem item)
            return;

        var action = TileAssignmentDecision.Decide(item.CurrentSeatId, column.SeatId);

        try
        {
            switch (action)
            {
                case TileDropAction.NoOp:
                    return;

                case TileDropAction.Unassign:
                    await UnassignAsync(item);
                    break;

                case TileDropAction.DirectAssign:
                    await AssignAsync(item, column.SeatId!);
                    break;

                case TileDropAction.ConfirmMove:
                    var currentSeatName = _seatsById.TryGetValue(item.CurrentSeatId!, out var currentSeat) ? currentSeat.Name : item.CurrentSeatId!;
                    var targetSeatName = _seatsById[column.SeatId!].Name;
                    var confirm = new ConfirmDeviceDestinationWindow(item.Label, currentSeatName, targetSeatName) { Owner = this };
                    if (confirm.ShowDialog() != true)
                        return;

                    await UnassignAsync(item);
                    await AssignAsync(item, column.SeatId!);
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't move \"{item.Label}\": {ex.Message}", "Tile Layout", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        await LoadAsync();
    }

    private async Task AssignAsync(TileItem item, string seatId)
    {
        switch (item.Kind)
        {
            case TileKind.Keyboard:
                await _seatManager.AssignDeviceToSeatAsync(seatId, item.ResourceId, InputDeviceType.Keyboard);
                break;
            case TileKind.Mouse:
                await _seatManager.AssignDeviceToSeatAsync(seatId, item.ResourceId, InputDeviceType.Mouse);
                break;
            case TileKind.Display:
                await _seatManager.AssignDisplayToSeatAsync(seatId, item.ResourceId);
                break;
            case TileKind.AudioPlayback:
                await _audioManager.AssignAudioDeviceToSeatAsync(seatId, item.ResourceId, AudioDeviceRole.Playback);
                break;
            case TileKind.AudioCapture:
                await _audioManager.AssignAudioDeviceToSeatAsync(seatId, item.ResourceId, AudioDeviceRole.Recording);
                break;
            default:
                await _seatManager.AssignOtherDeviceToSeatAsync(seatId, item.ResourceId);
                break;
        }
    }

    private async Task UnassignAsync(TileItem item)
    {
        switch (item.Kind)
        {
            case TileKind.Keyboard:
            case TileKind.Mouse:
                await _seatManager.UnassignDeviceFromSeatAsync(item.ResourceId);
                break;
            case TileKind.Display:
                await _seatManager.UnassignDisplayFromSeatAsync(item.ResourceId);
                break;
            case TileKind.AudioPlayback:
            case TileKind.AudioCapture:
                await _audioManager.UnassignAudioDeviceFromSeatAsync(item.ResourceId);
                break;
            default:
                await _seatManager.UnassignOtherDeviceFromSeatAsync(item.ResourceId);
                break;
        }
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
