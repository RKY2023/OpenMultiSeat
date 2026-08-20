using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Sessions;

var services = new ServiceCollection();

services.AddLogging(builder =>
    builder
        .ClearProviders()
        .AddConsole(config => config.IncludeScopes = true)
        .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<ISessionEnumerator, SessionEnumerator>();
services.AddSingleton<ICpuAffinityProvider, CpuAffinityProvider>();
services.AddSingleton<ISessionManager, SessionManager>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();
var sessionManager = provider.GetRequiredService<ISessionManager>();

try
{
    logger.LogInformation("╔════════════════════════════════════════╗");
    logger.LogInformation("║  Phase 3: Session Management Tester    ║");
    logger.LogInformation("║  Windows Session Lifecycle & Launching ║");
    logger.LogInformation("╚════════════════════════════════════════╝\n");

    logger.LogInformation("Menu:");
    logger.LogInformation("1. List all Windows sessions");
    logger.LogInformation("2. Check if user is logged in");
    logger.LogInformation("3. Launch process in session");
    logger.LogInformation("4. Monitor session changes (10 seconds)");
    logger.LogInformation("5. Get active console session ID");
    logger.LogInformation("6. Get session for specific user");
    logger.LogInformation("0. Exit\n");

    bool running = true;
    while (running)
    {
        Console.Write("Select option: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await ListAllSessions();
                break;
            case "2":
                await CheckUserLoggedIn();
                break;
            case "3":
                await LaunchProcess();
                break;
            case "4":
                await MonitorSessions();
                break;
            case "5":
                ShowActiveConsoleSession();
                break;
            case "6":
                await GetUserSession();
                break;
            case "0":
                running = false;
                break;
            default:
                logger.LogWarning("Invalid option");
                break;
        }
        Console.WriteLine();
    }

    logger.LogInformation("Thank you for using Session Management Tester");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    Environment.Exit(1);
}

async Task ListAllSessions()
{
    logger.LogInformation("\n=== Windows Sessions ===\n");

    try
    {
        var sessions = await sessionManager.GetAllSessionsAsync();

        if (sessions.Count == 0)
        {
            logger.LogInformation("No sessions found");
            return;
        }

        logger.LogInformation($"Total: {sessions.Count} session(s)\n");

        foreach (var session in sessions.OrderBy(s => s.SessionId))
        {
            logger.LogInformation($"► Session {session.SessionId}");
            logger.LogInformation($"  User: {session.Domain}\\{session.UserName}");
            logger.LogInformation($"  State: {session.State}");
            logger.LogInformation("");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to enumerate sessions");
    }
}

async Task CheckUserLoggedIn()
{
    logger.LogInformation("\n=== Check User Login Status ===");

    Console.Write("Username: ");
    var userName = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(userName))
    {
        logger.LogWarning("Invalid username");
        return;
    }

    try
    {
        var isLoggedIn = await sessionManager.IsUserLoggedInAsync(userName);

        if (isLoggedIn)
        {
            var session = await sessionManager.GetSessionForUserAsync(userName);
            logger.LogInformation($"\n✓ User '{userName}' is logged in");
            logger.LogInformation($"  Session ID: {session?.SessionId}");
            logger.LogInformation($"  State: {session?.State}");
        }
        else
        {
            logger.LogWarning($"User '{userName}' is not logged in");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to check user login status");
    }
}

async Task LaunchProcess()
{
    logger.LogInformation("\n=== Launch Process in Session ===");

    Console.Write("Username: ");
    var userName = Console.ReadLine() ?? "";

    Console.Write("Executable path (e.g., C:\\Windows\\System32\\notepad.exe): ");
    var exePath = Console.ReadLine() ?? "";

    Console.Write("Arguments (optional): ");
    var args = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(exePath))
    {
        logger.LogWarning("Invalid input");
        return;
    }

    try
    {
        var session = await sessionManager.GetSessionForUserAsync(userName);
        if (session == null)
        {
            logger.LogWarning($"User '{userName}' not logged in");
            return;
        }

        var processId = await sessionManager.LaunchProcessInSessionAsync(
            session.SessionId,
            userName,
            exePath,
            string.IsNullOrEmpty(args) ? null : args);

        logger.LogInformation($"\n✓ Process launched successfully");
        logger.LogInformation($"  Session: {session.SessionId}");
        logger.LogInformation($"  Process ID: {processId}");
        logger.LogInformation($"  Application: {Path.GetFileName(exePath)}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to launch process");
    }
}

async Task MonitorSessions()
{
    logger.LogInformation("\n=== Monitoring Session Changes (10 seconds) ===\n");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

    try
    {
        await sessionManager.MonitorSessionChangesAsync(
            async evt =>
            {
                logger.LogInformation(
                    $"[{evt.Timestamp:HH:mm:ss}] {evt.Reason}: Session {evt.SessionId} ({evt.UserName})");
                await Task.CompletedTask;
            },
            cts.Token);
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("\nMonitoring completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during monitoring");
    }
}

void ShowActiveConsoleSession()
{
    logger.LogInformation("\n=== Active Console Session ===\n");

    try
    {
        var sessionId = sessionManager.GetActiveConsoleSessionId();
        logger.LogInformation($"Active console session ID: {sessionId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get active console session");
    }
}

async Task GetUserSession()
{
    logger.LogInformation("\n=== Get Session for User ===");

    Console.Write("Username: ");
    var userName = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(userName))
    {
        logger.LogWarning("Invalid username");
        return;
    }

    try
    {
        var session = await sessionManager.GetSessionForUserAsync(userName);

        if (session == null)
        {
            logger.LogWarning($"No session found for user '{userName}'");
            return;
        }

        logger.LogInformation($"\n✓ Session found");
        logger.LogInformation($"  Session ID: {session.SessionId}");
        logger.LogInformation($"  Username: {session.UserName}");
        logger.LogInformation($"  Domain: {session.Domain}");
        logger.LogInformation($"  State: {session.State}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get user session");
    }
}
