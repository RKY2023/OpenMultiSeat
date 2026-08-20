using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenMultiSeat.Sessions;

public interface ISessionEnumerator
{
    Task<IReadOnlyList<WindowsSession>> EnumerateSessionsAsync();
}

public sealed class WindowsSession
{
    public required uint SessionId { get; set; }
    public required string UserName { get; set; }
    public SessionState State { get; set; }
    public string? Domain { get; set; }
}

public enum SessionState
{
    Active,
    Connected,
    ConnectQuery,
    Shadow,
    Disconnected,
    Idle,
    Listen,
    Reset,
    Down,
    Init
}

public class SessionEnumerator : ISessionEnumerator
{
    private readonly ILogger<SessionEnumerator> _logger;

    public SessionEnumerator(ILogger<SessionEnumerator> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<WindowsSession>> EnumerateSessionsAsync()
    {
        return await Task.Run(() => EnumerateSessions());
    }

    private List<WindowsSession> EnumerateSessions()
    {
        var sessions = new List<WindowsSession>();

        try
        {
            IntPtr sessionEnumHandle = IntPtr.Zero;
            uint sessionCount = 0;

            if (!NativeMethods.WtsEnumerateSessions(
                NativeMethods.WtsCurrentServerHandle,
                0,
                1,
                out sessionEnumHandle,
                out sessionCount))
            {
                _logger.LogWarning("Failed to enumerate sessions");
                return sessions;
            }

            try
            {
                if (sessionCount == 0)
                {
                    _logger.LogInformation("No sessions found");
                    return sessions;
                }

                var sessionInfo = new NativeMethods.WtsSessionInfo[sessionCount];
                int structSize = Marshal.SizeOf<NativeMethods.WtsSessionInfo>();

                for (int i = 0; i < sessionCount; i++)
                {
                    IntPtr sessionPtr = new IntPtr(sessionEnumHandle.ToInt64() + (i * structSize));
                    sessionInfo[i] = Marshal.PtrToStructure<NativeMethods.WtsSessionInfo>(sessionPtr);

                    var session = GetSessionDetails(sessionInfo[i]);
                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }
            }
            finally
            {
                if (sessionEnumHandle != IntPtr.Zero)
                {
                    NativeMethods.WtsFreeMemory(sessionEnumHandle);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating sessions");
        }

        return sessions;
    }

    private WindowsSession? GetSessionDetails(NativeMethods.WtsSessionInfo sessionInfo)
    {
        try
        {
            var userName = GetSessionString(sessionInfo.SessionId, NativeMethods.WtsInfoClass.UserName) ?? "Unknown";
            var domain = GetSessionString(sessionInfo.SessionId, NativeMethods.WtsInfoClass.DomainName) ?? "UNKNOWN";

            return new WindowsSession
            {
                SessionId = sessionInfo.SessionId,
                UserName = userName,
                Domain = domain,
                State = (SessionState)sessionInfo.State
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting details for session {sessionInfo.SessionId}");
            return null;
        }
    }

    private string? GetSessionString(uint sessionId, NativeMethods.WtsInfoClass infoClass)
    {
        try
        {
            if (!NativeMethods.WtsQuerySessionInformation(
                NativeMethods.WtsCurrentServerHandle,
                sessionId,
                infoClass,
                out IntPtr buffer,
                out uint bytesReturned))
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringAnsi(buffer);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    NativeMethods.WtsFreeMemory(buffer);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error querying session {sessionId} info class {infoClass}");
            return null;
        }
    }
}

internal static class NativeMethods
{
    public const IntPtr WtsCurrentServerHandle = (IntPtr)0;

    public enum WtsInfoClass : uint
    {
        InitialProgram = 0,
        ApplicationName = 1,
        WorkingDirectory = 2,
        OemId = 3,
        LogonUser = 4,
        LogonDomain = 5,
        LogonTime = 6,
        LogoffTime = 7,
        ErrorCode = 8,
        SessionName = 9,
        LockState = 10,
        ClientName = 11,
        ClientDirectory = 12,
        ClientBuildNumber = 13,
        ClientHardwareId = 14,
        ClientProductId = 15,
        ConnectedState = 16,
        ClientProtocolType = 17,
        IsAsync = 18,
        UserName = 24,
        DomainName = 25,
        ConnectState = 29,
        ClientAddress = 30,
        Display = 31,
        ProtocolName = 32
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WtsSessionInfo
    {
        public uint SessionId;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? SessionName;
        public uint State;
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    public static extern bool WtsEnumerateSessions(
        IntPtr serverHandle,
        uint reserved,
        uint version,
        out IntPtr ppSessionInfo,
        out uint pCount);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    public static extern void WtsFreeMemory(IntPtr pMemory);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    public static extern bool WtsQuerySessionInformation(
        IntPtr serverHandle,
        uint sessionId,
        WtsInfoClass wtsInfoClass,
        out IntPtr ppBuffer,
        out uint pBytesReturned);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    public static extern uint WtsGetActiveConsoleSessionId();
}
