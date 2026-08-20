using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenMultiSeat.Sessions;

public interface ISessionManager
{
    Task<WindowsSession?> GetSessionForUserAsync(string userName);
    Task<IReadOnlyList<WindowsSession>> GetAllSessionsAsync();
    Task<uint> LaunchProcessInSessionAsync(uint sessionId, string userName, string executablePath, string? arguments = null, string? workingDirectory = null, IReadOnlyList<int>? cpuCoreAffinity = null);

    /// <summary>
    /// Launches a process as a specific Windows user given their plaintext credentials, via
    /// CreateProcessWithLogonW — unlike LaunchProcessInSessionAsync, this does NOT require an
    /// already-logged-in WTS session for that user; it authenticates them itself. This is the
    /// same mechanism behind Explorer's "Run as different user". It does NOT create a new
    /// isolated desktop session bound to specific seat hardware — see
    /// docs/control-panel/user-account-for-workstation.md for what that would actually require.
    /// </summary>
    Task<uint> LaunchProcessWithCredentialsAsync(string userName, string? domain, string password, string executablePath, string? arguments = null, string? workingDirectory = null, IReadOnlyList<int>? cpuCoreAffinity = null);
    Task<bool> IsUserLoggedInAsync(string userName);
    Task MonitorSessionChangesAsync(Func<SessionChangeEvent, Task> onSessionChange, CancellationToken cancellationToken);
    uint GetActiveConsoleSessionId();
}

public class SessionChangeEvent
{
    public required uint SessionId { get; set; }
    public required string UserName { get; set; }
    public required SessionChangeReason Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum SessionChangeReason
{
    ConsoleConnect,
    ConsoleDisconnect,
    RemoteConnect,
    RemoteDisconnect,
    SessionLogon,
    SessionLogoff,
    SessionLock,
    SessionUnlock
}

public class SessionManager : ISessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly ISessionEnumerator _enumerator;
    private readonly ICpuAffinityProvider _cpuAffinityProvider;

    public SessionManager(ILogger<SessionManager> logger, ISessionEnumerator enumerator, ICpuAffinityProvider cpuAffinityProvider)
    {
        _logger = logger;
        _enumerator = enumerator;
        _cpuAffinityProvider = cpuAffinityProvider;
    }

