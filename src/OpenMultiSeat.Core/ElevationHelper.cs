using System.Security.Principal;

namespace OpenMultiSeat.Core;

/// <summary>
/// Whether the current process is running elevated (as Administrator). Used before attempting
/// anything that needs it — registering/removing a Windows Scheduled Task, in particular (see
/// WindowsStartupTriggerManager) — so the caller can act on that up front instead of discovering
/// it only after schtasks.exe itself fails with "Access is denied."
/// </summary>
public static class ElevationHelper
{
    public static bool IsRunningElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
