using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.Displays;
using OpenMultiSeat.Sessions;
using System.Text.Json;
using System.Text.Json.Serialization;

var services = new ServiceCollection();

services.AddLogging(builder =>
    builder
        .ClearProviders()
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<IHidDeviceEnumerator, HidDeviceEnumerator>();
services.AddSingleton<IDisplayEnumerator, DisplayEnumerator>();
services.AddSingleton<ISessionEnumerator, SessionEnumerator>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("=== OpenMultiSeat Phase 0: Hardware Discovery POC ===\n");

    var deviceEnumerator = provider.GetRequiredService<IHidDeviceEnumerator>();
    var displayEnumerator = provider.GetRequiredService<IDisplayEnumerator>();
    var sessionEnumerator = provider.GetRequiredService<ISessionEnumerator>();

    logger.LogInformation("Enumerating input devices...");
    var keyboards = await deviceEnumerator.EnumerateKeyboardsAsync();
    var mice = await deviceEnumerator.EnumerateMiceAsync();
    var allDevices = await deviceEnumerator.EnumerateAllDevicesAsync();

    logger.LogInformation($"Found {keyboards.Count} keyboard(s), {mice.Count} mouse/mice, {allDevices.Count} total device(s)\n");

    if (allDevices.Count > 0)
    {
        logger.LogInformation("Input Devices:");
        foreach (var device in allDevices)
        {
            logger.LogInformation($"  - {device.ProductName ?? "Unknown"}");
            logger.LogInformation($"    Type: {device.Type}");
            logger.LogInformation($"    ID: {device.DeviceId}");
            logger.LogInformation($"    Manufacturer: {device.Manufacturer ?? "Unknown"}");
        }
    }

    logger.LogInformation("\nEnumerating displays...");
    var displays = await displayEnumerator.EnumerateDisplaysAsync();
    logger.LogInformation($"Found {displays.Count} display(s)\n");

    if (displays.Count > 0)
    {
        logger.LogInformation("Displays:");
        foreach (var display in displays)
        {
            logger.LogInformation($"  - {display.DeviceName}");
            logger.LogInformation($"    Resolution: {display.Width}x{display.Height}@{display.RefreshRate}Hz");
            logger.LogInformation($"    Position: ({display.PositionX}, {display.PositionY})");
            logger.LogInformation($"    Primary: {display.IsPrimary}");
            logger.LogInformation($"    Connected: {display.IsConnected}");
            logger.LogInformation($"    Type: {display.ConnectionType ?? "Unknown"}");
        }
    }

    logger.LogInformation("\nEnumerating Windows sessions...");
    var sessions = await sessionEnumerator.EnumerateSessionsAsync();
    logger.LogInformation($"Found {sessions.Count} session(s)\n");

    if (sessions.Count > 0)
    {
        logger.LogInformation("Windows Sessions:");
        foreach (var session in sessions)
        {
            logger.LogInformation($"  - Session {session.SessionId}: {session.Domain}\\{session.UserName}");
            logger.LogInformation($"    State: {session.State}");
        }
    }

    logger.LogInformation("\n=== Exporting to JSON ===\n");

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = false
    };

    var deviceOutput = new { devices = allDevices, timestamp = DateTime.UtcNow };
    var displayOutput = new { displays = displays, timestamp = DateTime.UtcNow };
    var sessionOutput = new { sessions = sessions, timestamp = DateTime.UtcNow };

    File.WriteAllText("device-list.json", JsonSerializer.Serialize(deviceOutput, jsonOptions));
    logger.LogInformation("✓ Exported device-list.json");

    File.WriteAllText("display-list.json", JsonSerializer.Serialize(displayOutput, jsonOptions));
    logger.LogInformation("✓ Exported display-list.json");

    File.WriteAllText("session-list.json", JsonSerializer.Serialize(sessionOutput, jsonOptions));
    logger.LogInformation("✓ Exported session-list.json");

    logger.LogInformation("\n=== Phase 0 POC Complete ===");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    Environment.Exit(1);
}