    public async Task<WindowsSession?> GetSessionForUserAsync(string userName)
    {
        var sessions = await _enumerator.EnumerateSessionsAsync();
        return sessions.FirstOrDefault(s =>
            s.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<WindowsSession>> GetAllSessionsAsync()
    {
        return await _enumerator.EnumerateSessionsAsync();
    }

    public async Task<uint> LaunchProcessInSessionAsync(
        uint sessionId,
        string userName,
        string executablePath,
        string? arguments = null,
        string? workingDirectory = null,
        IReadOnlyList<int>? cpuCoreAffinity = null)
    {
        if (!File.Exists(executablePath))
            throw new FileNotFoundException($"Executable not found: {executablePath}");

        try
        {
            nint? affinityMask = null;
            if (cpuCoreAffinity is { Count: > 0 })
            {
                affinityMask = _cpuAffinityProvider.ComputeAffinityMask(cpuCoreAffinity);
            }

            var processId = ProcessLauncher.CreateProcessInSession(
                sessionId,
                userName,
                executablePath,
                arguments,
                workingDirectory,
                affinityMask);

            if (affinityMask.HasValue)
            {
                _logger.LogInformation(
                    $"Applied CPU affinity mask 0x{(long)affinityMask.Value:X} ({cpuCoreAffinity!.Count} core(s)) to process {processId}");
            }

            _logger.LogInformation(
                $"Process launched in session {sessionId}: {Path.GetFileName(executablePath)} (PID: {processId})");

            return processId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to launch process in session {sessionId}");
            throw;
        }
    }

    public async Task<uint> LaunchProcessWithCredentialsAsync(
        string userName,
        string? domain,
        string password,
        string executablePath,
        string? arguments = null,
        string? workingDirectory = null,
        IReadOnlyList<int>? cpuCoreAffinity = null)
    {
        if (!File.Exists(executablePath))
            throw new FileNotFoundException($"Executable not found: {executablePath}");

        try
        {
            nint? affinityMask = null;
            if (cpuCoreAffinity is { Count: > 0 })
            {
                affinityMask = _cpuAffinityProvider.ComputeAffinityMask(cpuCoreAffinity);
            }

            var processId = ProcessLauncher.CreateProcessWithCredentials(
                userName, domain, password, executablePath, arguments, workingDirectory, affinityMask);

            if (affinityMask.HasValue)
            {
                _logger.LogInformation(
                    $"Applied CPU affinity mask 0x{(long)affinityMask.Value:X} ({cpuCoreAffinity!.Count} core(s)) to process {processId}");
            }

            _logger.LogInformation(
                $"Process launched as {(domain != null ? $"{domain}\\{userName}" : userName)}: {Path.GetFileName(executablePath)} (PID: {processId})");

            return processId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to launch process as {(domain != null ? $"{domain}\\{userName}" : userName)}");
            throw;
        }
    }

    public async Task<bool> IsUserLoggedInAsync(string userName)
    {
        var session = await GetSessionForUserAsync(userName);
        return session != null && session.State == SessionState.Active;
    }

    public async Task MonitorSessionChangesAsync(
        Func<SessionChangeEvent, Task> onSessionChange,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting session change monitoring");

        var previousSessions = new Dictionary<uint, WindowsSession>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var currentSessions = await GetAllSessionsAsync();
                var currentMap = currentSessions.ToDictionary(s => s.SessionId);

                // Detect new sessions or state changes
                foreach (var current in currentSessions)
                {
                    if (!previousSessions.TryGetValue(current.SessionId, out var previous))
                    {
                        // New session
                        await onSessionChange(new SessionChangeEvent
                        {
                            SessionId = current.SessionId,
                            UserName = current.UserName,
                            Reason = SessionChangeReason.SessionLogon
                        });
                    }
                    else if (previous.State != current.State)
                    {
                        // State changed
                        var reason = (previous.State, current.State) switch
                        {
                            (SessionState.Disconnected, SessionState.Active) => SessionChangeReason.ConsoleConnect,
                            (SessionState.Active, SessionState.Disconnected) => SessionChangeReason.ConsoleDisconnect,
                            _ => SessionChangeReason.SessionLogoff
                        };

                        await onSessionChange(new SessionChangeEvent
                        {
                            SessionId = current.SessionId,
                            UserName = current.UserName,
                            Reason = reason
                        });
                    }
                }

                // Detect removed sessions
                foreach (var previous in previousSessions.Values)
                {
                    if (!currentMap.ContainsKey(previous.SessionId))
                    {
                        await onSessionChange(new SessionChangeEvent
                        {
                            SessionId = previous.SessionId,
                            UserName = previous.UserName,
                            Reason = SessionChangeReason.SessionLogoff
                        });
                    }
                }

                previousSessions = currentMap;
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring session changes");
                await Task.Delay(5000, cancellationToken);
            }
        }

        _logger.LogInformation("Session change monitoring stopped");
    }

    public uint GetActiveConsoleSessionId()
    {
        return NativeMethods.WtsGetActiveConsoleSessionId();
    }
}

public static class ProcessLauncher
{
    private static readonly ILogger<SessionManager> _logger;

    static ProcessLauncher()
    {
        var factory = new LoggerFactory();
        _logger = factory.CreateLogger<SessionManager>();
    }

