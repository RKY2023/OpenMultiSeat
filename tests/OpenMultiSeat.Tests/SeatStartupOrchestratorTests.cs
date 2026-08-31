using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMultiSeat.Core;
using OpenMultiSeat.Sessions;

namespace OpenMultiSeat.Tests;

[TestClass]
public class SeatStartupOrchestratorTests
{
    [TestMethod]
    public async Task StartAllSeatsAsync_SeatWithDisplayLoginDialog_IsSkippedNotAttempted()
    {
        var sessionManager = new FakeSessionManager();
        var orchestrator = new SeatStartupOrchestrator(NullLogger<SeatStartupOrchestrator>.Instance, sessionManager);

        var seat = new Seat { Id = "seat-a", Name = "Seat A", DisplayLoginDialog = true, WindowsUser = "alice" };

        var summary = await orchestrator.StartAllSeatsAsync([seat]);

        Assert.AreEqual(0, summary.Results.Count);
        Assert.AreEqual(1, summary.SkippedCount);
        Assert.AreEqual(0, sessionManager.LaunchCalls.Count);
    }

    [TestMethod]
    public async Task StartAllSeatsAsync_DisabledSeat_IsSkippedNotAttempted()
    {
        var sessionManager = new FakeSessionManager();
        var orchestrator = new SeatStartupOrchestrator(NullLogger<SeatStartupOrchestrator>.Instance, sessionManager);

        var seat = new Seat
        {
            Id = "seat-a",
            Name = "Seat A",
            Enabled = false,
            DisplayLoginDialog = false,
            WindowsUser = "alice",
            EncryptedPassword = SeatCredentialProtector.Protect("pw")
        };

        var summary = await orchestrator.StartAllSeatsAsync([seat]);

        Assert.AreEqual(1, summary.SkippedCount);
        Assert.AreEqual(0, sessionManager.LaunchCalls.Count);
    }

    [TestMethod]
    public async Task StartAllSeatsAsync_SeatWithSavedCredentials_LaunchesAsThatUser()
    {
        var sessionManager = new FakeSessionManager();
        var orchestrator = new SeatStartupOrchestrator(NullLogger<SeatStartupOrchestrator>.Instance, sessionManager);

        var seat = new Seat
        {
            Id = "seat-a",
            Name = "Seat A",
            DisplayLoginDialog = false,
            WindowsUser = "alice",
            EncryptedPassword = SeatCredentialProtector.Protect("hunter2"),
            CpuCoreAffinity = [0, 1]
        };

        var summary = await orchestrator.StartAllSeatsAsync([seat]);

        Assert.AreEqual(1, summary.SucceededCount);
        Assert.AreEqual(0, summary.SkippedCount);
        Assert.AreEqual(1, sessionManager.LaunchCalls.Count);
        Assert.AreEqual("alice", sessionManager.LaunchCalls[0].UserName);
        Assert.AreEqual("hunter2", sessionManager.LaunchCalls[0].Password);
        CollectionAssert.AreEqual(new[] { 0, 1 }, sessionManager.LaunchCalls[0].CpuCoreAffinity?.ToArray());
    }

    [TestMethod]
    public async Task StartAllSeatsAsync_CorruptEncryptedPassword_RecordsFailureRatherThanThrowing()
    {
        var sessionManager = new FakeSessionManager();
        var orchestrator = new SeatStartupOrchestrator(NullLogger<SeatStartupOrchestrator>.Instance, sessionManager);

        var seat = new Seat
        {
            Id = "seat-a",
            Name = "Seat A",
            DisplayLoginDialog = false,
            WindowsUser = "alice",
            EncryptedPassword = "not-a-valid-base64-dpapi-blob"
        };

        var summary = await orchestrator.StartAllSeatsAsync([seat]);

        Assert.AreEqual(1, summary.FailedCount);
        Assert.AreEqual(0, sessionManager.LaunchCalls.Count);
        Assert.IsFalse(string.IsNullOrEmpty(summary.Results[0].Error));
    }

    [TestMethod]
    public async Task StartAllSeatsAsync_OneSeatLaunchThrows_RecordsFailureAndStillAttemptsRemainingSeats()
    {
        var sessionManager = new FakeSessionManager { ThrowForUser = "bob" };
        var orchestrator = new SeatStartupOrchestrator(NullLogger<SeatStartupOrchestrator>.Instance, sessionManager);

        var seats = new List<Seat>
        {
            new() { Id = "seat-a", Name = "Seat A", DisplayLoginDialog = false, WindowsUser = "bob", EncryptedPassword = SeatCredentialProtector.Protect("pw") },
            new() { Id = "seat-b", Name = "Seat B", DisplayLoginDialog = false, WindowsUser = "carol", EncryptedPassword = SeatCredentialProtector.Protect("pw") }
        };

        var summary = await orchestrator.StartAllSeatsAsync(seats);

        Assert.AreEqual(1, summary.SucceededCount);
        Assert.AreEqual(1, summary.FailedCount);
        Assert.AreEqual(2, sessionManager.LaunchCalls.Count);
    }

    [TestMethod]
    public void ResolveAccountName_LocalAccount_ReturnsBareUsername()
    {
        var seat = new Seat { Id = "seat-a", Name = "Seat A", WindowsUser = "alice", WindowsDomain = null };
        Assert.AreEqual("alice", WindowsStartupTriggerManager.ResolveAccountName(seat));
    }

    [TestMethod]
    public void ResolveAccountName_DomainAccount_ReturnsDomainBackslashUsername()
    {
        var seat = new Seat { Id = "seat-a", Name = "Seat A", WindowsUser = "alice", WindowsDomain = "CORP" };
        Assert.AreEqual(@"CORP\alice", WindowsStartupTriggerManager.ResolveAccountName(seat));
    }
}

file sealed class FakeSessionManager : ISessionManager
{
    public string? ThrowForUser { get; set; }
    public List<(string UserName, string? Domain, string Password, IReadOnlyList<int>? CpuCoreAffinity)> LaunchCalls { get; } = [];

    public Task<uint> LaunchProcessWithCredentialsAsync(
        string userName, string? domain, string password, string executablePath,
        string? arguments = null, string? workingDirectory = null, IReadOnlyList<int>? cpuCoreAffinity = null)
    {
        LaunchCalls.Add((userName, domain, password, cpuCoreAffinity));
        if (userName == ThrowForUser)
            throw new InvalidOperationException($"simulated failure for {userName}");
        return Task.FromResult((uint)1234);
    }

    public Task<WindowsSession?> GetSessionForUserAsync(string userName) => throw new NotImplementedException();
    public Task<IReadOnlyList<WindowsSession>> GetAllSessionsAsync() => throw new NotImplementedException();
    public Task<uint> LaunchProcessInSessionAsync(uint sessionId, string userName, string executablePath, string? arguments = null, string? workingDirectory = null, IReadOnlyList<int>? cpuCoreAffinity = null) => throw new NotImplementedException();
    public Task<bool> IsUserLoggedInAsync(string userName) => throw new NotImplementedException();
    public Task MonitorSessionChangesAsync(Func<SessionChangeEvent, Task> onSessionChange, CancellationToken cancellationToken) => throw new NotImplementedException();
    public uint GetActiveConsoleSessionId() => throw new NotImplementedException();
}
