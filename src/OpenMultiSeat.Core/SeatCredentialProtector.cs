using System.Security.Cryptography;
using System.Text;

namespace OpenMultiSeat.Core;

/// <summary>
/// Protects a seat's stored login password with Windows DPAPI, <see cref="DataProtectionScope.LocalMachine"/>
/// scope, rather than storing it in plaintext.
///
/// This is deliberately <c>LocalMachine</c>, not <c>CurrentUser</c> (an earlier version of this
/// class used CurrentUser): the "At System Startup" Workplace Start Mode runs as SYSTEM, a
/// different Windows account from whichever admin account saves a seat's password via the GUI —
/// CurrentUser-scoped data is decryptable ONLY by the exact account that encrypted it, so SYSTEM
/// could never read it back. LocalMachine scope is keyed off the machine, not any one account, so
/// any local process — SYSTEM included — can decrypt it. See
/// docs/control-panel/general-settings-tab.md for the full trade-off this represents: a
/// LocalMachine-protected password is no longer bound to one Windows identity — any code able to
/// run locally on this machine (any user account, any local admin, any process with sufficient
/// rights) can decrypt it given the stored blob, not just the account that set it. That's a real,
/// deliberate weakening of the protection boundary, accepted specifically so the At-System-Startup
/// scheduled task can actually do its job; it is still meaningfully better than plaintext (a
/// remote attacker or a copy of seats.json alone, off this machine, can't decrypt it) but it's not
/// a secret from other local accounts the way CurrentUser scope was.
/// </summary>
public static class SeatCredentialProtector
{
    /// <summary>Encrypts a password for storage on Seat.EncryptedPassword. Returns null for a
    /// null/empty password (nothing to protect). Always writes LocalMachine-scoped data now —
    /// see the class doc comment for why.</summary>
    public static string? Protect(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return null;

        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(password), null, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(protectedBytes);
    }

    /// <summary>
    /// Decrypts a value previously produced by <see cref="Protect"/>. Returns null for a
    /// null/empty input.
    ///
    /// Tries <see cref="DataProtectionScope.LocalMachine"/> first (what <see cref="Protect"/> now
    /// writes), then falls back to <see cref="DataProtectionScope.CurrentUser"/> — the scope this
    /// class used before switching to LocalMachine — so seat passwords saved under the old scope
    /// still decrypt (for the same account that originally saved them; the old boundary still
    /// applies to old data) instead of silently breaking after an upgrade. There's no persisted
    /// version tag to tell old from new blobs apart other than trying both.
    /// </summary>
    public static string? Unprotect(string? encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return null;

        var bytes = Convert.FromBase64String(encryptedPassword);

        try
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.LocalMachine));
        }
        catch (CryptographicException)
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
        }
    }
}