    public static uint CreateProcessInSession(
        uint sessionId,
        string userName,
        string executablePath,
        string? arguments = null,
        string? workingDirectory = null,
        nint? cpuAffinityMask = null)
    {
        // Get session token
        if (!ProcessNativeMethods.WtsQueryUserToken(sessionId, out var userToken))
        {
            throw new InvalidOperationException(
                $"Failed to get user token for session {sessionId}: {Marshal.GetLastWin32Error()}");
        }

        try
        {
            // Create environment block for the user
            if (!ProcessNativeMethods.CreateEnvironmentBlock(out var envBlock, userToken, false))
            {
                throw new InvalidOperationException(
                    $"Failed to create environment block: {Marshal.GetLastWin32Error()}");
            }

            try
            {
                var startInfo = new ProcessNativeMethods.STARTUPINFO
                {
                    cb = (uint)Marshal.SizeOf<ProcessNativeMethods.STARTUPINFO>(),
                    lpDesktop = "winsta0\\default"
                };

                var cmdLine = $"\"{executablePath}\"";
                if (!string.IsNullOrEmpty(arguments))
                    cmdLine += $" {arguments}";

                var processInfo = new ProcessNativeMethods.PROCESS_INFORMATION();

                var success = ProcessNativeMethods.CreateProcessAsUser(
                    userToken,
                    executablePath,
                    cmdLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    (uint)(ProcessNativeMethods.CREATE_NEW_CONSOLE | ProcessNativeMethods.CREATE_UNICODE_ENVIRONMENT),
                    envBlock,
                    workingDirectory ?? Path.GetDirectoryName(executablePath),
                    ref startInfo,
                    out processInfo);

                if (!success)
                {
                    throw new InvalidOperationException(
                        $"Failed to create process: {Marshal.GetLastWin32Error()}");
                }

                // Apply CPU-core affinity, if requested, while the process handle is still open.
                // Non-fatal: the process is already running, so a failure here is logged, not thrown.
                if (cpuAffinityMask.HasValue && processInfo.hProcess != IntPtr.Zero)
                {
                    if (!ProcessNativeMethods.SetProcessAffinityMask(processInfo.hProcess, cpuAffinityMask.Value))
                    {
                        _logger.LogWarning(
                            $"Failed to set CPU affinity mask 0x{(long)cpuAffinityMask.Value:X} for process {processInfo.dwProcessId}: {Marshal.GetLastWin32Error()}");
                    }
                }

                // Close process and thread handles
                if (processInfo.hProcess != IntPtr.Zero)
                    ProcessNativeMethods.CloseHandle(processInfo.hProcess);
                if (processInfo.hThread != IntPtr.Zero)
                    ProcessNativeMethods.CloseHandle(processInfo.hThread);

                return processInfo.dwProcessId;
            }
            finally
            {
                if (envBlock != IntPtr.Zero)
                    ProcessNativeMethods.DestroyEnvironmentBlock(envBlock);
            }
        }
        finally
        {
            ProcessNativeMethods.CloseHandle(userToken);
        }
    }

