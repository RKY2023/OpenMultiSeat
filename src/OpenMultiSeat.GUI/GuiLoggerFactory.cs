using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.GUI;

/// <summary>
/// Single shared ILoggerFactory for GUI pages/windows that talk directly to Core/feature-project
/// classes (e.g. DevicesPage, AssignCpuCoresWindow), created once per process rather than once per
/// page/window instantiation. ILoggerFactory holds real resources (a console-provider background
/// thread) — creating a fresh one every time a page is navigated to or a dialog is opened leaked
/// one of those per open, since neither is IDisposable-aware in WPF's navigation lifecycle.
/// </summary>
internal static class GuiLoggerFactory
{
    public static ILoggerFactory Instance { get; } = LoggerFactory.Create(builder => builder.AddConsole());
}
