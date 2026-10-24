namespace OpenMultiSeat.Core;

public sealed class Seat
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public string? WindowsUser { get; set; }

    /// <summary>Domain name for WindowsUser when it's a domain account; null means local account.</summary>
    public string? WindowsDomain { get; set; }

    /// <summary>
    /// When true (the default, matching ASTER's own default), this seat shows the normal Windows
    /// login prompt instead of auto-logging in — WindowsUser/EncryptedPassword are informational
    /// only in that case. When false, WindowsUser (and EncryptedPassword, if a password was set)
    /// are meant for unattended auto-login.
    /// </summary>
    public bool DisplayLoginDialog { get; set; } = true;

    /// <summary>
    /// The seat's stored login password, protected with Windows DPAPI
    /// (System.Security.Cryptography.ProtectedData, LocalMachine scope) and base64-encoded — never
    /// stored in plaintext. Null when no password has been set (or DisplayLoginDialog is true).
    /// LocalMachine scope means any local process on this machine can decrypt it (not just the
    /// Windows account that saved it) — see SeatCredentialProtector's doc comment for why.
    /// </summary>
    public string? EncryptedPassword { get; set; }

    public List<string> DisplayIds { get; set; } = [];
    public List<string> KeyboardIds { get; set; } = [];
    public List<string> MouseIds { get; set; } = [];
    
    public string? AudioPlaybackId { get; set; }
    public string? AudioCaptureId { get; set; }

    /// <summary>
    /// Non-input, non-display, non-audio devices (cameras, USB controllers/hubs, Bluetooth
    /// devices/radios) recorded as belonging to this seat. This is an ownership/organizational
    /// record only — unlike KeyboardIds/MouseIds there's no InputIsolation-style routing that
    /// actually restricts a camera or USB device to one seat's session; it just tracks intent.
    /// </summary>
    public List<string> OtherDeviceIds { get; set; } = [];

    /// <summary>
    /// Logical CPU core indices this seat's processes are restricted to.
    /// Empty means no restriction (the seat may use any core).
    /// </summary>
    public List<int> CpuCoreAffinity { get; set; } = [];

    public bool Enabled { get; set; } = true;
    
    public SeatStatus Status { get; set; } = SeatStatus.Disabled;
}

public enum SeatStatus
{
    Disabled,
    Configured,
    Starting,
    Running,
    LoggedOff,
    Locked,
    Error,
    Recovering
}