    /// <summary>
    /// Launches a process as a specific user via CreateProcessWithLogonW, given their plaintext
    /// credentials directly — no existing WTS session/token needed, unlike
    /// <see cref="CreateProcessInSession"/>. This is the exact mechanism behind Explorer's "Run
    /// as different user"; CreateProcessWithLogonW is specifically designed for this scenario and
    /// (unlike raw LogonUser + CreateProcessAsUser) doesn't require the caller to hold
    /// SE_ASSIGNPRIMARYTOKEN_NAME/SE_INCREASE_QUOTA_NAME privileges. Requires Windows' Secondary
    /// Logon service (seclogon) to be running, which it is by default on non-hardened systems.
    ///
    /// What this does NOT do: create a new, hardware-isolated interactive desktop session bound
    /// to a specific seat's monitor/keyboard/mouse. The launched process runs in whatever session
    /// Windows assigns it — genuine simultaneous multi-seat login (each seat showing its own
    /// separately-logged-in desktop on its own hardware at the same time) needs either Windows
    /// Server with RDS/MultiPoint-style session support, or a Winlogon Credential Provider
    /// component, neither of which exists here. This is closer to "run this program as a
    /// different user" than "log this seat in".
    /// </summary>
    public static uint CreateProcessWithCredentials(
        string userName,
        string? domain,
        string password,
        string executablePath,
        string? arguments = null,
        string? workingDirectory = null,
        nint? cpuAffinityMask = null)
    {
        var startInfo = new ProcessNativeMethods.STARTUPINFOW
        {
            cb = (uint)Marshal.SizeOf<ProcessNativeMethods.STARTUPINFOW>(),
            lpDesktop = "winsta0\\default"
        };

        var cmdLine = $"\"{executablePath}\"";
        if (!string.IsNullOrEmpty(arguments))
            cmdLine += $" {arguments}";

        var success = ProcessNativeMethods.CreateProcessWithLogonW(
            userName,
            domain,
            password,
            ProcessNativeMethods.LOGON_WITH_PROFILE,
            null,
            cmdLine,
            (uint)(ProcessNativeMethods.CREATE_NEW_CONSOLE | ProcessNativeMethods.CREATE_UNICODE_ENVIRONMENT),
            IntPtr.Zero,
            workingDirectory,
            ref startInfo,
            out var processInfo);

        if (!success)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"CreateProcessWithLogonW failed (Win32 error {error}): {new System.ComponentModel.Win32Exception(error).Message}");
        }

        // Apply CPU-core affinity, if requested, while the process handle is still open.
        // Non-fatal: the process is already running, so a failure here is logged, not thrown.
        if (cpuAffinityMask.HasValue && processInfo.hProcess != IntPtr.Zero)
        {
            if (!ProcessNativeMethods.SetProcessAffinityMask(processInfo.hProcess, cpuAffinityMask.Value))
            {
                _logger.LogWarning(
                    $"Failed to set CPU affinity mask 0x{(long)cpuAffinityMask.Value:X} for process {processInfo.dwProcessId}: {Marshal.GetLastWin32Error()}");
            }
        }

        if (processInfo.hProcess != IntPtr.Zero)
            ProcessNativeMethods.CloseHandle(processInfo.hProcess);
        if (processInfo.hThread != IntPtr.Zero)
            ProcessNativeMethods.CloseHandle(processInfo.hThread);

        return processInfo.dwProcessId;
    }
}

internal static class ProcessNativeMethods
{
    public const uint CREATE_NEW_CONSOLE = 0x00000010;
    public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFO
    {
        public uint cb;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpReserved;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpDesktop;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    // Separate from STARTUPINFO above: CreateProcessWithLogonW is the "W" (wide/Unicode) variant
    // — Windows ships no ANSI counterpart for this specific function — so its STARTUPINFO must
    // marshal strings as Unicode (LPWStr), not the ANSI (LPStr) STARTUPINFO already used by the
    // ANSI-charset CreateProcessAsUser above. Reusing that struct here would compile fine (the
    // field layout/offsets are identical either way — only the string *content* encoding
    // differs) but would silently corrupt lpDesktop for the OS to read as UTF-16.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFOW
    {
        public uint cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    public const uint LOGON_WITH_PROFILE = 0x00000001;

    [DllImport("wtsapi32.dll", SetLastError = true)]
    public static extern bool WtsQueryUserToken(uint sessionId, out IntPtr phToken);

    [DllImport("userenv.dll", SetLastError = true)]
    public static extern bool CreateEnvironmentBlock(
        out IntPtr lpEnvironment,
        IntPtr hToken,
        bool bInherit);

    [DllImport("userenv.dll", SetLastError = true)]
    public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetProcessAffinityMask(IntPtr hProcess, IntPtr dwProcessAffinityMask);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool CreateProcessWithLogonW(
        string lpUsername,
        string? lpDomain,
        string lpPassword,
        uint dwLogonFlags,
        string? lpApplicationName,
        string lpCommandLine,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFOW lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);
}
