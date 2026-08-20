using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using System.Text.Json;
using System.Text.Json.Serialization;

var services = new ServiceCollection();

services.AddLogging(builder =>
    builder
        .ClearProviders()
        .AddConsole(config => config.IncludeScopes = true)
        .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<IDevicePersistence, DevicePersistence>();
services.AddSingleton<ISeatPersistence, SeatPersistence>();
services.AddSingleton<ISeatManager, SeatManager>();
services.AddSingleton<IHidDeviceEnumerator, HidDeviceEnumerator>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();
var seatManager = provider.GetRequiredService<ISeatManager>();
var deviceEnumerator = provider.GetRequiredService<IHidDeviceEnumerator>();

try
{
    logger.LogInformation("╔════════════════════════════════════════╗");
    logger.LogInformation("║  Phase 2: Seat Configuration Tool      ║");
    logger.LogInformation("║  Multi-Seat Mapping & Validation       ║");
    logger.LogInformation("╚════════════════════════════════════════╝\n");

    logger.LogInformation("Menu:");
    logger.LogInformation("1. List available devices");
    logger.LogInformation("2. Create seat");
    logger.LogInformation("3. List configured seats");
    logger.LogInformation("4. Assign device to seat");
    logger.LogInformation("5. View seat details");
    logger.LogInformation("6. Validate configuration");
    logger.LogInformation("7. Export configuration");
    logger.LogInformation("8. Delete seat");
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
                await CreateSeat();
                break;
            case "3":
                await ListSeats();
                break;
            case "4":
                await AssignDevice();
                break;
            case "5":
                await ViewSeat();
                break;
            case "6":
                await ValidateConfiguration();
                break;
            case "7":
                await ExportConfiguration();
                break;
            case "8":
                await DeleteSeat();
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

    logger.LogInformation("Thank you for using Seat Configurator");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    Environment.Exit(1);
}

async Task ListDevices()
{
    logger.LogInformation("\n=== Available Devices ===\n");

    var devices = await deviceEnumerator.EnumerateAllDevicesAsync();
    if (devices.Count == 0)
    {
        logger.LogWarning("No devices found");
        return;
    }

    var keyboards = devices.Where(d => d.Type == InputDeviceType.Keyboard).ToList();
    var mice = devices.Where(d => d.Type == InputDeviceType.Mouse).ToList();

    logger.LogInformation($"Keyboards ({keyboards.Count}):");
    foreach (var kb in keyboards)
    {
        logger.LogInformation($"  • {kb.ProductName ?? "Unknown"}");
        logger.LogInformation($"    ID: {kb.DeviceId}");
        logger.LogInformation($"    VID:PID: {kb.VendorId}:{kb.ProductId}");
    }

    logger.LogInformation($"\nMice ({mice.Count}):");
    foreach (var mouse in mice)
    {
        logger.LogInformation($"  • {mouse.ProductName ?? "Unknown"}");
        logger.LogInformation($"    ID: {mouse.DeviceId}");
        logger.LogInformation($"    VID:PID: {mouse.VendorId}:{mouse.ProductId}");
    }
}

async Task CreateSeat()
{
    logger.LogInformation("\n=== Create Seat ===");

    Console.Write("Seat ID (e.g., seat-1): ");
    var seatId = Console.ReadLine() ?? "";

    Console.Write("Seat Name (e.g., Workstation 1): ");
    var seatName = Console.ReadLine() ?? "";

    Console.Write("Windows User: ");
    var windowsUser = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(seatId) || string.IsNullOrWhiteSpace(windowsUser))
    {
        logger.LogWarning("Invalid input");
        return;
    }

    try
    {
        var seat = await seatManager.CreateSeatAsync(seatId, seatName, windowsUser);
        logger.LogInformation($"✓ Seat created: {seat.Id} ({seat.Name}) for {seat.WindowsUser}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create seat");
    }
}

