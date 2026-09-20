using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Tests;

[TestClass]
public class SeatCredentialProtectorTests
{
    [TestMethod]
    public void Protect_ThenUnprotect_RoundTripsTheOriginalPassword()
    {
        var encrypted = SeatCredentialProtector.Protect("correct horse battery staple");
        var decrypted = SeatCredentialProtector.Unprotect(encrypted);

        Assert.AreEqual("correct horse battery staple", decrypted);
    }

    [TestMethod]
    public void Protect_DoesNotStoreThePlaintextPasswordAnywhereInTheOutput()
    {
        var encrypted = SeatCredentialProtector.Protect("hunter2");

        Assert.IsNotNull(encrypted);
        StringAssert.DoesNotMatch(encrypted, new System.Text.RegularExpressions.Regex("hunter2"));
    }

    [TestMethod]
    public void Protect_NullOrEmptyPassword_ReturnsNull()
    {
        Assert.IsNull(SeatCredentialProtector.Protect(null));
        Assert.IsNull(SeatCredentialProtector.Protect(string.Empty));
    }

    [TestMethod]
    public void Unprotect_NullOrEmptyInput_ReturnsNull()
    {
        Assert.IsNull(SeatCredentialProtector.Unprotect(null));
        Assert.IsNull(SeatCredentialProtector.Unprotect(string.Empty));
    }

    [TestMethod]
    public void Protect_SamePasswordTwice_ProducesDifferentCiphertext()
    {
        // DPAPI includes randomness in its output even for identical input, which is expected
        // and desirable (no two seats' stored passwords should be visibly identical on disk even
        // if the actual passwords happen to match).
        var first = SeatCredentialProtector.Protect("same-password");
        var second = SeatCredentialProtector.Protect("same-password");

        Assert.AreNotEqual(first, second);
        Assert.AreEqual("same-password", SeatCredentialProtector.Unprotect(first));
        Assert.AreEqual("same-password", SeatCredentialProtector.Unprotect(second));
    }

    [TestMethod]
    public void Unprotect_PasswordSavedUnderThePreviousCurrentUserScope_StillDecrypts()
    {
        // Regression/migration test: SeatCredentialProtector switched from DPAPI CurrentUser
        // scope to LocalMachine scope (so the At-System-Startup scheduled task, which runs as
        // SYSTEM, can decrypt seat passwords). Seats saved before that switch have
        // CurrentUser-scoped ciphertext on disk — Unprotect must still read those back rather
        // than breaking every seat configured before the upgrade.
        var oldFormatBlob = Convert.ToBase64String(ProtectedData.Protect(
            Encoding.UTF8.GetBytes("legacy-password"), null, DataProtectionScope.CurrentUser));

        Assert.AreEqual("legacy-password", SeatCredentialProtector.Unprotect(oldFormatBlob));
    }

    [TestMethod]
    public void Protect_WritesLocalMachineScopeNotCurrentUserScope()
    {
        // The whole point of the LocalMachine switch: this must be decryptable via LocalMachine
        // scope directly (not just via Unprotect's CurrentUser fallback), since that's what lets
        // a SYSTEM-run process (the At-System-Startup scheduled task) decrypt it.
        var encrypted = SeatCredentialProtector.Protect("hunter2");
        var bytes = Convert.FromBase64String(encrypted!);

        var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.LocalMachine);
        Assert.AreEqual("hunter2", Encoding.UTF8.GetString(decrypted));
    }
}
