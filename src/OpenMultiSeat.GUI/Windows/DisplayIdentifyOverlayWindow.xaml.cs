using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// A borderless, topmost, full-monitor overlay showing a large label (the ASTER-style
/// "workplace.display-index" id, e.g. "1.4", from DisplaySubNumbering -- or a plain description
/// for a display not assigned to any seat) directly on the physical display it identifies, closing
/// itself after a few seconds. This is the Tile Layout window's "Indicate device" for Display
/// tiles: unlike a keyboard/mouse (identifiable by pressing it) there's no live per-device signal
/// to listen for, so a one-shot on-screen overlay -- similar in spirit to Windows' own "Identify
/// displays" -- is the closest real equivalent, verified by looking at the actual monitor rather
/// than a small tile in this app's own window.
///
/// Positioning: OpenMultiSeat.Core.Display's PositionX/Y/Width/Height are physical pixels (from
/// GDI's EnumDisplaySettings). WPF Window.Left/Top/Width/Height are device-independent units
/// (96 DPI), so physical pixels are divided by the owner window's current DPI scale factor
/// (VisualTreeHelper.GetDpi) before being applied here. This is correct for the common
/// single-DPI-for-the-whole-desktop case; a genuinely mixed-DPI multi-monitor setup (different
/// scale factors per monitor) could still land the overlay slightly off on a *non-primary*
/// monitor, since the single scale factor read from the owner window reflects whichever monitor
/// the Tile Layout window itself is on -- disclosed here rather than silently assumed correct.
/// </summary>
public partial class DisplayIdentifyOverlayWindow : Window
{
    public DisplayIdentifyOverlayWindow(
        string label,
        string subLabel,
        double physicalX,
        double physicalY,
        double physicalWidth,
        double physicalHeight,
        DpiScale dpi)
    {
        InitializeComponent();

        LabelText.Text = label;
        SubLabelText.Text = subLabel;

        Left = physicalX / dpi.DpiScaleX;
        Top = physicalY / dpi.DpiScaleY;
        Width = physicalWidth / dpi.DpiScaleX;
        Height = physicalHeight / dpi.DpiScaleY;

        Loaded += (_, _) =>
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Close();
            };
            timer.Start();
        };
    }
}
