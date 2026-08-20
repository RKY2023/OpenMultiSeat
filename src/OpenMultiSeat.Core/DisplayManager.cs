using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Core;

public interface IDisplayManager
{
    Task<Display?> GetDisplayAsync(string displayId);
    Task<IReadOnlyList<Display>> GetAllDisplaysAsync();
    Task<IReadOnlyList<Display>> GetDisplaysForSeatAsync(string seatId);
    Task AssignDisplayToSeatAsync(string seatId, string displayId);
    Task UnassignDisplayFromSeatAsync(string displayId);
    Task<DisplayTopology> GetDisplayTopologyAsync();
    Task<IReadOnlyList<ValidationError>> ValidateDisplayConfigurationAsync();
    Task<DisplayLayout> ExportDisplayLayoutAsync();
    Task ImportDisplayLayoutAsync(DisplayLayout layout);
}

public class DisplayTopology
{
    public required int TotalDisplays { get; set; }
    public required int ConnectedDisplays { get; set; }
    public required int PrimaryDisplay { get; set; }
    public required List<Display> DisplayList { get; set; }
    public required Dictionary<string, int> SeatDisplayCount { get; set; }
}

public class DisplayLayout
{
    public int Version { get; set; } = 1;
    public required List<Display> Displays { get; set; }
    public required Dictionary<string, List<string>> SeatDisplayMappings { get; set; }
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}

public class DisplayManager : IDisplayManager
{
    private readonly ILogger<DisplayManager> _logger;
    private readonly IDisplayEnumerator _enumerator;
    private readonly ISeatManager _seatManager;

    private Dictionary<string, Display> _displayCache = [];
    private Dictionary<string, string> _displayToSeatMap = []; // displayId -> seatId

    public DisplayManager(
        ILogger<DisplayManager> logger,
        IDisplayEnumerator enumerator,
        ISeatManager seatManager)
    {
        _logger = logger;
        _enumerator = enumerator;
        _seatManager = seatManager;
    }

    public async Task<Display?> GetDisplayAsync(string displayId)
    {
        await RefreshCacheAsync();
        return _displayCache.TryGetValue(displayId, out var display) ? display : null;
    }

    public async Task<IReadOnlyList<Display>> GetAllDisplaysAsync()
    {
        await RefreshCacheAsync();
        return _displayCache.Values.ToList();
    }

    public async Task<IReadOnlyList<Display>> GetDisplaysForSeatAsync(string seatId)
    {
        var seat = await _seatManager.GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        await RefreshCacheAsync();
        var seatDisplayIds = _displayToSeatMap
            .Where(kvp => kvp.Value == seatId)
            .Select(kvp => kvp.Key)
            .ToList();

        return seatDisplayIds
            .Select(id => _displayCache.TryGetValue(id, out var display) ? display : null)
            .Where(d => d != null)
            .ToList()!;
    }

    public async Task AssignDisplayToSeatAsync(string seatId, string displayId)
    {
        var seat = await _seatManager.GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        await RefreshCacheAsync();

        var display = await GetDisplayAsync(displayId);
        if (display == null)
            throw new InvalidOperationException($"Display '{displayId}' not found");

        // Check if display already assigned to another seat
        if (_displayToSeatMap.TryGetValue(displayId, out var existingSeat))
        {
            if (existingSeat == seatId)
            {
                _logger.LogInformation($"Display {displayId} already assigned to seat {seatId}");
                return;
            }

            throw new InvalidOperationException(
                $"Display '{displayId}' is already assigned to seat '{existingSeat}'");
        }

        _displayToSeatMap[displayId] = seatId;
        seat.DisplayIds.Add(displayId);
        await _seatManager.UpdateSeatAsync(seat);

        _logger.LogInformation($"Display {displayId} assigned to seat {seatId}");
    }

    public async Task UnassignDisplayFromSeatAsync(string displayId)
    {
        if (!_displayToSeatMap.TryGetValue(displayId, out var seatId))
        {
            _logger.LogWarning($"Display {displayId} not assigned to any seat");
            return;
        }

        var seat = await _seatManager.GetSeatAsync(seatId);
        if (seat != null)
        {
            seat.DisplayIds.Remove(displayId);
            await _seatManager.UpdateSeatAsync(seat);
        }

        _displayToSeatMap.Remove(displayId);
        _logger.LogInformation($"Display {displayId} unassigned from seat {seatId}");
    }

