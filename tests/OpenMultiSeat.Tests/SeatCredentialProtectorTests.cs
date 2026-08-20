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
}
