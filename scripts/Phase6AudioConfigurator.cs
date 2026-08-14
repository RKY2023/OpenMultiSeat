using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Audio;
using OpenMultiSeat.Core;

var services = new ServiceCollection();

services.AddLogging(builder =>
    builder
        .ClearProviders()
        .AddConsole(config => config.IncludeScopes = true)
        .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<ISeatPersistence, SeatPersistence>();
services.AddSingleton<ISeatManager, SeatManager>();
services.AddSingleton<IAudioDeviceEnumerator, AudioDeviceEnumerator>();
services.AddSingleton<IAudioPersistence, AudioPersistence>();
services.AddSingleton<IAudioManager, AudioManager>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();
var audioManager = provider.GetRequiredService<IAudioManager>();
var seatManager = provider.GetRequiredService<ISeatManager>();

try
{
    logger.LogInformation("╔════════════════════════════════════════╗");
    logger.LogInformation("║  Phase 6: Audio Configuration Tool     ║");
    logger.LogInformation("║  Audio Device & Routing Management     ║");
    logger.LogInformation("╚════════════════════════════════════════╝\n");

    logger.LogInformation("Menu:");
    logger.LogInformation("1. List all audio devices");
    logger.LogInformation("2. List configured seats");
    logger.LogInformation("3. Assign audio device to seat");
    logger.LogInformation("4. Get audio devices for seat");
    logger.LogInformation("5. View audio topology");
    logger.LogInformation("6. Validate audio configuration");
    logger.LogInformation("7. Enable audio routing");
    logger.LogInformation("8. Disable audio routing");
    logger.LogInformation("9. Unassign audio device from seat");
    logger.LogInformation("0. Exit\n");

    bool running = true;
    while (running)
    {
        Console.Write("Select option: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await ListDevices();
                break;
            case "2":
                await ListSeats();
                break;
            case "3":
                await AssignDevice();
                break;
            case "4":
                await GetSeatDevices();
                break;
            case "5":
                await ViewTopology();
                break;
            case "6":
                await ValidateConfiguration();
                break;
            case "7":
                await EnableRouting();
                break;
            case "8":
                await DisableRouting();
                break;
            case "9":
                await UnassignDevice();
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

    logger.LogInformation("Thank you for using Audio Configuration Tool");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    Environment.Exit(1);
}

async Task ListDevices()
{
    logger.LogInformation("\n=== Audio Devices ===\n");

    try
    {
        var devices = await audioManager.GetAudioDevicesAsync();

        if (devices.Count == 0)
        {
            logger.LogWarning("No audio devices found");
            return;
        }

        logger.LogInformation($"Total: {devices.Count} device(s)\n");

        foreach (var device in devices.OrderBy(d => d.DeviceId))
        {
            var status = device.IsConnected ? "Connected" : "Disconnected";
            var default_ = device.IsDefault ? " [DEFAULT]" : "";
            var mute = device.IsMuted ? "[MUTED]" : "";

            logger.LogInformation($"► {device.DeviceName}{default_}");
            logger.LogInformation($"  ID: {device.DeviceId}");
            logger.LogInformation($"  Type: {device.DeviceType}");
            logger.LogInformation($"  Role: {device.DeviceRole}");
            logger.LogInformation($"  Status: {status} {mute}");
            logger.LogInformation($"  Volume: {device.Volume}%");
            logger.LogInformation("");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to list devices");
    }
}

async Task ListSeats()
{
    logger.LogInformation("\n=== Configured Seats ===\n");

    try
    {
        var seats = await seatManager.GetAllSeatsAsync();

        if (seats.Count == 0)
        {
            logger.LogInformation("No seats configured");
            return;
        }

        foreach (var seat in seats.OrderBy(s => s.Id))
        {
            var devices = await audioManager.GetDevicesForSeatAsync(seat.Id);

            logger.LogInformation($"► {seat.Id}: {seat.Name}");
            logger.LogInformation($"  Audio devices: {devices.Count}");
            if (devices.Count > 0)
            {
                foreach (var device in devices)
                {
                    logger.LogInformation($"    • {device.DeviceName} ({device.DeviceRole})");
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to list seats");
    }
}

async Task AssignDevice()
{
    logger.LogInformation("\n=== Assign Audio Device to Seat ===");

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    Console.Write("Device ID: ");
    var deviceId = Console.ReadLine() ?? "";

    Console.Write("Device Role (Playback/Recording/BiDirectional): ");
    var roleStr = Console.ReadLine() ?? "Playback";

    if (string.IsNullOrWhiteSpace(seatId) || string.IsNullOrWhiteSpace(deviceId))
    {
        logger.LogWarning("Invalid input");
        return;
    }

    if (!Enum.TryParse<AudioDeviceRole>(roleStr, true, out var role))
        role = AudioDeviceRole.Playback;

    try
    {
        await audioManager.AssignAudioDeviceToSeatAsync(seatId, deviceId, role);
        logger.LogInformation($"\n✓ Audio device assigned: {deviceId} → {seatId} ({role})");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to assign device");
    }
}

async Task GetSeatDevices()
{
    logger.LogInformation("\n=== Audio Devices for Seat ===");

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(seatId))
    {
        logger.LogWarning("Invalid seat ID");
        return;
    }

    try
    {
        var devices = await audioManager.GetDevicesForSeatAsync(seatId);

        logger.LogInformation($"\nSeat {seatId} has {devices.Count} audio device(s):\n");

        if (devices.Count == 0)
        {
            logger.LogInformation("  No audio devices assigned");
            return;
        }

        foreach (var device in devices)
        {
            logger.LogInformation($"  • {device.DeviceName}");
            logger.LogInformation($"    Type: {device.DeviceType}");
            logger.LogInformation($"    Role: {device.DeviceRole}");
            logger.LogInformation($"    Volume: {device.Volume}%");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get seat devices");
    }
}

async Task ViewTopology()
{
    logger.LogInformation("\n=== Audio Topology ===\n");

    try
    {
        var topology = await audioManager.GetAudioTopologyAsync();

        logger.LogInformation($"Total Devices: {topology.TotalDevices}");
        logger.LogInformation($"Connected: {topology.ConnectedDevices}");

        logger.LogInformation($"\nSeat Audio Mapping:");
        if (topology.SeatAudioMapping.Count == 0)
        {
            logger.LogInformation("  No audio devices assigned to seats");
        }
        else
        {
            foreach (var kvp in topology.SeatAudioMapping.OrderBy(x => x.Key))
            {
                logger.LogInformation($"  {kvp.Key}:");
                if (kvp.Value.PlaybackDevices.Count > 0)
                {
                    logger.LogInformation($"    Playback: {string.Join(", ", kvp.Value.PlaybackDevices)}");
                }
                if (kvp.Value.RecordingDevices.Count > 0)
                {
                    logger.LogInformation($"    Recording: {string.Join(", ", kvp.Value.RecordingDevices)}");
                }
            }
        }

        logger.LogInformation($"\nDevice List:");
        foreach (var device in topology.DeviceList.OrderBy(d => d.DeviceId))
        {
            var status = device.IsConnected ? "✓" : "✗";
            logger.LogInformation($"  {status} {device.DeviceName} ({device.DeviceType})");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get audio topology");
    }
}

async Task ValidateConfiguration()
{
    logger.LogInformation("\n=== Validating Audio Configuration ===\n");

    try
    {
        var errors = await audioManager.ValidateAudioConfigAsync();

        if (errors.Count == 0)
        {
            logger.LogInformation("✓ Audio configuration is valid");
            return;
        }

        var byType = errors.GroupBy(e => e.Severity).OrderByDescending(g => g.Key);

        foreach (var group in byType)
        {
            logger.LogInformation($"\n{group.Key} Issues ({group.Count()}):");
            foreach (var error in group)
            {
                var symbol = error.Severity switch
                {
                    ValidationSeverity.Critical => "✗",
                    ValidationSeverity.Error => "✗",
                    ValidationSeverity.Warning => "⚠",
                    _ => "ℹ"
                };

                logger.LogInformation($"  {symbol} [{error.Code}] {error.Message}");
                if (error.AffectedSeat != null)
                    logger.LogInformation($"     Seat: {error.AffectedSeat}");
                if (error.AffectedDevice != null)
                    logger.LogInformation($"     Device: {error.AffectedDevice}");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to validate configuration");
    }
}

async Task EnableRouting()
{
    logger.LogInformation("\n=== Enabling Audio Routing ===\n");

    try
    {
        var success = await audioManager.EnableAudioRoutingAsync();

        if (success)
        {
            logger.LogInformation("✓ Audio routing enabled successfully");
        }
        else
        {
            logger.LogWarning("Failed to enable audio routing. Check configuration.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to enable routing");
    }
}

async Task DisableRouting()
{
    logger.LogInformation("\n=== Disabling Audio Routing ===\n");

    try
    {
        var success = await audioManager.DisableAudioRoutingAsync();

        if (success)
        {
            logger.LogInformation("✓ Audio routing disabled");
        }
        else
        {
            logger.LogWarning("Failed to disable audio routing");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to disable routing");
    }
}

async Task UnassignDevice()
{
    logger.LogInformation("\n=== Unassign Audio Device from Seat ===");

    Console.Write("Device ID: ");
    var deviceId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(deviceId))
    {
        logger.LogWarning("Invalid device ID");
        return;
    }

    try
    {
        await audioManager.UnassignAudioDeviceFromSeatAsync(deviceId);
        logger.LogInformation($"\n✓ Audio device unassigned: {deviceId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to unassign device");
    }
}