    public async Task<DisplayTopology> GetDisplayTopologyAsync()
    {
        await RefreshCacheAsync();
        var seats = await _seatManager.GetAllSeatsAsync();

        var seatDisplayCount = new Dictionary<string, int>();
        foreach (var seat in seats)
        {
            seatDisplayCount[seat.Id] = seat.DisplayIds.Count;
        }

        var primaryDisplay = _displayCache.Values.FirstOrDefault(d => d.IsPrimary)?.DisplayId ?? "UNKNOWN";

        return new DisplayTopology
        {
            TotalDisplays = _displayCache.Count,
            ConnectedDisplays = _displayCache.Values.Count(d => d.IsConnected),
            PrimaryDisplay = int.Parse(primaryDisplay.Split('_').Last()),
            DisplayList = _displayCache.Values.ToList(),
            SeatDisplayCount = seatDisplayCount
        };
    }

    public async Task<IReadOnlyList<ValidationError>> ValidateDisplayConfigurationAsync()
    {
        var errors = new List<ValidationError>();
        await RefreshCacheAsync();

        var displays = await GetAllDisplaysAsync();
        var seats = await _seatManager.GetAllSeatsAsync();

        // Check for duplicate assignments
        var allAssignedDisplays = new HashSet<string>();

        foreach (var seat in seats)
        {
            foreach (var displayId in seat.DisplayIds)
            {
                if (allAssignedDisplays.Contains(displayId))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DUPLICATE_DISPLAY_ASSIGNMENT",
                        Message = $"Display '{displayId}' assigned to multiple seats",
                        AffectedDevice = displayId,
                        Severity = ValidationSeverity.Critical
                    });
                }
                else
                {
                    allAssignedDisplays.Add(displayId);
                }

                // Verify display exists
                if (displays.FirstOrDefault(d => d.DisplayId == displayId) == null)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DISPLAY_NOT_FOUND",
                        Message = $"Display '{displayId}' not found in system",
                        AffectedSeat = seat.Id,
                        AffectedDevice = displayId,
                        Severity = ValidationSeverity.Warning
                    });
                }
            }

            // Warn if no displays assigned
            if (seat.DisplayIds.Count == 0)
            {
                errors.Add(new ValidationError
                {
                    Code = "NO_DISPLAYS",
                    Message = $"Seat '{seat.Id}' has no displays assigned",
                    AffectedSeat = seat.Id,
                    Severity = ValidationSeverity.Warning
                });
            }
        }

        // Check for unassigned displays
        var unassignedCount = displays.Count(d => !allAssignedDisplays.Contains(d.DisplayId));
        if (unassignedCount > 0)
        {
            errors.Add(new ValidationError
            {
                Code = "UNASSIGNED_DISPLAYS",
                Message = $"{unassignedCount} display(s) not assigned to any seat",
                Severity = ValidationSeverity.Info
            });
        }

        if (errors.Any(e => e.Severity == ValidationSeverity.Critical))
        {
            _logger.LogError($"Display configuration validation failed: {errors.Count} error(s)");
        }
        else
        {
            _logger.LogInformation($"Display configuration validation complete: {errors.Count} issue(s)");
        }

        return errors;
    }

    public async Task<DisplayLayout> ExportDisplayLayoutAsync()
    {
        var displays = await GetAllDisplaysAsync();
        var seats = await _seatManager.GetAllSeatsAsync();

        var seatDisplayMappings = new Dictionary<string, List<string>>();
        foreach (var seat in seats)
        {
            seatDisplayMappings[seat.Id] = seat.DisplayIds.ToList();
        }

        return new DisplayLayout
        {
            Displays = displays.ToList(),
            SeatDisplayMappings = seatDisplayMappings
        };
    }

    public async Task ImportDisplayLayoutAsync(DisplayLayout layout)
    {
        if (layout.Version != 1)
            throw new InvalidOperationException($"Unsupported layout version: {layout.Version}");

        _displayCache.Clear();
        _displayToSeatMap.Clear();

        foreach (var display in layout.Displays)
        {
            _displayCache[display.DisplayId] = display;
        }

        var seats = await _seatManager.GetAllSeatsAsync();
        foreach (var seat in seats)
        {
            seat.DisplayIds.Clear();
        }

        foreach (var kvp in layout.SeatDisplayMappings)
        {
            var seat = seats.FirstOrDefault(s => s.Id == kvp.Key);
            if (seat != null)
            {
                foreach (var displayId in kvp.Value)
                {
                    seat.DisplayIds.Add(displayId);
                    _displayToSeatMap[displayId] = kvp.Key;
                }
                await _seatManager.UpdateSeatAsync(seat);
            }
        }

        _logger.LogInformation($"Display layout imported: {layout.Displays.Count} display(s)");
    }

    private async Task RefreshCacheAsync()
    {
        try
        {
            var displays = await _enumerator.EnumerateDisplaysAsync();
            _displayCache = displays.ToDictionary(d => d.DisplayId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing display cache");
        }
    }
}
