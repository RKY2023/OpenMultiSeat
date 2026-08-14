using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Displays;
using System.Text.Json;
using System.Text.Json.Serialization;

var services = new ServiceCollection();

services.AddLogging(builder =>
    builder
        .ClearProviders()
        .AddConsole(config => config.IncludeScopes = true)
        .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<IDisplayEnumerator, DisplayEnumerator>();
services.AddSingleton<ISeatPersistence, SeatPersistence>();
services.AddSingleton<ISeatManager, SeatManager>();
services.AddSingleton<IDevicePersistence, DevicePersistence>();
services.AddSingleton<IDisplayManager, DisplayManager>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();
var displayManager = provider.GetRequiredService<IDisplayManager>();
var seatManager = provider.GetRequiredService<ISeatManager>();

try
{
    logger.LogInformation("╔════════════════════════════════════════╗");
    logger.LogInformation("║  Phase 4: Display Configuration Tool   ║");
    logger.LogInformation("║  Display Assignment & Topology         ║");
    logger.LogInformation("╚════════════════════════════════════════╝\n");

    logger.LogInformation("Menu:");
    logger.LogInformation("1. List all displays");
    logger.LogInformation("2. List configured seats");
    logger.LogInformation("3. Assign display to seat");
    logger.LogInformation("4. Get displays for seat");
    logger.LogInformation("5. View display topology");
    logger.LogInformation("6. Validate display configuration");
    logger.LogInformation("7. Export display layout");
    logger.LogInformation("8. Unassign display from seat");
    logger.LogInformation("0. Exit\n");

    bool running = true;
    while (running)
    {
        Console.Write("Select option: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await ListDisplays();
                break;
            case "2":
                await ListSeats();
                break;
            case "3":
                await AssignDisplay();
                break;
            case "4":
                await GetSeatDisplays();
                break;
            case "5":
                await ViewTopology();
                break;
            case "6":
                await ValidateConfiguration();
                break;
            case "7":
                await ExportLayout();
                break;
            case "8":
                await UnassignDisplay();
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

    logger.LogInformation("Thank you for using Display Configuration Tool");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    Environment.Exit(1);
}

async Task ListDisplays()
{
    logger.LogInformation("\n=== System Displays ===\n");

    try
    {
        var displays = await displayManager.GetAllDisplaysAsync();

        if (displays.Count == 0)
        {
            logger.LogWarning("No displays found");
            return;
        }

        logger.LogInformation($"Total: {displays.Count} display(s)\n");

        foreach (var display in displays.OrderBy(d => d.DisplayId))
        {
            var status = display.IsConnected ? "Connected" : "Disconnected";
            var primary = display.IsPrimary ? " [PRIMARY]" : "";

            logger.LogInformation($"► {display.DeviceName}{primary}");
            logger.LogInformation($"  ID: {display.DisplayId}");
            logger.LogInformation($"  Resolution: {display.Width}x{display.Height}@{display.RefreshRate}Hz");
            logger.LogInformation($"  Position: ({display.PositionX}, {display.PositionY})");
            logger.LogInformation($"  Type: {display.ConnectionType ?? "Unknown"}");
            logger.LogInformation($"  Status: {status}");
            logger.LogInformation("");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to list displays");
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
            logger.LogInformation($"► {seat.Id}: {seat.Name}");
            logger.LogInformation($"  Displays: {seat.DisplayIds.Count}");
            if (seat.DisplayIds.Count > 0)
            {
                foreach (var displayId in seat.DisplayIds)
                {
                    logger.LogInformation($"    • {displayId}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to list seats");
    }
}

async Task AssignDisplay()
{
    logger.LogInformation("\n=== Assign Display to Seat ===");

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    Console.Write("Display ID: ");
    var displayId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(seatId) || string.IsNullOrWhiteSpace(displayId))
    {
        logger.LogWarning("Invalid input");
        return;
    }

    try
    {
        await displayManager.AssignDisplayToSeatAsync(seatId, displayId);
        logger.LogInformation($"\n✓ Display assigned: {displayId} → {seatId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to assign display");
    }
}

async Task GetSeatDisplays()
{
    logger.LogInformation("\n=== Displays for Seat ===");

    Console.Write("Seat ID: ");
    var seatId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(seatId))
    {
        logger.LogWarning("Invalid seat ID");
        return;
    }

    try
    {
        var displays = await displayManager.GetDisplaysForSeatAsync(seatId);

        logger.LogInformation($"\nSeat {seatId} has {displays.Count} display(s):\n");

        if (displays.Count == 0)
        {
            logger.LogInformation("  No displays assigned");
            return;
        }

        foreach (var display in displays)
        {
            logger.LogInformation($"  • {display.DeviceName}");
            logger.LogInformation($"    Resolution: {display.Width}x{display.Height}@{display.RefreshRate}Hz");
            logger.LogInformation($"    Position: ({display.PositionX}, {display.PositionY})");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get seat displays");
    }
}

async Task ViewTopology()
{
    logger.LogInformation("\n=== Display Topology ===\n");

    try
    {
        var topology = await displayManager.GetDisplayTopologyAsync();

        logger.LogInformation($"Total Displays: {topology.TotalDisplays}");
        logger.LogInformation($"Connected: {topology.ConnectedDisplays}");
        logger.LogInformation($"Primary: Display {topology.PrimaryDisplay}");

        logger.LogInformation($"\nPer-Seat Display Count:");
        foreach (var kvp in topology.SeatDisplayCount.OrderBy(x => x.Key))
        {
            logger.LogInformation($"  {kvp.Key}: {kvp.Value} display(s)");
        }

        logger.LogInformation($"\nDetailed Display List:");
        foreach (var display in topology.DisplayList.OrderBy(d => d.DisplayId))
        {
            var status = display.IsConnected ? "✓" : "✗";
            var primary = display.IsPrimary ? " [P]" : "";
            logger.LogInformation($"  {status} {display.DeviceName}{primary} ({display.Width}x{display.Height})");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get display topology");
    }
}

async Task ValidateConfiguration()
{
    logger.LogInformation("\n=== Validating Display Configuration ===\n");

    try
    {
        var errors = await displayManager.ValidateDisplayConfigurationAsync();

        if (errors.Count == 0)
        {
            logger.LogInformation("✓ Display configuration is valid");
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
                    logger.LogInformation($"     Display: {error.AffectedDevice}");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to validate configuration");
    }
}

async Task ExportLayout()
{
    logger.LogInformation("\n=== Exporting Display Layout ===");

    try
    {
        var layout = await displayManager.ExportDisplayLayoutAsync();

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(layout, jsonOptions);
        var filePath = "display-layout.json";
        await File.WriteAllTextAsync(filePath, json);

        logger.LogInformation($"\n✓ Layout exported to: {filePath}");
        logger.LogInformation($"  Displays: {layout.Displays.Count}");
        logger.LogInformation($"  Seat mappings: {layout.SeatDisplayMappings.Count}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to export layout");
    }
}

async Task UnassignDisplay()
{
    logger.LogInformation("\n=== Unassign Display from Seat ===");

    Console.Write("Display ID: ");
    var displayId = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(displayId))
    {
        logger.LogWarning("Invalid display ID");
        return;
    }

    try
    {
        await displayManager.UnassignDisplayFromSeatAsync(displayId);
        logger.LogInformation($"\n✓ Display unassigned: {displayId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to unassign display");
    }
}
