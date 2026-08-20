using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.InputIsolation;

var services = new ServiceCollection();

services.AddLogging(builder =>
    builder
        .ClearProviders()
        .AddConsole(config => config.IncludeScopes = true)
        .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<IDevicePersistence, DevicePersistence>();
services.AddSingleton<ISeatPersistence, SeatPersistence>();
services.AddSingleton<ISeatManager, SeatManager>();
services.AddSingleton<IInputIsolationPersistence, InputIsolationPersistence>();
services.AddSingleton<IInputIsolationService, InputIsolationService>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();
var isolationService = provider.GetRequiredService<IInputIsolationService>();
var seatManager = provider.GetRequiredService<ISeatManager>();
var devicePersistence = provider.GetRequiredService<IDevicePersistence>();

try
{
    logger.LogInformation("╔════════════════════════════════════════╗");
    logger.LogInformation("║  Phase 5: Input Isolation Tester       ║");
    logger.LogInformation("║  Device-to-Seat Binding & Isolation    ║");
    logger.LogInformation("╚════════════════════════════════════════╝\n");

    logger.LogInformation("Menu:");
    logger.LogInformation("1. List all input devices");
    logger.LogInformation("2. List configured seats");
    logger.LogInformation("3. Bind device to seat");
    logger.LogInformation("4. Get bindings for seat");
    logger.LogInformation("5. View isolation status");
    logger.LogInformation("6. Enable input isolation");
    logger.LogInformation("7. Disable input isolation");
    logger.LogInformation("8. Validate isolation configuration");
    logger.LogInformation("9. Unbind device from seat");
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
                await BindDevice();
                break;
            case "4":
                await GetSeatBindings();
                break;
            case "5":
                await ViewStatus();
                break;
            case "6":
                await EnableIsolation();
                break;
            case "7":
                await DisableIsolation();
                break;
            case "8":
                await ValidateConfiguration();
                break;
            case "9":
                await UnbindDevice();
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

    logger.LogInformation("Thank you for using Input Isolation Tester");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    Environment.Exit(1);
}

async Task ListDevices()
{
    logger.LogInformation("\n=== Input Devices ===\n");

    try
    {
        var devices = await devicePersistence.GetAllDevicesAsync();
        var bindings = await isolationService.GetInputBindingsAsync();

        if (devices.Count == 0)
        {
            logger.LogWarning("No devices found");
            return;
        }

        logger.LogInformation($"Total: {devices.Count} device(s)\n");

        foreach (var device in devices.OrderBy(d => d.StableId))
        {
            var binding = bindings.FirstOrDefault(b => b.DeviceId == device.StableId);
            var status = binding != null ? $"[Bound to {binding.SeatId}]" : "[Unbound]";

            logger.LogInformation($"► {device.ProductName ?? "Unknown Device"} {status}");
            logger.LogInformation($"  ID: {device.StableId}");
            logger.LogInformation($"  VID:PID: {device.VendorId:X4}:{device.ProductId:X4}");
            logger.LogInformation($"  Hardware ID: {device.HardwareId}");
            if (binding != null)
            {
                logger.LogInformation($"  Bound to: {binding.SeatId} ({binding.DeviceType})");
            }
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
            var bindings = await isolationService.GetBindingsForSeatAsync(seat.Id);

            logger.LogInformation($"► {seat.Id}: {seat.Name}");
            logger.LogInformation($"  Bound devices: {bindings.Count}");
            if (bindings.Count > 0)
            {
                foreach (var binding in bindings)
                {
                    logger.LogInformation($"    • {binding.DeviceName} ({binding.DeviceType})");
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to list seats");
    }
}

async Task BindDevice()
{
    logger.LogInformation("\n=== Bind Device to Seat ===");

    Console.Write("Device ID (stable ID): ");
    var deviceId = Console.ReadLine() ?? "";

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(seatId))
    {
        logger.LogWarning("Invalid input");
        return;
    }

    try
    {
        await isolationService.BindDeviceToSeatAsync(deviceId, seatId);
        logger.LogInformation($"\n✓ Device bound: {deviceId} → {seatId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to bind device");
    }
}

async Task GetSeatBindings()
{
    logger.LogInformation("\n=== Bindings for Seat ===");

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(seatId))
    {
        logger.LogWarning("Invalid seat ID");
        return;
    }

    try
    {
        var bindings = await isolationService.GetBindingsForSeatAsync(seatId);

        logger.LogInformation($"\nSeat {seatId} has {bindings.Count} device(s) bound:\n");

        if (bindings.Count == 0)
        {
            logger.LogInformation("  No devices bound");
            return;
        }

        foreach (var binding in bindings)
        {
            var status = binding.IsActive ? "Active" : "Inactive";
            logger.LogInformation($"  • {binding.DeviceName}");
            logger.LogInformation($"    Type: {binding.DeviceType}");
            logger.LogInformation($"    Status: {status}");
            logger.LogInformation($"    Bound at: {binding.BoundAt:yyyy-MM-dd HH:mm:ss}");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get seat bindings");
    }
}

async Task ViewStatus()
{
    logger.LogInformation("\n=== Isolation Status ===\n");

    try
    {
        var status = await isolationService.GetIsolationStatusAsync();

        logger.LogInformation($"Isolation Enabled: {(status.IsEnabled ? "Yes ✓" : "No ✗")}");
        logger.LogInformation($"Total Devices: {status.TotalDevices}");
        logger.LogInformation($"Bound Devices: {status.BoundDevices}");
        logger.LogInformation($"Unbound Devices: {status.UnboundDevices}");

        logger.LogInformation($"\nDevices per Seat:");
        foreach (var kvp in status.DevicesPerSeat.OrderBy(x => x.Key))
        {
            logger.LogInformation($"  {kvp.Key}: {kvp.Value} device(s)");
        }

        logger.LogInformation($"\nActive Bindings: {status.ActiveBindings.Count}");
        foreach (var binding in status.ActiveBindings)
        {
            logger.LogInformation($"  • {binding.DeviceName} → {binding.SeatId}");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get isolation status");
    }
}

async Task EnableIsolation()
{
    logger.LogInformation("\n=== Enabling Input Isolation ===\n");

    try
    {
        var success = await isolationService.EnableIsolationAsync();

        if (success)
        {
            logger.LogInformation("✓ Input isolation enabled successfully");
        }
        else
        {
            logger.LogWarning("Failed to enable input isolation. Check configuration.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to enable isolation");
    }
}

async Task DisableIsolation()
{
    logger.LogInformation("\n=== Disabling Input Isolation ===\n");

    try
    {
        var success = await isolationService.DisableIsolationAsync();

        if (success)
        {
            logger.LogInformation("✓ Input isolation disabled");
        }
        else
        {
            logger.LogWarning("Failed to disable input isolation");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to disable isolation");
    }
}

async Task ValidateConfiguration()
{
    logger.LogInformation("\n=== Validating Isolation Configuration ===\n");

    try
    {
        var errors = await isolationService.ValidateIsolationConfigAsync();

        if (errors.Count == 0)
        {
            logger.LogInformation("✓ Input isolation configuration is valid");
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

async Task UnbindDevice()
{
    logger.LogInformation("\n=== Unbind Device from Seat ===");

    Console.Write("Device ID (stable ID): ");
    var deviceId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(deviceId))
    {
        logger.LogWarning("Invalid device ID");
        return;
    }

    try
    {
        await isolationService.UnbindDeviceFromSeatAsync(deviceId);
        logger.LogInformation($"\n✓ Device unbound: {deviceId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to unbind device");
    }
}
