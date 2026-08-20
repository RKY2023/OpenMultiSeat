using System.Security.Cryptography;
using System.Text;

namespace OpenMultiSeat.Core;

/// <summary>
/// Protects a seat's stored login password with Windows DPAPI (CurrentUser scope) rather than
/// storing it in plaintext. Decryptable only by the same Windows user account that encrypted it,
/// on the same machine — that's the actual security boundary DPAPI CurrentUser scope provides,
/// not a hidden limitation. A real auto-login/session-launch consumer (not implemented yet — see
/// docs/control-panel/user-account-for-workstation.md) would need to run as that same account to
/// read these back.
/// </summary>
public static class SeatCredentialProtector
{
    /// <summary>Encrypts a password for storage on Seat.EncryptedPassword. Returns null for a
    /// null/empty password (nothing to protect).</summary>
    public static string? Protect(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return null;

        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    /// <summary>Decrypts a value previously produced by <see cref="Protect"/>. Returns null for a
    /// null/empty input.</summary>
    public static string? Unprotect(string? encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return null;

        var plainBytes = ProtectedData.Unprotect(
            Convert.FromBase64String(encryptedPassword), null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
