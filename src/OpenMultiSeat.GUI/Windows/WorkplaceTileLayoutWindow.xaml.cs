using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
/// "Indicate device" is a different, real mechanism per tile type rather than one generic
/// right-click action, since each device category has its own genuinely-checkable signal:
/// - Keyboard/Mouse: press the physical key or move the physical mouse. This window registers
///   for Raw Input (RawInputInterop) while it has focus and blinks whichever tile's device
///   generated the event — a real correlation to the actual hardware, not a manual click.
/// - Display: right-click → shows a large ASTER-style "workplace.display-index" label
///   (DisplaySubNumbering) directly on that physical monitor for a few seconds
///   (DisplayIdentifyOverlayWindow) — verified by looking at the real screen, not a tile here.
/// - Audio: no click at all. While this window is open, a timer polls each endpoint's real
///   peak level (IAudioManager.GetPeakLevelAsync) and the tile glows live whenever that specific
///   endpoint is actually carrying sound — play a test tone and watch which tile lights up.
/// - Other (camera/USB/Bluetooth ownership records): right-click still blinks the tile's border
///   in-app only — there's no live signal for these (no key to press, no screen, no audio path),
///   so this is the one category where a GUI-only highlight is genuinely the closest available.
///
/// Per-workplace status dots reflect Seat.Status, which nothing in this codebase currently
/// updates in real time from an actually-running session (see docs/known-issues.md) — so they
/// reflect the last value SeatManager wrote, not a live poll of whether a process is running.
/// </summary>
public partial class WorkplaceTileLayoutWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly IDevicePersistence _devicePersistence;
    private readonly IDisplayEnumerator _displayEnumerator;
    private readonly IAudioManager _audioManager;

    private Dictionary<string, Seat> _seatsById = new();
    private IReadOnlyList<Seat> _seatsInColumnOrder = [];
    private IReadOnlyList<Display> _displays = [];
    private readonly Dictionary<string, Border> _tileBordersByResourceId = new();
    private readonly List<(string ResourceId, Border Border)> _audioTiles = [];

    private HwndSource? _hwndSource;
    private DispatcherTimer? _audioMeterTimer;
    private readonly HashSet<string> _audioTilesCurrentlyLive = [];

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

        SourceInitialized += OnSourceInitialized;
        Loaded += async (_, _) => await LoadAsync();
        Closed += OnWindowClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = (HwndSource?)PresentationSource.FromVisual(this);
        if (_hwndSource == null)
            return;

        _hwndSource.AddHook(WndProc);
        RawInputInterop.Register(_hwndSource.Handle);

        _audioMeterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _audioMeterTimer.Tick += async (_, _) => await PollAudioMetersAsync();
        _audioMeterTimer.Start();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _audioMeterTimer?.Stop();
        _hwndSource?.RemoveHook(WndProc);
        RawInputInterop.Unregister();
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
        _tileBordersByResourceId.Clear();
        _audioTiles.Clear();
        _audioTilesCurrentlyLive.Clear();
        StatusText.Text = "Loading...";

        var seats = await _seatManager.GetAllSeatsAsync();
        _seatsById = seats.ToDictionary(s => s.Id);
        _seatsInColumnOrder = seats;

        var devices = await _devicePersistence.GetAllDevicesAsync();
        var displays = await _displayEnumerator.EnumerateDisplaysAsync();
        _displays = displays;
        var audioDevices = await _audioManager.GetAudioDevicesAsync();
        var topology = await _audioManager.GetAudioTopologyAsync();

        // Keyed by the ACTUAL list a device is stored in (KeyboardIds/MouseIds/OtherDeviceIds),
        // not by DeviceRecord.DeviceType. Windows Raw Input regularly misclassifies real
        // keyboards/mice as "Other" (see AssignDeviceToSeatWindow's own doc comment on this same
        // issue) -- that's exactly why that window lets an admin override the type with a radio
        // button rather than trusting DeviceType blindly. Deriving TileKind from DeviceType here
        // instead of from which list the device is actually in was a real bug: on a Seat1->Seat2
        // drag, Unassign would call UnassignOtherDeviceFromSeatAsync (a no-op, since the device
        // was never in OtherDeviceIds) while Assign added it to Seat2.OtherDeviceIds -- leaving
        // the device duplicated (orphaned in Seat1.KeyboardIds *and* newly listed in
        // Seat2.OtherDeviceIds), which is exactly the "shows as added, but the list doesn't
        // reflect it correctly" symptom this fixes.
        var assignedDeviceKind = new Dictionary<string, (string SeatId, TileKind Kind)>();
        var displaySeat = new Dictionary<string, string>();
        var audioSeat = new Dictionary<string, string>();
        foreach (var seat in seats)
        {
            foreach (var id in seat.KeyboardIds)
                assignedDeviceKind[id] = (seat.Id, TileKind.Keyboard);
            foreach (var id in seat.MouseIds)
                assignedDeviceKind[id] = (seat.Id, TileKind.Mouse);
            foreach (var id in seat.OtherDeviceIds)
                assignedDeviceKind[id] = (seat.Id, TileKind.Other);
            foreach (var id in seat.DisplayIds)
                displaySeat[id] = seat.Id;
            if (topology.SeatAudioMapping.TryGetValue(seat.Id, out var mapping))
                foreach (var id in mapping.PlaybackDevices.Concat(mapping.RecordingDevices))
                    audioSeat[id] = seat.Id;
        }

        var allTiles = new List<TileItem>();

        foreach (var d in devices)
        {
            TileKind kind;
            string? currentSeatId;
            if (assignedDeviceKind.TryGetValue(d.StableId, out var assigned))
            {
                // Already assigned somewhere -- trust the list it's actually in, not DeviceType.
                kind = assigned.Kind;
                currentSeatId = assigned.SeatId;
            }
            else
            {
                // Unassigned: no existing list membership to read the real kind from, so fall
                // back to the same DeviceType-based guess DevicesPage's "Assign to Seat..." flow
                // starts from -- an admin can still correct a genuinely misclassified device the
                // same way, via the Devices page's Keyboard/Mouse radio button, before dragging
                // it here (this window has no such override control itself).
                kind = d.DeviceType switch
                {
                    "Keyboard" => TileKind.Keyboard,
                    "Mouse" => TileKind.Mouse,
                    _ => TileKind.Other
                };
                currentSeatId = null;
            }

            allTiles.Add(new TileItem
            {
                ResourceId = d.StableId,
                Label = d.ProductName ?? d.StableId,
                Kind = kind,
                CurrentSeatId = currentSeatId
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

        // Only Display and Other get a manual "Indicate device" — Keyboard/Mouse react to real
        // input (WndProc/raw input) and Audio reacts to a real live peak-level poll, neither of
        // which needs (or should offer) a click-to-fake-it fallback. See the class doc comment.
        border.ContextMenu = item.Kind switch
        {
            TileKind.Display => BuildDisplayIndicateContextMenu(item),
            TileKind.Keyboard or TileKind.Mouse or TileKind.AudioPlayback or TileKind.AudioCapture => null,
            _ => BuildBlinkContextMenu(border)
        };

        _tileBordersByResourceId[item.ResourceId] = border;
        if (item.Kind is TileKind.AudioPlayback or TileKind.AudioCapture)
            _audioTiles.Add((item.ResourceId, border));

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

    // ---- "Indicate device": manual blink (Other), on-monitor overlay (Display) ----

    private static readonly SolidColorBrush IndicateBrush = new(Color.FromRgb(0xFF, 0xD5, 0x1A));
    private static readonly SolidColorBrush AudioLiveBrush = new(Color.FromRgb(0x3D, 0xDC, 0x84));

    /// <summary>Camera/USB/Bluetooth ownership records: no live signal exists for these (see the
    /// class doc comment), so a manual in-app blink is the closest available "which one is this."</summary>
    private ContextMenu BuildBlinkContextMenu(Border border)
    {
        var menu = new ContextMenu();
        var indicate = new MenuItem { Header = "Indicate device" };
        indicate.Click += (_, _) => BlinkTile(border);
        menu.Items.Add(indicate);
        return menu;
    }

    private ContextMenu BuildDisplayIndicateContextMenu(TileItem item)
    {
        var menu = new ContextMenu();
        var indicate = new MenuItem { Header = "Indicate device" };
        indicate.Click += (_, _) => ShowDisplayIdentifyOverlay(item);
        menu.Items.Add(indicate);
        return menu;
    }

    /// <summary>Alternates the tile's border between its normal (transparent) state and a bright
    /// highlight for a few seconds, then restores it -- a GUI-only "which one is this" aid, used
    /// only for the one category with no real signal to react to (see class doc comment).</summary>
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

    /// <summary>Shows a large label directly on the physical monitor this tile represents (see
    /// DisplayIdentifyOverlayWindow), rather than blinking the small tile here in the app.</summary>
    private void ShowDisplayIdentifyOverlay(TileItem item)
    {
        var display = _displays.FirstOrDefault(d => d.DisplayId == item.ResourceId);
        if (display == null)
        {
            MessageBox.Show("This display is no longer connected.", "Indicate Device", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var subNumber = DisplaySubNumbering.Compute(_seatsInColumnOrder, item.ResourceId);
        var label = subNumber ?? "?";
        var subLabel = subNumber != null ? item.Label : $"{item.Label} (unassigned)";

        var overlay = new DisplayIdentifyOverlayWindow(
            label, subLabel,
            display.PositionX, display.PositionY, display.Width, display.Height,
            VisualTreeHelper.GetDpi(this));
        overlay.Show();
    }

    // ---- Keyboard/Mouse: raw-input-triggered indicate ----

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == RawInputInterop.WM_INPUT)
        {
            var hDevice = RawInputInterop.GetSourceDevice(lParam);
            if (hDevice != IntPtr.Zero)
                _ = HandleRawInputAsync(hDevice);
        }

        return IntPtr.Zero;
    }

    private async Task HandleRawInputAsync(IntPtr hDevice)
    {
        var devicePath = RawInputInterop.GetDevicePath(hDevice);
        if (string.IsNullOrEmpty(devicePath))
            return;

        var hardwareId = RawInputInterop.ExtractHardwareId(devicePath);
        if (string.IsNullOrEmpty(hardwareId))
            return;

        var device = await _devicePersistence.GetDeviceByHardwareIdAsync(hardwareId);
        if (device == null)
            return;

        if (_tileBordersByResourceId.TryGetValue(device.StableId, out var border))
            BlinkTile(border);
    }

    // ---- Audio: live peak-level indicate ----

    private const float AudioLiveThreshold = 0.02f;

    private async Task PollAudioMetersAsync()
    {
        foreach (var (resourceId, border) in _audioTiles)
        {
            float peak;
            try
            {
                peak = await _audioManager.GetPeakLevelAsync(resourceId);
            }
            catch
            {
                peak = 0f;
            }

            var isLive = peak > AudioLiveThreshold;
            var wasLive = _audioTilesCurrentlyLive.Contains(resourceId);
            if (isLive == wasLive)
                continue;

            if (isLive)
            {
                _audioTilesCurrentlyLive.Add(resourceId);
                border.BorderBrush = AudioLiveBrush;
                border.BorderThickness = new Thickness(3);
            }
            else
            {
                _audioTilesCurrentlyLive.Remove(resourceId);
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(2);
            }
        }
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