async Task ListSeats()
{
    logger.LogInformation("\n=== Configured Seats ===\n");

    var seats = await seatManager.GetAllSeatsAsync();
    if (seats.Count == 0)
    {
        logger.LogInformation("No seats configured");
        return;
    }

    foreach (var seat in seats.OrderBy(s => s.Id))
    {
        logger.LogInformation($"► {seat.Id}: {seat.Name}");
        logger.LogInformation($"  User: {seat.WindowsUser}");
        logger.LogInformation($"  Keyboards: {seat.KeyboardIds.Count}");
        logger.LogInformation($"  Mice: {seat.MouseIds.Count}");
        logger.LogInformation($"  Status: {seat.Status}");
    }
}

async Task AssignDevice()
{
    logger.LogInformation("\n=== Assign Device to Seat ===");

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    Console.Write("Device ID: ");
    var deviceId = Console.ReadLine() ?? "";

    Console.Write("Device Type (keyboard/mouse): ");
    var typeStr = Console.ReadLine()?.ToLower() ?? "";

    var type = typeStr switch
    {
        "keyboard" => InputDeviceType.Keyboard,
        "mouse" => InputDeviceType.Mouse,
        _ => InputDeviceType.Other
    };

    if (type == InputDeviceType.Other)
    {
        logger.LogWarning("Invalid device type");
        return;
    }

    try
    {
        await seatManager.AssignDeviceToSeatAsync(seatId, deviceId, type);
        logger.LogInformation($"✓ Device assigned: {deviceId} → {seatId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to assign device");
    }
}

async Task ViewSeat()
{
    logger.LogInformation("\n=== Seat Details ===");

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    var seat = await seatManager.GetSeatAsync(seatId);
    if (seat == null)
    {
        logger.LogWarning($"Seat '{seatId}' not found");
        return;
    }

    logger.LogInformation($"\nSeat: {seat.Id}");
    logger.LogInformation($"  Name: {seat.Name}");
    logger.LogInformation($"  User: {seat.WindowsUser}");
    logger.LogInformation($"  Status: {seat.Status}");
    logger.LogInformation($"  Enabled: {seat.Enabled}");

    logger.LogInformation($"\n  Keyboards ({seat.KeyboardIds.Count}):");
    foreach (var id in seat.KeyboardIds)
        logger.LogInformation($"    • {id}");

    logger.LogInformation($"\n  Mice ({seat.MouseIds.Count}):");
    foreach (var id in seat.MouseIds)
        logger.LogInformation($"    • {id}");
}

async Task ValidateConfiguration()
{
    logger.LogInformation("\n=== Validating Configuration ===\n");

    var errors = await seatManager.ValidateConfigurationAsync();

    if (errors.Count == 0)
    {
        logger.LogInformation("✓ Configuration is valid");
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

async Task ExportConfiguration()
{
    logger.LogInformation("\n=== Exporting Configuration ===");

    var config = await seatManager.ExportConfigurationAsync();

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    var json = JsonSerializer.Serialize(config, jsonOptions);
    var filePath = "seat-configuration.json";
    await File.WriteAllTextAsync(filePath, json);

    logger.LogInformation($"\n✓ Configuration exported to: {filePath}");
    logger.LogInformation($"  Seats: {config.Seats.Count}");
    logger.LogInformation($"  Total devices assigned: {config.Seats.Sum(s => s.KeyboardIds.Count + s.MouseIds.Count)}");
}

async Task DeleteSeat()
{
    logger.LogInformation("\n=== Delete Seat ===");

    Console.Write("Seat ID to delete: ");
    var seatId = Console.ReadLine() ?? "";

    Console.Write("Are you sure? (yes/no): ");
    var confirm = Console.ReadLine()?.ToLower();

    if (confirm != "yes")
    {
        logger.LogInformation("Cancelled");
        return;
    }

    try
    {
        await seatManager.DeleteSeatAsync(seatId);
        logger.LogInformation($"✓ Seat deleted: {seatId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to delete seat");
    }
}
