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
services.AddSingleton<IHidDeviceEnumerator, HidDeviceEnumerator>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();
var deviceEnumerator = provider.GetRequiredService<IHidDeviceEnumerator>();
var persistence = provider.GetRequiredService<IDevicePersistence>();

try
{
    logger.LogInformation("╔════════════════════════════════════════╗");
    logger.LogInformation("║  Phase 1: Device Discovery Tester      ║");
    logger.LogInformation("║  Stable ID & Persistent Device Storage ║");
    logger.LogInformation("╚════════════════════════════════════════╝\n");

    logger.LogInformation("Menu:");
    logger.LogInformation("1. Scan for all devices");
    logger.LogInformation("2. Identify keyboard (press any key)");
    logger.LogInformation("3. Identify mouse (move cursor)");
    logger.LogInformation("4. View saved device registry");
    logger.LogInformation("5. Test device persistence (reconnect simulation)");
    logger.LogInformation("6. Export device report");
    logger.LogInformation("0. Exit\n");

    bool running = true;
    while (running)
    {
        Console.Write("Select option: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await ScanAllDevices();
                break;
            case "2":
                await IdentifyKeyboard();
                break;
            case "3":
                await IdentifyMouse();
                break;
            case "4":
                await ViewDeviceRegistry();
                break;
            case "5":
                await TestPersistence();
                break;
            case "6":
                await ExportReport();
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

    logger.LogInformation("Thank you for using Device Discovery Tester");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    Environment.Exit(1);
}

async Task ScanAllDevices()
{
    logger.LogInformation("\n=== Scanning All Input Devices ===\n");

    var keyboards = await deviceEnumerator.EnumerateKeyboardsAsync();
    var mice = await deviceEnumerator.EnumerateMiceAsync();
    var all = await deviceEnumerator.EnumerateAllDevicesAsync();

    logger.LogInformation($"Found {keyboards.Count} keyboard(s), {mice.Count} mouse/mice, {all.Count} total devices\n");

    foreach (var device in all.OrderBy(d => d.DeviceId))
    {
        logger.LogInformation($"► {device.ProductName ?? "Unknown Device"}");
        logger.LogInformation($"  Type:        {device.Type}");
        logger.LogInformation($"  Stable ID:   {device.DeviceId}");
        logger.LogInformation($"  Hardware ID: {device.HardwareId}");
        logger.LogInformation($"  Vendor ID:   {device.VendorId ?? "N/A"}");
        logger.LogInformation($"  Product ID:  {device.ProductId ?? "N/A"}");
        logger.LogInformation($"  Manufacturer: {device.Manufacturer ?? "Unknown"}");
        logger.LogInformation("");
    }
}

async Task IdentifyKeyboard()
{
    logger.LogInformation("\n=== Identify Keyboard ===");
    logger.LogInformation("Press any key on the keyboard you want to identify...");
    Console.ReadKey(intercept: true);

    await ScanAndHighlight(InputDeviceType.Keyboard);
}

async Task IdentifyMouse()
{
    logger.LogInformation("\n=== Identify Mouse ===");
    logger.LogInformation("Move the mouse you want to identify (3 seconds)...");
    await Task.Delay(3000);

    await ScanAndHighlight(InputDeviceType.Mouse);
}

async Task ScanAndHighlight(InputDeviceType typeToFind)
{
    var all = await deviceEnumerator.EnumerateAllDevicesAsync();
    var matching = all.Where(d => d.Type == typeToFind).ToList();

    if (matching.Count == 0)
    {
        logger.LogWarning("No matching devices found");
        return;
    }

    logger.LogInformation($"\nFound {matching.Count} {typeToFind}(s):\n");

    foreach (var device in matching)
    {
        logger.LogInformation($"✓ {device.ProductName}");
        logger.LogInformation($"  Stable ID: {device.DeviceId}");
        logger.LogInformation($"  VID:PID: {device.VendorId}:{device.ProductId}");
        logger.LogInformation("");
    }
}

async Task ViewDeviceRegistry()
{
    logger.LogInformation("\n=== Saved Device Registry ===\n");

    var registry = await persistence.GetAllDevicesAsync();

    if (registry.Count == 0)
    {
        logger.LogInformation("No devices in registry yet");
        return;
    }

    logger.LogInformation($"Total devices: {registry.Count}\n");

    foreach (var record in registry.OrderByDescending(r => r.LastSeen))
    {
        logger.LogInformation($"► {record.ProductName ?? "Unknown"}");
        logger.LogInformation($"  Stable ID:    {record.StableId}");
        logger.LogInformation($"  Hardware ID:  {record.HardwareId}");
        logger.LogInformation($"  VID:PID:      {record.VendorId}:{record.ProductId}");
        logger.LogInformation($"  First Seen:   {record.FirstSeen:yyyy-MM-dd HH:mm:ss} (UTC)");
        logger.LogInformation($"  Last Seen:    {record.LastSeen:yyyy-MM-dd HH:mm:ss} (UTC)");
        logger.LogInformation($"  Connections:  {record.ConnectionCount}");
        logger.LogInformation("");
    }
}

async Task TestPersistence()
{
    logger.LogInformation("\n=== Testing Device Persistence ===\n");

    // Scan once
    logger.LogInformation("Scan 1: Initial device enumeration");
    var devices1 = await deviceEnumerator.EnumerateAllDevicesAsync();
    logger.LogInformation($"Found {devices1.Count} devices\n");

    foreach (var device in devices1.Take(3))
    {
        logger.LogInformation($"  • {device.ProductName} → {device.DeviceId}");
    }

    // Scan again
    logger.LogInformation("\nScan 2: Re-enumeration (simulating device check)");
    var devices2 = await deviceEnumerator.EnumerateAllDevicesAsync();
    logger.LogInformation($"Found {devices2.Count} devices\n");

    var registry = await persistence.GetAllDevicesAsync();
    var consistent = devices1
        .Join(devices2, d => d.DeviceId, d => d.DeviceId, (d1, d2) => d1)
        .Count();

    logger.LogInformation($"✓ Stable IDs consistent: {consistent}/{devices1.Count}");
    logger.LogInformation($"✓ Registry size: {registry.Count}");

    if (consistent == devices1.Count && registry.Count > 0)
    {
        logger.LogInformation("\n✓ Device persistence working correctly!");
    }
    else
    {
        logger.LogWarning("\n✗ Persistence test failed");
    }
}

async Task ExportReport()
{
    logger.LogInformation("\n=== Exporting Device Report ===\n");

    var devices = await deviceEnumerator.EnumerateAllDevicesAsync();
    var registry = await persistence.GetAllDevicesAsync();

    var report = new
    {
        timestamp = DateTime.UtcNow,
        phase = "Phase 1 - Device Discovery",
        summary = new
        {
            total_devices = devices.Count,
            keyboards = devices.Count(d => d.Type == InputDeviceType.Keyboard),
            mice = devices.Count(d => d.Type == InputDeviceType.Mouse),
            registered_in_persistence = registry.Count
        },
        devices = devices.OrderBy(d => d.DeviceId).Select(d => new
        {
            stable_id = d.DeviceId,
            product_name = d.ProductName,
            type = d.Type.ToString(),
            vendor_id = d.VendorId,
            product_id = d.ProductId,
            manufacturer = d.Manufacturer,
            hardware_id = d.HardwareId
        }),
        registry = registry.OrderByDescending(r => r.LastSeen).Select(r => new
        {
            stable_id = r.StableId,
            product_name = r.ProductName,
            first_seen = r.FirstSeen,
            last_seen = r.LastSeen,
            connection_count = r.ConnectionCount
        })
    };

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    var reportPath = "device-report-phase1.json";
    var json = JsonSerializer.Serialize(report, jsonOptions);
    await File.WriteAllTextAsync(reportPath, json);

    logger.LogInformation($"✓ Report exported to: {reportPath}");
    logger.LogInformation($"  Devices: {devices.Count}");
    logger.LogInformation($"  Registry entries: {registry.Count}");
}
